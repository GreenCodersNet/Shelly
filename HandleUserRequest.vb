' ###  DEBUG ###

' ##########################################################
'  Shelly - v1.0.1
'  License: Creative Commons Attribution-NonCommercial (CC BY-NC)
'  https://creativecommons.org/licenses/by-nc/4.0/
'  © 2025 Vlad Stefanescu | GreenCoders.net. Attribution required.
' ##########################################################

Imports System.Net.Http
Imports System.Text
Imports System.Text.RegularExpressions
Imports System.Threading
Imports Newtonsoft.Json
Imports Newtonsoft.Json.Linq

Module HandleUserRequest


    ' ================================
    '  HandleUserRequestAsync  � v5.0 ULTIMATE AI
    '  Enhanced with Goal Validation and Intelligent Retry
    ' ================================

    Public Async Function HandleUserRequestAsync(ct As CancellationToken) As Task
        Dim originalQuestion As String = Shelly.UserInputBox.Text.Trim()
        ' Store original request for goal validation
        Globals.OriginalUserRequest = originalQuestion
        If String.IsNullOrWhiteSpace(originalQuestion) Then
            Shelly.LabelStatusUpdate.Text = "Please enter a question or command."
            Return
        End If

        Try
            ' -- Reset typing flag and set active cancellation token ---
            FileHandler.typingStopped = False
            FileHandler.ActiveCancellationToken = ct

            ' -- ?? NEW: Initialize context-aware iteration system ---
            Globals.CurrentRequestId = Guid.NewGuid().ToString()
            Globals.CurrentIteration = 0
            StepOutputManager.Instance.StartNewRequest(Globals.CurrentRequestId)

            Debug.WriteLine($"[HandleUserRequest] Started new request: {Globals.CurrentRequestId}")

            ' -- ?? NEW: Reset retry strategy for new session ---
            RetryStrategy.Reset()

            ' -- ?? NEW: Reset execution ledger for new session ---
            ExecutorAgent.ResetExecutionLedger()

            ' -- UI prep -------------------------------------------
            Shelly.SetUIState(False)
            Shelly.CancelTaskButton.Enabled = True
            Await Shelly.WebView21.CoreWebView2.ExecuteScriptAsync("setColorDefault();")
            Await Shelly.WebView21.CoreWebView2.ExecuteScriptAsync("setColorGreen();")

            ' -- Optional "revise prompt" pass ---------------------
            Dim userQuestion As String = originalQuestion
            If Globals.UsePromptRevision Then
                userQuestion = Await ReviseUserQuestionAsync(originalQuestion, ct)
                If String.IsNullOrWhiteSpace(userQuestion) Then
                    Shelly.LabelStatusUpdate.Text = "Could not revise the question."
                    Return
                End If
            End If

            ' -- Add user message to history (with trimming) -------
            Await convHistory.TrimConversationHistoryByTokens_Dict(conversationHistory, Globals.MaxTotalTokens, userQuestion)
            conversationHistory.Add(ConversationHistoryFunctions.CreateHistoryMessage("user", userQuestion))
            InteractionLog.AppendInteraction("user", userQuestion)

            ' -- ?? INTELLIGENT PLANNER ? EXECUTOR LOOP ----------------
            Const MAX_ITER As Integer = 10
            Dim iteration As Integer = 0
            Dim taskDone As Boolean = False
            Dim completedSteps As New List(Of String)()
            Dim customFunctionUsed As Boolean = False
            lastRunMultiTask = False

            Do While iteration < MAX_ITER AndAlso Not taskDone AndAlso Not ct.IsCancellationRequested
                iteration += 1

                ' -- ?? UPDATE: Increment iteration counter ---
                Globals.CurrentIteration = iteration
                StepOutputManager.Instance.IncrementIteration()

                Shelly.LabelStatusUpdate.Text = $"Planning iteration #{iteration}/{MAX_ITER}�"
                Debug.WriteLine($"[HandleUserRequest] === Iteration {iteration} ===")

                ' 1?? ?? Build outcome digest for planner feedback
                Dim recentOutcomeDigest As String = OutcomeHistory.BuildRecentOutcomeDigest(conversationHistory, 5)

                ' ?? 2?? Build context summary from previous step outputs
                Dim previousOutputsSummary As String = StepOutputManager.Instance.BuildContextSummary()
                Debug.WriteLine($"[HandleUserRequest] Context summary length: {previousOutputsSummary.Length} chars")

                ' 2?? ?? Get retry guidance if there are failures
                Dim retryGuidance As String = ""
                If GoalValidator.HasRecoverableFailures() Then
                    retryGuidance = GoalValidator.GetRetryGuidance() & vbLf &
                                   RetryStrategy.GetRetryStats()
                    Debug.WriteLine($"[HandleUserRequest] Retry guidance: {retryGuidance}")
                End If

                Dim retryStats As String = RetryStrategy.GetRetryStats()
                Dim retryRemaining As String = RetryStrategy.GetRemainingStats()

                ' Track already successful tool+args combinations to prevent repeats
                Dim completedSignatures = GlobalOutcomeTracker.Instance.GetSuccessfulSignatures(10)
                Dim completedSignatureText As String = If(completedSignatures.Count = 0, "<none>", String.Join(vbLf, completedSignatures))
                Dim failedSignatures = GlobalOutcomeTracker.Instance.GetFailedSignatures(10)
                Dim failedSignatureText As String = If(failedSignatures.Count = 0, "<none>", String.Join(vbLf, failedSignatures))

                ' Last failure summary - for AI planner context only, NOT shown to user
                Dim lastFailures = GlobalOutcomeTracker.Instance.GetFailedOutcomes().TakeLast(3).ToList()
                Dim lastFailureSummary As New StringBuilder()
                If lastFailures.Count > 0 Then
                    lastFailureSummary.AppendLine("LAST FAILURES:")
                    For Each f In lastFailures
                        lastFailureSummary.AppendLine($"- {f.ToolName} | Attempt {f.AttemptNumber} | Exit {f.ExitCode} | Err: {If(String.IsNullOrWhiteSpace(f.StandardError), "n/a", f.StandardError)}")
                        If Not String.IsNullOrWhiteSpace(f.RemediationNote) Then
                            lastFailureSummary.AppendLine($"  Note: {f.RemediationNote}")
                        End If
                    Next
                End If

                ' 3?? Compose ENHANCED planner prompt with outcome feedback
                Dim toolsJson As String = ToolPlanner.GetAvailableToolsAsJson()
                Dim plannerSystemContent As String =
                    $"You are an AI Planner for a Windows desktop assistant. Available tools:{vbLf}{toolsJson}" & vbLf &
                    "⚡ EFFICIENCY FIRST - NEVER run the same tool twice!" & vbLf &
                    "- SIMPLE REQUESTS HONOR USER ORDER: when the user says 'first ... then ...' or 'and then', emit steps in THAT order. Never swap actions." & vbLf &
                    "- Example: 'tell me a joke, then tell me the time' => Step1: FreeResponse (joke), Step2: ExecutePowerShellScript (time)." & vbLf &
                    "- Simple requests = ONE step only (e.g., 'what time?' = just Get-Date, done!)" & vbLf &
                    "- Check COMPLETED SIGNATURES before planning - if already done, don't repeat!" & vbLf &
                    "- After PowerShell succeeds with output, return FreeResponse with the result - don't run again!" & vbLf & vbLf &
                    "- DO NOT mention future steps or ask for confirmations. Respond only with the current step's output." & vbLf &
                    "- Avoid phrases like 'next I will', 'tell me when', 'I can also', 'now tell me'." & vbLf &
                    "- If the user lists multiple actions (contains 'and' or 'then'), automatically plan the NEXT unfulfilled action without asking. Keep iterating until all are done." & vbLf &
                    "- For time/date requests, use ExecutePowerShellScript with Get-Date and return the value immediately (no confirmation)." & vbLf &
                    "🚨 ALWAYS prefer Custom Functions over FreeResponse when analyzing files, images, code, or data!" & vbLf &
                    "- Do NOT answer from memory or conversation history - always use the appropriate tool to get fresh data!" & vbLf &
                    "- Use StartOrRunApplicationByName ONLY for executables (do not open files/folders)." & vbLf &
                    "- Review previous outcomes and adjust your strategy if steps failed." & vbLf &
                    "- If validation failed, correct the arguments and retry." & vbLf &
                    "- If execution failed, try an alternative approach." & vbLf &
                    "- IMPORTANT: When using ExecutePowerShellScript, do NOT use 'Add-Type -AssemblyName System.Windows.Forms' or similar UI types unless absolutely necessary. Prefer standard cmdlets." & vbLf &
                    "- NEVER re-run a tool+args combination whose signature already succeeded (see COMPLETED SIGNATURES)." & vbLf &
                    "- AVOID repeating failed signatures unless you change the approach (see FAILED SIGNATURES)." & vbLf &
                    "- If a tool hit retry limits, choose an alternative tool or adjust arguments." & vbLf & vbLf &
                    "⚡ SIMPLE REQUESTS - BE DIRECT:" & vbLf &
                    "- For 'what time is it' → ONE step: Get-Date, then FreeResponse with result" & vbLf &
                    "- For greetings (hello, hi) → ONE FreeResponse step" & vbLf &
                    "- For system info → ONE PowerShell step, then FreeResponse" & vbLf &
                    "- NEVER run the same command twice - check PREVIOUS STEP OUTPUTS!" & vbLf &
                    "- NEVER ask user about PowerShell installation - it's ALWAYS available on Windows!" & vbLf &
                    "- For conversational requests (jokes, explanations) → Use FreeResponse directly" & vbLf & vbLf &
                    "🚨 CRITICAL JSON FORMAT RULES:" & vbLf &
                    "- ALWAYS put parameters inside 'args' object: {""tool"": ""Name"", ""args"": {""param"": ""value""}}" & vbLf &
                    "- NEVER use template placeholders like {{path}} or {{result}} - the system does NOT do substitution!" & vbLf &
                    "- If a step needs output from a previous step, wait for PREVIOUS STEP OUTPUTS and use the ACTUAL value." & vbLf &
                    "- Use single backslashes in paths: ""D:\\Demo\\file.txt"" (JSON escaping), NOT ""D:\\\\Demo\\\\file.txt""" & vbLf &
                    "- Return exactly ONE step per plan. If more actions are needed, return only the next best step and wait for the next iteration after outputs are available." & vbLf &
                    "- When planning multi-step tasks, plan ONE step at a time and wait for results before the next step."

                Dim plannerMessages As New List(Of Dictionary(Of String, String)) From {
                New Dictionary(Of String, String) From {
                    {"role", "system"}, {"content", plannerSystemContent}
                }
            }

                ' ✅ FIX: Inject Conversation History (Context Awareness)
                ' We exclude the last message (current user request) because we format it specially below.
                If conversationHistory.Count > 1 Then
                    Dim historyToInclude = conversationHistory.Take(conversationHistory.Count - 1).ToList()
                    plannerMessages.AddRange(historyToInclude)
                End If

                ' Add the structured current request
                plannerMessages.Add(New Dictionary(Of String, String) From {
                    {"role", "user"}, {"content",
                      $"ORIGINAL REQUEST:{vbLf}{originalQuestion}{vbLf}{vbLf}" &
                      $"COMPLETED STEPS:{vbLf}" &
                      If(completedSteps.Count = 0, "<none>", String.Join(vbLf, completedSteps)) & vbLf & vbLf &
                      $"COMPLETED SIGNATURES (tool|argshash):{vbLf}{completedSignatureText}{vbLf}{vbLf}" &
                      $"FAILED SIGNATURES (tool|argshash):{vbLf}{failedSignatureText}{vbLf}{vbLf}" &
                      If(lastFailures.Count > 0, lastFailureSummary.ToString() & vbLf, String.Empty) &
                      $"RETRY STATS:{vbLf}{retryStats}{vbLf}{vbLf}" &
                      $"REMAINING RETRIES:{vbLf}{retryRemaining}{vbLf}{vbLf}" &
                      $"PREVIOUS STEP OUTPUTS:{vbLf}{previousOutputsSummary}{vbLf}{vbLf}" &
                      If(String.IsNullOrWhiteSpace(recentOutcomeDigest), String.Empty, $"{recentOutcomeDigest}{vbLf}{vbLf}") &
                      If(String.IsNullOrWhiteSpace(retryGuidance), String.Empty, $"{retryGuidance}{vbLf}{vbLf}") &
                      "INSTRUCTIONS:" & vbLf &
                      "1. Review outcomes above - check for failures/errors" & vbLf &
                      "2. If steps failed: Generate corrected steps to retry with different approach" & vbLf &
                      "3. If goal achieved: Return FreeResponse with summary" & vbLf &
                      "4. If need more steps: Continue with next actions" & vbLf &
                      $"5. Max {RetryStrategy.MaxRetriesPerTool} retries per tool - use alternative tools if one keeps failing" & vbLf &
                      "6. 📋 LIST HANDLING:" & vbLf &
                      "   - For IMAGE ANALYSIS requests: Process EACH image separately (one ImageAnswer per image)" & vbLf &
                      "   - For FILE SEARCH followed by SINGLE file action: Use ONLY THE FIRST matching file path" & vbLf &
                      "   - If user says 'a file' (singular), read ONLY ONE file even if search returns multiple" & vbLf &
                      "   - If user says 'files' (plural) or 'all', then process multiple" & vbLf & vbLf &
                      "7. 📝 SIMPLICITY:" & vbLf &
                      "   - When user asks for a 'summary', provide ONLY a brief summary (3-5 sentences)" & vbLf &
                      "   - Do NOT over-expand simple requests into complex multi-part analyses" & vbLf &
                      "   - Match the complexity of your response to the user's request" & vbLf & vbLf &
                      "8. If a script is blocked by policy (see outcomes), ask the user for approval or use a safer alternative." & vbLf & vbLf &
                      " If you can answer entirely in natural language, return a single " &
                      "step using tool=""FreeResponse"" with args.text set to the answer." & vbLf &
                      " If the answer REQUIRES **running or evaluating PowerShell**, " &
                      "use tool=""ExecutePowerShellScript"" and place the script inside " &
                      "`args.script` wrapped in a fenced block." & vbLf &
                      " Otherwise list ALL remaining tool steps in order." & vbLf &
                      "Return only the JSON array (no markdown fences, no commentary)."}})

                ' 4?? Ask the Planner (V2: Use CallGPTCore with JSON Mode)
                Dim rawPlanReply As String = Await AIcall.CallGPTCore(
                    Globals.UserApiKey,
                    Globals.AiModelSelection,
                    plannerMessages,
                    Globals.temperature,
                    ct,
                    jsonMode:=True)

                LogDebugInformation("N/A", conversationHistory,
                                $"[Planner Raw Reply - Iteration {iteration}]{Environment.NewLine}{rawPlanReply}",
                                0, CalculateTokenCount(rawPlanReply))

                ' 5?? Extract JSON payload (strip fences only)
                Dim jsonTxt As String = HelperFunctions.StripCodeFences(rawPlanReply)

                ' 6?? Deserialize into PlanStep list
                Dim plan As List(Of PlanStep) = Nothing
                Dim parseFailedCorrection As String = Nothing
                Try
                    plan = JsonConvert.DeserializeObject(Of List(Of PlanStep))(jsonTxt)
                Catch ex As Exception
                    Debug.WriteLine($"[HandleUserRequest] Failed to parse plan: {ex.Message}")
                    StepOutputManager.Instance.RecordStepOutput(0, "Planner", $"[PARSING FAILED] {ex.Message}", False)

                    ' Add correction hint to planner context for next iteration
                    parseFailedCorrection = "Planner output was not valid JSON. Respond ONLY with a JSON array of steps, no code fences, no text outside JSON."
                    conversationHistory.Add(ConversationHistoryFunctions.CreateHistoryMessage("system", parseFailedCorrection))
                End Try

                If parseFailedCorrection IsNot Nothing Then
                    Await convHistory.TrimConversationHistoryByTokens_Dict(conversationHistory, Globals.MaxTotalTokens, parseFailedCorrection)
                    Continue Do
                End If

                lastRunMultiTask = (plan IsNot Nothing AndAlso plan.Count > 1)

                If plan Is Nothing OrElse plan.Count = 0 Then
                    Debug.WriteLine("[HandleUserRequest] Empty plan returned")
                    StepOutputManager.Instance.RecordStepOutput(0, "Planner", "[PARSING FAILED] Empty plan returned", False)
                    conversationHistory.Add(ConversationHistoryFunctions.CreateHistoryMessage("system", "Planner returned empty plan. Return a JSON array of steps only."))
                    Await convHistory.TrimConversationHistoryByTokens_Dict(conversationHistory, Globals.MaxTotalTokens, "Planner returned empty plan. Return a JSON array of steps only.")
                    Continue Do
                End If

                ' 7?? Check for terminal FreeResponse
                ' NOTE: Do not short-circuit on single FreeResponse plans; execute them to allow multi-action prompts to continue planning.

                ' 8?? Execute plan
                Shelly.LabelStatusUpdate.Text = $"Executing planned tasks (iteration {iteration})�"
                Await ExecutorAgent.ExecutePlanAsync(plan, ct)

                ' 🔧 CRITICAL FIX: Build execution results summary and add to conversation history
                Dim stepOutputs = StepOutputManager.Instance.GetOutputsForIteration(Globals.CurrentRequestId, iteration)

                ' Add concise system summary for planner context (always needed for AI context)
                Dim historySummary As New StringBuilder()
                historySummary.AppendLine("=== EXECUTION RESULTS ===")
                If stepOutputs.Count > 0 Then
                    For Each output In stepOutputs
                        historySummary.AppendLine($"Step {output.StepIndex} - {output.ToolName}: {output.Status}")
                        historySummary.AppendLine(output.Output)
                        historySummary.AppendLine()
                    Next
                Else
                    historySummary.AppendLine("No step outputs recorded")
                End If
                historySummary.AppendLine("=== END RESULTS ===")

                conversationHistory.Add(ConversationHistoryFunctions.CreateHistoryMessage("system", historySummary.ToString()))

                ' Show user-facing digest
                ' Clean output: previously showed only SUCCESSFUL results to user; now outputs are shown immediately per step.
                ' Keep brief status updates only.
                Dim successCount = stepOutputs.Where(Function(o) o.Status = OutcomeStatus.Success).Count()
                If successCount > 0 Then
                    Shelly.LabelStatusUpdate.Text = $"Completed {successCount} step(s)..."
                End If

                Debug.WriteLine($"[HandleUserRequest] Added execution results to history.")

                ' Track custom functions
                customFunctionUsed = plan.Any(Function(p) p.Tool.Equals("ReadFileAndAnswer", StringComparison.OrdinalIgnoreCase) OrElse
                                           p.Tool.Equals("GenerateImages", StringComparison.OrdinalIgnoreCase) OrElse
                                           p.Tool.Equals("UpdateFileByChunks", StringComparison.OrdinalIgnoreCase) OrElse
                                           p.Tool.Equals("ImageAnswer", StringComparison.OrdinalIgnoreCase))

                completedSteps.AddRange(plan.Select(Function(p) p.Tool & "|" & JsonConvert.SerializeObject(p.Args)))

                ' Check if ALL steps succeeded (simple success check)
                Dim allStepsSucceeded = stepOutputs.Count > 0 AndAlso
                                       stepOutputs.All(Function(o) o.Status = OutcomeStatus.Success)

                ' Detect if this is a multi-action request (user asked for multiple things)
                Dim hasMultipleActions = originalQuestion.ToLowerInvariant().Contains(" and ") OrElse
                                        originalQuestion.ToLowerInvariant().Contains(" then ") OrElse
                                        originalQuestion.ToLowerInvariant().Contains(", then") OrElse
                                        originalQuestion.Contains(vbLf)

                ' SIMPLE TASK EXIT: For truly simple single-step requests that succeeded
                ' Only exit early if this is NOT a multi-action request
                Dim isSimpleSingleStep = (plan.Count = 1 AndAlso allStepsSucceeded AndAlso Not hasMultipleActions)
                Dim isSimpleRequest = Not (originalQuestion.ToLowerInvariant().Contains("if found") OrElse
                                          originalQuestion.ToLowerInvariant().Contains("search") OrElse
                                          originalQuestion.ToLowerInvariant().Contains("find"))

                Dim needsMoreSteps = plan.Any(Function(p) p.Tool.StartsWith("Search", StringComparison.OrdinalIgnoreCase) OrElse
                                              (p.Tool.Equals("ExecutePowerShellScript", StringComparison.OrdinalIgnoreCase) AndAlso
                                              (originalQuestion.ToLowerInvariant().Contains("summary") OrElse
                                               originalQuestion.ToLowerInvariant().Contains("read") OrElse
                                               originalQuestion.ToLowerInvariant().Contains("analyze"))))

                ' Exit immediately ONLY for truly simple single-step tasks (no "and" / "then")
                If isSimpleSingleStep AndAlso isSimpleRequest AndAlso Not needsMoreSteps Then
                    taskDone = True
                    Debug.WriteLine("[HandleUserRequest] Simple single-step task completed - exiting immediately")
                    Shelly.LabelStatusUpdate.Text = "Done!"
                    Continue Do
                End If

                ' For multi-action requests: check if we've completed enough iterations
                ' Keep iterating until the planner signals completion OR we've done enough iterations
                Dim justSearchedOrRead = plan.Any(Function(p) p.Tool.StartsWith("Read", StringComparison.OrdinalIgnoreCase) OrElse
                                                  p.Tool.StartsWith("Search", StringComparison.OrdinalIgnoreCase) OrElse
                                                  (p.Tool.Equals("ExecutePowerShellScript", StringComparison.OrdinalIgnoreCase) AndAlso
                                                   originalQuestion.ToLowerInvariant().Contains("search")))
                Dim userWantsMore = originalQuestion.ToLowerInvariant().Contains("summary") OrElse
                                   originalQuestion.ToLowerInvariant().Contains("analyze") OrElse
                                   originalQuestion.ToLowerInvariant().Contains("read") OrElse
                                   originalQuestion.ToLowerInvariant().Contains("generate") OrElse
                                   originalQuestion.ToLowerInvariant().Contains("create")

                Dim prematureExit = justSearchedOrRead AndAlso userWantsMore

                ' For multi-action requests: require at least 2 iterations before allowing completion
                ' This ensures we don't exit after just the first "and" action
                If allStepsSucceeded AndAlso Not prematureExit Then
                    If hasMultipleActions Then
                        ' Multi-action: only mark done if we've done multiple iterations
                        If iteration >= 2 Then
                            taskDone = True
                            lastRunMultiTask = True
                            Debug.WriteLine($"[HandleUserRequest] Multi-action request completed after {iteration} iterations")
                            Shelly.LabelStatusUpdate.Text = "All tasks completed successfully!"
                        Else
                            Debug.WriteLine($"[HandleUserRequest] Multi-action request: iteration {iteration}, continuing...")
                        End If
                    Else
                        ' Single action: can complete after one iteration
                        taskDone = True
                        Debug.WriteLine("[HandleUserRequest] All steps succeeded - marking as complete")
                        Shelly.LabelStatusUpdate.Text = "All tasks completed successfully!"
                        If plan.Count > 1 OrElse iteration > 1 Then
                            lastRunMultiTask = True
                        End If
                    End If
                    Continue Do
                End If

                ' GOAL VALIDATION
                If GoalValidator.IsGoalAchieved() Then
                    taskDone = True
                    Debug.WriteLine("[HandleUserRequest] Goal achieved - exiting loop")
                    Shelly.LabelStatusUpdate.Text = "Goal achieved successfully!"

                    If plan.Count > 1 OrElse completedSteps.Count > 1 Then
                        lastRunMultiTask = True
                    End If

                    Continue Do
                End If

            Loop

            ' ========================================
            ' FINAL WRAP-UP: LocalAI + Piper Voice Summary
            ' Called ONCE after ALL steps are complete
            ' Voice-only - does NOT add text to chat!
            ' IMPORTANT: Runs in background (fire-and-forget) so UI is not blocked!
            ' ========================================

            ' Use Trace.WriteLine which works in both Debug AND Release builds
            System.Diagnostics.Trace.WriteLine("[HandleUserRequest] === FINAL WRAP-UP ===")
            System.Diagnostics.Trace.WriteLine($"[HandleUserRequest] taskDone={taskDone}, ct.IsCancellationRequested={ct.IsCancellationRequested}")

            If taskDone AndAlso Not ct.IsCancellationRequested Then
                ' Only attempt LocalAI summary if engine is ready and TTS enabled
                Globals.LoadLocalAISettings()

                System.Diagnostics.Trace.WriteLine($"[HandleUserRequest] LocalAI Check:")
                System.Diagnostics.Trace.WriteLine($"  - IsSharedLocalAIReady: {Globals.IsSharedLocalAIReady()}")
                System.Diagnostics.Trace.WriteLine($"  - IsSharedTTSReady: {Globals.IsSharedTTSReady()}")
                System.Diagnostics.Trace.WriteLine($"  - LocalAIIncludeTTS: {Globals.LocalAIIncludeTTS}")
                System.Diagnostics.Trace.WriteLine($"  - LocalAISelectedModel: {Globals.LocalAISelectedModel}")

                If Globals.IsSharedLocalAIReady() AndAlso Globals.LocalAIIncludeTTS Then
                    System.Diagnostics.Trace.WriteLine("[HandleUserRequest] LocalAI ready and TTS enabled - starting voice summary in BACKGROUND")

                    ' Build step descriptions BEFORE firing background task
                    Dim stepDescriptions = BuildStepDescriptionsForVoiceSummary()
                    System.Diagnostics.Trace.WriteLine($"[HandleUserRequest] Step descriptions count: {stepDescriptions.Count}")

                    ' Fallback if summaries are empty
                    If stepDescriptions.Count = 0 Then
                        Dim allOutputs = StepOutputManager.Instance.GetAllOutputs()
                        System.Diagnostics.Trace.WriteLine($"[HandleUserRequest] Fallback: GetAllOutputs count: {allOutputs.Count}")
                        For Each output In allOutputs
                            If output.Status = OutcomeStatus.Success Then
                                stepDescriptions.Add($"{output.ToolName}: completed successfully")
                            End If
                        Next
                    End If

                    If stepDescriptions.Count > 0 Then
                        ' Capture variables for the background task
                        Dim capturedDescriptions = stepDescriptions.ToList()
                        Dim capturedRequest = originalQuestion

                        ' FIRE-AND-FORGET: Run LocalAI voice summary in background
                        ' This allows the UI to show results immediately while TTS prepares
                        Task.Run(Async Function()
                                     Try
                                         System.Diagnostics.Trace.WriteLine("[HandleUserRequest-Background] Starting LocalAI voice summary...")
                                         Await SpeakLocalAISummaryAsync(capturedDescriptions, capturedRequest, CancellationToken.None)
                                         System.Diagnostics.Trace.WriteLine("[HandleUserRequest-Background] Voice summary completed")
                                     Catch ex As Exception
                                         System.Diagnostics.Trace.WriteLine($"[HandleUserRequest-Background] Voice summary error: {ex.Message}")
                                     End Try
                                 End Function)

                        System.Diagnostics.Trace.WriteLine("[HandleUserRequest] Voice summary task started in background - UI continues immediately")
                    Else
                        System.Diagnostics.Trace.WriteLine("[HandleUserRequest] No step descriptions to summarize")
                    End If
                Else
                    If Not Globals.IsSharedLocalAIReady() Then
                        System.Diagnostics.Trace.WriteLine("[HandleUserRequest] LocalAI not ready - skipping voice summary")
                        System.Diagnostics.Trace.WriteLine("[HandleUserRequest] >>> HINT: Open LocalAI Form and load a model first!")
                    End If
                    If Not Globals.LocalAIIncludeTTS Then
                        System.Diagnostics.Trace.WriteLine("[HandleUserRequest] TTS disabled in settings - skipping voice summary")
                        System.Diagnostics.Trace.WriteLine("[HandleUserRequest] >>> HINT: Check 'Include TTS' checkbox in LocalAI Form!")
                    End If
                End If
            Else
                System.Diagnostics.Trace.WriteLine($"[HandleUserRequest] Skipping voice summary: taskDone={taskDone}, cancelled={ct.IsCancellationRequested}")
            End If

            ' ========================================
            ' FINAL CLEANUP
            ' ========================================
            If taskDone Then
                If GoalValidator.IsGoalAchieved() Then
                    Shelly.LabelStatusUpdate.Text = "✅ All tasks completed successfully!"
                    Shelly.AIcommentBox.Text = "All requested tasks have been completed successfully."
                Else
                    Shelly.LabelStatusUpdate.Text = "⚠️ Tasks completed with some limitations."
                    Shelly.AIcommentBox.Text = "Completed what was possible within retry limits."
                End If
            Else
                Shelly.LabelStatusUpdate.Text = "Task execution stopped."
            End If

            Await Shelly.WebView21.CoreWebView2.ExecuteScriptAsync("setColorDefault();")

            ' Log final statistics (lightweight, no delays)
            Debug.WriteLine($"[HandleUserRequest] === Session Complete ===")
            Debug.WriteLine($"Total iterations: {iteration}")
            Debug.WriteLine($"Completed steps: {completedSteps.Count}")

        Catch ex As OperationCanceledException
            Shelly.LabelStatusUpdate.Text = "Task canceled."
            Debug.WriteLine("[HandleUserRequest] Task canceled by user")
        Catch ex As Exception
            Debug.WriteLine($"[HandleUserRequest] Error: {ex.Message}")
            Shelly.LabelStatusUpdate.Text = "Oh no, we got an error."
        Finally
            Shelly.CancelTaskButton.Enabled = False
            Shelly.SetUIState(True)
        End Try
    End Function

    ' ===========================================
    ' Hybrid HandleSingleSegmentAsync Function
    ' ===========================================

    Public Async Function HandleSingleSegmentAsync(
  segment As AISegment,
  ct As CancellationToken
) As Task(Of String)
        Try

            ' --------------------------------------------
            ' skip the very next Text segment if it's just a summary
            If Shelly.skipNextPlainTextSegment AndAlso segment.SegmentType = AISegmentType.Text Then
                Shelly.skipNextPlainTextSegment = False
                Debug.WriteLine("[DEBUG] Skipped summary segment.")
                Return "Summary segment skipped."
            End If

            Select Case segment.SegmentType
                Case AISegmentType.Text
                    Dim cleanText = Shelly.RemoveCodeBlocks(segment.Content).Trim()
                    If cleanText <> "" Then
                        Debug.WriteLine("5 ->")
                        AppendResultToBox(cleanText)
                        conversationHistory.Add(ConversationHistoryFunctions.CreateHistoryMessage("assistant", cleanText))
                    End If
                    Return "Text segment displayed."

                Case AISegmentType.TextRequest
                    Dim response = Await CallGPTCore(
                    Globals.UserApiKey,
                    AiModel,
                    New List(Of Dictionary(Of String, String)) From {
                        New Dictionary(Of String, String) From {
                            {"role", "system"},
                            {"content", "You are a helpful assistant answering creative text-only requests."}
                        },
                        New Dictionary(Of String, String) From {
                            {"role", "user"},
                            {"content", segment.Content}
                        }
                    },
                    Globals.temperature,
                    ct
                )
                    If Not String.IsNullOrWhiteSpace(response) Then
                        Debug.WriteLine("6 ->")
                        AppendResultToBox(response)
                        conversationHistory.Add(ConversationHistoryFunctions.CreateHistoryMessage("assistant", response))
                    End If
                    Return "TextRequest processed."

                Case AISegmentType.FunctionCall
                    Dim signature = segment.Content
                    If Shelly.executedCalls.Add(signature) Then
                        Dim result = Await ExecuteAppFunctionAsync(signature, ct)
                        If Not String.IsNullOrWhiteSpace(result) Then
                            Debug.WriteLine("7 ->")
                            AppendResultToBox(result)
                            Shelly.skipNextPlainTextSegment = True
                        End If
                    End If
                    Return "FunctionCall processed."

                Case AISegmentType.PowerShell
                    Dim key = "PS:" & segment.Content.GetHashCode().ToString()
                    If Shelly.executedCalls.Add(key) Then
                        Await ExecutePowerShellWithFixLoopAsync(segment.Content, ct)
                        Shelly.skipNextPlainTextSegment = True
                    End If
                    Return "PowerShell segment executed."

                Case Else
                    Debug.WriteLine("[ERROR] Unknown segment type: " & segment.SegmentType.ToString())
                    Return "Unknown segment type."
            End Select

        Catch ex As OperationCanceledException
            AppendResultToBox("[Canceled by user]")
            Return "Operation canceled."
        Catch ex As Exception
            AppendResultToBox($"[Error] {ex.Message}")
            Return $"Error: {ex.Message}"
        End Try
    End Function


    ' -------------------------------
    ' NEW ENUMERATION UPDATE:
    ' -------------------------------
    Public Enum AISegmentType
        Text
        PowerShell
        FunctionCall
        TextRequest  ' New type for text-based requests wrapped in <gen request> tags.
    End Enum

    Public Class AISegment
        Public Property SegmentType As AISegmentType
        Public Property Content As String
        Public Sub New(type As AISegmentType, content As String)
            Me.SegmentType = type
            Me.Content = content
        End Sub
    End Class


    ' ================================================================
    ' UPDATED FUNCTION: HandleMultiTaskResponseAsync
    ' ================================================================
    Public Async Function HandleMultiTaskResponseAsync(
    aiResponse As String,
    ct As CancellationToken
) As Task

        ' Reset the dedupe set for a fresh run
        Shelly.executedCalls.Clear()

        ' (Optional) Log the raw AI response for debugging
        LogDebugInformation("N/A", conversationHistory,
        "[Multi-Task] Initial AI Response:" & Environment.NewLine & aiResponse,
        0, 0)

        ' Parse actionable segments: PS blocks, function calls, text requests, plain text
        Dim segments As List(Of AISegment) = ParseAISegments(aiResponse)

        ' -- drop any trailing summary segment if it follows a PS or FunctionCall --
        If segments.Count >= 2 Then
            Dim lastSeg = segments(segments.Count - 1)
            Dim prevSeg = segments(segments.Count - 2)
            If lastSeg.SegmentType = AISegmentType.Text AndAlso
           (prevSeg.SegmentType = AISegmentType.PowerShell OrElse prevSeg.SegmentType = AISegmentType.FunctionCall) Then

                Debug.WriteLine("[DEBUG] Removed redundant summary segment.")
                segments.RemoveAt(segments.Count - 1)
            End If
        End If

        If segments.Count = 0 Then
            AppendResultToBox("No segments found to execute.")
            Return
        End If

        ' Execute each segment in order
        For Each seg As AISegment In segments
            If ct.IsCancellationRequested Then Exit For
            Await HandleSingleSegmentAsync(seg, ct)
            Await Task.Delay(50, ct)   ' UI responsiveness
        Next

        Shelly.LabelStatusUpdate.Text = "All tasks completed."
        Debug.WriteLine("[DEBUG] HandleMultiTaskResponseAsync completed all segments.")
    End Function

    ' --------------------------------------------
    ' UPDATED: ParseAISegments (two-pass parsing)
    ' --------------------------------------------
    Private Function ParseAISegments(aiResponse As String) As List(Of AISegment)
        Dim segments As New List(Of AISegment)()
        If String.IsNullOrWhiteSpace(aiResponse) Then Return segments

        ' 1. Find all PowerShell code blocks in order
        Dim psPattern As String = "```powershell\s*([\s\S]*?)\s*```"
        Dim psMatches = Regex.Matches(aiResponse, psPattern, RegexOptions.IgnoreCase)
        Dim lastPos As Integer = 0

        For Each m As Match In psMatches
            ' 1a. Everything before this PS block ? non-PS parser
            If m.Index > lastPos Then
                Dim before = aiResponse.Substring(lastPos, m.Index - lastPos)
                segments.AddRange(ParseNonPS(before))
            End If

            ' 1b. The PS block itself
            Dim psCode = m.Groups(1).Value.Trim()
            segments.Add(New AISegment(AISegmentType.PowerShell, psCode))

            lastPos = m.Index + m.Length
        Next

        ' 1c. Any trailing text after last PS block
        If lastPos < aiResponse.Length Then
            Dim tail = aiResponse.Substring(lastPos)
            segments.AddRange(ParseNonPS(tail))
        End If

        Return segments
    End Function

    ' --------------------------------------------
    ' NEW: Helper to split non-PS text into FunctionCall/Text
    ' --------------------------------------------
    Private Function ParseNonPS(text As String) As IEnumerable(Of AISegment)
        Dim result As New List(Of AISegment)()
        Dim lines = text.Split({vbCrLf, vbLf}, StringSplitOptions.None)
        Dim buffer As New StringBuilder()

        ' List of your custom functions
        Dim customFunctions As String() = {
        "WriteInsideFileOrWindow",
        "CheckMyScreenAndAnswer",
        "StartOrRunApplicationByName",
        "ReadFileAndAnswer",
        "GenerateLargeFileWithTextOrCode",
         "UpdateFileByChunks",
        "GenerateImages",
        "ChangeOrSetVolume",
        "SendMediaKey",
        "WebSearchAndRespondBasedOnPageContent",
        "TakePrintScreenOrScreenShot",
        "ImageAnswer",
        "ReadCopilotConversation",
        "SearchForTextInsideFiles",
        "ReadWebPageAndRespondBasedOnPageContent",
        "GenerateBatchAndPs1File"
    }
        Dim funcRegex = New Regex(
        "\b(" & String.Join("|", customFunctions) & ")\s*\([^)]*\)",
        RegexOptions.IgnoreCase
    )

        For Each rawLine In lines
            Dim line = rawLine.Trim()
            ' Skip empty lines (they�ll flush buffer later)
            If line = "" Then
                Continue For
            End If

            ' If this line invokes one or more custom functions:
            Dim matches = funcRegex.Matches(line)
            If matches.Count > 0 Then
                ' 1) Flush any accumulated text as a Text segment
                If buffer.Length > 0 Then
                    Dim txt = buffer.ToString().Trim()
                    If txt <> "" Then result.Add(New AISegment(AISegmentType.Text, txt))
                    buffer.Clear()
                End If

                ' 2) Emit each function call separately
                For Each m As Match In matches
                    result.Add(New AISegment(AISegmentType.FunctionCall, m.Value.Trim()))
                Next
            Else
                ' Otherwise, accumulate into text buffer
                buffer.AppendLine(rawLine)
            End If
        Next

        ' Flush leftover text
        If buffer.Length > 0 Then
            Dim tail = buffer.ToString().Trim()
            If tail <> "" Then result.Add(New AISegment(AISegmentType.Text, tail))
        End If

        Return result
    End Function

    ''' <summary>
    ''' Speaks a LocalAI-generated summary of what was done (VOICE ONLY - no chat output).
    ''' Takes a list of completed step descriptions and has LocalAI summarize them for TTS.
    ''' OPTIMIZED: Uses short prompts for fast inference (~2 seconds instead of 30+)
    ''' </summary>
    Private Async Function SpeakLocalAISummaryAsync(
        completedStepDescriptions As List(Of String),
        originalRequest As String,
        ct As CancellationToken
    ) As Task

        System.Diagnostics.Trace.WriteLine("[SpeakLocalAISummary] === STARTING ===")

        Try
            ' Check if LocalAIForm has loaded the model
            If Not Globals.IsSharedLocalAIReady() Then
                System.Diagnostics.Trace.WriteLine("[SpeakLocalAISummary] ABORT: No model loaded")
                Return
            End If

            ' Check if TTS engine is ready
            If Not Globals.IsSharedTTSReady() Then
                System.Diagnostics.Trace.WriteLine("[SpeakLocalAISummary] ABORT: TTS engine not ready")
                Return
            End If

            If completedStepDescriptions Is Nothing OrElse completedStepDescriptions.Count = 0 Then
                System.Diagnostics.Trace.WriteLine("[SpeakLocalAISummary] ABORT: No steps to summarize")
                Return
            End If

            System.Diagnostics.Trace.WriteLine($"[SpeakLocalAISummary] Summarizing {completedStepDescriptions.Count} step(s)")

            ' Build SHORT prompt for fast inference
            Dim prompt As String = BuildLocalAISummaryPrompt(completedStepDescriptions, originalRequest)
            System.Diagnostics.Trace.WriteLine($"[SpeakLocalAISummary] Prompt ({prompt.Length} chars): {prompt}")

            ' Use SHORT system prompt (not the 2000-char training file!)
            Dim systemPrompt = GetShortSystemPromptForTTS()
            System.Diagnostics.Trace.WriteLine($"[SpeakLocalAISummary] System prompt ({systemPrompt.Length} chars)")

            ' Generate voice summary - NO ARTIFICIAL LIMIT
            System.Diagnostics.Trace.WriteLine("[SpeakLocalAISummary] Calling LocalAI...")
            Dim sw = Diagnostics.Stopwatch.StartNew()

            Dim voiceText = Await Globals.SharedLocalAIEngine.GenerateResponseAsync(
                prompt:=prompt,
                systemPrompt:=systemPrompt,
                ct:=ct,
                maxTokens:=2048  ' No practical limit - LocalAI will stop at natural end
            )

            sw.Stop()
            System.Diagnostics.Trace.WriteLine($"[SpeakLocalAISummary] LocalAI responded in {sw.ElapsedMilliseconds}ms: {voiceText}")

            If String.IsNullOrWhiteSpace(voiceText) OrElse voiceText.StartsWith("[ERROR]") Then
                System.Diagnostics.Trace.WriteLine("[SpeakLocalAISummary] ABORT: Generation failed")
                Return
            End If

            ' Clean up the response for TTS
            voiceText = CleanTextForTTS(voiceText)
            System.Diagnostics.Trace.WriteLine($"[SpeakLocalAISummary] Clean TTS text: {voiceText}")

            ' Speak using shared TTS engine
            System.Diagnostics.Trace.WriteLine("[SpeakLocalAISummary] Speaking...")
            Await Globals.SharedTTSEngine.SpeakAsync(voiceText, ct)

            System.Diagnostics.Trace.WriteLine("[SpeakLocalAISummary] === COMPLETED ===")

        Catch ex As OperationCanceledException
            System.Diagnostics.Trace.WriteLine("[SpeakLocalAISummary] Cancelled")
        Catch ex As Exception
            System.Diagnostics.Trace.WriteLine($"[SpeakLocalAISummary] ERROR: {ex.Message}")
        End Try
    End Function

    ''' <summary>
    ''' Builds the prompt for LocalAI to generate a TTS-friendly summary.
    ''' OPTIMIZED: Shows ONLY what was requested + success/failed status.
    ''' Does NOT include actual content (joke text, file contents, etc.)
    ''' </summary>
    Private Function BuildLocalAISummaryPrompt(stepDescriptions As List(Of String), originalRequest As String) As String
        ' Format: "User asked: X. Result: Success/Failed"
        ' Local AI should NOT see the actual content, just what was done

        Dim sb As New StringBuilder()

        ' Truncate original request for context
        Dim shortRequest = originalRequest
        If shortRequest.Length > 80 Then
            shortRequest = shortRequest.Substring(0, 80) & "..."
        End If

        If stepDescriptions.Count = 1 Then
            ' Single task
            sb.AppendLine($"User asked: {shortRequest}")
            sb.AppendLine($"Action: {stepDescriptions(0)}")
            sb.AppendLine("Result: Completed successfully")
            sb.AppendLine()
            sb.AppendLine("Give a brief 1-sentence voice confirmation like: 'Done! I [action] for you.'")
        Else
            ' Multiple tasks
            sb.AppendLine($"User asked: {shortRequest}")
            sb.AppendLine($"Actions completed ({stepDescriptions.Count} tasks):")
            For i = 0 To Math.Min(4, stepDescriptions.Count - 1)
                sb.AppendLine($"  {i + 1}. {stepDescriptions(i)} - Success")
            Next
            sb.AppendLine()
            sb.AppendLine("Give a brief 2-sentence voice summary of what you did.")
        End If

        Return sb.ToString()
    End Function

    ''' <summary>
    ''' Builds a list of human-readable step descriptions from StepOutputManager.
    ''' IMPORTANT: Returns ONLY the action type (e.g., "Told a joke"), NOT the actual content!
    ''' </summary>
    Private Function BuildStepDescriptionsForVoiceSummary() As List(Of String)
        Dim descriptions As New List(Of String)()

        Try
            ' Get step outputs directly (not summaries which may contain content)
            Dim allOutputs = StepOutputManager.Instance.GetAllOutputs()

            For Each output In allOutputs
                If output.Status = OutcomeStatus.Success Then
                    ' Get action description based on tool name (NOT the actual output content!)
                    Dim actionDesc = GetActionDescriptionForTool(output.ToolName)
                    If Not String.IsNullOrWhiteSpace(actionDesc) Then
                        descriptions.Add(actionDesc)
                    End If
                End If
            Next

            System.Diagnostics.Trace.WriteLine($"[BuildStepDescriptions] Built {descriptions.Count} action descriptions from {allOutputs.Count} outputs")

        Catch ex As Exception
            System.Diagnostics.Trace.WriteLine($"[BuildStepDescriptions] Error: {ex.Message}")
        End Try

        Return descriptions
    End Function

    ''' <summary>
    ''' Returns a simple action description based on tool name.
    ''' Does NOT include actual output content - just describes what action was performed.
    ''' </summary>
    Private Function GetActionDescriptionForTool(toolName As String) As String
        Select Case toolName.ToLowerInvariant()
            ' Conversational
            Case "freeresponse"
                Return "Provided a response"

            ' System commands
            Case "executepowershellscript"
                Return "Ran a system command"

            ' File operations
            Case "readfileandanswer"
                Return "Read and analyzed a file"
            Case "searchfortextinsidefiles"
                Return "Searched for text in files"
            Case "generatelargefilewithtextorcode", "generatelargefile"
                Return "Generated a document"
            Case "updatefilebychunks"
                Return "Updated a file"
            Case "generatebatchandps1file"
                Return "Created script files"

            ' Image operations
            Case "generateimages"
                Return "Generated images"
            Case "imageanswer"
                Return "Analyzed an image"
            Case "takeprintscreenorscreenshot"
                Return "Took a screenshot"
            Case "checkmyscreenandanswer"
                Return "Analyzed the screen"

            ' Web operations
            Case "websearchandrespondbasedonpagecontent"
                Return "Searched the web"
            Case "readwebpageandrespondbasedonpagecontent"
                Return "Read a web page"

            ' Media/System control
            Case "changeorsetvolume"
                Return "Adjusted the volume"
            Case "sendmediakey"
                Return "Sent a media command"
            Case "startorunapplicationbyname"
                Return "Opened an application"
            Case "writeinsidefileorwindow"
                Return "Typed text"

            Case Else
                Return "Completed a task"
        End Select
    End Function

    ''' <summary>
    ''' Cleans up text for TTS playback (removes special characters, paths, etc.)
    ''' </summary>
    Private Function CleanTextForTTS(text As String) As String
        If String.IsNullOrWhiteSpace(text) Then Return ""

        Dim result = text.Trim()

        ' Remove common problematic characters for TTS
        result = result.Replace("|", " ")
        result = result.Replace("<", " ")
        result = result.Replace(">", " ")
        result = result.Replace("~", " ")
        result = result.Replace("#", " ")
        result = result.Replace("$", " ")
        result = result.Replace("@", " ")
        result = result.Replace("*", " ")
        result = result.Replace("\", " ")
        result = result.Replace("/", " ")

        ' Remove file paths (C:\... or D:\...)
        result = Regex.Replace(result, "[A-Za-z]:\\[^\s]+", "the file")

        ' Remove URLs
        result = Regex.Replace(result, "https?://[^\s]+", "the website")

        ' Clean up multiple spaces
        result = Regex.Replace(result, "\s+", " ")

        Return result.Trim()
    End Function

    ''' <summary>
    ''' Gets a SHORT system prompt optimized for fast TTS summary generation.
    ''' </summary>
    Private Function GetShortSystemPromptForTTS() As String
        ' Concise instructions for voice confirmation
        Return "You are Shelly, a voice assistant. " +
               "Give a brief spoken confirmation of what you just did. " +
               "Say 'I' as if YOU did the work. " +
               "Example: 'Done! I told you a joke.' or 'I found the time and wrote the story for you.' " +
               "Keep it to 1-2 sentences. No file paths or technical terms."
    End Function

End Module

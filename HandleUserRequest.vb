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

                ' Last failure summary
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

                If lastFailures.Count > 0 Then
                    AppendResultToBox(lastFailureSummary.ToString() & Environment.NewLine)
                End If

                ' 3?? Compose ENHANCED planner prompt with outcome feedback
                Dim toolsJson As String = ToolPlanner.GetAvailableToolsAsJson()
                Dim plannerSystemContent As String =
                    $"You are an AI Planner. Available tools:{vbLf}{toolsJson}" & vbLf &
                    "- ?? ALWAYS prefer Custom Functions over FreeResponse when analyzing files, images, code, or data!" & vbLf &
                    "- Do NOT answer from memory or conversation history - always use the appropriate tool to get fresh data!" & vbLf &
                    "- Use StartOrRunApplicationByName ONLY for executables (do NOT open files/folders)." & vbLf &
                    "- Use the OpenPath tool to open any file or folder path with the default application." & vbLf &
                    "- Review previous outcomes and adjust your strategy if steps failed." & vbLf &
                    "- If validation failed, correct the arguments and retry." & vbLf &
                    "- If execution failed, try an alternative approach." & vbLf &
                    "- IMPORTANT: When using ExecutePowerShellScript, do NOT use 'Add-Type -AssemblyName System.Windows.Forms' or similar UI types unless absolutely necessary. Prefer standard cmdlets." & vbLf &
                    "- For system info (time, monitors, etc.), use simple PowerShell commands (e.g., Get-Date, Get-CimInstance Win32_DesktopMonitor)." & vbLf &
                    "- NEVER re-run a tool+args combination whose signature already succeeded (see COMPLETED SIGNATURES)." & vbLf &
                    "- AVOID repeating failed signatures unless you change the approach (see FAILED SIGNATURES)." & vbLf &
                    "- If a tool hit retry limits, choose an alternative tool or adjust arguments."

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
                      "6. ?? CRITICAL: If previous output contains a LIST of items (files, images, etc.), you MUST generate one step for EACH item in that list!" & vbLf &
                      "   Example: If output shows 3 image files, create 3 separate ImageAnswer steps - one per image!" & vbLf &
                      "   DO NOT STOP after processing only the first item from a list!" & vbLf & vbLf &
                      "7. If a script is blocked by policy (see outcomes), ask the user for approval or use a safer alternative." & vbLf & vbLf &
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
                If plan.Count = 1 Then
                    Dim firstTool As String = If(plan(0).Tool, String.Empty)
                    If firstTool.Equals("FreeResponse", StringComparison.OrdinalIgnoreCase) Then
                        Dim ans As String = plan(0).Args("text").ToString()

                        ' ?? Check if this is premature (failures still exist)
                        If GoalValidator.HasRecoverableFailures() AndAlso iteration < MAX_ITER Then
                            Debug.WriteLine("[HandleUserRequest] FreeResponse received but failures exist, continuing")
                        Else
                            If Not customFunctionUsed Then
                                Debug.WriteLine("1 ->" & ans)
                                AppendResultToBox(ans & Environment.NewLine)
                                LogDebugInformation("N/A", conversationHistory, ans, 0, CalculateTokenCount(ans))
                                
                                ' ✅ FIX: Record Assistant Response in History
                                conversationHistory.Add(ConversationHistoryFunctions.CreateHistoryMessage("assistant", ans))
                                Await convHistory.TrimConversationHistoryByTokens_Dict(conversationHistory, Globals.MaxTotalTokens, ans)
                            End If
                            taskDone = True
                            Continue Do
                        End If
                    End If
                End If

                ' 8?? Execute plan
                Shelly.LabelStatusUpdate.Text = $"Executing planned tasks (iteration {iteration})�"
                Await ExecutorAgent.ExecutePlanAsync(plan, ct)

                ' ? CRITICAL FIX: Build execution results summary and add to conversation history
                Dim executionResults As New StringBuilder()
                executionResults.AppendLine("=== EXECUTION RESULTS ===")

                ' Get results from StepOutputManager
                Dim stepOutputs = StepOutputManager.Instance.GetOutputsForIteration(Globals.CurrentRequestId, iteration)

                If stepOutputs.Count > 0 Then
                    For Each output In stepOutputs
                        executionResults.AppendLine($"Step {output.StepIndex} - {output.ToolName}:")
                        If output.Status = OutcomeStatus.Success Then
                            executionResults.AppendLine($"  ✅ SUCCESS: {output.Output}")

                            ' ✅ FIX: Display web search results DIRECTLY (clean output, no prefix)
                            ' AND mark task as complete to prevent duplicate FreeResponse
                            If output.ToolName = "WebSearchAndRespondBasedOnPageContent" OrElse
                               output.ToolName = "ReadWebPageAndRespondBasedOnPageContent" Then
                                If Not String.IsNullOrWhiteSpace(output.Output) AndAlso
                                   Not output.Output.StartsWith("[ERROR]") Then
                                    AppendResultToBox(output.Output & Environment.NewLine)
                                    ' ✅ NEW: Mark as done to prevent AI from repeating with FreeResponse
                                    taskDone = True
                                    Debug.WriteLine("[HandleUserRequest] Web search completed - marking as done to prevent duplicate output")
                                End If
                            ElseIf Not String.IsNullOrWhiteSpace(output.Output) AndAlso
                               Not output.Output.StartsWith("[") AndAlso
                               output.ToolName <> "FreeResponse" Then
                                ' Other functions get tool name prefix
                                AppendResultToBox($"✅ {output.ToolName}: {output.Output}{Environment.NewLine}")
                            End If
                        Else
                            executionResults.AppendLine($"  ❌ FAILED: {output.Output}")

                            ' ❌ FIX #1: Display failures in chat box too
                            AppendResultToBox($"❌ {output.ToolName}: {output.Output}{Environment.NewLine}")
                        End If
                        executionResults.AppendLine()
                    Next
                Else
                    executionResults.AppendLine("No step outputs recorded")
                End If

                executionResults.AppendLine("=== END RESULTS ===")

                ' Add results to conversation history so AI can see them
                Dim resultsSummary = executionResults.ToString()
                conversationHistory.Add(ConversationHistoryFunctions.CreateHistoryMessage("system", resultsSummary))

                ' Show lightweight timeline in UI log
                AppendResultToBox(resultsSummary & Environment.NewLine)

                ' Log to debug
                Debug.WriteLine($"[HandleUserRequest] Added execution results to history:{Environment.NewLine}{resultsSummary}")

                ' Track custom functions
                customFunctionUsed = plan.Any(Function(p) p.Tool.Equals("ReadFileAndAnswer", StringComparison.OrdinalIgnoreCase) OrElse
                                           p.Tool.Equals("GenerateImages", StringComparison.OrdinalIgnoreCase) OrElse
                                           p.Tool.Equals("UpdateFileByChunks", StringComparison.OrdinalIgnoreCase) OrElse
                                           p.Tool.Equals("ImageAnswer", StringComparison.OrdinalIgnoreCase))

                completedSteps.AddRange(plan.Select(Function(p) $"{p.Tool}|{JsonConvert.SerializeObject(p.Args)}"))

                ' ? FIX #2 & #3: Check if ALL steps succeeded (simple success check)
                Dim allStepsSucceeded = stepOutputs.Count > 0 AndAlso
                                       stepOutputs.All(Function(o) o.Status = OutcomeStatus.Success)

                ' ? IMPROVED: Don't exit immediately - let AI decide if more steps needed
                ' Only exit if this is a single-step simple task OR if explicitly marked complete
                Dim isSingleStepTask = (plan.Count = 1 AndAlso completedSteps.Count <= 1)
                Dim hasMultipleActions = originalQuestion.ToLowerInvariant().Contains(" and ") OrElse
                                        originalQuestion.ToLowerInvariant().Contains(" then ") OrElse
                                        originalQuestion.ToLowerInvariant().Contains(", ") OrElse
                                        originalQuestion.Contains(". ") OrElse
                                        originalQuestion.Contains(vbLf)

                ' ? IMPROVED EXIT LOGIC:
                ' Don't exit if we just did a "Read" operation and the user asked for "Generate/Create"
                Dim justReadSomething = plan.Any(Function(p) p.Tool.StartsWith("Read", StringComparison.OrdinalIgnoreCase) OrElse p.Tool.StartsWith("Search", StringComparison.OrdinalIgnoreCase))
                Dim userWantsCreation = originalQuestion.ToLowerInvariant().Contains("generate") OrElse
                                       originalQuestion.ToLowerInvariant().Contains("create") OrElse
                                       originalQuestion.ToLowerInvariant().Contains("make") OrElse
                                       originalQuestion.ToLowerInvariant().Contains("save")

                Dim prematureExit = isSingleStepTask AndAlso justReadSomething AndAlso userWantsCreation

                ' Only mark as done if:
                ' 1. Single step succeeded AND no multi-action indicators in request AND not a premature exit scenario
                ' 2. OR all steps succeeded AND we've done multiple iterations
                If allStepsSucceeded AndAlso Not prematureExit AndAlso ((isSingleStepTask AndAlso Not hasMultipleActions) OrElse iteration >= 3) Then
                    ' Check if we should continue based on request complexity
                    If Not hasMultipleActions OrElse iteration >= 3 Then
                        taskDone = True
                        Debug.WriteLine("[HandleUserRequest] ? All steps succeeded - marking as complete")
                        Shelly.LabelStatusUpdate.Text = "All tasks completed successfully!"

                        ' ? FIX #3: Force summary to run by setting lastRunMultiTask
                        If plan.Count > 1 OrElse completedSteps.Count > 1 Then
                            lastRunMultiTask = True
                            Debug.WriteLine("[HandleUserRequest] Multi-step task detected - summary will run")
                        End If

                        Continue Do
                    End If
                End If

                ' 9?? ?? ? GOAL VALIDATION - The game changer!
                If GoalValidator.IsGoalAchieved() Then
                    taskDone = True
                    Debug.WriteLine("[HandleUserRequest] ? Goal achieved - exiting loop")
                    Shelly.LabelStatusUpdate.Text = "Goal achieved successfully!"

                    ' ? FIX #3: Ensure summary runs
                    If plan.Count > 1 OrElse completedSteps.Count > 1 Then
                        lastRunMultiTask = True
                    End If

                    Continue Do
                End If

            Loop

            ' -- Final wrap-up -------------------------------------
            If taskDone Then
                If GoalValidator.IsGoalAchieved() Then
                    Shelly.LabelStatusUpdate.Text = "? All tasks completed successfully!"
                    Shelly.AIcommentBox.Text = "All requested tasks have been completed successfully."
                Else
                    Shelly.LabelStatusUpdate.Text = "?? Tasks completed with some limitations."
                    Shelly.AIcommentBox.Text = "Completed what was possible within retry limits."
                End If
            Else
                Shelly.LabelStatusUpdate.Text = "Task execution stopped."
            End If

            Await Shelly.WebView21.CoreWebView2.ExecuteScriptAsync("setColorDefault();")

            ' ?? Log final statistics
            Debug.WriteLine($"[HandleUserRequest] === Session Complete ===")
            Debug.WriteLine($"Total iterations: {iteration}")
            Debug.WriteLine(RetryStrategy.GetRetryStats())
            Debug.WriteLine(GoalValidator.GetGoalStatus())

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

End Module

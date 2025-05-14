' ###  Shelly.vb - v1.0.1 ### 

' ##########################################################
'  Shelly App - v1.0.1
'  License: Creative Commons Attribution-NonCommercial (CC BY-NC)
'  https://creativecommons.org/licenses/by-nc/4.0/
'  © 2025 Vlad Stefanescu | GreenCoders.net. Attribution required.
' ##########################################################

Imports System.Runtime.InteropServices
Imports System.IO
Imports System.Text.RegularExpressions
Imports System.Threading
Imports System.Text
Imports Newtonsoft.Json
Imports System.Linq
Imports System.Windows.Forms
Imports Newtonsoft.Json.Linq

Partial Public Class Shelly


    <DllImport("user32.dll")>
    Private Shared Function ReleaseCapture() As Boolean
    End Function

    Private Const WM_NCLBUTTONDOWN As Integer = &HA1
    Private Const HTCAPTION As Integer = 2

    Private mouseX As Integer
    Private mouseY As Integer

    Private aiTrainingFile As String = "" 'Globals.TrainingText
    Private totalTokens As Integer = 0
    Public ReadOnly maxHistoryMessages As Integer = 20 ' Increased from 3
    Private AImodel As String = AiModelSelection
    Private cancellationTokenSource As CancellationTokenSource
    Private currentPowerShellProcess As Process
    Private processLock As New Object()
    Public functionRegistry As New FunctionRegistry()
    Private Shared _instance As Shelly

    ' Flags and Constants
    Private isTrainingSent As Boolean = False
    Private isVerificationDone As Boolean = False
    Private Const ScriptLineThreshold As Integer = 5
    Private executedCalls As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)

    ' Initial panel height (collapsed state)
    Private Const collapsedHeight As Integer = 0
    Private Const expandedHeight As Integer = 65 ' Adjust this as needed

    ' Flag to track whether the panel is expanded or collapsed
    Private isPanelExpanded As Boolean = False
    Private initialFormHeight As Integer

    ' Separate variable for system prompt
    Private systemPrompt As Dictionary(Of String, String) = Nothing

    ' Google:
    Public isGoogleTranslateLoaded As Boolean = False ' Track if page is loaded

    Private skipNextPlainTextSegment As Boolean = False

    Public Shared ReadOnly Property Instance As Shelly
        Get
            Return _instance
        End Get
    End Property

    Public Sub New()
        InitializeComponent()
        _instance = Me
    End Sub

    ' ==============================
    '       LOGIC & UTILITIES
    ' ==============================

    ' NUMBER OF TOKENS
    Public Shared Function CalculateTokenCount(text As String) As Integer
        If String.IsNullOrEmpty(text) Then Return 0
        ' Count words instead of just characters (closer to OpenAI’s tokenizer)
        Dim words As String() = text.Split({" "c, vbCrLf, vbLf}, StringSplitOptions.RemoveEmptyEntries)
        Return words.Length + (text.Length \ 6) ' words + approx sentence structure
    End Function

    Public Shared Function RemoveCodeBlocks(inputText As String) As String
        Dim pattern As String = "```[\s\S]*?```"
        Return Regex.Replace(inputText, pattern, "").Trim()
    End Function

    ' TRUNCATE TEXT
    Public Shared Function TruncateTextToMaxTokens(text As String, maxTokens As Integer) As String
        If String.IsNullOrEmpty(text) Then Return text
        Dim words As String() = text.Split({" "c, vbCrLf, vbLf}, StringSplitOptions.RemoveEmptyEntries)
        If words.Length <= maxTokens Then
            Return text
        End If
        Return String.Join(" ", words.Take(maxTokens)) & "..."
    End Function

    ' ============================
    '     MAIN BUTTON ("RUN")
    ' ============================
    Private Async Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        '  ----------CALL COPILOT ----------
        Dim phrases As New List(Of String) From {
            "engage copilot",
            "start copilot",
            "run copilot",
            "open copilot",
            "copilot start"
        }

        Dim textToCheck As String = UserInputBox.Text.ToLower() ' Convert once for efficiency
        Dim found As Boolean = phrases.Any(Function(phrase) textToCheck.Contains(phrase, StringComparison.OrdinalIgnoreCase))

        If found Then
            ' Do something when a match is found
            Copilot.Show()
        Else
            ' DEBUG ADD: Log that RUN button was clicked
            Debug.WriteLine("[DEBUG] RUN button clicked.")


            ' ── START SESSION ──


            cancellationTokenSource = New CancellationTokenSource
            CancelTaskButton.Enabled = True
            Await HandleUserRequestAsync(cancellationTokenSource.Token) ' ==1==

            ' =========================== CONFIRMATION ===========================
            If lastRunMultiTask = True Then
                Debug.WriteLine(" ------- multitask --------------")

                Dim userQuestion As String = "You just finished running a task or multiple tasks based on the User Prompt. 
                Can you do a very short summary of all the tasks that were completed based on Conversation History?" & Environment.NewLine &
                "*** Use the 'FreeResponse' tool for this step. ***" & Environment.NewLine &
                "Do so in a friendly, hilarious, dorky manner without repeating what the User asked, just ***a summary of what was done***." & Environment.NewLine &
                "In case you found any errors or issues, please mention them in the summary and adopt a disappointed tone, 
                telling that you tried your best and explained what happened (if you found the details). " & Environment.NewLine &
                "If there is no relevant data in the Conversation History, just respond that you are ready to help."

                Dim reply As String = Await AIBrainiac.CallGPTBrain(
                    Globals.UserApiKey,
                    userQuestion,
                    Globals.AssistantId,
                    conversationHistory
                )

                ' Parse the JSON response to extract the "text" field
                Try
                    Dim parsedResponse As JArray = JArray.Parse(reply)
                    Dim text As String = parsedResponse(0)("args")("text").ToString()

                    ' Append the extracted text to the result box
                    AppendResultToBox(text & Environment.NewLine & "-----------------------------" & Environment.NewLine)
                Catch ex As Exception
                    Debug.WriteLine($"[ERROR] Failed to parse JSON response: {ex.Message}")
                    AppendResultToBox("Error: Unable to extract response text." & Environment.NewLine)
                End Try
                ' =========================== END CONFIRMATION ==========================

                cancellationTokenSource = Nothing
                CancelTaskButton.Enabled = False
                lastRunMultiTask = False
            Else
                Debug.WriteLine(" ------- NOT multitask --------------")
            End If
        End If
        Me.ActiveControl = Nothing

        Debug.WriteLine($"[DEBUG] A total of {AICallCount} AI calls were made.")
    End Sub

    ' ================================
    '  HandleUserRequestAsync  – v4.1
    '  (drop‑in replacement)
    ' ================================
    Private Async Function HandleUserRequestAsync(ct As CancellationToken) As Task
        Dim originalQuestion As String = UserInputBox.Text.Trim()
        If String.IsNullOrWhiteSpace(originalQuestion) Then
            LabelStatusUpdate.Text = "Please enter a question or command."
            Return
        End If

        Try
            ' ── UI prep ───────────────────────────────────────────
            SetUIState(False)
            CancelTaskButton.Enabled = True
            Await WebView21.CoreWebView2.ExecuteScriptAsync("setColorDefault();")
            Await WebView21.CoreWebView2.ExecuteScriptAsync("setColorGreen();")

            ' ── Optional “revise prompt” pass ─────────────────────
            Dim userQuestion As String = originalQuestion
            If Globals.UsePromptRevision Then
                userQuestion = Await ReviseUserQuestionAsync(originalQuestion, ct)
                If String.IsNullOrWhiteSpace(userQuestion) Then
                    LabelStatusUpdate.Text = "Could not revise the question."
                    Return
                End If
            End If

            ' ── Add user message to history (with trimming) ───────
            Await convHistory.TrimConversationHistoryByTokens_Dict(conversationHistory, Globals.MaxTotalTokens, userQuestion)
            conversationHistory.Add(CreateHistoryMessage("user", userQuestion))

            ' ── Planner ↔ Executor iterative loop ────────────────
            Const MAX_ITER As Integer = 10
            Dim iteration As Integer = 0
            Dim taskDone As Boolean = False
            Dim completedSteps As New List(Of String)()
            Dim customFunctionUsed As Boolean = False ' Track if a custom function was used
            lastRunMultiTask = False

            Do While iteration < MAX_ITER AndAlso Not taskDone AndAlso Not ct.IsCancellationRequested
                iteration += 1
                LabelStatusUpdate.Text = $"Planning iteration #{iteration}…"

                ' 1️⃣ Compose planner prompt
                Dim toolsJson As String = ToolPlanner.GetAvailableToolsAsJson()
                Dim plannerSystemContent As String =
                $"You are an AI Planner. Available tools:{vbLf}{toolsJson}" & vbLf &
                "- Use StartOrRunApplicationByName ONLY for executables (do NOT open files/folders)." & vbLf &
                "- Use the OpenPath tool to open any file or folder path with the default application."

                Dim plannerMessages As New List(Of Dictionary(Of String, String)) From {
                New Dictionary(Of String, String) From {
                    {"role", "system"}, {"content", plannerSystemContent}
                },
                New Dictionary(Of String, String) From {
                    {"role", "user"}, {"content",
                      $"ORIGINAL REQUEST:{vbLf}{originalQuestion}{vbLf}{vbLf}" &
                      $"TOOLS ALREADY FINISHED (with full args):{vbLf}" &
                      If(completedSteps.Count = 0, "<none>", String.Join(vbLf, completedSteps)) & vbLf & vbLf &
                      "• If you can answer entirely in natural language, return a single " &
                      "step using tool=""FreeResponse"" with args.text set to the answer." & vbLf &
                      "• If the answer REQUIRES **running or evaluating PowerShell**, " &
                      "use tool=""ExecutePowerShellScript"" and place the script inside " &
                      "`args.script` wrapped in a fenced block." & vbLf &
                      "• Otherwise list ALL remaining tool steps in order." & vbLf &
                      "Return only the JSON array (no markdown fences, no commentary)."}}
            }

                ' 2️⃣ Ask the Planner
                Dim rawPlanReply As String = Await CallGPTBrain(
                Globals.UserApiKey,
                JsonConvert.SerializeObject(plannerMessages),
                Globals.AssistantId,
                conversationHistory)

                LogDebugInformation("N/A", conversationHistory,
                                $"[Planner Raw Reply]{Environment.NewLine}{rawPlanReply}",
                                0, CalculateTokenCount(rawPlanReply))

                ' 3️⃣ Extract JSON payload if fenced
                Dim match As Match = Regex.Match(rawPlanReply,
                             "```json\s*(?<payload>[\s\S]*?)\s*```",
                             RegexOptions.IgnoreCase)
                Dim jsonTxt As String = If(match.Success,
                                   match.Groups("payload").Value.Trim(),
                                   rawPlanReply.Trim())

                ' 4️⃣ Deserialize into PlanStep list
                Dim plan As List(Of PlanStep) = Nothing
                Try
                    plan = JsonConvert.DeserializeObject(Of List(Of PlanStep))(jsonTxt)
                Catch
                    plan = Nothing
                End Try

                lastRunMultiTask = (plan IsNot Nothing AndAlso plan.Count > 1)

                If plan Is Nothing OrElse plan.Count = 0 Then
                    ' fallback to multi‑task handler
                    Await HandleMultiTaskResponseAsync(rawPlanReply, ct)
                    Exit Do
                End If

                ' 5️⃣ Terminal FreeResponse (null‑safe)
                If plan.Count = 1 Then
                    Dim firstTool As String = If(plan(0).Tool, String.Empty)
                    If firstTool.Equals("FreeResponse", StringComparison.OrdinalIgnoreCase) Then
                        Dim ans As String = plan(0).Args("text").ToString()

                        ' Skip "1 ->" if a custom function was used
                        If Not customFunctionUsed Then
                            Debug.WriteLine("1 ->" & ans)
                            AppendResultToBox(ans & Environment.NewLine)
                            LogDebugInformation("N/A", conversationHistory,
                                     ans, 0, CalculateTokenCount(ans))
                        End If

                        taskDone = True
                        Continue Do
                    End If
                End If

                ' 6️⃣ Execute batch and mark steps complete
                LabelStatusUpdate.Text = "Executing planned tasks…"
                Await ExecutorAgent.ExecutePlanAsync(plan, ct)

                ' Check if a custom function was used
                customFunctionUsed = plan.Any(Function(p) p.Tool.Equals("ReadFileAndAnswer", StringComparison.OrdinalIgnoreCase) OrElse
                                           p.Tool.Equals("GenerateImages", StringComparison.OrdinalIgnoreCase) OrElse
                                           p.Tool.Equals("UpdateFileByChunks", StringComparison.OrdinalIgnoreCase) OrElse
                                           p.Tool.Equals("ImageAnswer", StringComparison.OrdinalIgnoreCase))

                completedSteps.AddRange(plan.Select(Function(p) $"{p.Tool}|{JsonConvert.SerializeObject(p.Args)}"))

                ' ▶️ Break after PowerShell or large-file generation steps
                Dim shouldBreak As Boolean = plan.Any(Function(p) p.Tool.Equals("ExecutePowerShellScript", StringComparison.OrdinalIgnoreCase) OrElse
                                             p.Tool.Equals("GenerateLargeFileWithTextOrCode", StringComparison.OrdinalIgnoreCase) OrElse
                                             p.Tool.Equals("WebSearchAndRespondBasedOnPageContent", StringComparison.OrdinalIgnoreCase) OrElse
                                              p.Tool.Equals("CheckMyScreenAndAnswer", StringComparison.OrdinalIgnoreCase) OrElse
                                             p.Tool.Equals("ReadWebPageAndRespondBasedOnPageContent", StringComparison.OrdinalIgnoreCase) OrElse
                                             p.Tool.Equals("UpdateFileByChunks", StringComparison.OrdinalIgnoreCase))
                If shouldBreak Then
                    taskDone = True
                End If

            Loop

            ' ── Final wrap‑up ─────────────────────────────────────
            LabelStatusUpdate.Text = "All tasks completed."
            AIcommentBox.Text = "All requested tasks have been completed successfully."

            Await WebView21.CoreWebView2.ExecuteScriptAsync("setColorDefault();")

        Catch ex As OperationCanceledException
            LabelStatusUpdate.Text = "Task canceled."
        Catch ex As Exception
            Debug.WriteLine($"Error: {ex.Message}")
            LabelStatusUpdate.Text = "Oh no, we got an error."
        Finally
            CancelTaskButton.Enabled = False
            SetUIState(True)
        End Try
    End Function



    ' ================================================================
    ' UPDATED FUNCTION: ExtractPowerShellScripts
    ' — Returns only unique PowerShell blocks, in order of appearance.
    ' ================================================================
    Private Function ExtractPowerShellScripts(aiResponse As String) As List(Of String)
        Dim scripts As New List(Of String)()
        If String.IsNullOrWhiteSpace(aiResponse) Then Return scripts

        ' Regex to find ```powershell … ``` blocks
        Dim pattern As String = "```powershell\s*([\s\S]*?)\s*```"
        Dim matches As MatchCollection = Regex.Matches(aiResponse, pattern, RegexOptions.IgnoreCase)

        ' Track which blocks we’ve already added
        Dim seen As New HashSet(Of String)()

        For Each match As Match In matches
            If match.Success Then
                Dim scriptBlock As String = match.Groups(1).Value.Trim()
                If Not String.IsNullOrWhiteSpace(scriptBlock) Then
                    ' If we haven't seen it yet, add to output and mark as seen
                    If Not seen.Contains(scriptBlock) Then
                        scripts.Add(scriptBlock)
                        seen.Add(scriptBlock)
                    End If
                End If
            End If
        Next

        Return scripts
    End Function

    Public Async Function ExecutePowerShellScriptAsync(
    script As String,
    ct As CancellationToken
) As Task(Of Tuple(Of Boolean, String))
        LabelStatusUpdate.Text = "Executing PowerShell script..."
        Try
            Dim modified = "$ErrorActionPreference = 'Stop'; " & script
            Dim bytes() = Encoding.Unicode.GetBytes(modified)
            Dim encoded = Convert.ToBase64String(bytes)
            Dim psi As New ProcessStartInfo With {
            .FileName = "powershell.exe",
            .Arguments = "-NoProfile -ExecutionPolicy Bypass -EncodedCommand " & encoded,
            .UseShellExecute = False,
            .RedirectStandardOutput = True,
            .RedirectStandardError = True,
            .CreateNoWindow = True
        }
            Using proc As New Process()
                proc.StartInfo = psi
                proc.Start()
                Dim outTask = proc.StandardOutput.ReadToEndAsync(ct)
                Dim errTask = proc.StandardError.ReadToEndAsync(ct)
                Dim waitTask = Task.Run(Sub() proc.WaitForExit(), ct)
                Await Task.WhenAll(outTask, errTask, waitTask)
                Dim exitCode = proc.ExitCode
                Return If(exitCode = 0,
                Tuple.Create(True, outTask.Result),
                Tuple.Create(False, errTask.Result))
            End Using

        Catch ex As OperationCanceledException
            Return Tuple.Create(False, "Execution canceled by user.")
        Catch ex As Exception
            Return Tuple.Create(False, $"Exception: {ex.Message}")
        End Try
    End Function

    Public Async Function ExecutePowerShellWithFixLoopAsync(
    originalScript As String,
    ct As CancellationToken
) As Task

        Dim scriptToRun = originalScript
        Dim maxAttempts = 5
        Dim attempt = 0
        Dim finalOutput = ""
        Dim wasSuccessful = False

        LogDebugInformation("N/A", conversationHistory,
        "[PS Loop Init] Starting PowerShell execution loop." & Environment.NewLine & originalScript,
        0, 0)

        While attempt < maxAttempts AndAlso Not ct.IsCancellationRequested
            attempt += 1
            LabelStatusUpdate.Text = $"Running PowerShell script… Attempt #{attempt}"
            Dim result = Await ExecutePowerShellScriptAsync(scriptToRun, ct)
            Dim success = result.Item1
            Dim raw = result.Item2
            Dim clean = RemoveCodeBlocks(raw).Trim()

            If success Then
                wasSuccessful = True
                finalOutput = If(clean = "",
                "✅ Task completed. Enjoy it!",
                clean)
                LogDebugInformation("N/A", conversationHistory,
                "[PS Success] " & finalOutput, 0, CalculateTokenCount(finalOutput))
                Exit While
            Else
                LogDebugInformation("N/A", conversationHistory,
                $"[PS Failure #{attempt}] {clean}", 0, CalculateTokenCount(clean))
                If attempt < maxAttempts Then
                    ' Ask GPT for a fix
                    Dim fixPrompt =
                    $"I tried running this PowerShell script but got an error:{Environment.NewLine}{clean}{Environment.NewLine}" &
                    $"Script:{Environment.NewLine}```powershell{Environment.NewLine}{scriptToRun}{Environment.NewLine}```" &
                    Environment.NewLine & "Please return only a corrected script in a ```powershell block."
                    Dim fixResp = Await AIcall.CallGPTCore(
                    Globals.UserApiKey,
                    AImodel,
                    New List(Of Dictionary(Of String, String)) From {
                        New Dictionary(Of String, String) From {
                            {"role", "system"},
                            {"content", "You are a PowerShell troubleshooting assistant."}
                        },
                        New Dictionary(Of String, String) From {
                            {"role", "user"},
                            {"content", fixPrompt}
                        }
                    },
                    Globals.temperature,
                    ct
                )
                    LogDebugInformation("N/A", conversationHistory,
                    "[PS Repair GPT] " & fixResp, 0, CalculateTokenCount(fixResp))
                    Dim fixes = ExtractPowerShellScripts(fixResp)
                    If fixes.Count > 0 Then
                        scriptToRun = fixes(0)
                    Else
                        Exit While
                    End If
                End If
            End If
        End While

        ' Show only a final status
        If wasSuccessful Then
            Debug.WriteLine("2 ->" & finalOutput)
            AppendResultToBox(finalOutput)
            conversationHistory.Add(CreateHistoryMessage("assistant", finalOutput))
        Else
            Dim msg = "❌ All attempts to run/fix PowerShell script failed."
            Debug.WriteLine("3 ->")
            AppendResultToBox(msg)
        End If

        ' Keep history trimmed
        Await convHistory.TrimConversationHistoryByTokens_Dict(
        Globals.conversationHistory,
        Globals.MaxTotalTokens,
        If(wasSuccessful, finalOutput, "PowerShell failure."))
    End Function


    ' ================================================================
    ' UPDATED FUNCTION: HandleMultiTaskResponseAsync
    ' ================================================================
    Public Async Function HandleMultiTaskResponseAsync(
    aiResponse As String,
    ct As CancellationToken
) As Task

        ' Reset the dedupe set for a fresh run
        executedCalls.Clear()

        ' (Optional) Log the raw AI response for debugging
        LogDebugInformation("N/A", conversationHistory,
        "[Multi-Task] Initial AI Response:" & Environment.NewLine & aiResponse,
        0, 0)

        ' Parse actionable segments: PS blocks, function calls, text requests, plain text
        Dim segments As List(Of AISegment) = ParseAISegments(aiResponse)

        ' ── drop any trailing summary segment if it follows a PS or FunctionCall ──
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
            MessageBox.Show("here")
        Next

        LabelStatusUpdate.Text = "All tasks completed."
        Debug.WriteLine("[DEBUG] HandleMultiTaskResponseAsync completed all segments.")
    End Function

    ' ────────────────────────────────────────────
    ' UPDATED: ParseAISegments (two‑pass parsing)
    ' ────────────────────────────────────────────
    Private Function ParseAISegments(aiResponse As String) As List(Of AISegment)
        Dim segments As New List(Of AISegment)()
        If String.IsNullOrWhiteSpace(aiResponse) Then Return segments

        ' 1. Find all PowerShell code blocks in order
        Dim psPattern As String = "```powershell\s*([\s\S]*?)\s*```"
        Dim psMatches = Regex.Matches(aiResponse, psPattern, RegexOptions.IgnoreCase)
        Dim lastPos As Integer = 0

        For Each m As Match In psMatches
            ' 1a. Everything before this PS block → non‑PS parser
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

    ' ────────────────────────────────────────────
    ' NEW: Helper to split non‑PS text into FunctionCall/Text
    ' ────────────────────────────────────────────
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
            ' Skip empty lines (they’ll flush buffer later)
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

    ' ===========================================
    ' Hybrid HandleSingleSegmentAsync Function
    ' ===========================================

    Private Async Function HandleSingleSegmentAsync(
    segment As AISegment,
    ct As CancellationToken
) As Task(Of String)
        Try

            ' ────────────────────────────────────────────
            ' skip the very next Text segment if it's just a summary
            If skipNextPlainTextSegment AndAlso segment.SegmentType = AISegmentType.Text Then
                skipNextPlainTextSegment = False
                Debug.WriteLine("[DEBUG] Skipped summary segment.")
                Return "Summary segment skipped."
            End If

            Select Case segment.SegmentType
                Case AISegmentType.Text
                    Dim cleanText = RemoveCodeBlocks(segment.Content).Trim()
                    If cleanText <> "" Then
                        Debug.WriteLine("5 ->")
                        AppendResultToBox(cleanText)
                        conversationHistory.Add(CreateHistoryMessage("assistant", cleanText))
                    End If
                    Return "Text segment displayed."

                Case AISegmentType.TextRequest
                    Dim response = Await CallGPTCore(
                    Globals.UserApiKey,
                    AImodel,
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
                        conversationHistory.Add(CreateHistoryMessage("assistant", response))
                    End If
                    Return "TextRequest processed."

                Case AISegmentType.FunctionCall
                    Dim signature = segment.Content
                    If executedCalls.Add(signature) Then
                        Dim result = Await ExecuteAppFunctionAsync(signature, ct)
                        If Not String.IsNullOrWhiteSpace(result) Then
                            Debug.WriteLine("7 ->")
                            AppendResultToBox(result)
                            skipNextPlainTextSegment = True
                        End If
                    End If
                    Return "FunctionCall processed."

                Case AISegmentType.PowerShell
                    Dim key = "PS:" & segment.Content.GetHashCode().ToString()
                    If executedCalls.Add(key) Then
                        Await ExecutePowerShellWithFixLoopAsync(segment.Content, ct)
                        skipNextPlainTextSegment = True
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

    ' ============================
    '      MISC. EVENT HANDLERS
    ' ============================
    Private Sub PSFunctResultsBox_TextChanged(sender As Object, e As EventArgs) Handles PSFunctResultsBox.TextChanged
        PSFunctResultsBox.SelectionStart = PSFunctResultsBox.Text.Length
        PSFunctResultsBox.ScrollToCaret()

        If ShellyAiResponseZoom IsNot Nothing AndAlso Not ShellyAiResponseZoom.IsDisposed Then
            ShellyAiResponseZoom.RichTextBox1.Text = PSFunctResultsBox.Text
        End If
    End Sub

    Private Sub PSFunctResultsBox_DoubleClick(sender As Object, e As EventArgs) Handles PSFunctResultsBox.DoubleClick
        ShellyAiResponseZoom.RichTextBox1.Text = PSFunctResultsBox.Text
        RestoreWindow(ShellyAiResponseZoom)
    End Sub


    ' ============================
    '      ZOOM AND COME TO VIEW
    '      ShellyAiResponseZoom
    ' ============================

    ' ================================================================
    ' UPDATED FUNCTION: CancelTaskButton_Click (Enhanced Cancellation Handling)
    ' ================================================================
    Private Sub CancelTaskButton_Click(sender As Object, e As EventArgs) Handles CancelTaskButton.Click
        Try
            If cancellationTokenSource IsNot Nothing Then
                cancellationTokenSource.Cancel()
                Debug.WriteLine("[DEBUG] Cancellation requested.")
            End If

            SyncLock processLock
                If currentPowerShellProcess IsNot Nothing AndAlso Not currentPowerShellProcess.HasExited Then
                    Try
                        currentPowerShellProcess.Kill()
                        currentPowerShellProcess.WaitForExit()
                        Debug.WriteLine("[DEBUG] Current PowerShell process terminated.")
                    Catch ex As Exception
                        LabelStatusUpdate.Text = "Error terminating PowerShell process."
                        Debug.WriteLine("[DEBUG] Error terminating PowerShell process: " & ex.ToString())
                    Finally
                        currentPowerShellProcess = Nothing
                    End Try
                Else
                    currentPowerShellProcess = Nothing
                End If
            End SyncLock

            LabelStatusUpdate.Text = "Cancelling operations..."
        Catch ex As Exception
            Debug.WriteLine("[DEBUG] Exception in CancelTaskButton_Click: " & ex.ToString())
        Finally
            SetUIState(True)
            cancellationTokenSource?.Dispose()
            cancellationTokenSource = Nothing
            Me.ActiveControl = Nothing
        End Try
    End Sub

    Private Async Sub ButtonUseSpeech_Click(sender As Object, e As EventArgs) Handles ButtonUseSpeech.Click
        Try

            If Globals.UserAudioSelection < 0 Then
                LabelStatusUpdate.Text = "Please select a recording device."
                Return
            End If

            ' 🔴 STT Starts
            isSpeechActive = True
            ButtonUseSpeech.BackgroundImage = My.Resources.mic3
            ButtonUseSpeech.Enabled = False
            Await WebView21.CoreWebView2.ExecuteScriptAsync("setColorGreen();")

            cancellationTokenSource = New CancellationTokenSource()
            Dim selectedDevice As Integer = Globals.UserAudioSelection
            Dim transcription As String = Await StartSpeechToText(selectedDevice)

            ' ✅ Only submit real input
            If Not String.IsNullOrWhiteSpace(transcription) AndAlso Not transcription.StartsWith("Error:") Then
                UserInputBox.Text = transcription
                Await HandleUserRequestAsync(cancellationTokenSource.Token)
            Else
                LabelStatusUpdate.Text = "No voice detected."
            End If

            Await WebView21.CoreWebView2.ExecuteScriptAsync("setColorDefault();")

        Catch ex As Exception
            LabelStatusUpdate.Text = "Oh no, we got an error"
            Debug.WriteLine($"Error: {ex.Message}")
        Finally
            ' 🔵 STT Ends
            isSpeechActive = False
            ButtonUseSpeech.BackgroundImage = My.Resources.mic
            ButtonUseSpeech.Enabled = True
            CancelTaskButton.Enabled = False
            cancellationTokenSource = Nothing

            Me.ActiveControl = Nothing
        End Try
    End Sub

    ' ================================================================
    ' UPDATED FUNCTION: Button4_Click (Enhanced Clear & Reset)
    ' ================================================================
    Private Async Sub Button4_Click(sender As Object, e As EventArgs) Handles Button4.Click
        Try
            Await ExecuteScriptSafeAsync("setColorDefault();")
            CleanupResources()
            ClearDebugLogs()
            ActiveControl = Nothing
        Catch ex As Exception
            Debug.WriteLine("Error in Button4_Click: " & ex.Message)
        End Try
    End Sub

    Private Sub ShowSettings_Click(sender As Object, e As EventArgs) Handles ShowSettings.Click
        RestoreWindow(Settings)
        ActiveControl = Nothing
    End Sub

    ' ENABLE / DISABLE UI
    Private Sub SetUIState(isEnabled As Boolean)
        ButtonUseSpeech.Enabled = isEnabled
        UserInputBox.ReadOnly = Not isEnabled
        Cursor = If(isEnabled, Cursors.Default, Cursors.WaitCursor)
    End Sub

    ' Debug Logging Function
    Public Shared Sub LogDebugInformation(userQuestion As String,
                                       conversationHistory As List(Of Dictionary(Of String, String)),
                                       aiResponse As String,
                                       tokensSent As Integer,
                                       tokensReceived As Integer)
        ' Extract user question if missing.
        Dim actualUserPrompt As String = userQuestion
        If String.IsNullOrWhiteSpace(actualUserPrompt) Then
            For i As Integer = conversationHistory.Count - 1 To 0 Step -1
                If String.Equals(conversationHistory(i)("role"), "user", StringComparison.OrdinalIgnoreCase) Then
                    actualUserPrompt = conversationHistory(i)("content")
                    Exit For
                End If
            Next
        End If

        ' Build debug message with added timestamp.
        Dim logBuilder As New System.Text.StringBuilder()
        logBuilder.AppendLine("======================================")
        logBuilder.AppendLine("          DEBUG INFORMATION           ")
        logBuilder.AppendLine("======================================")
        logBuilder.AppendLine($"Timestamp: {DateTime.Now}")
        logBuilder.AppendLine($"User Prompt: {actualUserPrompt}")
        logBuilder.AppendLine("")
        logBuilder.AppendLine("----- Conversation History -----")
        For Each msg In conversationHistory
            logBuilder.AppendLine($"{msg("role").ToUpper()}: {msg("content")}")
        Next
        logBuilder.AppendLine("--------------------------------")
        logBuilder.AppendLine("")
        logBuilder.AppendLine("----- AI Response -----")
        logBuilder.AppendLine($"AI: {aiResponse}")
        logBuilder.AppendLine("--------------------------------")
        logBuilder.AppendLine("")
        logBuilder.AppendLine("----- Token Counts -----")
        logBuilder.AppendLine($"Tokens Sent: {tokensSent}")
        logBuilder.AppendLine($"Tokens Received: {tokensReceived}")
        logBuilder.AppendLine("--------------------------------")
        logBuilder.AppendLine("=== END OF DEBUG INFORMATION ===")

        ' Save log message and update UI.
        Globals.AppendDebugLog(logBuilder.ToString())
    End Sub


    ' Show Right Click Menu
    Private Sub PSFunctResultsBox_MouseUp(sender As Object, e As MouseEventArgs) Handles PSFunctResultsBox.MouseUp
        ' Check if it's a right-click (context menu trigger)
        If e.Button = MouseButtons.Right Then
            ' Show the context menu at the mouse position
            PSFunctResultsContextMenu.Show(Cursor.Position)
        End If
    End Sub

    Private Sub UserInputBox_MouseUp(sender As Object, e As MouseEventArgs) Handles UserInputBox.MouseUp
        ' Check if it's a right-click (context menu trigger)
        If e.Button = MouseButtons.Right Then
            ' Show the context menu at the mouse position
            PSFunctResultsContextMenu2.Show(Cursor.Position)
        End If
    End Sub

    Private Sub Panel3_Paint(sender As Object, e As PaintEventArgs) Handles Panel3.Paint

    End Sub

    Private Sub PSFunctResultsContextMenu_Opening(sender As Object, e As System.ComponentModel.CancelEventArgs) Handles PSFunctResultsContextMenu.Opening

    End Sub

    Private Sub OpenInNewWindowToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles OpenInNewWindowToolStripMenuItem.Click
        If ShellyAiResponseZoom Is Nothing OrElse ShellyAiResponseZoom.IsDisposed Then
            ' Create a new instance if it's not open or has been closed
            RestoreWindow(ShellyAiResponseZoom)
        Else
            ' If the form is minimized, restore it
            If ShellyAiResponseZoom.WindowState = FormWindowState.Minimized Then
                ShellyAiResponseZoom.WindowState = FormWindowState.Normal
            End If
            ' Bring it to the front
            ShellyAiResponseZoom.BringToFront()
        End If

        ' Update RichTextBox in zoom form with the latest text
        ShellyAiResponseZoom.RichTextBox1.Text = PSFunctResultsBox.Text
        ActiveControl = Nothing
    End Sub

    Private Sub UserInputBox_TextChanged(sender As Object, e As EventArgs) Handles UserInputBox.TextChanged

    End Sub
    ' =======================
    ' Right-Click Menu
    ' =======================
    Private Sub OpenCopilotToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles OpenCopilotToolStripMenuItem.Click
        Copilot.Show()
        ActiveControl = Nothing
    End Sub

    Private Sub ConsoleStripMenuItem_Click(sender As Object, e As EventArgs) Handles ConsoleToolStripMenuItem.Click
        Console.Show()
        ActiveControl = Nothing
    End Sub

    Private Sub HintsText_TextChanged(sender As Object, e As EventArgs)

    End Sub

    Private Sub HintTime_Tick(sender As Object, e As EventArgs) Handles HintTime.Tick

    End Sub

    Private Sub Panel5_Paint(sender As Object, e As PaintEventArgs) Handles Panel5.Paint

    End Sub

    Private Sub WebView2Google_Click(sender As Object, e As EventArgs) Handles WebView2Google.Click

    End Sub

End Class

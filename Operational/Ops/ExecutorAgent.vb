' ###  ExecutorAgent.vb - v1.0.1 ### 

' ##########################################################
'  Shelly - v1.0.1
'  License: Creative Commons Attribution-NonCommercial (CC BY-NC)
'  https://creativecommons.org/licenses/by-nc/4.0/
'  © 2025 Vlad Stefanescu | GreenCoders.net. Attribution required.
' ##########################################################

' ********** FOR NEW FUNCTIONS UPDATE: **********
'   -> ToolPlanner.vb
'   -> CustomFunctionsEngine.vb
'   -> CustomFunctions.vb
'   -> ExecutorAgent.vb
'   -> CustomFunctionsEngine.VB (FOR CONSOLE HELP)
' ***********************************************

Imports System.Text
Imports System.Text.RegularExpressions
Imports System.Threading
Imports DocumentFormat.OpenXml.Office2019.Drawing
Imports Newtonsoft.Json
Imports ShellyAI.Shelly
Imports System.IO

Public Module ExecutorAgent

    ' Keeps signatures of tool+arg combinations executed in this request
    Private ReadOnly executedSteps As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)

    Public Async Function ExecutePlanAsync(
    plan As List(Of PlanStep),
    ct As CancellationToken
) As Task(Of List(Of String))

        ' List to store results of each step
        Dim stepResults As New List(Of String)()

        ' Remember the raw text of the last result we showed
        Dim lastResultText As String = ""

        ' Reset for this user prompt - using the module-level field
        executedSteps.Clear()
        Dim idx As Integer = 0
        For Each originalStep In plan.OrderBy(Function(p) p.StepIndex)
            idx += 1
            If ct.IsCancellationRequested Then Exit For

            ' 1️⃣ Optionally refine step #2 and beyond
            Dim stepDef As PlanStep = originalStep

            stepDef = Await RefinePlanStepAsync(stepDef, ct) ' change to RefinePlanStepAsync for CallGPTCore
            Debug.WriteLine($"[ExecutorAgent] Refined step #{idx}: {stepDef.Tool} args: {JsonConvert.SerializeObject(stepDef.Args)}")


            ' 2️⃣ Signature & de-dup (unchanged)
            Dim signature = $"{stepDef.Tool}|{JsonConvert.SerializeObject(stepDef.Args)}"
            If executedSteps.Contains(signature) Then Continue For
            executedSteps.Add(signature)

            Shelly.Instance.LabelStatusUpdate.Text = $"Step {stepDef.StepIndex}: {stepDef.Tool}"

            '============== Pre-step AI check & optional plan refinement ================
            If idx > 1 Then
                Try
                    Dim prompt As String =
                    "You just generated a step and are about to execute the next one. " &
                    "Please check the conversation history and, if this segment should be tweaked based on what’s already run or even changed, " &
                    "return an updated JSON PlanStep:" & Environment.NewLine &
                    JsonConvert.SerializeObject(stepDef) & "otherwise return the original json unchanged. ****DO NOT INLUDE ANY COMMENTS OR EXPLANATIONS OR THE APP WILL FAIL!***"

                    Dim aiResponse As String = Await AIBrainiac.CallGPTBrain(
                    Globals.UserApiKey,
                    prompt,
                    Globals.AssistantId,
                    Globals.conversationHistory
                )

                    Dim m As Match = Regex.Match(aiResponse, "(\{[\s\S]*\})")
                    If m.Success Then
                        stepDef = JsonConvert.DeserializeObject(Of PlanStep)(m.Groups(1).Value)
                        Debug.WriteLine($"[PreStepAI] Step updated to: {stepDef.Tool} args {JsonConvert.SerializeObject(stepDef.Args)}")
                    End If

                Catch ex As Exception
                    Debug.WriteLine($"[PreStepAI] Failed: {ex.Message}")
                End Try
            End If
            '===========================================================================

            Dim resultText As String = ""
            Try
                Select Case stepDef.Tool

                    Case "ExecutePowerShellScript", "StartOrRunApplicationByName", "TakePrintScreenOrScreenShot"
                        ' 1) Build the raw AI‐generated script
                        Dim rawScript As String
                        If stepDef.Tool = "ExecutePowerShellScript" Then
                            rawScript = CStr(stepDef.Args("script"))
                        ElseIf stepDef.Tool = "StartOrRunApplicationByName" Then
                            Dim appName = CStr(stepDef.Args("appName")).Replace("'", "''")
                            rawScript = CustomFunctions.StartOrRunApplicationByNameScript & Environment.NewLine &
                    $"StartOrRunApplicationByName -appName '{appName}'"
                        Else  ' TakePrintScreenOrScreenShot
                            Dim outputPath = CStr(stepDef.Args("outputPath")).Replace("'", "''")
                            rawScript = CustomFunctions.TakePrintScreenOrScreenShotScript & Environment.NewLine &
                    $"TakePrintScreenOrScreenShot -OutputPath '{outputPath}'"
                        End If

                        ' 2) Only unescape \n, \r, \t—not every backslash sequence
                        Dim script As String = UnescapeLiterals(rawScript)

                        ' 3) Run it
                        Await Shelly.Instance.ExecutePowerShellWithFixLoopAsync(script, ct)
                        Continue For

                    Case "FreeResponse"
                        ' Only show this if it's not just repeating the last result
                        Dim candidate = CStr(stepDef.Args("text"))
                        If candidate <> lastResultText Then
                            resultText = candidate
                        Else
                            ' skip redundant echo
                            Continue For
                        End If

                    Case "GenerateBatchAndPs1File"
                        Dim folder = CStr(stepDef.Args("outputFolder"))
                        Dim query = CStr(stepDef.Args("userQuery"))
                        resultText = Await CustomFunctions.GenerateBatchAndPs1File(folder, query)

                    Case "ReadFileAndAnswer"
                        Dim paths = CStr(stepDef.Args("filePaths"))
                        Dim q = CStr(stepDef.Args("query"))
                        resultText = Await CustomFunctions.ReadFileAndAnswer(paths, q)

                    Case "WebSearchAndRespondBasedOnPageContent"
                        Dim promptQ = CStr(stepDef.Args("promptQuery"))
                        Dim site = CStr(stepDef.Args("siteName"))
                        Dim q2 = CStr(stepDef.Args("query"))
                        resultText = Await CustomFunctions.WebSearchAndRespondBasedOnPageContent(promptQ, site, q2, ct)

                    Case "ReadWebPageAndRespondBasedOnPageContent"
                        Dim urlObj As Object = Nothing
                        Dim queryObj As Object = Nothing
                        If stepDef.Args.TryGetValue("url", urlObj) AndAlso stepDef.Args.TryGetValue("query", queryObj) Then
                            Dim url = CStr(urlObj)
                            Dim query = CStr(queryObj)
                            resultText = Await CustomFunctions.ReadWebPageAndRespondBasedOnPageContent(url, query, ct)
                        Else
                            resultText = "[Executor Error] ReadWebPageAndRespondBasedOnPageContent: Missing required arguments 'url' or 'query'."
                            Debug.WriteLine(resultText)
                        End If

                    Case "GenerateImages"
                        Dim imgPromptObj As Object = Nothing
                        Dim numObj As Object = Nothing
                        Dim styleObj As Object = Nothing
                        Dim folderObj As Object = Nothing
                        If stepDef.Args.TryGetValue("imagePrompt", imgPromptObj) AndAlso
                           stepDef.Args.TryGetValue("numImages", numObj) AndAlso
                           stepDef.Args.TryGetValue("style", styleObj) AndAlso
                           stepDef.Args.TryGetValue("folderPath", folderObj) Then

                            Dim imgPrompt = CStr(imgPromptObj)
                            Dim num = Convert.ToInt32(numObj)
                            Dim style = CStr(styleObj)
                            Dim folder = CStr(folderObj)
                            resultText = Await CustomFunctions.GenerateImages(imgPrompt, num, style, folder)
                        Else
                            resultText = "[Executor Error] GenerateImages: Missing required arguments in Args dictionary."
                            Debug.WriteLine(resultText)
                        End If

                    Case "ImageAnswer"
                        Dim imgs = CStr(stepDef.Args("imagePaths"))
                        Dim iq = CStr(stepDef.Args("query"))
                        resultText = Await CustomFunctions.ImageAnswer(imgs, iq)

                    Case "CheckMyScreenAndAnswer"
                        Dim query = CStr(stepDef.Args("query"))
                        resultText = Await CustomFunctions.CheckMyScreenAndAnswer(query)

                    Case "GenerateLargeFileWithTextOrCode"
                        Dim topic = CStr(stepDef.Args("topic"))
                        Dim outputPath = CStr(stepDef.Args("outputPath"))
                        Dim chunks = GetArgWithDefault(stepDef.Args, "totalChunks", 5)
                        resultText = Await CustomFunctions.GenerateLargeFileWithTextOrCode(topic, outputPath, chunks)

                    Case "WriteInsideFileOrWindow"
                        Dim topic2 = CStr(stepDef.Args("topic"))
                        Dim chunks2 = Convert.ToInt32(stepDef.Args("totalChunks"))
                        resultText = Await CustomFunctions.WriteInsideFileOrWindow(topic2, chunks2)
                    Case "UpdateFileByChunks"
                        Dim path = CStr(stepDef.Args("filePath"))
                        Dim instr = CStr(stepDef.Args("updateInstruction"))
                        Dim updatedCode As String = Await CustomFunctions.UpdateFileByChunks(path, instr)
                        If updatedCode.StartsWith("[ERROR]") Then
                            resultText = updatedCode
                        Else
                            File.WriteAllText(path, updatedCode, System.Text.Encoding.UTF8)
                            Globals.FileContents(path) = updatedCode
                            resultText = $"File successfully updated: {path}"
                        End If

                    Case "ChangeOrSetVolume"
                        Dim volume = Convert.ToInt32(stepDef.Args("volumePercentage"))
                        CustomFunctions.ChangeOrSetVolume(volume)
                        resultText = $"Volume set to {volume}%."

                    Case "SendMediaKey"
                        Dim keyName = CStr(stepDef.Args("keyName"))
                        CustomFunctions.SendMediaKey(keyName)
                        resultText = $"Media key '{keyName}' sent."

                    Case Else
                        resultText = $"[Executor] Unknown tool: {stepDef.Tool}"
                End Select

            Catch ex As Exception
                resultText = $"[Executor Error] {stepDef.Tool}: {ex.Message}"
            End Try


            ' 3️⃣ Display & log (unchanged)
            If Not String.IsNullOrWhiteSpace(resultText) Then
                ShellyOps.AppendResultToBox(resultText)
                Shelly.LogDebugInformation("N/A", Globals.conversationHistory, resultText, 0, Shelly.CalculateTokenCount(resultText))
                Await convHistory.TrimConversationHistoryByTokens_Dict(Globals.conversationHistory, Globals.MaxTotalTokens, resultText)
                lastResultText = resultText
            End If

            stepResults.Add(resultText)
            Await Task.Delay(50, ct)
        Next

        Shelly.Instance.LabelStatusUpdate.Text = "All planned tasks completed."

        ' Return the list of results
        Return stepResults
    End Function


    Public Function UnescapeLiterals(input As String) As String
        Dim result = input
        result = result.Replace("\r\n", Environment.NewLine)
        result = result.Replace("\n", Environment.NewLine)
        result = result.Replace("\r", Environment.NewLine)
        result = result.Replace("\t", vbTab)
        Return result
    End Function

    ' Helper function to safely get parameters with defaults
    Private Function GetArgWithDefault(Of T)(args As Dictionary(Of String, Object), key As String, defaultValue As T) As T
        Dim value As Object = Nothing
        If args.TryGetValue(key, value) Then
            Try
                Return CType(value, T)
            Catch ex As Exception
                Debug.WriteLine($"[WARNING] Cannot convert parameter '{key}' to {GetType(T).Name}, using default: {defaultValue}")
                Return defaultValue
            End Try
        Else
            Debug.WriteLine($"[WARNING] Missing parameter '{key}', using default: {defaultValue}")
            Return defaultValue
        End If
    End Function

    Private Async Function RefinePlanStepAsync(
    planStep As PlanStep,
    ct As CancellationToken
) As Task(Of PlanStep)

        ' Build a single string of the entire conversation history
        Dim historyText As New StringBuilder()
        For Each msg In Globals.conversationHistory
            historyText.AppendLine($"{msg("role").ToUpper()}: {msg("content")}")
            historyText.AppendLine()
        Next

        ' System prompt explaining the refinement task
        Dim sysPrompt =
        "You are a JSON Validator.The fllowing JSON will be used by ChatGPT inorder to perform a task." & Environment.NewLine &
        "Given the full conversation history and a single PlanStep JSON, " &
        "replace any placeholder argument values (e.g. file paths, URLs, Content) with the actual " &
        "values found in that history. You may change the prompt/query (text) to better match the Conversation History to ensure the best possible result." & Environment.NewLine &
        "If no change is needed, return the original PlanStep JSON."

        ' Serialize the PlanStep to send to the model
        Dim stepJson = JsonConvert.SerializeObject(planStep)

        ' Prepare messages
        Dim messages = New List(Of Dictionary(Of String, String)) From {
        New Dictionary(Of String, String) From {
            {"role", "system"}, {"content", sysPrompt}
        },
        New Dictionary(Of String, String) From {
            {"role", "assistant"}, {"content", historyText.ToString().Trim()}
        },
        New Dictionary(Of String, String) From {
            {"role", "user"}, {"content", stepJson}
        }
    }

        Const maxAttempts As Integer = 3
        For attempt As Integer = 1 To maxAttempts
            ' Call the core GPT model
            Dim resp As String = Await AIcall.CallGPTCore(
            Globals.UserApiKey,
            Globals.AiModelSelection,
            messages,
            Globals.temperature,
            ct
        )

            ' Extract the first {...} JSON block
            Dim m = Regex.Match(resp, "(\{[\s\S]*\})")
            If m.Success Then
                Dim jsonOnly = m.Groups(1).Value
                Try
                    Return JsonConvert.DeserializeObject(Of PlanStep)(jsonOnly)
                Catch ex As Exception
                    Debug.WriteLine($"[RefinePlanStep] Parse error (attempt {attempt}): {ex.Message}")
                End Try
            Else
                Debug.WriteLine($"[RefinePlanStep] No JSON block found (attempt {attempt})")
            End If

            ' Exponential back-off
            Await Task.Delay(500 * attempt, ct)
        Next

        ' All attempts failed: return original
        Return planStep
    End Function


End Module

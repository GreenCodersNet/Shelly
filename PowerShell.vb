' ###  AIBrainiac.vb - v1.0.1 ### 

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
Imports Newtonsoft.Json.Linq

Module PowerShell

    ' Enhanced conversation history trimming
    Public Sub OptimizeConversationHistory(ByRef conversationHistory As List(Of Dictionary(Of String, String)))
        If conversationHistory Is Nothing OrElse conversationHistory.Count <= 5 Then Return

        ' Keep only the most recent and most relevant messages
        Dim optimizedHistory As New List(Of Dictionary(Of String, String))

        ' Always keep system messages
        For Each msg In conversationHistory
            If msg.ContainsKey("role") AndAlso msg("role") = "system" Then
                optimizedHistory.Add(msg)
            End If
        Next

        ' Keep last 10 user/assistant exchanges (20 messages total)
        Dim userAssistantMsgs = conversationHistory.Where(Function(m) m.ContainsKey("role") AndAlso (m("role") = "user" OrElse m("role") = "assistant")).TakeLast(20).ToList()

        optimizedHistory.AddRange(userAssistantMsgs)

        ' Replace original with optimized version
        conversationHistory.Clear()
        conversationHistory.AddRange(optimizedHistory)

        Debug.WriteLine($"[OptimizeHistory] Reduced to {conversationHistory.Count} messages")
    End Sub




    ' ================================================================
    ' UPDATED FUNCTION: ExtractPowerShellScripts
    ' ? Returns only unique PowerShell blocks, in order of appearance.
    ' ================================================================
    Private Function ExtractPowerShellScripts(aiResponse As String) As List(Of String)
        Dim scripts As New List(Of String)()
        If String.IsNullOrWhiteSpace(aiResponse) Then Return scripts

        ' Regex to find ```powershell ? ``` blocks
        Dim pattern As String = "```powershell\s*([\s\S]*?)\s*```"
        Dim matches As MatchCollection = Regex.Matches(aiResponse, pattern, RegexOptions.IgnoreCase)

        ' Track which blocks we?ve already added
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

    ' Small diff-style snippet between original and updated scripts
    Private Function BuildDiffSnippet(originalScript As String, updatedScript As String) As String
        If String.IsNullOrWhiteSpace(originalScript) OrElse String.IsNullOrWhiteSpace(updatedScript) Then Return String.Empty

        Dim oldLines = originalScript.Split({vbCrLf, vbLf}, StringSplitOptions.None)
        Dim newLines = updatedScript.Split({vbCrLf, vbLf}, StringSplitOptions.None)
        Dim maxLines = Math.Min(oldLines.Length, newLines.Length)
        Dim snippets As New List(Of String)()

        For i = 0 To maxLines - 1
            If Not String.Equals(oldLines(i), newLines(i), StringComparison.Ordinal) Then
                snippets.Add("- " & oldLines(i))
                snippets.Add("+ " & newLines(i))
                If snippets.Count >= 4 Then Exit For
            End If
        Next

        If snippets.Count = 0 AndAlso oldLines.Length <> newLines.Length Then
            snippets.Add($"(length change) old {oldLines.Length} lines -> new {newLines.Length} lines")
        End If

        Return String.Join(vbLf, snippets)
    End Function

    Private Function IsPowerShellScriptSafe(script As String) As Boolean
        Dim loweredScript = script.ToLowerInvariant()

        ' 0) Always forbidden commands / constructs
        Dim forbiddenCommandsAlways = {
            "reg add", "reg delete", "reg query", "hklm", "hkey_local_machine",
            "hkey_classes_root", "set-itemproperty", "new-itemproperty",
            "invoke-expression", "iex", "downloadstring", "frombase64string",
            "add-type", "start-process", "powershell -enc", "set-executionpolicy",
            "new-object net.webclient", "rundll32", "mshta", "bitsadmin", "certutil"
        }
        For Each cmd In forbiddenCommandsAlways
            If loweredScript.Contains(cmd) Then Return False
        Next

        ' 1) Always-forbidden system folders (never allowed)
        Dim forbiddenPaths = {
        "c:\windows",
        "c:\program files",
        "c:\programdata",
        "c:\system32",
        "c:\boot",
        "c:\recovery"
    }
        For Each path In forbiddenPaths
            If loweredScript.Contains(path) Then
                Return False
            End If
        Next

        ' 2) Root-C:\ protection, only if the user has it enabled
        If SecurityFlags.BlockSystemC Then
            ' Block any C:\ path not under C:\Shelly\
            If loweredScript.Contains("c:\") AndAlso
           Not loweredScript.Contains("c:\shelly\") Then
                Return False
            End If
        End If

        ' 3) Optional user-configured checks
        If SecurityFlags.BlockNetworkCalls Then
            Dim netKeys = {"invoke-webrequest", "invoke-restmethod", "start-bitstransfer", "curl", "wget", "net.webclient", "new-object net.webclient"}
            If netKeys.Any(Function(k) loweredScript.Contains(k)) Then Return False
        End If

        If SecurityFlags.BlockEnvVariableAccess Then
            If loweredScript.Contains("$env:") Then Return False
        End If

        If SecurityFlags.BlockBackgroundJobs Then
            Dim jobKeys = {"start-job", "invoke-command", "register-scheduledtask", "runspace", "new-thread"}
            If jobKeys.Any(Function(k) loweredScript.Contains(k)) Then Return False
        End If

        Return True
    End Function



    Public Async Function ExecutePowerShellScriptAsync(
    script As String,
    ct As CancellationToken
) As Task(Of Tuple(Of Boolean, String))
        Shelly.LabelStatusUpdate.Text = "Executing PowerShell script..."
        Try
            Dim modifiedScript As String = "$ErrorActionPreference = 'Stop';" & Environment.NewLine

            If SecurityFlags.ConstrainedLanguageMode Then
                modifiedScript &= "$ExecutionContext.SessionState.LanguageMode = 'ConstrainedLanguage';" & Environment.NewLine
            End If

            modifiedScript &= script

            Dim bytes() = Encoding.Unicode.GetBytes(modifiedScript)
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

                ' Store process reference for potential cancellation
                SyncLock Shelly.processLock
                    Shelly.currentPowerShellProcess = proc
                End SyncLock

                proc.Start()
                Dim outTask = proc.StandardOutput.ReadToEndAsync(ct)
                Dim errTask = proc.StandardError.ReadToEndAsync(ct)
                Dim waitTask = Task.Run(Sub() proc.WaitForExit(), ct)
                Await Task.WhenAll(outTask, errTask, waitTask)
                Dim exitCode = proc.ExitCode

                ' Clear process reference
                SyncLock Shelly.processLock
                    Shelly.currentPowerShellProcess = Nothing
                End SyncLock

                Return If(exitCode = 0,
                Tuple.Create(True, outTask.Result),
                Tuple.Create(False, errTask.Result))
            End Using

        Catch ex As OperationCanceledException
            ' Ensure process cleanup on cancellation
            SyncLock Shelly.processLock
                If Shelly.currentPowerShellProcess IsNot Nothing AndAlso Not Shelly.currentPowerShellProcess.HasExited Then
                    Try
                        Shelly.currentPowerShellProcess.Kill()
                        Shelly.currentPowerShellProcess.WaitForExit(5000) ' Wait max 5 seconds
                    Catch killEx As Exception
                        Debug.WriteLine($"Error killing PowerShell process: {killEx.Message}")
                    End Try
                    Shelly.currentPowerShellProcess = Nothing
                End If
            End SyncLock
            Return Tuple.Create(False, "Execution canceled by user.")
        Catch ex As Exception
            ' Ensure process cleanup on any exception
            SyncLock Shelly.processLock
                Shelly.currentPowerShellProcess = Nothing
            End SyncLock
            Return Tuple.Create(False, $"Exception: {ex.Message}")
        End Try
    End Function

    Public Function IsRunningAsAdmin() As Boolean
        Dim identity = System.Security.Principal.WindowsIdentity.GetCurrent()
        Dim principal = New System.Security.Principal.WindowsPrincipal(identity)
        Return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator)
    End Function
    Public Async Function ExecutePowerShellWithFixLoopAsync(
    originalScript As String,
    ct As CancellationToken,
    Optional telemetryRecord As PowerShellExecutionRecord = Nothing
) As Task
        Globals.LoadPowerShellSecuritySettings()
        Dim scriptToRun = originalScript
        Dim maxAttempts = 5
        Dim attempt = 0
        Dim finalOutput As String = ""
        Dim wasSuccessful As Boolean = False
        Dim wasBlockedBySafety As Boolean = False
        Dim lastErrorOutput As String = String.Empty

        ' Log start of the loop
        LogDebugInformation("N/A", conversationHistory,
        "[PS Loop Init] Starting PowerShell execution loop." & Environment.NewLine & originalScript,
        0, 0)

        While attempt < maxAttempts AndAlso Not ct.IsCancellationRequested
            attempt += 1
            Shelly.LabelStatusUpdate.Text = $"Running PowerShell script? Attempt #{attempt}"
            If telemetryRecord IsNot Nothing Then
                telemetryRecord.Attempt = attempt
            End If

            ' ?? SECURITY CHECK BEFORE EXECUTION
            If Not IsPowerShellScriptSafe(scriptToRun) Then
                wasBlockedBySafety = True

                Dim blockedMsg =
                "PowerShell script was blocked by safety measures." & Environment.NewLine &
                "Access to system paths or critical commands is restricted by your current security settings."

                ' --- SHOW block message, not the script itself ---
                Using msgForm As New MessageForm(blockedMsg, "Script Blocked")
                    msgForm.ShowDialog()
                End Using

                AppendResultToBox("? PowerShell script was blocked by safety measures.")
                LogDebugInformation("N/A", conversationHistory,
                "[SECURITY BLOCKED] Unsafe PowerShell script was blocked:" & Environment.NewLine & scriptToRun,
                0, 0)

                If telemetryRecord IsNot Nothing Then
                    telemetryRecord.Blocked = True
                    telemetryRecord.StdErr = blockedMsg
                    telemetryRecord.ExitCode = -1
                    telemetryRecord.FinishedAt = DateTimeOffset.UtcNow
                End If

                Exit While
            End If

            ' Execute the script
            Dim result = Await ExecutePowerShellScriptAsync(scriptToRun, ct)
            Dim success = result.Item1
            Dim raw = result.Item2
            Dim clean = Shelly.RemoveCodeBlocks(raw).Trim()
            If Not success Then
                lastErrorOutput = clean
            End If

            ' 1) Constrained Language Mode block
            If SecurityFlags.ConstrainedLanguageMode Then
                Dim lowerClean = clean.ToLowerInvariant()
                If lowerClean.Contains("language mode") OrElse lowerClean.Contains("fulllanguage") Then
                    wasBlockedBySafety = True

                    Dim msg = "? PowerShell script was blocked due to Constrained Language Mode."

                    Using frm As New MessageForm(msg, "Script Blocked")
                        frm.ShowDialog()
                    End Using

                    AppendResultToBox(msg)
                    LogDebugInformation("N/A", conversationHistory,
            "[SECURITY BLOCKED] Script blocked by Constrained Language Mode.",
            0, 0)

                    If telemetryRecord IsNot Nothing Then
                        telemetryRecord.Blocked = True
                        telemetryRecord.StdErr = msg
                        telemetryRecord.ExitCode = -1
                        telemetryRecord.FinishedAt = DateTimeOffset.UtcNow
                    End If

                    Exit While
                End If
            End If

            ' 2) Network Access block
            If SecurityFlags.BlockNetworkCalls Then
                Dim lowerClean = clean.ToLowerInvariant()
                Dim netKeys = {"invoke-webrequest", "invoke-restmethod", "start-bitstransfer", "curl", "wget", "net.webclient", "new-object net.webclient"}
                If netKeys.Any(Function(k) lowerClean.Contains(k)) Then
                    wasBlockedBySafety = True

                    Dim msg = "? PowerShell script was blocked due to Network Access restriction."

                    Using frm As New MessageForm(msg, "Script Blocked")
                        frm.ShowDialog()
                    End Using

                    AppendResultToBox(msg)
                    LogDebugInformation("N/A", conversationHistory,
            "[SECURITY BLOCKED] Script blocked by Network Access restriction.",
            0, 0)

                    If telemetryRecord IsNot Nothing Then
                        telemetryRecord.Blocked = True
                        telemetryRecord.StdErr = msg
                        telemetryRecord.ExitCode = -1
                        telemetryRecord.FinishedAt = DateTimeOffset.UtcNow
                    End If

                    Exit While
                End If
            End If

            ' 3) Environment Variable Access block
            If SecurityFlags.BlockEnvVariableAccess Then
                Dim lowerClean = clean.ToLowerInvariant()
                If lowerClean.Contains("$env:") Then
                    wasBlockedBySafety = True

                    Dim msg = "? PowerShell script was blocked due to Environment Variable restriction."

                    Using frm As New MessageForm(msg, "Script Blocked")
                        frm.ShowDialog()
                    End Using

                    AppendResultToBox(msg)
                    LogDebugInformation("N/A", conversationHistory,
            "[SECURITY BLOCKED] Script blocked by Environment Variable restriction.",
            0, 0)

                    If telemetryRecord IsNot Nothing Then
                        telemetryRecord.Blocked = True
                        telemetryRecord.StdErr = msg
                        telemetryRecord.ExitCode = -1
                        telemetryRecord.FinishedAt = DateTimeOffset.UtcNow
                    End If

                    Exit While
                End If
            End If

            ' 4) Background Jobs block
            If SecurityFlags.BlockBackgroundJobs Then
                Dim lowerClean = clean.ToLowerInvariant()
                Dim jobKeys = {"start-job", "invoke-command", "register-scheduledtask", "runspace", "new-thread"}
                If jobKeys.Any(Function(k) lowerClean.Contains(k)) Then
                    wasBlockedBySafety = True

                    Dim msg = "? PowerShell script was blocked due to Background Jobs restriction."

                    Using frm As New MessageForm(msg, "Script Blocked")
                        frm.ShowDialog()
                    End Using

                    AppendResultToBox(msg)
                    LogDebugInformation("N/A", conversationHistory,
                    "[SECURITY BLOCKED] Script blocked by Background Jobs restriction.",
                    0, 0)

                    If telemetryRecord IsNot Nothing Then
                        telemetryRecord.Blocked = True
                        telemetryRecord.StdErr = msg
                        telemetryRecord.ExitCode = -1
                        telemetryRecord.FinishedAt = DateTimeOffset.UtcNow
                    End If

                    Exit While
                End If
            End If

            If success Then
                If String.IsNullOrWhiteSpace(clean) Then
                    success = False
                    clean = "[EMPTY OUTPUT] PowerShell returned no data."
                    lastErrorOutput = clean
                Else
                    ' Validate output against user request
                    Dim validation = Await ValidatePowerShellResultAsync(Globals.OriginalUserRequest, scriptToRun, clean, ct)
                    If Not validation.IsValid Then
                        success = False
                        clean = $"[VALIDATION FAILED] {validation.Reason}"
                        lastErrorOutput = clean
                    End If
                End If
            End If

            If success Then
                wasSuccessful = True
                finalOutput = clean

                LogDebugInformation("N/A", conversationHistory,
                "[PS Success] " & finalOutput, 0, CalculateTokenCount(finalOutput))

                If telemetryRecord IsNot Nothing Then
                    telemetryRecord.ExitCode = 0
                    telemetryRecord.StdOut = clean
                    telemetryRecord.StdErr = String.Empty
                    telemetryRecord.Blocked = False
                    telemetryRecord.FinishedAt = DateTimeOffset.UtcNow
                End If

                Exit While
            Else
                LogDebugInformation("N/A", conversationHistory,
                $"[PS Failure #{attempt}] {clean}", 0, CalculateTokenCount(clean))

                If telemetryRecord IsNot Nothing Then
                    telemetryRecord.StdErr = clean
                End If

                If attempt < maxAttempts Then
                    ' -- Try heuristic patch first --
                    Dim heuristicPatch = PowerShellRemediation.TryHeuristicFix(scriptToRun, clean)
                    If heuristicPatch IsNot Nothing Then
                        Debug.WriteLine("[PS Remediation] Applied heuristic fix")
                        If telemetryRecord IsNot Nothing Then
                            Dim diff = BuildDiffSnippet(scriptToRun, heuristicPatch)
                            telemetryRecord.RemediationNote = $"retry with heuristic fix:{vbLf}{diff}"
                        End If
                        scriptToRun = heuristicPatch
                        Continue While
                    End If

                    ' Ask GPT for a fix
                    Dim fixPrompt =
                    $"I tried running this PowerShell script but got an error or invalid/empty output:{Environment.NewLine}{clean}{Environment.NewLine}" &
                    $"Script:{Environment.NewLine}```powershell{Environment.NewLine}{scriptToRun}{Environment.NewLine}```" &
                    Environment.NewLine & "Please return only a corrected script in a ```powershell block that satisfies the user request."

                    Dim fixResp = Await AIcall.CallGPTCore(
                    Globals.UserApiKey,
                    AiModel,
                    New List(Of Dictionary(Of String, String)) From {
                        New Dictionary(Of String, String) From {
                            {"role", "system"}, {"content", "You are a PowerShell troubleshooting assistant."}
                        },
                        New Dictionary(Of String, String) From {
                            {"role", "user"}, {"content", fixPrompt}
                        }
                    },
                    Globals.temperature,
                    ct
                )

                    LogDebugInformation("N/A", conversationHistory,
                    "[PS Repair GPT] " & fixResp, 0, CalculateTokenCount(fixResp))

                    Dim fixes = ExtractPowerShellScripts(fixResp)
                    If fixes.Count > 0 Then
                        If telemetryRecord IsNot Nothing Then
                            Dim diff = BuildDiffSnippet(scriptToRun, fixes(0))
                            telemetryRecord.RemediationNote = $"retry with GPT fix:{vbLf}{diff}"
                        End If
                        scriptToRun = fixes(0)
                    Else
                        Exit While
                    End If
                End If
            End If
        End While

        ' -- Final status -----------------------------------
        If wasSuccessful Then
            Debug.WriteLine("2 ->" & finalOutput)
            AppendResultToBox(finalOutput)
            conversationHistory.Add(ConversationHistoryFunctions.CreateHistoryMessage("assistant", finalOutput))

        ElseIf Not wasBlockedBySafety Then
            Dim msg = "? All attempts to run/fix PowerShell script failed."
            Debug.WriteLine("3 ->")
            AppendResultToBox(msg)
            If telemetryRecord IsNot Nothing Then
                telemetryRecord.ExitCode = -1
                telemetryRecord.StdErr = If(String.IsNullOrWhiteSpace(lastErrorOutput), msg, lastErrorOutput)
                telemetryRecord.FinishedAt = DateTimeOffset.UtcNow
            End If
        End If

        If telemetryRecord IsNot Nothing AndAlso telemetryRecord.FinishedAt = DateTimeOffset.MinValue Then
            telemetryRecord.FinishedAt = DateTimeOffset.UtcNow
        End If

        ' Trim conversation history
        Await convHistory.TrimConversationHistoryByTokens_Dict(
        Globals.conversationHistory,
        Globals.MaxTotalTokens,
        If(wasSuccessful, finalOutput, "PowerShell failure."))
    End Function

    Private Async Function ValidatePowerShellResultAsync(userRequest As String, script As String, output As String, ct As CancellationToken) As Task(Of (IsValid As Boolean, Reason As String))
        Try
            Dim messages As New List(Of Dictionary(Of String, String)) From {
                New Dictionary(Of String, String) From {
                    {"role", "system"},
                    {"content", "You are a strict validator. Given a user request, a PowerShell script that was run, and its output, decide if the output satisfies the request. Respond with JSON: {""valid"":true|false,""reason"":string}. If empty or unrelated, valid=false."}
                },
                New Dictionary(Of String, String) From {
                    {"role", "user"},
                    {"content", $"User request: {userRequest}{Environment.NewLine}Script:{Environment.NewLine}{script}{Environment.NewLine}Output:{Environment.NewLine}{output}"}
                }
            }

            Dim resp = Await AIcall.CallGPTCore(Globals.UserApiKey, AiModel, messages, Globals.temperature, ct, jsonMode:=True)
            Dim jo = JObject.Parse(resp)
            Dim valid = jo("valid")?.ToObject(Of Boolean)() = True
            Dim reason = jo("reason")?.ToString()
            Return (valid, If(reason, ""))
        Catch ex As Exception
            Debug.WriteLine($"[PS Validate] Failed: {ex.Message}")
            Return (True, "") ' fail open to avoid blocking when validator fails
        End Try
    End Function

    Public Class PowerShellExecutionRecord
        Public Property Attempt As Integer
        Public Property ExitCode As Integer
        Public Property StdOut As String
        Public Property StdErr As String
        Public Property Blocked As Boolean
        Public Property FinishedAt As DateTimeOffset
        Public Property ScriptHash As String
        Public Property Duration As TimeSpan
        Public Property RemediationNote As String ' Added for remediation tracking
    End Class

End Module

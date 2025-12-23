' ###  Shelly.vb - v1.0.1 ### 

' ##########################################################
'  Shelly - v1.0.1
'  License: Creative Commons Attribution-NonCommercial (CC BY-NC)
'  https://creativecommons.org/licenses/by-nc/4.0/
'  � 2025 Vlad Stefanescu | GreenCoders.net. Attribution required.
' ##########################################################

Imports System.Runtime.InteropServices
Imports System.IO
Imports System.Text.RegularExpressions
Imports System.Threading
Imports System.Text
Imports Newtonsoft.Json
Imports Newtonsoft.Json.Linq

Partial Public Class Shelly


    <DllImport("user32.dll")>
    Public Shared Function ReleaseCapture() As Boolean
    End Function

    Public Const WM_NCLBUTTONDOWN As Integer = &HA1
    Public Const HTCAPTION As Integer = 2

    Public mouseX As Integer
    Public mouseY As Integer

    Public aiTrainingFile As String = "" 'Globals.TrainingText
    Public totalTokens As Integer = 0
    Public ReadOnly maxHistoryMessages As Integer = 20 ' Increased from 3
    Public AImodel As String = AiModelSelection
    Public cancellationTokenSource As CancellationTokenSource
    Public currentPowerShellProcess As Process
    Public processLock As New Object()
    Public functionRegistry As New FunctionRegistry()
    Public Shared _instance As Shelly

    ' Flags and Constants
    Public isTrainingSent As Boolean = False
    Public isVerificationDone As Boolean = False
    Public Const ScriptLineThreshold As Integer = 5
    Public executedCalls As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)

    ' Initial panel height (collapsed state)
    Public Const collapsedHeight As Integer = 0
    Public Const expandedHeight As Integer = 65 ' Adjust this as needed

    ' Flag to track whether the panel is expanded or collapsed
    Public isPanelExpanded As Boolean = False
    Public initialFormHeight As Integer

    ' Separate variable for system prompt
    Public systemPrompt As Dictionary(Of String, String) = Nothing

    ' Google:
    Public isGoogleTranslateLoaded As Boolean = False ' Track if page is loaded

    Public skipNextPlainTextSegment As Boolean = False
    Dim wasBlockedBySafety As Boolean = False

    Public Shared ReadOnly Property Instance As Shelly
        Get
            Return _instance
        End Get
    End Property



    Public Sub New()
        InitializeComponent()
        _instance = Me

        AddHandler Me.Shown, AddressOf Shelly_Shown

        ' Initialize per-run interaction log
        InteractionLog.InitializeInteractionLog()

        ' Reload persisted execution context for planner awareness
        GlobalOutcomeTracker.Instance.LoadFromDisk()
        StepOutputManager.Instance.LoadFromDisk()
    End Sub

    ' Enhanced cleanup method with unique name
    Public Sub CleanupApplicationResources()
        Try
            ' Cancel any ongoing operations
            If cancellationTokenSource IsNot Nothing Then
                cancellationTokenSource.Cancel()
                cancellationTokenSource.Dispose()
                cancellationTokenSource = Nothing
            End If

            ' Kill any running PowerShell processes
            SyncLock processLock
                If currentPowerShellProcess IsNot Nothing AndAlso Not currentPowerShellProcess.HasExited Then
                    Try
                        currentPowerShellProcess.Kill()
                        currentPowerShellProcess.WaitForExit(3000)
                    Catch ex As Exception
                        Debug.WriteLine($"Error killing PowerShell process during cleanup: {ex.Message}")
                    Finally
                        currentPowerShellProcess = Nothing
                    End Try
                End If
            End SyncLock

            ' Cleanup AI modules
            AIcall.Cleanup()
            AIBrainiac.Cleanup()

            ' Clear conversation history to free memory
            conversationHistory?.Clear()

            ' Clear global caches
            Globals.FileContents?.Clear()
            Globals.GeneratedImages?.Clear()
            Globals.TaskData?.Clear()
            executedCalls?.Clear()

            ' Clear debug logs
            Globals.ClearDebugLogs()

            Debug.WriteLine("[Cleanup] Resources cleaned up successfully")
        Catch ex As Exception
            Debug.WriteLine($"[Cleanup] Error during cleanup: {ex.Message}")
        End Try
    End Sub

    ' ==============================
    '       LOGIC & UTILITIES
    ' ==============================

    ' NUMBER OF TOKENS
    Public Shared Function CalculateTokenCount(text As String) As Integer
        If String.IsNullOrEmpty(text) Then Return 0
        ' Count words instead of just characters (closer to OpenAI�s tokenizer)
        Dim words As String() = text.Split({" "c, vbCrLf, vbLf}, StringSplitOptions.RemoveEmptyEntries)
        Return words.Length + (text.Length \ 6) ' words + approx sentence structure
    End Function

    Public Shared Function RemoveCodeBlocks(inputText As String) As String
        Dim pattern As String = "```[\s\S]*?```"
        Return Regex.Replace(inputText, pattern, "").Trim()
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


            ' -- START SESSION --


            cancellationTokenSource = New CancellationTokenSource
            CancelTaskButton.Enabled = True
            Await HandleUserRequestAsync(cancellationTokenSource.Token) ' ==1==

            ' =========================== CONFIRMATION ===========================
            If lastRunMultiTask = True Then
                Debug.WriteLine(" ------- multitask --------------")

                Dim userQuestion As String = "You just finished running a task or multiple tasks based on the User Prompt. 
                Can you do a very short summary of all the tasks that were completed based on Conversation History?" & Environment.NewLine &
                "*** Use the 'FreeResponse' tool for this step. ***" & Environment.NewLine &
                "Do so in a friendly manner without repeating what the User asked, just ***a summary of what was done***." & Environment.NewLine &
                "In case you found any errors or issues, please mention them in the summary." & Environment.NewLine &
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

    ' ================================================================
    ' UPDATED FUNCTION: CancelTaskButton_Click (Enhanced Cancellation Handling)
    ' ================================================================
    Private Sub CancelTaskButton_Click(sender As Object, e As EventArgs) Handles CancelTaskButton.Click
        Try
            ' Stop any typing operations first
            FileHandler.StopTyping()

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
        Me.ActiveControl = Nothing
    End Sub

    Private Async Sub ButtonUseSpeech_Click(sender As Object, e As EventArgs) Handles ButtonUseSpeech.Click
        Try

            If Globals.UserAudioSelection < 0 Then
                LabelStatusUpdate.Text = "Please select a recording device."
                Return
            End If

            ' STT Starts
            isSpeechActive = True
            ButtonUseSpeech.BackgroundImage = My.Resources.mic3
            ButtonUseSpeech.Enabled = False
            Await WebView21.CoreWebView2.ExecuteScriptAsync("setColorGreen();")

            cancellationTokenSource = New CancellationTokenSource()
            Dim selectedDevice As Integer = Globals.UserAudioSelection
            Dim transcription As String = Await StartSpeechToText(selectedDevice)

            ' Only submit real input
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
            ' STT Ends
            isSpeechActive = False
            ButtonUseSpeech.BackgroundImage = My.Resources.mic
            ButtonUseSpeech.Enabled = True
            CancelTaskButton.Enabled = False
            cancellationTokenSource = Nothing
        End Try
        Me.ActiveControl = Nothing
    End Sub

    ' ================================================================
    ' UPDATED FUNCTION: Button4_Click (Enhanced Clear & Reset)
    ' ================================================================
    Private Async Sub Button4_Click(sender As Object, e As EventArgs) Handles Button4.Click
        Try
            ' Set WebView color to default
            Await ExecuteScriptSafeAsync("setColorDefault();")

            ' ?? NEW: Reset intelligent retry system
            RetryStrategy.Reset()
            GlobalOutcomeTracker.Instance.Clear()

            ' Reset execution ledger
            ExecutorAgent.ResetExecutionLedger()

            ' Clear original request
            Globals.OriginalUserRequest = ""

            ' Clear PowerShell execution ledger
            If Globals.TaskData.ContainsKey("PowerShellExecutions") Then
                Dim ledger = TryCast(Globals.TaskData("PowerShellExecutions"), List(Of PowerShellExecutionRecord))
                ledger?.Clear()
            End If

            ' Clear all UI elements FIRST
            PSFunctResultsBox.Clear()
            UserInputBox.Clear()
            AIcommentBox.Clear()
            If AIresponseErrorBox IsNot Nothing Then
                AIresponseErrorBox.Clear()
            End If
            LabelStatusUpdate.Text = "Ready..."

            ' Clear conversation history completely
            conversationHistory.Clear()
            conversationHistory = New List(Of Dictionary(Of String, String))()

            ' Clear all global caches
            Globals.FileContents.Clear()
            Globals.GeneratedImages.Clear()
            Globals.TaskData.Clear()
            executedCalls.Clear()

            ' Reset state variables
            isTrainingSent = False
            isVerificationDone = False
            systemPrompt = Nothing
            totalTokens = 0
            lastRunMultiTask = False
            skipNextPlainTextSegment = False
            wasBlockedBySafety = False

            If Globals.LastFileQuery IsNot Nothing Then
                Globals.LastFileQuery = ""
            End If

            ' Cancel any ongoing operations
            If cancellationTokenSource IsNot Nothing Then
                cancellationTokenSource.Cancel()
                cancellationTokenSource.Dispose()
                cancellationTokenSource = Nothing
            End If

            ' Additional memory cleanup
            PerformPeriodicCleanup()

            ' Clear debug logs
            Globals.ClearDebugLogs()

            ActiveControl = Nothing

            Debug.WriteLine("[Button4] New conversation started - all data cleared")

        Catch ex As Exception
            Debug.WriteLine("Error in Button4_Click: " & ex.Message)
        End Try
        Me.ActiveControl = Nothing
    End Sub

    ' Fixed AppendResultToBox method to ensure proper line breaks
    Public Sub AppendResultToBox(text As String)
        Try
            ' Ensure we add text with proper line breaks for conversation flow
            If Not String.IsNullOrEmpty(PSFunctResultsBox.Text) AndAlso Not PSFunctResultsBox.Text.EndsWith(Environment.NewLine) Then
                PSFunctResultsBox.AppendText(Environment.NewLine)
            End If

            PSFunctResultsBox.AppendText(text)

            ' Ensure text ends with a line break for next message
            If Not text.EndsWith(Environment.NewLine) Then
                PSFunctResultsBox.AppendText(Environment.NewLine)
            End If

            PSFunctResultsBox.ScrollToCaret()

            ' Log assistant reply
            InteractionLog.AppendInteraction("assistant", text)
        Catch ex As Exception
            Debug.WriteLine($"Error appending result: {ex.Message}")
        End Try
    End Sub


    ' Add periodic cleanup method
    Private Sub PerformPeriodicCleanup()
        Try
            ' Force garbage collection
            GC.Collect()
            GC.WaitForPendingFinalizers()
            GC.Collect()

            ' Optimize conversation history
            OptimizeConversationHistory(conversationHistory)

            ' Clear old entries from file cache
            If Globals.FileContents.Count > 50 Then
                Dim keysToRemove = Globals.FileContents.Keys.Take(Globals.FileContents.Count - 25).ToList()
                For Each key In keysToRemove
                    Globals.FileContents.Remove(key)
                Next
                Debug.WriteLine($"[Cleanup] Cleared {keysToRemove.Count} old file cache entries")
            End If

            ' Limit debug logs
            If Globals.GetStoredLogs().Length > 500000 Then ' 500KB limit
                Globals.ClearDebugLogs()
                Debug.WriteLine("[Cleanup] Cleared debug logs due to size limit")
            End If

            ' Reset AI call count periodically
            If AICallCount > 100 Then
                ResetAICallCount()
                Debug.WriteLine("[Cleanup] Reset AI call counter")
            End If

            Debug.WriteLine("[Cleanup] Periodic cleanup completed")
        Catch ex As Exception
            Debug.WriteLine($"[Cleanup] Error in periodic cleanup: {ex.Message}")
        End Try
    End Sub

    Private Sub ShowSettings_Click(sender As Object, e As EventArgs) Handles ShowSettings.Click
        RestoreWindow(Settings)
        ActiveControl = Nothing
    End Sub

    ' ENABLE / DISABLE UI
    Public Sub SetUIState(isEnabled As Boolean)
        ButtonUseSpeech.Enabled = isEnabled
        UserInputBox.ReadOnly = Not isEnabled
        Cursor = If(isEnabled, Cursors.Default, Cursors.WaitCursor)
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

    ' Make sure you have this field at class-level:
    ' Private ShellyAiResponseZoom As ShellyAiResponseZoom

    Private Sub OpenInNewWindowToolStripMenuItem_Click(sender As Object, e As EventArgs) _
    Handles OpenInNewWindowToolStripMenuItem.Click

        ' If we don�t have a live instance (or it was disposed), create one
        RestoreWindow(ShellyAiResponseZoom)

        ' Always update its contents
        ShellyAiResponseZoom.RichTextBox1.Text = PSFunctResultsBox.Text

        ' Clear focus so your context menu stays usable
        Me.ActiveControl = Nothing
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

    Private Sub PSFunctResultsContextMenu2_Opening(sender As Object, e As System.ComponentModel.CancelEventArgs) Handles PSFunctResultsContextMenu2.Opening

    End Sub

    ' Resume last task using persisted outcomes and step outputs
    Public Async Function ResumeLastTaskAsync() As Task
        Try
            ' Reload persisted data
            GlobalOutcomeTracker.Instance.LoadFromDisk()
            StepOutputManager.Instance.LoadFromDisk()

            ' If no history, nothing to resume
            If conversationHistory Is Nothing OrElse conversationHistory.Count = 0 Then
                AppendResultToBox("No previous task to resume." & Environment.NewLine)
                Return
            End If

            ' Re-run handle request with last user message if available
            Dim lastUser = conversationHistory.LastOrDefault(Function(m) m.ContainsKey("role") AndAlso m("role") = "user")
            If lastUser Is Nothing Then
                AppendResultToBox("No user request found to resume." & Environment.NewLine)
                Return
            End If

            UserInputBox.Text = lastUser("content")
            cancellationTokenSource = New CancellationTokenSource()
            CancelTaskButton.Enabled = True
            Await HandleUserRequestAsync(cancellationTokenSource.Token)
        Catch ex As Exception
            AppendResultToBox($"[Resume Error] {ex.Message}" & Environment.NewLine)
        End Try
    End Function

End Class

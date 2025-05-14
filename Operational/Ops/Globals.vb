' ###  Globals.vb - v1.0.1 ### 

' ##########################################################
'  Shelly App - v1.0.1
'  License: Creative Commons Attribution-NonCommercial (CC BY-NC)
'  https://creativecommons.org/licenses/by-nc/4.0/
'  © 2025 Vlad Stefanescu | GreenCoders.net. Attribution required.
' ##########################################################

Imports NAudio.Wave ' Ensure NAudio is referenced in your project

Public Module Globals

    Public OriginalUserRequest As String = ""
    ' ========================== Conversation & API Keys ==============================
    ' We keep the conversationHistory as before
    Public Const MaxTotalTokens As Integer = 7000 ' Adjust based on your requirements
    Public conversationHistory As New List(Of Dictionary(Of String, String))()
    Public AiModelSelection = My.Settings.AiModelSelection
    ' ------------------------------------------------------------------------------
    '        FILE MANAGEMENT (unchanged)
    ' ------------------------------------------------------------------------------
    Public FileContents As New Dictionary(Of String, String)
    Public LastScreenshotPath As String = ""
    Public LastFileQuery As String = ""
    Public GeneratedImages As New List(Of String)
    Public temperature As Double = 0.7
    Public UserAudioSelection As Integer = My.Settings.SelectedDeviceIndex
    Public lastRunMultiTask As Boolean = False
    ' ------------------------------------------------------------------------------
    '        TASK MANAGEMENT (unchanged)
    ' ------------------------------------------------------------------------------
    Public TaskCompleted As Boolean = True
    Public TaskData As New Dictionary(Of String, Object)

    Public maxTokensPerChunk As Integer = 16000
    Public maxInputTokensPerChunk As Integer = 128000
    Public LastUsedModel As String = ""

    ' ------------------------------------------------------------------------------
    '        HINTS PANEL
    ' ------------------------------------------------------------------------------
    Public CheckBoxHintsState As Boolean = False ' Default to False (unchecked)
    ' ------------------------------------------------------------------------------
    '        ENCRYPTED API KEY PROPERTY
    ' ------------------------------------------------------------------------------
    ' Whenever code reads Globals.UserApiKey, it gets the decrypted string from
    ' SecureStorage. Whenever code sets it, it automatically encrypts and saves it.
    Public Property UserApiKey As String
        Get
            Return SecureStorage.GetApiKey() ' always decrypted
        End Get
        Set(value As String)
            SecureStorage.SaveApiKey(value)  ' encrypt & save
        End Set
    End Property


    Public isSpeechActive As Boolean = False

    ' ------------------------------------------------------------------------------
    '        REPHRASING OR NOT
    ' ------------------------------------------------------------------------------
    Public UsePromptRevision As Boolean = False ' default is unchecked

    ' ------------------------------------------------------------------------------
    '        AI SETTINGS
    ' ------------------------------------------------------------------------------
    Public Property AssistantId As String
        Get
            Return My.Settings.assistantId
        End Get
        Set(value As String)
            My.Settings.assistantId = value
            My.Settings.Save()
        End Set
    End Property

    ' ------------------------------------------------------------------------------
    '        AUDIO DEVICE MANAGEMENT
    ' ------------------------------------------------------------------------------
    Public Function GetAudioDevices() As List(Of String)
        Dim devices As New List(Of String)
        Try
            For index As Integer = 0 To WaveInEvent.DeviceCount - 1
                Dim deviceInfo As WaveInCapabilities = WaveInEvent.GetCapabilities(index)
                devices.Add($"{index}: {deviceInfo.ProductName}")
            Next
        Catch ex As Exception
            Debug.WriteLine($"[ERROR] Audio device retrieval failed: {ex.Message}")
        End Try

        If devices.Count = 0 Then
            devices.Add("No audio devices found.")
        End If
        Return devices
    End Function

    ' Sets the default audio device by picking the first available device (index 0).
    Public Sub SetDefaultAudioDevice()
        Dim devices = GetAudioDevices()
        If devices.Count > 0 AndAlso Not devices.Contains("No audio devices found.") Then
            SaveAudioSelection(0)
        Else
            Debug.WriteLine("[WARNING] No default audio device available.")
        End If
    End Sub

    ' Saves the selected audio device index to My.Settings
    Public Sub SaveAudioSelection(audioIndex As Integer)
        UserAudioSelection = audioIndex
        My.Settings.SelectedDeviceIndex = audioIndex
        My.Settings.Save()
    End Sub

    ' ------------------------------------------------------------------------------
    ' Show Debug lines inside Console
    ' ------------------------------------------------------------------------------

    Public ConsoleForm As Console = Nothing
    Public IsLiveLoggingEnabled As Boolean = False

    ' Store debug logs persistently
    Private ReadOnly MaxLogLines As Integer = 2000
    Private DebugLogBuffer As New List(Of String)

    Public Sub AppendDebugLog(message As String)
        ' Add to buffer
        DebugLogBuffer.Add(message)

        ' Trim old logs if needed
        If DebugLogBuffer.Count > MaxLogLines Then
            DebugLogBuffer.RemoveRange(0, DebugLogBuffer.Count - MaxLogLines)
        End If

        ' Update Console in real time
        If IsLiveLoggingEnabled AndAlso ConsoleForm IsNot Nothing AndAlso Not ConsoleForm.IsDisposed Then
            ConsoleForm.AppendDebugMessage(message)
        End If
    End Sub

    Public Function GetStoredLogs() As String
        Return String.Join(Environment.NewLine, DebugLogBuffer)
    End Function


    Public Sub ClearDebugLogs()
        DebugLogBuffer.Clear()
    End Sub


    ' ------------------------------------------------------------------------------
    ' Remember Console User Commands
    ' ------------------------------------------------------------------------------
    Private CommandHistory As New List(Of String)
    Private CommandHistoryIndex As Integer = -1 ' Tracks current position in history

    ' ✅ Add a command to history (max 20)
    Public Sub AddToCommandHistory(command As String)
        If Not String.IsNullOrWhiteSpace(command) Then
            ' Avoid duplicate consecutive commands
            If CommandHistory.Count = 0 OrElse CommandHistory.Last() <> command Then
                CommandHistory.Add(command)
                ' Limit to last 20 commands
                If CommandHistory.Count > 20 Then
                    CommandHistory.RemoveAt(0)
                End If
            End If
        End If
        ' Reset history index
        CommandHistoryIndex = -1
    End Sub

    ' ✅ Get previous command (UP arrow key)
    Public Function GetPreviousCommand() As String
        If CommandHistory.Count > 0 Then
            If CommandHistoryIndex = -1 Then
                CommandHistoryIndex = CommandHistory.Count - 1 ' Start at the most recent
            ElseIf CommandHistoryIndex > 0 Then
                CommandHistoryIndex -= 1 ' Move up in history
            End If
            Return CommandHistory(CommandHistoryIndex)
        End If
        Return ""
    End Function

    ' ✅ Get next command (DOWN arrow key)
    Public Function GetNextCommand() As String
        If CommandHistory.Count > 0 AndAlso CommandHistoryIndex <> -1 Then
            If CommandHistoryIndex < CommandHistory.Count - 1 Then
                CommandHistoryIndex += 1 ' Move down in history
                Return CommandHistory(CommandHistoryIndex)
            Else
                CommandHistoryIndex = -1 ' Reset if at the latest command
            End If
        End If
        Return ""
    End Function

    ' =============== AI CALL COUNT ================

    Public AICallCount As Integer = 0

    Public Sub ResetAICallCount()
        AICallCount = 0
    End Sub

    Public Sub IncrementAICallCount()
        AICallCount += 1
    End Sub


End Module

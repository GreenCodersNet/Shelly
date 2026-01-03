Imports NAudio.Wave ' Ensure NAudio is referenced in your project
Imports System.Collections.Specialized

Public Module Globals

    ' ========================== LOCAL AI PERSISTENCE ==============================
    Public LocalAIModelPaths As New List(Of String)
    Public LocalAIVoices As New List(Of String)
    Public LocalAISelectedModel As String = String.Empty
    Public LocalAISelectedVoice As String = String.Empty
    Public LocalAIUseGpu As Boolean = False  ' False = CPU, True = GPU
    Public LocalAIGpuLayerCount As Integer = -1  ' -1 = all layers, 0 = CPU only, >0 = specific layer count
    Public LocalAIIncludeTTS As Boolean = False  ' Whether to speak responses via Piper TTS
    Public LocalAIUseSummarization As Boolean = False  ' Whether to use LocalAI for text summarization tasks

    ' GPU Detection Cache
    Public LocalAIGpuDetected As Boolean = False
    Public LocalAICudaAvailable As Boolean = False
    Public LocalAIGpuName As String = "Not detected"
    Public LocalAIVramMB As Long = 0
    Public LocalAILastGpuCheckTime As DateTime = DateTime.MinValue

    Public Sub LoadLocalAISettings()
        LocalAIModelPaths = StringCollectionToList(My.Settings.LocalAIModelPaths)
        LocalAIVoices = StringCollectionToList(My.Settings.LocalAIVoices)
        LocalAISelectedModel = My.Settings.LocalAISelectedModel
        LocalAISelectedVoice = My.Settings.LocalAISelectedVoice
        LocalAIUseGpu = My.Settings.LocalAIUseGpu
        LocalAIGpuLayerCount = My.Settings.LocalAIGpuLayerCount
        LocalAIIncludeTTS = My.Settings.LocalAIIncludeTTS
        LocalAIUseSummarization = My.Settings.LocalAIUseSummarization
        
        ' GPU detection is now done ONCE and cached - don't refresh on every settings load
        ' Only refresh if never done before
        If LocalAILastGpuCheckTime = DateTime.MinValue Then
            RefreshGpuDetection()
        End If
    End Sub

    Public Sub SaveLocalAISettings()
        My.Settings.LocalAIModelPaths = ListToStringCollection(LocalAIModelPaths)
        My.Settings.LocalAIVoices = ListToStringCollection(LocalAIVoices)
        My.Settings.LocalAISelectedModel = LocalAISelectedModel
        My.Settings.LocalAISelectedVoice = LocalAISelectedVoice
        My.Settings.LocalAIUseGpu = LocalAIUseGpu
        My.Settings.LocalAIGpuLayerCount = LocalAIGpuLayerCount
        My.Settings.LocalAIIncludeTTS = LocalAIIncludeTTS
        My.Settings.LocalAIUseSummarization = LocalAIUseSummarization
        My.Settings.Save()
    End Sub

    ''' <summary>
    ''' Refreshes GPU detection cache from GpuDetection helper.
    ''' </summary>
    Public Sub RefreshGpuDetection()
        Try
            GpuDetection.EnsureDetectionPerformed()
            LocalAIGpuDetected = GpuDetection.IsNvidiaGpuPresent
            LocalAICudaAvailable = GpuDetection.IsCudaAvailable
            LocalAIGpuName = GpuDetection.GpuName
            LocalAIVramMB = GpuDetection.VramMB
            LocalAILastGpuCheckTime = DateTime.Now
            
            Debug.WriteLine($"[Globals] GPU Detection refreshed: GPU={LocalAIGpuDetected}, CUDA={LocalAICudaAvailable}, Name={LocalAIGpuName}, VRAM={LocalAIVramMB}MB")
        Catch ex As Exception
            Debug.WriteLine($"[Globals] GPU Detection failed: {ex.Message}")
            LocalAIGpuDetected = False
            LocalAICudaAvailable = False
        End Try
    End Sub

    Private Function StringCollectionToList(sc As StringCollection) As List(Of String)
        If sc Is Nothing Then Return New List(Of String)
        Return sc.Cast(Of String)().Where(Function(s) Not String.IsNullOrWhiteSpace(s)).Distinct().ToList()
    End Function

    Private Function ListToStringCollection(list As List(Of String)) As StringCollection
        Dim sc As New StringCollection()
        If list IsNot Nothing Then
            For Each item In list
                If Not String.IsNullOrWhiteSpace(item) Then sc.Add(item)
            Next
        End If
        Return sc
    End Function

    Public OriginalUserRequest As String = ""

    ' ========================== CONTEXT-AWARE ITERATION (NEW) ==============================
    ''' <summary>
    ''' Unique identifier for the current user request session
    ''' Used to track step outputs across iterations
    ''' </summary>
    Public CurrentRequestId As String = ""

    ''' <summary>
    ''' Current iteration number in the intelligent retry loop
    ''' Starts at 0 for each new request, increments with each planning cycle
    ''' </summary>
    Public CurrentIteration As Integer = 0

    ' ========================== NATURAL RESPONSE SYSTEM ==============================
    ''' <summary>
    ''' Feature flag for natural AI assistant responses
    ''' When True: Uses FinalResponseGenerator for conversational summaries
    ''' When False: Falls back to legacy technical logs display
    ''' </summary>
    Public UseNaturalResponses As Boolean = False  ' DISABLED - causing duplicate responses

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

    ' Token limits - dynamically adjusted based on model selection
    Public maxTokensPerChunk As Integer = 32000        ' Increased for GPT-5 series
    Public maxInputTokensPerChunk As Integer = 256000  ' Increased for GPT-5 series (256K context)
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

    Public Sub LoadPowerShellSecuritySettings()
        With My.Settings
            SecurityFlags.ConstrainedLanguageMode = .PowerShell_UseConstrainedMode
            SecurityFlags.BlockNetworkCalls = .PowerShell_BlockNetworkCalls
            SecurityFlags.BlockEnvVariableAccess = .PowerShell_BlockEnvVariables
            SecurityFlags.BlockBackgroundJobs = .PowerShell_BlockBackgroundJobs
            SecurityFlags.BlockSystemC = .PowerShell_BlockSystemC
            ' TODO: Add .PowerShell_BlockStartProcess to My.Settings, then uncomment:
            ' SecurityFlags.BlockStartProcess = .PowerShell_BlockStartProcess
        End With
    End Sub

    Public Sub SavePowerShellSecuritySettings()
        With My.Settings
            .PowerShell_UseConstrainedMode = SecurityFlags.ConstrainedLanguageMode
            .PowerShell_BlockNetworkCalls = SecurityFlags.BlockNetworkCalls
            .PowerShell_BlockEnvVariables = SecurityFlags.BlockEnvVariableAccess
            .PowerShell_BlockBackgroundJobs = SecurityFlags.BlockBackgroundJobs
            .PowerShell_BlockSystemC = SecurityFlags.BlockSystemC
            ' TODO: Add .PowerShell_BlockStartProcess to My.Settings, then uncomment:
            ' .PowerShell_BlockStartProcess = SecurityFlags.BlockStartProcess
            .Save()
        End With
    End Sub

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

    ' ========================== LOCAL AI CONVENIENCE HELPERS ==============================
    ''' <summary>
    ''' Quick check if Local AI is configured (model path set and file exists).
    ''' Does NOT load the model - just checks settings.
    ''' </summary>
    Public Function IsLocalAIConfigured() As Boolean
        LoadLocalAISettings()
        Return Not String.IsNullOrWhiteSpace(LocalAISelectedModel) AndAlso
               System.IO.File.Exists(LocalAISelectedModel)
    End Function

    ''' <summary>
    ''' Quick check if Piper TTS voice is configured.
    ''' </summary>
    Public Function IsPiperVoiceConfigured() As Boolean
        LoadLocalAISettings()
        Return Not String.IsNullOrWhiteSpace(LocalAISelectedVoice) AndAlso
               System.IO.File.Exists(LocalAISelectedVoice)
    End Function

    ''' <summary>
    ''' Checks if LocalAI summarization mode is enabled AND the engine is ready.
    ''' Use this before routing summarization tasks to LocalAI.
    ''' </summary>
    Public Function IsLocalAISummarizationEnabled() As Boolean
        LoadLocalAISettings()
        If Not LocalAIUseSummarization Then Return False
        Return IsSharedLocalAIReady()
    End Function

    ' ========================== SHARED LOCAL AI ENGINE ==============================
    ''' <summary>
    ''' Shared LocalAI engine instance - can be set by LocalAIForm when it loads the model.
    ''' </summary>
    Public SharedLocalAIEngine As LocalAIEngine = Nothing
    Public SharedTTSEngine As PiperTTSEngine = Nothing

    ''' <summary>
    ''' Checks if the shared LocalAI engine is ready (model already loaded).
    ''' Does NOT attempt to load - just checks if ready.
    ''' </summary>
    Public Function IsSharedLocalAIReady() As Boolean
        Return SharedLocalAIEngine IsNot Nothing AndAlso SharedLocalAIEngine.IsModelLoaded
    End Function

    ''' <summary>
    ''' Checks if the shared TTS engine is ready.
    ''' </summary>
    Public Function IsSharedTTSReady() As Boolean
        Return SharedTTSEngine IsNot Nothing AndAlso SharedTTSEngine.IsReady
    End Function

End Module

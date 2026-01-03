' ###  LocalAISummaryService.vb - v2.2.0 ###

' ##########################################################
'  Shelly - Local AI Summary Service
'  License: Creative Commons Attribution-NonCommercial (CC BY-NC)
'  https://creativecommons.org/licenses/by-nc/4.0/
'  © 2025 Vlad Stefanescu | GreenCoders.net. Attribution required.
' ##########################################################

' PURPOSE: Centralized service for Local AI text generation and TTS
' USAGE: ONLY used for final voice summary after ALL steps complete
' OPTIMIZATION: Model is preloaded at app startup for instant inference
' GPU SUPPORT: Automatic fallback to CPU if GPU fails

Imports System.IO
Imports System.Threading
Imports System.Text

''' <summary>
''' Centralized service for Local AI final summaries and TTS.
''' ONLY used after ALL task steps are complete to provide voice narration.
''' OPTIMIZATION: Call PreloadModelAsync() at app startup for instant responses.
''' GPU SUPPORT: Automatically falls back to CPU if GPU is unavailable or fails.
''' </summary>
Public NotInheritable Class LocalAISummaryService
    Implements IDisposable

    ' Singleton instance
    Private Shared ReadOnly _lazyInstance As New Lazy(Of LocalAISummaryService)(
        Function() New LocalAISummaryService(),
        LazyThreadSafetyMode.ExecutionAndPublication)

    Public Shared ReadOnly Property Instance As LocalAISummaryService
        Get
            Return _lazyInstance.Value
        End Get
    End Property

    ' Internal engines
    Private ReadOnly _engine As New LocalAIEngine()
    Private ReadOnly _tts As New PiperTTSEngine()
    Private ReadOnly _gate As New SemaphoreSlim(1, 1)
    Private _loadedModelPath As String = Nothing
    Private _isDisposed As Boolean = False
    Private _isPreloaded As Boolean = False
    Private _cachedTrainingInstructions As String = Nothing
    Private _lastLoadUsedGpu As Boolean = False

    ' Configuration
    Private Const DefaultMaxTokens As Integer = 256
    Private Const TrainingFileName As String = "localai_training.txt"

    Private Sub New()
        ' Private constructor for singleton
    End Sub

#Region "Preload - Call at App Startup"

    ''' <summary>
    ''' CALL THIS AT APP STARTUP: Preloads the LocalAI model and initializes TTS.
    ''' This ensures instant responses when GenerateFinalVoiceSummaryAsync is called.
    ''' Uses settings from LocalAIForm (model, GPU/CPU, voice) via Globals.
    ''' </summary>
    ''' <param name="progress">Optional progress callback for UI feedback</param>
    ''' <returns>True if preload succeeded, False otherwise</returns>
    Public Async Function PreloadModelAsync(Optional progress As IProgress(Of String) = Nothing) As Task(Of Boolean)
        If _isPreloaded AndAlso _engine.IsModelLoaded Then
            Debug.WriteLine("[LocalAISummaryService] Already preloaded - skipping")
            Return True
        End If

        ' Load latest settings from LocalAIForm
        Globals.LoadLocalAISettings()

        ' Check if LocalAI is configured
        If String.IsNullOrWhiteSpace(Globals.LocalAISelectedModel) Then
            Debug.WriteLine("[LocalAISummaryService] Preload skipped: no model configured")
            Return False
        End If

        If Not File.Exists(Globals.LocalAISelectedModel) Then
            Debug.WriteLine("[LocalAISummaryService] Preload skipped: model file not found")
            Return False
        End If

        Try
            ' Use GPU setting from LocalAIForm via Globals
            Dim attemptGpu = Globals.LocalAIUseGpu AndAlso Globals.LocalAICudaAvailable
            Dim modeText As String = If(attemptGpu, "GPU", "CPU")
            
            progress?.Report($"Loading Local AI model ({modeText})...")
            Debug.WriteLine($"[LocalAISummaryService] Preloading model: {Globals.LocalAISelectedModel}")
            Debug.WriteLine($"[LocalAISummaryService] GPU requested: {Globals.LocalAIUseGpu}, CUDA available: {Globals.LocalAICudaAvailable}")

            ' Use bulletproof load with automatic fallback
            Dim loaded = Await _engine.LoadModelAsync(
                Globals.LocalAISelectedModel, 
                progress, 
                attemptGpu,
                Globals.LocalAIGpuLayerCount
            ).ConfigureAwait(False)
            
            If loaded Then
                _loadedModelPath = Globals.LocalAISelectedModel
                _isPreloaded = True
                _lastLoadUsedGpu = _engine.IsUsingGpu
                
                Dim actualMode = If(_engine.IsUsingGpu, "GPU", "CPU")
                If _engine.LastLoadWasFallback Then
                    Debug.WriteLine($"[LocalAISummaryService] Model preloaded on {actualMode} (fallback from GPU)")
                Else
                    Debug.WriteLine($"[LocalAISummaryService] Model preloaded successfully on {actualMode}!")
                End If

                ' Also preload training instructions
                _cachedTrainingInstructions = LoadTrainingInstructionsFromFile()

                ' Initialize TTS only if Include TTS is enabled
                If Globals.LocalAIIncludeTTS Then
                    progress?.Report("Initializing voice...")
                    If Not _tts.IsReady Then
                        _tts.Initialize()
                        If _tts.IsReady AndAlso Not String.IsNullOrWhiteSpace(Globals.LocalAISelectedVoice) Then
                            _tts.SetVoice(Globals.LocalAISelectedVoice)
                        End If
                    End If
                Else
                    Debug.WriteLine("[LocalAISummaryService] TTS initialization skipped (Include TTS is disabled)")
                End If

                progress?.Report($"Local AI ready ({actualMode})!")
                Return True
            Else
                Debug.WriteLine("[LocalAISummaryService] Model preload failed")
                Return False
            End If

        Catch ex As Exception
            Debug.WriteLine($"[LocalAISummaryService] Preload error: {ex.Message}")
            Return False
        End Try
    End Function

    ''' <summary>
    ''' Checks if the model is preloaded and ready for instant inference.
    ''' </summary>
    Public ReadOnly Property IsPreloaded As Boolean
        Get
            Return _isPreloaded AndAlso _engine.IsModelLoaded
        End Get
    End Property

    ''' <summary>
    ''' Gets whether the preloaded model is using GPU acceleration.
    ''' </summary>
    Public ReadOnly Property IsUsingGpu As Boolean
        Get
            Return _engine.IsUsingGpu
        End Get
    End Property

#End Region

#Region "Availability Checks"

    ''' <summary>
    ''' Checks if Local AI is configured and available for use.
    ''' Does NOT load the model - just checks configuration.
    ''' </summary>
    Public Function IsLocalAIAvailable() As Boolean
        EnsureSettingsLoaded()
        If String.IsNullOrWhiteSpace(Globals.LocalAISelectedModel) Then Return False
        Return File.Exists(Globals.LocalAISelectedModel)
    End Function

    ''' <summary>
    ''' Checks if Piper TTS is configured and available.
    ''' </summary>
    Public Function IsTTSAvailable() As Boolean
        If Not _tts.IsReady Then
            _tts.Initialize()
        End If
        Return _tts.IsReady
    End Function

    ''' <summary>
    ''' Checks if the Local AI model is currently loaded and ready.
    ''' </summary>
    Public ReadOnly Property IsModelLoaded As Boolean
        Get
            Return _engine.IsModelLoaded
        End Get
    End Property

    ''' <summary>
    ''' Checks if GPU acceleration is available on this system.
    ''' </summary>
    Public Shared Function IsGpuAvailable() As Boolean
        GpuDetection.EnsureDetectionPerformed()
        Return GpuDetection.IsCudaAvailable
    End Function

    ''' <summary>
    ''' Gets GPU status information for display.
    ''' </summary>
    Public Shared Function GetGpuStatusMessage() As String
        Return GpuDetection.StatusMessage
    End Function

#End Region

#Region "Final Voice Summary - PRIMARY ENTRY POINT"

    ''' <summary>
    ''' PRIMARY METHOD: Generates and speaks a final voice summary after ALL steps complete.
    ''' This is the ONLY LocalAI+Piper entry point - called once per task completion.
    ''' OPTIMIZED: If model was preloaded, this runs in ~2 seconds instead of ~30 seconds.
    ''' RESPECTS: Globals.LocalAIIncludeTTS setting from LocalAIForm.
    ''' </summary>
    Public Async Function GenerateFinalVoiceSummaryAsync(
        completedStepDescriptions As List(Of String),
        Optional ct As CancellationToken = Nothing
    ) As Task(Of String)

        ' Quick availability check
        If Not IsLocalAIAvailable() Then
            Debug.WriteLine("[LocalAISummaryService] Final voice summary skipped: LocalAI not available")
            Return String.Empty
        End If

        If completedStepDescriptions Is Nothing OrElse completedStepDescriptions.Count = 0 Then
            Debug.WriteLine("[LocalAISummaryService] Final voice summary skipped: no steps to summarize")
            Return String.Empty
        End If

        ' Use short timeout for gate to prevent blocking
        Dim acquired = Await _gate.WaitAsync(TimeSpan.FromSeconds(5), ct).ConfigureAwait(False)
        If Not acquired Then
            Debug.WriteLine("[LocalAISummaryService] Final voice summary skipped: gate timeout (another call in progress)")
            Return String.Empty
        End If

        Try
            Dim sw = Diagnostics.Stopwatch.StartNew()

            ' Load model only if not preloaded (this is the slow path)
            If Not _engine.IsModelLoaded Then
                Debug.WriteLine("[LocalAISummaryService] Model not preloaded - loading now (slow path)...")
                Dim loadResult = Await EnsureModelLoadedAsync(ct).ConfigureAwait(False)
                If Not String.IsNullOrEmpty(loadResult) Then
                    Debug.WriteLine($"[LocalAISummaryService] Model load failed: {loadResult}")
                    Return String.Empty
                End If
                Debug.WriteLine($"[LocalAISummaryService] Model loaded in {sw.ElapsedMilliseconds}ms")
            Else
                Debug.WriteLine("[LocalAISummaryService] Using preloaded model (fast path)")
            End If

            ' Build the prompt for final summary
            Dim prompt = BuildFinalSummaryPrompt(completedStepDescriptions)
            Dim systemPrompt = If(_cachedTrainingInstructions, LoadTrainingInstructionsFromFile())

            ' Generate the summary
            Dim genSw = Diagnostics.Stopwatch.StartNew()
            Dim response = Await _engine.GenerateResponseAsync(
                prompt:=prompt,
                systemPrompt:=systemPrompt,
                ct:=ct,
                maxTokens:=DefaultMaxTokens
            ).ConfigureAwait(False)
            Debug.WriteLine($"[LocalAISummaryService] Generation took {genSw.ElapsedMilliseconds}ms")

            ' Check for errors
            If String.IsNullOrWhiteSpace(response) OrElse response.StartsWith("[ERROR]") OrElse response.StartsWith("[Cancelled]") Then
                Debug.WriteLine($"[LocalAISummaryService] Generation failed: {response}")
                Return String.Empty
            End If

            Debug.WriteLine($"[LocalAISummaryService] Generated: {response}")

            ' RESPECT GLOBALS.LOCALAIINCLUDETTS - Only speak if TTS is enabled
            Globals.LoadLocalAISettings()
            If Globals.LocalAIIncludeTTS Then
                Dim ttsSw = Diagnostics.Stopwatch.StartNew()
                Await SpeakAsync(response, ct).ConfigureAwait(False)
                Debug.WriteLine($"[LocalAISummaryService] TTS took {ttsSw.ElapsedMilliseconds}ms")
            Else
                Debug.WriteLine("[LocalAISummaryService] TTS skipped (Include TTS is disabled)")
            End If

            Debug.WriteLine($"[LocalAISummaryService] Total time: {sw.ElapsedMilliseconds}ms")
            Return response

        Catch ex As OperationCanceledException
            Debug.WriteLine("[LocalAISummaryService] Final summary cancelled")
            Return String.Empty
        Catch ex As Exception
            Debug.WriteLine($"[LocalAISummaryService] Final summary error: {ex.Message}")
            Return String.Empty
        Finally
            _gate.Release()
        End Try
    End Function

    ''' <summary>
    ''' Builds the prompt for final summary based on completed steps.
    ''' </summary>
    Private Function BuildFinalSummaryPrompt(stepDescriptions As List(Of String)) As String
        Dim sb As New StringBuilder()

        If stepDescriptions.Count = 1 Then
            sb.AppendLine("The user requested one task. Here is what was completed:")
            sb.AppendLine($"- {stepDescriptions(0)}")
            sb.AppendLine()
            sb.AppendLine("Provide a brief 1-2 sentence spoken confirmation of what YOU accomplished.")
        Else
            sb.AppendLine($"The user requested multiple tasks. Here are the {stepDescriptions.Count} completed actions:")
            For Each desc In stepDescriptions
                sb.AppendLine($"- {desc}")
            Next
            sb.AppendLine()
            sb.AppendLine("Provide a brief 2-4 sentence spoken summary of what YOU accomplished. List the actions naturally.")
        End If

        Return sb.ToString()
    End Function

#End Region

#Region "Text-to-Speech"

    ''' <summary>
    ''' Speaks the given text using Piper TTS.
    ''' </summary>
    Private Async Function SpeakAsync(
        text As String,
        Optional ct As CancellationToken = Nothing
    ) As Task(Of Boolean)

        If String.IsNullOrWhiteSpace(text) Then Return False

        Try
            ' Initialize TTS if needed (should already be done during preload)
            If Not _tts.IsReady Then
                _tts.Initialize()
                If Not _tts.IsReady Then
                    Debug.WriteLine("[LocalAISummaryService] TTS not ready")
                    Return False
                End If
            End If

            ' Set voice if configured (should already be set during preload)
            Dim voice = Globals.LocalAISelectedVoice
            If Not String.IsNullOrWhiteSpace(voice) Then
                _tts.SetVoice(voice)
            End If

            Return Await _tts.SpeakAsync(text, ct).ConfigureAwait(False)

        Catch ex As OperationCanceledException
            Return False
        Catch ex As Exception
            Debug.WriteLine($"[LocalAISummaryService] TTS error: {ex.Message}")
            Return False
        End Try
    End Function

    ''' <summary>
    ''' Stops any currently playing TTS audio.
    ''' </summary>
    Public Sub StopSpeaking()
        _tts.StopSpeaking()
    End Sub

#End Region

#Region "Private Helpers"

    Private Async Function EnsureModelLoadedAsync(ct As CancellationToken) As Task(Of String)
        EnsureSettingsLoaded()

        Dim modelPath = Globals.LocalAISelectedModel
        If String.IsNullOrWhiteSpace(modelPath) Then
            Return "[ERROR] No local AI model selected."
        End If

        If Not File.Exists(modelPath) Then
            Return "[ERROR] Selected model file not found."
        End If

        ' Load model if needed or if different model selected
        If Not _engine.IsModelLoaded OrElse Not String.Equals(_loadedModelPath, modelPath, StringComparison.OrdinalIgnoreCase) Then
            Dim loaded = Await _engine.LoadModelAsync(modelPath, Nothing, Globals.LocalAIUseGpu).ConfigureAwait(False)
            If Not loaded Then
                Return "[ERROR] Failed to load local AI model."
            End If
            _loadedModelPath = modelPath
        End If

        Return Nothing
    End Function

    Private Sub EnsureSettingsLoaded()
        Globals.LoadLocalAISettings()
    End Sub

    Private Function LoadTrainingInstructionsFromFile() As String
        Try
            Dim basePath = AppDomain.CurrentDomain.BaseDirectory
            Dim trainingPath = Path.Combine(basePath, "LocalAI", "Resources", TrainingFileName)
            If File.Exists(trainingPath) Then
                Return File.ReadAllText(trainingPath)
            End If
        Catch ex As Exception
            Debug.WriteLine($"[LocalAISummaryService] Training load error: {ex.Message}")
        End Try

        Return "You are a digital assistant. Summarize completed tasks succinctly for speech. " &
               "Speak as if YOU completed the actions. Avoid URLs, file paths, and technical jargon."
    End Function

#End Region

#Region "Helper: Build Step Description from Tool Name"

    ''' <summary>
    ''' Converts a tool name and optional context into a human-readable description.
    ''' </summary>
    Public Shared Function GetStepDescription(toolName As String, Optional context As String = Nothing) As String
        Select Case toolName.ToLowerInvariant()
            Case "freeresponse"
                Return If(String.IsNullOrWhiteSpace(context), "Provided a response", $"Responded about {TruncateContext(context, 30)}")
            Case "executepowershellscript"
                Return If(String.IsNullOrWhiteSpace(context), "Ran a system command", $"Retrieved {TruncateContext(context, 30)}")
            Case "generateimages"
                Return "Generated images"
            Case "readfileandanswer"
                Return "Read and analyzed a file"
            Case "imageanswer"
                Return "Analyzed an image"
            Case "searchfortextinsidefiles"
                Return "Searched for text in files"
            Case "websearchandrespondbasedonpagecontent"
                Return "Searched the web"
            Case "changeorsetvolume"
                Return "Adjusted the volume"
            Case "sendmediakey"
                Return "Sent a media command"
            Case "takeprintscreenorscreenshot"
                Return "Took a screenshot"
            Case "startorunapplicationbyname"
                Return "Opened an application"
            Case "generatelargefile", "generatelargefilewith", "generatelargefilewithtextorcode"
                Return "Generated a document"
            Case "generatebatchandps1file"
                Return "Created script files"
            Case Else
                Return $"Completed {toolName}"
        End Select
    End Function

    Private Shared Function TruncateContext(text As String, maxLen As Integer) As String
        If String.IsNullOrWhiteSpace(text) Then Return ""
        If text.Length <= maxLen Then Return text
        Return text.Substring(0, maxLen) & "..."
    End Function

#End Region

#Region "IDisposable"

    Protected Sub Dispose(disposing As Boolean)
        If Not _isDisposed Then
            If disposing Then
                _tts?.Dispose()
                _engine?.Dispose()
                _gate?.Dispose()
            End If
            _isDisposed = True
        End If
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
        Dispose(True)
        GC.SuppressFinalize(Me)
    End Sub

#End Region

End Class

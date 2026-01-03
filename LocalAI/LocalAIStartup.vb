' ###  LocalAIStartup.vb - v1.1.0 ###

' ##########################################################
'  Shelly - Local AI Startup Helper
'  License: Creative Commons Attribution-NonCommercial (CC BY-NC)
'  https://creativecommons.org/licenses/by-nc/4.0/
'  © 2025 Vlad Stefanescu | GreenCoders.net. Attribution required.
' ##########################################################

' PURPOSE: Preloads LocalAI model at app startup for instant voice summaries
' USAGE: Call LocalAIStartup.PreloadInBackground() in Shelly constructor
' This module creates shared engines that HandleUserRequest can use

Imports System.IO

''' <summary>
''' Helper module to preload LocalAI model at application startup.
''' Creates shared engine instances that are accessible via Globals.
''' </summary>
Public Module LocalAIStartup

    Private _preloadStarted As Boolean = False
    Private _preloadEngine As LocalAIEngine = Nothing
    Private _preloadTTS As PiperTTSEngine = Nothing

    ''' <summary>
    ''' Preloads the LocalAI model in a background task.
    ''' Call this from Shelly constructor AFTER the UI is visible.
    ''' Does not block the UI - the model will be ready for instant responses.
    ''' </summary>
    Public Sub PreloadInBackground()
        ' Prevent duplicate preloads
        If _preloadStarted Then
            System.Diagnostics.Trace.WriteLine("[LocalAIStartup] Preload already started - skipping")
            Return
        End If
        _preloadStarted = True

        Task.Run(Async Function()
                     Try
                         Globals.LoadLocalAISettings()
                         
                         System.Diagnostics.Trace.WriteLine("[LocalAIStartup] === Starting Background Preload ===")
                         System.Diagnostics.Trace.WriteLine($"[LocalAIStartup] LocalAISelectedModel: {Globals.LocalAISelectedModel}")
                         System.Diagnostics.Trace.WriteLine($"[LocalAIStartup] LocalAIIncludeTTS: {Globals.LocalAIIncludeTTS}")
                         
                         ' Check if model is configured
                         If String.IsNullOrWhiteSpace(Globals.LocalAISelectedModel) Then
                             System.Diagnostics.Trace.WriteLine("[LocalAIStartup] No model configured - skipping preload")
                             Return
                         End If
                         
                         If Not File.Exists(Globals.LocalAISelectedModel) Then
                             System.Diagnostics.Trace.WriteLine($"[LocalAIStartup] Model file not found: {Globals.LocalAISelectedModel}")
                             Return
                         End If
                         
                         Dim sw = Diagnostics.Stopwatch.StartNew()
                         
                         ' Create and load the LocalAI engine
                         System.Diagnostics.Trace.WriteLine("[LocalAIStartup] Creating LocalAI engine...")
                         _preloadEngine = New LocalAIEngine()
                         
                         Dim success = Await _preloadEngine.LoadModelAsync(
                             Globals.LocalAISelectedModel,
                             Nothing,
                             Globals.LocalAIUseGpu AndAlso Globals.LocalAICudaAvailable,
                             Globals.LocalAIGpuLayerCount
                         )
                         
                         If success Then
                             ' Share the engine with Globals so HandleUserRequest can use it
                             Globals.SharedLocalAIEngine = _preloadEngine
                             System.Diagnostics.Trace.WriteLine($"[LocalAIStartup] LocalAI engine loaded and shared in {sw.ElapsedMilliseconds}ms")
                         Else
                             System.Diagnostics.Trace.WriteLine("[LocalAIStartup] Failed to load LocalAI model")
                             _preloadEngine?.Dispose()
                             _preloadEngine = Nothing
                         End If
                         
                         ' Initialize TTS engine
                         System.Diagnostics.Trace.WriteLine("[LocalAIStartup] Initializing TTS engine...")
                         _preloadTTS = New PiperTTSEngine()
                         Dim ttsInitialized = _preloadTTS.Initialize()
                         
                         If ttsInitialized Then
                             ' Set voice if configured
                             If Not String.IsNullOrWhiteSpace(Globals.LocalAISelectedVoice) Then
                                 _preloadTTS.SetVoice(Globals.LocalAISelectedVoice)
                             End If
                             
                             ' Share TTS engine with Globals
                             Globals.SharedTTSEngine = _preloadTTS
                             System.Diagnostics.Trace.WriteLine("[LocalAIStartup] TTS engine initialized and shared")
                         Else
                             System.Diagnostics.Trace.WriteLine("[LocalAIStartup] TTS engine initialization failed (Piper not found?)")
                             _preloadTTS = Nothing
                         End If
                         
                         sw.Stop()
                         System.Diagnostics.Trace.WriteLine($"[LocalAIStartup] === Preload Complete in {sw.ElapsedMilliseconds}ms ===")
                         System.Diagnostics.Trace.WriteLine($"[LocalAIStartup] SharedLocalAIEngine ready: {Globals.IsSharedLocalAIReady()}")
                         System.Diagnostics.Trace.WriteLine($"[LocalAIStartup] SharedTTSEngine ready: {Globals.IsSharedTTSReady()}")
                         
                     Catch ex As Exception
                         System.Diagnostics.Trace.WriteLine($"[LocalAIStartup] Preload error: {ex.Message}")
                         System.Diagnostics.Trace.WriteLine($"[LocalAIStartup] Stack: {ex.StackTrace}")
                     End Try
                 End Function)
    End Sub

End Module

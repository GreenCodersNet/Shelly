'' ###  PiperTTSEngine.vb - v1.0.1 ###

' ##########################################################
'  Shelly - Local AI Module - Piper TTS
'  License: Creative Commons Attribution-NonCommercial (CC BY-NC)
'  https://creativecommons.org/licenses/by-nc/4.0/
'  © 2025 Vlad Stefanescu | GreenCoders.net. Attribution required.
' ##########################################################

' PURPOSE: Wrapper for Piper TTS (piper.exe) to provide local text-to-speech
' USAGE: Convert text to speech using local ONNX voice models
' NOTE: Requires piper.exe and voice model files (.onnx + .onnx.json)

Imports System.IO
Imports System.Threading
Imports NAudio.Wave
Imports NAudio.Wave.SampleProviders

''' <summary>
''' Provides local Text-to-Speech using Piper TTS (piper.exe).
''' High-quality neural TTS with multiple voice options.
''' </summary>
Public Class PiperTTSEngine
    Implements IDisposable

    Private _piperExePath As String
    Private _voiceModelPath As String
    Private _isReady As Boolean = False
    Private _isDisposed As Boolean = False
    Private _currentProcess As Process
    Private _waveOut As WaveOutEvent
    Private _audioFile As AudioFileReader

    ' Default paths (can be overridden)
    Private Const DefaultPiperFolder As String = "Piper"
    Private Const DefaultVoicesFolder As String = "voices"
    Private Const LeadingSilenceMs As Integer = 1400

    ''' <summary>
    ''' Gets whether Piper TTS is ready (piper.exe and voice model found).
    ''' </summary>
    Public ReadOnly Property IsReady As Boolean
        Get
            Return _isReady
        End Get
    End Property

    ''' <summary>
    ''' Gets the currently loaded voice model name.
    ''' </summary>
    Public ReadOnly Property CurrentVoice As String
        Get
            If String.IsNullOrEmpty(_voiceModelPath) Then Return "None"
            Return Path.GetFileNameWithoutExtension(_voiceModelPath)
        End Get
    End Property

    ''' <summary>
    ''' Gets the path to piper.exe.
    ''' </summary>
    Public ReadOnly Property PiperPath As String
        Get
            Return _piperExePath
        End Get
    End Property

    ''' <summary>
    ''' Initializes Piper TTS with automatic path detection.
    ''' Looks for piper.exe in: AppDir\LocalAI\Piper\piper.exe
    ''' </summary>
    Public Function Initialize(Optional piperExePath As String = Nothing, Optional voiceModelPath As String = Nothing) As Boolean
        Try
            ' Find piper.exe
            If String.IsNullOrEmpty(piperExePath) Then
                _piperExePath = FindPiperExe()
            Else
                _piperExePath = piperExePath
            End If

            ' Debug: Log what path was found
            Debug.WriteLine($"[PiperTTS] Looking for piper.exe...")
            Debug.WriteLine($"[PiperTTS] Found: {If(_piperExePath, "NOT FOUND")}")

            If String.IsNullOrEmpty(_piperExePath) OrElse Not File.Exists(_piperExePath) Then
                Debug.WriteLine("[PiperTTS] piper.exe not found!")
                _isReady = False
                Return False
            End If

            ' Find voice model
            If String.IsNullOrEmpty(voiceModelPath) Then
                _voiceModelPath = FindDefaultVoice()
            Else
                _voiceModelPath = voiceModelPath
            End If

            Debug.WriteLine($"[PiperTTS] Voice model: {If(_voiceModelPath, "NOT FOUND")}")

            If String.IsNullOrEmpty(_voiceModelPath) OrElse Not File.Exists(_voiceModelPath) Then
                Debug.WriteLine("[PiperTTS] No voice model found!")
                _isReady = False
                Return False
            End If

            _isReady = True
            Debug.WriteLine("[PiperTTS] Initialized successfully!")
            Return True

        Catch ex As Exception
            Debug.WriteLine($"[PiperTTS] Error: {ex.Message}")
            _isReady = False
            Return False
        End Try
    End Function

    ''' <summary>
    ''' Sets the voice model to use.
    ''' </summary>
    Public Function SetVoice(voiceModelPath As String) As Boolean
        If File.Exists(voiceModelPath) Then
            _voiceModelPath = voiceModelPath
            Return True
        End If
        Return False
    End Function

    ''' <summary>
    ''' Gets available voice models from the voices folder.
    ''' </summary>
    Public Function GetAvailableVoices() As List(Of String)
        Dim voices As New List(Of String)

        Try
            Dim voicesFolder = GetVoicesFolder()
            If Directory.Exists(voicesFolder) Then
                ' Find all .onnx files (voice models)
                Dim onnxFiles = Directory.GetFiles(voicesFolder, "*.onnx", SearchOption.AllDirectories)
                For Each onnxFile In onnxFiles
                    ' Check if corresponding .json config exists
                    If File.Exists(onnxFile & ".json") Then
                        voices.Add(onnxFile)
                    End If
                Next
            End If
        Catch
            ' Ignore errors
        End Try

        Return voices
    End Function

    ''' <summary>
    ''' Speaks text using Piper TTS (async).
    ''' </summary>
    Public Async Function SpeakAsync(text As String, Optional ct As CancellationToken = Nothing) As Task(Of Boolean)
        If Not _isReady Then
            Return False
        End If

        If String.IsNullOrWhiteSpace(text) Then
            Return False
        End If

        Try
            ' Generate audio to temp file
            Dim tempWavFile = Path.Combine(Path.GetTempPath(), $"piper_tts_{Guid.NewGuid():N}.wav")

            Dim success = Await GenerateAudioAsync(text, tempWavFile, ct)

            If success AndAlso File.Exists(tempWavFile) Then
                ' Play the audio using NAudio
                Await PlayAudioAsync(tempWavFile, ct)

                ' Cleanup temp file
                Try
                    File.Delete(tempWavFile)
                Catch
                    ' Ignore cleanup errors
                End Try

                Return True
            End If

            Return False

        Catch ex As OperationCanceledException
            Return False
        Catch ex As Exception
            Return False
        End Try
    End Function

    ''' <summary>
    ''' Generates audio file from text using piper.exe.
    ''' </summary>
    Public Async Function GenerateAudioAsync(text As String, outputWavPath As String, Optional ct As CancellationToken = Nothing) As Task(Of Boolean)
        If Not _isReady Then
            Return False
        End If

        Try
            ' Build piper command
            Dim startInfo As New ProcessStartInfo() With {
                .FileName = _piperExePath,
                .Arguments = $"--model ""{_voiceModelPath}"" --output_file ""{outputWavPath}""",
                .UseShellExecute = False,
                .RedirectStandardInput = True,
                .RedirectStandardOutput = True,
                .RedirectStandardError = True,
                .CreateNoWindow = True,
                .WorkingDirectory = Path.GetDirectoryName(_piperExePath)
            }

            _currentProcess = New Process() With {
                .StartInfo = startInfo,
                .EnableRaisingEvents = True
            }

            _currentProcess.Start()

            ' Write text to stdin and ensure it is flushed
            Await _currentProcess.StandardInput.WriteLineAsync(text)
            Await _currentProcess.StandardInput.FlushAsync()
            _currentProcess.StandardInput.Close()

            ' Wait for completion with cancellation support
            Await Task.Run(
                Sub()
                    _currentProcess.WaitForExit(30000) ' 30 second timeout
                End Sub, ct)

            Dim exitCode = _currentProcess.ExitCode
            _currentProcess.Dispose()
            _currentProcess = Nothing

            Return exitCode = 0 AndAlso File.Exists(outputWavPath)

        Catch ex As OperationCanceledException
            StopCurrentProcess()
            Return False
        Catch ex As Exception
            Return False
        End Try
    End Function

    ''' <summary>
    ''' Plays a WAV audio file using NAudio (better than SoundPlayer).
    ''' </summary>
    Private Async Function PlayAudioAsync(wavFilePath As String, ct As CancellationToken) As Task
        Try
            ' Stop and dispose any previous playback
            StopAudioPlayback()

            _audioFile = New AudioFileReader(wavFilePath)
            _audioFile.Position = 0

            Dim playbackSource As ISampleProvider = _audioFile
            If LeadingSilenceMs > 0 Then
                playbackSource = New OffsetSampleProvider(_audioFile) With {
                    .DelayBy = TimeSpan.FromMilliseconds(LeadingSilenceMs),
                    .LeadOut = TimeSpan.Zero
                }
            End If

            _waveOut = New WaveOutEvent() With {.DesiredLatency = 280}
            _waveOut.Init(playbackSource)

            ' Create completion source to wait for playback to finish
            Dim playbackComplete As New TaskCompletionSource(Of Boolean)(TaskCreationOptions.RunContinuationsAsynchronously)

            AddHandler _waveOut.PlaybackStopped,
                Sub(sender, e)
                    playbackComplete.TrySetResult(True)
                End Sub

            ' Start playback
            _waveOut.Play()

            ' Wait for playback to complete or cancellation
            Using ctRegistration = ct.Register(Sub() _waveOut?.Stop())
                Await playbackComplete.Task
            End Using

        Catch ex As OperationCanceledException
            StopAudioPlayback()
        Catch
            ' Ignore playback errors
        Finally
            StopAudioPlayback()
        End Try
    End Function

    ''' <summary>
    ''' Stops audio playback and disposes resources.
    ''' </summary>
    Private Sub StopAudioPlayback()
        Try
            _waveOut?.Stop()
            _waveOut?.Dispose()
            _waveOut = Nothing

            _audioFile?.Dispose()
            _audioFile = Nothing
        Catch
            ' Ignore
        End Try
    End Sub

    ''' <summary>
    ''' Stops any current TTS operation.
    ''' </summary>
    Public Sub StopSpeaking()
        StopCurrentProcess()
        StopAudioPlayback()
    End Sub

    Private Sub StopCurrentProcess()
        Try
            If _currentProcess IsNot Nothing AndAlso Not _currentProcess.HasExited Then
                _currentProcess.Kill()
                _currentProcess.Dispose()
                _currentProcess = Nothing
            End If
        Catch
            ' Ignore
        End Try
    End Sub

    ''' <summary>
    ''' Finds piper.exe in standard locations.
    ''' </summary>
    Private Function FindPiperExe() As String
        Dim appDir = AppDomain.CurrentDomain.BaseDirectory

        ' Search paths in priority order (project folder first)
        Dim searchPaths As String() = {
            Path.Combine(appDir, "LocalAI", "Piper", "piper.exe"),
            Path.Combine(appDir, "Piper", "piper.exe"),
            Path.Combine(appDir, "piper.exe")
        }

        For Each searchPath In searchPaths
            If File.Exists(searchPath) Then
                Return searchPath
            End If
        Next

        Return Nothing
    End Function

    ''' <summary>
    ''' Gets the voices folder path.
    ''' </summary>
    Private Function GetVoicesFolder() As String
        If Not String.IsNullOrEmpty(_piperExePath) Then
            Dim piperDir = Path.GetDirectoryName(_piperExePath)

            ' Check for voices in espeak-ng-data folder first (your structure)
            Dim espeakVoicesPath = Path.Combine(piperDir, "espeak-ng-data", "voices")
            If Directory.Exists(espeakVoicesPath) Then
                Return espeakVoicesPath
            End If

            ' Also check espeak-ng-data root for .onnx files
            Dim espeakDataPath = Path.Combine(piperDir, "espeak-ng-data")
            If Directory.Exists(espeakDataPath) Then
                Return espeakDataPath
            End If

            ' Fallback: voices folder next to piper.exe
            Return Path.Combine(piperDir, DefaultVoicesFolder)
        End If

        ' Default fallback
        Dim appDir = AppDomain.CurrentDomain.BaseDirectory
        Return Path.Combine(appDir, "LocalAI", "Piper", "espeak-ng-data")
    End Function

    ''' <summary>
    ''' Finds the first available voice model.
    ''' </summary>
    Private Function FindDefaultVoice() As String
        Dim voices = GetAvailableVoices()
        If voices.Count > 0 Then
            Return voices(0)
        End If
        Return Nothing
    End Function

    ''' <summary>
    ''' Gets a friendly display name for a voice model path.
    ''' </summary>
    Public Shared Function GetVoiceDisplayName(voicePath As String) As String
        If String.IsNullOrEmpty(voicePath) Then Return "Unknown"
        Dim fileName = Path.GetFileNameWithoutExtension(voicePath)
        Return fileName
    End Function

#Region "IDisposable Support"

    Protected Overridable Sub Dispose(disposing As Boolean)
        If Not _isDisposed Then
            If disposing Then
                StopSpeaking()
                StopAudioPlayback()
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

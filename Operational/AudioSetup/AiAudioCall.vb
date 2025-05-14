' ###  AiAudioCall.vb | v1.0.1 ### 

' ##########################################################
'  Shelly App - v1.0.1
'  License: Creative Commons Attribution-NonCommercial (CC BY-NC)
'  https://creativecommons.org/licenses/by-nc/4.0/
'  © 2025 Vlad Stefanescu | GreenCoders.net. Attribution required.
' ##########################################################

Imports System.IO
Imports System.Net.Http
Imports System.Net.Http.Headers
Imports System.Text
Imports System.Threading.Tasks
Imports Newtonsoft.Json
Imports NAudio.Wave
Imports System.Threading

Module AiAudioCall
    ' Define variables to manage recording and transcription
    Private waveIn As WaveInEvent
    Private writer As WaveFileWriter
    Private tempWavPath As String = Path.Combine(Path.GetTempPath(), "temp_audio.wav")
    Private ReadOnly apiKeyV As String = UserApiKey ' Replace with your OpenAI API key

    ' Parameters for silence detection
    Private silenceThreshold As Double = 0.01 ' Adjust this threshold based on testing
    Private silenceDuration As Integer = 2000 ' Duration in milliseconds to consider as silence

    Public Async Function StartSpeechToText(deviceNumber As Integer) As Task(Of String)

        If File.Exists(tempWavPath) Then File.Delete(tempWavPath)
        Dim transcription As String = String.Empty
        Dim recordingStoppedTcs As New TaskCompletionSource(Of Boolean)()

        Try
            ' Initialize the WaveInEvent for recording
            waveIn = New WaveInEvent()
            waveIn.DeviceNumber = deviceNumber
            waveIn.WaveFormat = New WaveFormat(16000, 1) ' 16kHz mono
            writer = New WaveFileWriter(tempWavPath, waveIn.WaveFormat)

            ' Silence detection setup
            Dim silentMilliseconds As Integer = 0
            Dim bytesPerSample As Integer = waveIn.WaveFormat.BitsPerSample / 8
            Dim channels As Integer = waveIn.WaveFormat.Channels

            ' Audio data handler
            AddHandler waveIn.DataAvailable, Sub(sender, e)
                                                 If writer IsNot Nothing Then
                                                     writer.Write(e.Buffer, 0, e.BytesRecorded)
                                                     writer.Flush()
                                                 End If

                                                 ' Silence detection using RMS
                                                 Dim samplesRecorded As Integer = e.BytesRecorded / bytesPerSample / channels
                                                 Dim sumSquares As Double = 0.0
                                                 For i As Integer = 0 To e.BytesRecorded - 1 Step bytesPerSample * channels
                                                     Dim sample As Short = BitConverter.ToInt16(e.Buffer, i)
                                                     Dim sample32 As Double = sample / 32768.0
                                                     sumSquares += sample32 * sample32
                                                 Next
                                                 Dim rms As Double = Math.Sqrt(sumSquares / samplesRecorded)

                                                 If rms < silenceThreshold Then
                                                     silentMilliseconds += CInt((e.BytesRecorded / waveIn.WaveFormat.AverageBytesPerSecond) * 1000)
                                                     If silentMilliseconds > silenceDuration Then
                                                         waveIn.StopRecording()
                                                     End If
                                                 Else
                                                     silentMilliseconds = 0
                                                 End If
                                             End Sub

            ' Stop recording handler
            AddHandler waveIn.RecordingStopped, Sub(sender, e)
                                                    If writer IsNot Nothing Then
                                                        writer.Dispose()
                                                        writer = Nothing
                                                    End If
                                                    If waveIn IsNot Nothing Then
                                                        waveIn.Dispose()
                                                        waveIn = Nothing
                                                    End If
                                                    recordingStoppedTcs.SetResult(True)
                                                End Sub

            ' Start recording
            waveIn.StartRecording()

            ' Wait until silence is detected
            Await recordingStoppedTcs.Task

            ' Transcribe
            transcription = Await TranscribeAudioAsync(tempWavPath)

            ' Filter: ignore low-value results
            If Not IsValidTranscription(transcription) Then
                Return Nothing
            End If

            Return transcription

        Catch ex As Exception
            Debug.WriteLine($"Error in StartSpeechToText: {ex.Message}")
            Return Nothing
        Finally
            If waveIn IsNot Nothing Then waveIn.Dispose()
            If writer IsNot Nothing Then writer.Dispose()
        End Try
    End Function



    Private Async Function TranscribeAudioAsync(filePath As String) As Task(Of String)
        Dim apiUrl As String = "https://api.openai.com/v1/audio/transcriptions"
        Dim transcription As String = String.Empty

        Using httpClient As New HttpClient()
            httpClient.DefaultRequestHeaders.Clear()
            httpClient.DefaultRequestHeaders.Authorization = New AuthenticationHeaderValue("Bearer", apiKeyV)

            Using form As New MultipartFormDataContent()
                Using modelContent As New StringContent("whisper-1")
                    form.Add(modelContent, "model")

                    Using fileStream As FileStream = File.OpenRead(filePath)
                        Using streamContent As New StreamContent(fileStream)
                            streamContent.Headers.ContentType = MediaTypeHeaderValue.Parse("audio/wav")
                            form.Add(streamContent, "file", Path.GetFileName(filePath))
                            Debug.WriteLine("Added WAV file to form.")

                            Dim response As HttpResponseMessage = Await httpClient.PostAsync(apiUrl, form)
                            If response.IsSuccessStatusCode Then
                                Dim jsonResponse As String = Await response.Content.ReadAsStringAsync()
                                Dim result As TranscriptionResponse = JsonConvert.DeserializeObject(Of TranscriptionResponse)(jsonResponse)
                                transcription = result.text
                                Debug.WriteLine("Transcription successful.")
                            Else
                                Dim errorContent As String = Await response.Content.ReadAsStringAsync()
                                transcription = $"Error: {response.StatusCode}, Content: {errorContent}"
                                Debug.WriteLine($"Error: {response.StatusCode}, Content: {errorContent}")
                            End If
                        End Using
                    End Using
                End Using
            End Using
        End Using

        Return transcription
    End Function

    Private Class TranscriptionResponse
        Public Property text As String
    End Class

    Private Function IsValidTranscription(text As String) As Boolean
        If String.IsNullOrWhiteSpace(text) Then Return False

        Dim trimmed = text.Trim().ToLowerInvariant()
        Dim fillerWords As String() = {"you", "uh", "um", "yeah", "okay", "hmm"}

        Return Not fillerWords.Contains(trimmed) AndAlso trimmed.Length > 2
    End Function


End Module

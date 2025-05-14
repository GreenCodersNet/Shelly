' ###  generateFiles.vb - v1.0.1 ### 

' ##########################################################
'  Shelly App - v1.0.1
'  License: Creative Commons Attribution-NonCommercial (CC BY-NC)
'  https://creativecommons.org/licenses/by-nc/4.0/
'  © 2025 Vlad Stefanescu | GreenCoders.net. Attribution required.
' ##########################################################

Imports System.IO
Imports System.Text
Imports System.Text.RegularExpressions
Imports System.Threading
Imports Newtonsoft.Json

Module generateFiles

    Public GeneratedFileContent As New StringBuilder()

    Private Sub PrepareEmptyFile(filePath As String)
        Dim dir = Path.GetDirectoryName(filePath)
        If Not Directory.Exists(dir) Then Directory.CreateDirectory(dir)
        If File.Exists(filePath) Then File.Delete(filePath)
        File.Create(filePath).Dispose()
    End Sub

    Public Function SaveGeneratedContentToFile(filePath As String) As Tuple(Of Boolean, String)
        Try
            Dim ext = Path.GetExtension(filePath).ToLowerInvariant()
            If ext = ".doc" OrElse ext = ".docx" Then
                ' Try Word COM
                Try
                    Dim wordApp = CreateObject("Word.Application")
                    wordApp.Visible = False
                    Dim doc = wordApp.Documents.Add()
                    doc.Content.Text = GeneratedFileContent.ToString()
                    ' Save as DOCX (12 = wdFormatXMLDocument)
                    doc.SaveAs2(filePath, 12)
                    doc.Close(False)
                    wordApp.Quit(False)
                    GeneratedFileContent.Clear()
                    Return Tuple.Create(True, $"✅ Word document saved: {filePath}")
                Catch
                    ' Fallback to plain-text .docx
                    File.WriteAllText(filePath, GeneratedFileContent.ToString(), Encoding.UTF8)
                    GeneratedFileContent.Clear()
                    Return Tuple.Create(True, $"⚠️ Word COM failed; saved plain-text DOCX at: {filePath}")
                End Try
            Else
                ' All other extensions: plain UTF-8 text
                File.WriteAllText(filePath, GeneratedFileContent.ToString(), Encoding.UTF8)
                GeneratedFileContent.Clear()
                Return Tuple.Create(True, $"✅ Text file saved: {filePath}")
            End If
        Catch ex As Exception
            Return Tuple.Create(False, $"Error saving file: {ex.Message}")
        End Try
    End Function

    Public Async Function GenerateLargeFileWithTextOrCode(
      filePath As String,
      userPrompt As String,
      Optional totalChunks As Integer = 5,
      Optional ct As CancellationToken = Nothing
  ) As Task(Of Tuple(Of Boolean, String))

        Dim ext = Path.GetExtension(filePath).ToLowerInvariant()

        If ext = ".pptx" Then
            ' delegate to our slide builder
            Return Await GeneratePowerPoint(filePath, userPrompt, totalChunks, ct)
        End If

        ' … your existing “text or Word” logic here …
        PrepareEmptyFile(filePath)
        GeneratedFileContent.Clear()
        For chunkIndex As Integer = 1 To totalChunks
            If ct.IsCancellationRequested Then Exit For
            ' build your chunk prompt exactly as before…
            Dim sb As New StringBuilder()
            sb.AppendLine($"File: {Path.GetFileName(filePath)}")
            sb.AppendLine($"Instruction: {userPrompt}")
            sb.AppendLine($"Chunk: {chunkIndex}/{totalChunks}")
            ' … tail context, etc. …
            sb.AppendLine("Now generate only the next portion of the file as plain text, no fences or commentary.")

            Dim rawChunk As String = Await AIcall.CallGPTCore(
                Globals.UserApiKey,
                Globals.AiModelSelection,
                New List(Of Dictionary(Of String, String)) From {
                    New Dictionary(Of String, String) From {
                        {"role", "system"},
                        {"content", "You are a file generator: produce text or code, no fences."}
                    },
                    New Dictionary(Of String, String) From {
                        {"role", "user"},
                        {"content", sb.ToString()}
                    }
                },
                Globals.temperature,
                ct
            )

            ' 5) Clean fences out of rawChunk
            Dim cleaned As String = Regex.Replace(rawChunk, "```.*?```", "", RegexOptions.Singleline).Trim()

            ' 6) Append into your buffer
            GeneratedFileContent.Append(cleaned)
        Next

        Return SaveGeneratedContentToFile(filePath)
    End Function


    Private Async Function GeneratePowerPoint(
    pptxPath As String,
    topic As String,
    numSlides As Integer,
    ct As CancellationToken
) As Task(Of Tuple(Of Boolean, String))

        Try
            ' 📌 Launch PowerPoint (must remain Visible)
            Dim ppt = CreateObject("PowerPoint.Application")
            ppt.Visible = True
            ppt.DisplayAlerts = 0                ' ppAlertsNone

            ' 🗔 Immediately minimize so it lives off-screen
            ' 2 = ppWindowMinimized
            ppt.WindowState = 2

            ' ───────────────────────────────
            ' Create a new blank presentation
            ' ───────────────────────────────
            Dim pres = ppt.Presentations.Add()

            ' ───────────────────────────────
            ' Build each slide
            ' ───────────────────────────────
            For i As Integer = 1 To numSlides
                If ct.IsCancellationRequested Then Exit For

                ' 1) Ask GPT for a slide definition
                Dim prompt =
                    $"Generate slide {i}/{numSlides} for ""{topic}"". Respond ONLY with valid JSON:" & vbCrLf &
                    "{" & vbCrLf &
                    "  ""title"": ""Short slide title""," & vbCrLf &
                    "  ""bullets"": [""First bullet"", ""Second bullet""]" & vbCrLf &
                    "}"
                Dim rawJson = Await AIcall.CallGPTCore(
                    Globals.UserApiKey,
                    Globals.AiModelSelection,
                    New List(Of Dictionary(Of String, String)) From {
                        New Dictionary(Of String, String) From {
                            {"role", "system"}, {"content", "You are a slide‐content generator. Output EXACTLY valid JSON."}
                        },
                        New Dictionary(Of String, String) From {
                            {"role", "user"}, {"content", prompt}
                        }
                    },
                    Globals.temperature,
                    ct
                )
                rawJson = Regex.Replace(rawJson, "```.*?```", "", RegexOptions.Singleline).Trim()
                Dim slideDef = JsonConvert.DeserializeObject(Of SlideDef)(rawJson)

                ' 2) Add it to the deck
                Dim slide = pres.Slides.Add(pres.Slides.Count + 1, 2) ' 2 = ppLayoutText
                slide.Shapes(1).TextFrame.TextRange.Text = slideDef.title
                slide.Shapes(2).TextFrame.TextRange.Text = String.Join(vbCrLf, slideDef.bullets)
            Next

            ' ───────────────────────────────
            ' Save & quit
            ' ───────────────────────────────
            pres.SaveAs(pptxPath)
            pres.Close()
            ppt.Quit()

            Return Tuple.Create(True, $"✅ PowerPoint saved: {pptxPath}")

        Catch ex As Exception
            Return Tuple.Create(False, $"PowerPoint error: {ex.Message}")
        End Try
    End Function

    Private Class SlideDef
        Public Property title As String
        Public Property bullets As List(Of String)
    End Class

End Module

' ###  AIimage.vb - v1.0.1 ### 

' ##########################################################
'  Shelly - v1.0.1
'  License: Creative Commons Attribution-NonCommercial (CC BY-NC)
'  https://creativecommons.org/licenses/by-nc/4.0/
'  © 2025 Vlad Stefanescu | GreenCoders.net. Attribution required.
' ##########################################################

Imports System.IO
Imports System.Net.Http
Imports System.Text
Imports Newtonsoft.Json
Imports Newtonsoft.Json.Linq
Imports System.Text.RegularExpressions
Imports System.Drawing.Imaging

Module AIimage
    Public Async Function CallImageGeneration(
    apiKey As String,
    imageRequests As List(Of ImageRequestData),
    Optional model As String = "dall-e-3",
    Optional quality As String = "hd"
) As Task(Of List(Of String))

        IncrementAICallCount()

        Dim imagesList As New List(Of String)()

        Dim historyText As String = ConversationHistoryToPlainText()

        If Not String.IsNullOrWhiteSpace(historyText) Then
            historyText = "Now here is our conversation history (if needed):" & vbCrLf & historyText
        Else
            historyText = ""
        End If

        Using httpClient As New HttpClient()
            Try
                ' The endpoint for DALL·E image generation
                Dim apiEndpoint As String = "https://api.openai.com/v1/images/generations"

                ' For each image request in the list
                For Each req In imageRequests
                    Dim payloadObj = New With {
                        model,
                        .prompt = req.ImagePrompt & historyText & vbCrLf & " *** Never add text inside images! ***",
                        req.Size,
                        quality,
                        .n = 1
                    }
                    Dim payloadJson As String = JsonConvert.SerializeObject(payloadObj, Formatting.Indented)

                    ' Wrap StringContent in a Using block
                    Using requestBody As New StringContent(payloadJson, Encoding.UTF8, "application/json")
                        ' Set headers for the request
                        httpClient.DefaultRequestHeaders.Clear()
                        httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " & apiKey)

                        ' Make the POST call
                        Dim response As HttpResponseMessage = Await httpClient.PostAsync(apiEndpoint, requestBody)

                        If response.IsSuccessStatusCode Then
                            ' Parse the JSON response
                            Dim responseContent As String = Await response.Content.ReadAsStringAsync()
                            Dim jObj = JObject.Parse(responseContent)

                            Dim dataArray = jObj("data")
                            If dataArray IsNot Nothing Then
                                For Each item In dataArray
                                    Dim imageUrl = item("url")?.ToString()
                                    If imageUrl IsNot Nothing Then
                                        imagesList.Add(imageUrl)
                                    End If
                                Next
                            End If
                        Else
                            ' If not success, log an error
                            Dim errorMessage = $"[Image Generation Error] {response.StatusCode}"
                            Dim details = Await response.Content.ReadAsStringAsync()
                            errorMessage &= vbCrLf & $"Details: {details}"

                            ' Show in Form1's RichTextBox or wherever you prefer
                            Debug.WriteLine($"[>] AI Image generator ERROR: Failed - {errorMessage}")
                        End If
                    End Using
                Next

            Catch ex As Exception
                ' If any exception occurs during the loop or request
                Debug.WriteLine("An error occurred while generating images: " & ex.Message & Environment.NewLine)
            End Try
        End Using

        ' Return all collected image URLs
        Return imagesList
    End Function

    Public Class ImageRequestData
        Public Property ImagePrompt As String = "An image"
        Public Property NumImages As Integer = 1
        Public Property Size As String = "512x512"
        Public Property FolderPath As String = $"C:\Users\{Environment.UserName}\Desktop\AIGenImages"
        Public Property Style As String = ""
        Public Property ImageName As String = "myimage.jpg"
    End Class
    Public Function ConversationHistoryToPlainText() As String
        Dim sb As New StringBuilder()

        For Each message In Globals.conversationHistory
            Dim role As String = Nothing
            Dim content As String = Nothing
            If message.TryGetValue("role", role) AndAlso message.TryGetValue("content", content) Then
                sb.AppendLine($"{role.ToUpper()}: {content}")
                sb.AppendLine()
            End If
        Next

        Return sb.ToString().Trim()
    End Function

    ' DownloadImage:
    '    Takes an imageUrl from the list, plus a local file path, 
    '    and saves the image data to disk.

    Public Async Function DownloadImage(imageUrl As String, localPath As String) As Task
        Try
            Using httpClient As New HttpClient()
                Dim data As Byte() = Await httpClient.GetByteArrayAsync(imageUrl)
                System.IO.File.WriteAllBytes(localPath, data)
            End Using
        Catch ex As Exception
            ' Log any download issues
            Debug.WriteLine("An error occurred while downloading image: " & ex.Message & Environment.NewLine)
        End Try
    End Function

    ' ====================================

    Private Function IsValidUrl(url As String) As Boolean
        Dim pattern As String = "^https?://"
        Return Regex.IsMatch(url, pattern)
    End Function

    ' Converts a local image file to a Base64 string.
    Public Function ConvertImageToBase64(imagePath As String) As String
        Dim imageBytes As Byte() = File.ReadAllBytes(imagePath)
        Return Convert.ToBase64String(imageBytes)
    End Function

    ' Converts an image from a URL to a Base64 string.
    Public Async Function ConvertImageUrlToBase64(imageUrl As String) As Task(Of String)
        Using client As New HttpClient()
            Dim imageBytes As Byte() = Await client.GetByteArrayAsync(imageUrl)
            Return Convert.ToBase64String(imageBytes)
        End Using
    End Function

    ' Sends the image (as Base64) along with the user query to the OpenAI Chat Completions endpoint.
    Private Async Function SendImageToOpenAI(apiKey As String, imageBase64 As String, userQuery As String) As Task(Of String)

        IncrementAICallCount()

        Dim apiUrl As String = "https://api.openai.com/v1/chat/completions"
        Using client As New HttpClient()
            client.DefaultRequestHeaders.Add("Authorization", "Bearer " & apiKey)

            ' Build a prompt that instructs the AI to analyze the image comprehensively.
            Dim analysisPrompt As String = "Please analyze the image provided and answer only the following query: " & userQuery &
                                       " Do not include a full description of the image; only include details directly relevant to the query."

            Dim requestBody As New With {
            .model = "gpt-4o-mini", ' Use 4o-mini for reading images
            .messages = New Object() {
                New With {
                    .role = "user",
                    .content = New Object() {
                        New With {.type = "text", .text = analysisPrompt},
                        New With {.type = "image_url", .image_url = New With {.url = "data:image/png;base64," & imageBase64}}
                    }
                }
            },
            .max_tokens = 1200
        }

            Dim jsonContent As String = JsonConvert.SerializeObject(requestBody)

            Using content As New StringContent(jsonContent, Encoding.UTF8, "application/json")
                Dim response As HttpResponseMessage = Await client.PostAsync(apiUrl, content)
                If response.IsSuccessStatusCode Then
                    Dim responseBody As String = Await response.Content.ReadAsStringAsync()
                    Return responseBody
                Else
                    Dim errorResponse As String = Await response.Content.ReadAsStringAsync()
                    Throw New Exception("Request failed. Status Code: " & response.StatusCode & vbCrLf & "Response Content: " & errorResponse)
                End If
            End Using
        End Using
    End Function


    ' Analyzes multiple images by converting each to Base64, sending it with the user query to OpenAI,
    ' and concatenating the responses.
    Public Async Function AnalyzeImagesContentAsync(apiKey As String, listOfImages As String, query As String) As Task(Of String)
        Dim results As New StringBuilder()
        Dim images As List(Of String) = listOfImages.Split(","c).Select(Function(img) img.Trim()).ToList()

        For Each img In images
            Dim imageBase64 As String = ""
            If IsValidUrl(img) Then
                Try
                    imageBase64 = Await ConvertImageUrlToBase64(img)
                Catch ex As Exception
                    results.AppendLine($"Error downloading image from URL: {img} - {ex.Message}")
                    Continue For
                End Try
            ElseIf File.Exists(img) Then
                Try
                    imageBase64 = ConvertImageToBase64(img)
                Catch ex As Exception
                    results.AppendLine($"Error reading local file: {img} - {ex.Message}")
                    Continue For
                End Try
            Else
                results.AppendLine($"Invalid input: {img}")
                Continue For
            End If

            Try
                Dim openAIResponse As String = Await SendImageToOpenAI(apiKey, imageBase64, query)
                Dim content As String = ExtractContentFromResponse(openAIResponse)
                results.AppendLine($"Result for {img}:")
                results.AppendLine(content)
                results.AppendLine()
            Catch ex As Exception
                results.AppendLine($"Error analyzing {img}: {ex.Message}")
            End Try
        Next

        Return results.ToString()
    End Function

    ' =========================================

    Public Function CaptureScreenshot() As String
        ' Determine a default folder for screenshots (My Pictures\Screenshots).
        Dim picturesPath As String = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)
        Dim screenshotFolder As String = IO.Path.Combine(picturesPath, "Screenshots")
        If Not IO.Directory.Exists(screenshotFolder) Then
            IO.Directory.CreateDirectory(screenshotFolder)
        End If

        Dim fileName As String = "Screenshot_" & DateTime.Now.ToString("yyyyMMdd_HHmmss") & ".png"
        Dim filePath As String = IO.Path.Combine(screenshotFolder, fileName)

        ' Capture the primary monitor.
        Dim bounds As Rectangle = Screen.PrimaryScreen.Bounds
        Dim bitmap As New Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb)
        Using g As Graphics = Graphics.FromImage(bitmap)
            g.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size)
        End Using

        bitmap.Save(filePath, ImageFormat.Png)
        bitmap.Dispose()
        Return filePath
    End Function

    Public Async Function AnalyzeScreenshotAsync(apiKey As String, userQuery As String) As Task(Of String)
        Dim screenshotPath As String = Globals.LastScreenshotPath
        If String.IsNullOrWhiteSpace(screenshotPath) OrElse Not IO.File.Exists(screenshotPath) Then
            screenshotPath = CaptureScreenshot()
            Globals.LastScreenshotPath = screenshotPath
        End If

        ' Convert the screenshot to Base64 using your existing function.
        Dim imageBase64 As String = ConvertImageToBase64(screenshotPath)
        ' Send the image along with the user query.
        Dim openAIResponse As String = Await SendImageToOpenAI(apiKey, imageBase64, userQuery)
        Dim content As String = ExtractContentFromResponse(openAIResponse)
        Return content
    End Function

    ' Extracts the "content" field from the JSON response returned by OpenAI.
    Private Function ExtractContentFromResponse(jsonResponse As String) As String
        Dim parsedResponse As JObject = JObject.Parse(jsonResponse)
        Dim content As String = parsedResponse("choices")(0)("message")("content").ToString()
        Return content
    End Function

End Module

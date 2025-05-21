' ###  AIcall.vb - v1.0.1 ### 

' ##########################################################
'  Shelly - v1.0.1
'  License: Creative Commons Attribution-NonCommercial (CC BY-NC)
'  https://creativecommons.org/licenses/by-nc/4.0/
'  © 2025 Vlad Stefanescu | GreenCoders.net. Attribution required.
' ##########################################################


Imports System.Net.Http
Imports Newtonsoft.Json
Imports Newtonsoft.Json.Linq
Imports System.Text
Imports System.Threading

Module AIcall

    ' ─── Configuration ───
    Private ReadOnly httpClient As New HttpClient() With {
        .Timeout = TimeSpan.FromMinutes(10)  ' Allow up to 10 minutes for large responses
    }
    Private ReadOnly ApiBaseUrl As String = "https://api.openai.com/v1"

    ' ─── Shelly’s Core System Instructions ───
    Private ReadOnly ShellySystemPrompt As String = String.Join(vbLf, New String() {
        "You are Shelly, a powerful Windows assistant application.",
        "Your goal is to decide which actions to take—via PowerShell scripts or custom VB.NET functions—to fulfill user requests fully and sequentially.",
        "Always:",
        " • Use available custom functions for covered tasks.",
        " • Otherwise, generate PowerShell scripts in ```powershell``` blocks.",
        " • Never explain your internal process.",
        " • Combine multiple steps into a single, comprehensive response when requested."
    })


    Public Async Function CallGPTCore(
        apiKey As String,
        model As String,
        messages As List(Of Dictionary(Of String, String)),
        temperature As Double,
        ct As CancellationToken
    ) As Task(Of String)

        IncrementAICallCount()
        Dim endpoint As String = $"{ApiBaseUrl}/chat/completions"
        Dim maxRetries As Integer = 3
        Dim lastError As Exception = Nothing

        ' 1️ Inject ShellySystemPrompt if no system role
        If Not messages.Any(Function(m) m.ContainsKey("role") AndAlso m("role").Equals("system", StringComparison.OrdinalIgnoreCase)) Then
            messages.Insert(0, New Dictionary(Of String, String) From {
                {"role", "system"},
                {"content", ShellySystemPrompt}
            })
        End If

        ' 2️ Estimate tokens
        Dim promptTokens As Integer = EstimateTokenCount(messages)
        Const ContextWindow As Integer = 128000       ' Max tokens input+output buffer
        Const MaxCompletion As Integer = 16000       ' Max tokens model will generate

        ' Warn if user-supplied messages alone exceed context
        If promptTokens > ContextWindow Then
            Debug.WriteLine($"[WARNING] promptTokens ({promptTokens}) exceed context window ({ContextWindow})")
        End If

        ' 3️ Determine allowable output tokens
        Dim availableTokens As Integer = ContextWindow - promptTokens
        Dim finalMaxTokens As Integer = Math.Min(availableTokens, MaxCompletion)
        If finalMaxTokens <= 0 Then
            Throw New Exception($"Prompt too long! Used {promptTokens} tokens; no room for completion.")
        End If

        ' 4️ Perform API call with retries
        For attempt As Integer = 1 To maxRetries
            Try
                Shelly.Instance.LabelStatusUpdate.Text = $"Sending AI request (attempt {attempt})…"

                Dim payload As New Dictionary(Of String, Object)
                payload("model") = model
                payload("messages") = messages

                If model.StartsWith("o3-mini", StringComparison.OrdinalIgnoreCase) Then
                    ' o3-mini only supports max_completion_tokens
                    payload("max_completion_tokens") = finalMaxTokens
                Else
                    ' All other models get the usual parameters
                    payload("temperature") = temperature
                    payload("top_p") = 1.0
                    payload("frequency_penalty") = 0.0
                    payload("presence_penalty") = 0.0
                    payload("max_tokens") = finalMaxTokens
                End If

                Dim jsonBody = JsonConvert.SerializeObject(payload)
                Using content As New StringContent(jsonBody, Encoding.UTF8, "application/json")
                    httpClient.DefaultRequestHeaders.Clear()
                    httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}")
                    Dim resp = Await httpClient.PostAsync(endpoint, content, ct)
                    Dim respText = Await resp.Content.ReadAsStringAsync(ct)

                    If Not resp.IsSuccessStatusCode Then
                        Dim errObj = JObject.Parse(respText)
                        If errObj("error")?("code")?.ToString() = "model_not_found" Then
                            Return "[ERROR] Model not found or inaccessible."
                        End If
                        Throw New Exception($"API returned {(CInt(resp.StatusCode))}: {respText}")
                    End If

                    Dim jobj As JObject = JObject.Parse(respText)

                    ' Extract model actually used
                    Dim modelUsed As String = jobj("model")?.ToString()
                    If Not String.IsNullOrWhiteSpace(modelUsed) Then
                        Globals.LastUsedModel = modelUsed  ' <- Store it in a global module or wherever you prefer
                        Debug.WriteLine($"[AI] Model actually used: {modelUsed}")
                    End If

                    ' Extract assistant response
                    Dim choice As String = jobj("choices")(0)("message")("content").ToString()
                    If String.IsNullOrWhiteSpace(choice) Then Throw New Exception("No completion in response.")
                    Return choice.Trim()
                End Using

            Catch ex As OperationCanceledException
                Throw
            Catch ex As Exception
                lastError = ex
                Debug.WriteLine($"[CallGPTCore] attempt {attempt} failed: {ex.Message}")
            End Try

            If attempt < maxRetries Then
                Shelly.Instance.LabelStatusUpdate.Text = "Retrying AI request…"
                Await Task.Delay(1000, ct)
            End If
        Next

        Debug.WriteLine($"[CallGPTCore] all attempts failed: {lastError?.Message}")
        Return "[>] ERROR: Failed to get a response from AI after retries."
    End Function

    Public Function EstimateTokenCount(messages As List(Of Dictionary(Of String, String))) As Integer
        Dim totalChars As Integer = 0
        For Each msg In messages
            Dim content As String = Nothing
            If msg.TryGetValue("content", content) AndAlso content IsNot Nothing Then
                totalChars += content.Length
            End If
        Next
        Return totalChars \ 4
    End Function

End Module

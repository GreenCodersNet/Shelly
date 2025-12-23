' ###  AIcall.vb - v1.0.2 ### 

' ##########################################################
'  Shelly - v1.0.2
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

    ' ─── Shelly's Core System Instructions ───
    Private ReadOnly ShellySystemPrompt As String = String.Join(vbLf, New String() {
        "You are Shelly, a powerful Windows assistant application.",
        "Your goal is to decide which actions to take—via PowerShell scripts or custom VB.NET functions—to fulfill user requests fully and sequentially.",
        "Always:",
        " • Use available custom functions for covered tasks.",
        " • Otherwise, generate PowerShell scripts in ```powershell``` blocks.",
        " • Never explain your internal process.",
        " • Combine multiple steps into a single, comprehensive response when requested."
    })

    ' ─── Model Classification ───
    ''' <summary>
    ''' Determines if a model is a reasoning model (GPT-5 series, o-series)
    ''' Reasoning models use max_completion_tokens and don't support temperature
    ''' </summary>
    Public Function IsReasoningModel(model As String) As Boolean
        If String.IsNullOrEmpty(model) Then Return False
        Dim lowerModel = model.ToLowerInvariant()
        
        ' GPT-5 series are reasoning models
        If lowerModel.StartsWith("gpt-5") Then Return True
        
        ' o-series models (o1, o3, o4, etc.) are reasoning models
        If lowerModel.StartsWith("o1") OrElse 
           lowerModel.StartsWith("o3") OrElse 
           lowerModel.StartsWith("o4") Then Return True
        
        Return False
    End Function

    ''' <summary>
    ''' Gets the context window size for a given model
    ''' </summary>
    Public Function GetContextWindow(model As String) As Integer
        If String.IsNullOrEmpty(model) Then Return 128000
        Dim lowerModel = model.ToLowerInvariant()
        
        ' GPT-5 series - 256K context
        If lowerModel.StartsWith("gpt-5") Then Return 256000
        
        ' GPT-4.1 series - 1M context
        If lowerModel.StartsWith("gpt-4.1") Then Return 1000000
        
        ' GPT-4o series - 128K context
        If lowerModel.StartsWith("gpt-4o") Then Return 128000
        
        ' o-series models - 200K context
        If lowerModel.StartsWith("o1") OrElse 
           lowerModel.StartsWith("o3") OrElse 
           lowerModel.StartsWith("o4") Then Return 200000
        
        ' Default fallback
        Return 128000
    End Function

    ''' <summary>
    ''' Gets the maximum output tokens for a given model
    ''' </summary>
    Public Function GetMaxOutputTokens(model As String) As Integer
        If String.IsNullOrEmpty(model) Then Return 16000
        Dim lowerModel = model.ToLowerInvariant()
        
        ' GPT-5 series - 32K output
        If lowerModel.StartsWith("gpt-5") Then Return 32000
        
        ' GPT-4.1 series - 32K output
        If lowerModel.StartsWith("gpt-4.1") Then Return 32000
        
        ' GPT-4o series - 16K output
        If lowerModel.StartsWith("gpt-4o") Then Return 16384
        
        ' o-series models - 100K output
        If lowerModel.StartsWith("o1") OrElse 
           lowerModel.StartsWith("o3") OrElse 
           lowerModel.StartsWith("o4") Then Return 100000
        
        ' Default fallback
        Return 16000
    End Function

    ' Add cleanup method for proper resource disposal
    Public Sub Cleanup()
        Try
            httpClient?.Dispose()
        Catch ex As Exception
            Debug.WriteLine($"[AIcall] Cleanup error: {ex.Message}")
        End Try
    End Sub

    Public Async Function CallGPTCore(
        apiKey As String,
        model As String,
        messages As List(Of Dictionary(Of String, String)),
        temperature As Double,
        ct As CancellationToken,
        Optional jsonMode As Boolean = False
    ) As Task(Of String)

        IncrementAICallCount()
        Dim endpoint As String = $"{ApiBaseUrl}/chat/completions"
        Dim maxRetries As Integer = 3
        Dim lastError As Exception = Nothing

        ' 1️⃣ Only inject ShellySystemPrompt if NO system role exists at all
        Dim hasSystemRole As Boolean = messages.Any(Function(m) m.ContainsKey("role") AndAlso m("role").Equals("system", StringComparison.OrdinalIgnoreCase))
            
        If Not hasSystemRole Then
            messages.Insert(0, New Dictionary(Of String, String) From {
                {"role", "system"},
                {"content", ShellySystemPrompt}
            })
        End If

        ' 1.5️⃣ JSON Mode Safety Check (OpenAI Requirement)
        If jsonMode Then
            Dim systemMsg = messages.FirstOrDefault(Function(m) m("role") = "system")
            If systemMsg IsNot Nothing AndAlso Not systemMsg("content").Contains("JSON") Then
                ' Append instruction to ensure API doesn't reject the request
                systemMsg("content") &= vbLf & "IMPORTANT: You must output valid JSON."
            End If
        End If

        ' 2️⃣ Get model-specific limits
        Dim contextWindow As Integer = GetContextWindow(model)
        Dim maxCompletion As Integer = GetMaxOutputTokens(model)
        Dim isReasoning As Boolean = IsReasoningModel(model)

        ' 3️⃣ Estimate tokens with improved accuracy
        Dim promptTokens As Integer = EstimateTokenCount(messages)

        ' Warn if user-supplied messages alone exceed context
        If promptTokens > contextWindow Then
            Debug.WriteLine($"[WARNING] promptTokens ({promptTokens}) exceed context window ({contextWindow})")
            ' Try to reduce message size by truncating older messages
            While promptTokens > contextWindow * 0.8 AndAlso messages.Count > 2
                ' Remove oldest non-system message
                For i = 1 To messages.Count - 1
                    If messages(i)("role") <> "system" Then
                        messages.RemoveAt(i)
                        promptTokens = EstimateTokenCount(messages)
                        Exit For
                    End If
                Next
            End While
        End If

        ' 4️⃣ Determine allowable output tokens
        Dim availableTokens As Integer = contextWindow - promptTokens
        Dim finalMaxTokens As Integer = Math.Min(availableTokens, maxCompletion)
        If finalMaxTokens <= 0 Then
            Throw New Exception($"Prompt too long! Used {promptTokens} tokens; no room for completion.")
        End If

        ' 5️⃣ Perform API call with retries
        For attempt As Integer = 1 To maxRetries
            Try
                Shelly.Instance.LabelStatusUpdate.Text = $"Sending AI request (attempt {attempt})…"

                Dim payload As New Dictionary(Of String, Object)
                payload("model") = model
                payload("messages") = messages

                ' ═══════════════════════════════════════════════════════════════
                ' MODEL-SPECIFIC PARAMETER HANDLING
                ' ═══════════════════════════════════════════════════════════════
                If isReasoning Then
                    ' Reasoning models (GPT-5 series, o-series):
                    ' - Use max_completion_tokens instead of max_tokens
                    ' - Do NOT support temperature, top_p, frequency_penalty, presence_penalty
                    payload("max_completion_tokens") = finalMaxTokens
                    
                    Debug.WriteLine($"[AIcall] Using reasoning model parameters for {model}")
                Else
                    ' Standard models (GPT-4.1, GPT-4o, etc.):
                    ' - Use traditional parameters
                    payload("temperature") = temperature
                    payload("top_p") = 1.0
                    payload("frequency_penalty") = 0.0
                    payload("presence_penalty") = 0.0
                    payload("max_tokens") = finalMaxTokens
                    
                    ' ✅ JSON Mode Support
                    If jsonMode Then
                        payload("response_format") = New Dictionary(Of String, String) From {{"type", "json_object"}}
                    End If
                    
                    Debug.WriteLine($"[AIcall] Using standard model parameters for {model}")
                End If

                Dim jsonBody = JsonConvert.SerializeObject(payload)
                Using content As New StringContent(jsonBody, Encoding.UTF8, "application/json")
                    ' Clear and set headers for each request to prevent accumulation
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
                    
                    ' Log token usage for optimization
                    Dim usage = jobj("usage")
                    If usage IsNot Nothing Then
                        Dim promptTokensUsed As Integer = If(usage("prompt_tokens")?.ToObject(Of Integer)(), 0)
                        Dim completionTokens As Integer = If(usage("completion_tokens")?.ToObject(Of Integer)(), 0)
                        Debug.WriteLine($"[AI] Tokens used - Prompt: {promptTokensUsed}, Completion: {completionTokens}")
                    End If
                    
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

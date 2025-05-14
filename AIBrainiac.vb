' ###  AIBrainiac.vb - v1.0.1 ### 

' ##########################################################
'  Shelly App - v1.0.1
'  License: Creative Commons Attribution-NonCommercial (CC BY-NC)
'  https://creativecommons.org/licenses/by-nc/4.0/
'  © 2025 Vlad Stefanescu | GreenCoders.net. Attribution required.
' ##########################################################

Imports System.Net.Http
Imports System.Text
Imports Newtonsoft.Json.Linq

Module AIBrainiac

    Private ReadOnly ApiBaseUrl As String = "https://api.openai.com/v1"
    Private ReadOnly httpClient As New HttpClient()

    ' Shelly’s core system instructions
    Private ReadOnly ShellySystemPrompt As String = "
You are Shelly, a powerful Windows assistant.
Decide which actions to take—via PowerShell scripts or custom VB.NET functions—to fulfill the user’s request fully and sequentially.
Always:
 • Use custom functions when available.
 • Otherwise generate PowerShell scripts wrapped in ```powershell``` blocks.
 • Never reveal your internal process.
 • Combine multiple steps into one comprehensive response when asked.
".Trim()


    Private Async Function RetrieveAssistant(
        apiKey As String,
        assistantId As String
    ) As Task(Of JObject)

        Dim endpoint = $"{ApiBaseUrl}/assistants/{assistantId}"
        Try
            httpClient.DefaultRequestHeaders.Clear()
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}")
            httpClient.DefaultRequestHeaders.Add("OpenAI-Beta", "assistants=v2")

            Dim resp = Await httpClient.GetAsync(endpoint)
            Dim body = Await resp.Content.ReadAsStringAsync()
            If resp.IsSuccessStatusCode Then
                Return JObject.Parse(body)
            Else
                Debug.WriteLine($"[RetrieveAssistant] {resp.StatusCode}: {body}")
                Return Nothing
            End If

        Catch ex As Exception
            Debug.WriteLine($"[RetrieveAssistant] Exception: {ex}")
            Return Nothing
        End Try
    End Function

    Public Async Function CallGPTBrain(
        apiKey As String,
        userQuestion As String,
        assistantId As String,
        conversationHistory As List(Of Dictionary(Of String, String)),
        Optional additionalMessages As List(Of Dictionary(Of String, String)) = Nothing
    ) As Task(Of String)

        IncrementAICallCount()

        ' 1️ Retrieve assistant details
        Dim details = Await RetrieveAssistant(apiKey, assistantId)
        If details Is Nothing Then
            Return "[>] ERROR: Could not retrieve assistant details."
        End If

        Dim model = details("model")?.ToString()
        Dim builtIn = details("instructions")?.ToString()

        ' 2️ Truncate history
        Dim maxHist = Shelly.Instance.maxHistoryMessages
        Dim hist = conversationHistory _
                   .Skip(Math.Max(0, conversationHistory.Count - maxHist)) _
                   .ToList()

        ' 3️ Build the messages array
        Dim msgs As New List(Of JObject)

        ' 3a. Shelly’s static system prompt
        msgs.Add(New JObject(
            New JProperty("role", "system"),
            New JProperty("content", ShellySystemPrompt)
        ))

        ' 3b. The assistant’s own instructions
        If Not String.IsNullOrWhiteSpace(builtIn) Then
            msgs.Add(New JObject(
                New JProperty("role", "system"),
                New JProperty("content", builtIn.Trim())
            ))
        End If

        ' 3c. Prior conversation
        For Each m In hist
            msgs.Add(New JObject(
                New JProperty("role", m("role")),
                New JProperty("content", m("content"))
            ))
        Next

        ' 3d. Any additional messages
        If additionalMessages IsNot Nothing Then
            For Each m In additionalMessages
                msgs.Add(New JObject(
                    New JProperty("role", m("role")),
                    New JProperty("content", m("content"))
                ))
            Next
        End If

        ' 3e. The user’s latest question
        msgs.Add(New JObject(
            New JProperty("role", "user"),
            New JProperty("content", userQuestion)
        ))

        ' 4 Create payload
        Dim payload As New JObject(
            New JProperty("model", model),
            New JProperty("messages", New JArray(msgs)),
            New JProperty("max_tokens", 16000),
            New JProperty("temperature", 0.7)
        )

        Try
            ' 5 Send POST
            httpClient.DefaultRequestHeaders.Clear()
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}")
            httpClient.DefaultRequestHeaders.Add("OpenAI-Beta", "assistants=v2")

            Dim endpoint = $"{ApiBaseUrl}/chat/completions"
            Using content = New StringContent(
                    payload.ToString(),
                    Encoding.UTF8,
                    "application/json")

                Dim resp = Await httpClient.PostAsync(endpoint, content)
                Dim body = Await resp.Content.ReadAsStringAsync()
                If Not resp.IsSuccessStatusCode Then
                    Debug.WriteLine($"[CallGPTBrain] {resp.StatusCode}: {body}")
                    Return "[>] ERROR: Chat completion failed."
                End If

                Dim json = JObject.Parse(body)

                ' 6 Extract the actual model used from the response
                Dim modelUsed As String = json("model")?.ToString()
                If Not String.IsNullOrWhiteSpace(modelUsed) Then
                    Globals.LastUsedModel = modelUsed
                    Debug.WriteLine($"[CallGPTBrain] Actual model used: {modelUsed}")
                End If

                ' 7 Extract the assistant response
                Dim answer = json("choices")?.First?("message")?("content")?.ToString()?.Trim()
                If String.IsNullOrEmpty(answer) Then
                    Return "[>] ERROR: Empty response from assistant."
                End If

                ' 8 Save to history & return
                conversationHistory.Add(New Dictionary(Of String, String) From {
                    {"role", "assistant"}, {"content", answer}
                })
                Await convHistory.TrimConversationHistoryByTokens_Dict(
                    conversationHistory, Globals.MaxTotalTokens, answer)

                Return answer
            End Using

        Catch ex As Exception
            Debug.WriteLine($"[CallGPTBrain] Exception: {ex}")
            Return "[>] ERROR: Exception in chat completion."
        End Try
    End Function

End Module

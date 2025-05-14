' ###  convHistory.vb - v1.0.1 ### 

' ##########################################################
'  Shelly App - v1.0.1
'  License: Creative Commons Attribution-NonCommercial (CC BY-NC)
'  https://creativecommons.org/licenses/by-nc/4.0/
'  © 2025 Vlad Stefanescu | GreenCoders.net. Attribution required.
' ##########################################################

Imports System.Text
Imports System.Text.RegularExpressions
Imports System.Threading

Module convHistory

    ' ===== ChatMessage Class & Token Helpers =====
    Public Class ChatMessage
        Public Property Role As String
        Public Property Content As String

        Public Sub New(ByVal role As String, ByVal content As String)
            Me.Role = role
            Me.Content = content
        End Sub
    End Class

    Public Function CountTokens(ByVal text As String) As Integer
        If String.IsNullOrEmpty(text) Then Return 0

        ' Approximation: Words + short code tokens (markdown etc.)
        Dim words As Integer = text.Split({" "c, vbTab, vbCrLf, vbLf}, StringSplitOptions.RemoveEmptyEntries).Length
        Dim punctuationWeight As Integer = Regex.Matches(text, "[{}[\]()=<>:;,.!@#%^&*_\-+/\\|~`""']+").Count

        ' Each 4 chars roughly = 1 token in OpenAI; we use a hybrid
        Return words + (text.Length \ 6) + punctuationWeight \ 4
    End Function


    Public Function CalculateTokenCount(text As String) As Integer
        Return CountTokens(text)
    End Function

    ' ===== Helper: Determine if a message is summarizable =====
    Public Function IsMessageSummarizable(msg As Dictionary(Of String, String)) As Boolean
        If msg.ContainsKey("summarizable") Then
            Dim flag As String = msg("summarizable").ToLower()
            Return flag = "true"
        End If
        Return True
    End Function

    ' ===== Summarization Function =====
    Public Async Function SummarizeMessagesAsync(messagesToSummarize As List(Of ChatMessage)) As Task(Of String)
        Dim conversationText As New StringBuilder()
        For Each msg In messagesToSummarize
            conversationText.AppendLine(msg.Role.ToUpper() & ": " & msg.Content)
        Next
        Dim summarizationPrompt As String = "Summarize the following conversation concisely, preserving key information and context. IMPORTANT: Do not alter or remove any content that appears within triple backticks (```), as these may contain code:" & vbCrLf & conversationText.ToString()

        Dim messages As New List(Of Dictionary(Of String, String)) From {
            New Dictionary(Of String, String) From {{"role", "system"}, {"content", "You are an assistant that summarizes conversations."}},
            New Dictionary(Of String, String) From {{"role", "user"}, {"content", summarizationPrompt}}
        }

        Dim summaryResult As String = Await AIcall.CallGPTCore(Config.OpenAiApiKey, Config.AssistantId, messages, 0.7, CancellationToken.None)
        If String.IsNullOrEmpty(summaryResult) Then
            Return "Summary unavailable due to an error."
        End If
        Return summaryResult.Trim()
    End Function

    ' ===== New Asynchronous Trimming Function for Dictionary-based Conversation History =====
    Public Async Function TrimConversationHistoryByTokens_Dict(ByVal convHistory As List(Of Dictionary(Of String, String)), ByVal maxTokens As Integer, ByVal nextUserPrompt As String) As Task
        Dim totalTokens As Integer = convHistory.Sum(Function(msg) CalculateTokenCount(msg("content")))
        Dim userInputTokens As Integer = CalculateTokenCount(nextUserPrompt)
        Dim remainingTokens As Integer = maxTokens - userInputTokens

        ' Identify system message if available.
        Dim systemMessage As Dictionary(Of String, String) = Nothing
        If convHistory.Count > 0 AndAlso convHistory(0)("role").ToLower() = "system" Then
            systemMessage = convHistory(0)
        End If

        ' Determine how many messages to preserve:
        ' - If system message exists, preserve that plus the last two messages.
        ' - Otherwise, preserve the last two messages.
        Dim preserveCount As Integer = If(systemMessage IsNot Nothing, 3, 2)

        ' If we have too few messages, no trimming is needed.
        If convHistory.Count <= preserveCount Then Return

        ' Accumulate messages that will be summarized from the beginning of the history.
        Dim messagesToSummarize As New List(Of Dictionary(Of String, String))()

        ' We target messages from index 1 (if systemMessage exists; else index 0)
        ' up to the message at position (convHistory.Count - preserveCount).
        Dim removalEndIndex As Integer = convHistory.Count - preserveCount
        Dim index As Integer = If(systemMessage IsNot Nothing, 1, 0)

        While totalTokens > remainingTokens AndAlso index < removalEndIndex
            Dim msg As Dictionary(Of String, String) = convHistory(index)
            If IsMessageSummarizable(msg) Then
                messagesToSummarize.Add(msg)
                totalTokens -= CalculateTokenCount(msg("content"))
                convHistory.RemoveAt(index)
                removalEndIndex -= 1   ' Adjust for the shortened list.
                ' Do not increment index—continue at the same position.
            Else
                index += 1
            End If
        End While

        ' If any messages were removed, summarize them and insert the summary into conversation history.
        If messagesToSummarize.Count > 0 Then
            Dim chatMessagesToSummarize As List(Of convHistory.ChatMessage) = ConvertDictListToChatMessages(messagesToSummarize)
            Dim summaryText As String = Await SummarizeMessagesAsync(chatMessagesToSummarize)
            Dim summaryDict As New Dictionary(Of String, String) From {
            {"role", "system"},
            {"content", summaryText},
            {"summarizable", "true"}
        }
            ' Insert the summary right after the system message, if available; otherwise at the beginning.
            If systemMessage IsNot Nothing Then
                convHistory.Insert(1, summaryDict)
            Else
                convHistory.Insert(0, summaryDict)
            End If
        End If
    End Function

    ' ===== Helper Functions for Converting Dictionary Messages =====
    ' Converts a dictionary message to a ChatMessage object.
    Public Function DictToChatMessage(dict As Dictionary(Of String, String)) As ChatMessage
        Return New ChatMessage(dict("role"), dict("content"))
    End Function

    ' Converts a list of dictionary messages into a list of ChatMessage objects.
    Public Function ConvertDictListToChatMessages(dictList As List(Of Dictionary(Of String, String))) As List(Of ChatMessage)
        Dim messages As New List(Of ChatMessage)
        For Each dict In dictList
            messages.Add(DictToChatMessage(dict))
        Next
        Return messages
    End Function

End Module

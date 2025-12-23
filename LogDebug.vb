' ###  DEBUG ###

' ##########################################################
'  Shelly - v1.0.1
'  License: Creative Commons Attribution-NonCommercial (CC BY-NC)
'  https://creativecommons.org/licenses/by-nc/4.0/
'  © 2025 Vlad Stefanescu | GreenCoders.net. Attribution required.
' ##########################################################

Imports System.Net.Http
Imports System.Text
Imports System.Text.RegularExpressions
Imports System.Threading
Imports Newtonsoft.Json.Linq

Module LogDebug


    Public Sub LogDebugInformation(userQuestion As String,
                                      conversationHistory As List(Of Dictionary(Of String, String)),
                                      aiResponse As String,
                                      tokensSent As Integer,
                                      tokensReceived As Integer)
        ' Extract user question if missing.
        Dim actualUserPrompt As String = userQuestion
        If String.IsNullOrWhiteSpace(actualUserPrompt) Then
            For i As Integer = conversationHistory.Count - 1 To 0 Step -1
                If String.Equals(conversationHistory(i)("role"), "user", StringComparison.OrdinalIgnoreCase) Then
                    actualUserPrompt = conversationHistory(i)("content")
                    Exit For
                End If
            Next
        End If

        ' Build debug message with added timestamp.
        Dim logBuilder As New System.Text.StringBuilder()
        logBuilder.AppendLine("======================================")
        logBuilder.AppendLine("          DEBUG INFORMATION           ")
        logBuilder.AppendLine("======================================")
        logBuilder.AppendLine($"Timestamp: {DateTime.Now}")
        logBuilder.AppendLine($"User Prompt: {actualUserPrompt}")
        logBuilder.AppendLine("")
        logBuilder.AppendLine("----- Conversation History -----")
        For Each msg In conversationHistory
            logBuilder.AppendLine($"{msg("role").ToUpper()}: {msg("content")}")
        Next
        logBuilder.AppendLine("--------------------------------")
        logBuilder.AppendLine("")
        logBuilder.AppendLine("----- AI Response -----")
        logBuilder.AppendLine($"AI: {aiResponse}")
        logBuilder.AppendLine("--------------------------------")
        logBuilder.AppendLine("")
        logBuilder.AppendLine("----- Token Counts -----")
        logBuilder.AppendLine($"Tokens Sent: {tokensSent}")
        logBuilder.AppendLine($"Tokens Received: {tokensReceived}")
        logBuilder.AppendLine("--------------------------------")
        logBuilder.AppendLine("=== END OF DEBUG INFORMATION ===")

        ' Save log message and update UI.
        Globals.AppendDebugLog(logBuilder.ToString())
    End Sub


End Module

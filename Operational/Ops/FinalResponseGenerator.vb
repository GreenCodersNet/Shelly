Imports System.Text
Imports System.Threading
Imports System.Linq
Imports System.IO

''' <summary>
''' Generates natural, conversational final responses from technical execution summaries.
''' NOTE: LocalAI+Piper voice summaries are now handled directly in HandleUserRequest via LocalAISummaryService.
''' This module provides cloud-based text rephrasing as a fallback only.
''' </summary>
Public Module FinalResponseGenerator

    ''' <summary>
    ''' Generates a natural AI assistant response from step summaries using cloud AI.
    ''' Token-efficient: Uses compact summaries instead of full outputs.
    ''' Does NOT trigger TTS - for voice summaries use LocalAISummaryService directly.
    ''' </summary>
    Public Async Function GenerateNaturalResponse(
        originalUserRequest As String,
        summaries As List(Of StepExecutionSummary),
        ct As CancellationToken
    ) As Task(Of String)
        
        Try
            ' Build compact context for AI (include full outputs only when large)
            Dim compactContext = BuildCompactContext(summaries, includeFullOutputs:=True, includeFullOutputsMinChars:=400)
            
            ' Create rephrasing prompt
            Dim systemPrompt = 
                "You are Shelly, a helpful AI assistant like Siri or Alexa. " &
                "Your task is to write a friendly, conversational response summarizing what you accomplished for the user. " &
                vbLf & vbLf &
                "CRITICAL RULES - BE A NATURAL ASSISTANT:" & vbLf &
                "1. NEVER mention technical terms: 'PowerShell', 'script', 'executed', 'iteration', 'tool'" & vbLf &
                "2. NEVER say 'I ran a PowerShell script' or 'I executed a command'" & vbLf &
                "3. NEVER mention running anything twice or multiple times" & vbLf &
                "4. Just state the RESULT naturally: 'It's 11:15 PM' or 'I found the file at...'" & vbLf &
                "5. Sound like Siri/Alexa - natural, friendly, direct" & vbLf &
                "6. DO NOT repeat large content that was already shown" & vbLf &
                "7. Keep response SHORT and conversational" & vbLf &
                "8. Focus on WHAT you accomplished, not HOW" & vbLf &
                "9. NEVER GUESS or make up information - only use what's in the context below!" & vbLf &
                "10. If the content was already displayed, just say 'I've shown you the content above'"
            
            Dim userPrompt = 
                $"The user asked:{vbLf}""{originalUserRequest}""{vbLf}{vbLf}" &
                $"Here's what you accomplished:{vbLf}{compactContext}{vbLf}{vbLf}" &
                "Write a SHORT, natural response. " &
                "IMPORTANT: Do NOT guess about file contents - if the content was already shown to the user, just acknowledge it briefly."
            
            Dim messages As New List(Of Dictionary(Of String, String)) From {
                New Dictionary(Of String, String) From {{"role", "system"}, {"content", systemPrompt}},
                New Dictionary(Of String, String) From {{"role", "user"}, {"content", userPrompt}}
            }
            
            Dim finalResponse = Await AIcall.CallGPTCore(
                Globals.UserApiKey,
                Globals.AiModelSelection,
                messages,
                0.7,
                ct
            )
            
            Debug.WriteLine($"[FinalResponseGenerator] Generated response: {finalResponse.Length} chars")
            Return finalResponse
            
        Catch ex As Exception
            Debug.WriteLine($"[FinalResponseGenerator] Error: {ex.Message}")
            Return GenerateFallbackResponse(summaries)
        End Try
    End Function
    
    ''' <summary>
    ''' Builds compact context from summaries (token-efficient)
    ''' </summary>
    Private Function BuildCompactContext(summaries As List(Of StepExecutionSummary), Optional includeFullOutputs As Boolean = False, Optional includeFullOutputsMinChars As Integer = 0) As String
        If summaries Is Nothing OrElse summaries.Count = 0 Then
            Return "<No steps completed>"
        End If

        Dim sb As New StringBuilder()
        For Each summary In summaries
            sb.Append(summary.GetCompactRepresentation())
            If includeFullOutputs AndAlso Not String.IsNullOrWhiteSpace(summary.FullOutput) AndAlso summary.FullOutput.Length >= includeFullOutputsMinChars Then
                sb.AppendLine("Full output:")
                sb.AppendLine(summary.FullOutput)
            End If
        Next
        Return sb.ToString()
    End Function
    
    ''' <summary>
    ''' Fallback response if AI call fails
    ''' </summary>
    Private Function GenerateFallbackResponse(summaries As List(Of StepExecutionSummary)) As String
        If summaries Is Nothing OrElse summaries.Count = 0 Then
            Return "I completed your request."
        End If
        
        Dim sb As New StringBuilder()
        If summaries.Count = 1 Then
            sb.Append(summaries(0).ShortDescription)
        Else
            sb.AppendLine("I've completed the following for you:")
            sb.AppendLine()
            For i = 0 To Math.Min(summaries.Count - 1, 4)
                sb.AppendLine($"• {summaries(i).ShortDescription}")
            Next
            If summaries.Count > 5 Then
                sb.AppendLine($"... and {summaries.Count - 5} more steps")
            End If
        End If
        Return sb.ToString()
    End Function
    
    ''' <summary>
    ''' Gets a brief summary of what was accomplished (for logging)
    ''' </summary>
    Public Function GetBriefSummary(summaries As List(Of StepExecutionSummary)) As String
        If summaries Is Nothing OrElse summaries.Count = 0 Then
            Return "No steps completed"
        End If
        
        Dim successCount = summaries.Where(Function(s) s.Status = OutcomeStatus.Success).Count()
        Dim toolNames = summaries.Select(Function(s) s.ToolName).Distinct().ToList()
        Return $"{successCount} step(s) completed using: {String.Join(", ", toolNames)}"
    End Function
    
End Module

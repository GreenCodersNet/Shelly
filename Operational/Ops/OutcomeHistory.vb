' ###  OutcomeHistory.vb - v2.0.0 ###

' ##########################################################
'  Shelly - v2.0.0
'  License: Creative Commons Attribution-NonCommercial (CC BY-NC)
'  https://creativecommons.org/licenses/by-nc/4.0/
'  © 2025 Vlad Stefanescu | GreenCoders.net. Attribution required.
' ##########################################################

Imports System.Text
Imports System.Linq
Imports Newtonsoft.Json

Public Module OutcomeHistory

    Private Const OutcomeMarker As String = "[PowerShell Outcome]"

    ''' <summary>
    ''' Builds a digest of recent outcomes for the AI planner
    ''' Enhanced version that uses GlobalOutcomeTracker for structured data
    ''' </summary>
    Public Function BuildRecentOutcomeDigest(history As List(Of Dictionary(Of String, String)), Optional maxEntries As Integer = 5) As String
        ' Try to use GlobalOutcomeTracker first (new system)
        Dim recent = GlobalOutcomeTracker.Instance.GetRecentOutcomes(maxEntries)
        
        If recent.Count > 0 Then
            Return BuildStructuredOutcomeDigest(recent)
        End If
        
        ' Fallback to legacy conversation history parsing
        If history Is Nothing OrElse history.Count = 0 Then
            Return String.Empty
        End If

        Dim legacyRecent = history.Where(Function(msg) msg.ContainsKey("role") AndAlso msg("role").Equals("system", StringComparison.OrdinalIgnoreCase) _
                                         AndAlso msg.ContainsKey("content") AndAlso msg("content").Contains(OutcomeMarker)) _
                               .TakeLast(maxEntries)

        If Not legacyRecent.Any() Then
            Return String.Empty
        End If

        Dim sb As New StringBuilder()
        For Each msg In legacyRecent
            sb.AppendLine(msg("content").Trim())
            sb.AppendLine()
        Next

        Return sb.ToString().Trim()
    End Function
    
    ''' <summary>
    ''' Builds a structured digest from ExecutionOutcome objects
    ''' </summary>
    Private Function BuildStructuredOutcomeDigest(outcomes As List(Of ExecutionOutcome)) As String
        If outcomes Is Nothing OrElse outcomes.Count = 0 Then
            Return String.Empty
        End If
        
        Dim sb As New StringBuilder()
        sb.AppendLine("=== RECENT EXECUTION OUTCOMES ===")
        
        For i = 0 To outcomes.Count - 1
            Dim outcome = outcomes(i)
            
            ' Build outcome summary
            sb.AppendLine($"{i + 1}. {outcome.ToolName} | Status: {outcome.Status}")
            
            ' Include attempt and policy flags
            If outcome.AttemptNumber > 0 Then
                sb.AppendLine($"   Attempt: {outcome.AttemptNumber}")
            End If
            If outcome.PolicyBlocked Then
                sb.AppendLine("   Policy: Blocked")
            End If
            If outcome.ExitCode <> 0 AndAlso outcome.IsPowerShell Then
                sb.AppendLine($"   ExitCode: {outcome.ExitCode}")
            End If
            
            ' Include arguments for context
            If outcome.Arguments IsNot Nothing AndAlso outcome.Arguments.Count > 0 Then
                Try
                    Dim argsJson = JsonConvert.SerializeObject(outcome.Arguments)
                    ' Truncate long arguments
                    If argsJson.Length > 150 Then
                        argsJson = argsJson.Substring(0, 150) & "..."
                    End If
                    sb.AppendLine($"   Args: {argsJson}")
                Catch
                    ' Ignore JSON serialization errors
                End Try
            End If
            
            If Not String.IsNullOrWhiteSpace(outcome.ArgsHash) Then
                sb.AppendLine($"   ArgsHash: {outcome.ArgsHash}")
            End If
            
            ' Include output preview if successful
            If outcome.IsSuccess AndAlso Not String.IsNullOrWhiteSpace(outcome.StandardOutput) Then
                Dim preview = outcome.StandardOutput.Substring(0, Math.Min(200, outcome.StandardOutput.Length))
                If outcome.StandardOutput.Length > 200 Then preview &= "..."
                sb.AppendLine($"   Output: {preview}")
            End If
            
            ' Include errors if failed
            If Not outcome.IsSuccess AndAlso Not String.IsNullOrWhiteSpace(outcome.StandardError) Then
                Dim errorPreview = outcome.StandardError.Substring(0, Math.Min(150, outcome.StandardError.Length))
                If outcome.StandardError.Length > 150 Then errorPreview &= "..."
                sb.AppendLine($"   Error: {errorPreview}")
            End If
            
            ' Include validation diagnostics if applicable
            If outcome.ValidationResult IsNot Nothing AndAlso Not outcome.ValidationResult.Passed Then
                sb.AppendLine($"   Validation Failed:")
                For Each diag In outcome.ValidationResult.Diagnostics.Take(3)
                    sb.AppendLine($"     - {diag}")
                Next
            End If
            
            ' Duration info
            sb.AppendLine($"   Duration: {outcome.Duration.TotalSeconds:F2}s")
            sb.AppendLine()
        Next
        
        ' Add summary statistics
        Dim successCount = outcomes.Where(Function(o) o.IsSuccess).Count()
        Dim failCount = outcomes.Where(Function(o) Not o.IsSuccess).Count()
        
        sb.AppendLine("Summary:")
        sb.AppendLine($"  Successful: {successCount}/{outcomes.Count}")
        sb.AppendLine($"  Failed: {failCount}/{outcomes.Count}")
        
        ' Add retry guidance if there are failures
        If failCount > 0 Then
            Dim retryGuidance = GoalValidator.GetRetryGuidance()
            If Not String.IsNullOrWhiteSpace(retryGuidance) Then
                sb.AppendLine()
                sb.AppendLine(retryGuidance)
            End If
        End If
        
        Return sb.ToString()
    End Function
    
    ''' <summary>
    ''' Builds a compact outcome digest for token-constrained scenarios
    ''' </summary>
    Public Function BuildCompactOutcomeDigest(lastN As Integer) As String
        Dim recent = GlobalOutcomeTracker.Instance.GetRecentOutcomes(lastN)
        
        If recent.Count = 0 Then
            Return String.Empty
        End If
        
        Dim sb As New StringBuilder()
        sb.AppendLine("=== OUTCOMES ===")
        
        For i = 0 To recent.Count - 1
            Dim outcome = recent(i)
            Dim statusIcon = If(outcome.IsSuccess, "?", "?")
            sb.AppendLine($"{statusIcon} {outcome.ToolName}")
            
            If Not outcome.IsSuccess AndAlso Not String.IsNullOrWhiteSpace(outcome.StandardError) Then
                Dim errorBrief = outcome.StandardError.Substring(0, Math.Min(80, outcome.StandardError.Length))
                sb.AppendLine($"  Error: {errorBrief}")
            End If
        Next
        
        Return sb.ToString()
    End Function

    ''' <summary>
    ''' Returns tool names from recent outcomes that succeeded (Status: Success).
    ''' </summary>
    Public Function GetSuccessfulToolNames(history As List(Of Dictionary(Of String, String)), Optional maxEntries As Integer = 10) As HashSet(Of String)
        Dim result As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
        
        ' Try GlobalOutcomeTracker first
        Dim recent = GlobalOutcomeTracker.Instance.GetRecentOutcomes(maxEntries)
        If recent.Count > 0 Then
            For Each outcome In recent.Where(Function(o) o.IsSuccess)
                result.Add(outcome.ToolName)
            Next
            Return result
        End If
        
        ' Fallback to legacy parsing
        If history Is Nothing OrElse history.Count = 0 Then
            Return result
        End If

        Dim legacyRecent = history.Where(Function(msg) msg.ContainsKey("role") AndAlso msg("role").Equals("system", StringComparison.OrdinalIgnoreCase) _
                                         AndAlso msg.ContainsKey("content") AndAlso msg("content").Contains(OutcomeMarker) _
                                         AndAlso msg("content").Contains("Status: Success")) _
                               .TakeLast(maxEntries)

        For Each msg In legacyRecent
            ' Extract tool name if present (format: "Tool: <name>")
            Dim match = System.Text.RegularExpressions.Regex.Match(msg("content"), "Tool:\s*(\w+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase)
            If match.Success Then
                result.Add(match.Groups(1).Value)
            End If
        Next

        Return result
    End Function

    ''' <summary>
    ''' Checks if a specific tool with matching arguments was already executed successfully.
    ''' </summary>
    Public Function WasToolSuccessful(history As List(Of Dictionary(Of String, String)), toolName As String, argsSignature As String) As Boolean
        ' Try GlobalOutcomeTracker first
        Dim outcomes = GlobalOutcomeTracker.Instance.GetOutcomesByTool(toolName)
        If outcomes.Count > 0 Then
            Return outcomes.Any(Function(o) o.IsSuccess)
        End If
        
        ' Fallback to legacy parsing
        If history Is Nothing OrElse history.Count = 0 Then
            Return False
        End If

        Dim searchPattern = $"Tool: {toolName}"
        Return history.Any(Function(msg) msg.ContainsKey("role") AndAlso msg("role").Equals("system", StringComparison.OrdinalIgnoreCase) _
                                         AndAlso msg.ContainsKey("content") AndAlso msg("content").Contains(OutcomeMarker) _
                                         AndAlso msg("content").Contains("Status: Success") _
                                         AndAlso msg("content").Contains(searchPattern))
    End Function
    
    ''' <summary>
    ''' Gets a list of failed tool names for quick reference
    ''' </summary>
    Public Function GetFailedToolNames() As List(Of String)
        Dim recent = GlobalOutcomeTracker.Instance.GetRecentOutcomes(10)
        Return recent.Where(Function(o) Not o.IsSuccess) _
                    .Select(Function(o) o.ToolName) _
                    .Distinct() _
                    .ToList()
    End Function
    
    ''' <summary>
    ''' Checks if a specific tool has been failing repeatedly
    ''' </summary>
    Public Function IsToolRepeatedlyFailing(toolName As String, threshold As Integer) As Boolean
        Dim recent = GlobalOutcomeTracker.Instance.GetOutcomesByTool(toolName)
        
        If recent.Count < threshold Then
            Return False
        End If
        
        ' Check last N attempts
        Dim lastAttempts = recent.TakeLast(threshold)
        Return lastAttempts.All(Function(o) Not o.IsSuccess)
    End Function

End Module

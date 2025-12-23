' ###  RetryStrategy.vb - v2.0.0 ### 

' ##########################################################
'  Shelly - v2.0.0
'  License: Creative Commons Attribution-NonCommercial (CC BY-NC)
'  https://creativecommons.org/licenses/by-nc/4.0/
'  © 2025 Vlad Stefanescu | GreenCoders.net. Attribution required.
' ##########################################################

Imports System.Text

''' <summary>
''' Module to manage retry attempts for tools and prevent infinite loops
''' </summary>
Public Module RetryStrategy

    ' Track retry attempts per tool
    Private ReadOnly retryCount As New Dictionary(Of String, Integer)
    Private ReadOnly retryLock As New Object()
    
    Public Const MaxRetriesPerTool As Integer = 3
    
    ''' <summary>
    ''' Checks if a tool can be retried
    ''' </summary>
    Public Function CanRetry(toolName As String) As Boolean
        SyncLock retryLock
            If Not retryCount.ContainsKey(toolName) Then
                retryCount(toolName) = 0
            End If
            
            Dim result = retryCount(toolName) < MaxRetriesPerTool
            Debug.WriteLine($"[RetryStrategy] {toolName} can retry: {result} (attempts: {retryCount(toolName)}/{MaxRetriesPerTool})")
            Return result
        End SyncLock
    End Function
    
    ''' <summary>
    ''' Records a retry attempt
    ''' </summary>
    Public Sub RecordRetry(toolName As String)
        SyncLock retryLock
            If Not retryCount.ContainsKey(toolName) Then
                retryCount(toolName) = 0
            End If
            
            retryCount(toolName) += 1
            Debug.WriteLine($"[RetryStrategy] {toolName} retry #{retryCount(toolName)}/{MaxRetriesPerTool}")
        End SyncLock
    End Sub
    
    ''' <summary>
    ''' Resets retry counters for new request
    ''' </summary>
    Public Sub Reset()
        SyncLock retryLock
            retryCount.Clear()
            Debug.WriteLine("[RetryStrategy] Reset retry counters")
        End SyncLock
    End Sub
    
    ''' <summary>
    ''' Gets retry statistics
    ''' </summary>
    Public Function GetRetryStats() As String
        SyncLock retryLock
            If retryCount.Count = 0 Then
                Return "No retries performed."
            End If
            
            Dim sb As New StringBuilder()
            sb.AppendLine("RETRY STATISTICS:")
            
            For Each kvp In retryCount.OrderByDescending(Function(k) k.Value)
                sb.AppendLine($"  {kvp.Key}: {kvp.Value} retries")
            Next
            
            Return sb.ToString()
        End SyncLock
    End Function
    
    ''' <summary>
    ''' Gets remaining retry attempts for a tool
    ''' </summary>
    Public Function GetRemainingRetries(toolName As String) As Integer
        SyncLock retryLock
            If Not retryCount.ContainsKey(toolName) Then
                Return MaxRetriesPerTool
            End If
            
            Return Math.Max(0, MaxRetriesPerTool - retryCount(toolName))
        End SyncLock
    End Function
    
    ''' <summary>
    ''' Checks if any tool has exhausted retries
    ''' </summary>
    Public Function HasExhaustedRetries() As Boolean
        SyncLock retryLock
            Return retryCount.Any(Function(kvp) kvp.Value >= MaxRetriesPerTool)
        End SyncLock
    End Function
    
    ''' <summary>
    ''' Gets tools that have exhausted their retries
    ''' </summary>
    Public Function GetExhaustedTools() As List(Of String)
        SyncLock retryLock
            Return retryCount.Where(Function(kvp) kvp.Value >= MaxRetriesPerTool) _
                            .Select(Function(kvp) kvp.Key) _
                            .ToList()
        End SyncLock
    End Function

    ''' <summary>
    ''' Gets remaining retries per tool (only tools seen so far)
    ''' </summary>
    Public Function GetRemainingStats() As String
        SyncLock retryLock
            If retryCount.Count = 0 Then
                Return "No retries performed."
            End If

            Dim sb As New StringBuilder()
            sb.AppendLine("REMAINING RETRIES:")
            For Each kvp In retryCount.OrderByDescending(Function(k) k.Value)
                Dim remaining = Math.Max(0, MaxRetriesPerTool - kvp.Value)
                sb.AppendLine($"  {kvp.Key}: {remaining} left of {MaxRetriesPerTool}")
            Next
            Return sb.ToString()
        End SyncLock
    End Function

End Module

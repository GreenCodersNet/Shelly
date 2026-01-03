Imports System.Text
Imports System.Collections.Concurrent
Imports Newtonsoft.Json
Imports System.IO
Imports System.Linq

''' <summary>
''' Represents the output from a single plan step execution
''' </summary>
Public Class StepOutput
    Public Property RequestId As String = ""
    Public Property Iteration As Integer
    Public Property StepIndex As Integer
    Public Property ToolName As String = ""
    Public Property Arguments As Dictionary(Of String, Object)
    Public Property Output As String = ""
    Public Property Status As OutcomeStatus
    Public Property Timestamp As DateTimeOffset = DateTimeOffset.UtcNow
    Public Property ExecutionTimeMs As Long = 0
    
    ''' <summary>
    ''' Returns a human-readable summary of this step output
    ''' </summary>
    Public Function GetSummary() As String
        Dim statusIcon = If(Status = OutcomeStatus.Success, "?", "?")
        Dim truncatedOutput = If(Output.Length > 100, Output.Substring(0, 97) & "...", Output)
        Return $"{statusIcon} {ToolName}: {truncatedOutput}"
    End Function
    
    ''' <summary>
    ''' Returns formatted output suitable for AI planner context
    ''' </summary>
    Public Function GetFormattedOutput() As String
        Dim sb As New StringBuilder()
        sb.AppendLine($"Step {StepIndex}: {ToolName} ({Status})")
        
        If Status = OutcomeStatus.Success Then
            sb.AppendLine($"Output: {Output}")
        Else
            sb.AppendLine($"Error: {Output}")
        End If
        
        Return sb.ToString()
    End Function
End Class

''' <summary>
''' Thread-safe manager for storing and retrieving step execution outputs
''' Used to pass context between iterations in the intelligent retry loop
''' </summary>
Public NotInheritable Class StepOutputManager
    
    Private Shared ReadOnly _instance As New Lazy(Of StepOutputManager)(Function() New StepOutputManager())
    Public Shared ReadOnly Property Instance As StepOutputManager
        Get
            Return _instance.Value
        End Get
    End Property
    
    ' Thread-safe storage: Key = "requestId_iteration_stepIndex"
    Private ReadOnly _outputs As New ConcurrentDictionary(Of String, StepOutput)()
    Private ReadOnly _persistencePath As String = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", "stepoutputs.json")
    
    ' NEW: Lightweight summaries for final AI rephrasing (token-efficient)
    Private ReadOnly _stepSummaries As New List(Of StepExecutionSummary)
    Private ReadOnly _summariesLock As New Object()
    
    ' Current session tracking
    Private _currentRequestId As String = ""
    Private _currentIteration As Integer = 0
    
    Private Sub New()
        ' Private constructor for singleton
    End Sub
    
    ''' <summary>
    ''' Starts a new request session
    ''' </summary>
    Public Sub StartNewRequest(requestId As String)
        _currentRequestId = requestId
        _currentIteration = 0
        
        ' Clear summaries for new request
        SyncLock _summariesLock
            _stepSummaries.Clear()
        End SyncLock
        
        Debug.WriteLine($"[StepOutputManager] Started new request: {requestId}")
    End Sub
    
    ''' <summary>
    ''' Increments iteration counter
    ''' </summary>
    Public Sub IncrementIteration()
        _currentIteration += 1
        Debug.WriteLine($"[StepOutputManager] Iteration incremented to: {_currentIteration}")
    End Sub
    
    ''' <summary>
    ''' Records the output from a step execution (overload for simpler signature)
    ''' </summary>
    Public Sub RecordStepOutput(stepIndex As Integer, toolName As String, outputText As String, success As Boolean, Optional shortSummary As String = Nothing)
        Try
            Dim stepOutput As New StepOutput With {
                .RequestId = _currentRequestId,
                .Iteration = _currentIteration,
                .StepIndex = stepIndex,
                .ToolName = toolName,
                .Arguments = New Dictionary(Of String, Object)(),
                .Output = outputText,
                .Status = If(success, OutcomeStatus.Success, OutcomeStatus.Failed),
                .Timestamp = DateTimeOffset.UtcNow
            }

            RecordOutput(_currentIteration, stepIndex, stepOutput, shortSummary)

        Catch ex As Exception
            Debug.WriteLine($"[StepOutputManager] Error in RecordStepOutput: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Records the output from a step execution
    ''' </summary>
    Public Sub RecordOutput(iteration As Integer, stepIndex As Integer, output As StepOutput, Optional shortSummary As String = Nothing)
        Try
            output.Iteration = iteration
            output.StepIndex = stepIndex
            output.RequestId = _currentRequestId

            Dim key = BuildKey(_currentRequestId, iteration, stepIndex)
            _outputs(key) = output

            Debug.WriteLine($"[StepOutputManager] Recorded output for {key}: {output.ToolName} -> {output.Status}")

            Dim success As Boolean = (output.Status = OutcomeStatus.Success)
            Dim summary = ContentClassifier.ClassifyStepOutput(output.ToolName, output.Output, success, output.Arguments)
            summary.StepIndex = stepIndex
            If Not String.IsNullOrWhiteSpace(shortSummary) Then
                summary.ShortSummary = shortSummary
            End If

            SyncLock _summariesLock
                _stepSummaries.Add(summary)
            End SyncLock

            Debug.WriteLine($"[StepOutputManager] Classified as {summary.ContentType}: {summary.ShortDescription}")

            SaveToDisk()
        Catch ex As Exception
            Debug.WriteLine($"[StepOutputManager] Error recording output: {ex.Message}")
        End Try
    End Sub

    Public Sub UpdateShortSummary(stepIndex As Integer, shortSummary As String)
        If String.IsNullOrWhiteSpace(shortSummary) Then Return
        SyncLock _summariesLock
            Dim target = _stepSummaries.LastOrDefault(Function(s) s.StepIndex = stepIndex)
            If target IsNot Nothing Then
                target.ShortSummary = shortSummary
            End If
        End SyncLock
    End Sub

    ''' <summary>
    ''' Gets all outputs for a specific iteration
    ''' </summary>
    Public Function GetOutputsForIteration(requestId As String, iteration As Integer) As List(Of StepOutput)
        Try
            Return _outputs.Values _
                .Where(Function(o) o.RequestId = requestId AndAlso o.Iteration = iteration) _
                .OrderBy(Function(o) o.StepIndex) _
                .ToList()
        Catch ex As Exception
            Debug.WriteLine($"[StepOutputManager] Error getting iteration outputs: {ex.Message}")
            Return New List(Of StepOutput)()
        End Try
    End Function
    
    ''' <summary>
    ''' Gets all outputs for the current request
    ''' </summary>
    Public Function GetAllOutputs() As List(Of StepOutput)
        Try
            Return _outputs.Values _
                .Where(Function(o) o.RequestId = _currentRequestId) _
                .OrderBy(Function(o) o.Iteration) _
                .ThenBy(Function(o) o.StepIndex) _
                .ToList()
        Catch ex As Exception
            Debug.WriteLine($"[StepOutputManager] Error getting all outputs: {ex.Message}")
            Return New List(Of StepOutput)()
        End Try
    End Function
    
    ''' <summary>
    ''' Gets the most recent N outputs across all iterations
    ''' </summary>
    Public Function GetRecentOutputs(count As Integer) As List(Of StepOutput)
        Try
            Return _outputs.Values _
                .Where(Function(o) o.RequestId = _currentRequestId) _
                .OrderByDescending(Function(o) o.Timestamp) _
                .Take(count) _
                .OrderBy(Function(o) o.Iteration) _
                .ThenBy(Function(o) o.StepIndex) _
                .ToList()
        Catch ex As Exception
            Debug.WriteLine($"[StepOutputManager] Error getting recent outputs: {ex.Message}")
            Return New List(Of StepOutput)()
        End Try
    End Function
    
    ''' <summary>
    ''' Builds formatted context summary for AI planner
    ''' This is the KEY method that provides context to the next iteration
    ''' </summary>
    Public Function BuildContextSummary() As String
        Try
            Dim allOutputs = GetAllOutputs()
            
            If allOutputs.Count = 0 Then
                Return "<No previous outputs yet>"
            End If
            
            Dim sb As New StringBuilder()
            sb.AppendLine("=== PREVIOUS STEP OUTPUTS ===")
            sb.AppendLine()
            
            ' Group by iteration
            Dim groupedByIteration = allOutputs.GroupBy(Function(o) o.Iteration)
            
            For Each iterGroup In groupedByIteration
                sb.AppendLine($"--- Iteration {iterGroup.Key} ---")
                
                For Each output In iterGroup.OrderBy(Function(o) o.StepIndex)
                    sb.AppendLine(output.GetFormattedOutput())
                    sb.AppendLine()
                Next
            Next
            
            sb.AppendLine("=== END PREVIOUS OUTPUTS ===")
            
            Dim summary = sb.ToString()
            Debug.WriteLine($"[StepOutputManager] Built context summary: {summary.Length} chars")
            
            Return summary
            
        Catch ex As Exception
            Debug.WriteLine($"[StepOutputManager] Error building context summary: {ex.Message}")
            Return "<Error building context summary>"
        End Try
    End Function
    
    ''' <summary>
    ''' Builds compact summary focusing on latest iteration
    ''' </summary>
    Public Function BuildLatestIterationSummary() As String
        Try
            Dim latestOutputs = GetOutputsForIteration(_currentRequestId, _currentIteration)
            
            If latestOutputs.Count = 0 Then
                Return "<No outputs in latest iteration>"
            End If
            
            Dim sb As New StringBuilder()
            sb.AppendLine($"=== LATEST ITERATION ({_currentIteration}) OUTPUTS ===")
            sb.AppendLine()
            
            For Each output In latestOutputs
                sb.AppendLine(output.GetFormattedOutput())
                sb.AppendLine()
            Next
            
            Return sb.ToString()
            
        Catch ex As Exception
            Debug.WriteLine($"[StepOutputManager] Error building latest summary: {ex.Message}")
            Return "<Error building latest summary>"
        End Try
    End Function
    
    ''' <summary>
    ''' Extracts file paths from outputs (useful for detecting lists)
    ''' </summary>
    Public Function ExtractFilePaths() As List(Of String)
        Try
            Dim paths As New List(Of String)()
            Dim filePathPattern = "(?i)(?:[a-z]:\\|\\\\)(?:[^\\/:*?""<>|\r\n]+\\)*[^\\/:*?""<>|\r\n]*"
            
            For Each output In GetAllOutputs()
                If output.Status = OutcomeStatus.Success Then
                    Dim matches = Text.RegularExpressions.Regex.Matches(output.Output, filePathPattern)
                    For Each match As Text.RegularExpressions.Match In matches
                        If Not String.IsNullOrWhiteSpace(match.Value) Then
                            paths.Add(match.Value)
                        End If
                    Next
                End If
            Next
            
            Return paths.Distinct().ToList()
            
        Catch ex As Exception
            Debug.WriteLine($"[StepOutputManager] Error extracting file paths: {ex.Message}")
            Return New List(Of String)()
        End Try
    End Function
    
    ''' <summary>
    ''' Detects if any output contains a list of items
    ''' </summary>
    Public Function DetectListOutput() As Boolean
        Try
            For Each output In GetAllOutputs()
                If output.Status = OutcomeStatus.Success Then
                    ' Common list indicators
                    If output.Output.Contains(Environment.NewLine) AndAlso
                       (output.Output.Contains("Found") OrElse
                        output.Output.Contains("Matched") OrElse
                        output.Output.Contains("files:") OrElse
                        output.ToolName.Contains("Search")) Then
                        Return True
                    End If
                End If
            Next
            
            Return False
            
        Catch ex As Exception
            Debug.WriteLine($"[StepOutputManager] Error detecting list: {ex.Message}")
            Return False
        End Try
    End Function
    
    ' ========== NEW: SUMMARY METHODS FOR FINAL AI REPHRASING ==========
    
    ''' <summary>
    ''' Gets lightweight summaries for final AI rephrasing call (token-efficient)
    ''' Only includes successful steps to keep response positive and focused
    ''' </summary>
    Public Function GetStepSummariesForRephrasing() As List(Of StepExecutionSummary)
        Try
            SyncLock _summariesLock
                Return _stepSummaries _
                    .Where(Function(s) s.Status = OutcomeStatus.Success) _
                    .OrderBy(Function(s) s.StepIndex) _
                    .ToList()
            End SyncLock
        Catch ex As Exception
            Debug.WriteLine($"[StepOutputManager] Error getting summaries: {ex.Message}")
            Return New List(Of StepExecutionSummary)()
        End Try
    End Function
    
    ''' <summary>
    ''' Gets all summaries including failures (for error reporting)
    ''' </summary>
    Public Function GetAllSummaries() As List(Of StepExecutionSummary)
        Try
            SyncLock _summariesLock
                Return _stepSummaries.OrderBy(Function(s) s.StepIndex).ToList()
            End SyncLock
        Catch ex As Exception
            Debug.WriteLine($"[StepOutputManager] Error getting all summaries: {ex.Message}")
            Return New List(Of StepExecutionSummary)()
        End Try
    End Function
    
    ''' <summary>
    ''' Builds a compact context string for final AI rephrasing
    ''' Uses summaries instead of full outputs to minimize tokens
    ''' </summary>
    Public Function BuildCompactContextForRephrasing(Optional includeFullOutputs As Boolean = False) As String
        Try
            Dim summaries = GetStepSummariesForRephrasing()
            If summaries.Count = 0 Then
                Return "<No completed steps>"
            End If

            Dim sb As New StringBuilder()
            sb.AppendLine("Steps completed for the user:")
            sb.AppendLine()

            For Each summary In summaries
                sb.Append(summary.GetCompactRepresentation())
                If includeFullOutputs AndAlso Not String.IsNullOrWhiteSpace(summary.FullOutput) Then
                    sb.AppendLine("  Full output:")
                    sb.AppendLine(summary.FullOutput)
                End If
            Next

            Return sb.ToString()

        Catch ex As Exception
            Debug.WriteLine($"[StepOutputManager] Error building compact context: {ex.Message}")
            Return "<Error building context>"
        End Try
    End Function
    
    ''' <summary>
    ''' Clears all stored outputs for current request
    ''' </summary>
    Public Sub Clear()
        Try
            Dim keysToRemove = _outputs.Keys.Where(Function(k) k.StartsWith(_currentRequestId)).ToList()
            
            For Each key In keysToRemove
                Dim removed As StepOutput = Nothing
                _outputs.TryRemove(key, removed)
            Next
            
            ' Clear summaries too
            SyncLock _summariesLock
                _stepSummaries.Clear()
            End SyncLock
            
            SaveToDisk()
            
            Debug.WriteLine($"[StepOutputManager] Cleared {keysToRemove.Count} outputs for request {_currentRequestId}")
            
        Catch ex As Exception
            Debug.WriteLine($"[StepOutputManager] Error clearing outputs: {ex.Message}")
        End Try
    End Sub
    
    ''' <summary>
    ''' Clears ALL outputs from all requests (use sparingly)
    ''' </summary>
    Public Sub ClearAll()
        Try
            _outputs.Clear()
            
            ' Clear summaries too
            SyncLock _summariesLock
                _stepSummaries.Clear()
            End SyncLock
            
            _currentRequestId = ""
            _currentIteration = 0
            SaveToDisk()
            Debug.WriteLine("[StepOutputManager] Cleared ALL outputs")
        Catch ex As Exception
            Debug.WriteLine($"[StepOutputManager] Error clearing all outputs: {ex.Message}")
        End Try
    End Sub
    
    Public Sub SaveToDisk()
        Try
            Directory.CreateDirectory(Path.GetDirectoryName(_persistencePath))
            Dim listOutputs = _outputs.Values.OrderBy(Function(o) o.Timestamp).ToList()
            Dim json = JsonConvert.SerializeObject(listOutputs)
            File.WriteAllText(_persistencePath, json)
        Catch ex As Exception
            Debug.WriteLine($"[StepOutputManager] Save failed: {ex.Message}")
        End Try
    End Sub

    Public Sub LoadFromDisk()
        Try
            If File.Exists(_persistencePath) Then
                Dim json = File.ReadAllText(_persistencePath)
                Dim listOutputs = JsonConvert.DeserializeObject(Of List(Of StepOutput))(json)
                If listOutputs IsNot Nothing Then
                    For Each output In listOutputs
                        Dim key = BuildKey(output.RequestId, output.Iteration, output.StepIndex)
                        _outputs(key) = output
                    Next
                    Debug.WriteLine($"[StepOutputManager] Loaded {_outputs.Count} outputs from disk")
                End If
            End If
        Catch ex As Exception
            Debug.WriteLine($"[StepOutputManager] Load failed: {ex.Message}")
        End Try
    End Sub
    
    ''' <summary>
    ''' Gets statistics about stored outputs
    ''' </summary>
    Public Function GetStatistics() As String
        Try
            Dim totalOutputs = _outputs.Count
            Dim currentRequestOutputs = GetAllOutputs().Count
            Dim allOutputs = GetAllOutputs()
            Dim successCount = allOutputs.Where(Function(o) o.Status = OutcomeStatus.Success).Count()
            Dim failureCount = allOutputs.Where(Function(o) o.Status = OutcomeStatus.Failed).Count()
            
            Return $"Total: {totalOutputs}, Current Request: {currentRequestOutputs}, Success: {successCount}, Failed: {failureCount}"
            
        Catch ex As Exception
            Return "Error getting statistics"
        End Try
    End Function
    
    ' Helper method to build dictionary key
    Private Function BuildKey(requestId As String, iteration As Integer, stepIndex As Integer) As String
        Return $"{requestId}_{iteration}_{stepIndex}"
    End Function
    
End Class

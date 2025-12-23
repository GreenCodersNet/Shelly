' ###  ExecutionOutcome.vb - v2.0.0 ### 

' ##########################################################
'  Shelly - v2.0.0
'  License: Creative Commons Attribution-NonCommercial (CC BY-NC)
'  https://creativecommons.org/licenses/by-nc/4.0/
'  © 2025 Vlad Stefanescu | GreenCoders.net. Attribution required.
' ##########################################################

Imports System.Collections.Generic
Imports System.Linq
Imports System.Security.Cryptography
Imports System.Text
Imports System.IO
Imports Newtonsoft.Json

''' <summary>
''' Outcome status of a tool execution
''' </summary>
Public Enum OutcomeStatus
    Success
    PartialSuccess
    Failed
    Cancelled
    ValidationFailed
End Enum

''' <summary>
''' Structured result of a tool execution with full telemetry
''' </summary>
Public Class ExecutionOutcome
    Public Property Id As Guid = Guid.NewGuid()
    Public Property StepIndex As Integer
    Public Property ToolName As String
    Public Property Arguments As Dictionary(Of String, Object)
    Public Property ArgsHash As String
    Public Property ScriptHash As String
    Public Property Status As OutcomeStatus
    Public Property StartTime As DateTimeOffset
    Public Property EndTime As DateTimeOffset
    Public Property StandardOutput As String
    Public Property StandardError As String
    Public Property ExitCode As Integer
    Public Property ValidationResult As ValidationOutcome
    Public Property RetryCount As Integer
    Public Property ParentOutcomeId As Guid?
    Public Property IsPowerShell As Boolean
    Public Property AttemptNumber As Integer
    Public Property PolicyBlocked As Boolean
    Public Property RemediationNote As String

    <JsonIgnore>
    Public ReadOnly Property Duration As TimeSpan
        Get
            If EndTime = DateTimeOffset.MinValue Then
                Return TimeSpan.Zero
            End If
            Return EndTime - StartTime
        End Get
    End Property

    <JsonIgnore>
    Public ReadOnly Property IsSuccess As Boolean
        Get
            Return Status = OutcomeStatus.Success OrElse Status = OutcomeStatus.PartialSuccess
        End Get
    End Property

    Public Sub New()
        Arguments = New Dictionary(Of String, Object)(StringComparer.OrdinalIgnoreCase)
        StartTime = DateTimeOffset.UtcNow
    End Sub

    ''' <summary>
    ''' Mark execution as completed
    ''' </summary>
    Public Sub Complete(status As OutcomeStatus)
        Me.Status = status
        Me.EndTime = DateTimeOffset.UtcNow
    End Sub

    ''' <summary>
    ''' Get human-readable summary
    ''' </summary>
    Public Function GetSummary() As String
        Dim sb As New Text.StringBuilder()
        sb.AppendLine($"Tool: {ToolName}")
        sb.AppendLine($"Status: {Status}")
        sb.AppendLine($"Duration: {Duration.TotalSeconds:F2}s")

        If Not String.IsNullOrEmpty(ArgsHash) Then
            sb.AppendLine($"ArgsHash: {ArgsHash}")
        End If

        If Not String.IsNullOrEmpty(ScriptHash) Then
            sb.AppendLine($"ScriptHash: {ScriptHash}")
        End If

        If Not String.IsNullOrEmpty(StandardOutput) Then
            sb.AppendLine($"Output: {StandardOutput.Substring(0, Math.Min(200, StandardOutput.Length))}...")
        End If

        If Not String.IsNullOrEmpty(StandardError) Then
            sb.AppendLine($"Error: {StandardError}")
        End If

        If ValidationResult IsNot Nothing Then
            sb.AppendLine($"Validation: {If(ValidationResult.Passed, "PASSED", "FAILED")}")
            If ValidationResult.Diagnostics.Count > 0 Then
                sb.AppendLine($"  {String.Join(", ", ValidationResult.Diagnostics)}")
            End If
        End If

        Return sb.ToString()
    End Function

    ''' <summary>
    ''' Convert to JSON for feeding back to planner
    ''' </summary>
    Public Function ToJson() As String
        Return JsonConvert.SerializeObject(Me, Formatting.Indented)
    End Function

    ''' <summary>
    ''' Deterministic hash of arguments for deduping identical tool runs
    ''' </summary>
    Public Shared Function ComputeArgsHash(args As Dictionary(Of String, Object)) As String
        If args Is Nothing OrElse args.Count = 0 Then
            Return "no-args"
        End If

        Dim ordered = args.OrderBy(Function(k) k.Key, StringComparer.OrdinalIgnoreCase).
                         ToDictionary(Function(k) k.Key, Function(k) If(k.Value, ""), StringComparer.OrdinalIgnoreCase)

        Dim json = JsonConvert.SerializeObject(ordered)
        Return ComputeSha256(json)
    End Function

    ''' <summary>
    ''' Hash a PowerShell script for telemetry/dedupe
    ''' </summary>
    Public Shared Function ComputeScriptHash(script As String) As String
        If String.IsNullOrWhiteSpace(script) Then
            Return "no-script"
        End If
        Return ComputeSha256(script)
    End Function

    Private Shared Function ComputeSha256(input As String) As String
        Using sha = SHA256.Create()
            Dim bytes = Encoding.UTF8.GetBytes(input)
            Dim hash = sha.ComputeHash(bytes)
            Return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant()
        End Using
    End Function

    Public Sub SetScriptAndHash(script As String)
        ScriptHash = ComputeScriptHash(script)
    End Sub

End Class

''' <summary>
''' Result of post-execution validation
''' </summary>
Public Class ValidationOutcome
    Public Property Passed As Boolean
    Public Property ValidatorName As String
    Public Property Diagnostics As New List(Of String)
    Public Property ExpectedCondition As String
    Public Property ActualState As String
    Public Property Timestamp As DateTimeOffset

    Public Sub New()
        Timestamp = DateTimeOffset.UtcNow
    End Sub

    Public Sub AddDiagnostic(message As String)
        Diagnostics.Add(message)
    End Sub
End Class

''' <summary>
''' Centralized tracking and management of execution outcomes
''' </summary>
Public Class OutcomeTracker
    Private ReadOnly _outcomes As New List(Of ExecutionOutcome)
    Private ReadOnly _lock As New Object()
    Private ReadOnly _persistencePath As String = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", "outcomes.json")

    ''' <summary>
    ''' Record a new outcome
    ''' </summary>
    Public Sub RecordOutcome(outcome As ExecutionOutcome)
        SyncLock _lock
            _outcomes.Add(outcome)

            ' Log for debugging
            Debug.WriteLine($"[OutcomeTracker] {outcome.ToolName} => {outcome.Status} ({outcome.Duration.TotalSeconds:F2}s)")

            ' Keep only last 100 outcomes to prevent memory bloat
            If _outcomes.Count > 100 Then
                _outcomes.RemoveAt(0)
            End If

            SaveToDiskInternal()
        End SyncLock
    End Sub

    Public Sub SaveToDisk()
        SyncLock _lock
            SaveToDiskInternal()
        End SyncLock
    End Sub

    Public Sub LoadFromDisk()
        SyncLock _lock
            Try
                If File.Exists(_persistencePath) Then
                    Dim json = File.ReadAllText(_persistencePath)
                    Dim loaded = JsonConvert.DeserializeObject(Of List(Of ExecutionOutcome))(json)
                    _outcomes.Clear()
                    If loaded IsNot Nothing Then
                        _outcomes.AddRange(loaded.TakeLast(100))
                    End If
                    Debug.WriteLine($"[OutcomeTracker] Loaded {_outcomes.Count} outcomes from disk")
                End If
            Catch ex As Exception
                Debug.WriteLine($"[OutcomeTracker] Load failed: {ex.Message}")
            End Try
        End SyncLock
    End Sub

    Private Sub SaveToDiskInternal()
        Try
            Directory.CreateDirectory(Path.GetDirectoryName(_persistencePath))
            Dim json = JsonConvert.SerializeObject(_outcomes)
            File.WriteAllText(_persistencePath, json)
        Catch ex As Exception
            Debug.WriteLine($"[OutcomeTracker] Save failed: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Get all outcomes for current session
    ''' </summary>
    Public Function GetAllOutcomes() As List(Of ExecutionOutcome)
        SyncLock _lock
            Return New List(Of ExecutionOutcome)(_outcomes)
        End SyncLock
    End Function

    ''' <summary>
    ''' Get outcomes for a specific tool
    ''' </summary>
    Public Function GetOutcomesByTool(toolName As String) As List(Of ExecutionOutcome)
        SyncLock _lock
            Return _outcomes.Where(Function(o) o.ToolName.Equals(toolName, StringComparison.OrdinalIgnoreCase)).ToList()
        End SyncLock
    End Function

    Public Function HasSuccessfulOutcome(toolName As String, argsHash As String) As Boolean
        If String.IsNullOrWhiteSpace(argsHash) Then Return False
        SyncLock _lock
            Return _outcomes.Any(Function(o) o.ToolName.Equals(toolName, StringComparison.OrdinalIgnoreCase) AndAlso o.ArgsHash = argsHash AndAlso o.IsSuccess)
        End SyncLock
    End Function

    Public Function GetSuccessfulSignatures(maxCount As Integer) As List(Of String)
        SyncLock _lock
            Return _outcomes.Where(Function(o) o.IsSuccess).
                               TakeLast(maxCount).
                               Select(Function(o) $"{o.ToolName}|{o.ArgsHash}").
                               ToList()
        End SyncLock
    End Function

    Public Function GetRecentOutcomes(count As Integer) As List(Of ExecutionOutcome)
        SyncLock _lock
            Return _outcomes.TakeLast(count).ToList()
        End SyncLock
    End Function

    ''' <summary>
    ''' Get failed outcomes only
    ''' </summary>
    Public Function GetFailedOutcomes() As List(Of ExecutionOutcome)
        SyncLock _lock
            Return _outcomes.Where(Function(o) Not o.IsSuccess).ToList()
        End SyncLock
    End Function

    ''' <summary>
    ''' Build structured digest for feeding to planner
    ''' </summary>
    Public Function BuildPlannerDigest(lastN As Integer) As String
        Dim recent = GetRecentOutcomes(lastN)
        If recent.Count = 0 Then
            Return "No previous execution outcomes."
        End If

        Dim sb As New Text.StringBuilder()
        sb.AppendLine("=== Recent Execution Outcomes ===")

        For i = 0 To recent.Count - 1
            Dim outcome = recent(i)
            sb.AppendLine($"{i + 1}. {outcome.ToolName} | {outcome.Status}")
            sb.AppendLine($"   Args: {JsonConvert.SerializeObject(outcome.Arguments)}")
            If Not String.IsNullOrEmpty(outcome.ArgsHash) Then
                sb.AppendLine($"   ArgsHash: {outcome.ArgsHash}")
            End If

            If Not String.IsNullOrEmpty(outcome.StandardOutput) Then
                Dim preview = outcome.StandardOutput.Substring(0, Math.Min(150, outcome.StandardOutput.Length))
                sb.AppendLine($"   Output: {preview}...")
            End If

            If Not String.IsNullOrEmpty(outcome.StandardError) Then
                sb.AppendLine($"   Error: {outcome.StandardError}")
            End If

            If outcome.ValidationResult IsNot Nothing Then
                sb.AppendLine($"   Validation: {If(outcome.ValidationResult.Passed, "PASSED", "FAILED")}")
                If Not outcome.ValidationResult.Passed Then
                    For Each diag In outcome.ValidationResult.Diagnostics
                        sb.AppendLine($"     - {diag}")
                    Next
                End If
            End If

            sb.AppendLine()
        Next

        Return sb.ToString()
    End Function

    ''' <summary>
    ''' Calculate success rate for a specific tool
    ''' </summary>
    Public Function GetToolSuccessRate(toolName As String) As Double
        Dim outcomes = GetOutcomesByTool(toolName)
        If outcomes.Count = 0 Then Return 0.0

        Dim successCount = outcomes.Where(Function(o) o.IsSuccess).Count()
        Return CDbl(successCount) / outcomes.Count
    End Function

    ''' <summary>
    ''' Clear all outcomes (start fresh session)
    ''' </summary>
    Public Sub Clear()
        SyncLock _lock
            _outcomes.Clear()
            SaveToDiskInternal()
            Debug.WriteLine("[OutcomeTracker] Cleared all outcomes")
        End SyncLock
    End Sub

    ''' <summary>
    ''' Get statistics summary
    ''' </summary>
    Public Function GetStatistics() As String
        SyncLock _lock
            If _outcomes.Count = 0 Then
                Return "No execution statistics available."
            End If

            Dim sb As New Text.StringBuilder()
            sb.AppendLine("=== Execution Statistics ===")
            sb.AppendLine($"Total Executions: {_outcomes.Count}")

            Dim successCount = _outcomes.Where(Function(o) o.Status = OutcomeStatus.Success).Count()
            Dim failCount = _outcomes.Where(Function(o) o.Status = OutcomeStatus.Failed).Count()
            Dim cancelCount = _outcomes.Where(Function(o) o.Status = OutcomeStatus.Cancelled).Count()

            sb.AppendLine($"Success: {successCount} ({successCount * 100.0 / _outcomes.Count:F1}%)")
            sb.AppendLine($"Failed: {failCount} ({failCount * 100.0 / _outcomes.Count:F1}%)")
            sb.AppendLine($"Cancelled: {cancelCount}")

            ' Average duration
            Dim avgDuration = _outcomes.Average(Function(o) o.Duration.TotalSeconds)
            sb.AppendLine($"Average Duration: {avgDuration:F2}s")

            ' Most used tools
            sb.AppendLine()
            sb.AppendLine("Most Used Tools:")
            Dim toolCounts = _outcomes.GroupBy(Function(o) o.ToolName) _
                                     .OrderByDescending(Function(g) g.Count()) _
                                     .Take(5)
            For Each grp In toolCounts
                sb.AppendLine($"  {grp.Key}: {grp.Count()} times")
            Next

            Return sb.ToString()
        End SyncLock
    End Function

    ''' <summary>
    ''' Get failed signatures
    ''' </summary>
    Public Function GetFailedSignatures(maxCount As Integer) As List(Of String)
        SyncLock _lock
            Return _outcomes.Where(Function(o) Not o.IsSuccess).
                               TakeLast(maxCount).
                               Select(Function(o) $"{o.ToolName}|{o.ArgsHash}").
                               ToList()
        End SyncLock
    End Function

End Class

''' <summary>
''' Global outcome tracker instance
''' </summary>
Public Module GlobalOutcomeTracker
    Private _instance As OutcomeTracker

    Public ReadOnly Property Instance As OutcomeTracker
        Get
            If _instance Is Nothing Then
                _instance = New OutcomeTracker()
                _instance.LoadFromDisk()
            End If
            Return _instance
        End Get
    End Property
End Module

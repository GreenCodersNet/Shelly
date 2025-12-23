' ###  TelemetryStorage.vb - v1.0.0 ###

' ##########################################################
'  Shelly - v1.0.1
'  License: Creative Commons Attribution-NonCommercial (CC BY-NC)
'  https://creativecommons.org/licenses/by-nc/4.0/
'  © 2025 Vlad Stefanescu | GreenCoders.net. Attribution required.
' ##########################################################

Imports System.IO
Imports System.Text
Imports System.Security.Cryptography
Imports Newtonsoft.Json

Public Module TelemetryStorage

    Private ReadOnly TelemetryFolder As String = Path.Combine(ShellyOps.DefaultPath, "telemetry")
    Private ReadOnly _powerShellExecutionsPath As String = Path.Combine(TelemetryFolder, "powershell-executions.json")

    Public ReadOnly Property PowerShellExecutionLogPath As String
        Get
            Return _powerShellExecutionsPath
        End Get
    End Property

    Public Sub SavePowerShellExecutions(records As IEnumerable(Of PowerShellExecutionRecord))
        If records Is Nothing Then Return

        Try
            Directory.CreateDirectory(TelemetryFolder)
            Dim sanitized = records.Select(Function(r) SanitizeRecord(r)).ToList()
            Dim payload As String = JsonConvert.SerializeObject(sanitized, Formatting.Indented)
            File.WriteAllText(_powerShellExecutionsPath, payload, Encoding.UTF8)
        Catch ex As Exception
            Debug.WriteLine($"[Telemetry] Failed to persist PowerShell executions: {ex.Message}")
        End Try
    End Sub

    Public Sub AppendHistoryEntry(record As PowerShellExecutionRecord, Optional toolName As String = "ExecutePowerShellScript")
        If record Is Nothing Then Return

        Try
            Dim status As String
            If record.Blocked Then
                status = "Blocked"
            ElseIf record.ExitCode = 0 Then
                status = "Success"
            Else
                status = "Failed"
            End If

            Dim sb As New StringBuilder()
            sb.AppendLine("[PowerShell Outcome]")
            sb.AppendLine($"Tool: {toolName}")
            sb.AppendLine($"Status: {status}")
            If record.Attempt > 0 Then
                sb.AppendLine($"Attempts: {record.Attempt}")
            End If
            If record.Duration <> TimeSpan.Zero Then
                sb.AppendLine($"Duration: {record.Duration.TotalSeconds:F1}s")
            End If
            If Not String.IsNullOrWhiteSpace(record.ScriptHash) Then
                sb.AppendLine($"Script Hash: {record.ScriptHash}")
            End If
            If Not String.IsNullOrWhiteSpace(record.StdOut) Then
                sb.AppendLine($"StdOut: {TruncateAndRedact(record.StdOut, 240)}")
            End If
            If Not String.IsNullOrWhiteSpace(record.StdErr) Then
                sb.AppendLine($"StdErr: {TruncateAndRedact(record.StdErr, 240)}")
            End If
            sb.AppendLine($"Log: {PowerShellExecutionLogPath}")

            Dim summary = sb.ToString().Trim()
            Globals.conversationHistory.Add(New Dictionary(Of String, String) From {
                {"role", "system"},
                {"content", summary},
                {"summarizable", "true"}
            })
        Catch ex As Exception
            Debug.WriteLine($"[Telemetry] Failed to append history entry: {ex.Message}")
        End Try
    End Sub

    Private Function SanitizeRecord(record As PowerShellExecutionRecord) As Dictionary(Of String, Object)
        Dim result As New Dictionary(Of String, Object)(StringComparer.OrdinalIgnoreCase)
        Dim props = record.GetType().GetProperties()

        For Each p In props
            Dim name = p.Name
            Dim valueObj = p.GetValue(record)
            If valueObj Is Nothing Then Continue For

            If name.Equals("Script", StringComparison.OrdinalIgnoreCase) OrElse name.Equals("ScriptContent", StringComparison.OrdinalIgnoreCase) Then
                Dim scriptStr = valueObj.ToString()
                result("ScriptHash") = HashString(scriptStr)
                ' Do not store raw script
                Continue For
            End If

            If name.Equals("StdOut", StringComparison.OrdinalIgnoreCase) OrElse name.Equals("StdErr", StringComparison.OrdinalIgnoreCase) Then
                result(name) = TruncateAndRedact(valueObj.ToString(), 500)
                Continue For
            End If

            ' Path-like fields: store hashed form
            If name.ToLowerInvariant().Contains("path") OrElse name.ToLowerInvariant().Contains("file") Then
                result(name & "Hash") = HashString(valueObj.ToString())
                Continue For
            End If

            result(name) = valueObj
        Next

        ' Ensure script hash present if already on record
        If Not result.ContainsKey("ScriptHash") AndAlso record.ScriptHash IsNot Nothing Then
            result("ScriptHash") = record.ScriptHash
        End If

        Return result
    End Function

    Private Function HashString(input As String) As String
        If String.IsNullOrWhiteSpace(input) Then Return String.Empty
        Using sha = SHA256.Create()
            Dim bytes = Encoding.UTF8.GetBytes(input)
            Dim hash = sha.ComputeHash(bytes)
            Return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant()
        End Using
    End Function

    Private Function TruncateAndRedact(value As String, maxLength As Integer) As String
        If String.IsNullOrWhiteSpace(value) Then Return value
        Dim clean = value.Replace(Environment.NewLine, " ").Trim()
        If clean.Length <= maxLength Then Return clean
        Return clean.Substring(0, maxLength) & "…"
    End Function

End Module

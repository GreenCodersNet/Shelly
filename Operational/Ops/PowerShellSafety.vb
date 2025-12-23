Imports System.Collections.Generic
Imports System.Linq
Imports System.Text
Imports System.Text.RegularExpressions

Public Class PowerShellValidationResult
    Public Property IsValid As Boolean
    Public Property Diagnostics As New List(Of String)
    Public Property HasDestructiveCommands As Boolean
    Public Property BlockReason As String = String.Empty
    Public Property NormalizedScript As String = String.Empty
    Public Property CommandSummaries As New List(Of String)
End Class

Public Class PowerShellExecutionRecord
    Public Property Id As Guid = Guid.NewGuid()
    Public Property ScriptHash As String = String.Empty
    Public Property ArgumentsSnapshot As String = String.Empty
    Public Property Attempt As Integer
    Public Property StartedAt As DateTimeOffset
    Public Property FinishedAt As DateTimeOffset
    Public Property ExitCode As Integer
    Public Property StdOut As String = String.Empty
    Public Property StdErr As String = String.Empty
    Public Property Validation As PowerShellValidationResult
    Public Property Blocked As Boolean

    Public ReadOnly Property Duration As TimeSpan
        Get
            If FinishedAt = DateTimeOffset.MinValue OrElse StartedAt = DateTimeOffset.MinValue Then
                Return TimeSpan.Zero
            End If
            Return FinishedAt - StartedAt
        End Get
    End Property
End Class

Public NotInheritable Class PowerShellScriptSafety
    Private Shared ReadOnly CommandPattern As New Regex("(?im)^[\s']*([a-zA-Z][a-zA-Z0-9\-]+)", RegexOptions.Compiled)
    Private Shared ReadOnly PathPattern As New Regex("(?i)c:\\[^\r\n]*", RegexOptions.Compiled)

    Private Shared ReadOnly DestructiveVerbs As HashSet(Of String) = New HashSet(Of String)(StringComparer.OrdinalIgnoreCase) From {
        "remove-item", "remove-childitem", "remove-itemproperty", "remove-acl",
        "clear-content", "clear-item", "stop-service", "stop-process",
        "invoke-expression", "invoke-command", "start-process", "restart-computer",
        "stop-computer", "format-volume", "set-item", "set-itemproperty",
        "new-scheduledtask", "register-scheduledtask", "add-content",
        "set-content", "add-type", "start-job", "new-job"
    }

    Private Shared ReadOnly RestrictedMarkers As HashSet(Of String) = New HashSet(Of String)(StringComparer.OrdinalIgnoreCase) From {
        "c:\windows", "c:\program files", "c:\programdata", "c:\system32", "c:\boot", "c:\recovery"
    }

    Public Shared Function Inspect(script As String, Optional enforceSystemBlock As Boolean = False) As PowerShellValidationResult
        Dim normalized = If(script, String.Empty).Trim()
        Dim result As New PowerShellValidationResult With {
            .NormalizedScript = normalized
        }

        If String.IsNullOrWhiteSpace(result.NormalizedScript) Then
            result.IsValid = False
            result.Diagnostics.Add("Script is empty.")
            result.BlockReason = "Empty script"
            Return result
        End If

        For Each match As Match In CommandPattern.Matches(result.NormalizedScript)
            Dim commandName = match.Groups(1).Value
            If String.IsNullOrWhiteSpace(commandName) Then Continue For
            result.CommandSummaries.Add(commandName)

            ' NEW: Skip Start-Process check if user has disabled BlockStartProcess
            If commandName.Equals("start-process", StringComparison.OrdinalIgnoreCase) Then
                If Not SecurityFlags.BlockStartProcess Then
                    ' User has allowed Start-Process, skip blocking it
                    result.Diagnostics.Add($"Command detected (allowed by user settings): {commandName}.")
                    Continue For
                End If
            End If

            If DestructiveVerbs.Contains(commandName) Then
                result.HasDestructiveCommands = True
                result.Diagnostics.Add($"Potentially destructive command detected: {commandName}.")
            End If
        Next

        If enforceSystemBlock Then
            For Each pathMatch As Match In PathPattern.Matches(result.NormalizedScript)
                Dim path = pathMatch.Value
                If RestrictedMarkers.Any(Function(marker) path.IndexOf(marker, StringComparison.OrdinalIgnoreCase) >= 0) Then
                    result.IsValid = False
                    result.BlockReason = "Script touches restricted system paths."
                    result.Diagnostics.Add("Restricted path detected while BlockSystemC is enabled.")
                    Return result
                End If
            Next
        End If

        If result.HasDestructiveCommands Then
            result.IsValid = False
            result.BlockReason = "Script contains destructive commands"
        Else
            result.IsValid = True
        End If

        Return result
    End Function

    Public Shared Function GetScriptHash(script As String) As String
        Using sha = System.Security.Cryptography.SHA256.Create()
            Dim bytes = Encoding.UTF8.GetBytes(script)
            Dim hash = sha.ComputeHash(bytes)
            Return Convert.ToHexString(hash)
        End Using
    End Function
End Class

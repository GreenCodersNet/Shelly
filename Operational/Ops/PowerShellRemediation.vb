' ###  PowerShellRemediation.vb - v1.0.0 ###

' ##########################################################
'  Shelly - v1.0.1
'  License: Creative Commons Attribution-NonCommercial (CC BY-NC)
'  https://creativecommons.org/licenses/by-nc/4.0/
'  © 2025 Vlad Stefanescu | GreenCoders.net. Attribution required.
' ##########################################################

Imports System.Text.RegularExpressions

Public Module PowerShellRemediation

    ''' <summary>
    ''' Attempts quick heuristic fixes on a script based on common stderr patterns.
    ''' Returns the patched script or Nothing if no heuristic applies.
    ''' </summary>
    Public Function TryHeuristicFix(script As String, stderr As String) As String
        If String.IsNullOrWhiteSpace(script) OrElse String.IsNullOrWhiteSpace(stderr) Then
            Return Nothing
        End If

        Dim lowerErr = stderr.ToLowerInvariant()

        ' 1) Missing -ErrorAction Stop on failing cmdlets
        If lowerErr.Contains("erroraction") OrElse lowerErr.Contains("non-terminating") Then
            If Not script.Contains("-ErrorAction") Then
                Dim patched = Regex.Replace(script, "\b(Get-ChildItem|Remove-Item|Copy-Item|Move-Item|Set-Content|Add-Content|Out-File)\b", "$1 -ErrorAction Stop", RegexOptions.IgnoreCase)
                If patched <> script Then
                    Debug.WriteLine("[Remediation] Added -ErrorAction Stop")
                    Return patched
                End If
            End If
        End If

        ' 2) Execution policy restrictions
        If lowerErr.Contains("execution of scripts is disabled") OrElse lowerErr.Contains("cannot be loaded because running scripts is disabled") Then
            If Not script.TrimStart().StartsWith("Set-ExecutionPolicy", StringComparison.OrdinalIgnoreCase) Then
                Dim patched = "Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass -Force" & Environment.NewLine & script
                Debug.WriteLine("[Remediation] Prepended Set-ExecutionPolicy Bypass")
                Return patched
            End If
        End If

        ' 3) Encoding issues (BOM, UTF-8)
        If lowerErr.Contains("utf") OrElse lowerErr.Contains("bom") OrElse lowerErr.Contains("encoding") Then
            If Not script.Contains("-Encoding") Then
                Dim patched = Regex.Replace(script, "\b(Out-File|Set-Content|Add-Content)\b(?![^|]*-Encoding)", "$1 -Encoding UTF8", RegexOptions.IgnoreCase)
                If patched <> script Then
                    Debug.WriteLine("[Remediation] Added -Encoding UTF8")
                    Return patched
                End If
            End If
        End If

        ' 4) Quoting / path issues (spaces in paths)
        If lowerErr.Contains("cannot find path") OrElse lowerErr.Contains("objectnotfound") Then
            ' Wrap bare paths in quotes
            Dim patched = Regex.Replace(script, "(?<=-Path\s+)([^\s""'][^\s]*\s+[^\s]+)", """$1""", RegexOptions.IgnoreCase)
            If patched <> script Then
                Debug.WriteLine("[Remediation] Quoted paths with spaces")
                Return patched
            End If
        End If

        ' 5) Missing $ErrorActionPreference at script level
        If lowerErr.Contains("terminating error") AndAlso Not script.Contains("$ErrorActionPreference") Then
            Dim patched = "$ErrorActionPreference = 'Stop'" & Environment.NewLine & script
            Debug.WriteLine("[Remediation] Prepended $ErrorActionPreference = 'Stop'")
            Return patched
        End If

        Return Nothing
    End Function

End Module

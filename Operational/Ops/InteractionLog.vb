' ### InteractionLog.vb
' Lightweight per-run interaction log (user prompts + assistant replies)

Imports System.IO
Imports Newtonsoft.Json

Module InteractionLog
    Private ReadOnly PreferredLogDirectory As String = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Logs"))
    Private ReadOnly FallbackLogDirectory As String = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs")
    Private resolvedLogDirectory As String = FallbackLogDirectory
    Private resolvedLogFilePath As String = Path.Combine(FallbackLogDirectory, "interaction-log.jsonl")

    Public Sub InitializeInteractionLog()
        Try
            resolvedLogDirectory = EnsureDirectory(PreferredLogDirectory, FallbackLogDirectory)
            resolvedLogFilePath = Path.Combine(resolvedLogDirectory, "interaction-log.jsonl")

            File.WriteAllText(resolvedLogFilePath, String.Empty)
            Debug.WriteLine("[InteractionLog] Cleared log at startup: " & resolvedLogFilePath)
        Catch ex As Exception
            Debug.WriteLine("[InteractionLog] Init error: " & ex.Message)
        End Try
    End Sub

    Public Sub AppendInteraction(role As String, content As String)
        Try
            If String.IsNullOrWhiteSpace(content) Then Return
            Dim entry = New With {
                .timestamp = DateTimeOffset.UtcNow.ToString("o"),
                .role = role,
                .content = content.Trim()
            }
            Dim line = JsonConvert.SerializeObject(entry)
            File.AppendAllText(resolvedLogFilePath, line & Environment.NewLine)
        Catch ex As Exception
            Debug.WriteLine("[InteractionLog] Append error: " & ex.Message)
        End Try
    End Sub

    Private Function EnsureDirectory(preferred As String, fallback As String) As String
        Try
            Directory.CreateDirectory(preferred)
            Return preferred
        Catch
            Try
                Directory.CreateDirectory(fallback)
                Return fallback
            Catch ex As Exception
                Debug.WriteLine("[InteractionLog] Directory creation failed: " & ex.Message)
                Return fallback
            End Try
        End Try
    End Function
End Module

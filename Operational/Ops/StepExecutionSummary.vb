' ###  StepExecutionSummary.vb - v1.0.0 ###

' ##########################################################
'  Shelly - v1.0.0
'  License: Creative Commons Attribution-NonCommercial (CC BY-NC)
'  https://creativecommons.org/licenses/by-nc/4.0/
'  © 2025 Vlad Stefanescu | GreenCoders.net. Attribution required.
' ##########################################################

''' <summary>
''' Enum representing different types of content produced by steps
''' </summary>
Public Enum ContentType
    ConversationalText    ' FreeResponse, short answers, explanations
    FilePath             ' Image paths, document paths, single files
    FileList             ' Lists of files (potentially very large)
    BulkText             ' Large summaries, long text outputs
    DataTable            ' Structured data (products, prices, lists)
    SystemInfo           ' PowerShell system queries, brief info
    ErrorMessage         ' Error information
End Enum

''' <summary>
''' Lightweight summary of a step execution for final AI rephrasing
''' Separates metadata from bulk content to reduce token usage
''' </summary>
Public Class StepExecutionSummary
    ''' <summary>
    ''' Step index in execution sequence
    ''' </summary>
    Public Property StepIndex As Integer
    
    ''' <summary>
    ''' Tool/function name that was executed
    ''' </summary>
    Public Property ToolName As String
    
    ''' <summary>
    ''' Execution status (Success, Failed, etc.)
    ''' </summary>
    Public Property Status As OutcomeStatus
    
    ''' <summary>
    ''' Classification of content type
    ''' </summary>
    Public Property ContentType As ContentType
    
    ''' <summary>
    ''' Short description for final AI call (token-efficient)
    ''' Example: "Generated 3 images", "Found 1,247 files"
    ''' </summary>
    Public Property ShortDescription As String
    
    ''' <summary>
    ''' Short summary of the step execution
    ''' </summary>
    Public Property ShortSummary As String
    
    ''' <summary>
    ''' Key metadata extracted from output (counts, paths, etc.)
    ''' Used to provide context without including bulk content
    ''' </summary>
    Public Property KeyDetails As New Dictionary(Of String, String)
    
    ''' <summary>
    ''' Full output text (stored for planner, NOT sent to final AI call)
    ''' </summary>
    Public Property FullOutput As String
    
    ''' <summary>
    ''' Flag indicating if content is too large (> 500 chars)
    ''' Large content is summarized only, not included in full
    ''' </summary>
    Public Property IsLargeContent As Boolean
    
    ''' <summary>
    ''' Original arguments passed to the tool (for context)
    ''' </summary>
    Public Property Arguments As Dictionary(Of String, Object)
    
    ''' <summary>
    ''' Timestamp of execution
    ''' </summary>
    Public Property Timestamp As DateTimeOffset = DateTimeOffset.UtcNow
    
    ''' <summary>
    ''' Returns a compact representation for final AI rephrasing call
    ''' Includes only essential metadata, excludes bulk content
    ''' </summary>
    Public Function GetCompactRepresentation() As String
        Dim sb As New System.Text.StringBuilder()

        Dim prefixSummary As String = If(String.IsNullOrWhiteSpace(ShortSummary), Nothing, ShortSummary)

        Select Case ContentType
            Case ContentType.ConversationalText
                If Not String.IsNullOrWhiteSpace(prefixSummary) Then
                    sb.AppendLine($"• {prefixSummary}")
                ElseIf IsLargeContent Then
                    sb.AppendLine($"• {ToolName}: {ShortDescription}")
                    sb.AppendLine("  (Full response already displayed to user)")
                Else
                    sb.AppendLine($"• {ToolName}: {FullOutput}")
                End If

            Case ContentType.FilePath
                sb.AppendLine($"• {If(prefixSummary, ShortDescription)}")
                If KeyDetails.ContainsKey("paths") Then
                    sb.AppendLine($"  Files: {KeyDetails("paths")}")
                End If

            Case ContentType.FileList
                sb.AppendLine($"• {If(prefixSummary, ShortDescription)}")
                If KeyDetails.ContainsKey("preview") Then
                    sb.AppendLine($"  Preview: {KeyDetails("preview")}")
                End If

            Case ContentType.BulkText
                sb.AppendLine($"• {If(prefixSummary, ShortDescription)}")
                If KeyDetails.ContainsKey("outputLength") Then
                    sb.AppendLine("  (Large text content already shown to user)")
                End If

            Case ContentType.DataTable
                sb.AppendLine($"• {If(prefixSummary, ShortDescription)}")
                If KeyDetails.ContainsKey("itemCount") Then
                    sb.AppendLine($"  ({KeyDetails("itemCount")} items)")
                End If
                sb.AppendLine("  (Structured data already displayed above)")

            Case ContentType.SystemInfo
                If Not String.IsNullOrWhiteSpace(prefixSummary) Then
                    sb.AppendLine($"• {prefixSummary}")
                ElseIf FullOutput.Length < 300 Then
                    sb.AppendLine($"• {ToolName}: {FullOutput}")
                Else
                    sb.AppendLine($"• {ShortDescription}")
                End If

            Case ContentType.ErrorMessage
                sb.AppendLine($"• {ToolName}: ❌ {If(prefixSummary, ShortDescription)}")
        End Select

        Return sb.ToString()
    End Function
    
    ''' <summary>
    ''' Returns a user-friendly description of what was accomplished
    ''' </summary>
    Public Function GetUserFriendlyDescription() As String
        Dim statusIcon = If(Status = OutcomeStatus.Success, "✅", "❌")
        Return $"{statusIcon} {ShortDescription}"
    End Function
End Class

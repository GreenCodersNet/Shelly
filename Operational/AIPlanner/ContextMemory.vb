' ###  ContextMemory.vb - v1.0.0 ###
' Maintains rich execution context across all steps

Imports Newtonsoft.Json

Public Class ContextMemory
    ' Extracted structured data
    Public Property ExtractedFilePaths As New List(Of String)
    Public Property ExtractedImagePaths As New List(Of String)
    Public Property Classifications As New Dictionary(Of String, String)
    Public Property Summaries As New Dictionary(Of String, String)
    Public Property Counts As New Dictionary(Of String, Integer)
    Public Property CustomData As New Dictionary(Of String, Object)
    
    ' Execution trace
    Public Property CompletedGoals As New List(Of SubGoal)
    Public Property CurrentGoal As SubGoal
    Public Property PendingGoals As New List(Of SubGoal)
    Public Property TotalStepsExecuted As Integer = 0
    
    ''' <summary>
    ''' Updates context based on AI analysis of step outcome
    ''' </summary>
    Public Sub UpdateFromAnalysis(analysis As AnalysisResult)
        ' Extract file paths
        If analysis.ExtractedData.ContainsKey("filePaths") Then
            Dim paths = TryCast(analysis.ExtractedData("filePaths"), List(Of String))
            If paths IsNot Nothing Then
                For Each path In paths
                    If Not ExtractedFilePaths.Contains(path) Then
                        ExtractedFilePaths.Add(path)
                    End If
                Next
            End If
        End If
        
        ' Extract image paths
        If analysis.ExtractedData.ContainsKey("imagePaths") Then
            Dim paths = TryCast(analysis.ExtractedData("imagePaths"), List(Of String))
            If paths IsNot Nothing Then
                For Each path In paths
                    If Not ExtractedImagePaths.Contains(path) Then
                        ExtractedImagePaths.Add(path)
                    End If
                Next
            End If
        End If
        
        ' Extract classifications
        If analysis.ExtractedData.ContainsKey("classifications") Then
            Dim classifs = TryCast(analysis.ExtractedData("classifications"), Dictionary(Of String, String))
            If classifs IsNot Nothing Then
                For Each kvp In classifs
                    Classifications(kvp.Key) = kvp.Value
                Next
            End If
        End If
        
        ' Extract summaries
        If analysis.ExtractedData.ContainsKey("summaries") Then
            Dim sums = TryCast(analysis.ExtractedData("summaries"), Dictionary(Of String, String))
            If sums IsNot Nothing Then
                For Each kvp In sums
                    Summaries(kvp.Key) = kvp.Value
                Next
            End If
        End If
        
        ' Extract counts
        If analysis.ExtractedData.ContainsKey("count") Then
            Dim countKey = If(analysis.ExtractedData.ContainsKey("countKey"), 
                             analysis.ExtractedData("countKey").ToString(), 
                             "total")
            Counts(countKey) = Convert.ToInt32(analysis.ExtractedData("count"))
        End If
        
        ' Store any custom data
        For Each kvp In analysis.ExtractedData
            If Not (kvp.Key = "filePaths" OrElse kvp.Key = "imagePaths" OrElse 
                    kvp.Key = "classifications" OrElse kvp.Key = "summaries" OrElse 
                    kvp.Key = "count" OrElse kvp.Key = "countKey") Then
                CustomData(kvp.Key) = kvp.Value
            End If
        Next
        
        TotalStepsExecuted += 1
    End Sub
    
    ''' <summary>
    ''' Builds AI-readable context summary
    ''' </summary>
    Public Function BuildContextSummary() As String
        Dim sb As New System.Text.StringBuilder()
        
        sb.AppendLine("EXECUTION CONTEXT:")
        sb.AppendLine()
        
        ' Goals progress
        sb.AppendLine($"Completed Goals: {CompletedGoals.Count}")
        For Each goal In CompletedGoals
            sb.AppendLine($"  ? {goal.Description}")
        Next
        sb.AppendLine()
        
        If CurrentGoal IsNot Nothing Then
            sb.AppendLine($"Current Goal: {CurrentGoal.Description}")
            sb.AppendLine($"  Success Criteria: {CurrentGoal.SuccessCriteria}")
            sb.AppendLine($"  Attempts: {CurrentGoal.AttemptedSteps.Count}")
            sb.AppendLine()
        End If
        
        ' Extracted data summary
        sb.AppendLine("EXTRACTED DATA:")
        
        If ExtractedFilePaths.Count > 0 Then
            sb.AppendLine($"  Files Found: {ExtractedFilePaths.Count}")
            For i = 0 To Math.Min(ExtractedFilePaths.Count - 1, 4)
                sb.AppendLine($"    {i + 1}. {ExtractedFilePaths(i)}")
            Next
            If ExtractedFilePaths.Count > 5 Then
                sb.AppendLine($"    ... and {ExtractedFilePaths.Count - 5} more")
            End If
        End If
        
        If ExtractedImagePaths.Count > 0 Then
            sb.AppendLine($"  Images Found: {ExtractedImagePaths.Count}")
            For i = 0 To Math.Min(ExtractedImagePaths.Count - 1, 4)
                Dim classification = If(Classifications.ContainsKey(ExtractedImagePaths(i)), 
                                       $" ({Classifications(ExtractedImagePaths(i))})", 
                                       "")
                sb.AppendLine($"    {i + 1}. {ExtractedImagePaths(i)}{classification}")
            Next
            If ExtractedImagePaths.Count > 5 Then
                sb.AppendLine($"    ... and {ExtractedImagePaths.Count - 5} more")
            End If
        End If
        
        If Summaries.Count > 0 Then
            sb.AppendLine($"  Summaries Created: {Summaries.Count}")
            For Each kvp In Summaries.Take(2)
                sb.AppendLine($"    • {IO.Path.GetFileName(kvp.Key)}: {kvp.Value.Substring(0, Math.Min(60, kvp.Value.Length))}...")
            Next
            If Summaries.Count > 2 Then
                sb.AppendLine($"    ... and {Summaries.Count - 2} more")
            End If
        End If
        
        If Counts.Count > 0 Then
            sb.AppendLine("  Counts:")
            For Each kvp In Counts
                sb.AppendLine($"    {kvp.Key}: {kvp.Value}")
            Next
        End If
        
        sb.AppendLine()
        sb.AppendLine($"Total Steps Executed: {TotalStepsExecuted}")
        
        Return sb.ToString()
    End Function
    
    ''' <summary>
    ''' Gets items available for next processing step
    ''' </summary>
    Public Function GetUnprocessedItems(itemType As String) As List(Of String)
        Select Case itemType.ToLower()
            Case "files"
                ' Files that don't have summaries yet
                Return ExtractedFilePaths.Where(
                    Function(f) Not Summaries.ContainsKey(f)
                ).ToList()
                
            Case "images"
                ' Images that don't have classifications yet
                Return ExtractedImagePaths.Where(
                    Function(img) Not Classifications.ContainsKey(img)
                ).ToList()
                
            Case "summaries"
                ' Summaries that don't have images generated yet
                ' (This would require tracking, for now return all)
                Return Summaries.Keys.ToList()
                
            Case Else
                Return New List(Of String)()
        End Select
    End Function
    
    ''' <summary>
    ''' Clears context for new request
    ''' </summary>
    Public Sub Clear()
        ExtractedFilePaths.Clear()
        ExtractedImagePaths.Clear()
        Classifications.Clear()
        Summaries.Clear()
        Counts.Clear()
        CustomData.Clear()
        CompletedGoals.Clear()
        CurrentGoal = Nothing
        PendingGoals.Clear()
        TotalStepsExecuted = 0
    End Sub
End Class

''' <summary>
''' Result of AI analysis of a step outcome
''' </summary>
Public Class AnalysisResult
    Public Property GoalAchieved As Boolean
    Public Property ExtractedData As New Dictionary(Of String, Object)
    Public Property NextStepRecommendation As String
    Public Property PlanModificationNeeded As Boolean
    Public Property Reasoning As String
    Public Property ConfidenceLevel As Double  ' 0.0 to 1.0
    Public Property IssuesDetected As New List(Of String)
End Class

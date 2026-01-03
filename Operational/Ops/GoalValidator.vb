' ###  GoalValidator.vb - v2.1.0 ### 

' ##########################################################
'  Shelly - v2.1.0
'  License: Creative Commons Attribution-NonCommercial (CC BY-NC)
'  https://creativecommons.org/licenses/by-nc/4.0/
'  © 2025 Vlad Stefanescu | GreenCoders.net. Attribution required.
' ##########################################################

Imports System.Linq
Imports System.Text.RegularExpressions

''' <summary>
''' Module to determine if the user's goal has been achieved based on execution outcomes
''' ENHANCED: Generic action detection - no hardcoded patterns
''' </summary>
Public Module GoalValidator

    Private Function IsTimeOutcome(outcome As ExecutionOutcome) As Boolean
        If outcome Is Nothing Then Return False

        Dim script As String = ""
        If outcome.Arguments IsNot Nothing AndAlso outcome.Arguments.ContainsKey("script") Then
            script = If(outcome.Arguments("script"), String.Empty).ToString().ToLowerInvariant()
        End If

        Dim output = If(outcome.StandardOutput, String.Empty).ToLowerInvariant()

        ' Detect explicit Get-Date usage or common time patterns
        If Not String.IsNullOrWhiteSpace(script) AndAlso script.Contains("get-date") Then
            Return True
        End If

        If Not String.IsNullOrWhiteSpace(output) Then
            If output.Contains("get-date") Then Return True
            If Regex.IsMatch(output, "\b\d{1,2}:\d{2}(:\d{2})?\s?(am|pm)?\b", RegexOptions.IgnoreCase) Then Return True
            If output.Contains("am") OrElse output.Contains("pm") Then Return True
        End If

        ' Custom function path
        If Not String.IsNullOrWhiteSpace(outcome.ToolName) AndAlso outcome.ToolName.Equals("GetCurrentDateTime", StringComparison.OrdinalIgnoreCase) Then
            Return True
        End If

        Return False
    End Function

    ''' <summary>
    ''' Detects all requested actions in the user's prompt
    ''' Returns list of action keywords that must be completed
    ''' </summary>
    Private Function DetectRequestedActions(originalRequest As String) As List(Of String)
        If String.IsNullOrWhiteSpace(originalRequest) Then Return New List(Of String)()
        
        Dim actions As New List(Of String)()
        Dim lowerRequest = originalRequest.ToLowerInvariant()
        
        ' Time / date requests
        If lowerRequest.Contains(" time") OrElse lowerRequest.Contains("time ") OrElse lowerRequest.Contains("clock") OrElse lowerRequest.Contains("date") OrElse lowerRequest.Contains("today") Then
            actions.Add("time")
        End If
         
        ' File operations
        If lowerRequest.Contains("search") OrElse lowerRequest.Contains("find") Then
            actions.Add("search")
        End If
        
        If lowerRequest.Contains("summar") Then
            actions.Add("summarize")
        End If
        
        If lowerRequest.Contains("read") AndAlso Not lowerRequest.Contains("summarize") Then
            actions.Add("read")
        End If

        ' ✅ NEW: Image analysis operations
        If lowerRequest.Contains("identify") OrElse lowerRequest.Contains("analyz") OrElse lowerRequest.Contains("check") OrElse lowerRequest.Contains("detect") Then
            ' If the request mentions images, this is an image analysis task
            If lowerRequest.Contains("image") OrElse lowerRequest.Contains("picture") OrElse lowerRequest.Contains("photo") OrElse lowerRequest.Contains("cartoon") Then
                actions.Add("analyze_image")
            End If
        End If

        ' ✅ NEW: Open/launch operations
        If lowerRequest.Contains("open") OrElse lowerRequest.Contains("launch") OrElse lowerRequest.Contains("start") OrElse lowerRequest.Contains("run") Then
            actions.Add("open")
        End If

        ' Image operations
        If lowerRequest.Contains("generat") AndAlso (lowerRequest.Contains("image") OrElse lowerRequest.Contains("picture")) Then
            actions.Add("generate_image")
        End If

        If lowerRequest.Contains("create") AndAlso (lowerRequest.Contains("image") OrElse lowerRequest.Contains("picture")) Then
            actions.Add("generate_image")
        End If

        ' File creation
        If lowerRequest.Contains("create") AndAlso lowerRequest.Contains("file") Then
            actions.Add("create_file")
        End If

        If lowerRequest.Contains("save") AndAlso lowerRequest.Contains("file") Then
            actions.Add("save_file")
        End If

        ' Updates
        If lowerRequest.Contains("update") OrElse lowerRequest.Contains("modify") OrElse lowerRequest.Contains("change") Then
            actions.Add("update")
        End If

        ' Web operations
        If lowerRequest.Contains("search") AndAlso (lowerRequest.Contains("web") OrElse lowerRequest.Contains("online") OrElse lowerRequest.Contains("internet")) Then
            actions.Add("web_search")
        End If

        ' Email operations
        If lowerRequest.Contains("email") OrElse lowerRequest.Contains("outlook") Then
            actions.Add("email")
        End If

        ' Screenshot
        If lowerRequest.Contains("screenshot") OrElse lowerRequest.Contains("screen shot") Then
            actions.Add("screenshot")
        End If

        Debug.WriteLine($"[GoalValidator] Detected {actions.Count} actions: {String.Join(", ", actions)}")

        Return actions.Distinct().ToList()
    End Function

    ''' <summary>
    ''' Checks which actions have been completed based on outcomes
    ''' </summary>
    Private Function GetCompletedActions() As List(Of String)
        Dim completed As New List(Of String)()
        Dim outcomes = GlobalOutcomeTracker.Instance.GetAllOutcomes()

        If outcomes.Count = 0 Then Return completed

        ' Check for search completion
        If outcomes.Any(Function(o) o.ToolName = "SearchForTextInsideFiles" AndAlso o.Status = OutcomeStatus.Success) Then
            completed.Add("search")
        End If

        ' Check for summarize completion
        If outcomes.Any(Function(o) o.ToolName = "ReadFileAndAnswer" AndAlso o.Status = OutcomeStatus.Success) Then
            completed.Add("summarize")
            completed.Add("read")
        End If

        ' ✅ NEW: Check for image analysis completion
        If outcomes.Any(Function(imgOutcome) imgOutcome.ToolName = "ImageAnswer" AndAlso imgOutcome.Status = OutcomeStatus.Success) Then
            completed.Add("analyze_image")
        End If

        ' ✅ NEW: Check for open completion (PowerShell with Start-Process or Invoke-Item)
        Dim hasOpenCommand As Boolean = False
        For Each outcome In outcomes
            If outcome.ToolName = "ExecutePowerShellScript" AndAlso outcome.Status = OutcomeStatus.Success Then
                Dim stdOut = If(outcome.StandardOutput, "").ToLowerInvariant()
                Dim argsStr = String.Join(" ", outcome.Arguments.Values.Select(Function(v) If(v IsNot Nothing, v.ToString(), ""))).ToLowerInvariant()
                If stdOut.Contains("start-process") OrElse stdOut.Contains("invoke-item") OrElse argsStr.Contains("start-process") OrElse argsStr.Contains("invoke-item") Then
                    hasOpenCommand = True
                    Exit For
                End If
            End If
        Next
        If hasOpenCommand Then
            completed.Add("open")
        End If

        ' Check for image generation completion
        If outcomes.Any(Function(genOutcome) genOutcome.ToolName = "GenerateImages" AndAlso genOutcome.Status = OutcomeStatus.Success) Then
            completed.Add("generate_image")
        End If

        ' Check for file creation completion
        Dim hasFileCreation = outcomes.Any(Function(o2) (o2.ToolName = "GenerateLargeFileWithTextOrCode" OrElse o2.ToolName.Contains("ExecutePowerShellScript")) AndAlso o2.Status = OutcomeStatus.Success)
        If hasFileCreation Then
            completed.Add("create_file")
            completed.Add("save_file")
        End If

        ' Check for update completion
        If outcomes.Any(Function(o3) o3.ToolName = "UpdateFileByChunks" AndAlso o3.Status = OutcomeStatus.Success) Then
            completed.Add("update")
        End If

        ' Check for web search completion
        Dim hasWebSearch = outcomes.Any(Function(o4) (o4.ToolName = "WebSearchAndRespondBasedOnPageContent" OrElse o4.ToolName = "ReadWebPageAndRespondBasedOnPageContent") AndAlso o4.Status = OutcomeStatus.Success)
        If hasWebSearch Then
            completed.Add("web_search")
        End If

        ' Check for email completion
        If outcomes.Any(Function(o5) o5.ToolName.Contains("Email") AndAlso o5.Status = OutcomeStatus.Success) Then
            completed.Add("email")
        End If

        ' Check for screenshot completion
        Dim hasScreenshot = outcomes.Any(Function(o6) (o6.ToolName = "TakePrintScreenOrScreenShot" OrElse o6.ToolName = "CheckMyScreenAndAnswer") AndAlso o6.Status = OutcomeStatus.Success)
        If hasScreenshot Then
            completed.Add("screenshot")
        End If

        ' Time completion
        If outcomes.Any(Function(o) o.Status = OutcomeStatus.Success AndAlso IsTimeOutcome(o)) Then
            completed.Add("time")
        End If

        Debug.WriteLine($"[GoalValidator] Completed {completed.Count} actions: {String.Join(", ", completed)}")

        Return completed.Distinct().ToList()
    End Function

    ''' <summary>
    ''' Enhanced goal achievement check with generic action tracking
    ''' NO HARDCODED PATTERNS - works for ANY request combination
    ''' </summary>
    Public Function IsGoalAchieved() As Boolean
        Dim outcomes = GlobalOutcomeTracker.Instance.GetAllOutcomes()

        ' If no outcomes yet, goal is NOT achieved
        If outcomes.Count = 0 Then
            Debug.WriteLine("[GoalValidator] No outcomes yet - goal not achieved")
            Return False
        End If

        ' Check for active failures (excluding skipped steps)
        Dim hasFailures = outcomes.Any(Function(outcome) (outcome.Status = OutcomeStatus.Failed OrElse outcome.Status = OutcomeStatus.ValidationFailed) AndAlso outcome.ToolName <> "Skipped")

        If hasFailures Then
            Debug.WriteLine("[GoalValidator] Has active failures - goal not achieved")
            Return False
        End If

        ' ═══════════════════════════════════════════════════════════════
        ' GENERIC ACTION TRACKING (NO HARDCODED PATTERNS)
        ' ═══════════════════════════════════════════════════════════════

        Dim requestedActions = DetectRequestedActions(Globals.OriginalUserRequest)
        Dim completedActions = GetCompletedActions()

        ' If we couldn't detect specific actions, use fallback logic
        If requestedActions.Count = 0 Then
            Debug.WriteLine("[GoalValidator] No specific actions detected, using fallback logic")
            Return FallbackGoalCheck(outcomes)
        End If

        ' Check if ALL requested actions are completed
        Dim missingActions As New List(Of String)()
        For Each action In requestedActions
            If Not completedActions.Contains(action) Then
                missingActions.Add(action)
            End If
        Next

        If missingActions.Count > 0 Then
            Debug.WriteLine($"[GoalValidator] Missing actions: {String.Join(", ", missingActions)}")
            Return False
        End If

        ' All requested actions completed!
        Debug.WriteLine($"[GoalValidator] ✅ All {requestedActions.Count} requested actions completed!")
        Return True
    End Function

    ''' <summary>
    ''' Fallback logic when specific actions cannot be detected
    ''' </summary>
    Private Function FallbackGoalCheck(outcomes As List(Of ExecutionOutcome)) As Boolean
        ' Count unique successful tools
        Dim uniqueSuccesses = outcomes _
            .Where(Function(o) o.Status = OutcomeStatus.Success AndAlso o.ToolName <> "Skipped") _
            .GroupBy(Function(o) o.ToolName) _
            .Count()

        ' If the original request clearly contains multiple actions (and/then), require at least 2 unique successes
        Dim req = Globals.OriginalUserRequest
        If Not String.IsNullOrWhiteSpace(req) Then
            Dim lr = req.ToLowerInvariant()
            Dim multi = lr.Contains(" and ") OrElse lr.Contains(" then ") OrElse lr.Contains(vbLf)
            If multi AndAlso uniqueSuccesses < 2 Then
                Debug.WriteLine("[GoalValidator] Multi-action prompt detected; waiting for additional successful steps")
                Return False
            End If
        End If

        Debug.WriteLine($"[GoalValidator] Fallback: {uniqueSuccesses} unique successful tools")

        ' Single PowerShell check (might need follow-up)
        If outcomes.Count = 1 AndAlso
           outcomes(0).ToolName = "ExecutePowerShellScript" AndAlso
           outcomes(0).Status = OutcomeStatus.Success Then
            Debug.WriteLine("[GoalValidator] Single PowerShell - may need follow-up")
            Return False
        End If

        ' If we have at least ONE unique success and no failures
        If uniqueSuccesses >= 1 Then
            Debug.WriteLine($"[GoalValidator] ✅ Fallback goal achieved with {uniqueSuccesses} success(es)")
            Return True
        End If

        Debug.WriteLine("[GoalValidator] Fallback: No clear success - continuing")
        Return False
    End Function

    ''' <summary>
    ''' Checks if there are failures that need retry
    ''' </summary>
    Public Function HasRecoverableFailures() As Boolean
        Dim recent = GlobalOutcomeTracker.Instance.GetRecentOutcomes(5)
        Return recent.Any(Function(o) o.Status = OutcomeStatus.Failed)
    End Function

    ''' <summary>
    ''' Gets a summary of what needs to be retried
    ''' </summary>
    Public Function GetRetryGuidance() As String
        Dim recent = GlobalOutcomeTracker.Instance.GetRecentOutcomes(5)
        Dim failures = recent.Where(Function(o) o.Status = OutcomeStatus.Failed).ToList()

        If failures.Count = 0 Then
            Return "No failures to retry."
        End If

        Dim sb As New System.Text.StringBuilder()
        sb.AppendLine("RETRY GUIDANCE:")

        For Each failure In failures
            Dim errorMsg = If(String.IsNullOrWhiteSpace(failure.StandardError),
                            "Unknown error",
                            failure.StandardError)
            sb.AppendLine($"- {failure.ToolName} failed: {errorMsg}")
        Next

        Return sb.ToString()
    End Function

    ''' <summary>
    ''' Gets a detailed status report of recent executions
    ''' </summary>
    Public Function GetGoalStatus() As String
        Dim recent = GlobalOutcomeTracker.Instance.GetRecentOutcomes(10)

        If recent.Count = 0 Then
            Return "No executions recorded yet."
        End If

        Dim sb As New System.Text.StringBuilder()
        sb.AppendLine("=== GOAL STATUS ===")

        Dim successCount = recent.Where(Function(o) o.Status = OutcomeStatus.Success).Count()
        Dim failCount = recent.Where(Function(o) o.Status = OutcomeStatus.Failed).Count()
        Dim partialCount = recent.Where(Function(o) o.Status = OutcomeStatus.PartialSuccess).Count()

        sb.AppendLine($"Total Steps: {recent.Count}")
        sb.AppendLine($"Successful: {successCount}")
        sb.AppendLine($"Failed: {failCount}")
        sb.AppendLine($"Partial: {partialCount}")

        ' Show what actions are still missing
        Dim requestedActions = DetectRequestedActions(Globals.OriginalUserRequest)
        Dim completedActions = GetCompletedActions()

        If requestedActions.Count > 0 Then
            sb.AppendLine()
            sb.AppendLine("Requested Actions:")
            For Each action In requestedActions
                Dim status = If(completedActions.Contains(action), "✓", "✗")
                sb.AppendLine($"  {status} {action}")
            Next
        End If

        If failCount > 0 Then
            sb.AppendLine()
            sb.AppendLine("Recent Failures:")
            For Each failure In recent.Where(Function(o) o.Status = OutcomeStatus.Failed).Take(3)
                sb.AppendLine($"  • {failure.ToolName}: {failure.StandardError}")
            Next
        End If

        Return sb.ToString()
    End Function

End Module

' ###  ExecutorAgent.vb - v2.3.0 ###
' Executes plan steps and tracks outcomes
' REFACTORED: All LocalAI+Piper logic moved to final summary only (HandleUserRequest)
' Step execution is now clean - no per-step summarization

Imports System.Threading
Imports System.Text
Imports Newtonsoft.Json
Imports System.Collections.Generic
Imports System.Linq
Imports System.Text.RegularExpressions

Public Module ExecutorAgent

    ' Tracks executed tool+args hashes within the current request to avoid repeats
    Private ReadOnly executionLedger As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)

    ''' <summary>
    ''' Resets the execution ledger for a new session
    ''' </summary>
    Public Sub ResetExecutionLedger()
        executionLedger.Clear()
        GlobalOutcomeTracker.Instance.Clear()
        Debug.WriteLine("[ExecutorAgent] Execution ledger reset")
    End Sub

    ''' <summary>
    ''' Executes a plan (list of steps) sequentially
    ''' </summary>
    Public Async Function ExecutePlanAsync(
        plan As List(Of PlanStep),
        ct As CancellationToken
    ) As Task

        If plan Is Nothing OrElse plan.Count = 0 Then
            Debug.WriteLine("[ExecutorAgent] Empty plan provided")
            Return
        End If

        Debug.WriteLine($"[ExecutorAgent] Executing plan with {plan.Count} steps")

        For i = 0 To plan.Count - 1
            If ct.IsCancellationRequested Then
                Debug.WriteLine("[ExecutorAgent] Execution cancelled")
                Exit For
            End If

            Dim currentStep = plan(i)
            currentStep.StepIndex = i + 1

            Try
                Await ExecuteSingleStep(currentStep, ct)
            Catch ex As Exception
                Debug.WriteLine($"[ExecutorAgent] Step {i + 1} failed: {ex.Message}")
                ' Continue with next step even if one fails
            End Try
        Next

        Debug.WriteLine("[ExecutorAgent] Plan execution complete")
    End Function

    ''' <summary>
    ''' Executes a single step from the plan
    ''' </summary>
    Private Async Function ExecuteSingleStep(
        planStep As PlanStep,
        ct As CancellationToken
    ) As Task

        ' Safety check for null tool
        If String.IsNullOrWhiteSpace(planStep.Tool) Then
            Debug.WriteLine($"[ExecutorAgent] Step {planStep.StepIndex} has no tool name.")
            StepOutputManager.Instance.RecordStepOutput(
                planStep.StepIndex,
                "Unknown",
                "[ERROR] Tool name is missing or empty.",
                False
            )
            Return
        End If

        ' Normalize known argument aliases before validation
        If planStep.Tool.Equals("ChangeOrSetVolume", StringComparison.OrdinalIgnoreCase) Then
            If Not planStep.Args.ContainsKey("volumePercentage") Then
                If planStep.Args.ContainsKey("level") Then
                    planStep.Args("volumePercentage") = planStep.Args("level")
                ElseIf planStep.Args.ContainsKey("volume") Then
                    planStep.Args("volumePercentage") = planStep.Args("volume")
                End If
            End If
        ElseIf planStep.Tool.Equals("WebSearchAndRespondBasedOnPageContent", StringComparison.OrdinalIgnoreCase) Then
            ' Accept planner aliases: query -> promptQuery, site -> siteName, question <-> query
            If Not planStep.Args.ContainsKey("promptQuery") AndAlso planStep.Args.ContainsKey("query") Then
                planStep.Args("promptQuery") = planStep.Args("query")
            End If
            If Not planStep.Args.ContainsKey("siteName") AndAlso planStep.Args.ContainsKey("site") Then
                planStep.Args("siteName") = planStep.Args("site")
            End If
            If Not planStep.Args.ContainsKey("query") AndAlso planStep.Args.ContainsKey("question") Then
                planStep.Args("query") = planStep.Args("question")
            End If
            If Not planStep.Args.ContainsKey("question") AndAlso planStep.Args.ContainsKey("query") Then
                planStep.Args("question") = planStep.Args("query")
            End If
        ElseIf planStep.Tool.Equals("TakePrintScreenOrScreenShot", StringComparison.OrdinalIgnoreCase) Then
            If Not planStep.Args.ContainsKey("OutputPath") Then
                Dim altKeys = New String() {"outputPath", "path", "folder", "folderPath"}
                For Each k In altKeys
                    If planStep.Args.ContainsKey(k) Then
                        planStep.Args("OutputPath") = planStep.Args(k)
                        Exit For
                    End If
                Next
            End If
        ElseIf planStep.Tool.Equals("GenerateBatchAndPs1File", StringComparison.OrdinalIgnoreCase) Then
            ' Accept planner aliases for userQuery: query, prompt, request, description, task
            If Not planStep.Args.ContainsKey("userQuery") Then
                Dim altKeys = New String() {"query", "prompt", "request", "description", "task", "instruction", "script"}
                For Each k In altKeys
                    If planStep.Args.ContainsKey(k) Then
                        planStep.Args("userQuery") = planStep.Args(k)
                        Debug.WriteLine($"[ExecutorAgent] GenerateBatchAndPs1File: Mapped '{k}' to 'userQuery'")
                        Exit For
                    End If
                Next
            End If
            ' Accept planner aliases for outputFolder: folder, path, outputPath, directory
            If Not planStep.Args.ContainsKey("outputFolder") Then
                Dim altKeys = New String() {"folder", "path", "outputPath", "directory", "folderPath"}
                For Each k In altKeys
                    If planStep.Args.ContainsKey(k) Then
                        planStep.Args("outputFolder") = planStep.Args(k)
                        Debug.WriteLine($"[ExecutorAgent] GenerateBatchAndPs1File: Mapped '{k}' to 'outputFolder'")
                        Exit For
                    End If
                Next
            End If
        End If

        Dim argsHash = ExecutionOutcome.ComputeArgsHash(planStep.Args)
        Dim ledgerKey = $"{planStep.Tool}|{argsHash}"

        Dim attemptNumber = RetryStrategy.MaxRetriesPerTool - RetryStrategy.GetRemainingRetries(planStep.Tool) + 1

        Dim outcome As New ExecutionOutcome With {
            .StepIndex = planStep.StepIndex,
            .ToolName = planStep.Tool,
            .Arguments = New Dictionary(Of String, Object)(planStep.Args),
            .ArgsHash = argsHash,
            .StartTime = DateTimeOffset.UtcNow,
            .AttemptNumber = Math.Max(attemptNumber, 1),
            .IsPowerShell = planStep.Tool.Equals("ExecutePowerShellScript", StringComparison.OrdinalIgnoreCase)
        }

        Try
            Debug.WriteLine($"[ExecutorAgent] Executing step {planStep.StepIndex}: {planStep.Tool}")

            ' Halt if retries exhausted
            If Not RetryStrategy.CanRetry(planStep.Tool) Then
                outcome.Status = OutcomeStatus.Failed
                outcome.StandardError = $"[RETRY LIMIT] {planStep.Tool} reached {RetryStrategy.MaxRetriesPerTool} attempts."
                outcome.Complete(outcome.Status)
                GlobalOutcomeTracker.Instance.RecordOutcome(outcome)
                StepOutputManager.Instance.RecordStepOutput(planStep.StepIndex, planStep.Tool, outcome.StandardError, False)
                Return
            End If

            ' Skip duplicate successful executions
            If executionLedger.Contains(ledgerKey) OrElse GlobalOutcomeTracker.Instance.HasSuccessfulOutcome(planStep.Tool, argsHash) Then
                Dim skipMsg = "[SKIPPED] Identical tool+args already succeeded."
                outcome.Status = OutcomeStatus.Success
                outcome.StandardOutput = skipMsg
                outcome.Complete(outcome.Status)
                GlobalOutcomeTracker.Instance.RecordOutcome(outcome)
                StepOutputManager.Instance.RecordStepOutput(planStep.StepIndex, planStep.Tool, skipMsg, True)
                Debug.WriteLine($"[ExecutorAgent] Skipped duplicate for {ledgerKey}")
                Return
            End If

            executionLedger.Add(ledgerKey)

            ' Validate step first
            Dim validationResult = ToolSchemaValidator.ValidateStep(planStep)
            If Not validationResult.IsValid Then
                outcome.Status = OutcomeStatus.ValidationFailed
                outcome.StandardError = String.Join("; ", validationResult.Errors)
                outcome.Complete(OutcomeStatus.ValidationFailed)
                GlobalOutcomeTracker.Instance.RecordOutcome(outcome)
                StepOutputManager.Instance.RecordStepOutput(
                    planStep.StepIndex,
                    planStep.Tool,
                    $"[VALIDATION FAILED] {outcome.StandardError}",
                    False
                )
                RetryStrategy.RecordRetry(planStep.Tool)
                Debug.WriteLine($"[ExecutorAgent] Validation failed: {outcome.StandardError}")
                Return
            End If

            ' Execute based on tool type
            Dim resultText As String = ""

            Select Case planStep.Tool.ToLower()
                Case "readfileandanswer"
                    resultText = Await ExecuteReadFileAndAnswer(planStep, ct)

                Case "imageanswer"
                    resultText = Await ExecuteImageAnswer(planStep, ct)

                Case "searchfortextinsidefiles"
                    resultText = Await ExecuteSearchForTextInsideFiles(planStep, ct)

                Case "generateimages"
                    resultText = Await ExecuteGenerateImages(planStep, ct)

                Case "executepowershellscript"
                    resultText = Await ExecutePowerShellScript(planStep, ct)

                Case "freeresponse"
                    resultText = ExecuteFreeResponse(planStep)

                Case Else
                    resultText = Await ExecuteCustomFunction(planStep, ct)
            End Select

            ' Record success/failure
            outcome.StandardOutput = resultText
            outcome.PolicyBlocked = resultText.Contains("[POLICY]")
            outcome.Status = If(resultText.Contains("[ERROR]") OrElse resultText.Contains("[POLICY]") OrElse resultText.StartsWith("❌"), OutcomeStatus.Failed, OutcomeStatus.Success)
            
            If planStep.Tool.Equals("ExecutePowerShellScript", StringComparison.OrdinalIgnoreCase) AndAlso planStep.Args.ContainsKey("script") Then
                outcome.SetScriptAndHash(CStr(planStep.Args("script")))
                If resultText.StartsWith("❌") Then
                    outcome.ExitCode = 1
                    outcome.StandardError = resultText
                    outcome.RemediationNote = "PowerShell execution failed"
                Else
                    outcome.ExitCode = 0
                    outcome.StandardOutput = resultText
                End If
            End If
            outcome.Complete(outcome.Status)

            ' Track outcome
            GlobalOutcomeTracker.Instance.RecordOutcome(outcome)

            ' Store in step output manager
            StepOutputManager.Instance.RecordStepOutput(
                planStep.StepIndex,
                planStep.Tool,
                resultText,
                outcome.Status = OutcomeStatus.Success
            )
            
            ' Display result once (successes only, non-skip messages)
            If outcome.Status = OutcomeStatus.Success AndAlso Not String.IsNullOrWhiteSpace(resultText) AndAlso Not resultText.Contains("[SKIPPED]") Then
                Shelly.Instance.AppendResultToBox(resultText & Environment.NewLine)
            End If

        Catch ex As OperationCanceledException
            outcome.Status = OutcomeStatus.Cancelled
            outcome.StandardError = "Cancelled by user"
            outcome.Complete(OutcomeStatus.Cancelled)
            GlobalOutcomeTracker.Instance.RecordOutcome(outcome)

        Catch ex As Exception
            outcome.Status = OutcomeStatus.Failed
            outcome.StandardError = ex.Message
            outcome.Complete(OutcomeStatus.Failed)
            GlobalOutcomeTracker.Instance.RecordOutcome(outcome)
            StepOutputManager.Instance.RecordStepOutput(
                planStep.StepIndex,
                planStep.Tool,
                $"[EXECUTION FAILED] {ex.Message}",
                False
            )
            RetryStrategy.RecordRetry(planStep.Tool)
            Debug.WriteLine($"[ExecutorAgent] Step exception: {ex.Message}")
        End Try
    End Function

    ' ========================================
    ' INDIVIDUAL STEP EXECUTORS
    ' ========================================

    Private Async Function ExecuteReadFileAndAnswer(planStep As PlanStep, ct As CancellationToken) As Task(Of String)
        Dim paths = CStr(planStep.Args("filePaths"))
        Dim query = CStr(planStep.Args("query"))
        Return Await CustomFunctions2.ReadFileAndAnswer(paths, query)
    End Function

    Private Async Function ExecuteImageAnswer(planStep As PlanStep, ct As CancellationToken) As Task(Of String)
        Dim imgs = CStr(planStep.Args("imagePaths"))
        Dim query = CStr(planStep.Args("query"))
        Return Await CustomFunctions.ImageAnswer(imgs, query)
    End Function

    Private Async Function ExecuteSearchForTextInsideFiles(planStep As PlanStep, ct As CancellationToken) As Task(Of String)
        Dim paths = CStr(planStep.Args("paths"))
        Dim searchWord = CStr(planStep.Args("searchText"))
        Return Await CustomFunctions2.SearchForTextInsideFiles(paths, searchWord)
    End Function

    Private Async Function ExecuteGenerateImages(planStep As PlanStep, ct As CancellationToken) As Task(Of String)
        Dim imgPrompt = CStr(planStep.Args("imagePrompt"))
        Dim num = Convert.ToInt32(planStep.Args("numImages"))
        Dim style = CStr(planStep.Args("style"))
        Dim folder = CStr(planStep.Args("folderPath"))
        Return Await CustomFunctions.GenerateImages(imgPrompt, num, style, folder)
    End Function

    Private Function CheckPolicyBlocks(script As String) As String
        Dim lowered = script.ToLowerInvariant()
        Dim denyPatterns As String() = {
            "remove-item", "remove-childitem", "rmdir", "del ", "format-volume", "clear-content",
            "stop-service", "stop-process", "set-executionpolicy", "disable-scheduledtask", "unregister-scheduledtask",
            "erase", "rm ", "shutdown", "restart-computer", "set-itemproperty", "new-itemproperty"
        }
        Dim allowPatterns As String() = {"get-", "select-", "measure-", "test-"}

        For Each deny In denyPatterns
            If lowered.Contains(deny) Then
                If allowPatterns.Any(Function(a) lowered.Contains(a)) Then Continue For
                Return $"[POLICY] Blocked destructive command: {deny}."
            End If
        Next
        Return Nothing
    End Function

    Private Async Function ExecutePowerShellScript(planStep As PlanStep, ct As CancellationToken) As Task(Of String)
        Dim script = HelperFunctions.StripCodeFences(CStr(planStep.Args("script")))

        Dim policyBlock = CheckPolicyBlocks(script)
        If Not String.IsNullOrWhiteSpace(policyBlock) Then Return policyBlock

        Dim parseErrors As List(Of String) = Nothing
        If Not PowerShellParser.TryParsePowerShellScript(script, parseErrors) Then
            Return "[PARSER] PowerShell syntax errors: " & String.Join(" | ", parseErrors.Take(3))
        End If

        Dim validation = PowerShellScriptSafety.Inspect(script, SecurityFlags.BlockSystemC)
        If Not validation.IsValid Then
            Return $"[SECURITY] Script blocked: {validation.BlockReason}"
        End If

        Dim psResult = Await ExecutePowerShellScriptAsync(script, ct)

        If psResult.Item1 Then
            Return If(String.IsNullOrWhiteSpace(psResult.Item2), "Done.", psResult.Item2)
        Else
            Return If(String.IsNullOrWhiteSpace(psResult.Item2), "❌ PowerShell execution failed", $"❌ {psResult.Item2}")
        End If
    End Function

    Private Function ExecuteFreeResponse(planStep As PlanStep) As String
        Return CStr(planStep.Args("text"))
    End Function

    Private Async Function ExecuteCustomFunction(planStep As PlanStep, ct As CancellationToken) As Task(Of String)
        Try
            Return Await CustomFunctionsEngine.ExecuteAppFunctionDirectAsync(planStep.Tool, planStep.Args, ct)
        Catch ex As Exception
            Return $"[ERROR] Function execution failed: {planStep.Tool} - {ex.Message}"
        End Try
    End Function

End Module

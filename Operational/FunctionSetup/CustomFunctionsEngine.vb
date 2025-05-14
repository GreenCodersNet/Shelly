' ###  CustomFunctionsEngine.vb - v1.0.1 ### 

' ##########################################################
'  Shelly - v1.0.1
'  License: Creative Commons Attribution-NonCommercial (CC BY-NC)
'  https://creativecommons.org/licenses/by-nc/4.0/
'  © 2025 Vlad Stefanescu | GreenCoders.net. Attribution required.
' ##########################################################

' ********** FOR NEW FUNCTIONS UPDATE: **********
'   -> ToolPlanner.vb
'   -> CustomFunctionsEngine.vb
'   -> CustomFunctions.vb
'   -> ExecutorAgent.vb
'   -> CustomFunctionsEngine.VB (FOR CONSOLE HELP)
' ***********************************************

Imports System.Reflection
Imports System.Text
Imports System.Text.RegularExpressions
Imports System.Threading

<AttributeUsage(AttributeTargets.Method, Inherited:=False)>
Public Class CustomFunctionAttribute
    Inherits Attribute
    Public Property Description As String
    Public Property Example As String

    Public Sub New(description As String, example As String)
        Me.Description = description
        Me.Example = example
    End Sub
End Class

Public Module CustomFunctionsEngine

    Public FunctionRegistryInstance As FunctionRegistry ' Local reference to the function registry

    Private ReadOnly RecognizedFunctions As String() = {
    "WriteInsideFileOrWindow",
    "CheckMyScreenAndAnswer",
    "StartOrRunApplicationByName",
    "ReadFileAndAnswer",
    "GenerateLargeFileWithTextOrCode",
    "UpdateFileByChunks",
    "GenerateImages",
    "ChangeOrSetVolume",
    "SendMediaKey",
    "WebSearchAndRespondBasedOnPageContent",
    "TakePrintScreenOrScreenShot",
    "ImageAnswer",
    "ReadCopilotConversation",
    "SearchForTextInsideFiles",
    "ReadWebPageAndRespondBasedOnPageContent",
    "GenerateBatchAndPs1File"
}

    Public Function GetRecognizedFunctions() As String
        Return String.Join(vbCrLf, RecognizedFunctions)
    End Function

    ' Initialize all functions in the function registry
    Public Sub InitializeFunctions(registry As FunctionRegistry)
        FunctionRegistryInstance = registry

        Dim customFunctionsType As Type = GetType(CustomFunctions)

        ' --- Register VB.NET functions ---
        Dim methods = customFunctionsType.GetMethods(BindingFlags.Public Or BindingFlags.Static)
        For Each method As MethodInfo In methods
            ' Exclude methods intended for internal use (e.g. those ending with "Script")
            If Not method.Name.EndsWith("Script") Then
                Dim paramList As New List(Of String)()
                For Each param As ParameterInfo In method.GetParameters()
                    paramList.Add($"{param.Name} As {param.ParameterType.Name}")
                Next

                ' Check for custom attribute metadata (if provided)
                Dim attr As CustomFunctionAttribute = method.GetCustomAttribute(Of CustomFunctionAttribute)()
                Dim description As String
                Dim example As String
                If attr IsNot Nothing Then
                    description = attr.Description
                    example = attr.Example
                Else
                    description = $"Automatically registered Function: {method.Name}"
                    example = $"{method.Name}({String.Join(", ", paramList)})"
                End If

                registry.AddFunction(New FunctionDefinition(
                                      method.Name,
                                      description,
                                      paramList,
                                      example,
                                      FunctionType.VBFunction,
                                      ""
                                  ))
            End If
        Next

        ' --- Register PowerShell script functions from fields ---
        Dim fields = customFunctionsType.GetFields(BindingFlags.Public Or BindingFlags.Static Or BindingFlags.FlattenHierarchy)
        For Each field As FieldInfo In fields
            If field.FieldType Is GetType(String) AndAlso field.Name.EndsWith("Script") Then
                Dim scriptName As String = field.Name.Substring(0, field.Name.Length - "Script".Length)
                Dim scriptContent As String = CStr(field.GetValue(Nothing))
                Dim paramList As New List(Of String)()

                ' Automatically parse parameter definitions from the PowerShell script using a regex.
                Dim paramBlockPattern As String = "param\s*\(\s*(.*?)\s*\)"
                Dim match As Match = Regex.Match(scriptContent, paramBlockPattern, RegexOptions.Singleline Or RegexOptions.IgnoreCase)
                If match.Success Then
                    Dim paramsText As String = match.Groups(1).Value
                    ' Split parameters by commas or newlines.
                    Dim paramCandidates As String() = Regex.Split(paramsText, "[,\r\n]+")
                    For Each candidate As String In paramCandidates
                        Dim trimmed As String = candidate.Trim()
                        If Not String.IsNullOrEmpty(trimmed) Then
                            ' Expect parameter in the form: [Type]$ParameterName (optionally with a default value)
                            Dim paramMatch As Match = Regex.Match(trimmed, "\[(.*?)\]\s*\$(\w+)")
                            If paramMatch.Success Then
                                Dim typeStr As String = paramMatch.Groups(1).Value
                                Dim nameStr As String = paramMatch.Groups(2).Value
                                paramList.Add($"{nameStr} As {typeStr}")
                            Else
                                ' Fallback: if the pattern isn't matched, use the raw string with a default type.
                                paramList.Add($"{trimmed} As String")
                            End If
                        End If
                    Next
                End If

                registry.AddFunction(New FunctionDefinition(
                                      scriptName,
                                      $"PowerShell script function: {scriptName}",
                                      paramList,
                                      $"{scriptName}({String.Join(", ", paramList)})",
                                      FunctionType.PowerShellScript,
                                      scriptContent
                                  ))
            End If
        Next
    End Sub

    ' Helper method to split parameters while respecting quoted strings
    Public Function SplitParameters(parameters As String) As List(Of String)
        ' Replace single quotes with double quotes to normalize the input.
        parameters = parameters.Replace("'", """")
        Dim paramList As New List(Of String)()
        ' Split on commas that are not inside double quotes.
        Dim splitted = Regex.Split(parameters, ",(?=(?:[^""]*""[^""]*"")*[^""]*$)")
        For Each part In splitted
            Dim trimmed = part.Trim()
            ' If the entire chunk is wrapped in double quotes, remove them.
            If trimmed.StartsWith("""") AndAlso trimmed.EndsWith("""") Then
                trimmed = trimmed.Substring(1, trimmed.Length - 2)
            End If
            paramList.Add(trimmed)
        Next
        Return paramList
    End Function


    Public Async Function ExecuteAppFunctionAsync(functionCall As String, ct As CancellationToken) As Task(Of String)
        Try
            ' Clean up any stray PowerShell fences
            functionCall = functionCall.Replace("```powershell", "").Replace("```", "").Trim()

            ' Basic pattern: functionName(params)
            Dim pattern As String = "([A-Za-z_][A-Za-z0-9_]*)\s*\((.*)\)"
            Dim match As Match = Regex.Match(functionCall, pattern)

            If match.Success Then
                Dim functionName As String = match.Groups(1).Value
                Dim parameters As String = match.Groups(2).Value
                Dim paramList As List(Of String) = SplitParameters(parameters)

                Dim funcDef = FunctionRegistryInstance.GetFunction(functionName)
                If funcDef Is Nothing Then
                    Throw New MissingMethodException($"Function '{functionName}' is not registered.")
                End If

                Debug.WriteLine($"[DEBUG] Found function '{funcDef.Name}' of type '{funcDef.FunctionType}'")

                Dim resultString As String = ""
                If funcDef.FunctionType = FunctionType.VBFunction Then
                    Dim vbMethod = GetType(CustomFunctions).GetMethod(functionName, BindingFlags.Public Or BindingFlags.Static)
                    If vbMethod IsNot Nothing Then
                        Dim vbParams As New List(Of Object)()
                        Dim vbParamInfo = vbMethod.GetParameters()
                        For i As Integer = 0 To vbParamInfo.Length - 1
                            If i >= paramList.Count Then
                                vbParams.Add(If(vbParamInfo(i).HasDefaultValue, vbParamInfo(i).DefaultValue, Nothing))
                            Else
                                Dim paramType = vbParamInfo(i).ParameterType
                                Dim paramValue = Convert.ChangeType(paramList(i), paramType)
                                Debug.WriteLine($"[DEBUG] Param index={i}, Name={vbParamInfo(i).Name}, Type={paramType}, Value='{paramList(i)}'")
                                vbParams.Add(paramValue)
                            End If
                        Next

                        Dim resultObj As Object = vbMethod.Invoke(Nothing, vbParams.ToArray())

                        If TypeOf resultObj Is Task Then
                            Dim taskType As Type = resultObj.GetType()
                            Await DirectCast(resultObj, Task)
                            If taskType.IsGenericType Then
                                Dim resultProp = taskType.GetProperty("Result")
                                Dim finalResult = resultProp.GetValue(resultObj)
                                resultString = If(finalResult IsNot Nothing, finalResult.ToString(), "")
                            End If
                        Else
                            resultString = If(resultObj IsNot Nothing, resultObj.ToString(), "")
                        End If

                        ' Append result to UI if not empty.
                        If Not String.IsNullOrEmpty(resultString) Then
                            Return resultString
                        End If

                        Globals.TaskCompleted = True
                    End If

                ElseIf funcDef.FunctionType = FunctionType.PowerShellScript Then
                    Dim psScript As String = funcDef.Implementation
                    Dim paramNames As List(Of String) = funcDef.Parameters.Select(Function(p) p.Split(" "c)(0)).ToList()
                    Dim paramPairs As New Dictionary(Of String, String)()
                    For i As Integer = 0 To paramNames.Count - 1
                        If i < paramList.Count Then
                            paramPairs.Add(paramNames(i), paramList(i))
                        End If
                    Next

                    Dim scriptBuilder As New StringBuilder()
                    scriptBuilder.AppendLine(psScript)
                    scriptBuilder.Append($"{funcDef.Name} ")
                    For Each kvp In paramPairs
                        scriptBuilder.Append($"'{kvp.Value}' ")
                    Next

                    Dim fullScript As String = scriptBuilder.ToString()
                    Dim executionResult As Tuple(Of Boolean, String) = Await Shelly.Instance.ExecutePowerShellScriptAsync(fullScript, ct)

                    If executionResult.Item1 Then
                        Dim psOutput As String = executionResult.Item2
                        If Not String.IsNullOrWhiteSpace(psOutput) Then
                            Debug.WriteLine("9 ->" & psOutput)
                            AppendResultToBox($"{Environment.NewLine}{psOutput}{Environment.NewLine}")
                        Else
                            Debug.WriteLine("10 ->" & "Task completed")
                            AppendResultToBox($"Task completed.{Environment.NewLine}")
                        End If

                        Dim shortResult As String = TruncateTextToMaxTokens(psOutput, 150)
                        If String.IsNullOrWhiteSpace(shortResult) Then shortResult = "[No output returned by script]"
                        Globals.conversationHistory.Add(New Dictionary(Of String, String) From {
                        {"role", "assistant"}, {"content", shortResult}
                    })
                        Await convHistory.TrimConversationHistoryByTokens_Dict(Globals.conversationHistory, Globals.MaxTotalTokens, shortResult)
                        resultString = shortResult
                    Else
                        Dim err = $"PowerShell Script '{funcDef.Name}' failed: {executionResult.Item2}"
                            Shelly.Instance.AIresponseErrorBox.AppendText(err & Environment.NewLine)
                        Debug.WriteLine(err)
                        resultString = err
                    End If
                End If

                ' Log the function call and its result in conversation history for context.
                Dim logMsg As String = $"Executed function '{functionName}' with parameters: ({String.Join(", ", paramList)})" & vbCrLf &
                                   $"Result: {resultString}"
                Globals.conversationHistory.Add(New Dictionary(Of String, String) From {
                {"role", "assistant"}, {"content", logMsg}
            })
                Await convHistory.TrimConversationHistoryByTokens_Dict(Globals.conversationHistory, Globals.MaxTotalTokens, logMsg)

                Return resultString
            Else
                Throw New FormatException("Invalid function call format.")
            End If

        Catch ex As Exception
            Debug.WriteLine($"Error executing function: {ex.Message}")
            Shelly.Instance.LabelStatusUpdate.Text = $"Execution Error: {ex.Message}"
            Return $"Execution Error: {ex.Message}"
        End Try
    End Function


    '  =============== REMOVE CODE BLOCKS FOR CUSTOM FUNCTIONS ===============

    Public Function RemoveCustomFunctionCodeBlocks(aiResponse As String) As String
        ' 1) Split into lines
        Dim lines As List(Of String) = aiResponse.Split({vbCrLf, vbCrLf, vbLf}, StringSplitOptions.None).ToList()

        For i As Integer = 0 To lines.Count - 1
            Dim currentLine As String = lines(i).Trim()

            ' 2) Check if the line starts with any recognized function + "("
            For Each funcName In RecognizedFunctions
                ' E.g., line starts with "WriteInsideFileOrWindow(" (case-insensitive)
                If currentLine.StartsWith(funcName & "(", StringComparison.OrdinalIgnoreCase) Then
                    ' 3) Check the line above (i-1) if it exists
                    If i - 1 >= 0 Then
                        Dim prevLine As String = lines(i - 1).Trim()
                        If prevLine.Contains("```powershell") Then
                            ' Remove that triple backtick line by clearing it
                            lines(i - 1) = ""
                        End If
                    End If

                    ' 4) Check the line below (i+1) if it exists
                    If i + 1 < lines.Count Then
                        Dim nextLine As String = lines(i + 1).Trim()
                        If nextLine.Contains("```") Then
                            ' Remove that triple backtick line by clearing it
                            lines(i + 1) = ""
                        End If
                    End If

                    ' We found a recognized function; no need to check other function names on this same line
                    Exit For
                End If
            Next
        Next

        ' 5) Rejoin the cleaned lines

        Return String.Join(vbCrLf, lines)
    End Function

End Module


' PowerShellParser.vb
' Lightweight AST validation using reflection to avoid hard dependency on System.Management.Automation

Imports System.Collections.Generic
Imports System.Reflection

Public Module PowerShellParser
    ''' <summary>
    ''' Attempts to parse a PowerShell script via System.Management.Automation.Language.Parser (if available).
    ''' Returns True when no parse errors or when parser is unavailable (fail-open), False when syntax errors are detected.
    ''' </summary>
    Public Function TryParsePowerShellScript(script As String, ByRef errors As List(Of String)) As Boolean
        errors = New List(Of String)()

        Try
            Dim asm As Assembly = Nothing
            For Each loadedAsm As Assembly In AppDomain.CurrentDomain.GetAssemblies()
                If loadedAsm.FullName.StartsWith("System.Management.Automation", StringComparison.OrdinalIgnoreCase) Then
                    asm = loadedAsm
                    Exit For
                End If
            Next

            If asm Is Nothing Then
                Try
                    asm = Assembly.Load("System.Management.Automation")
                Catch
                    asm = Nothing
                End Try
            End If

            If asm Is Nothing Then
                Return True ' parser not available
            End If

            Dim parserType = asm.GetType("System.Management.Automation.Language.Parser")
            Dim tokenType = asm.GetType("System.Management.Automation.Language.Token")
            Dim parseErrorType = asm.GetType("System.Management.Automation.Language.ParseError")
            If parserType Is Nothing OrElse tokenType Is Nothing OrElse parseErrorType Is Nothing Then
                Return True
            End If

            Dim tokensArray = Array.CreateInstance(tokenType, 0)
            Dim errorsArray As Array = Nothing
            Dim parseInput = parserType.GetMethod("ParseInput", BindingFlags.Public Or BindingFlags.Static, Nothing, New Type() {GetType(String), tokenType.MakeArrayType().MakeByRefType(), parseErrorType.MakeArrayType().MakeByRefType()}, Nothing)
            If parseInput Is Nothing Then Return True

            Dim parameters = New Object() {script, tokensArray, errorsArray}
            parseInput.Invoke(Nothing, parameters)
            errorsArray = TryCast(parameters(2), Array)

            If errorsArray IsNot Nothing AndAlso errorsArray.Length > 0 Then
                For Each err As Object In errorsArray
                    Dim msgProp = parseErrorType.GetProperty("Message")
                    Dim extentProp = parseErrorType.GetProperty("Extent")
                    Dim message = If(msgProp IsNot Nothing, CStr(msgProp.GetValue(err)), "Parse error")
                    Dim extent = If(extentProp IsNot Nothing, extentProp.GetValue(err), Nothing)
                    Dim line = 0
                    Dim col = 0
                    Dim snippet As String = ""
                    If extent IsNot Nothing Then
                        Dim textProp = extent.GetType().GetProperty("Text")
                        Dim lineProp = extent.GetType().GetProperty("StartLineNumber")
                        Dim colProp = extent.GetType().GetProperty("StartColumnNumber")
                        If textProp IsNot Nothing Then snippet = CStr(textProp.GetValue(extent))
                        If lineProp IsNot Nothing Then line = CInt(lineProp.GetValue(extent))
                        If colProp IsNot Nothing Then col = CInt(colProp.GetValue(extent))
                    End If
                    errors.Add(String.Format("{0} at line {1}, col {2}: {3}", message, line, col, snippet))
                Next
                Return False
            End If

        Catch ex As Exception
            Debug.WriteLine(String.Format("[PowerShellParser] Reflection parse failed: {0}", ex.Message))
            Return True ' fail open
        End Try

        Return True
    End Function
End Module

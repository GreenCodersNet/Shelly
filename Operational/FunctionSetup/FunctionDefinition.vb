' ###  FunctionDefinition.vb - v1.0.1 ### 

' ##########################################################
'  Shelly - v1.0.1
'  License: Creative Commons Attribution-NonCommercial (CC BY-NC)
'  https://creativecommons.org/licenses/by-nc/4.0/
'  © 2025 Vlad Stefanescu | GreenCoders.net. Attribution required.
' ##########################################################

Public Enum FunctionType
    VBFunction
    PowerShellScript
End Enum

Public Class FunctionDefinition
    Public Property Name As String
    Public Property Description As String
    Public Property Parameters As List(Of String)
    Public Property Example As String
    Public Property FunctionType As FunctionType
    Public Property Implementation As String
    Public Property ScriptFunctionName As String

    Public Sub New(name As String, description As String, parameters As List(Of String), example As String, functionType As FunctionType, implementation As String, Optional scriptFunctionName As String = "")
        Me.Name = name
        Me.Description = description
        Me.Parameters = parameters
        Me.Example = example
        Me.FunctionType = functionType
        Me.Implementation = implementation
        Me.ScriptFunctionName = If(String.IsNullOrEmpty(scriptFunctionName), name, scriptFunctionName)
    End Sub
End Class

Public Class FunctionRegistry
    Private FunctionList As New Dictionary(Of String, FunctionDefinition)

    Public Sub AddFunction(func As FunctionDefinition)
        If Not FunctionList.ContainsKey(func.Name) Then
            FunctionList.Add(func.Name, func)
        End If
    End Sub

    Public Function GetFunction(name As String) As FunctionDefinition
        Return If(FunctionList.ContainsKey(name), FunctionList(name), Nothing)
    End Function

    Public Function ContainsFunction(name As String) As Boolean
        Return FunctionList.ContainsKey(name)
    End Function

End Class
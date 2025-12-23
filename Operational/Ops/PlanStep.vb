' ###  PlanStep.vb - v1.0.1 ### 

' ##########################################################
'  Shelly - v1.0.1
'  License: Creative Commons Attribution-NonCommercial (CC BY-NC)
'  https://creativecommons.org/licenses/by-nc/4.0/
'  © 2025 Vlad Stefanescu | GreenCoders.net. Attribution required.
' ##########################################################

Imports Newtonsoft.Json

Public Class PlanStep

    <JsonProperty("step")>
    Public Property StepIndex As Integer

    <JsonProperty("tool")>
    Public Property Tool As String

    <JsonProperty("args")>
    Public Property Args As Dictionary(Of String, Object)

    ' Aliases for AI models that use name/arguments schema
    <JsonProperty("name")>
    Private Property ToolAlias As String
        Get
            Return Tool
        End Get
        Set(value As String)
            If String.IsNullOrEmpty(Tool) Then Tool = value
        End Set
    End Property

    <JsonProperty("arguments")>
    Private Property ArgsAlias As Dictionary(Of String, Object)
        Get
            Return Args
        End Get
        Set(value As Dictionary(Of String, Object))
            If Args Is Nothing OrElse Args.Count = 0 Then Args = value
        End Set
    End Property

End Class

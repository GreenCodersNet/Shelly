' ###  PlanStep.vb - v1.0.1 ### 

' ##########################################################
'  Shelly App - v1.0.1
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
End Class

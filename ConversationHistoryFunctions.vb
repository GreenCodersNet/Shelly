' ###  DEBUG ###

' ##########################################################
'  Shelly - v1.0.1
'  License: Creative Commons Attribution-NonCommercial (CC BY-NC)
'  https://creativecommons.org/licenses/by-nc/4.0/
'  © 2025 Vlad Stefanescu | GreenCoders.net. Attribution required.
' ##########################################################

Imports System.Net.Http
Imports System.Text
Imports System.Text.RegularExpressions
Imports System.Threading
Imports Newtonsoft.Json.Linq

Module ConversationHistoryFunctions


    Public Function CreateHistoryMessage(role As String, content As String) As Dictionary(Of String, String)
        Return New Dictionary(Of String, String) From {
            {"role", role},
            {"content", content}
        }
    End Function

End Module

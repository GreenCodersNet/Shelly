' ###  Config.vb - v1.0.1 ### 

' ##########################################################
'  Shelly - v1.0.1
'  License: Creative Commons Attribution-NonCommercial (CC BY-NC)
'  https://creativecommons.org/licenses/by-nc/4.0/
'  © 2025 Vlad Stefanescu | GreenCoders.net. Attribution required.
' ##########################################################

Imports System.Configuration

Module Config

    Public ReadOnly Property AiModel As String
        Get
            Return My.Settings.AiModelSelection
        End Get
    End Property

    Public ReadOnly Property OpenAiApiKey As String
        Get
            ' Read from secure store (set via Globals.UserApiKey)
            Dim key = Globals.UserApiKey
            If Not String.IsNullOrEmpty(key) Then
                Return key
            End If
            ' (Optional fallback if you ever populate My.Settings directly)
            Return My.Settings.UserApiKey
        End Get
    End Property

    Public ReadOnly Property AssistantId As String
        Get
            ' Read via Globals (which wraps My.Settings.assistantId)
            Return Globals.AssistantId
        End Get
    End Property

    Public ReadOnly Property DefaultRootFolder As String
        Get
            Return ConfigurationManager.AppSettings("DefaultRootFolder")
        End Get
    End Property

    Public ReadOnly Property PowerShellTimeoutSeconds As Integer
        Get
            Dim s = ConfigurationManager.AppSettings("PowerShellTimeoutSeconds")
            Dim parsed As Integer
            If Integer.TryParse(s, parsed) Then
                Return parsed
            End If
            Return 60
        End Get
    End Property
End Module

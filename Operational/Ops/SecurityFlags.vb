' ###  SecurityFlags.vb | v1.0.2 ### 

' ##########################################################
'  Shelly - v1.0.1
'  License: Creative Commons Attribution-NonCommercial (CC BY-NC)
'  https://creativecommons.org/licenses/by-nc/4.0/
'  © 2025 Vlad Stefanescu | GreenCoders.net. Attribution required.
' ##########################################################

Public Module SecurityFlags
    ' Enforced flags (always ON)
    Public ReadOnly Property BlockRegistryEdits As Boolean = True
    Public ReadOnly Property EnforceNonAdminExecution As Boolean = True

    ' Backing fields
    Private _constrainedLanguageMode As Boolean = True
    Private _blockSystemC As Boolean = True
    Private _blockNetworkCalls As Boolean = True
    Private _blockEnvVariableAccess As Boolean = False
    Private _blockBackgroundJobs As Boolean = True


    ' User-configurable flags (with backing properties)
    Public Property ConstrainedLanguageMode As Boolean
        Get
            Return _constrainedLanguageMode
        End Get
        Set(value As Boolean)
            _constrainedLanguageMode = value
        End Set
    End Property

    Public Property BlockSystemC As Boolean
        Get
            Return _blockSystemC
        End Get
        Set(value As Boolean)
            _blockSystemC = value
        End Set
    End Property

    Public Property BlockNetworkCalls As Boolean
        Get
            Return _blockNetworkCalls
        End Get
        Set(value As Boolean)
            _blockNetworkCalls = value
        End Set
    End Property

    Public Property BlockEnvVariableAccess As Boolean
        Get
            Return _blockEnvVariableAccess
        End Get
        Set(value As Boolean)
            _blockEnvVariableAccess = value
        End Set
    End Property

    Public Property BlockBackgroundJobs As Boolean
        Get
            Return _blockBackgroundJobs
        End Get
        Set(value As Boolean)
            _blockBackgroundJobs = value
        End Set
    End Property



End Module

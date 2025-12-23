' ###  ShellyInitializer.vb - v2.0.0 ### 

' ##########################################################
'  Shelly - v2.0.0
'  License: Creative Commons Attribution-NonCommercial (CC BY-NC)
'  https://creativecommons.org/licenses/by-nc/4.0/
'  © 2025 Vlad Stefanescu | GreenCoders.net. Attribution required.
' ##########################################################

''' <summary>
''' Centralized initialization for Shelly v2.0 upgrade systems
''' </summary>
Public Module ShellyInitializer
    
    Private _isInitialized As Boolean = False
    
    ''' <summary>
    ''' Initialize all v2.0 systems (call once at application startup)
    ''' </summary>
    Public Sub Initialize()
        If _isInitialized Then
            Debug.WriteLine("[ShellyInitializer] Already initialized, skipping")
            Return
        End If
        
        Try
            Debug.WriteLine("[ShellyInitializer] Starting initialization...")
            
            ' 1. Initialize tool schema registry
            ToolSchemaRegistry.Initialize()
            Debug.WriteLine("[ShellyInitializer] ? Tool schemas loaded")
            
            ' 2. Clear any stale outcomes from previous session
            GlobalOutcomeTracker.Instance.Clear()
            Debug.WriteLine("[ShellyInitializer] ? Outcome tracker cleared")
            
            ' ?? 3. Initialize step output manager
            StepOutputManager.Instance.ClearAll()
            Debug.WriteLine("[ShellyInitializer] ? Step output manager initialized")
            
            ' 4. Load security settings
            Globals.LoadPowerShellSecuritySettings()
            Debug.WriteLine("[ShellyInitializer] ? Security settings loaded")
            
            ' 5. Validate critical paths exist
            EnsureCriticalPathsExist()
            Debug.WriteLine("[ShellyInitializer] ? Critical paths verified")
            
            _isInitialized = True
            Debug.WriteLine("[ShellyInitializer] ? Initialization complete")
            
        Catch ex As Exception
            Debug.WriteLine($"[ShellyInitializer] ? Initialization failed: {ex.Message}")
            Throw New Exception("Failed to initialize Shelly v2.0 systems", ex)
        End Try
    End Sub
    
    ''' <summary>
    ''' Ensure required directories exist
    ''' </summary>
    Private Sub EnsureCriticalPathsExist()
        Dim requiredPaths As String() = {
            "C:\Shelly",
            "C:\Shelly\Logs",
            "C:\Shelly\Temp",
            "C:\Shelly\Scripts"
        }
        
        For Each path In requiredPaths
            Try
                If Not IO.Directory.Exists(path) Then
                    IO.Directory.CreateDirectory(path)
                    Debug.WriteLine($"[ShellyInitializer] Created directory: {path}")
                End If
            Catch ex As Exception
                Debug.WriteLine($"[ShellyInitializer] Warning: Could not create {path}: {ex.Message}")
            End Try
        Next
    End Sub
    
    ''' <summary>
    ''' Get initialization status
    ''' </summary>
    Public Function IsInitialized() As Boolean
        Return _isInitialized
    End Function
    
    ''' <summary>
    ''' Validate all systems are functioning
    ''' </summary>
    Public Function RunDiagnostics() As String
        Dim report As New Text.StringBuilder()
        report.AppendLine("=== Shelly v2.0 Diagnostics ===")
        report.AppendLine()
        
        ' Check schema registry
        Dim toolCount = ToolSchemaRegistry.GetAllToolNames().Count
        report.AppendLine($"? Tool Schemas: {toolCount} tools registered")
        
        ' Check outcome tracker
        Dim outcomeCount = GlobalOutcomeTracker.Instance.GetAllOutcomes().Count
        report.AppendLine($"? Outcome Tracker: {outcomeCount} outcomes recorded")
        
        ' ?? Check step output manager
        Dim outputStats = StepOutputManager.Instance.GetStatistics()
        report.AppendLine($"? Step Output Manager: {outputStats}")
        
        ' Check security settings
        report.AppendLine($"? Security Settings:")
        report.AppendLine($"  - Constrained Language: {SecurityFlags.ConstrainedLanguageMode}")
        report.AppendLine($"  - Block System C: {SecurityFlags.BlockSystemC}")
        report.AppendLine($"  - Block Network: {SecurityFlags.BlockNetworkCalls}")
        report.AppendLine($"  - Block Env Variables: {SecurityFlags.BlockEnvVariableAccess}")
        report.AppendLine($"  - Block Background Jobs: {SecurityFlags.BlockBackgroundJobs}")
        
        ' Check paths
        report.AppendLine($"? Critical Paths:")
        report.AppendLine($"  - C:\Shelly exists: {IO.Directory.Exists("C:\Shelly")}")
        report.AppendLine($"  - C:\Shelly\Logs exists: {IO.Directory.Exists("C:\Shelly\Logs")}")
        
        ' Check global state
        report.AppendLine($"? Global State:")
        report.AppendLine($"  - Conversation history: {Globals.conversationHistory.Count} messages")
        report.AppendLine($"  - File cache: {Globals.FileContents.Count} files")
        report.AppendLine($"  - AI calls made: {Globals.AICallCount}")
        report.AppendLine($"  - Current iteration: {Globals.CurrentIteration}")
        report.AppendLine($"  - Current request ID: {If(String.IsNullOrEmpty(Globals.CurrentRequestId), "None", Globals.CurrentRequestId.Substring(0, 8) & "...")}")
        
        report.AppendLine()
        report.AppendLine($"Status: {If(_isInitialized, "? Fully Initialized", "? Not Initialized")}")
        
        Return report.ToString()
    End Function
    
    ''' <summary>
    ''' Reset all systems (useful for testing)
    ''' </summary>
    Public Sub Reset()
        Try
            Debug.WriteLine("[ShellyInitializer] Resetting all systems...")
            
            GlobalOutcomeTracker.Instance.Clear()
            StepOutputManager.Instance.ClearAll()
            Globals.conversationHistory.Clear()
            Globals.FileContents.Clear()
            Globals.GeneratedImages.Clear()
            Globals.TaskData.Clear()
            Globals.ResetAICallCount()
            Globals.CurrentRequestId = ""
            Globals.CurrentIteration = 0
            
            _isInitialized = False
            Debug.WriteLine("[ShellyInitializer] ? Reset complete")
            
        Catch ex As Exception
            Debug.WriteLine($"[ShellyInitializer] Error during reset: {ex.Message}")
        End Try
    End Sub
    
End Module

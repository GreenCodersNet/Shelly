' ###  ToolPlanner.vb - v1.0.1 ### 

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

Imports System.IO
Imports Newtonsoft.Json

Public Class ToolDefinition
    <JsonProperty("name")>
    Public Property Name As String

    <JsonProperty("description")>
    Public Property Description As String

    <JsonProperty("parameters")>
    Public Property Parameters As List(Of String)
End Class

Public Class ToolCapability
    <JsonProperty("name")>
    Public Property Name As String

    <JsonProperty("description")>
    Public Property Description As String

    <JsonProperty("parameters")>
    Public Property Parameters As List(Of String)

    <JsonProperty("capabilities")>
    Public Property Capabilities As List(Of String)

    <JsonProperty("riskLevel")>
    Public Property RiskLevel As String

    <JsonProperty("prerequisites")>
    Public Property Prerequisites As List(Of String)

    <JsonProperty("outputType")>
    Public Property OutputType As String
End Class

Public Module ToolPlanner

    Private _toolCapabilities As List(Of ToolCapability) = Nothing

    Public ReadOnly Property ToolCapabilities As List(Of ToolCapability)
        Get
            If _toolCapabilities Is Nothing Then
                LoadToolCapabilities()
            End If
            Return _toolCapabilities
        End Get
    End Property

    Private Sub LoadToolCapabilities()
        Try
            Dim jsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "ToolCapabilities.json")
            If File.Exists(jsonPath) Then
                Dim json = File.ReadAllText(jsonPath)
                _toolCapabilities = JsonConvert.DeserializeObject(Of List(Of ToolCapability))(json)
                Debug.WriteLine($"[ToolPlanner] Loaded {_toolCapabilities.Count} tool capabilities from file")
            Else
                Debug.WriteLine($"[ToolPlanner] ToolCapabilities.json not found at {jsonPath}, using embedded resource")
                LoadFromEmbeddedResource()
            End If
        Catch ex As Exception
            Debug.WriteLine($"[ToolPlanner] Error loading ToolCapabilities.json: {ex.Message}")
            LoadFromEmbeddedResource()
        End Try
    End Sub

    Private Sub LoadFromEmbeddedResource()
        Try
            Dim asm = Reflection.Assembly.GetExecutingAssembly()
            Using stream = asm.GetManifestResourceStream("ShellyAI.Resources.ToolCapabilities.json")
                If stream IsNot Nothing Then
                    Using reader As New StreamReader(stream)
                        Dim json = reader.ReadToEnd()
                        _toolCapabilities = JsonConvert.DeserializeObject(Of List(Of ToolCapability))(json)
                        Debug.WriteLine($"[ToolPlanner] Loaded {_toolCapabilities.Count} tool capabilities from embedded resource")
                    End Using
                Else
                    _toolCapabilities = New List(Of ToolCapability)()
                    Debug.WriteLine("[ToolPlanner] No embedded ToolCapabilities resource found")
                End If
            End Using
        Catch ex As Exception
            _toolCapabilities = New List(Of ToolCapability)()
            Debug.WriteLine($"[ToolPlanner] Error loading embedded resource: {ex.Message}")
        End Try
    End Sub

    Public Function GetToolByName(name As String) As ToolCapability
        Return ToolCapabilities.FirstOrDefault(Function(t) t.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
    End Function

    Public Function GetToolsByCapability(capability As String) As List(Of ToolCapability)
        Return ToolCapabilities.Where(Function(t) t.Capabilities IsNot Nothing AndAlso t.Capabilities.Contains(capability, StringComparer.OrdinalIgnoreCase)).ToList()
    End Function

    Public Function GetHighRiskTools() As List(Of ToolCapability)
        Return ToolCapabilities.Where(Function(t) t.RiskLevel IsNot Nothing AndAlso t.RiskLevel.Equals("high", StringComparison.OrdinalIgnoreCase)).ToList()
    End Function

    Public Function GetAvailableToolsAsJson() As String
        Dim tools As New List(Of ToolDefinition) From {
            New ToolDefinition With {
                .Name = "FreeResponse",
                .Description = "Use when no external tool is needed—answer directly in natural language.",
                .Parameters = New List(Of String)()
            },
            New ToolDefinition With {
                .Name = "ReadFileAndAnswer",
                .Description = "Reads one or more files and answers a question about their content.",
                .Parameters = New List(Of String) From {"filePaths:String", "query:String"}
            },
            New ToolDefinition With {
                .Name = "WebSearchAndRespondBasedOnPageContent",
                .Description = "GENERAL web search only - use when user wants to search the entire internet without mentioning any specific website. " &
                               "Example: 'what is the weather today', 'latest news about AI'. " &
                               "DO NOT USE if user mentions ANY specific site like emag.ro, amazon.com, etc.",
                .Parameters = New List(Of String) From {"promptQuery:String", "siteName:String", "question:String"}
            },
            New ToolDefinition With {
                .Name = "ReadWebPageAndRespondBasedOnPageContent",
                .Description = "USE THIS for ANY request mentioning a specific website or domain. " &
                               "Builds Google site: search URL and scrapes results. " &
                               "REQUIRED FORMAT: url='https://www.google.com/search?q=site:[DOMAIN]+[KEYWORDS]' " &
                               "Examples: " &
                               "'search on emag.ro for HP laptops' → url='https://www.google.com/search?q=site:emag.ro+HP+laptops' " &
                               "'find iphones on amazon.com' → url='https://www.google.com/search?q=site:amazon.com+iphones' " &
                               "ONLY skip Google if user provides full https:// URL directly.",
                .Parameters = New List(Of String) From {"url:String", "query:String"}
            },
            New ToolDefinition With {
                .Name = "GenerateImages",
                .Description = "Generates images from a text prompt and saves them to a folder.",
                .Parameters = New List(Of String) From {"imagePrompt:String", "numImages:Int32", "style:String", "folderPath:String"}
            },
            New ToolDefinition With {
                .Name = "ImageAnswer",
                .Description = "Analyzes one or more images to answer a user's question about their visual content.",
                .Parameters = New List(Of String) From {"imagePaths:String", "query:String"}
            },
            New ToolDefinition With {
                .Name = "CheckMyScreenAndAnswer",
                .Description = "Takes a screenshot of the primary monitor and analyzes it to answer a query.",
                .Parameters = New List(Of String) From {"query:String"}
            },
            New ToolDefinition With {
              .Name = "GenerateLargeFileWithTextOrCode",
              .Description = "Generate large files in multiple chunks.",
              .Parameters = New List(Of String) From {"topic:String", "outputPath:String", "totalChunks:Int32"}
            },
             New ToolDefinition With {
                 .Name = "UpdateFileByChunks",
                 .Description = "Reads a file in chunks, summarizes each, then applies an update instruction and rewrites it.",
                .Parameters = New List(Of String) From {
                    "filePath:String",
                    "updateInstruction:String"
                }
            },
            New ToolDefinition With {
                .Name = "WriteInsideFileOrWindow",
                .Description = "Inserts generated text directly into the active window at the caret position.",
                .Parameters = New List(Of String) From {"topic:String", "totalChunks:Int32"}
            },
            New ToolDefinition With {
                .Name = "ExecutePowerShellScript",
                .Description = "Execute a PowerShell script server‑side and return its output or error.",
                .Parameters = New List(Of String) From {"script:String"}
            },
            New ToolDefinition With {
                .Name = "StartOrRunApplicationByName",
                .Description = "Starts an installed application by name (searches Start Menu shortcuts). DO NOT use for openeing files or folders paths. Use a PowerShell instead!",
                .Parameters = New List(Of String) From {"appName:String"}
            },
            New ToolDefinition With {
                .Name = "ChangeOrSetVolume",
                .Description = "Sets the system master volume to a specified percentage.",
                .Parameters = New List(Of String) From {"volumePercentage:Int32"}
            },
            New ToolDefinition With {
                .Name = "SendMediaKey",
                .Description = "Sends a media or system key event. Valid keys: VK_MEDIA_PLAY_PAUSE, VK_MEDIA_STOP, VK_MEDIA_NEXT_TRACK, VK_MEDIA_PREV_TRACK, VK_VOLUME_MUTE, VK_VOLUME_UP, VK_VOLUME_DOWN, VK_SNAPSHOT (PrintScreen), VK_TAB, VK_ENTER, VK_ESCAPE, VK_SPACE, VK_BACK, VK_DELETE, VK_HOME, VK_END, VK_UP, VK_DOWN, VK_LEFT, VK_RIGHT.",
                .Parameters = New List(Of String) From {"keyName:String"}
            },
            New ToolDefinition With {
                .Name = "TakePrintScreenOrScreenShot",
                .Description = "Captures a screenshot of the primary monitor and saves it to a folder. If no path is provided, it saves to C:\\Shelly.",
                .Parameters = New List(Of String) From {"outputPath:String"}
            },
            New ToolDefinition With {
                .Name = "ReadCopilotConversation",
                .Description = "Reads the Copilot chat window content and answers a question about it.",
                .Parameters = New List(Of String) From {"query:String"}
            },
             New ToolDefinition With {
                .Name = "GenerateBatchAndPs1File",
                .Description = "Creates a .bat launcher and matching .ps1 script to perform the requested task. The PowerShell script is wrapped in Try/Catch and always pauses at the end so the console stays open.",
                .Parameters = New List(Of String) From {
                "outputFolder:String",
                "userQuery:String"
                }
            },
            New ToolDefinition With {
                .Name = "SearchForTextInsideFiles",
                .Description = "Searches files in folders (recursively) for text content. Returns ONLY the file paths of matching files - NOT the file content. " &
                               "Supports PDF, DOCX, XLSX, PPTX, TXT, and other text files. " &
                               "Use this to FIND files, then use ReadFileAndAnswer to READ their content if needed.",
                .Parameters = New List(Of String) From {"paths:String", "searchText:String"}
            },
            New ToolDefinition With {
                .Name = "CopyFileOrFolder",
                .Description = "Copies a file or folder to a destination path. Set overwrite=True to replace existing.",
                .Parameters = New List(Of String) From {"sourcePath:String", "destPath:String", "overwrite:Boolean"}
            },
            New ToolDefinition With {
                .Name = "MoveFileOrFolder",
                .Description = "Moves a file or folder to a new location.",
                .Parameters = New List(Of String) From {"sourcePath:String", "destPath:String"}
            },
            New ToolDefinition With {
                .Name = "DeleteFileOrFolder",
                .Description = "Deletes a file or folder. Set recursive=True to delete folder contents.",
                .Parameters = New List(Of String) From {"targetPath:String", "recursive:Boolean"}
            },
            New ToolDefinition With {
                .Name = "ControlWindowsService",
                .Description = "Controls a Windows service: start, stop, restart, or get status.",
                .Parameters = New List(Of String) From {"serviceName:String", "action:String"}
            },
            New ToolDefinition With {
                .Name = "ManageScheduledTask",
                .Description = "Manages scheduled tasks: create, delete, enable, disable, status, or list.",
                .Parameters = New List(Of String) From {"taskName:String", "action:String", "triggerTime:String", "scriptPath:String"}
            },
            New ToolDefinition With {
                .Name = "NetworkDiagnostics",
                .Description = "Runs network diagnostics: ping, dns, ipconfig, or ports.",
                .Parameters = New List(Of String) From {"diagType:String", "target:String"}
            }
        }

        Return JsonConvert.SerializeObject(tools)
    End Function

End Module

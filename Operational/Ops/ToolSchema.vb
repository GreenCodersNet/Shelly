' ###  ToolSchema.vb - v2.0.0 ### 

' ##########################################################
'  Shelly - v2.0.0
'  License: Creative Commons Attribution-NonCommercial (CC BY-NC)
'  https://creativecommons.org/licenses/by-nc/4.0/
'  © 2025 Vlad Stefanescu | GreenCoders.net. Attribution required.
' ##########################################################

Imports System.Collections.Generic

''' <summary>
''' Defines the schema for a tool parameter, including type and validation rules
''' </summary>
Public Class ToolParameterSchema
    Public Property Name As String
    Public Property Type As Type
    Public Property IsRequired As Boolean
    Public Property AllowedValues As List(Of String)
    Public Property MinValue As Object
    Public Property MaxValue As Object
    Public Property PathMustExist As Boolean = False
    Public Property MustBeValidPath As Boolean = False
    
    Public Sub New(name As String, type As Type, isRequired As Boolean)
        Me.Name = name
        Me.Type = type
        Me.IsRequired = isRequired
        Me.AllowedValues = New List(Of String)
    End Sub
End Class

''' <summary>
''' Defines the complete schema for a tool, including parameters and security requirements
''' </summary>
Public Class ToolSchema
    Public Property ToolName As String
    Public Property Description As String
    Public Property Parameters As List(Of ToolParameterSchema)
    Public Property RequiresApproval As Boolean
    Public Property RequiresElevation As Boolean
    Public Property RequiresAuth As Boolean
    Public Property AuthScope As String
    Public Property IsDestructive As Boolean
    Public Property Category As ToolCategory
    
    Public Sub New()
        Parameters = New List(Of ToolParameterSchema)
    End Sub
End Class

''' <summary>
''' Tool categories for organization and risk assessment
''' </summary>
Public Enum ToolCategory
    Information      ' Read-only operations
    FileSystem       ' File/folder operations
    System           ' PowerShell, services, tasks
    Network          ' Web, email, API calls
    Multimedia       ' Images, audio, video
    UserInterface    ' Typing, screenshots
    Automation       ' Complex multi-step operations
End Enum

''' <summary>
''' Central registry of all tool schemas
''' </summary>
Public Module ToolSchemaRegistry
    
    Private _schemas As Dictionary(Of String, ToolSchema)
    
    ''' <summary>
    ''' Initialize all tool schemas
    ''' </summary>
    Public Sub Initialize()
        _schemas = New Dictionary(Of String, ToolSchema)(StringComparer.OrdinalIgnoreCase)
        
        ' ???????????????????????????????????????????????????????????????
        ' INFORMATION TOOLS
        ' ???????????????????????????????????????????????????????????????
        
        _schemas("FreeResponse") = New ToolSchema With {
            .ToolName = "FreeResponse",
            .Description = "Answer user with natural language, no external tools needed",
            .RequiresApproval = False,
            .IsDestructive = False,
            .Category = ToolCategory.Information,
            .Parameters = New List(Of ToolParameterSchema) From {
                New ToolParameterSchema("text", GetType(String), True)
            }
        }
        
        _schemas("ReadFileAndAnswer") = New ToolSchema With {
            .ToolName = "ReadFileAndAnswer",
            .Description = "Read file(s) and answer questions about content",
            .RequiresApproval = False,
            .IsDestructive = False,
            .Category = ToolCategory.FileSystem,
            .Parameters = New List(Of ToolParameterSchema) From {
                New ToolParameterSchema("filePaths", GetType(String), True) With {.MustBeValidPath = True},
                New ToolParameterSchema("query", GetType(String), True)
            }
        }
        
        _schemas("CheckMyScreenAndAnswer") = New ToolSchema With {
            .ToolName = "CheckMyScreenAndAnswer",
            .Description = "Take screenshot and analyze content",
            .RequiresApproval = False,
            .IsDestructive = False,
            .Category = ToolCategory.Multimedia,
            .Parameters = New List(Of ToolParameterSchema) From {
                New ToolParameterSchema("query", GetType(String), True)
            }
        }
        
        _schemas("ImageAnswer") = New ToolSchema With {
            .ToolName = "ImageAnswer",
            .Description = "Analyze image(s) and answer questions",
            .RequiresApproval = False,
            .IsDestructive = False,
            .Category = ToolCategory.Multimedia,
            .Parameters = New List(Of ToolParameterSchema) From {
                New ToolParameterSchema("imagePaths", GetType(String), True) With {.MustBeValidPath = True},
                New ToolParameterSchema("query", GetType(String), True)
            }
        }
        
        _schemas("SearchForTextInsideFiles") = New ToolSchema With {
            .ToolName = "SearchForTextInsideFiles",
            .Description = "Searches files in folders (recursively) for text content. Returns ONLY file paths of matching files - NOT content. Supports PDF, DOCX, XLSX, PPTX, TXT, and other text files.",
            .RequiresApproval = False,
            .IsDestructive = False,
            .Category = ToolCategory.FileSystem,
            .Parameters = New List(Of ToolParameterSchema) From {
                New ToolParameterSchema("paths", GetType(String), True) With {.MustBeValidPath = True},
                New ToolParameterSchema("searchText", GetType(String), True)
            }
        }
        
        ' ???????????????????????????????????????????????????????????????
        ' FILE SYSTEM TOOLS (LOW RISK)
        ' ???????????????????????????????????????????????????????????????
        
        _schemas("GenerateLargeFileWithTextOrCode") = New ToolSchema With {
            .ToolName = "GenerateLargeFileWithTextOrCode",
            .Description = "Generate large text/code files in chunks",
            .RequiresApproval = False,
            .IsDestructive = False,
            .Category = ToolCategory.FileSystem,
            .Parameters = New List(Of ToolParameterSchema) From {
                New ToolParameterSchema("topic", GetType(String), True),
                New ToolParameterSchema("outputPath", GetType(String), True) With {.MustBeValidPath = True},
                New ToolParameterSchema("totalChunks", GetType(Integer), False) With {.MinValue = 1, .MaxValue = 50}
            }
        }
        
        _schemas("WriteInsideFileOrWindow") = New ToolSchema With {
            .ToolName = "WriteInsideFileOrWindow",
            .Description = "Type generated text into active window",
            .RequiresApproval = False,
            .IsDestructive = False,
            .Category = ToolCategory.UserInterface,
            .Parameters = New List(Of ToolParameterSchema) From {
                New ToolParameterSchema("topic", GetType(String), True),
                New ToolParameterSchema("totalChunks", GetType(Integer), False) With {.MinValue = 1, .MaxValue = 20}
            }
        }
        
        _schemas("UpdateFileByChunks") = New ToolSchema With {
            .ToolName = "UpdateFileByChunks",
            .Description = "Update existing file content based on instructions",
            .RequiresApproval = True,
            .IsDestructive = False,
            .Category = ToolCategory.FileSystem,
            .Parameters = New List(Of ToolParameterSchema) From {
                New ToolParameterSchema("filePath", GetType(String), True) With {.PathMustExist = True, .MustBeValidPath = True},
                New ToolParameterSchema("updateInstruction", GetType(String), True)
            }
        }
        
        _schemas("GenerateBatchAndPs1File") = New ToolSchema With {
            .ToolName = "GenerateBatchAndPs1File",
            .Description = "Generate batch and PowerShell script files",
            .RequiresApproval = False,
            .IsDestructive = False,
            .Category = ToolCategory.FileSystem,
            .Parameters = New List(Of ToolParameterSchema) From {
                New ToolParameterSchema("outputFolder", GetType(String), True) With {.MustBeValidPath = True},
                New ToolParameterSchema("userQuery", GetType(String), True)
            }
        }
        
        ' ???????????????????????????????????????????????????????????????
        ' FILE SYSTEM TOOLS (HIGH RISK - REQUIRE APPROVAL)
        ' ???????????????????????????????????????????????????????????????
        
        _schemas("CopyFileOrFolder") = New ToolSchema With {
            .ToolName = "CopyFileOrFolder",
            .Description = "Copy file or folder to destination",
            .RequiresApproval = True,
            .IsDestructive = False,
            .Category = ToolCategory.FileSystem,
            .Parameters = New List(Of ToolParameterSchema) From {
                New ToolParameterSchema("sourcePath", GetType(String), True) With {.PathMustExist = True, .MustBeValidPath = True},
                New ToolParameterSchema("destPath", GetType(String), True) With {.MustBeValidPath = True},
                New ToolParameterSchema("overwrite", GetType(Boolean), False)
            }
        }
        
        _schemas("MoveFileOrFolder") = New ToolSchema With {
            .ToolName = "MoveFileOrFolder",
            .Description = "Move file or folder to new location",
            .RequiresApproval = True,
            .IsDestructive = True,
            .Category = ToolCategory.FileSystem,
            .Parameters = New List(Of ToolParameterSchema) From {
                New ToolParameterSchema("sourcePath", GetType(String), True) With {.PathMustExist = True, .MustBeValidPath = True},
                New ToolParameterSchema("destPath", GetType(String), True) With {.MustBeValidPath = True}
            }
        }
        
        _schemas("DeleteFileOrFolder") = New ToolSchema With {
            .ToolName = "DeleteFileOrFolder",
            .Description = "Delete file or folder (DESTRUCTIVE)",
            .RequiresApproval = True,
            .IsDestructive = True,
            .Category = ToolCategory.FileSystem,
            .Parameters = New List(Of ToolParameterSchema) From {
                New ToolParameterSchema("targetPath", GetType(String), True) With {.PathMustExist = True, .MustBeValidPath = True},
                New ToolParameterSchema("recursive", GetType(Boolean), False)
            }
        }
        
        ' ???????????????????????????????????????????????????????????????
        ' SYSTEM TOOLS (HIGH RISK - REQUIRE APPROVAL)
        ' ???????????????????????????????????????????????????????????????
        
        _schemas("ExecutePowerShellScript") = New ToolSchema With {
            .ToolName = "ExecutePowerShellScript",
            .Description = "Execute PowerShell script with sandboxing",
            .RequiresApproval = True,
            .IsDestructive = True,
            .Category = ToolCategory.System,
            .Parameters = New List(Of ToolParameterSchema) From {
                New ToolParameterSchema("script", GetType(String), True)
            }
        }
        
        _schemas("StartOrRunApplicationByName") = New ToolSchema With {
            .ToolName = "StartOrRunApplicationByName",
            .Description = "Launch application by name from Start Menu",
            .RequiresApproval = False,
            .IsDestructive = False,
            .Category = ToolCategory.System,
            .Parameters = New List(Of ToolParameterSchema) From {
                New ToolParameterSchema("appName", GetType(String), True)
            }
        }
        
        _schemas("ControlWindowsService") = New ToolSchema With {
            .ToolName = "ControlWindowsService",
            .Description = "Control Windows services (start/stop/restart)",
            .RequiresApproval = True,
            .RequiresElevation = True,
            .IsDestructive = True,
            .Category = ToolCategory.System,
            .Parameters = New List(Of ToolParameterSchema) From {
                New ToolParameterSchema("serviceName", GetType(String), True),
                New ToolParameterSchema("action", GetType(String), True) With {
                    .AllowedValues = New List(Of String) From {"start", "stop", "restart", "status"}
                }
            }
        }
        
        _schemas("ManageScheduledTask") = New ToolSchema With {
            .ToolName = "ManageScheduledTask",
            .Description = "Manage Windows scheduled tasks",
            .RequiresApproval = True,
            .RequiresElevation = True,
            .IsDestructive = True,
            .Category = ToolCategory.System,
            .Parameters = New List(Of ToolParameterSchema) From {
                New ToolParameterSchema("taskName", GetType(String), True),
                New ToolParameterSchema("action", GetType(String), True) With {
                    .AllowedValues = New List(Of String) From {"create", "delete", "enable", "disable", "status", "list"}
                },
                New ToolParameterSchema("triggerTime", GetType(String), False),
                New ToolParameterSchema("scriptPath", GetType(String), False) With {.MustBeValidPath = True}
            }
        }
        
        ' ???????????????????????????????????????????????????????????????
        ' NETWORK TOOLS
        ' ???????????????????????????????????????????????????????????????
        
        _schemas("WebSearchAndRespondBasedOnPageContent") = New ToolSchema With {
            .ToolName = "WebSearchAndRespondBasedOnPageContent",
            .Description = "General Google web search - use ONLY when no specific website is mentioned. For site-specific searches like 'search on emag.ro' or 'find on amazon', use ReadWebPageAndRespondBasedOnPageContent instead.",
            .RequiresApproval = False,
            .IsDestructive = False,
            .Category = ToolCategory.Network,
            .Parameters = New List(Of ToolParameterSchema) From {
                New ToolParameterSchema("promptQuery", GetType(String), True),
                New ToolParameterSchema("siteName", GetType(String), False),
                New ToolParameterSchema("question", GetType(String), True)
            }
        }
        
        _schemas("ReadWebPageAndRespondBasedOnPageContent") = New ToolSchema With {
            .ToolName = "ReadWebPageAndRespondBasedOnPageContent",
            .Description = "Scrapes URL content. For 'search on [site]' use Google: https://www.google.com/search?q=site:[domain]+[keywords]. Only use direct URL if user provides https:// in prompt.",
            .RequiresApproval = False,
            .IsDestructive = False,
            .Category = ToolCategory.Network,
            .Parameters = New List(Of ToolParameterSchema) From {
                New ToolParameterSchema("url", GetType(String), True),
                New ToolParameterSchema("query", GetType(String), True)
            }
        }
        
        _schemas("NetworkDiagnostics") = New ToolSchema With {
            .ToolName = "NetworkDiagnostics",
            .Description = "Run network diagnostics (ping, DNS, ipconfig)",
            .RequiresApproval = False,
            .IsDestructive = False,
            .Category = ToolCategory.Network,
            .Parameters = New List(Of ToolParameterSchema) From {
                New ToolParameterSchema("diagType", GetType(String), True) With {
                    .AllowedValues = New List(Of String) From {"ping", "dns", "ipconfig", "ports"}
                },
                New ToolParameterSchema("target", GetType(String), False)
            }
        }
        
        ' ???????????????????????????????????????????????????????????????
        ' MULTIMEDIA TOOLS
        ' ???????????????????????????????????????????????????????????????
        
        _schemas("GenerateImages") = New ToolSchema With {
            .ToolName = "GenerateImages",
            .Description = "Generate images using DALL-E",
            .RequiresApproval = False,
            .IsDestructive = False,
            .Category = ToolCategory.Multimedia,
            .Parameters = New List(Of ToolParameterSchema) From {
                New ToolParameterSchema("imagePrompt", GetType(String), True),
                New ToolParameterSchema("numImages", GetType(Integer), True) With {.MinValue = 1, .MaxValue = 10},
                New ToolParameterSchema("style", GetType(String), True),
                New ToolParameterSchema("folderPath", GetType(String), True) With {.MustBeValidPath = True}
            }
        }
        
        _schemas("TakePrintScreenOrScreenShot") = New ToolSchema With {
            .ToolName = "TakePrintScreenOrScreenShot",
            .Description = "Captures a screenshot of the primary monitor and saves it. If no path is provided, it saves to C:\\Shelly.",
            .RequiresApproval = False,
            .IsDestructive = False,
            .Category = ToolCategory.Multimedia,
            .Parameters = New List(Of ToolParameterSchema) From {
                New ToolParameterSchema("OutputPath", GetType(String), False) With {.MustBeValidPath = False}
            }
        }
        
        _schemas("ChangeOrSetVolume") = New ToolSchema With {
            .ToolName = "ChangeOrSetVolume",
            .Description = "Set system volume percentage",
            .RequiresApproval = False,
            .IsDestructive = False,
            .Category = ToolCategory.Multimedia,
            .Parameters = New List(Of ToolParameterSchema) From {
                New ToolParameterSchema("volumePercentage", GetType(Integer), True) With {.MinValue = 0, .MaxValue = 100}
            }
        }
        
        _schemas("SendMediaKey") = New ToolSchema With {
            .ToolName = "SendMediaKey",
            .Description = "Send media control key (play, pause, etc.)",
            .RequiresApproval = False,
            .IsDestructive = False,
            .Category = ToolCategory.Multimedia,
            .Parameters = New List(Of ToolParameterSchema) From {
                New ToolParameterSchema("keyName", GetType(String), True)
            }
        }
        
        Debug.WriteLine($"[ToolSchemaRegistry] Initialized {_schemas.Count} tool schemas")
    End Sub
    
    ''' <summary>
    ''' Get schema for a specific tool
    ''' </summary>
    Public Function GetSchema(toolName As String) As ToolSchema
        If _schemas Is Nothing Then Initialize()
        
        Dim schema As ToolSchema = Nothing
        If _schemas.TryGetValue(toolName, schema) Then
            Return schema
        End If
        
        Return Nothing
    End Function
    
    ''' <summary>
    ''' Check if a tool exists in the registry
    ''' </summary>
    Public Function IsValidTool(toolName As String) As Boolean
        If _schemas Is Nothing Then Initialize()
        Return _schemas.ContainsKey(toolName)
    End Function
    
    ''' <summary>
    ''' Get all registered tool names
    ''' </summary>
    Public Function GetAllToolNames() As List(Of String)
        If _schemas Is Nothing Then Initialize()
        Return _schemas.Keys.ToList()
    End Function
    
    ''' <summary>
    ''' Get all schemas for a specific category
    ''' </summary>
    Public Function GetSchemasByCategory(category As ToolCategory) As List(Of ToolSchema)
        If _schemas Is Nothing Then Initialize()
        Return _schemas.Values.Where(Function(s) s.Category = category).ToList()
    End Function
    
    ''' <summary>
    ''' Get all destructive tools
    ''' </summary>
    Public Function GetDestructiveTools() As List(Of ToolSchema)
        If _schemas Is Nothing Then Initialize()
        Return _schemas.Values.Where(Function(s) s.IsDestructive).ToList()
    End Function
    
End Module

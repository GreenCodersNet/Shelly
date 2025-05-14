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

Imports Newtonsoft.Json

Public Class ToolDefinition
    <JsonProperty("name")>
    Public Property Name As String

    <JsonProperty("description")>
    Public Property Description As String

    <JsonProperty("parameters")>
    Public Property Parameters As List(Of String)
End Class

Public Module ToolPlanner

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
                .Description = "Searches the web using Google query, retrieves page content, and answers based on it. Use only when the User prompt suggests there is a need for real time information. Otherwise, use you own reasoning data.",
                .Parameters = New List(Of String) From {"promptQuery:String", "siteName:String", "query:String"}
            },
             New ToolDefinition With {
                .Name = "ReadWebPageAndRespondBasedOnPageContent",
                .Description = "Loads a specific URL, extracts visible text, and answers a query about that page.",
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
                .Description = "Sends a media or system key event (play, pause, volume up/down, etc.).",
                .Parameters = New List(Of String) From {"keyName:String"}
            },
            New ToolDefinition With {
                .Name = "TakePrintScreenOrScreenShot",
                .Description = "Captures a screenshot of the primary monitor and saves it to a folder.",
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
                .Description = "Searches one or more files or folders for a case‑insensitive text match.",
                .Parameters = New List(Of String) From {"paths:String", "searchWord:String"}
            }
        }

        Return JsonConvert.SerializeObject(tools)
    End Function

End Module

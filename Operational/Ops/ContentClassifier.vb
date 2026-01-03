' ###  ContentClassifier.vb - v1.0.0 ###

' ##########################################################
'  Shelly - v1.0.0
'  License: Creative Commons Attribution-NonCommercial (CC BY-NC)
'  https://creativecommons.org/licenses/by-nc/4.0/
'  © 2025 Vlad Stefanescu | GreenCoders.net. Attribution required.
' ##########################################################

Imports System.Text.RegularExpressions
Imports System.Linq

''' <summary>
''' Automatically classifies step execution outputs into content types
''' and generates lightweight summaries for token-efficient final AI calls
''' </summary>
Public Module ContentClassifier
    
    ''' <summary>
    ''' Classifies step output and creates a summary suitable for final rephrasing
    ''' </summary>
    Public Function ClassifyStepOutput(
        toolName As String,
        output As String,
        success As Boolean,
        Optional args As Dictionary(Of String, Object) = Nothing
    ) As StepExecutionSummary
        
        Dim summary As New StepExecutionSummary With {
            .ToolName = toolName,
            .Status = If(success, OutcomeStatus.Success, OutcomeStatus.Failed),
            .FullOutput = If(output, String.Empty),
            .Arguments = If(args, New Dictionary(Of String, Object))
        }
        
        ' Handle failures
        If Not success Then
            summary.ContentType = ContentType.ErrorMessage
            summary.ShortDescription = If(output.Length > 150, output.Substring(0, 147) & "...", output)
            summary.IsLargeContent = False
            Return summary
        End If
        
        ' Classify based on tool type
        Select Case toolName.ToLowerInvariant()
            
            ' === CONVERSATIONAL TEXT ===
            Case "freeresponse"
                ClassifyFreeResponse(summary, output)
                
            ' === IMAGE OPERATIONS ===
            Case "generateimages"
                ClassifyGenerateImages(summary, output, args)
                
            Case "imageanswer", "checkmyscreenandanswer"
                ClassifyImageAnalysis(summary, output)
                
            Case "takeprintscreenorscreenshot"
                ClassifyScreenshot(summary, output, args)
                
            ' === FILE OPERATIONS ===
            Case "readfileandanswer"
                ClassifyReadFile(summary, output, args)
                
            Case "searchfortextinsidefiles"
                ClassifyFileSearch(summary, output)
                
            Case "generatelargefile", "generatelargefile withtext orcode"
                ClassifyGenerateLargeFile(summary, output, args)
                
            Case "updatefilebyc hunks"
                ClassifyUpdateFile(summary, output, args)
                
            Case "copyfileorfolder"
                ClassifyCopyFile(summary, output, args)
                
            Case "movefileorfolder"
                ClassifyMoveFile(summary, output, args)
                
            Case "deletefileorfolder"
                ClassifyDeleteFile(summary, output, args)
                
            ' === WEB OPERATIONS ===
            Case "websearchandrespondbasedonpagecontent"
                ClassifyWebSearch(summary, output)
                
            Case "readwebpageandrespondbasedonpagecontent"
                ClassifyReadWebPage(summary, output)
                
            ' === SYSTEM OPERATIONS ===
            Case "startorrunapplicationbyname", "openpath"
                ClassifyLaunchApp(summary, output, args)
                
            Case "changeorsetvolume"
                ClassifyVolumeChange(summary, output, args)
                
            Case "sendmediakey"
                ClassifyMediaKey(summary, output, args)
                
            Case "writeinsidefileorwindow"
                ClassifyWriteText(summary, output)
                
            Case "generatebatchandps1file"
                ClassifyGenerateBatch(summary, output, args)
                
            ' === POWERSHELL ===
            Case "executepowershellscript"
                ClassifyPowerShell(summary, output, args)
                
            ' === DEFAULT ===
            Case Else
                ClassifyGeneric(summary, output)
                
        End Select
        
        Return summary
    End Function
    
    ' ========== CLASSIFICATION HELPERS ==========
    
    Private Sub ClassifyFreeResponse(summary As StepExecutionSummary, output As String)
        summary.ContentType = ContentType.ConversationalText
        summary.IsLargeContent = output.Length > 500
        
        If summary.IsLargeContent Then
            summary.ShortDescription = output.Substring(0, Math.Min(150, output.Length))
            If output.Length > 150 Then summary.ShortDescription &= "..."
        Else
            summary.ShortDescription = output
        End If
    End Sub
    
    Private Sub ClassifyGenerateImages(summary As StepExecutionSummary, output As String, args As Dictionary(Of String, Object))
        summary.ContentType = ContentType.FilePath
        
        ' Count image paths in output
        Dim imageCount = CountLines(output)
        summary.IsLargeContent = False ' Paths are never large
        
        summary.ShortDescription = $"Generated {imageCount} image{If(imageCount = 1, "", "s")}"
        summary.KeyDetails("count") = imageCount.ToString()
        summary.KeyDetails("paths") = output.Trim()
        
        ' Extract folder path if available
        If args IsNot Nothing AndAlso args.ContainsKey("folderPath") Then
            summary.KeyDetails("folder") = args("folderPath").ToString()
        End If
    End Sub
    
    Private Sub ClassifyImageAnalysis(summary As StepExecutionSummary, output As String)
        summary.ContentType = ContentType.ConversationalText
        summary.IsLargeContent = output.Length > 500
        
        If summary.IsLargeContent Then
            summary.ShortDescription = "Image analysis completed (details shown above)"
        Else
            summary.ShortDescription = output
        End If
    End Sub
    
    Private Sub ClassifyScreenshot(summary As StepExecutionSummary, output As String, args As Dictionary(Of String, Object))
        summary.ContentType = ContentType.FilePath
        summary.IsLargeContent = False
        summary.ShortDescription = "Screenshot captured"
        summary.KeyDetails("path") = output.Trim()
    End Sub
    
    Private Sub ClassifyReadFile(summary As StepExecutionSummary, output As String, args As Dictionary(Of String, Object))
        summary.ContentType = ContentType.BulkText
        summary.IsLargeContent = output.Length > 1000
        
        summary.ShortDescription = "Read and analyzed file"
        summary.KeyDetails("outputLength") = output.Length.ToString()
        
        ' Extract file path if available
        If args IsNot Nothing AndAlso args.ContainsKey("filePaths") Then
            Dim paths = args("filePaths").ToString()
            summary.KeyDetails("filePath") = paths
        End If
    End Sub
    
    Private Sub ClassifyFileSearch(summary As StepExecutionSummary, output As String)
        summary.ContentType = ContentType.FileList
        
        ' Try to count files in output
        Dim fileCount = EstimateFileCount(output)
        summary.IsLargeContent = fileCount > 10
        
        summary.ShortDescription = $"Found {fileCount} matching file{If(fileCount = 1, "", "s")}"
        summary.KeyDetails("count") = fileCount.ToString()
        
        ' Extract first few files as preview
        If fileCount > 0 Then
            Dim lines = output.Split({vbCrLf, vbLf}, StringSplitOptions.RemoveEmptyEntries)
            Dim previewCount = Math.Min(5, fileCount)
            Dim preview = String.Join(", ", lines.Take(previewCount))
            If fileCount > 5 Then preview &= $" ... and {fileCount - 5} more"
            summary.KeyDetails("preview") = preview
        End If
    End Sub
    
    Private Sub ClassifyGenerateLargeFile(summary As StepExecutionSummary, output As String, args As Dictionary(Of String, Object))
        summary.ContentType = ContentType.FilePath
        summary.IsLargeContent = False
        summary.ShortDescription = "Created file"
        
        ' Extract file path from output or args
        If args IsNot Nothing AndAlso args.ContainsKey("filePath") Then
            summary.KeyDetails("path") = args("filePath").ToString()
        Else
            ' Try to extract path from output
            Dim pathMatch = Regex.Match(output, "[A-Za-z]:\\[\w\s\-\\\.]+")
            If pathMatch.Success Then
                summary.KeyDetails("path") = pathMatch.Value
            End If
        End If
    End Sub
    
    Private Sub ClassifyUpdateFile(summary As StepExecutionSummary, output As String, args As Dictionary(Of String, Object))
        summary.ContentType = ContentType.SystemInfo
        summary.IsLargeContent = False
        summary.ShortDescription = "Updated file"
        
        If args IsNot Nothing AndAlso args.ContainsKey("filePath") Then
            summary.KeyDetails("path") = args("filePath").ToString()
        End If
    End Sub
    
    Private Sub ClassifyCopyFile(summary As StepExecutionSummary, output As String, args As Dictionary(Of String, Object))
        summary.ContentType = ContentType.SystemInfo
        summary.IsLargeContent = False
        
        Dim dest = If(args?.ContainsKey("destPath"), args("destPath").ToString(), "destination")
        summary.ShortDescription = $"Copied to {dest}"
        summary.KeyDetails("destination") = dest
    End Sub
    
    Private Sub ClassifyMoveFile(summary As StepExecutionSummary, output As String, args As Dictionary(Of String, Object))
        summary.ContentType = ContentType.SystemInfo
        summary.IsLargeContent = False
        
        Dim dest = If(args?.ContainsKey("destPath"), args("destPath").ToString(), "destination")
        summary.ShortDescription = $"Moved to {dest}"
        summary.KeyDetails("destination") = dest
    End Sub
    
    Private Sub ClassifyDeleteFile(summary As StepExecutionSummary, output As String, args As Dictionary(Of String, Object))
        summary.ContentType = ContentType.SystemInfo
        summary.IsLargeContent = False
        
        Dim path = If(args?.ContainsKey("path"), args("path").ToString(), "file/folder")
        summary.ShortDescription = $"Deleted {path}"
        summary.KeyDetails("path") = path
    End Sub
    
    Private Sub ClassifyWebSearch(summary As StepExecutionSummary, output As String)
        ' Determine if output contains structured data (lists, tables)
        Dim hasStructuredData = output.Contains("1)") AndAlso output.Contains("2)") AndAlso output.Length > 500
        
        If hasStructuredData Then
            summary.ContentType = ContentType.DataTable
            summary.IsLargeContent = True
            summary.ShortDescription = "Retrieved web search results"
            
            ' Try to count items
            Dim itemCount = Regex.Matches(output, "^\d+\)", RegexOptions.Multiline).Count
            If itemCount > 0 Then
                summary.KeyDetails("itemCount") = itemCount.ToString()
            End If
        Else
            summary.ContentType = ContentType.ConversationalText
            summary.IsLargeContent = output.Length > 500
            summary.ShortDescription = If(output.Length > 150, output.Substring(0, 147) & "...", output)
        End If
    End Sub
    
    Private Sub ClassifyReadWebPage(summary As StepExecutionSummary, output As String)
        ' Similar to web search
        ClassifyWebSearch(summary, output)
    End Sub
    
    Private Sub ClassifyLaunchApp(summary As StepExecutionSummary, output As String, args As Dictionary(Of String, Object))
        summary.ContentType = ContentType.SystemInfo
        summary.IsLargeContent = False
        
        Dim appName = If(args?.ContainsKey("appName"), args("appName").ToString(), "application")
        If args?.ContainsKey("path") Then appName = args("path").ToString()
        
        summary.ShortDescription = $"Opened {appName}"
        summary.KeyDetails("app") = appName
    End Sub
    
    Private Sub ClassifyVolumeChange(summary As StepExecutionSummary, output As String, args As Dictionary(Of String, Object))
        summary.ContentType = ContentType.SystemInfo
        summary.IsLargeContent = False
        
        Dim volume = If(args?.ContainsKey("volumePercentage"), args("volumePercentage").ToString(), "?")
        summary.ShortDescription = $"Set volume to {volume}%"
        summary.KeyDetails("volume") = volume
    End Sub
    
    Private Sub ClassifyMediaKey(summary As StepExecutionSummary, output As String, args As Dictionary(Of String, Object))
        summary.ContentType = ContentType.SystemInfo
        summary.IsLargeContent = False
        
        Dim key = If(args?.ContainsKey("key"), args("key").ToString(), "media key")
        summary.ShortDescription = $"Sent {key} command"
        summary.KeyDetails("key") = key
    End Sub
    
    Private Sub ClassifyWriteText(summary As StepExecutionSummary, output As String)
        summary.ContentType = ContentType.SystemInfo
        summary.IsLargeContent = False
        summary.ShortDescription = "Inserted text into active window"
    End Sub
    
    Private Sub ClassifyGenerateBatch(summary As StepExecutionSummary, output As String, args As Dictionary(Of String, Object))
        summary.ContentType = ContentType.FilePath
        summary.IsLargeContent = False
        summary.ShortDescription = "Generated batch and PowerShell files"
        
        If output.Contains("\") Then
            summary.KeyDetails("paths") = output.Trim()
        End If
    End Sub
    
    Private Sub ClassifyPowerShell(summary As StepExecutionSummary, output As String, args As Dictionary(Of String, Object))
        ' Analyze PowerShell output to determine content type
        Dim script = If(args?.ContainsKey("script"), args("script").ToString(), "")
        
        ' Check if it's a file listing command
        If script.Contains("Get-ChildItem") OrElse script.Contains("dir") OrElse script.Contains("ls") Then
            summary.ContentType = ContentType.FileList
            Dim lineCount = CountLines(output)
            summary.IsLargeContent = lineCount > 10
            summary.ShortDescription = $"Found {lineCount} item{If(lineCount = 1, "", "s")}"
            summary.KeyDetails("count") = lineCount.ToString()
            
            If lineCount <= 10 Then
                summary.KeyDetails("preview") = output
            Else
                Dim lines = output.Split({vbCrLf, vbLf}, StringSplitOptions.RemoveEmptyEntries)
                summary.KeyDetails("preview") = String.Join(", ", lines.Take(5)) & $" ... and {lineCount - 5} more"
            End If
        Else
            ' General PowerShell command
            summary.ContentType = ContentType.SystemInfo
            summary.IsLargeContent = output.Length > 300
            
            If summary.IsLargeContent Then
                summary.ShortDescription = "Executed PowerShell command (results shown above)"
            Else
                summary.ShortDescription = output
            End If
        End If
    End Sub
    
    Private Sub ClassifyGeneric(summary As StepExecutionSummary, output As String)
        summary.ContentType = ContentType.ConversationalText
        summary.IsLargeContent = output.Length > 500
        
        If summary.IsLargeContent Then
            summary.ShortDescription = output.Substring(0, Math.Min(150, output.Length))
            If output.Length > 150 Then summary.ShortDescription &= "..."
        Else
            summary.ShortDescription = output
        End If
    End Sub
    
    ' ========== UTILITY FUNCTIONS ==========
    
    Private Function CountLines(text As String) As Integer
        If String.IsNullOrWhiteSpace(text) Then Return 0
        Return text.Split({vbCrLf, vbLf}, StringSplitOptions.RemoveEmptyEntries).Length
    End Function
    
    Private Function EstimateFileCount(text As String) As Integer
        If String.IsNullOrWhiteSpace(text) Then Return 0
        
        ' Try common patterns for file listings
        Dim patterns = {
            "\.(?:txt|doc|docx|pdf|xlsx|xls|csv|jpg|png|gif|bmp|mp4|mp3|zip|rar|exe|dll|vb|cs|js|html|css)",
            "^[A-Za-z]:\\",
            "File \d+:",
            "\[\d+\]"
        }
        
        Dim maxCount = 0
        For Each pattern In patterns
            Dim count = Regex.Matches(text, pattern, RegexOptions.Multiline Or RegexOptions.IgnoreCase).Count
            If count > maxCount Then maxCount = count
        Next
        
        ' Fallback: count lines
        If maxCount = 0 Then
            maxCount = CountLines(text)
        End If
        
        Return maxCount
    End Function
    
End Module

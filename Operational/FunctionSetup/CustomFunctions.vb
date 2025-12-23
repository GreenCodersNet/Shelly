'==============================================
' CUSTOM FUNCTIONS MODULE - PART 1
' ALL CUSTOM FUNCTION DEFINITIONS ARE SET HERE.
' HELPER FUNCTIONS FOR CUSTOM FUNCTIONS MUST BE PLACED INSIDE [HelperFunctions.vb] FILE.
'==============================================

Imports System.Runtime.InteropServices
Imports NAudio.CoreAudioApi
Imports System.Threading
Imports System.IO
Imports FxResources.System
Imports System.Text.RegularExpressions
Imports System.Reflection
Imports System.Text
Imports System.Globalization


Public Module CustomFunctions

    ' Win32 API function for simulating key events.
    <DllImport("user32.dll", SetLastError:=True)>
    Private Sub keybd_event(bVk As Byte, bScan As Byte, dwFlags As UInteger, dwExtraInfo As UIntPtr)
    End Sub

    ' Constants for key events.
    Public Const WmKeyDown As UInteger = &H100
    Public Const WKeyUp As UInteger = &H101
    Public Const VkReturn As Integer = 13

    ' Constants for key events
    Private Const KEYEVENTF_EXTENDEDKEY As UInteger = &H1
    Private Const KEYEVENTF_KEYUP As UInteger = &H2

    ' Sends a media key press using the provided key name.

    Public Async Function SendMediaKey(keyName As String) As Task(Of String)
        Dim keyMap As New Dictionary(Of String, Byte) From {
            {"VK_MEDIA_PLAY_PAUSE", &HB3},
            {"VK_MEDIA_STOP", &HB2},
            {"VK_MEDIA_NEXT_TRACK", &HB0},
            {"VK_MEDIA_PREV_TRACK", &HB1},
            {"VK_VOLUME_MUTE", &HAD},
            {"VK_VOLUME_UP", &HAF},
            {"VK_VOLUME_DOWN", &HAE},
            {"VK_BROWSER_HOME", &HAC},
            {"VK_BROWSER_REFRESH", &HA8},
            {"VK_BROWSER_FORWARD", &HA7},
            {"VK_BROWSER_BACK", &HA6},
            {"VK_BROWSER_SEARCH", &HAA},
            {"VK_LAUNCH_MAIL", &HB4},
            {"VK_LAUNCH_MEDIA_SELECT", &HB5},
            {"VK_LAUNCH_APP1", &HB6},
            {"VK_LAUNCH_APP2", &HB7},
            {"VK_SNAPSHOT", &H2C},
            {"VK_SCROLL", &H91},
            {"VK_PAUSE", &H13},
            {"VK_TAB", &H9},
            {"VK_ENTER", &HD},
            {"VK_RETURN", &HD},
            {"VK_BACK", &H8},
            {"VK_DELETE", &H2E},
            {"VK_INSERT", &H2D},
            {"VK_HOME", &H24},
            {"VK_END", &H23},
            {"VK_PRIOR", &H21},
            {"VK_NEXT", &H22},
            {"VK_LEFT", &H25},
            {"VK_UP", &H26},
            {"VK_RIGHT", &H27},
            {"VK_DOWN", &H28},
            {"VK_ESCAPE", &H1B},
            {"VK_SPACE", &H20},
            {"VK_CONTROL", &H11},
            {"VK_SHIFT", &H10},
            {"VK_MENU", &H12},
            {"VK_CAPITAL", &H14},
            {"VK_WIN", &H5B},
            {"VK_RWIN", &H5C},
            {"VK_MEDIA_PLAY", &HB3},
            {"VK_PLAY", &HB3},
            {"VK_MEDIA_NEXT", &HB0},
            {"VK_MEDIA_PREV", &HB1}
        }

        Dim mediaKeys As New HashSet(Of String) From {
            "VK_MEDIA_PLAY_PAUSE",
            "VK_MEDIA_STOP",
            "VK_MEDIA_NEXT_TRACK",
            "VK_MEDIA_PREV_TRACK",
            "VK_VOLUME_MUTE",
            "VK_VOLUME_UP",
            "VK_VOLUME_DOWN",
            "VK_MEDIA_PLAY",
            "VK_PLAY",
            "VK_MEDIA_NEXT",
            "VK_MEDIA_PREV"
        }

        Dim upperKey As String = keyName.ToUpper()
        Dim vk As Byte
        If keyMap.TryGetValue(upperKey, vk) Then
            Dim flagsDown As UInteger
            Dim flagsUp As UInteger

            If mediaKeys.Contains(upperKey) Then
                flagsDown = KEYEVENTF_EXTENDEDKEY
                flagsUp = KEYEVENTF_EXTENDEDKEY Or KEYEVENTF_KEYUP
            Else
                flagsDown = 0
                flagsUp = KEYEVENTF_KEYUP
            End If

            ' Simulate key press with a small delay to ensure registration
            keybd_event(vk, 0, flagsDown, UIntPtr.Zero)
            Await Task.Delay(100)
            keybd_event(vk, 0, flagsUp, UIntPtr.Zero)

            Return $"Sent media key: {upperKey}"
        Else
            Dim shellyInstance = Shelly.Instance
            If shellyInstance IsNot Nothing Then
                shellyInstance.LabelStatusUpdate.Text = "Unknown key: " & keyName & " Error"
                Await shellyInstance.ExecuteScriptSafeAsync("setColorDefault();")
            End If
            Return $"[ERROR] Unknown key: {keyName}"
        End If
    End Function


    ' PowerShell script to start an application by name.
    Public Const StartOrRunApplicationByNameScript As String = "
Function StartOrRunApplicationByName {
    param (
        [string]$appName
    )

    # Define the Start Menu directories.
    $startMenuDirs = @(
        [System.Environment]::GetFolderPath('StartMenu'),
        [System.Environment]::GetFolderPath('CommonStartMenu')
    )

    # Collect all matching shortcuts (case-insensitive match).
    $matches = @()
    foreach ($dir in $startMenuDirs) {
        $matches += Get-ChildItem -Path $dir -Recurse -Filter '*.lnk' -ErrorAction SilentlyContinue |
            Where-Object { $_.BaseName -imatch $appName }
    }

    if ($matches.Count -gt 0) {
        # Sort matches by the position of the search term in the BaseName.
        $bestMatch = $matches | Sort-Object {
            ($_.BaseName.ToLower()).IndexOf($appName.ToLower())
        } | Select-Object -First 1

        if ($bestMatch) {
            try {
                Start-Process $bestMatch.FullName
                Write-Host 'Successfully started application: ' + $bestMatch.BaseName
                return
            } catch {
                Write-Host 'Failed to start application: ' + $bestMatch.BaseName + '. Error: ' + $_.Exception.Message
            }
        }
    }

    Write-Host (""Application '{0}' not found in Start Menu."" -f $appName)
}
"
    ' PowerShell script to take a screenshot of the main monitor.
    Public Const TakePrintScreenOrScreenShotScript As String = "
Function TakePrintScreenOrScreenShot {
    param (
        [string]$OutputPath
    )
    
    # Default and normalization
    if ([string]::IsNullOrWhiteSpace($OutputPath)) {
        $OutputPath = 'C:\Shelly'
    }
    else {
        # Remove surrounding quotes/whitespace
        $OutputPath = $OutputPath.Trim().Trim('""')
        if ([string]::IsNullOrWhiteSpace($OutputPath)) {
            $OutputPath = 'C:\Shelly'
        }
    }

    try {
        $normalized = [System.IO.Path]::GetFullPath($OutputPath)
    } catch {
        $normalized = 'C:\Shelly'
    }

    # Determine folder/file
    $folder = $null
    $fileName = $null

    if ([System.IO.Path]::HasExtension($normalized)) {
        $folder = [System.IO.Path]::GetDirectoryName($normalized)
        $fileName = [System.IO.Path]::GetFileName($normalized)
    }
    else {
        $folder = $normalized.TrimEnd([char]92, [char]47)
        $fileName = 'Screenshot_' + (Get-Date -Format 'yyyyMMdd_HHmmss') + '.png'
    }

    if ([string]::IsNullOrWhiteSpace($folder)) {
        $folder = 'C:\Shelly'
    }

    # Ensure file has .png extension
    if ([string]::IsNullOrWhiteSpace([System.IO.Path]::GetExtension($fileName))) {
        $fileName = $fileName + '.png'
    }

    # Create folder (recursive, safe for drive roots)
    [System.IO.Directory]::CreateDirectory($folder) | Out-Null
    
    $filePath = Join-Path -Path $folder -ChildPath $fileName
    
    Add-Type -AssemblyName System.Windows.Forms
    Add-Type -AssemblyName System.Drawing
    
    $bitmap = $null
    $graphics = $null
    $ms = $null

    try {
        $bounds = [System.Windows.Forms.Screen]::PrimaryScreen.Bounds
        $bitmap = New-Object System.Drawing.Bitmap $bounds.Width, $bounds.Height
        $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
        $graphics.CopyFromScreen($bounds.Location, [System.Drawing.Point]::Empty, $bounds.Size)
        $graphics.Dispose(); $graphics = $null

        $ms = New-Object System.IO.MemoryStream
        $bitmap.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
        $ms.Seek(0, [System.IO.SeekOrigin]::Begin) | Out-Null
        [System.IO.File]::WriteAllBytes($filePath, $ms.ToArray())
    }
    catch {
        if ($ms) { $ms.Dispose() }
        if ($graphics) { $graphics.Dispose() }
        if ($bitmap) { $bitmap.Dispose() }
        Write-Error (""Failed to save screenshot: "" + $_.Exception.Message)
        return
    }
    finally {
        if ($ms) { $ms.Dispose() }
        if ($graphics) { $graphics.Dispose() }
        if ($bitmap) { $bitmap.Dispose() }
    }
    
    Write-Output ('Screenshot saved to: ' + $filePath)
}
"

    ' Sets the system volume to the given percentage.
    Public Function ChangeOrSetVolume(volumePercentage As Integer) As String
        Try
            If volumePercentage < 0 OrElse volumePercentage > 100 Then
                Shelly.Instance.LabelStatusUpdate.Text = "Volume percentage must be between 0 and 100. Error"
                Return "[ERROR] Volume percentage must be between 0 and 100."
            End If

            Dim enumerator As New MMDeviceEnumerator()
            Dim device As MMDevice = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia)
            Dim newVolume As Single = CSng(volumePercentage) / 100.0F
            device.AudioEndpointVolume.MasterVolumeLevelScalar = newVolume
            Shelly.Instance.LabelStatusUpdate.Text = $"Volume set to {volumePercentage}%."
            device.Dispose()
            enumerator.Dispose()
            Return $"✅ Volume set to {volumePercentage}%."
        Catch ex As Exception
            Shelly.Instance.LabelStatusUpdate.Text = $"Error setting volume: {ex.Message} Error"
            Return $"[ERROR] Error setting volume: {ex.Message}"
        End Try
    End Function


    ' =====================================================================================================
    ' ================================= IMAGE MANIPULATION CUSTOM FUNCTIONS ================================
    ' =====================================================================================================

    ' ----------------------------------------------------------------------
    ' v3.1 – GenerateImages
    '   • Friendly, unique filenames: <slug‑of‑prompt>‑<yyyyMMdd‑HHmmss>-<n>.jpg
    '   • Ensures folder exists; prevents name collisions by appending “‑1”, “‑2”, …
    '   • Returns full list, one per line, for easy copy‑paste.
    ' ----------------------------------------------------------------------
    Public Async Function GenerateImages(
        imagePrompt As String,
        numImages As Integer,
        style As String,
        folderPath As String) As Task(Of String)

        Try
            ' -------- 1. normalise folder ----------------------------------------
            If File.Exists(folderPath) Then
                folderPath = Path.GetDirectoryName(folderPath)
            End If
            If Not Directory.Exists(folderPath) Then
                Directory.CreateDirectory(folderPath)
            End If

            ' -------- 2. build nice base filename --------------------------------
            Dim slug As String = Regex.Replace(
            imagePrompt.ToLowerInvariant(),
            "[^a-z0-9]+", "-").Trim("-"c)
            If slug.Length > 40 Then slug = slug.Substring(0, 40)

            Dim stamp As String = DateTime.Now.ToString("yyyyMMdd-HHmms")

            ' -------- 3. create ImageRequestData list ----------------------------
            Dim reqs As New List(Of ImageRequestData)
            For i = 1 To numImages
                Dim fname As String = $"{slug}-{stamp}-{i}.jpg"

                ' avoid accidental collision
                Dim finalName As String = fname
                Dim suffix As Integer = 1
                While File.Exists(Path.Combine(folderPath, finalName))
                    suffix += 1
                    finalName = $"{Path.GetFileNameWithoutExtension(fname)}-{suffix}.jpg"
                End While

                reqs.Add(New ImageRequestData With {
                .ImagePrompt = $"{imagePrompt} {style}".Trim(),
                .Size = "1792x1024",
                .FolderPath = folderPath,
                .ImageName = finalName
            })
            Next

            ' -------- 4. call OpenAI image endpoint ------------------------------
            Dim urls As List(Of String) =
            Await AIimage.CallImageGeneration(Config.OpenAiApiKey, reqs)
            If urls Is Nothing OrElse urls.Count = 0 Then
                Return "[ERROR] No images returned by the API."
            End If

            ' -------- 5. download & record ---------------------------------------
            Dim saved As New List(Of String)
            For i = 0 To urls.Count - 1
                Dim local = Path.Combine(folderPath, reqs(i).ImageName)
                Await AIimage.DownloadImage(urls(i), local)
                saved.Add(local)
            Next

            Globals.GeneratedImages.AddRange(saved)
            Globals.TaskData("ImagePaths") = saved

            Return "Images saved:" & Environment.NewLine &
               String.Join(Environment.NewLine, saved)

        Catch ex As Exception
            Return "[ERROR] " & ex.Message
        End Try
    End Function


    <CustomFunction(
     "Analyzes one or more images to answer a user’s question about their visual content. " &
     "For each valid image (JPEG, JPG, PNG, GIF, BMP, WebP), the function converts the image to Base64 " &
     "and sends it along with the query to OpenAI's chat completions endpoint (GPT-4) to generate an answer.",
     "ImageAnswer(""C:\Shelly\image1.jpg, C:\Shelly\image2.jpg"", ""How many fruits do you see?"")")>
    Public Async Function ImageAnswer(imagePaths As String, query As String) As Task(Of String)
        ' Split input by comma and check each file exists.
        Dim paths() As String = imagePaths.Split(","c)
        For Each p In paths
            Dim trimmedPath As String = p.Trim()
            If Not System.IO.File.Exists(trimmedPath) Then
                Return $"Invalid input: {trimmedPath}"
            End If
        Next

        ' All files exist – proceed to analyze.
        Return Await AIimage.AnalyzeImagesContentAsync(Config.OpenAiApiKey, imagePaths, query)
    End Function

    <CustomFunction(
      "Captures (or re-uses a previously captured) screenshot of the primary monitor and analyzes its content based on the user's query. " &
      "If a screenshot has already been captured, that image is re-used for analysis until a new screenshot is specifically requested.",
      "MyScreenAnswer(""What error do you see on my screen?"")")>
    Public Async Function CheckMyScreenAndAnswer(query As String) As Task(Of String)
        Return Await AIimage.AnalyzeScreenshotAsync(Config.OpenAiApiKey, query)
    End Function


    ' =====================================================================================================
    ' ================================= FILE MANIPULATION CUSTOM FUNCTIONS ================================
    ' =====================================================================================================

    ' Generates batch (.bat) and PowerShell (.ps1) files based on a user query
    <CustomFunction(
      "Generates both a batch (.bat) file and a PowerShell (.ps1) file based on the user's query. " &
      "The files are saved to the specified output folder with appropriate content generated by AI.",
      "GenerateBatchAndPs1File(""C:\Scripts"", ""Create a script that cleans temp files"")"
    )>
    Public Async Function GenerateBatchAndPs1File(
        outputFolder As String,
        userQuery As String,
        Optional ct As CancellationToken = Nothing
    ) As Task(Of String)
        Try
            ' Use active cancellation token if none provided
            If ct = Nothing OrElse ct = CancellationToken.None Then
                ct = FileHandler.ActiveCancellationToken
            End If

            ' Ensure the output folder exists
            If Not Directory.Exists(outputFolder) Then
                Directory.CreateDirectory(outputFolder)
            End If

            ' Generate a filename base from the query
            Dim rawSlug As String = Regex.Replace(
                userQuery.ToLowerInvariant(),
                "[^a-z0-9]+", "-").Trim("-"c)
            Dim invalidChars = Path.GetInvalidFileNameChars()
            Dim slugBuilder As New StringBuilder()
            For Each ch As Char In rawSlug
                If (ch = "-"c OrElse Char.IsLetterOrDigit(ch)) AndAlso Array.IndexOf(invalidChars, ch) = -1 Then
                    slugBuilder.Append(ch)
                End If
            Next
            Dim cleanedSlug As String = slugBuilder.ToString()
            If String.IsNullOrWhiteSpace(cleanedSlug) Then cleanedSlug = "script"
            Dim slugTokens = cleanedSlug.Split(New Char() {"-"c}, StringSplitOptions.RemoveEmptyEntries)
            If slugTokens.Length > 0 Then
                cleanedSlug = String.Join("-", slugTokens.Take(2))
            End If
            If cleanedSlug.Length > 20 Then cleanedSlug = cleanedSlug.Substring(0, 20)
            Dim timestamp As String = DateTime.Now.ToString("yyyyMMdd-HHmmss")
            Dim baseFileName As String = $"{cleanedSlug}-{timestamp}"

            ' Define file paths
            Dim batPath As String = Path.Combine(outputFolder, $"{baseFileName}.bat")
            Dim ps1Path As String = Path.Combine(outputFolder, $"{baseFileName}.ps1")

            ' Generate batch file content
            Dim batPrompt As String = $"Create a Windows batch (.bat) script that satisfies the following user request:{Environment.NewLine}{userQuery}{Environment.NewLine}{Environment.NewLine}Constraints:{Environment.NewLine}- Implement the entire behavior in this single batch file (do NOT create or reference other scripts).{Environment.NewLine}- Prompt the user exactly as described and keep the console window open at the end (for example, by calling 'pause').{Environment.NewLine}- Handle errors gracefully and display user-friendly messages.{Environment.NewLine}- Output ONLY the batch code, with no explanations or markdown fences."
            Dim batMessages As New List(Of Dictionary(Of String, String)) From {
                New Dictionary(Of String, String) From {
                    {"role", "system"},
                    {"content", "You are a script generator. Output only valid batch script code with no markdown or explanations."}
                },
                New Dictionary(Of String, String) From {
                    {"role", "user"},
                    {"content", batPrompt}
                }
            }
            Dim batContent As String = Await AIcall.CallGPTCore(
                Globals.UserApiKey,
                Globals.AiModelSelection,
                batMessages,
                Globals.temperature,
                ct
            )
            batContent = StripCodeFences(batContent)
            If String.IsNullOrWhiteSpace(batContent) Then
                Return "[ERROR] AI did not return any batch script content."
            End If

            ' Generate PowerShell script content
            Dim ps1Prompt As String = $"Create a PowerShell (.ps1) script that satisfies the following user request:{Environment.NewLine}{userQuery}{Environment.NewLine}{Environment.NewLine}Constraints:{Environment.NewLine}- Implement the entire behavior in this single PowerShell file without creating additional files.{Environment.NewLine}- Prompt for user input exactly as described and display results within the console, keeping the window open at the end (e.g., Read-Host).{Environment.NewLine}- Handle errors gracefully and provide clear user-facing messages.{Environment.NewLine}- Output ONLY the PowerShell code, with no explanations or markdown fences."
            Dim ps1Messages As New List(Of Dictionary(Of String, String)) From {
                New Dictionary(Of String, String) From {
                    {"role", "system"},
                    {"content", "You are a script generator. Output only valid PowerShell script code with no markdown or explanations."}
                },
                New Dictionary(Of String, String) From {
                    {"role", "user"},
                    {"content", ps1Prompt}
                }
            }
            Dim ps1Content As String = Await AIcall.CallGPTCore(
                Globals.UserApiKey,
                Globals.AiModelSelection,
                ps1Messages,
                Globals.temperature,
                ct
            )
            ps1Content = StripCodeFences(ps1Content)
            If String.IsNullOrWhiteSpace(ps1Content) Then
                Return "[ERROR] AI did not return any PowerShell script content."
            End If

            ' Write files with safe encodings (BAT = ASCII, PS1 = UTF-8 without BOM)
            Dim asciiEncoding = Encoding.ASCII
            Dim utf8NoBom = New UTF8Encoding(False)
            File.WriteAllText(batPath, batContent, asciiEncoding)
            File.WriteAllText(ps1Path, ps1Content, utf8NoBom)

            Return $"Scripts generated successfully:" & Environment.NewLine &
                   $"  Batch file: {batPath}" & Environment.NewLine &
                   $"  PowerShell file: {ps1Path}"

        Catch ex As OperationCanceledException
            Return "[Cancelled] Script generation was cancelled."
        Catch ex As Exception
            Return "[ERROR] " & ex.Message
        End Try
    End Function



    ' Generates a large file by appending multiple content chunks generated by AI.
    ' Updated GenerateLargeFileWithPowerShell function
    <CustomFunction(
      "Generates arbitrarily large files (Word or any text-based) by asking the AI in multiple chunks.",
      "GenerateLargeFileWithTextOrCode(""Create a 10-page essay on climate change."", ""C:\Demo\Essay.docx"", 10)"
    )>
    Public Async Function GenerateLargeFileWithTextOrCode(
      topic As String,
      outputPath As String,
      Optional totalChunks As Integer = 5,
      Optional ct As CancellationToken = Nothing
    ) As Task(Of String)

        Try
            ' Use active cancellation token if none provided
            If ct = Nothing OrElse ct = CancellationToken.None Then
                ct = FileHandler.ActiveCancellationToken
            End If

            ' Delegate to our helper
            Dim result = Await generateFiles.GenerateLargeFileWithTextOrCode(
              filePath:=outputPath,
              userPrompt:=topic,
              totalChunks:=totalChunks,
              ct:=ct
            )

            If result.Item1 Then
                Return result.Item2
            Else
                Return "[ERROR] " & result.Item2
            End If

        Catch ex As OperationCanceledException
            Return "[Cancelled] File generation was cancelled."
        Catch ex As Exception
            Return "[ERROR] Exception in GenerateLargeFileWithTextOrCode: " & ex.Message
        End Try

    End Function




End Module


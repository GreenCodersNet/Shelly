Imports System.Runtime.InteropServices
Imports NAudio.CoreAudioApi
Imports System.Threading
Imports System.IO
Imports FxResources.System
Imports System.Text.RegularExpressions
Imports System.Reflection
Imports System.Text


Public Module CustomFunctions

    ' Win32 API function for simulating key events.
    <DllImport("user32.dll", SetLastError:=True)>
    Private Sub KeybdEvent(bVk As Byte, bScan As Byte, dwFlags As UInteger, dwExtraInfo As UIntPtr)
    End Sub

    ' Constants for key events.
    Public Const WmKeyDown As UInteger = &H100
    Public Const WKeyUp As UInteger = &H101
    Public Const VkReturn As Integer = 13

    ' Constants for key events
    Private Const KEYEVENTF_EXTENDEDKEY As UInteger = &H1
    Private Const KEYEVENTF_KEYUP As UInteger = &H2

    ' Sends a media key press using the provided key name.

    Public Async Function SendMediaKey(keyName As String) As Task
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
            {"VK_RWIN", &H5C}
        }

        Dim mediaKeys As New HashSet(Of String) From {
            "VK_MEDIA_PLAY_PAUSE",
            "VK_MEDIA_STOP",
            "VK_MEDIA_NEXT_TRACK",
            "VK_MEDIA_PREV_TRACK",
            "VK_VOLUME_MUTE",
            "VK_VOLUME_UP",
            "VK_VOLUME_DOWN"
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

            ' Simulate key press
            KeybdEvent(vk, 0, flagsDown, UIntPtr.Zero)
            KeybdEvent(vk, 0, flagsUp, UIntPtr.Zero)
        Else
            Dim shellyInstance = Shelly.Instance
            If shellyInstance IsNot Nothing Then
                shellyInstance.LabelStatusUpdate.Text = "Unknown key: " & keyName & " Error"
                Await shellyInstance.ExecuteScriptSafeAsync("setColorDefault();")
            End If
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
    if (-not (Test-Path $OutputPath)) {
        New-Item -ItemType Directory -Path $OutputPath | Out-Null
    }
    $fileName = 'Screenshot_' + (Get-Date -Format 'yyyyMMdd_HHmmss') + '.png'
    $filePath = Join-Path -Path $OutputPath -ChildPath $fileName
    Add-Type -AssemblyName System.Windows.Forms
    Add-Type -AssemblyName System.Drawing
    $bounds = [System.Windows.Forms.Screen]::PrimaryScreen.Bounds
    $bitmap = New-Object System.Drawing.Bitmap $bounds.Width, $bounds.Height
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.CopyFromScreen($bounds.Location, [System.Drawing.Point]::Empty, $bounds.Size)
    $bitmap.Save($filePath, [System.Drawing.Imaging.ImageFormat]::Png)
    $graphics.Dispose()
    $bitmap.Dispose()
    Write-Output 'Screenshot saved to: ' + $filePath
}
"

    ' Sets the system volume to the given percentage.
    Public Sub ChangeOrSetVolume(volumePercentage As Integer)
        Try
            If volumePercentage < 0 OrElse volumePercentage > 100 Then
                Shelly.Instance.LabelStatusUpdate.Text = "Volume percentage must be between 0 and 100. Error"
                Exit Sub
            End If

            Dim enumerator As New MMDeviceEnumerator()
            Dim device As MMDevice = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia)
            Dim newVolume As Single = CSng(volumePercentage) / 100.0F
            device.AudioEndpointVolume.MasterVolumeLevelScalar = newVolume
            Shelly.Instance.LabelStatusUpdate.Text = $"Volume set to {volumePercentage}%."
            device.Dispose()
            enumerator.Dispose()
        Catch ex As Exception
            Shelly.Instance.LabelStatusUpdate.Text = $"Error setting volume: {ex.Message} Error"
        End Try
    End Sub


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

            Dim stamp As String = DateTime.Now.ToString("yyyyMMdd-HHmmss")

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
                Dim local = Path.Combine(folderPath, reqs(i).imageName)
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

    Public Async Function ReadFileAndAnswer(
        ByVal filePaths As String,
        ByVal query As String,
        Optional ct As CancellationToken = Nothing
    ) As Task(Of String)

        Try
            ' Use active cancellation token if none provided
            If ct = Nothing OrElse ct = CancellationToken.None Then
                ct = FileHandler.ActiveCancellationToken
            End If

            ' Check for cancellation before starting
            ct.ThrowIfCancellationRequested()

            Dim paths = filePaths.Split(","c) _
                             .Select(Function(p) p.Trim()) _
                             .ToArray()
            Dim combinedText As New StringBuilder()

            For Each path In paths
                ct.ThrowIfCancellationRequested()

                If Not File.Exists(path) Then
                    Throw New FileNotFoundException($"File not found: {path}")
                End If

                Dim name = System.IO.Path.GetFileName(path)

                combinedText.AppendLine($"--- Begin File: {name} ---")
                combinedText.AppendLine(FileHandler.GetFileContent(path))
                combinedText.AppendLine($"--- End File: {name} ---")
            Next

            ' 3) Break into chunks under your token limit
            Dim fullText = combinedText.ToString()
            Dim maxCharsPerChunk As Integer = Globals.maxInputTokensPerChunk * 4
            Dim chunks As New List(Of String)
            Dim current As New StringBuilder()
            For Each line In fullText.Split({Environment.NewLine}, StringSplitOptions.None)
                If current.Length + line.Length + 1 > maxCharsPerChunk AndAlso current.Length > 0 Then
                    chunks.Add(current.ToString())
                    current.Clear()
                End If
                current.AppendLine(line)
            Next
            If current.Length > 0 Then chunks.Add(current.ToString())

            ' 4) Build messages: generic system prompt + all chunks + final question
            Dim messages As New List(Of Dictionary(Of String, String)) From {
                New Dictionary(Of String, String) From {
                    {"role", "system"},
                    {"content", "You are a helpful assistant. Read the provided text and answer the user's question based on its content."}
                }
            }

            ' feed each chunk
            For idx = 0 To chunks.Count - 1
                Dim tag As String = If(idx = 0, $"<CHUNK 1/{chunks.Count}>", $"<CONTINUATION {idx + 1}/{chunks.Count}>")
                messages.Add(New Dictionary(Of String, String) From {
                    {"role", "user"},
                    {"content", tag & vbCrLf & chunks(idx)}
                })
            Next

            ' then the actual question
            messages.Add(New Dictionary(Of String, String) From {
                {"role", "user"},
                {"content", $"Now, based on all of the above, {query}"}
            })

            ' 5) Single API call with cancellation support
            Dim answer = Await AIcall.CallGPTCore(
                apiKey:=Config.OpenAiApiKey,
                model:=Globals.AiModelSelection,
                messages:=messages,
                temperature:=0.0,
                ct:=ct
            )

            Return answer.Trim()
        Catch ex As OperationCanceledException
            Return "[Cancelled] File reading was cancelled."
        Catch ex As Exception
            Return "[ERROR] " & ex.Message
        End Try
    End Function


    ' This function types text directly into the active file or window by pasting its full content.
    ' Now supports cancellation and has loop protection
    ' IMPROVED: Better window detection, focus handling, and paste mechanism
    Public Async Function WriteInsideFileOrWindow(topic As String, Optional totalChunks As Integer = 1, Optional ct As CancellationToken = Nothing) As Task(Of String)
        ' Reset typing stopped flag at the start
        typingStopped = False

        ' Use the provided token or fall back to the active one
        If ct = Nothing OrElse ct = CancellationToken.None Then
            ct = FileHandler.ActiveCancellationToken
        End If

        ' Safety: Limit chunks to prevent runaway loops
        Const MAX_CHUNKS As Integer = 50
        If totalChunks > MAX_CHUNKS Then
            Debug.WriteLine($"[WriteInsideFileOrWindow] Capping chunks from {totalChunks} to {MAX_CHUNKS}")
            totalChunks = MAX_CHUNKS
        End If
        If totalChunks < 1 Then totalChunks = 1

        Debug.WriteLine("----- WriteInsideFileOrWindow START -----")
        Debug.WriteLine($"Topic = {topic}, totalChunks = {totalChunks}")

        ' 1. Show user instruction and wait for them to click on target window
        Shelly.Instance.LabelStatusUpdate.Text = "Click on the target window within 5 seconds..."

        Try
            ' Wait for user to position their cursor/click on target window
            For countdown As Integer = 5 To 1 Step -1
                If ct.IsCancellationRequested OrElse typingStopped Then
                    Return "[Cancelled] Operation was cancelled by user."
                End If
                Shelly.Instance.LabelStatusUpdate.Text = $"Click on target window... {countdown}s"
                Await Task.Delay(1000, ct)
            Next
        Catch ex As OperationCanceledException
            Debug.WriteLine("[WriteInsideFileOrWindow] Cancelled during countdown.")
            Return "[Cancelled] Operation was cancelled by user."
        End Try

        ' 2. NOW capture the window after the delay (user should have clicked)
        originalWindow = FileHandler.GetForegroundWindow()
        originalControl = FileHandler.GetFocusedControl(originalWindow)

        Debug.WriteLine($"Captured window handle = {originalWindow}, control handle = {originalControl}")

        ' Validate window capture
        If originalWindow = IntPtr.Zero Then
            Debug.WriteLine("[WriteInsideFileOrWindow] No foreground window detected.")
            Shelly.Instance.LabelStatusUpdate.Text = "Error: No target window detected."
            Return "[ERROR] No foreground window detected. Please click on a text input area and try again."
        End If

        ' Get window title for debugging
        Dim windowTitle As String = GetWindowTitle(originalWindow)
        Debug.WriteLine($"Target window title: {windowTitle}")
        Shelly.Instance.LabelStatusUpdate.Text = $"Writing to: {If(String.IsNullOrEmpty(windowTitle), "Unknown Window", If(windowTitle.Length > 30, windowTitle.Substring(0, 30) & "...", windowTitle))}"

        ' Small delay to ensure focus is stable
        Await Task.Delay(200, ct)

        Dim chunksProcessed As Integer = 0
        Dim totalCharsWritten As Integer = 0

        For i As Integer = 1 To totalChunks
            ' Check for cancellation at the start of each iteration
            If ct.IsCancellationRequested OrElse typingStopped Then
                Debug.WriteLine($"[WriteInsideFileOrWindow] Cancelled at chunk {i}/{totalChunks}")
                Exit For
            End If

            Try
                Shelly.Instance.LabelStatusUpdate.Text = $"Generating chunk {i}/{totalChunks}..."

                Dim promptText As String = $"
You are writing part #{i} of {totalChunks} for: {topic}.
Generate the exact content that should be inserted, preserving all spaces, line breaks, tabs, and formatting exactly as it should appear.
***IMPORTANT: Do not include any code block markers (```), markdown formatting, or extra commentary. Output ONLY the raw text content.***"

                Dim messages As New List(Of Dictionary(Of String, String)) From {
                    New Dictionary(Of String, String) From {{"role", "system"}, {"content", "You are a content generator. Output ONLY the requested text with no markdown, code fences, or commentary."}},
                    New Dictionary(Of String, String) From {{"role", "user"}, {"content", promptText}}
                }

                Dim chunkText As String = Await AIcall.CallGPTCore(Config.OpenAiApiKey, Config.AiModel, messages, Globals.temperature, ct)

                ' Check cancellation after AI call
                If ct.IsCancellationRequested OrElse typingStopped Then
                    Debug.WriteLine($"[WriteInsideFileOrWindow] Cancelled after AI call for chunk {i}")
                    Exit For
                End If

                ' Clean up the response
                chunkText = RemoveCustomFunctionCodeBlocks(chunkText)
                chunkText = chunkText.Trim()

                If String.IsNullOrEmpty(chunkText) Then
                    Debug.WriteLine($"[WriteInsideFileOrWindow] Empty content for chunk #{i}, skipping.")
                    Continue For
                End If

                Debug.WriteLine($"----- AI Generated Text for Chunk {i} (length={chunkText.Length}) -----")
                Debug.WriteLine(If(chunkText.Length > 200, chunkText.Substring(0, 200) & "...", chunkText))
                Debug.WriteLine("----- END OF TEXT -----")

                Shelly.Instance.LabelStatusUpdate.Text = $"Pasting chunk {i}/{totalChunks}..."

                ' Use the improved paste function with retry logic
                Dim pasteSuccess As Boolean = Await PasteTextToWindow(originalWindow, chunkText, ct)

                If pasteSuccess Then
                    chunksProcessed += 1
                    totalCharsWritten += chunkText.Length
                    Debug.WriteLine($"[WriteInsideFileOrWindow] Chunk {i} pasted successfully ({chunkText.Length} chars)")
                Else
                    Debug.WriteLine($"[WriteInsideFileOrWindow] Failed to paste chunk {i}")
                    ' Try to continue with next chunk anyway
                End If

                ' Delay between chunks to allow UI to update and paste to complete
                Await Task.Delay(300, ct)

            Catch ex As OperationCanceledException
                Debug.WriteLine($"[WriteInsideFileOrWindow] OperationCanceledException at chunk {i}")
                Exit For
            Catch ex As Exception
                Debug.WriteLine($"[WriteInsideFileOrWindow] Error at chunk {i}: {ex.Message}")
                ' Continue to next chunk on error, don't break
            End Try
        Next

        ' Clear the clipboard to release any lock
        Try
            ClearClipboardSafe()
        Catch
            ' Ignore clipboard errors
        End Try

        Debug.WriteLine("----- WriteInsideFileOrWindow END -----")
        Debug.WriteLine($"Processed {chunksProcessed}/{totalChunks} chunks, {totalCharsWritten} total characters")

        If chunksProcessed = 0 Then
            Shelly.Instance.LabelStatusUpdate.Text = "Error: No content was written."
            Return "[ERROR] No content was generated or pasted. Please ensure you clicked on a text input area."
        ElseIf ct.IsCancellationRequested OrElse typingStopped Then
            Shelly.Instance.LabelStatusUpdate.Text = $"Cancelled after {chunksProcessed} chunk(s)."
            Return $"[Cancelled] Processed {chunksProcessed}/{totalChunks} chunks ({totalCharsWritten} characters) before cancellation."
        Else
            Shelly.Instance.LabelStatusUpdate.Text = $"Successfully wrote {chunksProcessed} chunk(s)."
            Return $"Successfully wrote {chunksProcessed} chunk(s) ({totalCharsWritten} characters) to the window."
        End If
    End Function

    ' Helper function to get window title
    <Runtime.InteropServices.DllImport("user32.dll", CharSet:=Runtime.InteropServices.CharSet.Auto)>
    Private Function GetWindowText(hWnd As IntPtr, lpString As Text.StringBuilder, nMaxCount As Integer) As Integer
    End Function

    Private Function GetWindowTitle(hWnd As IntPtr) As String
        Dim sb As New Text.StringBuilder(256)
        GetWindowText(hWnd, sb, sb.Capacity)
        Return sb.ToString()
    End Function

    ' Simplified and more reliable paste function
    Private Async Function PasteTextToWindow(targetWindow As IntPtr, textToPaste As String, ct As CancellationToken) As Task(Of Boolean)
        Const MAX_RETRIES As Integer = 3
        Const SW_SHOW As Integer = 5

        Dim needsRetryDelay As Boolean = False

        For attempt As Integer = 1 To MAX_RETRIES
            ' Handle retry delay from previous failed attempt (moved outside catch block)
            If needsRetryDelay Then
                Await Task.Delay(300, ct)
                needsRetryDelay = False
            End If

            If ct.IsCancellationRequested OrElse typingStopped Then
                Return False
            End If

            Dim attemptSucceeded As Boolean = False
            Dim shouldContinue As Boolean = False

            Try
                Debug.WriteLine($"[PasteTextToWindow] Attempt {attempt}/{MAX_RETRIES}")

                ' Step 1: Bring target window to foreground
                FileHandler.ShowWindow(targetWindow, SW_SHOW)
                FileHandler.BringWindowToTop(targetWindow)
                FileHandler.SetForegroundWindow(targetWindow)

                ' Wait for window to become active
                Await Task.Delay(150, ct)

                ' Step 2: Verify we have the right window
                Dim currentWindow As IntPtr = FileHandler.GetForegroundWindow()
                If currentWindow <> targetWindow Then
                    Debug.WriteLine($"[PasteTextToWindow] Window focus lost on attempt {attempt}")
                    shouldContinue = True
                End If

                If Not shouldContinue Then
                    ' Step 3: Set clipboard using STA thread (required for clipboard operations)
                    Dim clipboardSet As Boolean = False
                    Dim clipboardThread As New Thread(
                        Sub()
                            Try
                                For retryClip As Integer = 1 To 5
                                    Try
                                        Clipboard.Clear()
                                        Thread.Sleep(30)
                                        Clipboard.SetText(textToPaste, TextDataFormat.UnicodeText)
                                        clipboardSet = True
                                        Exit For
                                    Catch clipEx As Exception
                                        Debug.WriteLine($"[PasteTextToWindow] Clipboard retry {retryClip}: {clipEx.Message}")
                                        Thread.Sleep(100)
                                    End Try
                                Next
                            Catch ex As Exception
                                Debug.WriteLine($"[PasteTextToWindow] Clipboard thread error: {ex.Message}")
                            End Try
                        End Sub)
                    clipboardThread.SetApartmentState(ApartmentState.STA)
                    clipboardThread.Start()
                    clipboardThread.Join(2000) ' Wait up to 2 seconds

                    If Not clipboardSet Then
                        Debug.WriteLine($"[PasteTextToWindow] Failed to set clipboard on attempt {attempt}")
                        shouldContinue = True
                    End If
                End If

                If Not shouldContinue Then
                    Debug.WriteLine($"[PasteTextToWindow] Clipboard set successfully")

                    ' Step 4: Small delay before pasting
                    Await Task.Delay(50, ct)

                    ' Step 5: Simulate Ctrl+V using InputSimulator
                    Dim sim As New InputSimulatorStandard.InputSimulator()

                    ' Release any stuck modifier keys first
                    sim.Keyboard.KeyUp(InputSimulatorStandard.Native.VirtualKeyCode.CONTROL)
                    sim.Keyboard.KeyUp(InputSimulatorStandard.Native.VirtualKeyCode.SHIFT)
                    sim.Keyboard.KeyUp(InputSimulatorStandard.Native.VirtualKeyCode.MENU)
                    Await Task.Delay(30, ct)

                    ' Perform Ctrl+V
                    sim.Keyboard.ModifiedKeyStroke(
                        InputSimulatorStandard.Native.VirtualKeyCode.CONTROL,
                        InputSimulatorStandard.Native.VirtualKeyCode.VK_V)

                    ' Wait for paste to complete
                    Await Task.Delay(200, ct)

                    Debug.WriteLine($"[PasteTextToWindow] Paste command sent successfully on attempt {attempt}")
                    attemptSucceeded = True
                End If

            Catch ex As OperationCanceledException
                Throw
            Catch ex As Exception
                Debug.WriteLine($"[PasteTextToWindow] Attempt {attempt} failed: {ex.Message}")
                If attempt < MAX_RETRIES Then
                    needsRetryDelay = True
                End If
            End Try

            If attemptSucceeded Then
                Return True
            End If

            ' Add delay before next attempt if we need to continue
            If shouldContinue AndAlso attempt < MAX_RETRIES Then
                Await Task.Delay(200, ct)
            End If
        Next

        Debug.WriteLine("[PasteTextToWindow] All attempts failed")
        Return False
    End Function

    ' Helper to clear clipboard safely on STA thread
    Private Sub ClearClipboardSafe()
        Dim clearThread As New Thread(
            Sub()
                Try
                    Clipboard.Clear()
                Catch
                    ' Ignore
                End Try
            End Sub)
        clearThread.SetApartmentState(ApartmentState.STA)
        clearThread.Start()
        clearThread.Join(1000)
    End Sub

    ' Legacy helper functions - kept for compatibility but no longer used by main paste function
    Private Async Function PasteTextWithRetry(targetWindow As IntPtr, targetControl As IntPtr, textToPaste As String, ct As CancellationToken) As Task(Of Boolean)
        ' Redirect to new implementation
        Return Await PasteTextToWindow(targetWindow, textToPaste, ct)
    End Function

    ' Helper to set clipboard with retry (synchronous to avoid Await in catch)
    Private Function SetClipboardWithRetry(text As String, maxRetries As Integer) As Boolean
        For clipRetry As Integer = 1 To maxRetries
            Try
                Clipboard.Clear()
                Thread.Sleep(50)
                Clipboard.SetText(text, TextDataFormat.UnicodeText)
                Return True
            Catch ex As Exception
                Debug.WriteLine($"[SetClipboardWithRetry] Retry {clipRetry}: {ex.Message}")
                Thread.Sleep(100)
            End Try
        Next
        Return False
    End Function

    ' Helper to restore clipboard without async
    Private Sub RestoreClipboard(backupData As IDataObject)
        Try
            If backupData IsNot Nothing Then
                Clipboard.SetDataObject(backupData, True)
            End If
        Catch
            ' Ignore clipboard restore errors
        End Try
    End Sub

    ' ----------------------------------------------------------------------
    ' v3.1 – WebSearchAndRespondBasedOnPageContent
    '         • Uses a local off‑screen WebView2 so Instance.WebView2Google
    '           can be Nothing and we still work.
    '         • Disposes the control afterwards = no memory leaks.
    '         • Optional CancellationToken for easier reuse.
    ' ----------------------------------------------------------------------


    <CustomFunction(
"Searches Google for a query, opens the first non-Google result, " &
"reads visible text, and extracts info based on user question—single-call if possible, fallback to chunked extraction.",
"WebSearchAndRespondBasedOnPageContent(""HP laptops"", ""emag.ro"", ""List all HP laptops and prices"")")>
    Public Async Function WebSearchAndRespondBasedOnPageContent(
    promptQuery As String,
    siteName As String,
    question As String,
    Optional ct As CancellationToken = Nothing
) As Task(Of String)
        Dim browser As Microsoft.Web.WebView2.WinForms.WebView2 = Nothing
        Try
            Debug.WriteLine($"[WebSearch] Starting with query='{promptQuery}', site='{siteName}'")
            ' 1️⃣ Build search URL
            Dim searchUrl = If(String.IsNullOrWhiteSpace(siteName) OrElse siteName.Contains("google", StringComparison.OrdinalIgnoreCase),
                            $"https://www.google.com/search?q={Uri.EscapeDataString(promptQuery)}",
                            $"https://www.google.com/search?q={Uri.EscapeDataString($"site:{siteName} {promptQuery}")}")
            Debug.WriteLine($"[WebSearch] Search URL: {searchUrl}")

            browser = New Microsoft.Web.WebView2.WinForms.WebView2()
            Shelly.Instance.Controls.Add(browser)
            Await browser.EnsureCoreWebView2Async()

            ' Navigate to search
            browser.CoreWebView2.Navigate(searchUrl)
            If Not Await WaitForNavAsync(browser, ct) Then Return "[ERROR] Could not load search page"
            ' Allow dynamic load & scroll
            Await Task.Delay(3000, ct)
            For i = 1 To 2
                Await browser.CoreWebView2.ExecuteScriptAsync("window.scrollTo(0,document.body.scrollHeight);")
                Await Task.Delay(2000, ct)
            Next

            ' Extract first result URL
            Dim linkJson = Await browser.ExecuteScriptAsync(
            "JSON.stringify(Array.from(document.querySelectorAll('a')).map(a=>a.href));")
            Dim urls = ParseUrlsFromJson(linkJson)
            Dim firstUrl = urls.Select(AddressOf ExtractActualUrl) _
                         .FirstOrDefault(Function(u) Not String.IsNullOrWhiteSpace(u) AndAlso Not u.Contains("google"))
            If firstUrl Is Nothing Then Return "[ERROR] No result URL"
            Debug.WriteLine($"[WebSearch] Target URL: {firstUrl}")

            ' Navigate to target
            browser.CoreWebView2.Navigate(firstUrl)
            If Not Await WaitForNavAsync(browser, ct) Then Return $"[ERROR] Failed to load {firstUrl}"
            ' Dynamic load & scroll again
            Await Task.Delay(3000, ct)
            For i = 1 To 2
                Await browser.CoreWebView2.ExecuteScriptAsync("window.scrollTo(0,document.body.scrollHeight);")
                Await Task.Delay(2000, ct)
            Next

            ' Grab entire visible text
            Dim pageJson = Await browser.CoreWebView2.ExecuteScriptAsync("document.body.innerText;")
            Dim pageText = System.Text.Json.JsonDocument.Parse(pageJson).RootElement.GetString()
            If String.IsNullOrWhiteSpace(pageText) Then Return $"[ERROR] Empty page text from {firstUrl}"
            Debug.WriteLine($"[WebSearch] pageText length={pageText.Length}")

            ' 2️⃣ Attempt single-call extraction
            Const ContextWindow = 128000
            Const MaxCompletion = 16384
            Dim singlePrompt = $"Extract all info to answer: '{question}' from the text below. List each item on its own line. Text:<<<{pageText}>>>"
            Dim singleMsgs = New List(Of Dictionary(Of String, String)) From {
            New Dictionary(Of String, String) From {{"role", "system"}, {"content", "You are an information extractor."}},
            New Dictionary(Of String, String) From {{"role", "user"}, {"content", singlePrompt}}
        }
            Dim needed = AIcall.EstimateTokenCount(singleMsgs) + MaxCompletion
            If needed <= ContextWindow Then
                Debug.WriteLine($"[WebSearch] Single-call OK (promptTokens={needed - MaxCompletion}, maxOut={MaxCompletion})")
                Dim allResp = Await AIcall.CallGPTCore(
                Globals.UserApiKey, Globals.AiModelSelection, singleMsgs, temperature:=0.0, ct:=ct)
                Debug.WriteLine($"[WebSearch] Single-call resp length={allResp.Length}")
                Return $"URL: {firstUrl}{Environment.NewLine}{allResp.Trim()}"
            End If

            ' 3️⃣ Fallback: chunked extraction
            Debug.WriteLine($"[WebSearch] Falling back to chunked extraction (text too large)")
            Const maxTokensPerChunk = 2000
            Const charsPerToken = 4
            Dim maxChars = maxTokensPerChunk * charsPerToken
            Dim extractedAll As New List(Of String)
            Dim pos = 0, idx = 1
            While pos < pageText.Length
                Dim length = Math.Min(maxChars, pageText.Length - pos)
                Dim chunk = pageText.Substring(pos, length)
                Debug.WriteLine($"[WebSearch] Chunk #{idx}, len={length}")
                Dim prompt = $"Extract info to answer: '{question}' from this text chunk. List items each on own line:<<<{chunk}>>>"
                Dim msgs = New List(Of Dictionary(Of String, String)) From {
                New Dictionary(Of String, String) From {{"role", "system"}, {"content", "You are an information extractor."}},
                New Dictionary(Of String, String) From {{"role", "user"}, {"content", prompt}}
            }
                Dim resp = Await AIcall.CallGPTCore(
                Globals.UserApiKey, Globals.AiModelSelection, msgs, temperature:=0.0, ct:=ct)
                Debug.WriteLine($"[WebSearch] Chunk #{idx} resp='{If(resp.Length > 100, resp.Substring(0, 100), resp)}...' ")
                extractedAll.AddRange(resp.Split({Environment.NewLine}, StringSplitOptions.RemoveEmptyEntries))
                pos += length : idx += 1
            End While

            ' Consolidate results (filter out empty/no-info responses)
            Dim filtered = extractedAll _
            .Where(Function(item) _
                Not Regex.IsMatch(item, "(?i)does not contain any information|cannot extract|sorry")) _
            .Distinct() _
            .ToList()
            If filtered.Count = 0 Then
                Return $"URL: {firstUrl}{Environment.NewLine}[ERROR] No relevant items found."
            End If

            ' Optionally reformat via AI for cleaner output
            Dim formatPrompt = $"Format the following items as 'Product — Price' lines only, no extra commentary:" & vbCrLf &
                           String.Join(vbCrLf, filtered)
            Dim formatMessages As New List(Of Dictionary(Of String, String)) From {
            New Dictionary(Of String, String) From {{"role", "system"}, {"content", "You are a formatter: output a clean list of items only."}},
            New Dictionary(Of String, String) From {{"role", "user"}, {"content", formatPrompt}}
        }
            Dim formatted As String = Await AIcall.CallGPTCore(
            Globals.UserApiKey,
            Globals.AiModelSelection,
            formatMessages,
            temperature:=0.0,
            ct:=ct
        )
            Return $"URL: {firstUrl}{Environment.NewLine}{formatted.Trim()}"
        Catch ex As Exception
            Debug.WriteLine($"[WebSearch] Exception: {ex.Message}")
            Return "[ERROR] " & ex.Message
        Finally
            ' Ensure WebView2 is properly disposed
            If browser IsNot Nothing Then
                Try
                    If Shelly.Instance.Controls.Contains(browser) Then
                        Shelly.Instance.Controls.Remove(browser)
                    End If
                    browser.Dispose()
                Catch disposeEx As Exception
                    Debug.WriteLine($"[WebSearch] Error disposing WebView2: {disposeEx.Message}")
                End Try
            End If
        End Try
    End Function



    Private Function ExtractActualUrl(url As String) As String
        Try
            If url.StartsWith("https://www.google.com/url?") Then
                ' Create a Uri object to parse the query parameters.
                Dim uri As New Uri(url)
                Dim queryParams = System.Web.HttpUtility.ParseQueryString(uri.Query)
                Dim actualUrl As String = queryParams("q")
                Return actualUrl
            Else
                Return url
            End If
        Catch ex As Exception
            Debug.WriteLine("ExtractActualUrl error: " & ex.Message)
            Return url
        End Try
    End Function

    Public Function ParseUrlsFromJson(json As String) As List(Of String)
        Try ' Trim any leading/trailing whitespace. json = json.Trim()


            ' If the JSON string is wrapped in extra quotes (common with ExecuteScriptAsync),
            ' remove the first and last characters and unescape any escaped characters.
            If json.StartsWith(""""c) AndAlso json.EndsWith(""""c) Then
                json = json.Substring(1, json.Length - 2)
                json = System.Text.RegularExpressions.Regex.Unescape(json)
            End If

            ' Deserialize the JSON array into a List(Of String)
            Dim urls As List(Of String) = Newtonsoft.Json.JsonConvert.DeserializeObject(Of List(Of String))(json)
            Return urls
        Catch ex As Exception
            Debug.WriteLine("ParseUrlsFromJson error: " & ex.Message)
            ' If deserialization fails, return an empty list.
            Return New List(Of String)()
        End Try
    End Function

    Public Async Function WaitForNavigationAsync(webView As Microsoft.Web.WebView2.WinForms.WebView2) As Task(Of Boolean)
        Dim tcs As New TaskCompletionSource(Of Boolean)()
        Dim handler As EventHandler(Of Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs) = Nothing
        handler = Sub(sender, args)
                      tcs.TrySetResult(args.IsSuccess)
                      RemoveHandler webView.NavigationCompleted, handler
                  End Sub
        AddHandler webView.NavigationCompleted, handler
        Return Await tcs.Task
    End Function

    <CustomFunction("Reads the Copilot conversation from RichTextBox1 (only if in chat mode), processes it in multiple chunks based on the user query, and returns an integrated answer based on the conversation content.", "ReadCopilotConversation(""Provide me a list of websites for each restaurant described"")")>
    Public Async Function ReadCopilotConversation(query As String) As Task(Of String)
        Try
            ' Step 0: Check if Copilot form is open and visible.
            If Copilot.Instance Is Nothing OrElse Not Copilot.Instance.Visible Then
                Return "[ERROR] Copilot window is not open. Please open the Copilot window and try again."
            End If

            ' Step 1: Get conversation text from RichTextBox1.
            Dim convText As String = Copilot.Instance.AiOnePrompt.Text.Trim()
            If String.IsNullOrWhiteSpace(convText) Then
                Return "[ERROR] No conversation found in RichTextBox1."
            End If

            ' Step 2: Split the conversation text into chunks (using a max word count of 800).
            Dim chunks As List(Of String) = FileHandler.SplitTextIntoChunks(convText, 800)
            If chunks.Count = 0 Then
                Return "[ERROR] Unable to split conversation text into chunks."
            End If

            ' Step 3: Process each chunk with the provided query.
            Dim chunkSummaries As New List(Of String)()
            Dim chunkIndex As Integer = 1
            For Each chunk In chunks
                Dim result As Tuple(Of String, String) = Await FileProcessChunk(chunk, query)
                ' Use only the answer portion (result.Item1) for summarization.
                chunkSummaries.Add($"[Chunk {chunkIndex}]: {result.Item1}")
                chunkIndex += 1
            Next

            Dim overallSummary As String = String.Join(Environment.NewLine & Environment.NewLine, chunkSummaries)
            Debug.WriteLine("ReadCopilotConversation - Combined Chunk Summaries: " & overallSummary)

            ' Step 4: Build a final prompt that integrates all chunk responses.
            Dim finalPrompt As String = $"Based on the following Copilot conversation summaries:{Environment.NewLine}{overallSummary}{Environment.NewLine}{Environment.NewLine}" &
                                          $"Answer the following question concisely, ensuring that you include all relevant details: {query}"

            Dim messages As New List(Of Dictionary(Of String, String)) From {
                New Dictionary(Of String, String) From {{"role", "system"}, {"content", "You are an assistant that integrates conversation summaries from Copilot to provide a comprehensive answer."}},
                New Dictionary(Of String, String) From {{"role", "user"}, {"content", finalPrompt}}
            }

            ' Step 5: Call the GPT assistant via CallGPTCore.
            Dim finalAnswer As String = Await AIcall.CallGPTCore(Config.OpenAiApiKey, Config.AssistantId, messages, 0.7, CancellationToken.None)
            finalAnswer = RemoveCustomFunctionCodeBlocks(finalAnswer)

            ' Optionally update conversation history.

            Return finalAnswer
        Catch ex As Exception
            Return "[ERROR] " & ex.Message
        End Try
    End Function


    <CustomFunction(
    "Searches file(s) or folder(s) for a specific string (always case-insensitive). " &
    "Example usage: SearchForTextInsideFiles(""D:\Files;D:\Other\Stuff.docx"", ""MySearchWord"")",
    "SearchForTextInsideFiles(""C:\Shelly"", ""hello world"")"
)>
    Public Async Function SearchForTextInsideFiles(paths As String, searchWord As String) As Task(Of String)
        Try
            ' -- Build one inline script that uses -imatch (case-insensitive). --
            ' -- We embed the values for 'paths' & 'searchWord' directly into the script. --
            ' -- No param(...) block, so no parsing conflicts.

            Dim script As String = $"
$($ErrorActionPreference = 'Stop')

function Read-FileContent {{
    param (
        [string]$filePath
    )
    try {{
        if ($filePath.EndsWith("".txt"") -or $filePath.EndsWith("".js"") -or $filePath.EndsWith("".html"")) {{
            return Get-Content -Path $filePath -ErrorAction Stop
        }}
        elseif ($filePath.EndsWith("".docx"")) {{
            $word = New-Object -ComObject Word.Application
            $doc = $word.Documents.Open($filePath, [ref]0, [ref]1)
            $text = $doc.Content.Text
            $doc.Close()
            $word.Quit()
            return $text
        }}
        elseif ($filePath.EndsWith("".xlsx"")) {{
            $excel = New-Object -ComObject Excel.Application
            $workbook = $excel.Workbooks.Open($filePath)
            $text = """"
            foreach ($sheet in $workbook.Sheets) {{
                $text += $sheet.UsedRange.Value2 | Out-String
            }}
            $workbook.Close()
            $excel.Quit()
            return $text
        }}
        else {{
            # fallback: read file as binary
            $bytes = [System.IO.File]::ReadAllBytes($filePath)
            return [System.Text.Encoding]::UTF8.GetString($bytes)
        }}
    }} catch {{
        return $null
    }}
}}

function Get-Files($p) {{
    if (Test-Path $p) {{
        $item = Get-Item $p
        if ($item.PSIsContainer) {{
            return Get-ChildItem -Path $p -Recurse -File
        }} else {{
            return $item
        }}
    }} else {{
        return $null
    }}
}}

$foundFiles = @()
# Split paths by semicolon
$pathsArray = '{paths.Replace("'", "''")}' -split ';'
$searchText = '{searchWord.Replace("'", "''")}'

foreach ($p in $pathsArray) {{
    $p = $p.Trim()
    if ($p) {{
        $files = Get-Files($p)
        if ($files) {{
            foreach ($file in $files) {{
                $content = Read-FileContent -filePath $file.FullName
                if ($content -and ($content -imatch [regex]::Escape($searchText))) {{
                    $foundFiles += $file.FullName
                }}
            }}
        }}
    }}
}}

$foundFiles
"
            ' Execute the script
            Dim result = Await Shelly.Instance.ExecutePowerShellScriptAsync(script, Threading.CancellationToken.None)

            If result.Item1 Then
                ' success: check the output
                Dim foundList As String = result.Item2.Trim()
                If String.IsNullOrEmpty(foundList) Then
                    Return "No files matched."
                Else
                    Return "Matched files:" & Environment.NewLine & foundList
                End If
            Else
                ' error
                Return "[ERROR] PowerShell script failed: " & result.Item2
            End If
        Catch ex As Exception
            Return "[ERROR] " & ex.Message
        End Try
    End Function

    ' -------------------------- new web -------------------------------

    <CustomFunctionAttribute(
  "Directly accesses a user-provided URL, retrieves its visible text, " &
  "then extracts information to answer the user's query—single-call if possible, fallback to chunks.",
  "ReadWebPageAndRespondBasedOnPageContent(""https://www.example.com/page.html"", ""List all product names and prices"")")>
    Public Async Function ReadWebPageAndRespondBasedOnPageContent(
    url As String,
    query As String,
    Optional ct As CancellationToken = Nothing
) As Task(Of String)
        Dim browser As Microsoft.Web.WebView2.WinForms.WebView2 = Nothing
        Try
            ' 1️⃣ Validate URL
            If Not Uri.IsWellFormedUriString(url, UriKind.Absolute) Then
                Return $"[ERROR] Invalid URL: '{url}'"
            End If

            ' 2️⃣ Initialize off-screen WebView2
            browser = New Microsoft.Web.WebView2.WinForms.WebView2()
            Shelly.Instance.Controls.Add(browser)
            Await browser.EnsureCoreWebView2Async()

            ' 3️⃣ Navigate to the page
            browser.CoreWebView2.Navigate(url)
            If Not Await WaitForNavigationAsync(browser) Then
                Return $"[ERROR] Page failed to load: {url}"
            End If

            ' Give dynamic scripts time to run and lazy-load content
            Await Task.Delay(TimeSpan.FromSeconds(3), ct)
            For i As Integer = 1 To 2
                Await browser.CoreWebView2.ExecuteScriptAsync("window.scrollTo(0, document.body.scrollHeight);")
                Await Task.Delay(TimeSpan.FromSeconds(2), ct)
            Next

            ' 4️⃣ Extract visible text
            Dim pageTextJson As String = Await browser.CoreWebView2.ExecuteScriptAsync("document.body.innerText;")
            Dim pageText As String = System.Text.Json.JsonDocument.Parse(pageTextJson).RootElement.GetString()
            If String.IsNullOrWhiteSpace(pageText) Then
                Return $"[ERROR] Unable to retrieve page content from {url}"
            End If

            ' 5️⃣ Attempt single-call extraction
            Const ContextWindow As Integer = 128000
            Const MaxCompletion As Integer = 16384
            Dim singlePrompt As String =
            $"Extract information to answer: '{query}' from the text below. " &
            $"List each item on its own line. Text:<<<{pageText}>>>"
            Dim singleMsgs = New List(Of Dictionary(Of String, String)) From {
            New Dictionary(Of String, String) From {{"role", "system"}, {"content", "You are an information extractor."}},
            New Dictionary(Of String, String) From {{"role", "user"}, {"content", singlePrompt}}
        }
            Dim neededTokens As Integer = AIcall.EstimateTokenCount(singleMsgs) + MaxCompletion
            If neededTokens <= ContextWindow Then
                Dim fullResp As String = Await AIcall.CallGPTCore(
                Globals.UserApiKey,
                Globals.AiModelSelection,
                singleMsgs,
                temperature:=0.0,
                ct:=ct
            )
                Return $"URL: {url}{Environment.NewLine}{fullResp.Trim()}"
            End If

            ' 6️⃣ Fallback: chunked extraction
            Const maxTokensPerChunk As Integer = 800
            Const charsPerToken As Integer = 4
            Dim maxChars As Integer = maxTokensPerChunk * charsPerToken
            Dim extracted As New List(Of String)
            Dim pos As Integer = 0, idx As Integer = 1

            While pos < pageText.Length
                Dim length = Math.Min(maxChars, pageText.Length - pos)
                Dim chunk = pageText.Substring(pos, length)
                Dim promptChunk As String =
                $"Extract information to answer: '{query}' from this text chunk. " &
                $"List each item on its own line:<<<{chunk}>>>"
                Dim msgsChunk = New List(Of Dictionary(Of String, String)) From {
                New Dictionary(Of String, String) From {{"role", "system"}, {"content", "You are an information extractor."}},
                New Dictionary(Of String, String) From {{"role", "user"}, {"content", promptChunk}}
            }
                Dim respChunk As String = Await AIcall.CallGPTCore(
                Globals.UserApiKey,
                Globals.AiModelSelection,
                msgsChunk,
                temperature:=0.0,
                ct:=ct
            )
                extracted.AddRange(respChunk.Split({Environment.NewLine}, StringSplitOptions.RemoveEmptyEntries))
                pos += length
                idx += 1
            End While

            ' 7️⃣ Filter out non-informative replies and dedupe
            Dim filtered = extracted _
            .Where(Function(item) Not Regex.IsMatch(item, "(?i)does not contain any information|cannot extract|sorry")) _
            .Distinct() _
            .ToList()
            If filtered.Count = 0 Then
                Return $"URL: {url}{Environment.NewLine}[ERROR] No relevant items found."
            End If

            ' 8️⃣ Final formatting
            Dim formatPrompt As String =
            "Format these items cleanly, one item per line with no extra commentary:" & Environment.NewLine &
            String.Join(Environment.NewLine, filtered)
            Dim formatMessages As New List(Of Dictionary(Of String, String)) From {
            New Dictionary(Of String, String) From {{"role", "system"}, {"content", "You format lists: one item per line, no commentary."}},
            New Dictionary(Of String, String) From {{"role", "user"}, {"content", formatPrompt}}
        }
            Dim formatted As String = Await AIcall.CallGPTCore(
            Globals.UserApiKey,
            Globals.AiModelSelection,
            formatMessages,
            temperature:=0.0,
            ct:=ct
        )

            Return $"URL: {url}{Environment.NewLine}{formatted.Trim()}"
        Catch ex As Exception
            Return "[ERROR] " & ex.Message
        Finally
            ' Ensure WebView2 is properly disposed
            If browser IsNot Nothing Then
                Try
                    If Shelly.Instance.Controls.Contains(browser) Then
                        Shelly.Instance.Controls.Remove(browser)
                    End If
                    browser.Dispose()
                Catch disposeEx As Exception
                    Debug.WriteLine($"[ReadWebPage] Error disposing WebView2: {disposeEx.Message}")
                End Try
            End If
        End Try
    End Function

    Public Async Function UpdateFileByChunks(
    filePath As String,
    updateInstruction As String,
    Optional chunkTokenOverride As Integer = -1,
    Optional ct As CancellationToken = Nothing
) As Task(Of String)

        ' Use active cancellation token if none provided
        If ct = Nothing OrElse ct = CancellationToken.None Then
            ct = FileHandler.ActiveCancellationToken
        End If

        Try
            ' Check for cancellation before starting
            ct.ThrowIfCancellationRequested()

            ' 1️⃣ Load and cache full file text
            Dim fullText As String = Nothing
            If Not Globals.FileContents.TryGetValue(filePath, fullText) Then
                fullText = File.ReadAllText(filePath, Encoding.UTF8)
            End If
            Globals.FileContents(filePath) = fullText

            ' 2️⃣ Estimate total tokens for full text
            Dim contentMessage = New Dictionary(Of String, String) From {
                {"role", "user"},
                {"content", fullText}
            }
            Dim totalTokens As Integer = AIcall.EstimateTokenCount(
                New List(Of Dictionary(Of String, String)) From {contentMessage}
            )

            Const ContextWindow As Integer = 128000  ' max GPT context tokens
            Const MaxCompletion As Integer = 16000  ' max tokens per reply

            ' 3️⃣ Determine maximum input tokens for each chunk (up to 128K)
            Dim safeInputTokens As Integer = ContextWindow
            If chunkTokenOverride > 0 Then
                safeInputTokens = Math.Min(chunkTokenOverride, ContextWindow)
            End If
            Const charsPerToken As Integer = 4
            Dim maxCharsPerChunk As Integer = safeInputTokens * charsPerToken
            If maxCharsPerChunk < 200 Then maxCharsPerChunk = 200

            ' 4️⃣ Split fullText into chunks (if needed), with 1-line overlap
            Dim chunks As New List(Of String)
            If totalTokens <= ContextWindow Then
                chunks.Add(fullText)
            Else
                Dim lines = fullText.Replace(vbCr, "").Split(vbLf)
                Const overlapLines As Integer = 1
                Dim startIdx As Integer = 0
                While startIdx < lines.Length
                    Dim sb As New StringBuilder()
                    Dim i As Integer = startIdx
                    ' include overlap from previous chunk
                    If startIdx > 0 Then
                        For j = startIdx - overlapLines To startIdx - 1
                            sb.AppendLine(lines(j))
                        Next
                    End If
                    ' accumulate lines until maxCharsPerChunk is reached
                    While i < lines.Length AndAlso sb.Length + lines(i).Length + 1 <= maxCharsPerChunk
                        sb.AppendLine(lines(i))
                        i += 1
                    End While
                    chunks.Add(sb.ToString())
                    startIdx = i
                End While
            End If

            ' 5️⃣ Process each chunk with the AI (handling continuation if needed)
            Dim updatedChunks As New List(Of String)
            For idx = 0 To chunks.Count - 1
                ' Check for cancellation at each chunk
                ct.ThrowIfCancellationRequested()

                Dim chunkText = chunks(idx)
                Dim fullResult As New StringBuilder()
                Dim isFirst As Boolean = True
                Dim maxContinuations As Integer = 10 ' Safety limit for continuation loops

                Do
                    ct.ThrowIfCancellationRequested()
                    maxContinuations -= 1
                    If maxContinuations <= 0 Then
                        Debug.WriteLine("[UpdateFileByChunks] Max continuations reached, breaking loop")
                        Exit Do
                    End If

                    ' Build prompt: initial or continuation
                    Dim messages As New List(Of Dictionary(Of String, String))
                    ' System role (optional, for better editing style)
                    messages.Add(New Dictionary(Of String, String) From {
                        {"role", "system"},
                        {"content", "You are a skilled editor and programmer."}
                    })

                    If isFirst Then
                        ' Initial request with full instruction and chunk content
                        Dim promptBuilder As New StringBuilder()
                        promptBuilder.AppendLine("Below is a portion of a file. Update the content according to the following instruction:")
                        promptBuilder.AppendLine($"Instruction: {updateInstruction}")
                        promptBuilder.AppendLine("Text:")
                        promptBuilder.Append(chunkText)
                        promptBuilder.AppendLine()
                        promptBuilder.AppendLine("Output only the updated text exactly as it should appear, with no additional commentary or markdown.")
                        messages.Add(New Dictionary(Of String, String) From {
                            {"role", "user"},
                            {"content", promptBuilder.ToString()}
                        })
                    Else
                        ' Continuation request (no repeating)
                        messages.Add(New Dictionary(Of String, String) From {
                            {"role", "user"},
                            {"content", "Continue updating the rest of this chunk, appending only new content without repeating prior content."}
                        })
                    End If

                    ' Call the GPT model with cancellation support
                    Dim resp As String = Await AIcall.CallGPTCore(
                        Globals.UserApiKey,
                        Globals.AiModelSelection,
                        messages,
                        temperature:=0.0,
                        ct:=ct
                    )
                    ' Remove any accidental code fences from response
                    resp = Regex.Replace(resp, "^\s*`{3,}\s*|\s*`{3,}\s*$", "", RegexOptions.Multiline).TrimEnd()
                    fullResult.Append(resp)

                    ' Check if the response was truncated (by token count)
                    Dim respTokens As Integer = AIcall.EstimateTokenCount(
                        New List(Of Dictionary(Of String, String)) From {
                            New Dictionary(Of String, String) From {{"role", "assistant"}, {"content", resp}}
                        }
                    )
                    If respTokens < MaxCompletion Then Exit Do
                    isFirst = False
                Loop

                updatedChunks.Add(fullResult.ToString())
            Next

            ' 6️⃣ Reassemble updated chunks, trimming the overlapping first line of each subsequent chunk
            Dim finalSb As New StringBuilder()
            For idx = 0 To updatedChunks.Count - 1
                Dim updLines = updatedChunks(idx).Replace(vbCr, "").Split(vbLf)
                If idx = 0 Then
                    ' Include all lines from the first chunk
                    For Each line In updLines
                        finalSb.AppendLine(line)
                    Next
                Else
                    ' Skip the first line of this chunk (overlap from previous)
                    For Each line In updLines.Skip(1)
                        finalSb.AppendLine(line)
                    Next
                End If
            Next

            ' Finalize content and write to disk
            Dim finalContent = finalSb.ToString().TrimEnd() & vbCrLf
            File.WriteAllText(filePath, finalContent, Encoding.UTF8)
            Globals.FileContents(filePath) = finalContent

            Return finalContent

        Catch ex As OperationCanceledException
            Return "[Cancelled] File update was cancelled."
        Catch ex As Exception
            Return "[ERROR] " & ex.Message
        End Try
    End Function

    Private Function StripCodeFences(raw As String) As String
        If String.IsNullOrWhiteSpace(raw) Then Return String.Empty
        Dim text = raw.Trim()

        If text.StartsWith("```", StringComparison.Ordinal) Then
            Dim newlineIndex = text.IndexOfAny({ChrW(10), ChrW(13)})
            If newlineIndex >= 0 Then
                Dim nextIndex = newlineIndex + 1
                While nextIndex < text.Length AndAlso (text(nextIndex) = ChrW(10) OrElse text(nextIndex) = ChrW(13))
                    nextIndex += 1
                End While
                text = text.Substring(Math.Min(nextIndex, text.Length))
            Else
                text = text.Substring(3)
            End If
        End If

        If text.EndsWith("```", StringComparison.Ordinal) Then
            Dim lastFence = text.LastIndexOf("```", StringComparison.Ordinal)
            If lastFence >= 0 Then
                text = text.Substring(0, lastFence)
            End If
        End If

        Return text.Trim()
    End Function

End Module


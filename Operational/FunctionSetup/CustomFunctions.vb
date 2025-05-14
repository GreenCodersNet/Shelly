' ###  CustomFunctions.vb - v1.0.1 ### 

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
    Public Const WmKeyUp As UInteger = &H101
    Public Const VkReturn As Integer = 13

    ' Constants for key events
    Private Const KEYEVENTF_EXTENDEDKEY As UInteger = &H1
    Private Const KEYEVENTF_KEYUP As UInteger = &H2

    ' Sends a media key press using the provided key name.

    Public Async Sub SendMediaKey(keyName As String)
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
            Shelly.Instance.LabelStatusUpdate.Text = "Unknown key: " & keyName & " Error"
            Await Shelly.ExecuteScriptSafeAsync("setColorDefault();")
        End If
    End Sub


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

    Write-Host 'Application ''$appName'' not found in Start Menu.'
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
                .imagePrompt = $"{imagePrompt} {style}".Trim(),
                .size = "1792x1024",
                .folderPath = folderPath,
                .imageName = finalName
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

    ' Generates a large file by appending multiple content chunks generated by AI.
    ' Updated GenerateLargeFileWithPowerShell function
    <CustomFunction(
      "Generates arbitrarily large files (Word or any text-based) by asking the AI in multiple chunks.",
      "GenerateLargeFileWithTextOrCode(""Create a 10-page essay on climate change."", ""C:\Demo\Essay.docx"", 10)"
    )>
    Public Async Function GenerateLargeFileWithTextOrCode(
      topic As String,
      outputPath As String,
      Optional totalChunks As Integer = 5
    ) As Task(Of String)

        Try
            ' Delegate to our helper
            Dim result = Await generateFiles.GenerateLargeFileWithTextOrCode(
              filePath:=outputPath,
              userPrompt:=topic,
              totalChunks:=totalChunks,
              ct:=CancellationToken.None
            )

            If result.Item1 Then
                Return result.Item2
            Else
                Return "[ERROR] " & result.Item2
            End If

        Catch ex As Exception
            Return "[ERROR] Exception in GenerateLargeFileWithTextOrCode: " & ex.Message
        End Try

    End Function

    Public Async Function ReadFileAndAnswer(
        ByVal filePaths As String,
        ByVal query As String
    ) As Task(Of String)

        Try
            Dim paths = filePaths.Split(","c) _
                             .Select(Function(p) p.Trim()) _
                             .ToArray()
            Dim combinedText As New StringBuilder()

            For Each path In paths
                If Not File.Exists(path) Then
                    Throw New FileNotFoundException($"File not found: {path}")
                End If

                ' <-- THIS is the fix:
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
                {"content", "You are a helpful assistant. Read the provided text and answer the user’s question based on its content."}
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

            ' 5) Single API call
            Dim answer = Await AIcall.CallGPTCore(
            apiKey:=Config.OpenAiApiKey,
            model:=Globals.AiModelSelection,
            messages:=messages,
            temperature:=0.0,
            ct:=CancellationToken.None
        )

            Return answer.Trim()
        Catch ex As Exception
            Return "[ERROR] " & ex.Message
        End Try
    End Function


    ' This function types text directly into the active file or window by pasting its full content.
    Public Async Function WriteInsideFileOrWindow(topic As String, Optional totalChunks As Integer = 1) As Task(Of String)
        ' 1. Capture the original window and control
        originalWindow = FileHandler.GetForegroundWindow()
        originalControl = FileHandler.GetFocusedControl(originalWindow)
        Debug.WriteLine("----- WriteInsideFileOrWindow -----")
        Debug.WriteLine($"Captured window handle = {originalWindow}, control handle = {originalControl}")
        Debug.WriteLine($"Topic = {topic}, totalChunks = {totalChunks}")

        ' 2. Wait a few seconds for the user to ensure the caret is correctly placed.
        Await Task.Delay(5000)

        If originalWindow = IntPtr.Zero OrElse originalControl = IntPtr.Zero Then
            Debug.WriteLine("No valid window/control detected.")
            Return String.Empty
        End If

        For i As Integer = 1 To totalChunks
            If typingStopped Then Exit For

            Dim promptText As String = $"
                You are writing part #{i} of {totalChunks} for: {topic}.
                Generate the exact content that should be inserted, preserving all spaces, line breaks, tabs, and formatting exactly as it should appear (unless instructed otherwise).
                ***Do not include any code block markers or extra commentary or information.***."
            Dim messages As New List(Of Dictionary(Of String, String)) From {
            New Dictionary(Of String, String) From {{"role", "system"}, {"content", "You are a content generator focused on producing exact text for insertion."}},
            New Dictionary(Of String, String) From {{"role", "user"}, {"content", promptText}}
        }
            Dim chunkText As String = Await CallGPTCore(Config.OpenAiApiKey, Config.AiModel, messages, Globals.temperature, CancellationToken.None)

            chunkText = RemoveCustomFunctionCodeBlocks(chunkText)
            If String.IsNullOrEmpty(chunkText) Then
                Debug.WriteLine($"Failed to generate content for chunk #{i}.")
                Continue For
            End If

            Debug.WriteLine($"----- AI Generated Text for Chunk {i} -----")
            Debug.WriteLine(chunkText)
            Debug.WriteLine("----- END OF TEXT -----")
            ' Insert the chunk using clipboard paste.
            Await FileHandler.PasteTextBulk(originalWindow, originalControl, chunkText)
        Next

        ' OPTIONAL: Clear the clipboard if needed to fully release our lock.
        Clipboard.Clear()

        Return String.Empty
    End Function

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
        Try
            Debug.WriteLine($"[WebSearch] Starting with query='{promptQuery}', site='{siteName}'")
            ' 1️⃣ Build search URL
            Dim searchUrl = If(String.IsNullOrWhiteSpace(siteName) OrElse siteName.Contains("google", StringComparison.OrdinalIgnoreCase),
                            $"https://www.google.com/search?q={Uri.EscapeDataString(promptQuery)}",
                            $"https://www.google.com/search?q={Uri.EscapeDataString($"site:{siteName} {promptQuery}")}")
            Debug.WriteLine($"[WebSearch] Search URL: {searchUrl}")

            Using browser As New Microsoft.Web.WebView2.WinForms.WebView2()
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
                Dim formatMsgs As New List(Of Dictionary(Of String, String)) From {
                New Dictionary(Of String, String) From {{"role", "system"}, {"content", "You are a formatter: output a clean list of items only."}},
                New Dictionary(Of String, String) From {{"role", "user"}, {"content", formatPrompt}}
            }
                Dim formatted As String = Await AIcall.CallGPTCore(
                Globals.UserApiKey,
                Globals.AiModelSelection,
                formatMsgs,
                temperature:=0.0,
                ct:=ct
            )
                Return $"URL: {firstUrl}{Environment.NewLine}{formatted.Trim()}"
            End Using
        Catch ex As Exception
            Debug.WriteLine($"[WebSearch] Exception: {ex.Message}")
            Return "[ERROR] " & ex.Message
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
        Try
            ' 1️⃣ Validate URL
            If Not Uri.IsWellFormedUriString(url, UriKind.Absolute) Then
                Return $"[ERROR] Invalid URL: '{url}'"
            End If

            ' 2️⃣ Initialize off-screen WebView2
            Using browser As New Microsoft.Web.WebView2.WinForms.WebView2()
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
                "Format these items cleanly, one per line with no extra commentary:" & Environment.NewLine &
                String.Join(Environment.NewLine, filtered)
                Dim formatMsgs As New List(Of Dictionary(Of String, String)) From {
                New Dictionary(Of String, String) From {{"role", "system"}, {"content", "You format lists: one item per line, no commentary."}},
                New Dictionary(Of String, String) From {{"role", "user"}, {"content", formatPrompt}}
            }
                Dim formatted As String = Await AIcall.CallGPTCore(
                Globals.UserApiKey,
                Globals.AiModelSelection,
                formatMsgs,
                temperature:=0.0,
                ct:=ct
            )

                Return $"URL: {url}{Environment.NewLine}{formatted.Trim()}"
            End Using

        Catch ex As Exception
            Return "[ERROR] " & ex.Message
        End Try
    End Function

    Public Async Function UpdateFileByChunks(
        filePath As String,
        updateInstruction As String,
        Optional chunkTokenOverride As Integer = -1
    ) As Task(Of String)

        ' ─── 1️⃣ Load and cache full file text ───
        Dim cachedText As String = Nothing
        If Not Globals.FileContents.TryGetValue(filePath, cachedText) Then
            cachedText = File.ReadAllText(filePath)
        End If
        Dim fullText As String = cachedText
        Globals.FileContents(filePath) = fullText

        ' ─── 2️⃣ Estimate total tokens for fullText ───
        Dim contentMessage = New Dictionary(Of String, String) From {
            {"role", "user"},
            {"content", fullText}
        }
        Dim totalTokens As Integer = AIcall.EstimateTokenCount(New List(Of Dictionary(Of String, String)) From {contentMessage})

        Const ContextWindow As Integer = 128000
        Const MaxCompletion As Integer = 16384

        ' ─── 3️⃣ Determine safe input chunk size ───
        Dim safeInputTokens As Integer = CInt(ContextWindow * 0.65)
        If chunkTokenOverride > 0 Then
            safeInputTokens = Math.Min(chunkTokenOverride, safeInputTokens)
        End If
        Const charsPerToken As Integer = 4
        Dim maxCharsPerChunk As Integer = safeInputTokens * charsPerToken
        If maxCharsPerChunk < 200 Then maxCharsPerChunk = 200

        ' ─── 4️⃣ Split fullText into chunks if needed ───
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
                ' include overlap from previous
                If startIdx > 0 Then
                    For j = startIdx - overlapLines To startIdx - 1
                        sb.AppendLine(lines(j))
                    Next
                End If
                ' accumulate until maxCharsPerChunk
                While i < lines.Length AndAlso sb.Length + lines(i).Length + 1 <= maxCharsPerChunk
                    sb.AppendLine(lines(i))
                    i += 1
                End While
                chunks.Add(sb.ToString())
                startIdx = i
            End While
        End If

        ' ─── 5️⃣ Process each chunk with continuation ───
        Dim updatedChunks As New List(Of String)
        For idx = 0 To chunks.Count - 1
            Dim chunkText = chunks(idx)
            Dim fullResult As New StringBuilder()
            Dim isFirst As Boolean = True

            Do
                ' Build messages: initial or continue
                Dim messages As List(Of Dictionary(Of String, String))
                If isFirst Then
                    ' initial request for this chunk
                    Dim promptBuilder As New StringBuilder()
                    promptBuilder.AppendLine($"Instruction: {updateInstruction}")
                    promptBuilder.AppendLine("---")
                    promptBuilder.AppendLine($"Chunk {idx + 1}/{chunks.Count}:")
                    promptBuilder.AppendLine("````")
                    promptBuilder.Append(chunkText)
                    promptBuilder.AppendLine("````")
                    promptBuilder.AppendLine("---")
                    promptBuilder.AppendLine("Respond with only the fully updated content for this chunk; do not include fences.")
                    messages = New List(Of Dictionary(Of String, String)) From {
                        New Dictionary(Of String, String) From {{"role", "user"}, {"content", promptBuilder.ToString()}}
                    }
                Else
                    ' continuation request
                    messages = New List(Of Dictionary(Of String, String)) From {
                        New Dictionary(Of String, String) From {{"role", "user"}, {"content", "Continue updating the rest of this chunk, appending only new content without repeating prior content."}}
                    }
                End If

                ' Call AI
                Dim resp As String = Await AIcall.CallGPTCore(
                    Globals.UserApiKey,
                    Globals.AiModelSelection,
                    messages,
                    temperature:=0.0,
                    ct:=CancellationToken.None
                )
                ' strip fences
                resp = Regex.Replace(resp, "^\s*````\s*|\s*````\s*$", "", RegexOptions.Multiline).TrimEnd()
                fullResult.Append(resp)

                ' check if model likely truncated at MaxCompletion
                Dim respTokens As Integer = AIcall.EstimateTokenCount(New List(Of Dictionary(Of String, String)) From {
                    New Dictionary(Of String, String) From {{"role", "assistant"}, {"content", resp}}
                })
                If respTokens < MaxCompletion Then Exit Do
                isFirst = False
            Loop

            updatedChunks.Add(fullResult.ToString())
        Next

        ' ─── 6️⃣ Reassemble updated chunks ───
        Dim finalSb As New StringBuilder()
        For idx = 0 To updatedChunks.Count - 1
            Dim updLines = updatedChunks(idx).Replace(vbCr, "").Split(vbLf)
            If idx = 0 Then
                For Each line In updLines
                    finalSb.AppendLine(line)
                Next
            Else
                For Each line In updLines.Skip(1)
                    finalSb.AppendLine(line)
                Next
            End If
        Next

        Dim finalContent = finalSb.ToString().TrimEnd() & vbCrLf
        File.WriteAllText(filePath, finalContent, Encoding.UTF8)
        Globals.FileContents(filePath) = finalContent

        Return finalContent
    End Function

    ' ================== BAT ===========================

    <CustomFunction(
        "Generates a batch file and a PowerShell script.  
        The .bat calls the .ps1 with Bypass execution policy.  
        The .ps1 must wrap logic in Try/Catch and, on any path, end with a ReadKey pause so the window stays open.",
        "GenerateBatchAndPs1File(""D:\Demo"", ""Check my last 20 Outlook emails and print sender address and subject"")"
    )>
    Public Async Function GenerateBatchAndPs1File(
        outputFolder As String,
        userQuery As String
    ) As Task(Of String)

        Try
            ' 1️⃣ Ensure output folder exists
            If Not Directory.Exists(outputFolder) Then
                Directory.CreateDirectory(outputFolder)
            End If

            ' 2️⃣ Build the AI prompt
            Dim prompt As New Text.StringBuilder()
            prompt.AppendLine("You are a PowerShell and batch-file expert.")
            prompt.AppendLine("Create two files in the target folder:")
            prompt.AppendLine("  1) A .ps1 script that performs the following task:")
            prompt.AppendLine($"     {userQuery}")
            prompt.AppendLine("     - Wrap the entire logic in Try/Catch.")
            prompt.AppendLine("     - In the Catch block, write the error to host.")
            prompt.AppendLine("     - At the very end (both in Try and Catch), display a prompt:")
            prompt.AppendLine("         Write-Host 'Press any key to exit…'")
            prompt.AppendLine("         [void][System.Console]::ReadKey()")
            prompt.AppendLine()
            prompt.AppendLine("  2) A .bat file that calls the .ps1 with:")
            prompt.AppendLine("     powershell -ExecutionPolicy Bypass -File script.ps1")
            prompt.AppendLine()
            prompt.AppendLine("Respond with exactly two fenced code blocks:")
            prompt.AppendLine("```bat")
            prompt.AppendLine("...content of the .bat file here...")
            prompt.AppendLine("```")
            prompt.AppendLine("```powershell")
            prompt.AppendLine("...content of the .ps1 file here...")
            prompt.AppendLine("```")

            ' 3️⃣ Call the model
            Dim messages = New List(Of Dictionary(Of String, String)) From {
                New Dictionary(Of String, String) From {
                    {"role", "system"},
                    {"content", "You are an expert shell‐and‐powershell scripter."}
                },
                New Dictionary(Of String, String) From {
                    {"role", "user"},
                    {"content", prompt.ToString()}
                }
            }
            Dim aiResponse As String = Await AIcall.CallGPTCore(
                apiKey:=Globals.UserApiKey,
                model:=Globals.AiModelSelection,
                messages:=messages,
                temperature:=0.0,
                ct:=Threading.CancellationToken.None
            )

            ' 4️⃣ Extract and write each block
            Dim batMatch = Regex.Match(aiResponse, "```bat\s*(.+?)\s*```", RegexOptions.Singleline)
            Dim psMatch = Regex.Match(aiResponse, "```powershell\s*(.+?)\s*```", RegexOptions.Singleline)
            If Not batMatch.Success OrElse Not psMatch.Success Then
                Return "[ERROR] Could not parse AI response into .bat and .ps1 blocks."
            End If

            Dim batContent = batMatch.Groups(1).Value.Trim().Replace(vbLf, vbCrLf)
            Dim psContent = psMatch.Groups(1).Value.Trim().Replace(vbLf, vbCrLf)

            Dim batPath = Path.Combine(outputFolder, "script.bat")
            Dim ps1Path = Path.Combine(outputFolder, "script.ps1")

            File.WriteAllText(batPath, batContent, System.Text.Encoding.UTF8)
            File.WriteAllText(ps1Path, psContent, System.Text.Encoding.UTF8)

            Return $"Batch and PowerShell scripts written to:{vbCrLf}{batPath}{vbCrLf}{ps1Path}"
        Catch ex As Exception
            Return $"[ERROR] GenerateBatchAndPs1File failed: {ex.Message}"
        End Try

    End Function


End Module


'==============================================
' CUSTOM FUNCTIONS MODULE - PART 2
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


Public Module CustomFunctions2

    ''' <summary>
    ''' Reads one or more files and answers a question about their content.
    ''' 
    ''' WHEN LocalAIUseSummarization IS ON:
    '''   1. LocalAI processes each chunk (extracts relevant info)
    '''   2. All chunk summaries are collected in memory
    '''   3. Cloud AI (OpenAI) generates the FINAL comprehensive response
    '''   -> NO TOKEN LIMITS on final response!
    ''' 
    ''' WHEN LocalAIUseSummarization IS OFF:
    '''   -> Cloud AI handles everything (existing behavior)
    ''' </summary>
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

            Dim fullText = combinedText.ToString()
            
            ' === LOCALAI MULTI-BATCH APPROACH ===
            ' LocalAI processes chunks -> Cloud AI generates final response
            If Globals.IsLocalAISummarizationEnabled() Then
                Debug.WriteLine("[ReadFileAndAnswer] === LOCALAI MULTI-BATCH MODE ===")
                Debug.WriteLine($"[ReadFileAndAnswer] Total content length: {fullText.Length} chars")
                Debug.WriteLine($"[ReadFileAndAnswer] Query: {query}")
                
                ' Step 1: Split into chunks
                Dim maxWordsPerChunk As Integer = 500  ' ~2000 chars per chunk
                Dim chunks As List(Of String) = FileHandler.SplitTextIntoChunks(fullText, maxWordsPerChunk)
                
                Debug.WriteLine($"[ReadFileAndAnswer] Split into {chunks.Count} chunks for LocalAI processing")
                
                ' Step 2: Process each chunk with LocalAI (extract relevant info)
                Dim chunkSummaries As New List(Of String)()
                
                For i As Integer = 0 To chunks.Count - 1
                    ct.ThrowIfCancellationRequested()
                    
                    Shelly.Instance.LabelStatusUpdate.Text = $"LocalAI processing chunk {i + 1}/{chunks.Count}..."
                    Debug.WriteLine($"[ReadFileAndAnswer] LocalAI processing chunk {i + 1}/{chunks.Count}")
                    
                    ' LocalAI extracts relevant information from this chunk
                    Dim chunkResult = Await ProcessChunkWithLocalAI(chunks(i), query, i + 1, chunks.Count, ct)
                    
                    If Not String.IsNullOrWhiteSpace(chunkResult) AndAlso 
                       Not chunkResult.StartsWith("[ERROR]") AndAlso
                       Not chunkResult.StartsWith("[LocalAI") Then
                        chunkSummaries.Add(chunkResult)
                        Debug.WriteLine($"[ReadFileAndAnswer] Chunk {i + 1} summary: {chunkResult.Length} chars")
                    Else
                        Debug.WriteLine($"[ReadFileAndAnswer] Chunk {i + 1} returned no relevant content or error")
                    End If
                Next
                
                ' Step 3: ALWAYS use Cloud AI for final response (NO TOKEN LIMITS!)
                Debug.WriteLine($"[ReadFileAndAnswer] === CLOUD AI FINAL RESPONSE ===")
                Debug.WriteLine($"[ReadFileAndAnswer] Combining {chunkSummaries.Count} chunk summaries for Cloud AI")
                
                Shelly.Instance.LabelStatusUpdate.Text = "Generating final response..."
                
                Dim finalAnswer = Await GenerateFinalResponseWithCloudAI(chunkSummaries, query, ct)
                
                Debug.WriteLine($"[ReadFileAndAnswer] Final response length: {finalAnswer.Length} chars")
                Return finalAnswer
            End If
            
            ' === CLOUD AI ONLY (Original behavior) ===
            Debug.WriteLine("[ReadFileAndAnswer] Using cloud AI only (LocalAI summarization OFF)")
            
            ' Break into chunks under token limit
            Dim maxCharsPerChunk As Integer = Globals.maxInputTokensPerChunk * 4
            Dim cloudChunks As New List(Of String)
            Dim current As New StringBuilder()
            For Each line In fullText.Split({Environment.NewLine}, StringSplitOptions.None)
                If current.Length + line.Length + 1 > maxCharsPerChunk AndAlso current.Length > 0 Then
                    cloudChunks.Add(current.ToString())
                    current.Clear()
                End If
                current.AppendLine(line)
            Next
            If current.Length > 0 Then cloudChunks.Add(current.ToString())

            ' Build messages: generic system prompt + all chunks + final question
            Dim messages As New List(Of Dictionary(Of String, String)) From {
                New Dictionary(Of String, String) From {
                    {"role", "system"},
                    {"content", "You are a helpful assistant. Read the provided text and answer the user's question based on its content. Provide a comprehensive answer with no artificial length limits."}
                }
            }

            ' Feed each chunk
            For idx = 0 To cloudChunks.Count - 1
                Dim tag As String = If(idx = 0, $"<CHUNK 1/{cloudChunks.Count}>", $"<CONTINUATION {idx + 1}/{cloudChunks.Count}>")
                messages.Add(New Dictionary(Of String, String) From {
                    {"role", "user"},
                    {"content", tag & vbCrLf & cloudChunks(idx)}
                })
            Next

            ' Then the actual question
            messages.Add(New Dictionary(Of String, String) From {
                {"role", "user"},
                {"content", $"Now, based on all of the above, {query}"}
            })

            ' Single API call with cancellation support
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

    ''' <summary>
    ''' Processes a single chunk with LocalAI to extract information relevant to the query.
    ''' This is the "workhorse" function - LocalAI reads and extracts, doesn't generate final answer.
    ''' </summary>
    Private Async Function ProcessChunkWithLocalAI(
        chunkText As String,
        userQuery As String,
        chunkNumber As Integer,
        totalChunks As Integer,
        ct As CancellationToken
    ) As Task(Of String)
        
        Debug.WriteLine($"[ProcessChunkWithLocalAI] Processing chunk {chunkNumber}/{totalChunks}")
        
        ' Build prompt for LocalAI - focus on EXTRACTION, not final answer
        Dim prompt As New StringBuilder()
        prompt.AppendLine($"You are reading part {chunkNumber} of {totalChunks} from a document.")
        prompt.AppendLine($"User's question: {userQuery}")
        prompt.AppendLine()
        prompt.AppendLine("TASK: Extract ALL information from this text that is relevant to answering the user's question.")
        prompt.AppendLine("- Include specific details, names, dates, numbers, quotes")
        prompt.AppendLine("- If this chunk has no relevant information, respond with: NO_RELEVANT_INFO")
        prompt.AppendLine("- Do NOT provide a final answer - just extract the relevant facts")
        prompt.AppendLine()
        prompt.AppendLine("TEXT:")
        prompt.AppendLine(chunkText)
        
        Dim result = Await LocalAITextService.GenerateWithModeAsync(
            prompt.ToString(),
            LocalAIMode.Summarization,
            ct,
            512  ' Allow decent extraction length per chunk
        )
        
        ' Filter out "no relevant info" responses
        If result.Contains("NO_RELEVANT_INFO") OrElse 
           result.Trim().Length < 20 Then
            Return ""
        End If
        
        Return result.Trim()
    End Function

    ''' <summary>
    ''' Generates the FINAL comprehensive response using Cloud AI (OpenAI).
    ''' This combines all chunk summaries and generates an UNLIMITED length response.
    ''' </summary>
    Private Async Function GenerateFinalResponseWithCloudAI(
        chunkSummaries As List(Of String),
        userQuery As String,
        ct As CancellationToken
    ) As Task(Of String)
        
        Debug.WriteLine("[GenerateFinalResponseWithCloudAI] Building final response with Cloud AI")
        
        ' Combine all chunk summaries
        Dim combinedSummaries As New StringBuilder()
        combinedSummaries.AppendLine("=== EXTRACTED INFORMATION FROM DOCUMENT ===")
        combinedSummaries.AppendLine()
        
        For i As Integer = 0 To chunkSummaries.Count - 1
            If Not String.IsNullOrWhiteSpace(chunkSummaries(i)) Then
                combinedSummaries.AppendLine($"[Section {i + 1}]")
                combinedSummaries.AppendLine(chunkSummaries(i))
                combinedSummaries.AppendLine()
            End If
        Next
        
        ' Build messages for Cloud AI - NO TOKEN LIMITS
        Dim messages As New List(Of Dictionary(Of String, String)) From {
            New Dictionary(Of String, String) From {
                {"role", "system"},
                {"content", "You are a helpful assistant. Based on the extracted information provided, " &
                           "answer the user's question comprehensively. " &
                           "There are NO LENGTH LIMITS - provide as detailed an answer as needed. " &
                           "Include all relevant details, examples, and explanations from the source material."}
            },
            New Dictionary(Of String, String) From {
                {"role", "user"},
                {"content", combinedSummaries.ToString() & Environment.NewLine & Environment.NewLine &
                           "USER QUESTION: " & userQuery & Environment.NewLine & Environment.NewLine &
                           "Please provide a comprehensive answer based on the extracted information above. " &
                           "Include all relevant details - there is no length limit."}
            }
        }
        
        ' Call Cloud AI with no artificial limits
        Dim answer = Await AIcall.CallGPTCore(
            apiKey:=Config.OpenAiApiKey,
            model:=Globals.AiModelSelection,
            messages:=messages,
            temperature:=0.3,  ' Slightly creative but factual
            ct:=ct
        )
        
        Return answer.Trim()
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



    ' ----------------------------------------------------------------------
    ' v4.0 – WebSearchAndRespondBasedOnPageContent
    '         • Uses Google search to find information across the web
    '         • Best for general searches NOT targeting a specific website
    '         • For site-specific searches, use ReadWebPageAndRespondBasedOnPageContent instead
    ' ----------------------------------------------------------------------

    <CustomFunction(
 "GENERAL web search ONLY - use when searching the entire internet without any specific website mentioned. " &
 "DO NOT USE if user mentions ANY specific site like emag.ro, amazon.com - use ReadWebPageAndRespondBasedOnPageContent instead.",
 "WebSearchAndRespondBasedOnPageContent(""best laptops 2024"", """", ""List top 5 laptops"")")>
    Public Async Function WebSearchAndRespondBasedOnPageContent(
    promptQuery As String,
    siteName As String,
    question As String,
    Optional ct As CancellationToken = Nothing
) As Task(Of String)
        Dim browser As Microsoft.Web.WebView2.WinForms.WebView2 = Nothing
        Try
            Debug.WriteLine($"[WebSearch] Starting with query='{promptQuery}', site='{siteName}'")

            ' Build search URL - ignore google.com as site parameter (it's not useful)
            Dim searchTerm As String
            If String.IsNullOrWhiteSpace(siteName) OrElse
               siteName.Contains("google", StringComparison.OrdinalIgnoreCase) OrElse
               siteName.Trim().Length < 3 Then
                searchTerm = promptQuery
            Else
                searchTerm = $"site:{siteName} {promptQuery}"
            End If

            Dim searchUrl = $"https://www.google.com/search?q={Uri.EscapeDataString(searchTerm)}&hl=en"
            Debug.WriteLine($"[WebSearch] Search URL: {searchUrl}")

            browser = New Microsoft.Web.WebView2.WinForms.WebView2()
            browser.Size = New Drawing.Size(1280, 1024)
            browser.Visible = False
            Shelly.Instance.Controls.Add(browser)
            Await browser.EnsureCoreWebView2Async()

            ' Step 1: Navigate to Google search
            Shelly.Instance.LabelStatusUpdate.Text = "Searching Google..."
            browser.CoreWebView2.Navigate(searchUrl)
            If Not Await FileHandler.WaitForNavAsync(browser, ct) Then
                Return "[ERROR] Could not load Google search page"
            End If
            Await Task.Delay(2500, ct) ' Wait for dynamic content to load

            ' Step 2: Extract search result URLs using multiple strategies
            Debug.WriteLine("[WebSearch] Extracting search result URLs...")

            ' JavaScript to extract actual search result URLs from Google
            Dim extractUrlsScript As String = "
(function() {
    var urls = [];
    
    // Strategy 1: Look for links inside search result containers (div.g)
    document.querySelectorAll('div.g a[href]').forEach(function(a) {
        var href = a.href;
        if (href && href.startsWith('http') && !href.includes('google.') && !href.includes('youtube.com') && !href.includes('webcache')) {
            urls.push(href);
        }
    });
    
    // Strategy 2: Look for links that are parents of h3 elements (title links)
    document.querySelectorAll('h3').forEach(function(h3) {
        var parent = h3.closest('a');
        if (parent && parent.href && parent.href.startsWith('http') && !parent.href.includes('google.') && !parent.href.includes('youtube.com')) {
            urls.push(parent.href);
        }
    });
    
    // Strategy 3: Look for cite elements and get their parent links
    document.querySelectorAll('cite').forEach(function(cite) {
        var container = cite.closest('a');
        if (container && container.href && container.href.startsWith('http') && !container.href.includes('google.')) {
            urls.push(container.href);
        }
    });
    
    // Remove duplicates and return
    return JSON.stringify([...new Set(urls)]);
})();
"
            Dim urlsJson = Await browser.CoreWebView2.ExecuteScriptAsync(extractUrlsScript)
            Debug.WriteLine($"[WebSearch] Raw URLs JSON: {urlsJson}")

            Dim resultUrls As New List(Of String)
            Try
                ' Parse the JSON result
                If urlsJson.StartsWith(""""c) AndAlso urlsJson.EndsWith(""""c) Then
                    urlsJson = urlsJson.Substring(1, urlsJson.Length - 2)
                    urlsJson = Regex.Unescape(urlsJson)
                End If
                resultUrls = Newtonsoft.Json.JsonConvert.DeserializeObject(Of List(Of String))(urlsJson)
            Catch parseEx As Exception
                Debug.WriteLine($"[WebSearch] URL parsing error: {parseEx.Message}")
            End Try

            ' Filter and clean URLs
            resultUrls = resultUrls.Where(Function(u)
                                              Return Not String.IsNullOrWhiteSpace(u) AndAlso
                                                     (u.StartsWith("http://") OrElse u.StartsWith("https://")) AndAlso
                                                     Not u.Contains("google.com") AndAlso
                                                     Not u.Contains("google.ro") AndAlso
                                                     Not u.Contains("googleapis.com") AndAlso
                                                     Not u.Contains("gstatic.com") AndAlso
                                                     Not u.Contains("youtube.com") AndAlso
                                                     Not u.Contains("webcache.") AndAlso
                                                     Not u.Contains("/search?") AndAlso
                                                     Not u.Contains("translate.google")
                                          End Function).Distinct().Take(5).ToList()

            Debug.WriteLine($"[WebSearch] Found {resultUrls.Count} valid result URLs:")
            For Each u In resultUrls
                Debug.WriteLine($"  - {u}")
            Next

            ' Step 3: If no URL found, handle it gracefully
            If resultUrls.Count = 0 Then
                Return "[ERROR] No valid URLs found in search results. Please try a different query or check your settings."
            End If

            ' Step 4: Visit result pages and collect content (WITH AUTO-SCROLL)
            Dim combinedContent As New StringBuilder()
            Dim pagesVisited As Integer = 0
            Dim maxPagesToVisit As Integer = Math.Min(3, resultUrls.Count)

            For i As Integer = 0 To maxPagesToVisit - 1
                Dim targetUrl As String = resultUrls(i)
                Debug.WriteLine($"[WebSearch] Visiting page {i + 1}/{maxPagesToVisit}: {targetUrl}")
                Shelly.Instance.LabelStatusUpdate.Text = $"Reading page {i + 1}/{maxPagesToVisit}..."

                Try
                    browser.CoreWebView2.Navigate(targetUrl)
                    If Not Await FileHandler.WaitForNavAsync(browser, ct) Then
                        Debug.WriteLine($"[WebSearch] Failed to load: {targetUrl}")
                        Continue For
                    End If
                    Await Task.Delay(2000, ct) ' Wait for page to fully render

                    ' AUTO-SCROLL to load lazy content (products, etc.)
                    Dim scrollScript As String = "
(async function() {
    let lastHeight = document.body.scrollHeight;
    let scrollCount = 0;
    const maxScrolls = 5;
    
    while (scrollCount < maxScrolls) {
        window.scrollTo(0, document.body.scrollHeight);
        await new Promise(resolve => setTimeout(resolve, 800));
        let newHeight = document.body.scrollHeight;
        if (newHeight === lastHeight) break;
        lastHeight = newHeight;
        scrollCount++;
    }
    window.scrollTo(0, 0);
    return scrollCount;
})();
"
                    Try
                        Await browser.CoreWebView2.ExecuteScriptAsync(scrollScript)
                    Catch
                        ' Ignore scroll errors
                    End Try
                    Await Task.Delay(1000, ct)

                    ' Extract page content
                    Dim contentScript As String = "
(function() {
    var body = document.body.cloneNode(true);
    body.querySelectorAll('script, style, nav, header, footer, aside, iframe, noscript').forEach(function(el) { el.remove(); });
    return body.innerText;
})();
"
                    Dim contentJson = Await browser.CoreWebView2.ExecuteScriptAsync(contentScript)
                    Dim pageContent As String = ""
                    Try
                        pageContent = System.Text.Json.JsonDocument.Parse(contentJson).RootElement.GetString()
                    Catch
                        pageContent = contentJson
                    End Try

                    If Not String.IsNullOrWhiteSpace(pageContent) AndAlso pageContent.Length > 100 Then
                        combinedContent.AppendLine($"=== Source: {targetUrl} ===")
                        ' Limit content per page to prevent token overflow
                        If pageContent.Length > 15000 Then
                            pageContent = pageContent.Substring(0, 15000) & "..."
                        End If
                        combinedContent.AppendLine(pageContent)
                        combinedContent.AppendLine()
                        pagesVisited += 1
                        Debug.WriteLine($"[WebSearch] Extracted {pageContent.Length} chars from {targetUrl}")
                    End If
                Catch navEx As Exception
                    Debug.WriteLine($"[WebSearch] Error visiting {targetUrl}: {navEx.Message}")
                    Continue For
                End Try
            Next

            If pagesVisited = 0 OrElse combinedContent.Length < 100 Then
                Return "[ERROR] Could not extract content from any search result pages. The pages may be blocking automated access."
            End If

            ' Step 5: Ask AI to answer based on collected content
            Debug.WriteLine($"[WebSearch] Total content collected: {combinedContent.Length} chars from {pagesVisited} pages")
            Shelly.Instance.LabelStatusUpdate.Text = "Analyzing content..."

            Dim finalPrompt As String = $"Based on information I found online, answer this question: '{question}'{Environment.NewLine}{Environment.NewLine}Content:{Environment.NewLine}{combinedContent}"

            ' Limit total prompt size
            If finalPrompt.Length > 40000 Then
                finalPrompt = finalPrompt.Substring(0, 40000) & "...[truncated]"
            End If

            Dim finalMessages = New List(Of Dictionary(Of String, String)) From {
                New Dictionary(Of String, String) From {{"role", "system"}, {"content", "You are a helpful assistant. Present information naturally and conversationally. " &
                                                                                        "NEVER mention 'scraping', 'scraped', 'extracted content', or technical data collection terms. " &
                                                                                        "Simply answer the user's question based on the information you have. " &
                                                                                        "For product listings, use clean numbered lists with names and prices."}},
                New Dictionary(Of String, String) From {{"role", "user"}, {"content", finalPrompt}}
            }

            Dim finalAnswer = Await AIcall.CallGPTCore(
                Globals.UserApiKey,
                Globals.AiModelSelection,
                finalMessages,
                temperature:=0.0,
                ct:=ct
            )

            Shelly.Instance.LabelStatusUpdate.Text = "Done."
            Return finalAnswer.Trim()

        Catch ex As OperationCanceledException
            Return "[Cancelled] Web search was cancelled."
        Catch ex As Exception
            Debug.WriteLine($"[WebSearch] Exception: {ex.Message}")
            Debug.WriteLine($"[WebSearch] StackTrace: {ex.StackTrace}")
            Return "[ERROR] " & ex.Message
        Finally
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


    <CustomFunction(
        "Searches files in one or more folders (recursively) for text content. Returns ONLY the file paths of matching files - NOT the file content. " &
        "Supports all file types: PDF, DOCX, XLSX, PPTX, TXT, and other text-based files. " &
        "Use this to find files containing specific text, then use ReadFileAndAnswer if you need to read the content.",
        "SearchForTextInsideFiles(""D:\Demo"", ""Alexandra"")")>
    Public Async Function SearchForTextInsideFiles(paths As String, searchText As String, Optional ct As CancellationToken = Nothing) As Task(Of String)
        Try
            ' Use active cancellation token if none provided
            If ct = Nothing OrElse ct = CancellationToken.None Then
                ct = FileHandler.ActiveCancellationToken
            End If

            If String.IsNullOrWhiteSpace(searchText) Then
                Return "[ERROR] searchText is required."
            End If

            Dim targets = paths.Split({","c}, StringSplitOptions.RemoveEmptyEntries).
                Select(Function(p) p.Trim()).
                Where(Function(p) Not String.IsNullOrWhiteSpace(p)).
                ToList()

            If targets.Count = 0 Then
                Return "[ERROR] No valid paths provided."
            End If

            Dim matchingFiles As New List(Of String)
            Dim scannedCount As Integer = 0
            Dim errorFiles As New List(Of String)

            ' Supported file extensions (same as FileHandler.ReadFileContent)
            Dim supportedExtensions As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase) From {
                ".pdf", ".docx", ".xlsx", ".pptx",
                ".txt", ".doc", ".odt", ".rtf", ".tex", ".wpd", ".log", ".csv",
                ".xml", ".html", ".htm", ".xhtml", ".md", ".json", ".yaml", ".yml",
                ".ini", ".config", ".conf", ".properties", ".sql", ".bat", ".sh",
                ".java", ".c", ".cpp", ".py", ".js", ".php", ".css", ".scss",
                ".asp", ".aspx", ".jsp", ".pl", ".rb", ".vb", ".swift", ".kt"
            }

            Shelly.Instance.LabelStatusUpdate.Text = "Searching for files..."

            For Each target In targets
                ct.ThrowIfCancellationRequested()

                If Directory.Exists(target) Then
                    ' Get all files recursively
                    Dim allFiles As IEnumerable(Of String)
                    Try
                        allFiles = Directory.EnumerateFiles(target, "*", SearchOption.AllDirectories)
                    Catch ex As UnauthorizedAccessException
                        errorFiles.Add($"[ACCESS DENIED] {target}")
                        Continue For
                    End Try

                    For Each filePath In allFiles
                        ct.ThrowIfCancellationRequested()

                        ' Check if file extension is supported
                        Dim ext = Path.GetExtension(filePath).ToLowerInvariant()
                        If Not supportedExtensions.Contains(ext) Then
                            Continue For
                        End If

                        scannedCount += 1

                        ' Update status periodically
                        If scannedCount Mod 50 = 0 Then
                            Shelly.Instance.LabelStatusUpdate.Text = $"Scanned {scannedCount} files, found {matchingFiles.Count} matches..."
                        End If

                        ' Check if file contains the search text
                        If FileContainsText(filePath, searchText) Then
                            matchingFiles.Add(filePath)
                        End If
                    Next

                ElseIf File.Exists(target) Then
                    scannedCount += 1
                    If FileContainsText(target, searchText) Then
                        matchingFiles.Add(target)
                    End If
                Else
                    errorFiles.Add($"[NOT FOUND] {target}")
                End If
            Next

            ' Build result
            Dim result As New StringBuilder()

            If matchingFiles.Count = 0 Then
                result.AppendLine($"No files found containing ""{searchText}"".")
                result.AppendLine($"Scanned {scannedCount} supported files.")
            Else
                result.AppendLine($"Found {matchingFiles.Count} file(s) containing ""{searchText}"":")
                result.AppendLine()
                For Each filePath In matchingFiles
                    result.AppendLine(filePath)
                Next
                result.AppendLine()
                result.AppendLine($"(Scanned {scannedCount} files total)")
            End If

            ' Add warnings if any
            If errorFiles.Count > 0 Then
                result.AppendLine()
                result.AppendLine("Warnings:")
                For Each errorItem In errorFiles.Take(5)
                    result.AppendLine($"  {errorItem}")
                Next
                If errorFiles.Count > 5 Then
                    result.AppendLine($"  ... and {errorFiles.Count - 5} more")
                End If
            End If

            Shelly.Instance.LabelStatusUpdate.Text = $"Search complete. Found {matchingFiles.Count} matching file(s)."
            Return result.ToString().Trim()

        Catch ex As OperationCanceledException
            Return "[Cancelled] Search was cancelled."
        Catch ex As Exception
            Return "[ERROR] " & ex.Message
        End Try
    End Function

    ''' <summary>
    ''' Checks if a file contains the specified text (case-insensitive).
    ''' Uses FileHandler.ReadFileContent to support all file types (PDF, DOCX, XLSX, PPTX, text files).
    ''' Does NOT send content to AI - just searches locally.
    ''' </summary>
    Private Function FileContainsText(filePath As String, searchText As String) As Boolean
        Try
            ' Use FileHandler to read content (supports PDF, DOCX, XLSX, PPTX, and text files)
            Dim content As String = FileHandler.ReadFileContent(filePath)

            If String.IsNullOrWhiteSpace(content) Then
                Return False
            End If

            ' Case-insensitive search
            Return content.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0

        Catch ex As Exception
            ' File couldn't be read (unsupported format, locked, etc.) - skip it
            Debug.WriteLine($"[SearchForTextInsideFiles] Could not read {filePath}: {ex.Message}")
            Return False
        End Try
    End Function


    ' =====================================================================================================
    ' ================================= ADDITIONAL CUSTOM FUNCTIONS ====================================
    ' =====================================================================================================

    <CustomFunction(
        "Reads and updates a file content (code or text): reads it in line-safe chunks, applies your update instruction to each chunk, then rewrites the file.",
        "UpdateFileByChunks(""C:\Demo\myfile.txt"", ""Fix all spelling errors"")")>
    Public Async Function UpdateFileByChunks(
        filePath As String,
        updateInstruction As String,
        Optional chunkTokenOverride As Integer = -1,
        Optional ct As CancellationToken = Nothing
    ) As Task(Of String)
        Try
            ' Use active cancellation token if none provided
            If ct = Nothing OrElse ct = CancellationToken.None Then
                ct = FileHandler.ActiveCancellationToken
            End If

            ' Check file exists
            If Not File.Exists(filePath) Then
                Return $"[ERROR] File not found: {filePath}"
            End If

            ' Read the file content
            Dim originalContent As String = File.ReadAllText(filePath)
            If String.IsNullOrWhiteSpace(originalContent) Then
                Return $"[ERROR] File is empty: {filePath}"
            End If

            ' Split into chunks (line-safe)
            Dim maxWordsPerChunk As Integer = If(chunkTokenOverride > 0, chunkTokenOverride, 500)
            Dim chunks As List(Of String) = FileHandler.SplitTextIntoChunks(originalContent, maxWordsPerChunk)

            If chunks.Count = 0 Then
                Return "[ERROR] Could not split file into chunks."
            End If

            Shelly.Instance.LabelStatusUpdate.Text = $"Updating file in {chunks.Count} chunk(s)..."

            ' Process each chunk
            Dim updatedChunks As New List(Of String)
            For i As Integer = 0 To chunks.Count - 1
                ct.ThrowIfCancellationRequested()

                Shelly.Instance.LabelStatusUpdate.Text = $"Processing chunk {i + 1}/{chunks.Count}..."

                Dim updatedChunk As String = Await FileHandler.ProcessChunkUpdate(chunks(i), updateInstruction)
                updatedChunks.Add(updatedChunk)
            Next

            ' Combine and rewrite
            Dim finalContent As String = String.Join(Environment.NewLine, updatedChunks)
            File.WriteAllText(filePath, finalContent, Encoding.UTF8)

            Shelly.Instance.LabelStatusUpdate.Text = "File updated successfully."
            Return $"✅ File updated successfully: {filePath} ({chunks.Count} chunk(s) processed)"

        Catch ex As OperationCanceledException
            Return "[Cancelled] File update was cancelled."
        Catch ex As Exception
            Return $"[ERROR] {ex.Message}"
        End Try
    End Function


    <CustomFunction(
        "Reads the Copilot chat window content and answers a question about it.",
        "ReadCopilotConversation(""What was the last topic discussed?"")")>
    Public Async Function ReadCopilotConversation(
        query As String,
        Optional ct As CancellationToken = Nothing
    ) As Task(Of String)
        Try
            ' Use active cancellation token if none provided
            If ct = Nothing OrElse ct = CancellationToken.None Then
                ct = FileHandler.ActiveCancellationToken
            End If

            ' Check if Copilot form is open and has content
            If Copilot.Instance Is Nothing OrElse Copilot.Instance.IsDisposed Then
                Return "[ERROR] Copilot window is not open. Please open Copilot first."
            End If

            Dim copilotText As String = ""

            ' Get the text from the Copilot form's AiOnePrompt RichTextBox
            If Copilot.Instance.InvokeRequired Then
                Copilot.Instance.Invoke(Sub()
                                            copilotText = Copilot.Instance.AiOnePrompt.Text
                                        End Sub)
            Else
                copilotText = Copilot.Instance.AiOnePrompt.Text
            End If

            If String.IsNullOrWhiteSpace(copilotText) Then
                Return "[ERROR] No conversation found in Copilot window."
            End If

            ' Build messages for AI analysis
            Dim messages As New List(Of Dictionary(Of String, String)) From {
                New Dictionary(Of String, String) From {
                    {"role", "system"},
                    {"content", "You are analyzing a conversation from Microsoft Copilot. Answer the user's question based on the conversation content provided."}
                },
                New Dictionary(Of String, String) From {
                    {"role", "user"},
                    {"content", $"Here is the Copilot conversation:{Environment.NewLine}{copilotText}{Environment.NewLine}{Environment.NewLine}Question: {query}"}
                }
            }

            Dim answer As String = Await AIcall.CallGPTCore(
                Config.OpenAiApiKey,
                Globals.AiModelSelection,
                messages,
                0.3,
                ct
            )

            Return answer.Trim()

        Catch ex As OperationCanceledException
            Return "[Cancelled] Reading Copilot conversation was cancelled."
        Catch ex As Exception
            Return $"[ERROR] {ex.Message}"
        End Try
    End Function


    ' ----------------------------------------------------------------------
    ' v3.0 – ReadWebPageAndRespondBasedOnPageContent
    '         • For site: searches, extracts actual site URL from Google results
    '         • Then navigates to that page and scrapes with full scrolling
    '         • For direct URLs (user provides https://), goes directly
    ' ----------------------------------------------------------------------
    <CustomFunction(
        "USE THIS for ANY request mentioning a specific website. " &
        "Build URL as: https://www.google.com/search?q=site:[DOMAIN]+[KEYWORDS]. " &
        "Will extract first result URL and navigate there for full scraping.",
        "ReadWebPageAndRespondBasedOnPageContent(""https://www.google.com/search?q=site:emag.ro+HP+laptops"", ""List products and prices"")")>
    Public Async Function ReadWebPageAndRespondBasedOnPageContent(
        url As String,
        query As String,
        Optional ct As CancellationToken = Nothing
    ) As Task(Of String)
        Dim browser As Microsoft.Web.WebView2.WinForms.WebView2 = Nothing
        Try
            Debug.WriteLine($"[ReadWebPage] Starting with URL: {url}")

            ' Validate URL
            If String.IsNullOrWhiteSpace(url) Then
                Return "[ERROR] URL is required."
            End If

            ' Clean and normalize URL
            url = url.Trim().Trim(""""c).Trim("'"c)
            If Not url.StartsWith("http://") AndAlso Not url.StartsWith("https://") Then
                url = "https://" & url
            End If

            Shelly.Instance.LabelStatusUpdate.Text = "Loading page..."
            Debug.WriteLine($"[ReadWebPage] Navigating to: {url}")

            browser = New Microsoft.Web.WebView2.WinForms.WebView2()
            browser.Size = New Drawing.Size(1280, 1024)
            browser.Visible = False
            Shelly.Instance.Controls.Add(browser)
            Await browser.EnsureCoreWebView2Async()

            ' Navigate to URL
            browser.CoreWebView2.Navigate(url)
            If Not Await FileHandler.WaitForNavAsync(browser, ct) Then
                Return $"[ERROR] Could not load page: {url}"
            End If
            Await Task.Delay(2500, ct)

            ' Check if this is a Google site: search - if so, extract first result and navigate there
            Dim targetUrl As String = url
            If url.Contains("google.com/search") AndAlso url.Contains("site:") Then
                Debug.WriteLine("[ReadWebPage] Detected Google site: search, extracting first result URL...")
                Shelly.Instance.LabelStatusUpdate.Text = "Finding target page..."

                ' Extract the domain from site: parameter
                Dim siteMatch = Regex.Match(url, "site:([^\s+&]+)", RegexOptions.IgnoreCase)
                Dim targetDomain As String = If(siteMatch.Success, siteMatch.Groups(1).Value.ToLower(), "")

                ' JavaScript to extract first result URL that matches the target domain
                Dim extractUrlScript As String = $"
(function() {{
    var targetDomain = '{targetDomain}';
    var urls = [];
    
    // Look for links in search results
    document.querySelectorAll('div.g a[href], h3 a[href]').forEach(function(a) {{
        var href = a.href;
        if (href && href.includes(targetDomain) && !href.includes('google.') && !href.includes('webcache')) {{
            urls.push(href);
        }}
    }});
    
    // Also check h3 parent links
    document.querySelectorAll('h3').forEach(function(h3) {{
        var parent = h3.closest('a');
        if (parent && parent.href && parent.href.includes(targetDomain)) {{
            urls.push(parent.href);
        }}
    }});
    
    // Return first unique URL
    var unique = [...new Set(urls)];
    return unique.length > 0 ? unique[0] : '';
}})();
"
                Dim resultUrlJson = Await browser.CoreWebView2.ExecuteScriptAsync(extractUrlScript)
                Dim extractedUrl As String = ""
                Try
                    extractedUrl = System.Text.Json.JsonDocument.Parse(resultUrlJson).RootElement.GetString()
                Catch
                    extractedUrl = resultUrlJson.Trim(""""c)
                End Try

                If Not String.IsNullOrWhiteSpace(extractedUrl) AndAlso extractedUrl.StartsWith("http") Then
                    Debug.WriteLine($"[ReadWebPage] Found target URL: {extractedUrl}")
                    targetUrl = extractedUrl

                    ' Navigate to the actual product page
                    Shelly.Instance.LabelStatusUpdate.Text = "Loading product page..."
                    browser.CoreWebView2.Navigate(targetUrl)
                    If Not Await FileHandler.WaitForNavAsync(browser, ct) Then
                        Return $"[ERROR] Could not load page: {targetUrl}"
                    End If
                    Await Task.Delay(2500, ct)
                Else
                    Debug.WriteLine("[ReadWebPage] No matching URL found in Google results, using Google page content")
                End If
            End If

            ' Now we're on the actual target page - scroll to load all content
            Shelly.Instance.LabelStatusUpdate.Text = "Scrolling to load all content..."
            Debug.WriteLine("[ReadWebPage] Starting auto-scroll...")

            Dim scrollScript As String = "
(async function() {
    let lastHeight = document.body.scrollHeight;
    let scrollCount = 0;
    const maxScrolls = 15;
    
    while (scrollCount < maxScrolls) {
        window.scrollTo(0, document.body.scrollHeight);
        await new Promise(resolve => setTimeout(resolve, 1200));
        
        // Also try clicking 'load more' buttons if present
        var loadMoreBtns = document.querySelectorAll('[class*=""load-more""], [class*=""show-more""], button[class*=""more""]');
        loadMoreBtns.forEach(function(btn) { try { btn.click(); } catch(e) {} });
        
        let newHeight = document.body.scrollHeight;
        if (newHeight === lastHeight) {
            // Try one more scroll after a longer wait
            await new Promise(resolve => setTimeout(resolve, 2000));
            newHeight = document.body.scrollHeight;
            if (newHeight === lastHeight) break;
        }
        lastHeight = newHeight;
        scrollCount++;
    }
    
    window.scrollTo(0, 0);
    return scrollCount;
})();
"
            Try
                Dim scrollResult = Await browser.CoreWebView2.ExecuteScriptAsync(scrollScript)
                Debug.WriteLine($"[ReadWebPage] Scrolled {scrollResult} times")
            Catch scrollEx As Exception
                Debug.WriteLine($"[ReadWebPage] Scroll error (non-fatal): {scrollEx.Message}")
            End Try

            Await Task.Delay(2000, ct)

            Shelly.Instance.LabelStatusUpdate.Text = "Extracting content..."

            ' Extract page content
            Dim contentScript As String = "
(function() {
    var body = document.body.cloneNode(true);
    
    // Remove noise elements
    var removeSelectors = [
        'script', 'style', 'noscript', 'iframe',
        'nav', 'header', 'footer', 'aside',
        '[role=""navigation""]', '[role=""banner""]',
        '.cookie-banner', '.cookie-notice', '.cookies',
        '.popup', '.modal', '.overlay',
        '.advertisement', '.ad', '.ads'
    ];
    
    removeSelectors.forEach(function(selector) {
        try {
            body.querySelectorAll(selector).forEach(function(el) { el.remove(); });
        } catch(e) {}
    });
    
    return body.innerText || body.textContent || '';
})();
"
            Dim pageJson = Await browser.CoreWebView2.ExecuteScriptAsync(contentScript)
            Dim pageText As String = ""
            Try
                pageText = System.Text.Json.JsonDocument.Parse(pageJson).RootElement.GetString()
            Catch
                pageText = pageJson.Trim(""""c)
            End Try

            If String.IsNullOrWhiteSpace(pageText) OrElse pageText.Length < 100 Then
                Return "[ERROR] Unable to retrieve meaningful page content."
            End If

            Debug.WriteLine($"[ReadWebPage] Extracted {pageText.Length} chars from {targetUrl}")

            ' Limit content size
            Dim maxContentSize As Integer = 50000
            If pageText.Length > maxContentSize Then
                pageText = pageText.Substring(0, maxContentSize) & Environment.NewLine & "...[content truncated]"
            End If

            Shelly.Instance.LabelStatusUpdate.Text = "Analyzing content..."

            ' Ask AI to answer
            Dim prompt As String = $"Based on the content from {targetUrl}, please answer: {query}{Environment.NewLine}{Environment.NewLine}=== PAGE CONTENT ==={Environment.NewLine}{pageText}"

            Dim messages = New List(Of Dictionary(Of String, String)) From {
                New Dictionary(Of String, String) From {
                    {"role", "system"},
                    {"content", "You are a helpful assistant. Present information naturally and conversationally. " &
                               "NEVER mention 'scraping', 'scraped', 'extracted content', or technical data collection terms. " &
                               "Simply answer the user's question based on the information you have. " &
                               "For product listings, use clean numbered lists with names and prices."}
                },
                New Dictionary(Of String, String) From {
                    {"role", "user"},
                    {"content", prompt}
                }
            }

            Dim answer = Await AIcall.CallGPTCore(
                Globals.UserApiKey,
                Globals.AiModelSelection,
                messages,
                temperature:=0.0,
                ct:=ct
            )

            Shelly.Instance.LabelStatusUpdate.Text = "Done."
            Return answer.Trim()

        Catch ex As OperationCanceledException
            Return "[Cancelled] Page reading was cancelled."
        Catch ex As Exception
            Debug.WriteLine($"[ReadWebPage] Exception: {ex.Message}")
            Debug.WriteLine($"[ReadWebPage] StackTrace: {ex.StackTrace}")
            Return "[ERROR] " & ex.Message
        Finally
            If browser IsNot Nothing Then
                Try
                    If Shelly.Instance.Controls.Contains(browser) Then
                        Shelly.Instance.Controls.Remove(browser)
                    End If
                    browser.Dispose()
                Catch disposeEx As Exception
                    Debug.WriteLine($"[ReadWebPage] Error disposing: {disposeEx.Message}")
                End Try
            End If
        End Try
    End Function


End Module


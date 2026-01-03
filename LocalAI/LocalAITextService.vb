' ###  LocalAITextService.vb - v1.1.0 ###

' ##########################################################
'  Shelly - Local AI Text Service
'  Provides THREE-mode LocalAI operations:
'    1. Confirmation Mode: Short spoken confirmations (TTS) - ALWAYS INDEPENDENT
'    2. Summarization Mode: File/text analysis and Q&A
'    3. ConversationHistory Mode: Specialized conversation context preservation
'  
'  IMPORTANT: 
'    - Confirmation mode runs INDEPENDENTLY (controlled by LocalAIIncludeTTS)
'    - Summarization + ConversationHistory modes are CONTROLLED by LocalAIUseSummarization checkbox
'    - LocalAI is NEVER used for: PowerShell execution, reasoning, planning
'  
'  License: Creative Commons Attribution-NonCommercial (CC BY-NC)
'  © 2025 Vlad Stefanescu | GreenCoders.net. Attribution required.
' ##########################################################

Imports System.Threading
Imports System.Text

''' <summary>
''' Enumeration for LocalAI operation modes - THREE distinct roles
''' </summary>
Public Enum LocalAIMode
    ''' <summary>
    ''' Voice confirmation mode - short, spoken TTS responses
    ''' INDEPENDENT: Controlled by LocalAIIncludeTTS checkbox
    ''' Used for: Final step confirmation after task completion
    ''' </summary>
    Confirmation

    ''' <summary>
    ''' Summarization mode - analytical text processing for files
    ''' CONTROLLED BY: LocalAIUseSummarization checkbox
    ''' Used for: ReadFileAndAnswer, ProcessChunk, file Q&A
    ''' </summary>
    Summarization

    ''' <summary>
    ''' Conversation history mode - specialized for preserving conversation context
    ''' CONTROLLED BY: LocalAIUseSummarization checkbox
    ''' Used for: convHistory.SummarizeMessagesAsync (token management)
    ''' </summary>
    ConversationHistory
End Enum

''' <summary>
''' Centralized service for LocalAI text generation with THREE-mode support.
''' 
''' ? NEVER USED FOR:
'''   - PowerShell script generation or execution
'''   - AI reasoning or planning
'''   - Tool selection or orchestration
''' 
''' ? USED FOR (when LocalAIUseSummarization is ON):
'''   - Reading and analyzing large files (ReadFileAndAnswer)
'''   - Chunk processing (ProcessChunk, ProcessChunkUpdate)
'''   - Conversation history summarization (convHistory)
'''   - Prompt rephrasing (ReviseUserQuestionAsync)
''' 
''' ? ALWAYS USED FOR (when LocalAIIncludeTTS is ON):
'''   - Final step voice confirmation (independent of summarization setting)
''' </summary>
Public Module LocalAITextService

    Private Const DebugPrefix As String = "[LocalAI-TextService]"

    ''' <summary>
    ''' Generates text using LocalAI with the specified mode.
    ''' Automatically selects the appropriate system prompt based on mode.
    ''' </summary>
    ''' <param name="userPrompt">The user's prompt/question</param>
    ''' <param name="mode">The operation mode (Confirmation, Summarization, or ConversationHistory)</param>
    ''' <param name="ct">Cancellation token</param>
    ''' <param name="maxTokens">Maximum tokens to generate (default varies by mode)</param>
    ''' <returns>Generated text or error message</returns>
    Public Async Function GenerateWithModeAsync(
        userPrompt As String,
        mode As LocalAIMode,
        Optional ct As CancellationToken = Nothing,
        Optional maxTokens As Integer = -1
    ) As Task(Of String)

        ' Debug: Log every LocalAI usage
        Debug.WriteLine($"{DebugPrefix} === LocalAI CALLED ===")
        Debug.WriteLine($"{DebugPrefix} Mode: {mode}")
        Debug.WriteLine($"{DebugPrefix} Prompt length: {userPrompt.Length} chars")

        ' Check if LocalAI engine is ready
        If Not Globals.IsSharedLocalAIReady() Then
            Debug.WriteLine($"{DebugPrefix} ABORT: LocalAI engine not ready")
            Return "[LocalAI NOT READY] Model not loaded. Please open LocalAI Form and load a model."
        End If

        Try
            Dim systemPrompt As String = GetSystemPromptForMode(mode)
            Dim tokenLimit As Integer = If(maxTokens > 0, maxTokens, GetDefaultTokensForMode(mode))

            Debug.WriteLine($"{DebugPrefix} System prompt mode: {mode}")
            Debug.WriteLine($"{DebugPrefix} Token limit: {tokenLimit}")

            Dim sw = Diagnostics.Stopwatch.StartNew()

            Dim response = Await Globals.SharedLocalAIEngine.GenerateResponseAsync(
                prompt:=userPrompt,
                systemPrompt:=systemPrompt,
                ct:=ct,
                maxTokens:=tokenLimit
            )

            sw.Stop()

            Debug.WriteLine($"{DebugPrefix} Response generated in {sw.ElapsedMilliseconds}ms")
            Debug.WriteLine($"{DebugPrefix} Response length: {If(response IsNot Nothing, response.Length, 0)} chars")
            Debug.WriteLine($"{DebugPrefix} === LocalAI COMPLETE ===")

            If String.IsNullOrWhiteSpace(response) OrElse response.StartsWith("[ERROR]") Then
                Debug.WriteLine($"{DebugPrefix} Generation failed: {response}")
                Return If(response, "[ERROR] LocalAI returned empty response")
            End If

            Return response.Trim()

        Catch ex As OperationCanceledException
            Debug.WriteLine($"{DebugPrefix} Operation cancelled")
            Return "[Cancelled] LocalAI operation was cancelled."
        Catch ex As Exception
            Debug.WriteLine($"{DebugPrefix} ERROR: {ex.Message}")
            Return $"[ERROR] LocalAI: {ex.Message}"
        End Try
    End Function

    ''' <summary>
    ''' Summarizes text content using LocalAI (for FILES, not conversations).
    ''' Uses Summarization mode.
    ''' </summary>
    Public Async Function SummarizeTextAsync(
        textContent As String,
        Optional summarizationInstruction As String = Nothing,
        Optional ct As CancellationToken = Nothing
    ) As Task(Of String)

        Debug.WriteLine($"{DebugPrefix} SummarizeTextAsync called (FILE summarization)")
        Debug.WriteLine($"{DebugPrefix} Content length: {textContent.Length} chars")

        Dim prompt As New StringBuilder()

        If Not String.IsNullOrWhiteSpace(summarizationInstruction) Then
            prompt.AppendLine(summarizationInstruction)
            prompt.AppendLine()
        Else
            prompt.AppendLine("Summarize the following text concisely, preserving key information:")
            prompt.AppendLine()
        End If

        ' Truncate if content is too long for LocalAI context
        Const MaxContentChars As Integer = 6000
        If textContent.Length > MaxContentChars Then
            prompt.AppendLine(textContent.Substring(0, MaxContentChars))
            prompt.AppendLine("...[content truncated]")
        Else
            prompt.AppendLine(textContent)
        End If

        Return Await GenerateWithModeAsync(prompt.ToString(), LocalAIMode.Summarization, ct)
    End Function

    ''' <summary>
    ''' Summarizes CONVERSATION HISTORY using LocalAI.
    ''' Uses ConversationHistory mode with specialized context preservation.
    ''' </summary>
    Public Async Function SummarizeConversationAsync(
        conversationText As String,
        Optional ct As CancellationToken = Nothing
    ) As Task(Of String)

        Debug.WriteLine($"{DebugPrefix} SummarizeConversationAsync called (CONVERSATION summarization)")
        Debug.WriteLine($"{DebugPrefix} Conversation length: {conversationText.Length} chars")

        Dim prompt As New StringBuilder()
        prompt.AppendLine("Summarize this conversation history concisely while preserving:")
        prompt.AppendLine("- Key user requests and intents")
        prompt.AppendLine("- Important assistant responses and results")
        prompt.AppendLine("- Any code blocks (keep them intact within triple backticks)")
        prompt.AppendLine("- File paths and technical details mentioned")
        prompt.AppendLine()
        prompt.AppendLine("Conversation:")

        ' Truncate if too long
        Const MaxConvChars As Integer = 5000
        If conversationText.Length > MaxConvChars Then
            prompt.AppendLine(conversationText.Substring(0, MaxConvChars))
            prompt.AppendLine("...[conversation truncated]")
        Else
            prompt.AppendLine(conversationText)
        End If

        Return Await GenerateWithModeAsync(prompt.ToString(), LocalAIMode.ConversationHistory, ct, 384)
    End Function

    ''' <summary>
    ''' Answers a question about provided text content using LocalAI.
    ''' </summary>
    Public Async Function AnswerFromTextAsync(
        textContent As String,
        question As String,
        Optional ct As CancellationToken = Nothing
    ) As Task(Of String)

        Debug.WriteLine($"{DebugPrefix} AnswerFromTextAsync called")
        Debug.WriteLine($"{DebugPrefix} Content length: {textContent.Length} chars")
        Debug.WriteLine($"{DebugPrefix} Question: {question}")

        Dim prompt As New StringBuilder()
        prompt.AppendLine($"Based on the following text, answer this question: {question}")
        prompt.AppendLine()
        prompt.AppendLine("Text:")

        ' Truncate if content is too long
        Const MaxContentChars As Integer = 5000
        If textContent.Length > MaxContentChars Then
            prompt.AppendLine(textContent.Substring(0, MaxContentChars))
            prompt.AppendLine("...[content truncated]")
        Else
            prompt.AppendLine(textContent)
        End If

        Return Await GenerateWithModeAsync(prompt.ToString(), LocalAIMode.Summarization, ct, 512)
    End Function

    ''' <summary>
    ''' Rephrases/restructures a user prompt using LocalAI.
    ''' </summary>
    Public Async Function RephrasePromptAsync(
        originalPrompt As String,
        Optional ct As CancellationToken = Nothing
    ) As Task(Of String)

        Debug.WriteLine($"{DebugPrefix} RephrasePromptAsync called")
        Debug.WriteLine($"{DebugPrefix} Original prompt: {originalPrompt}")

        Dim prompt As String = $"Review and restructure this user prompt for clarity. If it's already clear, return it unchanged. " &
                               $"If it contains multiple tasks, list them as bullet points. Do not add commentary.{Environment.NewLine}{Environment.NewLine}" &
                               $"Prompt: {originalPrompt}"

        Dim result = Await GenerateWithModeAsync(prompt, LocalAIMode.Summarization, ct, 256)

        ' If LocalAI returns something weird, return original
        If result.StartsWith("[ERROR]") OrElse result.StartsWith("[Cancelled]") OrElse result.StartsWith("[LocalAI") Then
            Debug.WriteLine($"{DebugPrefix} Rephrase failed, returning original prompt")
            Return originalPrompt
        End If

        Return result
    End Function

    ''' <summary>
    ''' Gets the appropriate system prompt for the specified mode.
    ''' THREE DISTINCT INSTRUCTIONS for three different roles.
    ''' </summary>
    Private Function GetSystemPromptForMode(mode As LocalAIMode) As String
        Select Case mode
            Case LocalAIMode.Confirmation
                ' ========================================
                ' ROLE 1: Voice Confirmation (TTS)
                ' Short, spoken confirmations - INDEPENDENT
                ' ========================================
                Return "You are Shelly, a voice assistant. " &
                       "Give a brief spoken confirmation of what you just did. " &
                       "Say 'I' as if YOU did the work. " &
                       "Example: 'Done! I told you a joke.' or 'I found the time and wrote the story for you.' " &
                       "Keep it to 1-2 sentences. No file paths or technical terms."

            Case LocalAIMode.Summarization
                ' ========================================
                ' ROLE 2: File/Text Summarization
                ' Analytical processing for files and content
                ' ========================================
                Return "You are a helpful AI assistant specialized in text analysis and summarization. " &
                       "Provide clear, concise, and accurate responses. " &
                       "When summarizing files, preserve key information, data points, and structure. " &
                       "When answering questions about text, be direct and factual. " &
                       "Preserve any code blocks exactly as they appear. " &
                       "Do not add unnecessary commentary or disclaimers."

            Case LocalAIMode.ConversationHistory
                ' ========================================
                ' ROLE 3: Conversation History Compression
                ' Specialized for preserving conversation context
                ' ========================================
                Return "You are a conversation summarizer for an AI assistant system. " &
                       "Your job is to compress conversation history while preserving critical context. " &
                       "MUST PRESERVE: " &
                       "- User's original requests and intentions " &
                       "- Key results and outcomes from assistant actions " &
                       "- Any code blocks (keep intact in triple backticks) " &
                       "- File paths, URLs, and technical references " &
                       "- Error messages or important warnings " &
                       "OUTPUT: A concise summary that allows the conversation to continue seamlessly."

            Case Else
                Return "You are a helpful AI assistant."
        End Select
    End Function

    ''' <summary>
    ''' Gets the default max tokens for the specified mode.
    ''' </summary>
    Private Function GetDefaultTokensForMode(mode As LocalAIMode) As Integer
        Select Case mode
            Case LocalAIMode.Confirmation
                Return 64   ' Short spoken confirmations
            Case LocalAIMode.Summarization
                Return 512  ' File analysis responses
            Case LocalAIMode.ConversationHistory
                Return 384  ' Conversation summaries (compact but complete)
            Case Else
                Return 256
        End Select
    End Function

    ''' <summary>
    ''' Checks if LocalAI is available and ready for the specified mode.
    ''' </summary>
    Public Function IsAvailableForMode(mode As LocalAIMode) As Boolean
        Select Case mode
            Case LocalAIMode.Confirmation
                ' Confirmation mode: requires LocalAI + TTS (INDEPENDENT setting)
                Return Globals.IsSharedLocalAIReady() AndAlso Globals.LocalAIIncludeTTS

            Case LocalAIMode.Summarization, LocalAIMode.ConversationHistory
                ' Summarization modes: requires LocalAI + summarization checkbox
                Return Globals.IsLocalAISummarizationEnabled()

            Case Else
                Return Globals.IsSharedLocalAIReady()
        End Select
    End Function

End Module

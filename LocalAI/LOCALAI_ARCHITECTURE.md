# LocalAI Three-Role Architecture

## Overview

Shelly's LocalAI system operates with **three distinct roles**, each with specialized system prompts and behaviors. This architecture ensures optimal performance for different tasks while maintaining clear separation of concerns.

---

## The Three Roles

### Role 1: Confirmation Mode (Voice TTS)

| Aspect | Detail |
|--------|--------|
| **Enum Value** | `LocalAIMode.Confirmation` |
| **Trigger** | `LocalAIIncludeTTS` checkbox in LocalAIForm |
| **Purpose** | Short spoken confirmations after task completion |
| **Independence** | ? **RUNS INDEPENDENTLY** - regardless of summarization setting |
| **Max Tokens** | 64 (short responses) |
| **Used By** | `HandleUserRequest.SpeakLocalAISummaryAsync()` |

**System Prompt:**
```
You are Shelly, a voice assistant. Give a brief spoken confirmation of what you just did. 
Say 'I' as if YOU did the work. Example: 'Done! I told you a joke.' or 
'I found the time and wrote the story for you.' Keep it to 1-2 sentences. 
No file paths or technical terms.
```

---

### Role 2: Summarization Mode (Files/Text)

| Aspect | Detail |
|--------|--------|
| **Enum Value** | `LocalAIMode.Summarization` |
| **Trigger** | `LocalAIUseSummarization` checkbox in Settings |
| **Purpose** | Analyze and EXTRACT relevant information from file content |
| **Independence** | ? Only when checkbox is ON |
| **Max Tokens** | 512 (detailed extractions) |
| **Used By** | `ReadFileAndAnswer`, `ProcessChunk`, `ProcessChunkUpdate`, `RephrasePromptAsync` |

**System Prompt:**
```
You are a helpful AI assistant specialized in text analysis and summarization. 
Provide clear, concise, and accurate responses. When summarizing files, preserve 
key information, data points, and structure. When answering questions about text, 
be direct and factual. Preserve any code blocks exactly as they appear. 
Do not add unnecessary commentary or disclaimers.
```

**Important:** In this mode, LocalAI is the **workhorse** - it extracts facts from chunks but does NOT generate the final answer. Cloud AI always generates the final user-facing response.

---

### Role 3: ConversationHistory Mode (Context Preservation)

| Aspect | Detail |
|--------|--------|
| **Enum Value** | `LocalAIMode.ConversationHistory` |
| **Trigger** | `LocalAIUseSummarization` checkbox in Settings |
| **Purpose** | Compress conversation history while preserving critical context |
| **Independence** | ? Only when checkbox is ON |
| **Max Tokens** | 384 (compact but complete) |
| **Used By** | `convHistory.SummarizeMessagesAsync()` |

**System Prompt:**
```
You are a conversation summarizer for an AI assistant system. Your job is to 
compress conversation history while preserving critical context. 
MUST PRESERVE: 
- User's original requests and intentions 
- Key results and outcomes from assistant actions 
- Any code blocks (keep intact in triple backticks) 
- File paths, URLs, and technical references 
- Error messages or important warnings 
OUTPUT: A concise summary that allows the conversation to continue seamlessly.
```

---

## What LocalAI is NEVER Used For

| ? Prohibited Use | Reason |
|------------------|--------|
| PowerShell script generation | Security - requires Cloud AI reasoning |
| PowerShell execution | Security - Cloud AI validates scripts |
| AI reasoning/planning | Complexity - Cloud AI handles tool selection |
| Task orchestration | Complexity - Cloud AI manages multi-step plans |
| FreeResponse generation | Quality - Cloud AI provides comprehensive answers |

---

## Multi-Batch Architecture for File Reading

When `LocalAIUseSummarization` is ON and user requests file analysis:

```
???????????????????????????????????????????????????????????????????????????????
?                     ReadFileAndAnswer - MULTI-BATCH FLOW                     ?
???????????????????????????????????????????????????????????????????????????????
?                                                                              ?
?  STEP 1: FILE CHUNKING                                                       ?
?  ??> Split large file into ~500-word chunks                                  ?
?                                                                              ?
?  STEP 2: LOCALAI EXTRACTION (for each chunk)                                ?
?  ??> LocalAI extracts ONLY relevant facts                                   ?
?  ??> Prompt: "Extract ALL information relevant to the question..."          ?
?  ??> NO FINAL ANSWER - just facts, names, dates, quotes                     ?
?  ??> Store each extraction in memory list                                   ?
?                                                                              ?
?  STEP 3: CLOUD AI FINAL RESPONSE (ALWAYS!)                                  ?
?  ??> Combine all chunk extractions                                          ?
?  ??> Cloud AI generates comprehensive final answer                          ?
?  ??> Prompt: "There are NO LENGTH LIMITS - provide as detailed as needed"   ?
?  ??> UNLIMITED response length!                                             ?
?                                                                              ?
???????????????????????????????????????????????????????????????????????????????
```

**Key Insight:** LocalAI = Workhorse (reads chunks), Cloud AI = Final Answer (unlimited response)

---

## Status Updates (LabelStatusUpdate)

All functions that switch between LocalAI and Cloud AI now display real-time status:

| Function | LocalAI Status | Cloud AI Status | Fallback Status |
|----------|---------------|-----------------|-----------------|
| `ReadFileAndAnswer` | "LocalAI processing chunk X/Y..." | "Cloud AI processing..." | N/A (multi-batch) |
| `ProcessChunk` | "LocalAI extracting from chunk..." | "Cloud AI processing chunk..." | "Cloud AI processing chunk (LocalAI fallback)..." |
| `ProcessChunkUpdate` | "LocalAI updating chunk..." | "Cloud AI updating chunk..." | "Cloud AI updating chunk (LocalAI fallback)..." |
| `SummarizeMessagesAsync` | "LocalAI compressing conversation history..." | "Cloud AI compressing conversation history..." | "Cloud AI compressing history (LocalAI fallback)..." |
| `ReviseUserQuestionAsync` | "LocalAI rephrasing prompt..." | "Cloud AI rephrasing prompt..." | "Cloud AI rephrasing (LocalAI fallback)..." |

---

## Service Layer: LocalAITextService

The `LocalAITextService` module provides a centralized interface for all LocalAI text operations:

### Key Methods

| Method | Purpose | Mode Used |
|--------|---------|-----------|
| `GenerateWithModeAsync()` | Core generation with mode selection | Any |
| `SummarizeTextAsync()` | File/text summarization | Summarization |
| `SummarizeConversationAsync()` | Conversation history compression | ConversationHistory |
| `AnswerFromTextAsync()` | Answer questions about text | Summarization |
| `RephrasePromptAsync()` | Rephrase user prompts | Summarization |
| `IsAvailableForMode()` | Check if mode is enabled | Any |

### Availability Check

```vb
' Check if LocalAI is available for a specific mode
If LocalAITextService.IsAvailableForMode(LocalAIMode.Summarization) Then
    ' Use LocalAI
Else
    ' Fall back to Cloud AI
End If
```

---

## Settings & Persistence

### User Settings (Settings.vb)

| Setting | Type | Controls |
|---------|------|----------|
| `LocalAIUseSummarization` | Boolean | Roles 2 & 3 (Summarization + ConversationHistory) |
| `LocalAIIncludeTTS` | Boolean | Role 1 (Confirmation/TTS) - Independent |

### Globals Helper Methods

```vb
' Check if summarization is enabled AND LocalAI is ready
Globals.IsLocalAISummarizationEnabled() As Boolean

' Check if LocalAI engine is ready (model loaded)
Globals.IsSharedLocalAIReady() As Boolean

' Load/Save settings
Globals.LoadLocalAISettings()
Globals.SaveLocalAISettings()
```

---

## Debug Logging

All LocalAI operations are logged with `[LocalAI-TextService]` prefix:

```
[LocalAI-TextService] === LocalAI CALLED ===
[LocalAI-TextService] Mode: ConversationHistory
[LocalAI-TextService] Prompt length: 2345 chars
[LocalAI-TextService] System prompt mode: ConversationHistory
[LocalAI-TextService] Token limit: 384
[LocalAI-TextService] Response generated in 523ms
[LocalAI-TextService] Response length: 287 chars
[LocalAI-TextService] === LocalAI COMPLETE ===
```

---

## Files Modified/Created

| File | Changes |
|------|---------|
| `LocalAI\LocalAITextService.vb` | Added three-mode architecture with specialized prompts |
| `Operational\Ops\convHistory.vb` | Uses `SummarizeConversationAsync()` for history |
| `Operational\FunctionSetup\CustomFunctions_2.vb` | Multi-batch ReadFileAndAnswer with Cloud AI final |
| `Operational\FunctionSetup\FileHandler.vb` | Status updates for ProcessChunk/ProcessChunkUpdate |
| `Operational\Ops\ShellyOps.vb` | Status updates for ReviseUserQuestionAsync |
| `Settings.vb` | LocalAI summarization checkbox handling |

---

## Version History

| Version | Date | Changes |
|---------|------|---------|
| v1.0.0 | Initial | Single-mode LocalAI (Confirmation only) |
| v1.1.0 | Current | Three-mode architecture, multi-batch file reading, status updates |

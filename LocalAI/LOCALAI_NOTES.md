# Shelly Local AI (LLM + Piper TTS)

## Overview
- Purpose: Provide on-device LLM responses (LLamaSharp) plus Piper TTS playback.
- Scope: Isolated under `LocalAI/` with `LocalAIForm` UI and reusable pipeline helper.
- Default training: `LocalAI/Resources/localai_training.txt` (packaged with app; loaded relative to app base). Edit this file to change grounding instructions for all local models.

## Three-Role Architecture (NEW)

LocalAI now operates with **three distinct modes**, each with specialized system prompts:

| Mode | Enum Value | Trigger Setting | Purpose |
|------|------------|-----------------|---------|
| **Confirmation** | `LocalAIMode.Confirmation` | `LocalAIIncludeTTS` | Voice TTS confirmations |
| **Summarization** | `LocalAIMode.Summarization` | `LocalAIUseSummarization` | File/text extraction |
| **ConversationHistory** | `LocalAIMode.ConversationHistory` | `LocalAIUseSummarization` | Context compression |

### Mode Independence
- **Confirmation mode** runs INDEPENDENTLY (controlled by `LocalAIIncludeTTS`)
- **Summarization + ConversationHistory** are controlled by `LocalAIUseSummarization` checkbox

### Key Principle
**LocalAI = Workhorse (extraction), Cloud AI = Final Answer (generation)**

LocalAI extracts facts from chunks but NEVER generates the final user-facing response. Cloud AI always generates final answers with no token limits.

## Key Components
- `LocalAIEngine.vb`: LLM wrapper (LLamaSharp). Loads GGUF, builds prompts, injects training text, generates responses.
- `LocalAITextService.vb`: **NEW** - Three-mode service layer. Routes requests to appropriate system prompts based on mode.
- `PiperTTSEngine.vb`: Piper TTS wrapper. Auto-detects `piper.exe` + voices, plays audio via NAudio.
- `LocalAIForm.vb` (+ Designer): Production UI for selecting models/voices, saving/deleting model entries, submitting prompts, and auto-speaking responses.
- `LocalAIPipeline.vb`: One-call helper (`LocalAIAndVoiceAsync`) to generate + speak in a single method for reuse across Shelly flows.
- `Globals.vb` + `My.Settings`: Persist model list, voice list, and last selections across sessions.

## Persistence (Globals + My.Settings)
- Lists: `LocalAIModelPaths` (StringCollection), `LocalAIVoices` (StringCollection)
- Selections: `LocalAISelectedModel`, `LocalAISelectedVoice`
- Settings: `LocalAIUseGpu`, `LocalAIGpuLayerCount`, `LocalAIIncludeTTS`, `LocalAIUseSummarization`
- Load/save helpers: `Globals.LoadLocalAISettings()`, `Globals.SaveLocalAISettings()`
- Availability check: `Globals.IsLocalAISummarizationEnabled()` - checks both setting AND model loaded

## LocalAITextService Methods

| Method | Purpose | Mode |
|--------|---------|------|
| `GenerateWithModeAsync()` | Core generation with mode selection | Any |
| `SummarizeTextAsync()` | Summarize file/text content | Summarization |
| `SummarizeConversationAsync()` | Compress conversation history | ConversationHistory |
| `AnswerFromTextAsync()` | Answer questions about text | Summarization |
| `RephrasePromptAsync()` | Rephrase user prompts | Summarization |
| `IsAvailableForMode()` | Check if mode is available | Any |

## System Prompts by Mode

### Confirmation Mode
```
You are Shelly, a voice assistant. Give a brief spoken confirmation of what you just did. 
Say 'I' as if YOU did the work. Keep it to 1-2 sentences. No file paths or technical terms.
```

### Summarization Mode
```
You are a helpful AI assistant specialized in text analysis and summarization. 
Provide clear, concise, and accurate responses. Preserve key information and code blocks.
```

### ConversationHistory Mode
```
You are a conversation summarizer. Compress conversation history while preserving:
- User's original requests and intentions
- Key results and outcomes
- Code blocks (keep intact)
- File paths, URLs, and technical references
```

## Multi-Batch File Reading (ReadFileAndAnswer)

When `LocalAIUseSummarization` is enabled:

1. **Chunking**: File split into ~500-word chunks
2. **LocalAI Extraction**: Each chunk processed by `ProcessChunkWithLocalAI()`
   - Extracts ONLY relevant facts
   - Does NOT generate final answer
   - Returns "NO_RELEVANT_INFO" if chunk has nothing relevant
3. **Cloud AI Final**: `GenerateFinalResponseWithCloudAI()` combines extractions
   - NO LENGTH LIMITS on final response
   - Comprehensive, detailed answer

## Status Updates

All switching functions update `Shelly.Instance.LabelStatusUpdate.Text`:

| Function | LocalAI Status | Cloud AI Status |
|----------|---------------|-----------------|
| `ReadFileAndAnswer` | "LocalAI processing chunk X/Y..." | "Generating final response..." |
| `ProcessChunk` | "LocalAI extracting from chunk..." | "Cloud AI processing chunk..." |
| `ProcessChunkUpdate` | "LocalAI updating chunk..." | "Cloud AI updating chunk..." |
| `SummarizeMessagesAsync` | "LocalAI compressing conversation history..." | "Cloud AI compressing..." |
| `ReviseUserQuestionAsync` | "LocalAI rephrasing prompt..." | "Cloud AI rephrasing..." |

## LocalAIForm Behavior
- On load: loads persisted settings, populates model/voice dropdowns, auto-loads last model if available, initializes Piper voices.
- Browse: `browseMdoelButton` opens `.gguf` via `OpenFileDialog1`.
- Save: `saveModelButton` stores path, persists, refreshes list, and auto-loads the model.
- Delete: `deleteModelButton` removes selected model and clears selection if removed.
- Select model: changing `localAIModel` auto-loads that model.
- Voice select: updates Piper voice and persists selection.
- Submit: sends `userPrompt` to `LocalAIEngine`, writes response to `aiPrompt`, and auto-speaks via Piper if ready.
- Status label `_lblStatus` reflects load/generation states.

## Training File
- Location: `LocalAI/Resources/localai_training.txt` (copied/used at runtime). Falls back to app `Resources` or seeds default text if missing.
- Contents are injected into the system prompt for every local inference.

## Debug Logging

All LocalAI operations log with `[LocalAI-TextService]` prefix:
```
[LocalAI-TextService] === LocalAI CALLED ===
[LocalAI-TextService] Mode: Summarization
[LocalAI-TextService] Prompt length: 2345 chars
[LocalAI-TextService] Token limit: 512
[LocalAI-TextService] Response generated in 523ms
[LocalAI-TextService] === LocalAI COMPLETE ===
```

## How to use LocalAITextService (examples)

```vb
' Check availability first
If LocalAITextService.IsAvailableForMode(LocalAIMode.Summarization) Then
    ' Use LocalAI for extraction
    Dim result = Await LocalAITextService.SummarizeTextAsync(content)
Else
    ' Fall back to Cloud AI
End If

' Or with specific mode
Dim response = Await LocalAITextService.GenerateWithModeAsync(
    prompt:="Summarize this text...",
    mode:=LocalAIMode.Summarization,
    ct:=cancellationToken,
    maxTokens:=512
)
```

## Notes / Limits
- Piper must be present under `LocalAI/Piper/piper.exe` (or detected path). Voices auto-scanned; matching `.onnx` + `.json` required.
- Local models must be valid `.gguf` paths. Loading is synchronous per selection/save, guarded by semaphore in pipeline.
- LocalAI is NEVER used for PowerShell, reasoning, or final user responses.
- Multi-batch file reading always uses Cloud AI for final response (no token limits).
- Fallback to Cloud AI is automatic if LocalAI fails.

## Files Modified for Three-Role Architecture
- `LocalAI/LocalAITextService.vb` - New service layer with three modes
- `Operational/Ops/convHistory.vb` - Uses `SummarizeConversationAsync()`
- `Operational/FunctionSetup/CustomFunctions_2.vb` - Multi-batch `ReadFileAndAnswer`
- `Operational/FunctionSetup/FileHandler.vb` - Status updates for chunk processing
- `Operational/Ops/ShellyOps.vb` - Status updates for prompt revision
- `Settings.vb` - LocalAI summarization checkbox handling
- `Globals.vb` - `LocalAIUseSummarization` setting + `IsLocalAISummarizationEnabled()`

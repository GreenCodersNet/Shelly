# Local AI Setup Guide

## Overview

This module allows Shelly to use a local LLM for multiple purposes:

1. **Voice Confirmations (TTS)** - Short spoken responses after task completion
2. **File Summarization** - Extract information from large files locally (saves API costs!)
3. **Conversation History Compression** - Compress chat history locally

For detailed architecture information, see [LOCALAI_ARCHITECTURE.md](./LOCALAI_ARCHITECTURE.md).

---

## Quick Start: Enable LocalAI Features

### For Voice Confirmations (TTS)
1. Open **LocalAI Form** (from Settings)
2. Load a GGUF model
3. Check **"Include TTS"** checkbox
4. Select a Piper voice

### For File Summarization & History Compression
1. Load a model in LocalAI Form (as above)
2. Go to **Settings**
3. Check **"LocalAI Summarization"** checkbox

When enabled, you'll see status updates like:
- "LocalAI processing chunk 1/5..."
- "LocalAI compressing conversation history..."
- "Cloud AI generating final response..."

---

## Part 1: Local LLM (Text Generation)

### Prerequisites

1. **Download a GGUF Model**
   
   Recommended: `llama-3.2-3b-instruct` in GGUF format
   
   Download from: https://huggingface.co/lmstudio-community/Llama-3.2-3B-Instruct-GGUF
   
   Recommended quantization: `Q4_K_M` (~2GB) or `Q5_K_M` (~2.5GB)

2. **Place the Model File**
   
   Create a `Models` folder in Shelly's directory and place the `.gguf` file there:
   ```
   Shelly/
   ??? Models/
   ?   ??? llama-3.2-3b-instruct-Q4_K_M.gguf
   ??? ShellyAI.exe
   ??? ...
   ```

### Testing the Local AI

1. Open Shelly
2. Go to **Settings** ? **Local AI** (or run the test form directly)
3. Click **Browse** and select your `.gguf` model file
4. Click **Load Model** (first load takes 10-30 seconds)
5. Type a test prompt and click **Submit**

---

## Part 2: Piper TTS (Text-to-Speech)

### What is Piper?

Piper is a fast, high-quality neural text-to-speech system that runs 100% locally.
- No internet required
- Multiple voices available
- Sounds much better than Windows built-in TTS

### Setup Piper TTS

1. **Download Piper**
   
   Download from: https://github.com/rhasspy/piper/releases
   
   Get the Windows release: `piper_windows_amd64.zip`

2. **Download Voice Models**
   
   Download from: https://github.com/rhasspy/piper/blob/master/VOICES.md
   
   Recommended voices:
   | Voice | Description | Download |
   |-------|-------------|----------|
   | `en_US-amy-medium` | Female, American | [Link](https://huggingface.co/rhasspy/piper-voices/tree/main/en/en_US/amy/medium) |
   | `en_US-ryan-medium` | Male, American | [Link](https://huggingface.co/rhasspy/piper-voices/tree/main/en/en_US/ryan/medium) |
   | `en_GB-alan-medium` | Male, British | [Link](https://huggingface.co/rhasspy/piper-voices/tree/main/en/en_GB/alan/medium) |
   | `en_US-lessac-medium` | Female, Very Natural | [Link](https://huggingface.co/rhasspy/piper-voices/tree/main/en/en_US/lessac/medium) |

3. **Folder Structure**
   
   Set up Piper like this:
   ```
   Shelly/
   ??? Piper/
   ?   ??? piper.exe
   ?   ??? piper_phonemize.dll
   ?   ??? espeak-ng-data/
   ?   ??? voices/
   ?       ??? en_US-amy-medium.onnx
   ?       ??? en_US-amy-medium.onnx.json
   ?       ??? en_US-ryan-medium.onnx
   ?       ??? en_US-ryan-medium.onnx.json
   ??? ShellyAI.exe
   ??? ...
   ```

   **Or** use a custom location like:
   ```
   E:\Shelly\LocalAIs\Piper\
   ??? piper.exe
   ??? voices/
       ??? ...
   ```

4. **Voice File Requirements**
   
   Each voice needs **two files**:
   - `voice-name.onnx` - The voice model
   - `voice-name.onnx.json` - The configuration file
   
   Both files must be in the `voices` folder.

### Using TTS in the Test Form

1. If Piper is set up correctly, the **"?? Speak Response"** checkbox will be enabled
2. Select a voice from the dropdown
3. Check the checkbox
4. Submit a prompt - the AI response will be spoken automatically
5. Click **Stop** to interrupt speech

---

## Part 3: LocalAI Summarization (NEW!)

### What It Does

When **"LocalAI Summarization"** is enabled in Settings:

| Feature | LocalAI Role | Cloud AI Role |
|---------|-------------|---------------|
| **Read Large Files** | Extracts facts from each chunk | Generates final comprehensive answer |
| **Conversation History** | Compresses old messages | N/A (LocalAI handles fully) |
| **Prompt Rephrasing** | Restructures user prompts | Fallback if LocalAI fails |

### Benefits

- **Cost Savings:** LocalAI handles the bulk reading/extraction work
- **Unlimited Responses:** Cloud AI generates final answer with no token limits
- **Real-time Status:** See which AI is processing at any moment

### How Multi-Batch File Reading Works

```
User: "Who are the main characters in story.docx?"

1. File split into 5 chunks
2. LocalAI extracts from chunk 1/5... ?
3. LocalAI extracts from chunk 2/5... ?
4. LocalAI extracts from chunk 3/5... ?
5. LocalAI extracts from chunk 4/5... ?
6. LocalAI extracts from chunk 5/5... ?
7. Cloud AI generating final response...
8. Done! (comprehensive answer, no length limits)
```

---

## System Requirements

### For Local LLM
| Component | Minimum | Recommended |
|-----------|---------|-------------|
| RAM | 4 GB free | 8 GB free |
| CPU | x64 processor | 4+ core CPU |
| Storage | 3 GB | SSD preferred |

### For Piper TTS
| Component | Requirement |
|-----------|-------------|
| RAM | ~100 MB |
| CPU | Any x64 |
| Storage | ~100 MB per voice |

---

## Troubleshooting

### LLM Issues

#### "Model failed to load"
- Ensure the `.gguf` file is not corrupted (re-download if needed)
- Check you have enough free RAM
- Try a smaller quantization (Q4_K_S instead of Q5_K_M)

#### "Response is very slow"
- This is normal for CPU inference (5-30 seconds)
- Consider using a smaller model

#### "LocalAI summarization failed, falling back to cloud AI"
- This is expected behavior - the system automatically falls back
- Check that your model is loaded in LocalAI Form

### TTS Issues

#### "Piper not found" in dropdown
- Check that `piper.exe` exists in one of these locations:
  - `[App Directory]/Piper/piper.exe`
  - `E:\Shelly\LocalAIs\Piper\piper.exe`
- Make sure all DLL files from the Piper release are present

#### "No voices found"
- Check that voice files (`.onnx` + `.onnx.json`) are in the `voices` folder
- Both the model file and config file must be present

#### No sound plays
- Check Windows sound settings
- Ensure the voice model downloaded correctly

---

## Notes

- Local AI is **optional** - Shelly works fine without it
- If local AI fails, Shelly automatically falls back to cloud AI
- LocalAI is NEVER used for PowerShell or reasoning tasks (security)
- Model and voice files are NOT included in the app download

---

## Related Documentation

- [LOCALAI_ARCHITECTURE.md](./LOCALAI_ARCHITECTURE.md) - Detailed three-role architecture
- [LOCALAI_NOTES.md](./LOCALAI_NOTES.md) - Developer notes and implementation details

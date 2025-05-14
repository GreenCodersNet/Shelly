# ShellyAI – Your AI-Powered Windows Assistant (VB.NET Proof of Concept)

**ShellyAI** is a next-generation **AI automation assistant for Windows**, built with **VB.NET** and designed to show what’s possible when ChatGPT-style intelligence meets real-world automation.

> ⚠️ Shelly is a **proof of concept**, not a finished product — but it already demonstrates how AI can manage real PC tasks, run scripts, and interact with the OS securely and intelligently.

---

## 💡 What Is Shelly?

Shelly is an experimental AI agent designed to:
- Interpret user prompts
- Segment them into logical steps
- Run AI-generated **PowerShell scripts**
- Execute predefined **Custom Functions**
- Manage **conversation history** intelligently across steps
- Control **system-level tasks** via AI
- Respond with formatted content (e.g., generate files, rename folders, summarize documents)

---

## ✨ Key Features

| Feature                         | Description |
|-------------------------------|-------------|
| 🧠 **Multistep AI Planning**  | Breaks down prompts into task segments (e.g., "Download file → Rename → Explain content") |
| ⚡ **PowerShell Execution**    | Executes AI-generated scripts directly on your system, safely |
| 🧩 **Custom Function Support** | Lets you define and execute reusable VB.NET functions (e.g., open apps, log results, display alerts) |
| 🔗 **Chained Task Results**    | Each step's result is fed into the next one to preserve context |
| 🌐 **WebView2 Integration**    | Used for scraping, displaying results, and interacting with websites |
| 📦 **Secure API Key Storage** | API keys are encrypted and never hardcoded |
| 🔍 **Live Debug Logging**     | Internal logs shown in real time via a custom Console panel |
| 🧪 **Rephrasing + Token Trim**| Auto-optimizes prompts to save tokens when needed |

---

## 🧪 Why a Proof of Concept?

Shelly is a **research-driven prototype** meant to test:

- Whether AI can automate real-world computing tasks on a desktop
- How to bridge large language models with local system access
- Whether AI-generated scripts can safely and reliably run without user retyping

It’s an evolving platform to explore **AI x OS integration** — not yet production-grade, but already exciting.

---

## 🔧 Getting Started

### Prerequisites
- Windows 10 or 11
- Visual Studio 2022+
- .NET 8
- A valid OpenAI API key (GPT-4o recommended)

### Clone and Run

```bash
git clone https://github.com/GreenCodersNet/ShellyAI.git
```

1. Open `ShellyAI.sln` in Visual Studio
2. Restore NuGet packages (Newtonsoft.Json, NAudio, etc.)
3. Set project as startup and run (`F5`)
4. Enter your API key via the app settings (stored securely)

---

## 🛡️ Safety and Control

- The **main branch is protected** — only tested changes are merged.
- Users can create **feature branches** and submit pull requests.
- All PowerShell execution is tracked and optionally sandboxed.

---

## 🤝 Contributing

Interested in building smarter assistants?

1. Fork this repo
2. Add new tools, scripts, or function integrations
3. Submit a PR targeting `dev` branch
4. We’ll test and review before any merge to `main`

---

## 📚 Learn More

- [GreenCoders.net/Story](https://greencoders.net/story/) — our philosophy
- [Shelly Prototype Walkthrough (Coming Soon)] — use cases & tech dive
- [Assistant Integration Examples] — real-world scenarios

---

## 🧠 Roadmap Ideas

- AI-generated UI responses
- Local AI (offline support)
- Script approval sandbox
- Plugin system (like VS Code extensions)

---

## ⚖️ License

Licensed under [Creative Commons Attribution-NonCommercial 4.0](https://creativecommons.org/licenses/by-nc/4.0/)

---

## 🌱 Built by [GreenCoders](https://greencoders.net) with ❤️ for sustainable, intelligent software.

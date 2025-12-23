# Shelly — AI Orchestrator for Windows

**Version:** 2.x (Windows, .NET 8+ WinForms)
**License:** Creative Commons Attribution-NonCommercial (CC BY-NC)
**Author:** Vlad Stefanescu | GreenCoders.net

## What Shelly Is
Shelly is a Windows desktop assistant that plans and executes multi-step tasks. It blends:
- An AI planner (OpenAI chat models) that emits JSON plans of tool calls.
- A rich catalog of compiled VB custom functions for speed and safety.
- A guarded PowerShell runner for system automation when no function fits.
- Iterative context passing (retry/adjust/finish) until the user’s goal is met.

Shelly is not a chatbot; it is a task solver with guardrails, outcome tracking, and dynamic replanning.

## Core Capabilities
- **Multi-step planning:** Generates, validates, and executes ordered tool steps; iterates up to 10 times with feedback.
- **Context-aware retries:** Reuses prior step outputs to branch, retry, or add follow-up actions.
- **Custom functions first:** File read/search, image gen/analysis, screenshot QA, web search/scrape, typing into windows, volume/media control, script generation, batch/PS emitters, large file writers.
- **Guarded PowerShell:** Constrained/blocked surfaces (network/env/jobs/system paths) plus heuristic + GPT remediation loop.
- **Free-response:** Falls back to natural-language answers when no tools are needed.
- **Caching:** File contents cached in RAM to reduce token use.
- **Logging/telemetry:** Execution outcomes recorded for planner feedback and debugging.

## High-Level Flow
1) **Input** — User prompt (or speech) captured by `Shelly.vb` UI.
2) **Request init** — `HandleUserRequestAsync` sets request ID, resets retry & ledgers, trims history.
3) **Planning** — Planner prompt (tools JSON, prior outcomes, history, retry hints) ? `AIcall.CallGPTCore` ? JSON plan.
4) **Validation** — `ToolSchemaValidator` checks tool names, types, ranges, required params.
5) **Execution** — `ExecutorAgent` routes each step:
   - Custom function via `CustomFunctionsEngine.ExecuteAppFunctionAsync`.
   - PowerShell via `ExecutePowerShellScriptAsync` with safety gates.
   - FreeResponse writes directly to UI.
6) **Outcome capture** — `ExecutionOutcome` + `StepOutputManager` store outputs per iteration/step.
7) **Goal check** — `GoalValidator` compares requested vs. completed actions; triggers summary for multi-step runs.
8) **Iterate or finish** — Up to 10 iterations; final summary shown.

## Key Files & Responsibilities
- **UI / Orchestration**
  - `Shelly.vb` — Main form, lifecycle, cancellation, cleanup.
  - `HandleUserRequest.vb` — Core loop (plan/execute/retry), conversation trim, summary trigger.
  - `ConversationHistoryFunctions.vb`, `convHistory.vb` — History helpers and token trimming.
- **Planning & Tooling**
  - `Operational\Ops\ToolPlanner.vb` — Tool list JSON for planner prompt.
  - `Operational\Ops\PlanStep.vb` — Plan DTO.
  - `Operational\Ops\ToolSchema.vb`, `ToolSchemaValidator.vb` — Schemas, validation (types/ranges/paths/required/approval flags).
- **Execution & Outcomes**
  - `Operational\Ops\ExecutorAgent.vb` — Executes steps, argument normalization, outcome recording.
  - `Operational\Ops\ExecutionOutcome.vb`, `OutcomeHistory.vb`, `StepOutputManager.vb`, `RetryStrategy.vb`, `GoalValidator.vb` — Outcome tracking, digests, retries, goal detection.
  - `Operational\Ops\Globals.vb` — Session/model settings, caches, flags.
- **Custom Functions**
  - `Operational\FunctionSetup\CustomFunctions.vb` — Media keys, volume, image gen/analysis, screenshot capture/analysis, batch+ps1 generation, large-file generation.
  - `Operational\FunctionSetup\CustomFunctions_2.vb` — File read/QA, typing into windows, web search/scrape, text search, etc.
  - `Operational\FunctionSetup\CustomFunctionsEngine.vb` — Reflection-based registration, signature parsing, function dispatch.
  - `Operational\FunctionSetup\FileHandler.vb`, `generateFiles.vb` — File IO (pdf/docx/xlsx/pptx/text), chunking, Office COM helpers, clipboard/paste typing, PowerPoint builder.
- **PowerShell**
  - `PowerShell.vb` — Script execution, constrained mode, cancellation, remediation loop, dedupe of runs.
  - `Operational\Ops\PowerShellSafety.vb`, `PowerShellRemediation.vb`, `PowerShellSafety` form — Safety inspection, flags UI, heuristic fixes.
- **AI IO**
  - `AIcall.vb` — Chat completion wrapper with token budgeting and model handling (reasoning vs standard models).
  - `AIBrainiac.vb` — Assistant-based calls (conversation aware).
  - `AIimage.vb` — Image gen/vision (DALL-E, GPT-4.x vision), download helpers.

## Tool Catalog (selected)
- **File / Text**: `ReadFileAndAnswer`, `SearchForTextInsideFiles`, `GenerateLargeFileWithTextOrCode`, `UpdateFileByChunks`, `WriteInsideFileOrWindow`.
- **Web**: `WebSearchAndRespondBasedOnPageContent` (Google + scrape + QA), `ReadWebPageAndRespondBasedOnPageContent`.
- **Images**: `GenerateImages` (DALL-E), `ImageAnswer`, `CheckMyScreenAndAnswer`, `TakePrintScreenOrScreenShot`.
- **System**: `StartOrRunApplicationByName`, `ChangeOrSetVolume`, `SendMediaKey`.
- **Automation**: `GenerateBatchAndPs1File` (paired .bat/.ps1 with safe encodings).
- **Free text**: `FreeResponse` for direct natural-language answers.

## Safety Model
- **Schema guardrails**: Type/range/path checks, approval flags for high-risk tools (delete/move/copy/PS, services, scheduled tasks).
- **PowerShell gates**: Blocklists for destructive verbs, system paths, optional blocks for network/env/jobs/C:\. Constrained Language Mode optional. Scripts inspected before run; blocked scripts surfaced to user.
- **Remediation loop**: Up to 5 attempts; heuristic fixes then GPT repair; stops on repeated failure or safety block.
- **Execution ledger**: Prevents duplicate function/powershell calls per request.
- **API key safety**: `SecureStorage` uses DPAPI; `Globals.UserApiKey` always decrypted on read, encrypted on save.

## Iteration & Context
- `StepOutputManager` persists every step’s output per request/iteration; planner prompt always includes these summaries.
- `OutcomeHistory` supplies recent execution digest; `GoalValidator` maps user intent to required actions (search/summarize/open/generate_image/etc.).
- `RetryStrategy` caps retries per tool (default 3) and total iterations (10).

## Models
- User-selectable via settings (`Globals.AiModelSelection`), with reasoning model handling in `AIcall`.
- Fixed overrides: image generation ? `dall-e-3`; image/screen analysis ? vision (e.g., `gpt-4.1-mini`).

## How to Use (end user)
1. Enter a natural-language request (e.g., “Search C:\Docs for budget.xlsx and summarize it”).
2. Click **Run** (or use speech). Shelly plans and executes steps automatically.
3. Watch the Results panel; cancel anytime. Multi-step summaries appear after completion.
4. Adjust PowerShell safety and model selection in Settings.

## Extending Shelly (developer quick path)
1. Add a function in `CustomFunctions.vb` or `CustomFunctions_2.vb` (use `<CustomFunction>` for metadata).
2. Register schema in `ToolSchema.vb` (types, ranges, required params, risk flags).
3. Ensure executor dispatch in `ExecutorAgent` (add case or rely on generic dispatch).
4. Update planner training/tool list if needed (`ToolPlanner`, training resource) so AI can call it.
5. If PowerShell-based, add safety considerations and schema requirements.

## Logging & Telemetry
- Outcomes: `GlobalOutcomeTracker`/`ExecutionOutcome` with statuses, outputs, errors.
- Step outputs: `StepOutputManager` for planner context.
- Debug logs: `Globals.AppendDebugLog`; clear via UI.
- Interaction log: per-run JSONL at `Logs/interaction-log.jsonl` (relative to the executable). Cleared on startup; records each user prompt and assistant reply.

## Licensing
Creative Commons Attribution-NonCommercial (CC BY-NC 4.0). Commercial use is not permitted. Attribution: Vlad Stefanescu | GreenCoders.net.

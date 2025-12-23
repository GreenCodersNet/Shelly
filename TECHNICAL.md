# Shelly Technical Overview

Shelly is a Windows assistant that plans, validates, executes, and adapts multi-step workflows. It mixes three pillars:
- **Reasoning:** GPT-based planner generates JSON plans, iterates with feedback.
- **Custom Functions:** Compiled VB functions for fast, deterministic operations (files, web, images, UI typing, media/system controls).
- **PowerShell:** Sandboxed scripts for system queries/automation when no custom function fits.

## Request Lifecycle
1. **Capture:** User prompt is read (or speech ? text). Request ID + iteration counters are set.
2. **History trim:** Conversation history is trimmed to fit token budgets.
3. **Planning:** Planner prompt includes tool catalog JSON, prior step outputs (`StepOutputManager`), recent outcomes (`OutcomeHistory`), retry stats, and original request. AI returns a JSON array of steps (tools + args). If it returns a single `FreeResponse`, it can end immediately.
4. **Validation:** `ToolSchemaValidator` enforces required params, types, ranges, and path rules. High-risk tools (PS, delete/move/copy, services, tasks) carry approval flags.
5. **Execution:** `ExecutorAgent` runs steps sequentially.
   - **Custom Functions first:** Routed via `CustomFunctionsEngine` to VB methods in `CustomFunctions.vb` / `CustomFunctions_2.vb`.
   - **PowerShell:** `PowerShell.vb` inspects safety (`PowerShellScriptSafety`, `SecurityFlags`) and executes with constrained/job/network/env/system-path guards; remediation loop can request GPT fixes.
   - **FreeResponse:** Writes plain text to UI.
6. **Outcome recording:** `ExecutionOutcome` + `StepOutputManager` store outputs/statuses per iteration/step for planner feedback.
7. **Goal check:** `GoalValidator` maps requested actions (search/summarize/open/generate_image/etc.) to completed tool outcomes. If incomplete and under iteration/retry limits, Shelly replans with fresh context.
8. **Summary:** For multi-step runs, a final `FreeResponse` summary is produced.

## Single-Step vs Multi-Step
- **Single-step task:** Planner may emit one tool or a `FreeResponse`. After execution and goal check, Shelly stops. Example: “What’s 2+2?” ? `FreeResponse`.
- **Multi-step task:** Planner emits multiple steps or iterates: execute ? record outputs ? replan using outputs/errors ? continue until goal achieved or limits hit.

## Adaptation Mechanics
- **Context reuse:** `StepOutputManager` surfaces prior outputs to the planner (e.g., lists of files, URLs, errors).
- **Retry limits:** `RetryStrategy` caps per-tool retries (default 3) and total iterations (default 10).
- **Execution ledger:** Prevents duplicate function/PS invocations per request.
- **Outcome-aware planning:** Planner sees successes/failures and can branch, fix arguments, or switch tools.

## Custom Functions (highlights)
- Files/Text: `ReadFileAndAnswer`, `SearchForTextInsideFiles`, `GenerateLargeFileWithTextOrCode`, `UpdateFileByChunks`, `WriteInsideFileOrWindow`.
- Web: `WebSearchAndRespondBasedOnPageContent`, `ReadWebPageAndRespondBasedOnPageContent`.
- Images/Screen: `GenerateImages` (DALL-E), `ImageAnswer`, `CheckMyScreenAndAnswer`, `TakePrintScreenOrScreenShot`.
- System/Media: `StartOrRunApplicationByName`, `ChangeOrSetVolume`, `SendMediaKey`.
- Automation: `GenerateBatchAndPs1File`.

## PowerShell Pipeline
- **Safety gates:** Destructive verb/path checks, optional blocks for network/env/jobs/C:\, optional Constrained Language Mode. Blocked scripts surface errors and do not run.
- **Execution:** `ExecutePowerShellScriptAsync` with cancellation support. Remediation loop can apply heuristic fixes or GPT-suggested corrections (up to 5 attempts).

## Example: "Find any file inside this folder [path] that talk about ‘ww2’"
1) **Plan:** AI proposes steps:
   - `ExecutePowerShellScript` to list files in the folder.
   - For each file returned, `ReadFileAndAnswer(filePaths=<file>, query="Does it mention ww2?")`.
2) **Execute:**
   - PS runs and outputs a file list; output is stored in `StepOutputManager`.
   - Planner sees the list and generates one `ReadFileAndAnswer` per file (iteration 2).
3) **Aggregate & respond:**
   - Successful matches are recorded; errors (e.g., unreadable files) are also recorded.
   - Planner returns `FreeResponse` summarizing files that reference ww2.
4) **Adaptation:**
   - If a file failed to read, next iteration can skip or adjust (e.g., different encoding or path fix) within retry limits.

## Logging & Telemetry
- Outcomes: `GlobalOutcomeTracker` / `ExecutionOutcome` with statuses, outputs, errors.
- Step outputs: `StepOutputManager` feeds planner context.
- Debug logs: `Globals.AppendDebugLog`; clear via UI.
- Interaction log: per-run JSONL at `Logs/interaction-log.jsonl` (adjacent to the executable). Cleared on startup; captures each user prompt and assistant reply.

## Notes for Contributors
- Add new functions in `CustomFunctions*.vb`, register schemas in `ToolSchema`, and ensure executor dispatch.
- Prefer Custom Functions over PowerShell; only use PS when no function covers the need.
- Keep safety flags and validation intact; never bypass schema checks.
- Image gen uses `dall-e-3`; image/screen analysis uses vision models (e.g., `gpt-4.1-mini`) regardless of user selection.

## Files to Review First
- Orchestration: `Shelly.vb`, `HandleUserRequest.vb`
- Planner/Validation: `ToolPlanner.vb`, `ToolSchema.vb`, `ToolSchemaValidator.vb`
- Execution/Outcomes: `ExecutorAgent.vb`, `ExecutionOutcome.vb`, `OutcomeHistory.vb`, `StepOutputManager.vb`, `GoalValidator.vb`, `RetryStrategy.vb`
- Functions: `CustomFunctions.vb`, `CustomFunctions_2.vb`, `CustomFunctionsEngine.vb`
- PowerShell: `PowerShell.vb`, `PowerShellSafety.vb`
- AI I/O: `AIcall.vb`, `AIBrainiac.vb`, `AIimage.vb`

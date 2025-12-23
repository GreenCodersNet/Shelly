# Shelly Visual Schema (Pipelines + Examples)

## Core Pipeline (per request)
```
User Prompt
  ?
Conversation History (trimmed; prior runs + step outputs)
  ?
Planner (GPT) ? JSON steps [tool + args]
  ?
Schema Validation (ToolSchemaValidator)
  ?
ExecutorAgent (per step)
   ?? Custom Functions (prefer)
   ?? PowerShell (guarded; remediation loop)
   ?? FreeResponse (text)
  ?
StepOutputManager + OutcomeTracker (record outputs/status)
  ?
GoalValidator + RetryStrategy (branch/adjust/retry up to limits)
  ?
If not done: Replan using recorded outputs/errors
  ?
Final Summary (FreeResponse)
  ?
Conversation History updated
```

## Roles of Key Parts
- **Conversation History:** Stores prior user/assistant turns and execution summaries; fed to planner so it adapts instead of repeating.
- **StepOutputManager:** Per-request/iteration/step outputs; planner consumes to branch (e.g., lists of files/URLs, errors).
- **OutcomeTracker:** ExecutionOutcome log (status, errors, timings) for retry/goal checks.
- **GoalValidator:** Maps requested actions (search/summarize/open/generate_image/etc.) to completed outcomes; stops only when all actions done.
- **RetryStrategy:** Caps retries per tool and total iterations.
- **Safety:** PowerShell inspection (blocked verbs/paths, optional network/env/jobs/system blocks, ConstrainedLanguage), validation of schemas/paths/types.

## Example 1: "Hello, how are you?"
- Planner: Single-step `FreeResponse`.
- Validation: OK.
- Execution: Writes text to UI.
- Outcome: Success; history updated.

## Example 2: "What time is it now?"
- Planner: `ExecutePowerShellScript` to get system time.
- Validation: Script arg present.
- Execution: PowerShell (guarded) returns time.
- Outcome: Success; history stores time for later reference.

## Example 3: "Generate an image of a cute dog"
- Planner: `GenerateImages` (Custom Function).
- Validation: imagePrompt/numImages/style/folderPath.
- Execution: DALL-E via `AIimage`; saves file(s); records paths.
- Outcome: Success; history holds saved paths for follow-up.

## Example 4: "Read this file and generate an image to illustrate the file content. Then search online for HP laptops and show me each product found and their prices."
- Planner (iteration 1):
  1) `ReadFileAndAnswer(filePaths=<file>, query="Summarize/describe")`
  2) `GenerateImages(imagePrompt=<summary>, style=<default>, folderPath=...)`
  3) `WebSearchAndRespondBasedOnPageContent(promptQuery="HP laptops", siteName=..., question="List products + prices")`
  4) `FreeResponse` to combine outputs.
- Validation: Each step checked (types/paths/required params).
- Execution:
  - Step 1 reads file (cached in RAM) ? summary stored in StepOutputManager.
  - Step 2 generates image(s) from summary ? file paths recorded.
  - Step 3 web-searches/scrapes ? product/price list.
  - Step 4 FreeResponse aggregates summary + image paths + products/prices.
- Adaptation: If a step fails (e.g., file read), planner sees the error/output and can adjust or retry within limits.

## Why Shelly Adapts (Not Blind Planning)
- Planner prompt always includes previous step outputs + recent outcomes + retry stats.
- Execution ledger prevents duplicates; failures surface to planner for branch/fix.
- GoalValidator enforces all requested actions complete before stopping.

## Tool Preference
- Custom Functions > PowerShell > FreeResponse.
- Fixed models: DALL-E for image gen; vision model for image/screen analysis.

## Safety Snapshot
- Schema validation (types/ranges/paths/required/approval flags).
- PowerShell gates: blocked destructive verbs/paths, optional network/env/jobs/C:\ blocks, ConstrainedLanguage option, remediation loop.
- API key via DPAPI (`SecureStorage`).

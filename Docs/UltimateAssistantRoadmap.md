# Shelly Modernization Roadmap

## 1. PowerShell Structural Validation & Telemetry
- Parse AI-generated scripts with `System.Management.Automation.Language.Parser` to detect syntax errors and sensitive cmdlets before launching PowerShell.
- Add a dry-run policy engine to flag destructive commands (e.g., `Remove-Item`, registry edits) unless a per-capability allowlist approves them.
- Capture full telemetry for every run: script hash, arguments, start/end timestamps, stdout/stderr, exit code, attempt number, and store it with the associated `PlanStep`.

## 2. Deterministic Remediation Loop
- Build a remediation pipeline that records failure context (`command`, `exitCode`, stderr, relevant stdout tail) and feeds structured data back to GPT for fixes.
- Implement automatic retries for common issues (missing `-ErrorAction Stop`, quoting problems, constrained language toggles) before escalating to GPT.
- Log each remediation attempt as a discrete plan outcome so future iterations avoid repeating failed strategies.

## 3. Task Outcome Tracking & Planner Feedback
- Extend `PlanStep` to include an `Outcome` object (`Status`, `Artifacts`, `Metrics`) populated after each tool runs.
- Persist outcome summaries in conversation history and on disk so the planner can chain dependent steps and resume after restarts.
- Provide UI surfacing (timeline/journal) to show users what succeeded, failed, or produced artifacts.

## 4. Broader Windows Capability Surface
- Add custom functions for filesystem automation (copy/move/delete with confirmations), registry management, service/process control, scheduled tasks, network diagnostics, package inventory, and printer management.
- Document each function with schema metadata (inputs, side effects, emitted artifacts) so the planner can select them intelligently.

## 5. Tool Metadata & Planner Intelligence
- Create a formal tool registry (`ToolCapabilities.json`) describing capability tags, prerequisites, risk levels, and output types.
- Update `ToolPlanner` to reason over this registry, ensuring FreeResponse is used only when no actionable tools remain.
- Introduce guardrails to prevent redundant steps by checking outcome history before emitting a new plan segment.

## 6. Conversation & History Robustness
- Consolidate token counting logic (single implementation shared by `Shelly` and `convHistory`) to avoid inconsistent trimming.
- Persist trimmed history plus recent outcomes/artifacts to disk and restore them on launch for long-running tasks.
- When summarizing history, maintain speaker roles and references to stored outcomes so context remains actionable.

## 7. UX & Transparency Improvements
- Enhance the UI with a real-time plan timeline showing each step, telemetry snapshot, and produced files/screenshots.
- Offer optional “view script” and “approve high-risk action” toggles to balance automation with transparency.
- Provide user-facing summaries after multi-step runs that cite concrete outcomes and link to generated artifacts.

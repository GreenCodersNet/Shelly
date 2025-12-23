# Shelly V2 Update Plan

## Goal
Upgrade Shelly to V2 architecture to improve robustness, safety, and maintainability. The primary focus is removing brittle string parsing logic, enforcing structured AI outputs, and securing telemetry.

## Phase 1: Core Architecture Refactor (Invocation Pipeline)
**Objective:** Eliminate the "String Trip" anti-pattern where arguments are converted to strings and back, causing parsing errors with complex inputs.

### Steps
1.  **Update `CustomFunctionsEngine.vb`**:
    -   Create a new method `ExecuteAppFunctionDirectAsync` that accepts `functionName` and `args As Dictionary(Of String, Object)`.
    -   Implement reflection logic to map dictionary keys to method parameters by name (case-insensitive).
    -   Handle type conversion (e.g., JSON numbers to Integer/Double).
    -   Support optional parameters.
2.  **Update `ExecutorAgent.vb`**:
    -   Refactor `ExecuteCustomFunction` to skip `ConvertArgsToSignature`.
    -   Pass the `planStep.Args` dictionary directly to the new engine method.
3.  **Cleanup**:
    -   Remove `ConvertArgsToSignature` and `SplitParameters` helper functions once verified.

## Phase 2: Planner Reliability (Structured Outputs)
**Objective:** Stop relying on Regex to parse JSON from AI responses. Use the model's native capabilities to guarantee valid execution plans.

### Steps
1.  **Update `AIcall.vb`**:
    -   Modify `CallGPTCore` to support `response_format: { "type": "json_object" }` (for OpenAI models).
    -   Ensure the system prompt explicitly demands JSON output when this mode is active.
2.  **Update `HandleUserRequest.vb`**:
    -   Remove the fallback logic that tries to parse "multi-task" responses via Regex if JSON parsing fails.
    -   Enforce strict JSON validation. If the planner fails to produce JSON, trigger a retry with a correction prompt.

## Phase 3: Security & Telemetry
**Objective:** Ensure user safety and privacy.

### Steps
1.  **Telemetry Audit (`TelemetryStorage.vb`)**:
    -   Review data being sent to cloud/logs.
    -   Hash or redact file paths, script contents, and user prompts before storage.
2.  **PowerShell Safety Upgrade (`PowerShell.vb`)**:
    -   Improve `IsPowerShellScriptSafe` to use tokenization (AST) instead of simple string `Contains` matching if possible, or at least expand the blocklist to catch obfuscation attempts.

## Phase 4: Testing & Validation
1. **Unit Tests**: TODO – add tests for `ExecuteAppFunctionDirectAsync` (complex args) and PowerShell safety guards when test project is available.
2. **Regression Testing**: TODO – verify core tools (File Read, Image Gen) after adding tests.

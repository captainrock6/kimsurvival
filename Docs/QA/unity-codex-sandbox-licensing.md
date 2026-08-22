# Unity Licensing popup under Codex on Windows

## Symptom

- `Unity.Licensing.Client.exe` shows application error `0xe0434352` while Codex runs Unity.
- The Unity log can also report that the Package Manager cannot open its SQLite database or connect over IPC.

## Confirmed cause in this workspace

The Editor, Hub licensing clients, and installed entitlement were valid. The popup was reproduced only when Unity ran inside the restricted Codex filesystem/process sandbox. The licensing clients then failed to read WMI machine identifiers (`System.Management.ManagementException: Access denied`), while Unity Package Manager could not use its normal AppData cache.

This is an execution-environment failure, not evidence of a corrupt Unity license.

## Required execution policy

1. Run all Unity Editor batch, Play Mode, build, and Windows Player smoke commands outside the Codex sandbox using explicit escalation.
2. Reuse the exact installed Editor executable path so approval remains narrowly scoped.
3. Do not retry the same Unity command inside the sandbox, use `-noUpm` as a workaround, delete license/cache data, or replace signed licensing binaries.
4. Keep Unity logs under `work/` and durable QA evidence under `Artifacts/ParallelQA/<run-id>/`.

## Verification

The Wave 4 integrated run completed script compilation, Edit Mode, Play Mode, a Windows x64 Development build, and a hidden Windows Player smoke after Unity was launched outside the sandbox. The licensing popup did not recur in that execution mode.

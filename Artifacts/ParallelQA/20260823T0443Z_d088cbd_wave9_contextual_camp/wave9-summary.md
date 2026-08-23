# Wave 9 contextual camp interaction verification

- Baseline: `d088cbdf021765a811ed88af9b22b58db49b917c`
- Unity: `6000.4.9f1`
- Execution: Unity Editor, Play Mode, Windows build, and player smoke ran outside the Codex sandbox.

## Product contract

- Deterministic Edit checks: **PASS**
- Full product Play Mode loop: **PASS**
- Normal camp: legacy `campActions` dashboard and large camp bag panel hidden
- Distance states: far hides prompt/popup; near shows one prompt; interact opens only the selected facility popup
- Target resolution: distance + facing direction + current-target hysteresis
- Modal behavior: movement locked; cancel/action returns to the same field position
- Atomic behavior: popup confirmation is consumed once; `GameSession` changes resources only on successful actions
- Popup ownership: workbench, campfire, rain collector, and rescue signal actions remain separated
- Preserved regression path: limited placement, free relocation, signal anchor, rain open-sky zone, bag 4→6, shore/swimming, barrier/tool, three-day rescue
- 1280×800 KO/EN contextual captures: **PASS**, no product-contract overflow
- Windows x64 Development build: **PASS**, errors 0, warnings 0
- Hidden Windows player smoke: **PASS**, alive after 8 seconds

## Historical runner compatibility

- Wave 7 Edit contracts: **PASS 12/12**
- Wave 6 Edit contracts: **PASS 15/15**
- Wave 7 old Play layout checks: **EXPECTED FAIL 2** because they require the former always-visible large camp bag panel; Wave 9 explicitly removes that dashboard. Six-slot pointer/gamepad submit still passed.
- Wave 6 old Play signal readability checks: **EXPECTED FAIL 2** because they capture the post-action field message without reopening the new signal popup. Signal semantics and barrier checks passed; the Wave 9 product Play contract captures the new signal popup instead.
- Physical gamepad human actuation: **UNVERIFIED**; synthetic keyboard/gamepad action paths passed.

## Primary evidence

- `editmode-checks.txt`
- `playmode-checks.txt`
- `kim-survival-wave9-camp-far-ko-1280x800.png`
- `kim-survival-wave9-proximity-prompt-ko-1280x800.png`
- `kim-survival-wave9-campfire-popup-ko-1280x800.png`
- `kim-survival-wave9-workbench-popup-en-1280x800.png`
- `kim-survival-wave7-signal-stage1-missing-ko-1280x800.png`
- `kim-survival-wave7-signal-stage2-missing-en-1280x800.png`
- `kim-survival-wave7-placement-ko-invalid-1280x800.png`
- `kim-survival-wave7-placement-en-valid-gamepad-1280x800.png`
- `kim-survival-wave7-exploration-ko-1280x800.png`
- `kim-survival-wave7-swimming-en-1280x800.png`
- `windows-build.txt`
- `windows-player-smoke.log`

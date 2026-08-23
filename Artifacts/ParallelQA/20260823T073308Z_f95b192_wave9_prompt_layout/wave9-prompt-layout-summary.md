# Wave 9 proximity prompt layout verification

- Baseline: `f95b192d45f04e36f173ae274e29a3684cce7bf0`
- Unity: `6000.4.9f1`
- Unity Editor, Play Mode, Windows build, and player smoke ran outside the Codex sandbox.

## Result

- Unity compilation and deterministic Edit checks: **PASS**
- Full product Play Mode loop: **PASS**
- 1280x800 Korean prompt: **PASS**
- 1280x800 English prompt: **PASS**
- 1280x800 qps-long single-line ellipsis policy: **PASS**
- Far hidden -> one nearby target -> popup hidden -> cancel restores prompt: **PASS**
- Campfire, workbench, rain collector, and rescue signal shared prompt layout: **PASS**
- Keyboard and gamepad localized prompt routes: **PASS** (synthetic action-path verification)
- Windows x64 Development build: **PASS**, 0 errors / 0 warnings
- Hidden Windows player smoke: **PASS**, alive after 8 seconds
- Physical gamepad actuation: **UNVERIFIED**

## Layout contract at 1280x800

- Independent Canvas UI directly below the narration card
- 8 px card gap
- 512 px maximum width (40 percent)
- 48 px height
- One line, auto-size 20-24 reference units, ellipsis on overflow
- No overlap with HUD, bottom controls, player, facilities, floor, or walking path in captured states

## Primary evidence

- `editmode-checks.txt`
- `playmode-checks.txt`
- `kim-survival-wave9-camp-far-ko-1280x800.png`
- `kim-survival-wave9-proximity-prompt-ko-1280x800.png`
- `kim-survival-wave9-proximity-prompt-en-1280x800.png`
- `kim-survival-wave9-proximity-prompt-qps-long-1280x800.png`
- `kim-survival-wave9-campfire-popup-ko-1280x800.png`
- `windows-build.txt`
- `windows-player-smoke.log`

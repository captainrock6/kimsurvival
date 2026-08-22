# Wave 5 integrated playtest verification

- Integrated baseline: `e8d01c119b736ee771d2361e5704ab318eefdb7d`
- Unity: `6000.4.9f1`
- Compile: PASS, 0 errors
- Standard Edit checks: PASS
- Independent Edit checks: PASS (9/9 gameplay contracts)
- Standard Play Mode: PASS
- Independent Play Mode: PASS (ko/en Day 1-3 natural loop, no resource grants)
- 1280x800 visual gates: placement 24/24 PASS; exploration/swimming 10/10 PASS; qps-long 10/10 PASS
- Asset and release contracts: PASS (244 passed, 0 failed, physical gamepad unverified)
- Addressables temporary `link.xml` ownership: PASS before load, build, cleanup, and post-smoke
- Windows x64 Development build: Succeeded, 0 errors, 0 warnings
- Hidden Windows player smoke: PASS for 6.322 seconds at 1280x800 windowed
- Steam readiness: NOT_READY because Steamworks SDK/API, App ID, depot/upload, Steam Input, Cloud, and Achievements are intentionally not integrated yet

## Playtest executable

`C:/Users/dev/Documents/ChatGPT/신규 개발 본부/work/ParallelQA/20260822T231356Z_e8d01c1_wave5_integrated/WindowsBuild/KimSurvivalIsland.exe`

## Current-slice notes

- Runtime balance remains Food 1, Hunger 75, and 25 hunger consumed per day. The accepted v0.2 proposal (Food 0, Hunger 70, 35 hunger per day) is documented but not implemented in this build.
- The adopted comedy-feedback effect package is imported and contract-checked, but its six effects are not wired to runtime events in this build.
- Camp background and camp structures use adopted production assets. Exploration, swimming, and Mr. Kim still use prototype placeholder graphics.
- Shared keyboard/gamepad action paths pass automated checks. A physical gamepad was not exposed to batch-mode Unity, so human physical-device verification remains open.

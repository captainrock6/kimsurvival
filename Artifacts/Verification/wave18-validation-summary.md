# Wave 18 runtime validation

- Branch: `codex/wave18-pacing-region-hazard-runtime`
- Original implementation baseline: `fac8545148e1422fc6258f57cab2205cbb4596a9`
- Integrated infrastructure baseline: `cc15f38d7ad8cf398ced9d3e48c62ecb1f4cc39c`
- Validated code commit: `e9829556c4736da468ab62d004318dae8747b95f`
- Full gate run: `Artifacts/ParallelQA/20260824T_wave18_green_evidence_01`

## Result

- Overall: GREEN
- Product: PASS, 0 failures, 0 expected gaps
- Infrastructure: PASS
- Wave 15 campaign map: GREEN
- Frozen Wave 16 hazard/escape/ending foundation: GREEN
- Wave 17/18 Edit contracts: 20 PASS, 0 FAIL
- Wave 17/18 Play contracts: 5 PASS, 0 FAIL
- Unity compile: 0 errors, 0 warnings
- qps-long global layout: 10/10 PASS
- Ending layout at 1280x800: KO/EN/qps-long each 3 panels, overflow 0, offscreen 0, overlap 0
- Windows x64 Development build: PASS
- Hidden smoke: PASS, 6.456 seconds
- Addressables link contract: PASS
- Physical gamepad: UNVERIFIED
- Steam: NOT_READY

## Windows player

- Executable: `work/ParallelQA/StableWindowsBuild/KimSurvivalIsland.exe`
- SHA-256: `93c19f9e7c681845d34407807d33b6438e781dd34c4d8895ebdf2c6fb083711d`
- Build options: Development, with AllowDebugging absent through the integrated infrastructure contract

## Selected-only presentation binding

- Hazard phase atlas GUID: `22aa0efe034962041860a1171b2c5a73`
- Escape route signature GUID: `a3fc27f38161341409c6fbe0be7bea6f`
- Ending triptych GUID: `ba9091d85a3bddd4a8c8b90aa07d1b7c`
- Runtime binding: `Assets/_Project/Settings/Resources/Wave18PresentationAssets.asset`
- Each selected package has a one-file runtime allowlist and `runtimeConnectAllowed/runtimeConnected=true`.
- No review-board or unselected file is referenced by the runtime binding.

## Reproduction

```powershell
& '.\Assets\Editor\ParallelQA\Invoke-Wave17PacingHazardGate.ps1' -RunId '<fresh-run-id>' -BaselineCommit '<git-rev-parse-HEAD>'
```

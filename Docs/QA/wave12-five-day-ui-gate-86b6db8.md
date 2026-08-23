# Wave 12 five-day / compact-a independent QA gate

## Verdict

- Branch: `codex/wave12-five-day-ui-gate`
- Exact baseline: `86b6db8d5bc628aa7cb9cdb0d3e59539b6633c91`
- Final Run ID: `20260823T114000Z_86b6db8_wave12_final`
- Overall: **FAIL**
- Product: **FAIL** — four approved RED gaps plus one independently reproduced P1 normal-visual regression contract
- QA infrastructure: **PASS**
- Physical gamepad: **UNVERIFIED**
- Steam: **NOT_READY**

The approved five-day deadline and adopted compact-a runtime connection remain intentionally RED on this baseline. The gate also found a separate current normal-visual regression and therefore does not downgrade the final result to RED-only.

## Verification matrix

| Area | Result | Independent evidence |
|---|---|---|
| Unity compilation | PASS | Unity 6000.4.9f1; compiler errors/warnings `0/0` |
| Wave11 direct slots and popup/cancel flow | PASS | `4/4` product checks; upper/side/basement near/action/first-candidate/cancel `3/3` |
| Wave11 1280×800 walking path | PASS | actual render-target rects; clear `3/3`; failure reasons `none` |
| Fresh Wave3 generation | PASS (infrastructure) | same RunId/SHA; placement `24`, exploration/swimming `10`, qps-long `10` targets |
| Five-day deadline | EXPECTED_FAIL P0 | `FinalDay=3`; Day 4 unavailable; early Day 1 rescue still succeeds |
| compact-a package identity | PASS | engine-ready job, A GUID `070048b5b443d5d4a9c757c871873eb3`, border L70/R30/T12/B12, B/C unreferenced |
| compact-a runtime connection | EXPECTED_FAIL P0 | A has no enabled runtime/scene dependency reference |
| Runtime frame/glyph split | EXPECTED_FAIL P0 | no sprite, `Image.Type.Simple`, zero border, separate glyph false, action still contains `[E]` |
| ko/en/qps 12-state capture geometry | EXPECTED_FAIL P1 | 12/12 PNGs valid; current prompt `440×48`, narration gap `8`; qps popup overflow `4` |
| Device/locale/target/progress independence | PASS | keyboard→synthetic gamepad and ko→en→qps preserve target and progress fingerprint |
| Current normal Wave3 visual | FAIL P1 | placement `4/24` and exploration/swimming `1/10` fail unchanged pixel thresholds |
| Full survival regression | PASS | placement, bag, signal, search, swimming, shore return, room module, and natural three-day rescue |
| Asset/release non-visual contracts | PASS | 335 PASS; only three `visual.current_*` product rows fail |
| Addressables | PASS | load/build/post-smoke ownership and SHA/GUID stability |
| Windows x64 Development build | PASS | succeeded, errors/warnings `0/0` |
| Hidden Windows smoke | PASS | responsive/alive through `6.305s` |
| Physical gamepad | UNVERIFIED | no connected device/human actuation evidence |
| Steam release readiness | NOT_READY | no release configuration/evidence claimed |

## Defects and reproduction

### P0 · W12-D01 · Five-day deadline is not implemented (expected RED)

1. Start a fresh `GameSession`.
2. Complete search/return/end-day without Grant or coordinate warp.
3. Observe Day 3 remains playable but ending Day 3 immediately produces `Deadline`; Day 4 cannot be played.
4. Separately complete both signal stages early and observe immediate `Rescued` still works.

Impact: the user-approved five-day pacing cannot be playtested. Recommended product files: `Assets/_Project/Scripts/Runtime/GameSession.cs`, `Assets/_Project/Scripts/Runtime/KimSurvivalPrototype.cs`.

### P0 · W12-A02/P01 · compact-a is adopted but not connected (expected RED)

1. Audit the adopted A source and manifest; confirm GUID `070048...` and border `L70/R30/T12/B12`.
2. Approach a direct connection slot in Play Mode.
3. Inspect the prompt `Image`: sprite/path/GUID are absent, type is `Simple`, border is zero.
4. Inspect descendants: no separate glyph component exists and the action TMP contains `[E]`/`[X]` semantics.

Impact: approved A art and device-specific glyph separation are not represented at runtime. Recommended product/scene files: `KimSurvivalPrototype.cs` and `KimSurvivalPrototype.unity`.

### P1 · W12-P03 · compact geometry/qps-long layout is not release-ready (expected RED)

1. Open the twelve `wave12-{ko,en,qps-long}-{far,near,popup,direct-slot}-1280x800.png` files.
2. Compare prompt/narration rects: current prompt is `440×48px`, gap `8px`; contract is `220..440×40..44px`, gap `>=12px`.
3. Open qps-long popup; observe overlapping/wrapped copy and `4` overflowing active TMP elements.

Impact: compact-a and future-locale layout cannot pass release gating.

### P1 · W12-P04 · Fresh normal visual regression (unexpected)

1. Run the exact command below with a fresh RunId.
2. Inspect `wave3-visual-gate.txt` and `wave3-visual-metrics.tsv`.
3. In four ko/en placement frames, observe `설비·증축 계획` / `Facilities · Expansion` at `13.6px` / `12.9px`, below `16px` (`4/24` failures).
4. In Korean land exploration, observe `표류물 ×2` at `16.5px`, below `18px` (`1/10` failures).

Impact: small world labels remain hard to read at 1280×800. Recommended product files: `KimSurvivalPrototype.cs`, `PrototypeCampUse.cs`. This was not reclassified as infrastructure and no runtime fix was made.

## Infrastructure corrections

1. Wave11 prompt layout now attaches a real `1280×800` RenderTexture to the camera, forces the CanvasScaler layout, projects prompt/player/target/walking world bounds into that exact pixel rect, and records every rect plus failure reason. This changes the false `0/3` result to evidence-backed `3/3` without relaxing overlap safety.
2. Wave12 Play generates fresh Wave3 frames in the same RunId through the spatial near→popup→action access order. The obsolete far/global dashboard button assumption is isolated.
3. Asset/release runs after that fresh report. Mixed PNG/SVG engine packages are audited as a PNG subset plus all declared source existence/GUID/hash checks, avoiding a false infrastructure failure from editable SVG coexistence.
4. Product-failing Unity stages may exit `1` while their structured report still declares `infrastructureOverall=PASS`; the aggregator no longer misclassifies that product exit as infrastructure failure.

## GREEN transition contract

The same runner turns GREEN only when all of the following are true:

- `GameSession.FinalDay == 5`, Day 3 and Day 4 remain playable, ending Day 5 fails, and early rescue remains immediate.
- compact-a is referenced by stable GUID/path, rendered as a sliced Image with exact L70/R30/T12/B12 border, and compact-b/c remain unreferenced.
- input glyph and localized TMP action are separate; device and locale switching preserve target/progress state.
- all 12 ko/en/qps state captures pass compact geometry, no-occlusion, and active TMP overflow `0`.
- fresh normal Wave3 returns placement `24/24` and exploration/swimming `10/10`; qps-long returns `10/10`.
- existing full regression, Addressables, Windows build, and hidden smoke remain PASS.

## Exact rerun

Run outside the Codex sandbox per `Docs/QA/unity-codex-sandbox-licensing.md`; do not use `-noUpm`:

```powershell
& '.\Assets\Editor\ParallelQA\Invoke-Wave12FiveDayUiGate.ps1' `
  -RunId '<NEW_RUN_ID>' `
  -BaselineCommit '86b6db8d5bc628aa7cb9cdb0d3e59539b6633c91' `
  -MinimumSmokeSeconds 6
```

Evidence root: `Artifacts/ParallelQA/20260823T114000Z_86b6db8_wave12_final`.

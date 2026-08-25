# GAME JAM Wave B RED-first independent QA gate

## Verdict

- Exact baseline: `00e0d5a9df597ab4a9f54bff665291f367d40c92` from `origin/codex/gamejam-director-search-node-integration`
- QA branch: `codex/gamejam-wave-b-redfirst-qa`
- Baseline verdict: **RED**
- Product verdict: **RED_EXPECTED_GAP** (9 product contracts)
- Infrastructure verdict: **PASS**
- Physical gamepad: **UNVERIFIED**; only keyboard/mouse and synthetic gamepad are automatable here.
- Steam: **NOT_READY**; this gate neither configures nor certifies Steam.

The RED result is intentional. The integrated build keeps the earlier environmental-search slice green, but it does not yet expose the Wave B 21-archetype/42-instance/144-unit catalog or a complete natural disease Play trace. A product gap never changes an infrastructure result to PASS or FAIL, and an infrastructure failure is never reported as a product gap.

## Current baseline observation

| Surface | Required | Observed at `00e0d5a` | Classification |
|---|---:|---:|---|
| Regions | 7 | 7 | product evidence, retained |
| Stable archetypes | 21 | 0 | `GWB-E01` expected gap |
| Stable instances | 42 | 0 | `GWB-E01` expected gap |
| Legacy integrated nodes | replaced by the Wave B instance catalog | 28 | diagnostic fact, not accepted as 42 |
| General resource units | 144 | 78 derived from the current catalog | `GWB-E01` expected gap |
| Disease Play lifecycle | telegraph -> exposure -> effect -> worsen -> mitigate/treat | no structured natural Play observer | `GWB-P03/P05` expected gap |

The 28-node value counts only the current `node.<region>...` node IDs. Loot and protected-part item IDs are excluded, so they cannot inflate the node count.

## Contract matrix

| ID | Severity | Baseline result | Independent acceptance / GREEN condition | Recommended product surface |
|---|---|---|---|---|
| `GWB-E01.catalog_7_21_42_144` | P0 | EXPECTED_GAP | A structured public catalog derives exactly 7 regions, 21 stable archetypes, 42 stable instances, and 144 general-resource units. Stable IDs must be observable; a count assertion is not evidence. | Search-node catalog and loot definitions, currently near `PrototypeSearchNodeRuntime.cs` / `PrototypeExpeditionMap.cs` |
| `GWB-E02.seed_revision_byte_determinism` | P0 | EXPECTED_GAP | Two generations with the same seed + `contractRevision` + `lootTableRevision` canonicalize to byte-identical output; another seed varies contents while retaining the stable catalog. | Search-node generation/resolution surface and run seed contract |
| `GWB-E03.revision_and_new_game_stock_schema` | P0 | EXPECTED_GAP | Public saved state contains both revisions and a new-game-only stock generation marker. Loading continues with stored revisions. | `GameSession.cs` and search-node ledger/snapshot data |
| `GWB-E04.ko_en_qps_wave_b_surface` | P1 | EXPECTED_GAP | KO/EN/qps-long provide canonical barrier-broken, permanent-hazard-removed, and disease telegraph/exposure/effect/worsen/mitigate-or-treat meanings. | Product localization tables after implementation |
| `GWB-P01.return_forced_return_snapshot_persistence` | P0 | EXPECTED_GAP | Actual Play records hidden -> partial -> depleted, a non-empty known-remainder fingerprint, return, forced return, revisit, snapshot restore, broken barrier, and removed hazard, with `grant=false`, `warp=false`, `skip=false`. | Search runtime, travel/forced-return handling, and save snapshot |
| `GWB-P02.stock_not_regenerated_outside_new_game` | P0 | EXPECTED_GAP | Stock fingerprints are identical across screen transition, return, forced return, revisit, and restore; the only generation event is a new game. | Search-node ledger/snapshot lifecycle |
| `GWB-P03.natural_disease_lifecycle_atomic` | P0 | EXPECTED_GAP | One actual Play disease ID advances in order through telegraph, exposure, effect, worsen, mitigate/treat; each apply/cost count is 1 and duplicate/cancel deltas are 0. | Hazard runtime, currently adjacent to `PrototypeHazardEscapeEnding.cs` |
| `GWB-P04.ko_en_qps_input_layout_parity` | P1 | EXPECTED_GAP | Three 1280x800 layout observations report zero overflow/offscreen/player/walking-band occlusion; keyboard/mouse and synthetic gamepad retain the same action meaning and locale switches preserve the same state fingerprint. | Live Wave B observation plus localized search/disease UI |
| `GWB-P05.actual_natural_trace_no_cheats` | P0 | EXPECTED_GAP | An active Scene component returns a structured public observation whose interaction trace includes region, expedition, search, and treat/mitigate; explicit grant/warp/skip fields all remain false. | Actual Play observation on the integrated gameplay owner |

## Acceptance integrity

The runner deliberately does not accept:

- a bare `bool`, assertion string, primitive value, or fixture-only probe;
- a preferred implementation class-name allowlist;
- a reported `Passed` member without deriving the result from structured values;
- `grant`, coordinate `warp`, time/state `skip`, or equivalent shortcuts;
- existing GSN 15/15 as a substitute for the new 21/42/144 and disease contracts.

Catalog discovery examines structured public roots and stable IDs. Generation discovery requires a public seed-and-revision method whose returned structure contains all 42 instances, then canonicalizes and hashes the returned values. Play acceptance inspects the active Scene and derives transitions, fingerprints, counts, input meanings, and 1280x800 image dimensions from returned structured evidence.

## Locked GREEN prerequisites

Each Wave B invocation first runs the existing GSN entry point with the same fresh RunId and exact baseline. That nested gate retains:

- GSN 15/15 PASS;
- Wave 19 21/21 PASS;
- Wave 20 16/16 PASS;
- Unity compile 0 errors / 0 warnings;
- Windows x64 Development build PASS;
- hidden smoke for at least 6 seconds PASS;
- Addressables/link ownership PASS;
- exact stable executable inbound firewall Block contract PASS;
- raw player log quarantine PASS.

Any missing or mismatched report, RunId, baseline, build path, SHA, smoke duration, Addressables evidence, or firewall evidence is `INFRASTRUCTURE_FAIL`, not `EXPECTED_GAP`.

## Reproduction

Run Unity/build/smoke outside the Codex sandbox as required by `Docs/QA/unity-codex-sandbox-licensing.md`:

```powershell
& '.\Assets\Editor\ParallelQA\Invoke-GameJamWaveBRedFirstGate.ps1' `
  -RunId '<NEW_RUN_ID>' `
  -BaselineCommit '00e0d5a9df597ab4a9f54bff665291f367d40c92' `
  -MinimumSmokeSeconds 6
```

Exit code `0` means all product and infrastructure contracts are GREEN, `2` means product RED with infrastructure PASS, and `1` means an infrastructure or unexpected product failure. The entry point is compatible with Windows PowerShell 5.1 and PowerShell 7, writes UTF-8 without BOM, refuses an existing evidence directory, and checks exact HEAD before creating it.

## Evidence

- Final canonical run: `Artifacts/ParallelQA/20260826T070000Z_gamejam_wave_b_redfirst_final/`
- Earlier development run (preserved, not canonical after the node-count diagnostic correction): `Artifacts/ParallelQA/20260826T063000Z_gamejam_wave_b_redfirst/`
- Primary machine-readable verdict: `gamejam-wave-b-summary.json`
- Product reports: `gamejam-wave-b-edit-contracts.json`, `gamejam-wave-b-play-contracts.json`
- Raw structured observations: `gamejam-wave-b-edit-observation-evidence.json`, `gamejam-wave-b-play-observation-evidence.json`
- Locked prerequisite verdict: `gamejam-search-node-summary.json`
- Build/smoke contracts: `windows-development-build.json`, `windows-hidden-smoke.json`, `addressables-link-post-smoke-contract.json`, `wave19-windows-firewall-contract.json`

## GREEN transition

After the Wave B implementation is integrated, rerun the same entry point at that exact integration SHA using a new RunId. GREEN requires all nine `GWB-*` product IDs to report PASS from actual structured Edit/Play observations and all locked prerequisites to remain PASS. Synthetic gamepad parity does not promote the physical-gamepad gate; it remains UNVERIFIED until a human test with connected hardware is recorded.

# O11 post-integration gate readiness at 2c88100b

## Scope and identity

- Integration baseline: `2c88100b1cb103ba7d603177dfbbad286729cdf6`
- QA branch: `codex/o11-postintegration-ready-2c88100b`
- Unity: `6000.4.9f1`
- Product runtime, scenes, localization, art, and `.forge/**` are read-only inputs to this QA change.

The public live-observation bridge is not present at this baseline. The readiness mode therefore runs compile, O11 Edit contracts, and O11 Play/render observation without claiming the build/smoke release locks or a GREEN result.

## Review-only search art blocker

`O11-P1-005` must remain blocked even if a product observation reports `ReviewOnly=false` while the independent adoption sources still say review-only:

- `.forge/assets.json`: `background.expedition-seven-region-set` is `review`.
- `.forge/assets.json`: `object.searchable-resource-node-production-set` is `review`.
- `Assets/_Project/Art/Runtime/Resources/O11/Regions/o11-region-runtime-manifest.json`: `reviewState=review`, `decision=review`, empty runtime allowlist, `packageAllowed=false`, `runtimeConnectAllowed=false`, and `formalRuntimeConnected=false`.

The gate now requires both live rendered GUID/clip observations and this independent adoption audit. Provisional review-build connection is diagnostic evidence, never adoption.

## Commands

Bridge/readiness preflight at an exact SHA (no Windows build or smoke claim):

```powershell
& '.\Assets\Editor\ParallelQA\O11IntegrationGate.ps1' -RunId 'O11_<UTC>_2c88100b_readiness' -BaselineCommit '2c88100b1cb103ba7d603177dfbbad286729cdf6' -ReadinessOnly
```

After the bridge is integrated, run the complete release gate with a fresh RunId and the new full SHA:

```powershell
& '.\Assets\Editor\ParallelQA\O11IntegrationGate.ps1' -RunId 'O11_<UTC>_postintegration_full' -BaselineCommit '<BRIDGE_INTEGRATED_FULL_SHA>' -IncludeBuild
```

Only the second command can produce O11 `GREEN`; it includes the inherited compile, Windows StableWindowsBuild, hidden smoke, Addressables, and firewall locks. Physical gamepad remains `UNVERIFIED`, and Steam remains `NOT_READY` without separate human/device and partner evidence.

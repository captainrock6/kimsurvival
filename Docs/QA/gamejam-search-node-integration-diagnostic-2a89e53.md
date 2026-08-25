# GameJam 환경 수색 node 통합 QA 진단 — 2a89e53

## 판정

- 결합 기준: `2a89e53000f7a5e8d19ad0ecec519fd29bfb38f7`
- 브랜치 기준: `codex/gamejam-director-search-node-integration`
- 최종 RunId: `20260826T041500Z_gamejam_search_node_diagnostic_final`
- 전체 / 제품 / 인프라: `RED / FAIL / PASS` (종료 코드 `2`)
- search-node: `11/15 PASS`, 제품 실패 `4`, 인프라 실패 `0`
- Wave 20: `16/16 PASS`
- Wave 19: `20/21 PASS`; 유일한 실패는 진화한 `W19-P02.resource_nodes_adopted_icons`
- 컴파일 / Windows x64 Development build / hidden smoke: `0/0 / PASS / PASS 6.244s`
- Addressables / 고정 경로 방화벽 Block / Windows PowerShell 5.1: `PASS / PASS / PASS`
- 물리 게임패드 / Steam: `UNVERIFIED / NOT_READY`

초기 통합 증거 `Artifacts/ParallelQA/20260825T160510Z_gamejam_search_node_integrated`의 search-node `0/15`는 실제 제품 공백 4건과 관찰기 오탐 11건이 섞인 결과였다. 이 진단에서는 공개 stable ID·구조화 데이터·실제 Play 상태·실제 입력 trace만 수용하도록 owner 탐색을 최소 확장했다. bool assertion, 클래스명 allowlist, fixture-only 결과, review-only search-node 아트는 PASS 근거로 사용하지 않았다.

## 해소된 관찰기 오탐

| ID | 최종 | 오탐 원인과 수정된 관찰 |
|---|---|---|
| `GSN-E01` | PASS | 지역 정의 안의 중첩 `Nodes`를 평탄화하여 실제 7지역·28 node catalog를 관찰 |
| `GSN-E02` | PASS | 옛 3-string signature 대신 공개 `(seed, structured node definition)` resolver를 발견하고 실제 contents fingerprint 비교 |
| `GSN-E04` | PASS | 구현 이름 token이 아니라 take/leave/remaining/cost/risk의 ko/en/qps-long 동등 의미를 canonical table에서 확인 |
| `GSN-E05` | PASS | 모든 실제 resolver output에서 `part.raft.sailcloth`와 공개 transaction/뗏목 연결을 확인 |
| `GSN-P02` | PASS | 실제 ledger에서 동일 seed+node 결정성, cancel·재방문·snapshot 복원 뒤 무재추첨 확인 |
| `GSN-P03` | PASS | 실제 `Hidden → RevealedPartial → Depleted`와 남은 item/count snapshot 복원 확인 |
| `GSN-P04` | PASS | 실제 take/replace/cancel transaction의 총량 보존과 중복 delta 0 확인 |
| `GSN-P05` | PASS | 실제 protected sailcloth의 폐기·복제·중복 소비 방지와 raft stable ID 연결 확인 |
| `GSN-P07` | PASS | 실제 완료 비용·위험 1회, 취소 0회, tray 동안 신규 위험 판정 정지 확인 |
| `GSN-P09` | PASS | keyboard/mouse와 synthetic gamepad의 node/action/focus 의미 일치 확인 |
| `GSN-P10` | PASS | 실제 Scene owner와 자연 이동 3 step, interaction trace, `grant=false`, `warp=false`, `skip=false` 확인 |

## 남은 실제 제품 공백

| 심각도 | ID | 실제 관찰 | 재현 절차 | GREEN 조건 / 권장 제품 소유 파일 |
|---:|---|---|---|---|
| P0 | `GSN-E03.persistent_snapshot_schema` | node snapshot은 상태·잔량·위험 노출을 보존하지만 지역 장벽 상태와 영구 위험 제거 상태가 없음 | Edit report에서 `PrototypeSearchNodeSnapshot`/run snapshot shape 확인 | barrier/permanent-hazard stable state를 저장·복원하는 공개 schema와 transition. `Assets/_Project/Scripts/Runtime/PrototypeSearchNodeRuntime.cs` 인접 persistence owner |
| P1 | `GSN-P01.actual_node_prompt_tray` | 실제 far/near prompt 수가 `1/1`; far 계약은 `0` | Play에서 대상 밖 캡처 후 자연 이동 3 step, prompt count 비교 | 1.25m 밖 0, 근거리 정확히 1, tray open 때 숨김, Cancel 때 동일 대상 복귀. `Assets/_Project/Scripts/Runtime/KimSurvivalPrototype.cs`의 node label/prompt owner |
| P0 | `GSN-P06.seven_region_finite_persistence` | 7지역 finite resource는 PASS지만 barrier persistence와 permanent-hazard persistence는 각각 FAIL | snapshot 저장→화면 전환/재방문→두 state fingerprint 비교 | 두 지역 상태가 재방문 뒤 유지. `PrototypeSearchNodeRuntime.cs` 인접 region-state owner |
| P1 | `GSN-P08.ko_en_qps_compact_tray_1280` | ko/en/qps-long 모두 overflow/offscreen `0/0`, compact PASS이나 player-clear/path-clear가 FAIL; tray Rect 약 `723.2×308` | 세 locale 1280×800 tray 캡처에서 player/walking band 교차 판정 | 세 locale 모두 player·핵심 보행 band occlusion 0. `KimSurvivalPrototype.cs`의 compact tray layout owner |
| P1 | `W19-P02.resource_nodes_adopted_icons` | loose Wood node는 더 이상 강제하지 않는다. 실제 환경 node/compact tray에서 Wood/Stone/Food/Salvage 채택 GUID 렌더러가 모두 `0`, unresolved node는 각각 `1/3/2/2` | actual `NodeView.Definition`을 resolver로 풀고 각 resource item의 live `SpriteRenderer/Image.sprite` GUID 수집 | 네 종류가 각각 기존 채택 GUID를 실제 렌더링하고 geometric fallback 0. `KimSurvivalPrototype.cs`의 search node/tray resource icon wiring owner |

`W19-P02`의 과거 실제 문구 `no Wood node exists`는 구 loose-node 모델을 새 환경 node에 강제한 하니스 오탐이었다. 수정된 계약은 실제 node 정의와 compact tray를 관찰하므로 loose node 부재 자체는 실패시키지 않는다. 다만 현재 제품은 네 채택 resource icon GUID를 어느 환경 node/tray에도 렌더링하지 않으므로 동등 의미 계약은 실제 제품 FAIL이다. review-only search-node 후보와 그 GUID는 adopted로 간주하지 않는다.

## 재현 명령

Unity Editor/build/Windows Player는 `Docs/QA/unity-codex-sandbox-licensing.md`에 따라 샌드박스 밖에서 실행한다. Windows PowerShell 5.1과 7 모두 같은 진입점을 사용하며 fresh RunId가 필요하다.

```powershell
& '.\Assets\Editor\ParallelQA\Invoke-GameJamSearchNodeRedFirstGate.ps1' `
  -RunId '<NEW_RUN_ID>' `
  -BaselineCommit '2a89e53000f7a5e8d19ad0ecec519fd29bfb38f7' `
  -MinimumSmokeSeconds 6
```

종료 코드는 `0=GREEN`, `2=제품 RED`, `1=인프라 FAIL`이다. GREEN 전환에는 search-node `15/15`, Wave 19 `21/21`, Wave 20 `16/16`, 컴파일 `0/0`, build/smoke/Addressables/firewall PASS가 모두 필요하다. 물리 게임패드와 Steam은 이 자동 GREEN에 포함하지 않으며 각각 `UNVERIFIED`, `NOT_READY`를 유지한다.

## 최종 증거

- `Artifacts/ParallelQA/20260826T041500Z_gamejam_search_node_diagnostic_final/gamejam-search-node-summary.json`
- `Artifacts/ParallelQA/20260826T041500Z_gamejam_search_node_diagnostic_final/gamejam-search-node-edit-observation-evidence.json`
- `Artifacts/ParallelQA/20260826T041500Z_gamejam_search_node_diagnostic_final/gamejam-search-node-play-observation-evidence.json`
- `Artifacts/ParallelQA/20260826T041500Z_gamejam_search_node_diagnostic_final/wave19-play-observation-evidence.json`
- `Artifacts/ParallelQA/20260826T041500Z_gamejam_search_node_diagnostic_final/kim-survival-search-tray-ko-1280x800.png`
- `Artifacts/ParallelQA/20260826T041500Z_gamejam_search_node_diagnostic_final/kim-survival-search-tray-en-1280x800.png`
- `Artifacts/ParallelQA/20260826T041500Z_gamejam_search_node_diagnostic_final/kim-survival-search-tray-qps-long-1280x800.png`
- `Artifacts/ParallelQA/20260826T041500Z_gamejam_search_node_diagnostic_final/compile-result.txt`
- `Artifacts/ParallelQA/20260826T041500Z_gamejam_search_node_diagnostic_final/windows-development-build.json`
- `Artifacts/ParallelQA/20260826T041500Z_gamejam_search_node_diagnostic_final/windows-hidden-smoke.json`
- `Artifacts/ParallelQA/20260826T041500Z_gamejam_search_node_diagnostic_final/addressables-link-post-smoke-contract.json`
- `Artifacts/ParallelQA/20260826T041500Z_gamejam_search_node_diagnostic_final/wave19-windows-firewall-contract.json`
- `Artifacts/ParallelQA/20260826T041500Z_gamejam_search_node_diagnostic_final/gamejam-search-node-powershell-compatibility.json`

Raw Unity/Player log는 `work/ParallelQA/20260826T041500Z_gamejam_search_node_diagnostic_final`에만 격리되며 durable evidence에는 포함하지 않는다.

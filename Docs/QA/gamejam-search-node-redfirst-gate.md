# GameJam 환경 수색 node RED-first 독립 회귀 게이트

## 판정

- 기준 커밋: `5248809018ce934fe328328f194686d8c287734f`
- 기준 RunId: `20260826T_search_node_redfirst_02`
- 전체 / 제품 / 인프라: `RED / RED_EXPECTED_GAP / PASS`
- 검색 node 제품 결과: `PASS 0 / EXPECTED_GAP 15 / unexpected FAIL 0`
- 선행 잠금: Wave 20 `16/16 PASS`, Wave 19 `21/21 PASS`
- Unity 컴파일: `0 errors / 0 warnings`
- Windows x64 Development build: `PASS`
- 숨김 스모크: `PASS 6.255s`
- Addressables / 고정 빌드 경로 방화벽 Block: `PASS / PASS`
- 물리 게임패드: `UNVERIFIED`
- Steam: `NOT_READY`

현재 제품은 6지역 캠페인 데이터와 seed 기반 단발 채집 roll을 제공하지만, 환경 수색 node의 구조화된 내용물, 영속 상태, 원자적 발견물 선별 transaction과 실제 compact 트레이는 제공하지 않는다. 따라서 기존 `PrototypeRegionLootRng`의 결정적 단일 `Resource/Amount` 결과나 QA assertion 문자열은 새 계약의 PASS 근거가 아니다.

## 독립 판정 원칙

게이트는 권장 제품 클래스명이나 파일명 allowlist로 PASS시키지 않는다. Edit 계약은 공개 stable ID, 구조화된 catalog/generator/snapshot shape와 canonical TSV 값을 검사한다. Play 계약은 활성 Scene의 실제 `Component`에서 구조화된 관찰 결과를 찾고, stable region/node ID, interaction trace, 상태·수량 delta와 fresh RunId 내부의 실제 캡처를 독립적으로 다시 검사한다. `Success=true`, 설명 문자열, EditMode fixture만으로는 Play 계약을 통과할 수 없다.

제품 공백은 정확한 기준 SHA에서 `PRODUCT_EXPECTED_GAP`으로 기록한다. Unity 시작 실패, 보고서 누락, identity mismatch, 컴파일/빌드/스모크 실패는 `INFRASTRUCTURE_FAIL`이며 제품 공백과 합산하지 않는다. 구현 통합 SHA에서 남은 계약 실패는 `PRODUCT_REGRESSION`이다.

## RED 기준선 매트릭스

| ID | 심각도 | 기준선 | 현재 관찰 | GREEN 조건 |
|---|---:|---|---|---|
| `GSN-E01.seven_region_node_catalog` | P0 | EXPECTED_GAP | 공개 region catalog는 6개이고 구조화된 유한 search-node catalog가 없음 | region stable ID 정확히 7개, node stable ID와 유한 양수 item/count catalog 관찰 |
| `GSN-E02.seed_node_content_determinism` | P0 | EXPECTED_GAP | 기존 roll은 단일 `Resource/Amount`이며 structured contents collection이 아님 | 동일 seed+region+node fingerprint 일치, 5개 대체 seed 중 유효 변형 1개 이상 |
| `GSN-E03.persistent_snapshot_schema` | P0 | EXPECTED_GAP | hidden/partial/depleted, remaining item/count, barrier/hazard를 함께 가진 node/region snapshot 없음 | 모든 상태와 stable ID, 수색 횟수, 장벽·영구 위험 상태가 공개 snapshot에서 관찰 |
| `GSN-E04.ko_en_qps_search_surface` | P1 | EXPECTED_GAP | 검색 관련 기존 행 23개는 있지만 reveal/take/take-all/leave/replace/cancel/remaining/depleted/protected/cost/risk surface가 없음 | 요구 semantic row가 ko/en/qps-long에서 비어 있지 않고 qps-long 평균 팽창비 1.25 이상 |
| `GSN-E05.protected_part_raft_link` | P0 | EXPECTED_GAP | 보호 프로젝트 인벤토리는 있으나 search-node catalog에 `part.raft.sailcloth` 연결과 discard/duplicate/consume 계약이 없음 | 돛천이 보호 발견물로 연결되고 폐기·복제·중복 소비가 구조적으로 금지됨 |
| `GSN-P01.actual_node_prompt_tray` | P0 | EXPECTED_GAP | 실제 Play search-node target/compact tray 없음 | far 0, near 1, Interact tray open, prompt hidden, Cancel 동일 target 복귀 |
| `GSN-P02.no_reroll_cancel_transition_revisit` | P0 | EXPECTED_GAP | 실제 node 내용물 상태/trace 없음 | Cancel·화면 전환·재방문 전후 item/count fingerprint 동일, 다른 seed 유효 변형 |
| `GSN-P03.hidden_partial_depleted_restore` | P0 | EXPECTED_GAP | 상태 lifecycle과 남은 발견물 restore 관찰 불가 | 동일 node에서 hidden→revealed-partial→depleted, 잔량 정확 복원 |
| `GSN-P04.loot_bag_transaction_atomicity` | P0 | EXPECTED_GAP | 발견물 take/leave/replace/cancel transaction 없음 | bag+node 총량 보존, 취소 불변, 동일 transaction 중복 비용 delta 0 |
| `GSN-P05.protected_parts_and_sailcloth` | P0 | EXPECTED_GAP | 실제 search node에서 돛천 발견·보호·뗏목 연결 trace 없음 | 폐기 거부, 복제 delta 0, 중복 소비 delta 0, 뗏목 연결 정확히 1회 |
| `GSN-P06.seven_region_finite_persistence` | P0 | EXPECTED_GAP | 실제 관찰 region ID 0개, 유한 총자원/장벽/영구 위험 재방문 trace 없음 | 7지역 finite budget, 장벽 파괴와 영구 위험 제거가 재방문 뒤 유지 |
| `GSN-P07.search_cost_hazard_pause` | P0 | EXPECTED_GAP | 뒤지기 완료 비용·위험 ledger와 트레이 pause 없음 | 완료 때 비용/노출 각 1회, 취소 0회, 트레이 동안 신규 위험 판정 정지 |
| `GSN-P08.ko_en_qps_compact_tray_1280` | P1 | EXPECTED_GAP | 실제 tray와 fresh ko/en/qps-long 캡처 없음 | 각 locale 1280×800 캡처 1개, overflow/offscreen 0, compact/플레이어/보행 band clear |
| `GSN-P09.keyboard_mouse_synthetic_gamepad_parity` | P1 | EXPECTED_GAP | search-node 입력 action/focus 결과 없음 | node/action/item/count/focus 의미 동일, 장치 prompt만 변경 |
| `GSN-P10.natural_trace_no_fixture_cheats` | P0 | EXPECTED_GAP | 구조화된 live Scene observation 없음 | stable region/node와 8개 이상 실제 interaction trace, `grant=false`, `warp=false`, `skip=false` |

## 재현 절차

Unity Editor/build/Windows Player smoke는 `Docs/QA/unity-codex-sandbox-licensing.md`에 따라 샌드박스 밖에서 실행한다. `-noUpm` 우회는 금지한다. Windows PowerShell 5.1.26100.9168 진입점 구문 검사와 PowerShell 7.6.4 전체 실행을 통과했으며 결과 JSON/TXT는 UTF-8 without BOM이다.

```powershell
& '.\Assets\Editor\ParallelQA\Invoke-GameJamSearchNodeRedFirstGate.ps1' `
  -RunId '<NEW_RUN_ID>' `
  -BaselineCommit '<EXACT_INTEGRATED_HEAD_SHA>' `
  -MinimumSmokeSeconds 6
```

종료 코드는 `0=GREEN`, `2=제품 RED(예상 공백 또는 구현 통합 뒤 남은 제품 실패)`, `1=인프라 FAIL`이다. 제품 실패와 컴파일·실행·증거 실패를 종료 코드로도 섞지 않는다. 증거 디렉터리가 이미 존재하면 러너는 시작 전에 중단하므로 항상 fresh RunId를 사용한다.

기준선 명령은 다음과 같다.

```powershell
& '.\Assets\Editor\ParallelQA\Invoke-GameJamSearchNodeRedFirstGate.ps1' `
  -RunId '20260826T_search_node_redfirst_02' `
  -BaselineCommit '5248809018ce934fe328328f194686d8c287734f' `
  -MinimumSmokeSeconds 6
```

## 구현 통합 뒤 GREEN 전환

1. 구현 브랜치를 통합한 격리 상태의 정확한 HEAD SHA를 기록한다.
2. 위 명령의 `RunId`를 새 값으로, `BaselineCommit`을 통합 HEAD SHA로 바꿔 실행한다.
3. `gamejam-search-node-summary.json`이 `GREEN/PASS/PASS`, search-node failure 0인지 확인한다.
4. Wave 20 `16/16`, Wave 19 `21/21`, canonical camp/module/map와 지도 당일 재출발 안내, 컴파일 `0/0`, Windows build/smoke, Addressables, firewall가 계속 PASS인지 확인한다.
5. 실제 물리 게임패드는 사람이 연결 장치로 전체 경로를 끝내기 전까지 `UNVERIFIED`, Steam은 파트너/배포 증거가 없으면 `NOT_READY`로 유지한다.

구조화된 Play 관찰은 실제 Scene `Component`의 공개 0-인자 capture/observe/snapshot/trace 결과로 발견된다. 결과에는 stable region/node ID, contents/state/transaction/hazard/input/layout 필드와 실제 interaction trace가 있어야 한다. 캡처 경로는 반드시 같은 fresh evidence 디렉터리 내부 파일이어야 한다. 구현의 공개 데이터 계약이 동등한 의미를 다른 이름으로 제공하면 러너의 alias 목록만 QA 범위에서 확장하되, bool assertion이나 권장 클래스명으로 acceptance를 완화하지 않는다.

## 권장 제품 소유 지점(수정은 구현 브랜치 담당)

- 7지역·node catalog와 seed generator: 현재 region/loot foundation 인접 runtime data owner
- node snapshot·region persistence·inventory transaction: 새 stable-ID 기반 runtime state owner
- 보호 부품·뗏목 돛천 연결: protected project inventory와 `escape.raft` 연결 owner
- compact tray와 실제 입력/캡처 surface: 실제 탐색 Scene UI owner
- ko/en/qps-long row: canonical localization table owner

이 QA 브랜치는 위 제품 파일을 수정하지 않는다.

## 기준선 증거

- `Artifacts/ParallelQA/20260826T_search_node_redfirst_02/gamejam-search-node-summary.json`
- `Artifacts/ParallelQA/20260826T_search_node_redfirst_02/gamejam-search-node-edit-observation-evidence.json`
- `Artifacts/ParallelQA/20260826T_search_node_redfirst_02/gamejam-search-node-play-observation-evidence.json`
- `Artifacts/ParallelQA/20260826T_search_node_redfirst_02/gamejam-search-node-ps51-syntax.txt`
- `Artifacts/ParallelQA/20260826T_search_node_redfirst_02/compile-result.txt`
- `Artifacts/ParallelQA/20260826T_search_node_redfirst_02/windows-development-build.json`
- `Artifacts/ParallelQA/20260826T_search_node_redfirst_02/windows-hidden-smoke.json`
- `Artifacts/ParallelQA/20260826T_search_node_redfirst_02/addressables-link-post-smoke-contract.json`
- `Artifacts/ParallelQA/20260826T_search_node_redfirst_02/wave19-windows-firewall-contract.json`
- `Artifacts/ParallelQA/20260826T_search_node_redfirst_02/kim-survival-hotfix-expedition-complete-notice-ko-1280x800.png`

Raw Unity/Player logs are 격리된 `work/ParallelQA/20260826T_search_node_redfirst_02`에만 있으며 durable evidence에는 raw `windows-player.log`가 없다.

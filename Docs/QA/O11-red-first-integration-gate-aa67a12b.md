# O11 독립 RED-first 통합 게이트

- RED 기준: `origin/codex/gamejam-wave-b-integration` `aa67a12bb38180f7cf2635a2a2bca3c403b5248a`
- 인간 근거: `Docs/Design/Playtest/Sessions/O10-H1-2026-08-28.md`
- 소유: `Assets/Editor/ParallelQA/O11*`, `Docs/QA/O11*`, `Artifacts/ParallelQA/O11*`
- 제품 런타임·아트를 수정하지 않는다.

## 판정 원칙

`O11IntegrationGateRunner`는 Play scene의 활성 `MonoBehaviour`를 탐색하고, 구현 클래스 이름이 아니라 반환 데이터의 구조화 의미(`Placements`, `Launches`, `Reactions`, `Layouts`, `Pacing`, `RouteBurdens`, `AssetBindings` 중 5개 이상)로 O11 observation owner를 발견한다. 제품이 기록한 PASS bool, assertion 문자열, fixture-only 결과는 인수하지 않는다. 상위 trace에 `grant`, `warp`, `skip`이 한 번이라도 관찰되면 자연 Play 계약은 실패한다.

정확한 RED 기준에서 관찰이 부족하면 `EXPECTED_GAP`이다. 통합 후 다른 SHA에서 같은 부족은 `FAIL`이며, 일곱 항목이 모두 수치 계약을 만족할 때만 `GREEN`이다.

## O10-H1에서 유래한 일곱 계약

| ID | RED 재현 기준 | GREEN 완료 조건 |
|---|---|---|
| `O11-P0-001` | 2층·지하 일반 설비 배치 불가 | 작업대·빗물받이·침대·소파 x 시작층·2층·지하 12조합의 배치/재배치, `StableRoomId`/좌표 exact save/restore, 겹침·출입구·필수 통로 거부와 상태 불변 |
| `O11-P0-002` | 출항 가능 표시 뒤 불가, 당일 재시도 식량 차감 | 불가/당일 중복/가능 3 case의 버튼·사유·ledger·진척·transaction·terminal 일치 |
| `O11-P1-001` | 증축 후 놀람 상태 고정 | 2층과 지하 각각 `surprise/build -> idle -> walk` 실제 상태 trace |
| `O11-P1-002` | 채택 V2가 아닌 임시 UI | `ui.gamejam.style-benchmark` `job_20260828122852_c9ccf2aa` 실제 runtime GUID 연결 + KO/EN 1280x800·1920x1080 가방·팝업·월드 overlap/offscreen/overflow 0 |
| `O11-P1-003` | 연속 수색 기력 가속 고갈 | 자연 3회 수색의 비용·회복 수단·다음 가능 시간 관찰과 동일 seed 바이트 동일 fingerprint |
| `O11-P1-004` | 떼목이 압도적으로 쉬움 | 최소 3 representative seed에서 떼목·연기·무전 모두 feasible, 떼목 burden `>= 0.75 * min(smoke, radio)` |
| `O11-P1-005` | 임시 수색 그래픽·캐릭터 동작 | 7 region + `kim.idle/walk/search/ladder/swim`의 실제 runtime GUID/clip, placeholder/review-only 0 |

## 실행

RED 기준에서는 O11 후보 빌드를 만들지 않는다.
컴파일 인프라는 compiler error 0과 O11 소유 경고 0을 요구한다. 정확한 기준선에 이미 있는 다른 소유 경고는 개수와 원문을 증거에 보존하되 O11 QA 인프라 실패로 돔리지 않는다.

```powershell
& '.\Assets\Editor\ParallelQA\O11IntegrationGate.ps1' `
  -RunId '20260829T000000Z_aa67a12_red' `
  -BaselineCommit 'aa67a12bb38180f7cf2635a2a2bca3c403b5248a'
```

세 제품 브랜치 통합 후에는 exact 통합 SHA와 `-IncludeBuild`를 사용한다. 이 모드는 기존 검색 node 릴리스 진입점을 선행하여 compile, Windows build, 6초 hidden smoke, Addressables, firewall lock을 같은 `O11_*` 증거에 포함한다.

```powershell
& '.\Assets\Editor\ParallelQA\O11IntegrationGate.ps1' `
  -RunId '<NEW_RUN_ID>' `
  -BaselineCommit '<INTEGRATED_FULL_SHA>' `
  -IncludeBuild
```

## 별도 수동 게이트

- 물리 게임패드: `UNVERIFIED`를 유지한다. 합성 입력은 실기 PASS를 대체하지 않는다.
- Steam: App ID·Depot·Input·Cloud·Achievements·권한·배포 증거가 없으므로 `NOT_READY`를 유지한다.

## `aa67a12b` RED 기준선 실행 결과

- 정본 run: `Artifacts/ParallelQA/O11_20260829T003000Z_aa67a12_ps51_red`
- shell: Windows PowerShell `5.1.26100.9168`
- Unity: `6000.4.9f1`
- 전체/제품/인프라: `RED / RED_EXPECTED_GAP / PASS`
- 제품: `0 PASS / 7 EXPECTED_GAP / 0 unexpected FAIL`
- 컴파일: `0 errors / 0 warnings`
- Play/render: Unity exit `0`, KO/EN의 `1280x800`, `1920x1080` baseline 캡처 4장 생성
- build/smoke: `NOT_RUN_RED_BASELINE_POLICY`. QA 브랜치에서 O11 후보를 만들지 말라는 지시에 따른 비실행이다.
- 채택 V2 정적 증거: registry `adopted=true`, `engine_ready=true`, package present, image GUID `6ea907ec446c7dd4eb6a039227d04b84`.
- 실행 연결 결손 증거: playable scene의 V2 package dependency `0`, project `AnimationClip` asset `0`, O11 구조화 live observation owner `MISSING`.
- 실행 캡처는 현재 화면을 보존하는 RED 증거이며 layout PASS가 아니다. GREEN layout은 bag/popup/world를 동시에 구조화하여 계측한 observation과 함께 다시 캡처해야 한다.

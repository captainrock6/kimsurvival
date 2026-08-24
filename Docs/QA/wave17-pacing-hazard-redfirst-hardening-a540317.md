# Wave 17 페이싱·위험·탈출·엔딩 RED-first 강화 감사

- 기준선: `origin/master@a5403173f299abc71ed4724bdaaf30c31ce8cc94`
- 브랜치: `codex/wave17-pacing-hazard-redfirst-hardening`
- 실행 ID: `20260824T121000Z_a540317_wave17_redfirst`
- Unity: `6000.4.9f1`
- PowerShell: Windows PowerShell `5.1.26100.9168`
- 전체 판정: **RED / PRODUCT RED_EXPECTED_GAP / INFRASTRUCTURE PASS**
- 물리 게임패드: **UNVERIFIED**
- Steam: **NOT_READY**

이 게이트는 제품 런타임·씬·현지화·아트·Forge 원장을 수정하지 않는다. 공개 런타임 데이터, 안정 ID, 결정적 semantic probe 결과, 실제 Play 상태만으로 판정한다. 권장 구현 파일명이나 한 클래스 이름만 존재한다는 이유로 PASS하지 않는다.

## 1. 선행 잠금과 인프라 결과

| 항목 | fresh 결과 | 판정 |
|---|---|---|
| 정확한 기준 SHA | `a5403173f299abc71ed4724bdaaf30c31ce8cc94` | PASS |
| Wave 15 캠페인 지도 | `GREEN` | PASS |
| Wave 16 통합 기준선 | 제품 실패 ID 정확히 `17/17`, 인프라 PASS | PASS baseline lock |
| qps-long 전역 레이아웃 | `10/10 PASS` | PASS |
| Unity 컴파일 | `0 errors / 0 warnings` | PASS |
| Windows x64 Development build | 성공, build warnings `0` | PASS |
| 숨김 Player 스모크 | `6.463s`, alive/responding | PASS |
| Addressables | load/build/post-smoke ownership·stability | PASS |
| Wave 17 증거 생성·UTF-8 no BOM | Windows PowerShell 5.1 | PASS |

Wave 16의 기존 17건은 `W16-H01/H02/H03`, `W16-E01/E02/E03/E04`, `W16-O01`, `W16-N01/N02/N03`, `W16-L01`, `W16-P01/P02/P03/P04/P05`다. a540317에서는 모두 `PRODUCT_REGRESSION`으로 다시 재현됐고 인프라 실패로 재분류되지 않았다. 후속 구현 기준에서는 이 17건이 먼저 전부 GREEN이어야 Wave 17 전체 GREEN이 가능하다.

## 2. Wave 17 RED 제품 갭

총 20건이며 `P0 17건`, `P1 3건`이다. 현재 기준선에서 아래 실패는 예상된 제품 미구현이다.

| ID | P | 현재 관찰 | GREEN 조건 / 재현 |
|---|---:|---|---|
| `W17-T01.day_band_boundaries` | P0 | 공개 pacing band 5개 없음 | Day 1–10/11–20/21–35/36–49/50 데이터와 Day49 continue·Day50 terminal을 공개 데이터로 열거 |
| `W17-T02.early_escape_no_hardlock` | P0 | 결정적 pacing/escape probe 없음 | 모든 band 경계에서 충족된 조기 탈출이 날짜로 막히지 않음을 동일 probe로 실행 |
| `W17-R01.six_region_primary_alternative` | P0 | 6지역 공개 계약과 확장 3지역 primary/alternative 없음 | 6 안정 ID와 확장 3지역의 두 독립 해금·단일 lock reason 실행 |
| `W17-R02.seed_forecast_hazard_pity_determinism` | P0 | same-seed probe 없음 | 같은 seed/day/state의 전망·위험·해금·pity가 동일하고 다른 seed도 유효 |
| `W17-R03.eligible_search_hint3_guarantee5` | P0 | eligible/hint/guarantee 데이터·probe 없음 | 성공 eligible만 계산, 3회 hint, 5회 뒤 다음 eligible guarantee; 취소/실패/무관/중복 불변 |
| `W17-R04.minimum_three_completable_paths` | P0 | softlock audit 없음 | seed 생성·확장 해금·Day35·Day49 각각 completable escape ID 최소 3개 |
| `W17-H01.three_hazard_four_phase_lifecycle` | P0 | injury/disaster/food-theft 공개 상태 없음 | 세 ID 각각 telegraph→occurrence→mitigation→recovery 실행 |
| `W17-H02.rolling_calm_and_major_recovery` | P0 | rolling calm/recovery probe 없음 | 모든 rolling 5일 창 평온일 ≥1, major 다음 날 recovery budget 2, same-family major 금지 |
| `W17-H03.atomic_retry_loss_and_keypart_protection` | P0 | 원자/idempotent probe 없음 | 동일 instance 재호출 무변경, 도난/파손 1회, 핵심 부품·완료 stage 보존 |
| `W17-E01.five_escape_ids_and_two_axes` | P0 | 5 escape 공개 카탈로그 없음 | 정확한 5 ID와 모든 쌍의 requirement axis 차이 ≥2 |
| `W17-E02.smoke_radio_natural_interaction_routes` | P0 | 자연 경로 probe 없음 | smoke/radio 각각 actual interaction count >0, `grant=false`, `warp=false`, terminal complete |
| `W17-E03.raft_flare_beacon_data_only` | P1 | data-only 3경로 데이터 없음 | catalog/primary+alternative/snapshot/atomic result 검증; playable PASS는 금지 |
| `W17-O01.snapshot_and_private_log` | P0 | pacing snapshot/log schema 없음 | seed·region·hazard·project·behavior 보존, stable result fields, 이름/계정/IP/자유 입력 없음 |
| `W17-N01.ending_catalog_19_and_samples` | P0 | ending `0/19`, sample `0/4` | 19개 고유 안정 ID와 4 sample을 공개 데이터로 열거 |
| `W17-N02.priority_tiebreak_and_hysteresis` | P0 | terminal/identity probe 없음 | 조기 탈출 우선, Day50 단일 ending, 동일 snapshot 결정론, 명시적 tie-break, 마지막 2점 행동으로 identity 불변 |
| `W17-P01.live_hazard_lifecycle` | P0 | 실제 Play hazard surface/probe 없음 | Play 상태에서 세 hazard 4단계와 idempotent instance 실행 |
| `W17-P02.live_smoke_radio_natural_paths` | P0 | 실제 Play 자연 경로 probe 없음 | 두 경로를 별도 실제 상호작용으로 완료하고 grant/warp 0 기록 |
| `W17-P03.live_terminal_priority_and_three_panels` | P0 | ending presentation panel `0` | 조기/Day50 resolver와 core panel 정확히 3개 |
| `W17-P04.ko_en_qps_1280_layout` | P1 | locale 전환은 유지되나 ending panel `0` | ko/en/qps-long 1280×800에서 3 panel, TMP overflow/offscreen/overlap 모두 0 |
| `W17-P05.keyboard_synthetic_gamepad_parity` | P1 | 합성 전환은 가능하나 ending semantic state 없음 | 동일 ending/pacing/hazard/escape snapshot에서 키보드↔합성 패드 의미 불변 |

러너는 제품 기준선이 a540317일 때만 위 실패를 `PRODUCT_EXPECTED_GAP`으로 분류한다. 다른 SHA에서 동일 누락은 `PRODUCT_REGRESSION/FAIL`이다. 따라서 기준 SHA만 바꿔 허위 GREEN을 만들 수 없다.

## 3. review-only 아트 human-adoption gate

아래 3종은 모두 `review`, `selectedCandidate=null`, runtime allowlist 빈 상태이며 Runtime/Scene/Addressables에서 candidate ID, job ID, 파일명, GUID 참조가 0건이다.

| 후보 | GUID | 판정 |
|---|---|---|
| `effect.survival-hazards.phase-silhouette-a` | `22aa0efe034962041860a1171b2c5a73` | PASS review-only |
| `ui.escape-project-progress.route-signature-a` | `a3fc27f38161341409c6fbe0be7bea6f` | PASS review-only |
| `ui.ending-comic.triptych-a` | `ba9091d85a3bddd4a8c8b90aa07d1b7c` | PASS review-only |

명시적 사용자 채택 전에는 품질 점수나 파일 존재를 runtime 연결 승인으로 간주하지 않는다.

## 4. 캡처 육안 검토

`wave17-ko/en/qps-long-ending-state-1280x800.png`를 원본 1:1로 확인했다. 세 캡처 모두 Day 1/50 캠프와 기존 근접 prompt를 표시하고 locale 전환은 적용된다. 그러나 ending UI가 없어서 core comic panel이 `0/3`이며, 이 때문에 `P03/P04`는 정확히 RED다. 기존 qps-long 전역 게이트 10/10 PASS는 유지되지만 그것이 미구현 ending 레이아웃 PASS를 대신하지 않는다.

Windows Development Player 원본 로그에는 Unity가 기록한 로컬 인터페이스 IP와 호스트명이 있어 커밋 증거에서 제외했다. durable `windows-hidden-smoke.json`은 실행 시간·SHA-256·alive/responding 결과만 보존한다. 진입점은 후속 실행에서 raw 로그를 ignored `work/ParallelQA/<run-id>/`로 격리한다.

## 5. 재실행

Unity·빌드·Player 단계는 `Docs/QA/unity-codex-sandbox-licensing.md`에 따라 반드시 Codex 샌드박스 밖에서 실행한다. `-noUpm`은 사용하지 않는다.

```powershell
& '.\Assets\Editor\ParallelQA\Invoke-Wave17PacingHazardGate.ps1' `
  -RunId '<NEW_RUN_ID>' `
  -BaselineCommit '<CHECKED_OUT_HEAD_SHA>' `
  -MinimumSmokeSeconds 6
```

종료 코드 계약은 `0=GREEN`, `2=RED_EXPECTED_GAP`, `1=unexpected product/adoption regression 또는 infrastructure failure`다. 구현 통합 후에는 새 SHA를 `-BaselineCommit`으로 전달한다. 새 SHA에서는 Wave 16 17건과 Wave 17 20건이 모두 0 failure이고 선행 인프라가 PASS여야 GREEN이다.

## 6. 증거

- `Artifacts/ParallelQA/20260824T121000Z_a540317_wave17_redfirst/wave17-summary.json`
- `Artifacts/ParallelQA/20260824T121000Z_a540317_wave17_redfirst/wave17-edit-contracts.json`
- `Artifacts/ParallelQA/20260824T121000Z_a540317_wave17_redfirst/wave17-play-contracts.json`
- `Artifacts/ParallelQA/20260824T121000Z_a540317_wave17_redfirst/wave17-edit-evidence.json`
- `Artifacts/ParallelQA/20260824T121000Z_a540317_wave17_redfirst/wave17-play-evidence.json`
- `Artifacts/ParallelQA/20260824T121000Z_a540317_wave17_redfirst/wave16-summary.json`
- `Artifacts/ParallelQA/20260824T121000Z_a540317_wave17_redfirst/wave15-summary.json`
- `Artifacts/ParallelQA/20260824T121000Z_a540317_wave17_redfirst/wave14-qps-global-layout-gate.json`
- `Artifacts/ParallelQA/20260824T121000Z_a540317_wave17_redfirst/windows-development-build.json`
- `Artifacts/ParallelQA/20260824T121000Z_a540317_wave17_redfirst/windows-hidden-smoke.json`
- `Artifacts/ParallelQA/20260824T121000Z_a540317_wave17_redfirst/addressables-link-post-smoke-contract.json`

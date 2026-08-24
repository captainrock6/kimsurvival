# Wave 18 독립 QA green-transition hardening

## 판정

- 브랜치: `codex/wave18-green-transition-redfirst`
- 변경 전 정본: `fac8545148e1422fc6258f57cab2205cbb4596a9`
- 방화벽 방지 통합: `origin/master` `cc15f38d7ad8cf398ced9d3e48c62ecb1f4cc39c`
- 최종 검증 기준: `fab9e9c7c527e9d7fa62dc3fd85f736bf0c30fd8`
- Unity: `6000.4.9f1`
- 최종 RunId: `20260824T152500Z_fab9e9c_wave18`
- 전체/제품/인프라: `RED / FAIL / PASS`
- 변경 전 Wave 17 재현: 제품 FAIL `15/15`, EXPECTED_GAP `0`, 인프라 PASS
- 강화된 23개 제품 행렬: PASS `10`, FAIL `13`, EXPECTED_GAP `0`
- 물리 게임패드: `UNVERIFIED`
- Steam: `NOT_READY`

제품 RED를 인프라 실패나 EXPECTED_GAP으로 숨기지 않는다. 이후 구현 기준에서 23개 ID가 모두 존재하고 제품 FAIL이 0이며 인프라가 PASS일 때만 GREEN이다.

## 변경 전 기준 보존

`20260824T134000Z_fac8545_wave17_baseline`은 Wave 18 코드를 추가하기 전에 정확한 `fac8545`에서 실행했다. Wave 15와 Wave 16은 GREEN, Wave 17은 아래 15개 ID가 정확히 FAIL, 컴파일·Windows Development 빌드·6초 숨김 스모크·Addressables는 PASS였다. 원시 Windows Player 로그는 커밋 증거에서 격리됐다.

Wave 18은 이 15개 ID를 삭제하거나 이름을 바꾸지 않는다. 보강 후에는 개인정보 오탐과 실제 Play 위험 상태 관찰 누락이 해결되어 `W17-O01`, `W17-P01`이 독립 증거로 PASS가 됐고, 나머지 13개는 계속 제품 FAIL이다.

## PASS 잠금과 바로잡은 게이트 결함

다음 8개 기존 PASS는 새 실행에서도 전부 PASS다: `W17-H01`, `W17-E01`, `W17-N01`, `W17-A01`–`A03`, `W17-P04`, `W17-P05`.

- 개인정보: Unity 객체의 일반 `name`이나 `event_name`을 PII로 보지 않는다. Unity가 직렬화하는 비-`UnityEngine.Object` `[Serializable]` snapshot/event-record 필드만 검사한다. `account`, `email`, `IP`, `host`, `free-text` 계열 실제 필드는 계속 FAIL시킨다. 현재 snapshot/log schema와 금지 필드 0건은 PASS다.
- 위험 Play lifecycle: 활성 Scene의 실제 `KimSurvival` 컴포넌트를 메서드 형태와 stable ID로 찾고, 세 위험 각각 `Telegraph → Occurrence → Mitigation → Recovery` 및 retry 무변경을 관찰했다. 권장 클래스명에는 결합하지 않는다.
- 연기/무전: 활성 Play 객체가 반환하는 별도 stable ID, interaction trace, `grant=false`, `warp=false`, terminal completion이 모두 필요하다. 정적 Edit fixture 3개는 명시적으로 무시되며 `W17-P02`를 통과시키지 않는다.
- 엔딩 priority: 실제 Play 객체에서 조기 탈출 우선, Day 50 미탈출, 결정론 tie-break, 3패널을 함께 관찰해야 한다. 현재 단일 Day 1 결과 객체만 있으므로 `W17-P03`은 FAIL이다.
- 아트: 현재 세 후보의 명시 채택·미연결 상태는 PASS다. 향후에는 선택된 세 primary만 정확한 allowlist로 연결된 상태도 PASS할 수 있다. review board, QA preview, 미선택 파일 또는 GUID가 Runtime/Scene/Addressables에 들어가면 FAIL한다.
- Windows 방화벽: 공통 `StableWindowsBuild` 경로, `Development` only, `AllowDebugging` 부재, build/smoke RunId·baseline·path·SHA-256 일치, 현재 작업 폴더 전용 Inbound Block 규칙을 별도 계약으로 잠갔다.

## 남은 제품 FAIL

| ID | 심각도 | 현재 재현 | GREEN 조건 / 권장 영역 |
|---|---:|---|---|
| `W17-T01.day_band_boundaries` | P0 | 5개 pacing band 공개 항목 누락 | Day 1/11/21/36/50 경계와 Day 49 계속을 공개 pacing catalog/config로 제공 |
| `W17-T02.early_escape_no_hardlock` | P0 | 정적 smoke/radio/terminal fixture만 있고 band 전체 증거 없음 | 모든 band에서 fulfilled 조기 탈출을 실제 resolver 경로로 완료 |
| `W17-R01.six_region_primary_alternative` | P0 | 6 ID는 있으나 ridge/cove/ruins primary·alternative 해금 항목 누락 | 날짜 하드락 없는 공개 region unlock data/fixture 제공 |
| `W17-R02.seed_forecast_hazard_pity_determinism` | P0 | ending 결정론 fixture만 발견 | 같은 seed+day+state의 전망·위험·해금·pity 전체 재현 probe 제공 |
| `W17-R03.eligible_search_hint3_guarantee5` | P0 | eligible/pity 공개 데이터와 probe 없음 | 취소·실패·중복 제외, 3회 hint/5회 guarantee transaction 제공 |
| `W17-R04.minimum_three_completable_paths` | P0 | 네 audit point의 route audit 없음 | seed 생성·확장·Day 35·49마다 최소 3경로 stable ID 증명 |
| `W17-H02.rolling_calm_and_major_recovery` | P0 | 5일 평온일·major 다음 recovery cadence 증거 없음 | 10일 이상 결정론 cadence와 post-major 예약 공개 |
| `W17-H03.atomic_retry_loss_and_keypart_protection` | P0 | health idempotence만 있고 도난/파손·핵심부품 보호 없음 | 같은 event 재적용, 단일 loss, key-part/stage 보호 snapshot 비교 |
| `W17-E02.smoke_radio_natural_interaction_routes` | P0 | 문자열 기반 정적 fixture만 존재 | smoke/radio 별도 실제 interaction trace와 grant/warp false 제공 |
| `W17-E03.raft_flare_beacon_data_only` | P1 | catalog 일부는 있으나 graph/snapshot/atomic validator 불완전 | 세 data-only 경로의 공개 graph·snapshot·atomic result validator 제공 |
| `W17-N02.priority_tiebreak_and_hysteresis` | P0 | 결정론·Day 50 정적 fixture는 있으나 identity hysteresis 없음 | established identity를 2점 행동 하나가 뒤집지 않는 fixture 제공 |
| `W17-P02.live_smoke_radio_natural_paths` | P0 | live result/trace 없음; static fixture 3개는 무시됨 | 활성 Play 객체의 두 독립 trace, stable ID, grant=false, warp=false, terminal=true |
| `W17-P03.live_terminal_priority_and_three_panels` | P0 | 실제 Play 객체는 Day 1 `ending.stay.just-kim` 단일 결과만 노출 | 실제 조기 탈출/Day 50 두 상태, tie-break, 3패널을 한 live 관찰로 증명 |

## 인프라 결과

| 항목 | 결과 |
|---|---|
| Unity compile | PASS, errors `0`, warnings `0` |
| Wave 15 / Wave 16 | GREEN / GREEN |
| qps-long 전역 레이아웃 | PASS `10/10` |
| Windows x64 Development build | PASS, errors `0`, warnings `0` |
| 숨김 1280×800 smoke | PASS, `6.393s`, alive/responding |
| Addressables load/build/post-smoke | PASS |
| Build/smoke RunId·baseline·path·SHA | PASS / PASS |
| `AllowDebugging` 부재 | PASS |
| 정확한 QA EXE 인바운드 Block | PASS |
| 원시 Player log 격리 | PASS |
| Windows PowerShell 5.1 실행·UTF-8 no BOM | PASS |
| PowerShell 7.6.4 구문 검사 | PASS |

고정 실행 파일은 `work/ParallelQA/StableWindowsBuild/KimSurvivalIsland.exe`이며 build와 smoke SHA-256은 모두 `93c19f9e7c681845d34407807d33b6438e781dd34c4d8895ebdf2c6fb083711d`였다.

## 재실행

Unity 라이선스 제약 때문에 다음 명령 전체를 Codex sandbox 밖에서 실행한다. Windows PowerShell 5.1과 PowerShell 7 모두 지원한다.

```powershell
& '.\Assets\Editor\ParallelQA\Invoke-Wave18GreenTransitionGate.ps1' `
  -RunId '<NEW_RUN_ID>' `
  -BaselineCommit 'fab9e9c7c527e9d7fa62dc3fd85f736bf0c30fd8' `
  -MinimumSmokeSeconds 6
```

후속 제품 커밋을 검증할 때는 `-BaselineCommit`을 그 결합 HEAD의 정확한 `git rev-parse HEAD` 값으로 바꾸고 새 RunId를 사용한다.

## 증거

- 변경 전 `fac8545` 전체 증거: `Artifacts/ParallelQA/20260824T134000Z_fac8545_wave17_baseline/`
- 최종 Wave 18 전체 증거: `Artifacts/ParallelQA/20260824T152500Z_fab9e9c_wave18/`
- 최종 요약: `wave18-summary.json`, `wave18-summary.txt`
- 개인정보 schema: `wave18-privacy-schema-evidence.json`
- 실제 Play 관찰: `wave18-play-observation-evidence.json`
- 아트 연결 상태: `wave18-art-connection-evidence.json`
- 방화벽·고정 경로 identity: `wave18-windows-firewall-contract.json`
- PowerShell/UTF-8: `wave18-powershell-compatibility.json`, `wave18-powershell7-syntax.json`

실제 물리 게임패드와 Steam 파트너 설정 증거가 없으므로 각각 `UNVERIFIED`, `NOT_READY`를 유지한다.

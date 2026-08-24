# Wave 16 위험·다중 탈출·행동 엔딩 RED-first 독립 게이트

## 판정

- 기준: `origin/master` `635725b3e2679a7d6d4f66c09b137575bac374c8`
- 브랜치: `codex/wave16-hazard-ending-redfirst`
- 권위 실행: `20260824T015000Z_635725b_wave16_red_final`
- Unity: `6000.4.9f1`
- 전체 / 제품 / 인프라: **RED / RED_EXPECTED_GAP / PASS**
- 제품 계약: `0 PASS / 17 PRODUCT_EXPECTED_GAP / 0 unexpected FAIL`
- 선행 Wave 15 지도 전체 게이트: **GREEN**
- 물리 게임패드: **UNVERIFIED**. 합성 입력을 실기 PASS로 계산하지 않았다.
- Steam: **NOT_READY**. App ID, Depot, Input, Cloud, Achievements, 권한·스토어 증거를 만들거나 추정하지 않았다.

현재 기준에는 위험·다섯 탈출 프로젝트·행동 엔딩 런타임이 아직 없다. 게이트는 이 결손을 PASS로 가리지 않으며, 정확한 `635725b`에서만 `PRODUCT_EXPECTED_GAP`으로 분류한다. 다른 SHA에서 같은 계약이 실패하면 `PRODUCT_REGRESSION`과 종료 코드 `1`이다.

제품 런타임·씬·현지화 테이블·아트·Forge 원장은 수정하지 않았다. 변경은 QA Editor 러너, PowerShell 진입점, 이 문서와 신규 증거뿐이다.

## 선행 GREEN과 인프라

같은 RunId에서 기존 Wave 15 게이트를 먼저 실행했다. 따라서 아래 항목은 과거 증거 재사용이 아니라 현재 HEAD의 fresh 결과다.

| 잠금 | 결과 |
|---|---|
| Wave 15 Day 50·지도·세 지역·seed·softlock·ko/en/qps-long | GREEN |
| qps-long 전역 레이아웃 | 10/10 PASS |
| 캠프 근접 안내·배치·모듈·가방 4→6·수영·신호 | PASS |
| Unity 컴파일 | PASS, 0 errors / 0 warnings |
| Windows x64 Development build | PASS, 0 errors / 0 warnings |
| 숨김 1280×800 Player smoke | PASS, 6.255초, alive/responding |
| Addressables load/build/post-smoke | PASS |
| PowerShell 증거 UTF-8 no BOM | PASS |
| Windows PowerShell 5.1 진입점 파서 | PASS, 5.1.26100.9168 |

## PRODUCT_EXPECTED_GAP

| ID | 심각도 | 현재 관찰 | GREEN 조건 |
|---|---:|---|---|
| `W16-H01` | P0 | 세 sample hazard의 공개 안정 ID entry와 4단계 lifecycle이 모두 없음 | injury/disaster/food-theft 각각 warning→occurrence→mitigation→recovery 공개 데이터와 실행 상태 제공 |
| `W16-H02` | P0 | daily/major/active/recovery-reserve 예산 계약 없음 | 일일 예산·major 1회·active 2개·회복 예약을 공개 config와 결정적 fixture로 검증 |
| `W16-H03` | P0 | 원자·idempotent hazard probe 없음 | 같은 `hazardInstanceId` retry가 자원·상태·점수·로그를 재적용하지 않는 실제 transaction probe PASS |
| `W16-E01` | P0 | 공개 escape catalog `0/5` | raft/smoke/radio/flare/beacon 안정 ID entry 5개 |
| `W16-E02` | P0 | pairwise 축 비교 불가 | 모든 method 쌍이 region/research/facility/part/material/time/risk/timing 중 최소 2축 상이 |
| `W16-E03` | P0 | smoke/radio playable metadata와 probe 없음 | 두 경로의 progress·commit·complete 실제 플레이 fixture PASS |
| `W16-E04` | P1 | raft/flare/beacon 공개 데이터 `0/6` | 각 entry가 region·research·facility·part·risk·completion rule 보유 |
| `W16-O01` | P0 | snapshot `2/5`, event log `2/5`; seed·region만 있고 hazard·project·behavior 없음 | 다섯 필드 저장·복원·비식별 로그 PASS. 현재 PII 필드는 `none` |
| `W16-N01` | P0 | ending `0/19`, sample `0/4` | 19개 고유 안정 ID와 smoke/radio/DJ/just-Kim sample 4개 공개 catalog |
| `W16-N02` | P0 | 결정적 resolver/probe 없음; priority·condition-count·event-day 계약 없음 | 동일 snapshot 2회가 같은 단일 ending ID·근거·panel·mapping을 내고 명시적 tie-break 적용 |
| `W16-N03` | P0 | escape/Day50 행동 ending terminal probe 없음 | Day 50 전 escape 우선과 Day 50 미탈출 행동 ending fixture 모두 PASS |
| `W16-L01` | P1 | 요구 key group `0/65`; 기존 일반 TSV 271행만 존재 | hazard 3, escape 5, ending 19×title/summary/hint가 ko/en/qps-long 정렬·비어 있지 않음·qps 35% 이상 팽창 |
| `W16-P01` | P0 | live hazard 상태 surface 없음 | 세 hazard의 예고·발생·완화·회복과 retry 원자성을 Play 상태에서 확인 |
| `W16-P02` | P0 | live escape 상태 surface 없음 | smoke/radio를 별도 snapshot에서 완성하고 Day 50 전 terminal 확인 |
| `W16-P03` | P0 | 활성 ending comic panel `0` | sample ending이 정확히 3개 placeholder core panel을 표시 |
| `W16-P04` | P1 | ko/en/qps-long 캡처 3/3은 생성됐지만 ending panel이 없어 레이아웃 판정 불가 | 실제 세 locale comic에서 TMP overflow/offscreen/overlap 모두 0 |
| `W16-P05` | P1 | 현재 fingerprint는 장치 전환에 불변이나 검증할 hazard/project/ending 상태가 없음 | keyboard↔synthetic gamepad가 glyph/focus 외 locale·위험·프로젝트·점수·terminal·ending을 보존 |

`W16-O01`은 개인정보 안전을 별도로 기록한다. 현재 공개 event record에는 user/machine/home path/email/IP/account 필드가 없으므로 PII 스캔은 PASS지만, 새 상태 필드가 빠져 전체 계약은 RED다.

Windows Player 원시 로그는 Unity가 로컬 IP·호스트명을 기록하므로 원격 증거에서 제외했다. 빌드·스모크 판정, 실행 시간, alive/responding, 실행 파일 SHA-256은 개인정보를 제거한 `windows-development-build.*`와 `windows-hidden-smoke.*`에 보존했다.

## 게이트 설계

- 특정 구현 파일명이나 한 클래스 이름을 요구하지 않는다.
- `KimSurvival` 런타임 assembly의 공개 static/instance data를 순회하고 `hazard.*`, `escape.*`, `ending.*` 안정 ID를 우선 찾는다.
- lifecycle, 비용 축, tie-break와 snapshot은 공개 member의 의미와 결정적 QA probe 결과로 판정한다.
- Play 계약은 live object/state와 화면을 별도로 찾는다. 권장 클래스가 없다는 이유만으로 실패하지 않는다.
- 한국어 문구의 특정 철자·오타를 정답으로 고정하지 않는다. 안정 localization key, locale 열, 의미 상태와 실제 TMP geometry를 검사한다.
- 기준 SHA 불일치, Unity stage 실패, report/capture 누락은 제품 gap이 아니라 `INFRASTRUCTURE_FAIL`이다.
- 정확한 RED 기준 이외에는 `EXPECTED_GAP`을 허용하지 않는다.

## 화면 검토

`wave16-ko/en/qps-long-ending-state-1280x800.png`는 현재 ending UI 부재의 재현 증거다. 세 PNG는 모두 정확히 1280×800이며 locale 전이는 `ko→ko`, `en→en`, `qps-long→qps-long`으로 기록됐다. qps-long 캡처는 실제 장문 데이터가 표시된다. 다만 세 화면 모두 기존 캠프 화면이고 comic panel은 0이므로 ending 레이아웃 PASS가 아니다.

## 재실행

Unity와 Windows Player는 `Docs/QA/unity-codex-sandbox-licensing.md`에 따라 샌드박스 밖에서 실행하며 `-noUpm`을 쓰지 않는다. Windows PowerShell 5.1 또는 PowerShell 7에서 fresh RunId를 사용한다.

```powershell
& '.\Assets\Editor\ParallelQA\Invoke-Wave16HazardEndingGate.ps1' `
  -RunId '<NEW_RUN_ID>' `
  -BaselineCommit '635725b3e2679a7d6d4f66c09b137575bac374c8' `
  -MinimumSmokeSeconds 6
```

종료 코드:

- `0`: GREEN — fresh Wave 15 GREEN, Wave 16 gap/fail 0, 인프라 PASS.
- `2`: RED_EXPECTED_GAP — 정확한 `635725b`에서 승인된 미구현 제품 계약만 실패.
- `1`: 기준 불일치, 예상 밖 제품 회귀 또는 인프라 실패.

후속 Unity 구현 통합 뒤에는 동일 명령의 `-BaselineCommit`만 통합 HEAD 전체 SHA로 바꾼다. 그 SHA에서 남은 계약 실패는 EXPECTED_GAP이 아니라 FAIL이며, 17개 gap이 모두 0일 때만 GREEN이다.

## 증거

권위 증거 루트: `Artifacts/ParallelQA/20260824T015000Z_635725b_wave16_red_final`

- `wave16-summary.json` / `wave16-summary.txt`
- `wave16-edit-contracts.json` / `wave16-edit-contracts.txt`
- `wave16-edit-evidence.json`
- `wave16-play-contracts.json` / `wave16-play-contracts.txt`
- `wave16-play-evidence.json`
- `wave16-ko-ending-state-1280x800.png`
- `wave16-en-ending-state-1280x800.png`
- `wave16-qps-long-ending-state-1280x800.png`
- `wave16-powershell-compatibility.json`
- `wave15-summary.json` / Wave 15 Edit·Play 보고서와 지도 캡처
- `wave14-qps-global-layout-gate.json` / `wave14-qps-global-layout-targets.tsv`
- `compile-result.txt`
- `windows-development-build.json` / `.txt`
- `windows-hidden-smoke.json` / `.txt`
- `addressables-link-build-contract.json` / `addressables-link-post-smoke-contract.json`

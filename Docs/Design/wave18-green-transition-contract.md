# Wave 18 GREEN 전환 구현 계약

- 상태: `DESIGN CONTRACT COMPLETE / IMPLEMENTATION·LIVE QA PENDING / HUMAN 6 SESSIONS UNRUN`
- 기준: `origin/master@fac8545148e1422fc6258f57cab2205cbb4596a9`
- 계약 ID: `implementation.wave18.green-transition.v1`
- 기계 정본: `.forge/packets/wave18-green-transition-contract.json`, `.forge/design/project.json`의 `greenTransitionContract`
- RED 근거: `Artifacts/ParallelQA/20260824T_wave17_integrated_d3e71f3_full/wave17-summary.json`
- 입력 정본: `campaign.wave15.escape-hazard-ending-matrix.v1`, `campaign.wave16.fifty-day-pacing.v1`, `playtest.wave17.natural-fifty-day.v1`

이 문서는 Wave 17 통합 결과에서 실패한 제품 항목 15개만 GREEN으로 전환하기 위한 최소 구현 계약이다. Wave 15/16 GREEN, 컴파일·Windows 빌드·숨김 스모크·Addressables·qps-long 인프라 PASS를 보존한다. Day 50, 조기 탈출, 탈출법 5개, 지역 6개, 위험 7개, 엔딩 19개와 Wave 16 `SAMPLE_ONLY` 수치는 추가·삭제·개명·정식 승격하지 않는다.

## 1. 공통 공개 계약

모든 항목은 기존 게이트 ID 자체를 공개 `probeId`로 사용한다. 동의어 probe ID를 만들지 않는다. 공개 probe는 QA 러너의 reflection이나 파일명 탐색이 아니라 런타임 의미 데이터를 읽거나 실제 상호작용을 실행하는 결정론적 표면이다.

| 필드 | 계약 |
|---|---|
| 공통 입력 | `probeId`, `schemaVersion=1`, `runSeed`, `day`, `worldStateHashBefore`, 항목별 안정 ID |
| 공통 출력 | `success`, `resultCode`, `stableEntityIds[]`, `worldStateHashAfter`, `mutationApplied`, 항목별 관찰 필드 |
| 결정론 | 같은 `runSeed+day+world state+입력`은 안정 ID 순서와 결과까지 동일하다. 다른 seed도 카탈로그·예산·softlock 불변식을 벗어나지 않는다. |
| 자연 경로 | `fresh save`, `grant=false`, `warp=false`, 날짜 skip·save 편집·invulnerability 없음. 캠프 이동, 지도, 수색, 귀환, 설비·프로젝트 상호작용은 실제 공개 입력 경로를 쓴다. |
| 원자성 | preview·cancel·실패는 상태 불변이다. confirm은 한 idempotency key로 전부 반영되거나 전혀 반영되지 않는다. 같은 key 재시도는 추가 비용·손실·점수·로그를 만들지 않는다. |
| 표시 | KO가 의미 기준 원문, EN은 같은 원인·요구·결과를 전달한다. qps-long은 레이아웃 QA 전용이며 플레이 결과 언어가 아니다. |
| 로그 | 허용 필드는 `runSeed`, `day`, `pacingBandId`, `eventId`, `stableEntityId`, `resultCode`, `elapsedSegmentMs`, 항목별 숫자·불리언뿐이다. 이름·계정·IP·장치 ID·자유 입력·좌표 궤적은 금지한다. |
| 증거 구분 | Edit 결정론 probe, Play 실제 상호작용, 인간 자연 플레이를 별개 등급으로 기록한다. 자동 PASS로 아직 미실행인 사용자 6세션을 PASS 처리하지 않는다. |

### 공통 KO/EN 표시 키

| 키 | KO 기준 | EN 의미 | 사용 |
|---|---|---|---|
| `ui.pacing.day-band` | `Day {day}/50 · {bandName}` | `Day {day}/50 · {bandName}` | 현재 날짜와 밴드. 밴드는 안내일 뿐 잠금 조건이 아님 |
| `ui.region.lock-reason` | `잠김: {reason} · 다른 길: {alternative}` | `Locked: {reason} · Alternate: {alternative}` | 우선 잠금 사유 한 개와 대체 해금 단서 |
| `ui.key-part.pity-hint` | `단서 발견: {sourceRegion}` | `Clue found: {sourceRegion}` | eligible 3회 뒤 출처 힌트. 부품 지급 문구가 아님 |
| `ui.hazard.telegraph` | `{hazardName} 예고 · 대상 {target} · 대비 {mitigation}` | `{hazardName} forecast · Target {target} · Prepare {mitigation}` | 위험 예고 |
| `ui.hazard.recovery` | `회복 가능: {recoveryAction}` | `Recovery available: {recoveryAction}` | major 다음 회복 창 |
| `ui.escape.project-action` | `{projectName}: {action}` | `{projectName}: {action}` | 프로젝트마다 다른 현장 상호작용 |
| `ui.escape.ready` | `탈출 준비 완료` | `Escape ready` | 날짜와 무관한 완료 가능 상태 |
| `ui.ending.reason` | `결말: {endingTitle} · 근거 {reason}` | `Ending: {endingTitle} · Reason {reason}` | 단일 ending과 우세 행동 근거 |

문장 순서는 locale별 템플릿에서 바꿀 수 있다. `{...}` 토큰, 안정 ID와 원인 enum은 번역하지 않는다.

## 2. 15개 FAIL 1:1 GREEN 매핑

### T — 날짜 밴드와 조기 탈출

| Gate | 플레이어가 보거나 하는 행동 | 공개 데이터·결정론 probe | 자연 경로 acceptance | 실패·취소·재시도 원자성 | KO/EN·로그 | `SAMPLE_ONLY` 경계 |
|---|---|---|---|---|---|---|
| `W17-T01.day_band_boundaries` | HUD에서 Day 1–10/11–20/21–35/36–49/50의 현재 밴드를 본다. Day 49 미탈출 정산은 Day 50으로, Day 50 정산은 terminal로 간다. | 다섯 `pacing.band.*`의 `startDay/endDay`; probe는 day `1,10,11,20,21,35,36,49,50`을 열거해 경계와 Day49 `continue`, Day50 `day50_settlement`을 반환한다. | fresh run에서 Day 49 정산 뒤 Day 50 진입, Day 50에서 Day 51 미진입을 실제 정산 입력으로 증명한다. | band 조회는 read-only. 정산 중 실패·취소는 날짜·위험·프로젝트 불변, 같은 settlement key 재시도는 날짜를 두 번 올리지 않는다. | `ui.pacing.day-band`; `pacing.band.entered`에 `day,pacingBandId,resultCode`. | 날짜 경계·Day50 terminal은 고정. 밴드별 위험 budget·전망 보정은 SAMPLE이며 최종 난이도가 아니다. |
| `W17-T02.early_escape_no_hardlock` | 요구를 모두 채운 플레이어가 어느 날짜든 프로젝트 현장에서 완료를 누르면 즉시 탈출한다. | 같은 아홉 경계일마다 5개 `escape.*` 중 하나가 fulfilled인 snapshot을 넣고 `escape_complete`가 date lock 없이 Day50 settlement보다 우선함을 반환한다. | smoke와 radio 자연 경로에서 실제 요구를 채운 뒤 완료 상호작용을 실행한다. `grant/warp/skip=0`, 완료일은 어떤 밴드도 허용한다. | 완료 전 cancel·실패는 비용·단계·terminal 불변. 성공 transaction 재시도는 같은 ending만 반환하고 소비·로그·panel을 중복하지 않는다. | `ui.escape.ready`; `escape.completed`의 `escapeMethodId,day,resultCode=escape_complete`. | 프로젝트 준비 기간·비용은 SAMPLE. “충족된 탈출을 날짜가 막지 않음”은 튜닝 불가 고정 규칙이다. |

### R — 지역·시드·pity·softlock

| Gate | 플레이어가 보거나 하는 행동 | 공개 데이터·결정론 probe | 자연 경로 acceptance | 실패·취소·재시도 원자성 | KO/EN·로그 | `SAMPLE_ONLY` 경계 |
|---|---|---|---|---|---|---|
| `W17-R01.six_region_primary_alternative` | 캠프 지도에서 시작 3지역과 잠긴 확장 3지역을 모두 preview하고, 확장 지역마다 현재 우선 사유와 대체 경로를 읽는다. | 정확히 6개 `region.*`; 확장 `region.ridge.highland`, `region.cove.wreck`, `region.ruins.relay`는 primary·alternative 조건과 단일 `lockReasonCode`를 공개한다. probe는 각 경로를 독립 충족해 같은 region ID를 해금한다. | fresh run의 실제 귀환·연구·장비·발견으로 primary 한 건과 alternative 한 건을 각각 해금한다. 권장 밴드 이전 충족도 허용한다. | preview/cancel 불변. unlock confirm은 `regionUnlockId` 한 번만 기록; 부족·실패는 무변경, retry는 보상·점수 중복 없음. | `ui.region.lock-reason`; `region.previewed/unlocked`에 `regionId,unlockRouteId,lockReasonCode`. | 구체 요구 횟수·travel band·전망 보정은 SAMPLE. 6 ID와 두 독립 경로·날짜 비잠금은 고정이다. |
| `W17-R02.seed_forecast_hazard_pity_determinism` | 같은 seed의 같은 날·상태에서는 지도 전망, 위험 예고, 해금과 pity 단서가 저장·재개 후 바뀌지 않는다. | 동일 snapshot을 두 번 평가해 `forecastIds,hazardIds,unlockIds,pityCounters`와 hash가 동일함을 확인하고, 다른 seed는 유효 ID·예산·최소 경로 범위 안 결과를 낸다. | fresh save 두 개를 같은 seed·행동 순서로 진행하고 map→search→return을 재현한다. 다른 seed 한 개는 결과 다양성만 확인한다. | probe는 read-only. preview·locale/input 전환·save reload는 RNG를 소비하지 않는다. 확정 event만 선언된 stream position을 한 번 전진시킨다. | 지도 KO/EN은 같은 풍부함·위험·잠금 의미; 로그에 `runSeed,day,stableEntityId,resultCode`, RNG 내부값·개인정보 없음. | 확률·풍부함 이동·위험 빈도는 SAMPLE. 동일 입력 결정론과 카탈로그 범위는 고정이다. |
| `W17-R03.eligible_search_hint3_guarantee5` | 핵심 부품을 못 찾은 eligible 수색 3회 뒤 출처 단서를 보고, 5회 miss 뒤 다음 eligible 결과에서 부품을 받는다. | key part별 `eligibleSearchCount`; probe cases `eligible-complete`, `cancelled`, `failed`, `unrelated`, `duplicate`, `hint-3`, `miss-5`, `next-guarantee`. 성공 eligible만 증가하고 guarantee는 optional loot보다 먼저 배치한다. | primary/alternative 지역을 실제 선택·수색·귀환한다. hint와 guarantee는 전역 날짜나 비eligible 반복으로 생기지 않는다. | cancel/실패/무관/중복은 counter·loot 불변. guarantee transaction은 가방/보호 inventory 수용 가능할 때 전부 커밋하고 retry로 두 번째 부품을 만들지 않는다. | `ui.key-part.pity-hint`; `key-part.pity-hint/guaranteed`에 `keyPartId,eligibleSearchCount,sourceRegionId`. | `3/5`는 SAMPLE_ONLY. eligible 의미, 핵심 부품 우선 배치, 중복·실패 불계수는 고정이다. |
| `W17-R04.minimum_three_completable_paths` | 플레이어는 하나를 고르되 seed 때문에 가능한 탈출법이 2개 이하로 줄지 않는다. 감사 UI는 노출하지 않는다. | 정확히 `seed_generation`, `expansion_region_unlock`, `day_35_settlement`, `day_49_settlement`에서 5개 `escape.*` 그래프를 감사하고 `completableEscapeIds` 3개 이상을 안정 ID 정렬로 반환한다. | fresh run snapshot을 네 시점에서 실제 진행 상태로 캡처한다. 선택 포기와 softlock을 구분하며, 모든 audit에서 최소 3경로가 기술적으로 완성 가능해야 한다. | audit read는 불변. 실패 시 run 공개 전 또는 unlock transaction 안에서 미보유 key-part 배치만 원자 재추첨; 일반 자원·행동·보유 부품은 보존, retry 결과 동일. | 플레이어 로그에는 audit 내부 배치를 숨기고 `softlock.audit`에 `auditPoint,resultCode,completableEscapeIds`; KO/EN 결과 의미 동일. | 최소 3경로와 네 audit 시점은 고정. 어느 미보유 부품을 재배치할지와 확률은 SAMPLE이다. |

### H — 위험 페이싱·원자성

| Gate | 플레이어가 보거나 하는 행동 | 공개 데이터·결정론 probe | 자연 경로 acceptance | 실패·취소·재시도 원자성 | KO/EN·로그 | `SAMPLE_ONLY` 경계 |
|---|---|---|---|---|---|---|
| `W17-H02.rolling_calm_and_major_recovery` | 위험 예고를 보고 대비하며, 모든 rolling 5일 안에 숨 돌릴 날을 만나고 major 다음 날 회복 행동을 선택할 수 있다. | 동일 seed로 최소 10일 schedule을 산출한다. 모든 rolling 5일 `calmDayCount>=1`; major 다음 날 `reservedRecoveryBudget=2`, `sameFamilyNewMajor=false`; Day50 surprise theft/damage 없음. | 자연 정산 10일 이상에서 예고→major→다음 날 회복을 실제 입력으로 실행한다. hunger와 기존 recovery는 평온일에도 계속된다. | schedule preview는 무변경. 하루 arm은 한 day key로 한 번; cancel은 arm 전 불변. 사용하지 않은 recovery budget은 다른 major로 전용하지 않으며 retry로 중복 예약하지 않는다. | `ui.hazard.telegraph`, `ui.hazard.recovery`; `calm-day.applied`, `hazard.telegraphed/recovered`의 hazard ID·family·budget/result. | rolling `5`, 회복 `2`, band budget은 SAMPLE_ONLY. 예고, 회복 예약, 같은 family major 금지, Day50 surprise 금지는 공정성 고정이다. |
| `W17-H03.atomic_retry_loss_and_keypart_protection` | 도난·파손이 한 번만 적용되고 원인을 보며, 핵심 부품과 이미 끝낸 탈출 단계가 사라지지 않는다. | 같은 `hazardInstanceId`를 두 번 resolve해 첫 호출만 한 food batch 또는 한 facility 상태를 바꾸고 두 번째는 `already_resolved`; protected part, completed stage, score와 추가 log 불변. | 실제 캠프에서 telegraph→armed→resolved→recovering→recovered를 수행하고 mitigation 성공·실패 각 한 번을 확인한다. | 한 transaction이 resource/facility/hazard/score/log를 함께 commit. 취소·오류는 전부 rollback. retry·locale/input 전환은 손실·수리·로그 중복 없음. | 손실/보존 사유를 KO/EN 같은 의미로 표시; `hazard.resolved/recovered`에 `hazardInstanceId,targetId,lossCode,resultCode`, 수량 외 자유문 없음. | 손실량·회복 비용은 SAMPLE. 단일 손실, idempotency, 핵심 부품·완료 stage 보호는 고정이다. |

### E/O/N — 탈출·로그·엔딩

| Gate | 플레이어가 보거나 하는 행동 | 공개 데이터·결정론 probe | 자연 경로 acceptance | 실패·취소·재시도 원자성 | KO/EN·로그 | `SAMPLE_ONLY` 경계 |
|---|---|---|---|---|---|---|
| `W17-E02.smoke_radio_natural_interaction_routes` | 연기는 연료·점화·연기 유지 설비를 직접 다루고, 무전은 전원·주파수·송신 설비를 직접 다룬다. 같은 전역 완료 버튼으로 대체하지 않는다. | `smoke.route.smoke`와 `smoke.route.radio`를 별도 probe로 실행해 각각 고유 `interactionIds`, `actualInteractionCount>0`, `grant=false`, `warp=false`, `terminalResult=escape_complete`를 반환한다. | 두 fresh 30분 smoke fixture를 별도 run으로 실제 캠프 이동→지도→수색→귀환→현장 프로젝트 상호작용까지 실행한다. | preview/cancel/실패는 자원·단계 불변. 각 milestone 원자 commit; retry는 비용·점수·terminal 중복 없음. 한 경로 실패가 다른 경로 완료 단계를 지우지 않는다. | `ui.escape.project-action`; event에 `escapeMethodId,interactionId,milestoneId,resultCode,grantUsed=false,warpUsed=false`. | 비용·준비 시간·위험 강도는 SAMPLE. smoke/radio의 서로 다른 직접 상호작용과 no-debug 자연 경로는 고정이다. |
| `W17-E03.raft_flare_beacon_data_only` | 플레이어용 build에는 playable로 속여 노출하지 않는다. 구현/QA는 세 경로의 데이터와 resolver만 검증한다. | `escape.raft/flare/beacon` 각각 region/research/facility/keyPart/material/preparation/risk, primary+alternative, snapshot schema와 원자 result validator를 제공하며 `playableState=data-only`. | 실제 사용자 자연 경로 PASS를 요구하거나 주장하지 않는다. catalog load와 결정론 snapshot 검증만 자연 캠페인 그래프와 같은 안정 ID를 사용한다. | validator read-only. sample result transaction은 성공·지연·불발 등 선언 필드만 한 번 변경하고 다른 완료 stage·부품을 보존한다. | KO/EN catalog 제목·요구 의미는 준비하되 CTA는 비노출; 로그에 `escapeMethodId,playableState=data-only,resultCode`. | 세 경로의 모든 준비 일수·비용·확률은 SAMPLE. data-only 경계와 세 안정 ID는 고정이다. |
| `W17-O01.snapshot_and_private_log` | 저장·재개 후 같은 지역·위험·프로젝트·행동 상태를 계속한다. 플레이어 개인 정보는 요구하지 않는다. | snapshot은 `runSeed,day,pacingBandId,regionStates,hazardStates,projectStates,behaviorScores,protectedKeyPartIds`; event log는 공통 허용 필드만 schema로 열거하고 금지 필드 scan 0을 반환한다. | fresh run을 저장→재개해 다음 실제 상호작용 결과가 동일함을 확인한다. 로그 없이도 게임은 진행하며 logging 실패가 gameplay transaction을 실패시키지 않는다. | snapshot write는 temp→commit 원자 교체, 실패 시 이전 snapshot 유지. event idempotency key 재시도는 한 줄만 남기며 gameplay state를 재적용하지 않는다. | KO/EN 표시 상태는 같은 ID·원인; 구조 로그만 사용하고 `name/account/ip/deviceId/freeText/rawInput/positionTrace` 금지. | 저장 schema·privacy는 밸런스가 아니다. event 빈도·elapsed 값은 분석용이며 SAMPLE 수치의 승격 증거가 아니다. |
| `W17-N02.priority_tiebreak_and_hysteresis` | 탈출 완료 시 Day50 잔류보다 탈출 결말을 보고, 결과 화면에서 우세 생활 방식과 근거를 읽는다. 마지막 버튼 한 번으로 생활 방식이 뒤집히지 않는다. | 동일 terminal snapshot 두 번→동일 ending/reason/identity; early escape vs Day50→escape 우선; 동점→기존 identity, 성립 day, stat ASCII; established identity에 challenger `+2`→불변. | fresh smoke/radio terminal과 Day50 settlement snapshot을 실제 행동 누적에서 만든다. debug score grant는 금지한다. | resolver는 pure/read-only. 결과 표시 재열기·cancel·locale 전환은 ending/score 불변; gallery unlock은 동일 ending key로 한 번만 commit. | `ui.ending.reason`; `behavior.identity-updated`, `ending.resolved`에 stat·ending·reasonCode만, 자유문 없음. | 1–2점, cap4, 후보12, lead4/switch6은 SAMPLE. terminal 우선순위·tie-break 순서·한 행동 비전복 원칙은 고정이다. |

### P — 실제 Play 표면

| Gate | 플레이어가 보거나 하는 행동 | 공개 데이터·결정론 probe | 자연 경로 acceptance | 실패·취소·재시도 원자성 | KO/EN·로그 | `SAMPLE_ONLY` 경계 |
|---|---|---|---|---|---|---|
| `W17-P01.live_hazard_lifecycle` | Play 상태에서 부상·재해·식량 도난의 예고→발생→완화→회복을 화면과 직접 입력으로 경험한다. | Play probe가 `hazard.injury/disaster/food-theft` 각각 네 phase와 고유 `hazardInstanceId`, active surface ID, transition result를 기록한다. reflection-only 결과는 불합격이다. | 실제 Play에서 세 위험을 각 1회 자연 상호작용으로 진행한다. 세션은 자동 fixture이며 인간 6세션과 구분한다. | phase 실패/cancel 무변경, 같은 instance 재시도 추가 손실 없음, 회복 완료 뒤 재호출 `already_recovered`. | 각 phase KO/EN 원인·대응 의미 동일; 구조 event만 저장. | 발생 확률·피해·회복 비용은 SAMPLE. 네 phase와 실제 Play 표면·idempotency는 고정이다. |
| `W17-P02.live_smoke_radio_natural_paths` | Play 상태에서 연기와 무전의 서로 다른 설비로 이동해 각 프로젝트를 완료한다. | 두 Play probe가 별도 interaction trace, `actualInteractionCount>0`, `grantCount=0`, `warpCount=0`, terminal escape ID를 반환한다. | fresh save 두 개에서 실제 이동·지도·수색·귀환·설비 입력만 사용한다. 한 probe가 두 method를 동시에 완료하면 실패다. | interaction 실패/cancel은 stage·cost 불변; retry 한 번만 commit; terminal 재호출로 다른 ending이나 두 번째 소비 없음. | KO/EN CTA는 method별 같은 행동 의미; 로그 필드는 E02와 동일하고 raw input은 저장하지 않는다. | 시간·비용은 SAMPLE. 두 경로의 서로 다른 live 자연 상호작용 증거는 고정이다. |
| `W17-P03.live_terminal_priority_and_three_panels` | 조기 탈출 또는 Day50 결과 뒤 setup→escalation→punchline의 core comic panel 정확히 3개를 보고 닫거나 다시 본다. | 기존 Wave15 sample ending ID 4개를 Play resolver에 넣어 우선순위·ending ID와 `panelRoleCode=setup|escalation|punchline` 정확히 3개 active hierarchy를 기록한다. 새 content ID는 만들지 않는다. | smoke, radio, Day50 생활형 중 실제 terminal 하나 이상을 자연 행동으로 만들고 결과 화면을 연다. 나머지 snapshot은 결정론 fixture로 검증한다. | panel 닫기·재열기·locale 전환은 terminal/점수/해금 불변. 중간 로드 실패는 terminal snapshot을 보존하고 같은 panel 순서로 재시도한다. | panel title/body는 ending별 기존 KO/EN 키를 사용하고 `ui.ending.reason` 의미 동일; 로그는 `endingId,panelRoleCode,panelIndex,resultCode`. | 패널 최종 아트·애니메이션·문구 길이는 미확정. 정확히 3개 core role과 terminal 우선순위는 구현 smoke 계약이며 새 ending이 아니다. |

## 3. 구현 순서와 GREEN 판단

1. `T01/T02/R01–R04`의 공개 데이터와 pure probe를 먼저 열어 seed·날짜·경로 불변식을 잠근다.
2. `H02/H03`의 scheduler·transaction을 붙이고 핵심 부품·완료 단계 보호를 위 1단계 audit에 연결한다.
3. `E02/E03/O01/N02`로 두 playable route, 세 data-only route, snapshot/log, ending resolver를 완성한다.
4. `P01–P03`에서 같은 공개 의미 표면을 Play 실제 입력에 연결한다. Edit fixture와 다른 숨은 치트 경로를 만들지 않는다.

Wave 18 GREEN은 다음을 모두 만족할 때만 선언한다.

- fresh post-implementation SHA에서 지정 15개가 전부 PASS하고 `EXPECTED_GAP/FAIL=0`이다.
- Wave 15/16 GREEN과 인프라 PASS가 그대로다.
- 모든 공개 probe가 동일 seed 재실행에서 동일 결과를 내고, 자연 경로에는 `grant/warp/skip=0`이 기록된다.
- 최소 3경로 audit 네 시점, eligible-only pity, 평온일·회복 예약, 핵심 부품 보호, smoke/radio 별도 상호작용, ending hysteresis, privacy allowlist가 각각 독립 PASS다.
- `escape.raft/flare/beacon`은 data-only를 넘어 playable PASS로 보고되지 않는다.
- 리뷰 전용 아트는 사용자 채택 전 runtime에 연결하지 않는다.
- 실제 first-user ko 3/en 3 자연 50일 6세션은 계속 `UNRUN`; 물리 게임패드와 Steam은 `UNVERIFIED`다.

## 4. QA 증거 인덱스

각 게이트는 결과 JSON에 `probeId`, 실행 모드 `edit|play`, baseline SHA, 입력 snapshot ID, 결과, stable IDs, before/after hash, mutation count를 남긴다. `P01–P03`은 Play 증거가 없으면 Edit PASS로 대체할 수 없다. E02/P02는 smoke와 radio 결과를 별도 객체로 남긴다. O01 privacy scan은 허용 필드 목록과 금지 필드 발견 수 `0`을 함께 기록한다.

통합 재실행은 기존 Wave 17 게이트를 새 구현 SHA에 대해 fresh로 수행한다. 과거 증거의 baseline SHA를 바꾸거나 기존 FAIL JSON을 수정하지 않는다.

## 5. 미검증·금지

- 인간 첫 사용자 6세션, 실제 자연 Day50 완주, 물리 게임패드, Steamworks: `UNRUN/UNVERIFIED`.
- 최종 자원 비용·드롭·허기/체력 소모·위험 빈도·준비 기간·pity·행동 임계값: 여전히 `SAMPLE_ONLY`, E3 전 승격 금지.
- 새 탈출법, 새 지역, 새 위험, 새 엔딩, 새 전역 메뉴, 아트 채택: 이 계약 밖이다.
- 구현자가 안정 ID를 개명하거나 QA 전용 reflection 표면으로 공개 계약을 대체하는 것은 불합격이다.

현재 구현을 막는 추가 설계 질문은 없다. 구현 파일 소유권과 내부 클래스 배치는 구현 담당자가 기존 Runtime 구조 안에서 정하되, 여기 정의한 공개 ID·입출력·원자성·증거 계약은 바꾸지 않는다.

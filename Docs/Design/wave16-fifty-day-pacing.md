# Wave 16 50일 SAMPLE_ONLY 페이싱 계약

- 상태: `DESIGN CONTRACT COMPLETE / IMPLEMENTATION·PLAYTEST UNRUN`
- 기준: `origin/master@635725b3e2679a7d6d4f66c09b137575bac374c8`
- 기계 정본: `.forge/design/project.json`의 `campaignPacingContract`
- 입력 정본: `campaign.wave15.escape-hazard-ending-matrix.v1`
- 밸런스 상태: `SAMPLE_ONLY_NOT_FINAL_FIFTY_DAY_BALANCE`

이 문서는 Wave 15의 고정 카탈로그를 바꾸지 않고 50일 동안 선택의 성격이 변하는 시간축을 정의한다. Day 50 기본 데드라인, Day 50 이전 조기 탈출, `escape.*` 5개, `region.*` 6개, `hazard.*` 7개, `ending.*` 19개와 캠프 지도 상호작용은 고정이다. 아래 일수·예산·풍부함 이동·임계값은 구현 smoke와 첫 장기 플레이테스트용 표본이며 최종 밸런스가 아니다.

## 1. 페이싱 원칙

1. 날짜 밴드는 권장 곡선이지 진행 퀘스트가 아니다. 조건을 충족하면 어느 밴드에서든 확장 지역을 일찍 열고 탈출할 수 있다.
2. 새 선택은 `새 지역 → 다른 자원 전망 → 다른 위험·장비 → 다른 탈출 프로젝트`의 인과로 생긴다. 단순 수확량 상승이나 동일 수색의 강제 반복으로 시간을 채우지 않는다.
3. 같은 지역을 다시 골라도 지도에는 날씨, 위험, 특별 발견, 활성 탈출 프로젝트 중 최소 한 축이 달라질 수 있어야 한다. 플레이어가 일부러 같은 안전 루프를 택하는 것은 허용한다.
4. 진행 보호는 핵심 부품을 선물하는 전역 타이머가 아니라 primary·alternative 지역의 `eligible-search` pity와 최소 3개 완성 가능 경로 검사로 처리한다.
5. 위험 압력은 예고와 회복 창을 포함한 예산 안에서만 높아진다. 도난·파손은 핵심 부품을 지우거나 한 사건을 두 번 차감하지 않는다.

## 2. Day 1–50 일일 밴드

| 밴드 ID | 날짜 | 플레이 경험 목표 | 지도·수색 변화 | 탈출 프로젝트 기대 | 위험·회복 표본 | 관찰 가능한 통과 조건 |
|---|---:|---|---|---|---|---|
| `pacing.band.onboarding` | 1–10 | 세 시작 지역의 전망을 비교하고 준비→수색→귀환→정산을 이해 | 해변·숲·얕은 바다 공개. 첫 3회 지도 갱신에서 전체 후보가 완전히 같은 전망 조합으로 반복되지 않음 | 5개 프로젝트 preview 가능. 연기·무전의 다음 요구를 읽되 날짜로 제작을 막지 않음 | 신규 위험 budget 2, 새 major 0. 굶주림은 별도 결정론 상태 | Day 10까지 강제 체크리스트는 없지만 첫 귀환, 위험 예고 확인, 프로젝트 preview가 로그로 구분됨 |
| `pacing.band.expansion` | 11–20 | 장비·연구·발견을 통해 첫 확장 지역을 열고 짧은 경로와 먼 경로를 비교 | 고지대·난파선 만·폐중계소는 조건 충족 순으로 공개. 첫 성공 귀환 뒤 long→medium 친숙화 가능 | 한 프로젝트의 핵심 부품 출처와 대체 출처를 설명할 수 있음 | budget 3, 새 major 최대 1, 동시 active 최대 2 | 하드 Day 잠금 없이 primary 또는 alternative 해금이 재현되고, 미해금 이유가 한 가지 우선 사유로 표시됨 |
| `pacing.band.compound-choice` | 21–35 | 생존·캠프 보강·여러 탈출 계획 사이에서 투자와 전환을 선택 | 6지역 모두 조건상 discoverable. 활성 프로젝트에 맞는 전망 보정과 위험이 함께 보임 | 한 경로를 밀거나 다른 경로로 pivot. 완성 조건 충족 시 즉시 조기 탈출 | Wave 15 상한 budget 4, 새 major 1, active 2. rolling 5일 안에 최소 평온일 1회 | 적어도 3개 탈출 경로의 획득 그래프가 seed 검사에서 열려 있고, 동일 행동 반복 없이 2개 이상 프로젝트 진척 선택이 존재 |
| `pacing.band.finish-pressure` | 36–49 | 남은 기간과 생존 여유를 보고 완성·수리·우회 중 하나를 결정 | 미완성 활성 프로젝트의 일반 재료 전망을 eligible 지역에서 한 단계 올릴 수 있음. 핵심 부품은 pity만 사용 | 완성, 복구, 경로 전환이 모두 가능하며 완료 즉시 terminal | budget 4 상한 유지. major 뒤 다음 날 recovery 2 예약, 같은 family major 금지 | Day 49 미탈출이어도 run은 계속되고, 경고가 남은 일수·막힌 요구·대체 출처를 구분해 표시 |
| `pacing.band.resolution` | 50 | 마지막 행동 뒤 탈출 완료 또는 잔류 엔딩을 결정 | 새 장기 해금 보너스 없음. 이미 공개된 지역·전망·pity 상태 유지 | 그날 완료된 탈출은 `escape_complete`가 우선. 미완료면 정산 뒤 Day 50 엔딩 | 예고되지 않은 신규 도난·파손 major 금지. 기존 위험의 해결·결과만 적용 | 같은 terminal snapshot은 같은 ending ID를 내고 Day 51로 진행하지 않음 |

`신규 위험 budget`은 랜덤으로 새로 arm할 수 있는 가중치다. critical hunger는 Wave 15처럼 랜덤 roll이 아니며 active slot을 예약한다. 숙련자가 Day 10 이전에 확장 조건이나 탈출 조건을 달성했을 때 시스템은 날짜를 이유로 막지 않는다.

## 3. 지역 해금·대체 경로·이동·풍부함

### 3.1 해금 계약

시작 지역은 Day 1부터 공개된다. 확장 지역의 `권장 밴드`는 노출 곡선일 뿐 하드 날짜 조건이 아니다.

| 지역 | 기본 이동 | primary 해금 표본 | alternative 해금 표본 | 친숙화 | 권장 밴드 |
|---|---|---|---|---|---|
| `region.coast.beach` | short | 새 run 즉시 | 해당 없음 | 계속 short | Day 1–10 |
| `region.forest.grove` | short | 새 run 즉시 | 해당 없음 | 계속 short | Day 1–10 |
| `region.sea.shallows` | medium | 새 run 즉시, 실제 진입에는 `equipment.swim-ready` 요구 | 장비가 없으면 지도 preview만 유지 | 수영 준비 상태로 성공 귀환 2회 뒤 short | Day 1–10 |
| `region.ridge.highland` | long | 숲 성공 귀환 2회 + `discovery.old-trap-line` + `tool.rope` | 해변·숲 각 1회 귀환 + `research.ropework` + `equipment.weatherproof-kit` | 요구 장비를 갖춘 첫 성공 귀환 뒤 medium | Day 11–20 |
| `region.cove.wreck` | long | 얕은 바다 성공 귀환 2회 + `discovery.wreck-chart` + 수영 준비·밧줄 | 난파선 지도 미발견 상태에서 얕은 바다 eligible-search 3회면 위치 힌트 공개 | 요구 장비를 갖춘 첫 성공 귀환 뒤 medium | Day 11–20 |
| `region.ruins.relay` | long | 고지대 귀환 + `discovery.weather-log` + 밧줄·절연 장비 | 난파선 만 귀환 + `discovery.radio-chassis` + `research.electronics` + 절연 장비 | 요구 장비를 갖춘 첫 성공 귀환 뒤 medium | Day 21–35 |

해금 transaction은 preview에서 자원·상태를 바꾸지 않는다. 확정 시 `regionUnlockId` 한 건만 기록하고, 같은 조건을 다시 처리해도 중복 해금·점수·보상은 없다. 장비 부족은 지역의 존재를 숨기지 않고 `preview_locked_equipment`로 보여 준다.

### 3.2 지도에 보이는 풍부함 변화

기본 `풍부/보통/희귀` 표는 Wave 15를 그대로 쓴다. 아래는 정확한 드롭량을 바꾸지 않는 범주형 SAMPLE modifier다.

| 날짜대 | 시작 3지역 | 확장 3지역 | 반복 방지 규칙 |
|---|---|---|---|
| Day 1–10 | Wave 15 baseline. 날씨가 한 범주에만 ±1 전망을 줄 수 있음 | 미해금 상태에서도 이름·대표 실루엣·해금 단서 preview | 세 지도 갱신 연속으로 3개 후보의 `자원 전망+위험+특별 발견` 조합이 모두 같지 않음 |
| Day 11–20 | baseline + 활성 프로젝트 일반 재료 한 범주 `+1` 후보 | 최초 해금일은 기존 abundant 범주와 special discovery 가능성을 명시 | 같은 지역 3회 연속 선택 시에도 날씨·위험·발견 중 하나는 달라질 수 있으나 결과를 강제하지 않음 |
| Day 21–35 | 활성 프로젝트 일반 재료 `+1`, 비활성 common 한 범주 `-1` 가능. scarce 미만·abundant 초과 금지 | baseline + 장비가 맞으면 안전 경로 전망. 잘못된 장비는 수량 감소가 아니라 위험 상승으로 표시 | 모든 지역을 고르게 방문할 의무 없음. 지도는 최소 2개의 합리적 선택을 제시 |
| Day 36–49 | 미완성 milestone의 비핵심 재료가 있는 eligible 지역 한 곳을 `+1` | 동일. 핵심 부품은 전망 보정이 아니라 pity 3/5만 적용 | 완성 재료를 숨기는 전역 고갈 금지. 위험 때문에 우회할 alternative를 최소 1개 유지 |
| Day 50 | 직전 전망과 pity 상태 유지 | 새 장기 보너스·신규 지역 잠금 없음 | terminal 전에 보여 준 전망과 실제 resolver 입력이 일치 |

`+1/-1`은 `희귀↔보통↔풍부` 한 단계다. 자원 비용, 실제 획득량, 생존 소모는 이번 Wave에서 확정하거나 변경하지 않는다.

## 4. 위험 페이싱과 공정한 캠프 피해

### 4.1 예고·예산·평온일

| 상태 | 규칙 |
|---|---|
| 예고 | 지도 선택 전 위험 family·강도 범주·영향 대상을 표시한다. 캠프 위험은 정산 전날 흔적 또는 일기예보로 arm한다. |
| 일일 예산 | Day 1–10: 2 / Day 11–20: 3 / Day 21–49: 4 / Day 50: 예고된 결과만. severity 1/2/3, 새 major 1, active 2 상한은 유지한다. |
| 평온일 | rolling 5일마다 최소 1일은 랜덤 신규 위험 weight 0~1만 허용한다. hunger 진행과 이미 active인 회복은 계속된다. |
| 회복 창 | major 결과 다음 날 budget 2를 회복에 예약하고 같은 family의 새 major를 금지한다. 회복 행동을 하지 않아도 예약은 다른 major로 전용되지 않는다. |
| 중첩 | hunger가 critical이면 active slot 1개를 점유한다. major(3)+moderate(2)처럼 budget을 넘는 조합은 arm하지 않는다. |

### 4.2 식량 도난·캠프 파손 단계

1. `telegraphed`: 발자국·흔들린 잠금·폭풍 경고와 영향 후보를 보여 준다. 손실 없음.
2. `armed`: 다음 정산까지 보강·이동·덫·수리를 선택할 수 있다. 취소와 locale/input 전환은 무변경.
3. `resolved`: 미완화 도난은 보호되지 않은 식량 batch 하나만, 파손은 설비 하나만 `working→damaged`로 바꾼다. 한 raid는 한 `hazardInstanceId`와 한 원자 transaction을 쓴다.
4. `recovering`: 회수·재보급 또는 결정론적 수리 경로를 노출한다. 핵심 부품·완성된 탈출 단계·다른 설비는 불변이다.
5. `recovered`: 같은 idempotency key로 재호출해도 자원·점수·상태가 다시 변하지 않는다.

첫 무방비 침입도 예고 단계를 건너뛰지 않는다. Day 50에는 이전부터 arm된 결과만 해결하며, 엔딩 직전 새 surprise theft/damage를 만들지 않는다.

## 5. 핵심 부품 pity와 softlock 보호

- primary/alternative 지역에서 해당 부품이 없는 상태로 완료한 수색만 `eligible-search`다.
- `SAMPLE_ONLY 3`: 세 번째 eligible-search 뒤 대체 출처를 지도에 공개하고 다음 eligible 결과 가중치를 올린다.
- `SAMPLE_ONLY 5`: 다섯 번째까지 미발견이면 다음 eligible 결과에서 optional loot보다 먼저 보장한다.
- 중복 부품, 실패·취소한 수색, unrelated 지역은 counter를 초기화하거나 증가시키지 않는다.
- 부품을 얻으면 보호 project inventory로 이동한다. 일반 도난·파손은 삭제하지 못하고, 명시적 손상은 결정론적 수리 경로를 동반한다.
- seed 생성, 확장 지역 해금, Day 35, Day 49에 경로 감사를 실행한다. 매번 primary+alternative chain으로 완성 가능한 탈출 method가 최소 3개여야 한다. 부족하면 일반 자원이나 player state가 아니라 미획득 핵심 부품 배치만 재추첨한다.

## 6. 30분 smoke slice

30분 프로필은 표준 50일 밸런스가 아니라 대표 상태기계의 실행용 압축 fixture다. 새 run 시점에 deterministic seed·pacing profile을 선택하며, 진행 중 grant·warp로 상태를 건너뛰지 않는다. 모든 이동·지도 선택·귀환·위험 대응·프로젝트 확인은 실제 상호작용 경로를 사용한다.

| 시간 | `smoke.route.smoke` | `smoke.route.radio` | 공통 검증 |
|---:|---|---|---|
| 0–4분 | 캠프 이동→지도→숲 전망 비교 | 캠프 이동→지도→얕은 바다 전망 비교 | 직접 지도 상호작용, Day 1/50, 위험 예고 |
| 4–10분 | 숲 수색·귀환, 촉진제 primary counter | 얕은 바다 수색·귀환, 난파 지도·송수신기 alternative counter | 실제 보상 profile, 귀환 정산, 행동 점수 |
| 10–16분 | 고지대 해금 primary/alternative 단서 | 난파선 만 또는 폐중계소 해금 단서 | 날짜 하드 잠금 없음, 잠금 사유·대체 경로 표시 |
| 16–22분 | 고지대/숲 재수색, 연기 설비 milestone | 난파선 만/폐중계소 수색, 무전 설비 milestone | pity 3 hint 또는 정상 획득 중 하나, 원자 project progress |
| 22–27분 | 재해·캠프 피해 예고→완화→회복 | 도난·부상 예고→완화→회복 | budget, calm/recovery, 중복 차감 없음 |
| 27–30분 | `escape.smoke` 완료→대표 smoke ending | `escape.radio` 완료→대표 radio ending | 조기 탈출 우선, 결정론 ending snapshot·개인정보 없는 로그 |

두 경로는 각각 독립 30분 세션이다. 시간 초과는 즉시 밸런스 실패가 아니라 `발견성 / 이동·전환 / 자원 획득 / 위험 회복 / 프로젝트 요구` 중 병목 축을 기록하는 결과다.

### data-only 3경로 검증 지점

| 경로 | catalog load | 해금 그래프 | 중간 snapshot | terminal snapshot |
|---|---|---|---|---|
| `escape.raft` | 해변·얕은 바다·난파선 만, 연구·출항 설비·돛천 참조 유효 | primary와 alternative가 최소 하나씩 reachable | Day 20/35 fixture에서 선체 단계·날씨 창·보급이 독립 필드 | 안전 창 성공 또는 지연/표류 실패가 다른 완료 단계를 삭제하지 않음 |
| `escape.flare` | 해변·얕은 바다·난파선 만, 화공·발사기·카트리지 참조 유효 | 목격 창과 부품 경로가 날짜 잠금 없이 reachable | Day 20/35 fixture에서 단일 카트리지와 witness state 분리 | 성공·불발·무목격이 한 transaction만 소비 |
| `escape.beacon` | 고지대·폐중계소·숲, 이중 연구·relay anchor·코일 참조 유효 | relay primary/alternative와 pity chain reachable | Day 35/49 fixture에서 구조·발전기·신호두 단계가 보존 | 낙뢰 trip이 powered만 바꾸고 구조 stage·핵심 부품을 지우지 않음 |

이 세 경로는 catalog·resolver·softlock 시뮬레이션만 통과한 상태를 `playable PASS`로 보고하지 않는다.

## 7. 행동 점수·히스테리시스

Wave 15의 SAMPLE 값을 유지한다: 의미 행동 1~2점, 통계별 하루 cap 4, 첫 후보 누적 12·2위보다 4점 우세, 기존 우세 교체는 6점 우세다. `stat.swimming/farming/hunting-trapping/mechanics/building/search/hazard-response`만 사용한다.

- 입력 spam, 이동 시간, preview·cancel, 마지막 탈출 완료 버튼은 생활 방식 점수를 주지 않는다.
- Day 10·20·35·49와 terminal에 동일 resolver로 identity snapshot을 기록한다. 중간 snapshot은 엔딩을 잠그지 않는다.
- challenger의 단일 최대 행동은 2점이므로 established identity의 6점 switchLead를 한 번에 넘지 못한다.
- 같은 점수는 기존 우세 유지→최초 후보 성립 day→stat ID ASCII 순으로 결정한다. 마지막 행동 하나로 run 전체의 우세 생활 방식을 뒤집지 않는다.
- 숫자는 여전히 SAMPLE_ONLY다. 50일에서 너무 빨리 고정되거나 끝까지 후보가 없다는 플레이테스트 증거가 있으면 다른 축과 동시에 바꾸지 않고 점수 축만 조정한다.

## 8. 예상 플레이 세션과 관찰

| 프로필 | 예상 선택 곡선 | 예상 탈출 창 | 반드시 관찰할 값 | 확인 질문 |
|---|---|---|---|---|
| 초보 생존형 | Day 1–15 시작 지역·식량·캠프 보강 중심, 위험한 확장 지연, 연기 또는 뗏목 선호 | Day 36–49 또는 Day 50 잔류 | 첫 확장 해금 day, critical hunger 일수, 평온일 활용, 프로젝트 preview→commit 간격 | “왜 오늘 그 지역을 골랐나요?”, “막힌 이유가 자원인지 위치/장비인지 구분됐나요?” |
| 탐험형 | 여러 지역을 빠르게 열고 발견·대체 경로를 비교, raft/flare/smoke 사이 전환 | Day 25–40 | unique region 수, 연속 동일 지역 선택, pity hint·guarantee, 여행 시간 비중, project pivot | “새 지역이 기존 지역과 무엇이 달랐나요?”, “다른 출처가 공정하게 보였나요?” |
| 기술형 | 난파선·폐중계소, mechanics/building, radio/beacon 단계 투자 | Day 30–45 | relay 해금 경로, radio/beacon milestone 간격, 수리·위험 대응, identity switch 시도 | “프로젝트가 같은 제작 버튼의 다른 이름처럼 느껴졌나요?”, “위험 때문에 계획을 바꾼 순간이 있었나요?” |

예상 탈출 창은 목표 분포이지 합격 보장이나 날짜 제한이 아니다.

### 개인정보 없는 telemetry

`pacing.band.entered`, `region.previewed`, `region.unlocked`, `expedition.selected`, `expedition.returned`, `resource.forecast.shown`, `hazard.telegraphed`, `hazard.resolved`, `hazard.recovered`, `calm-day.applied`, `key-part.pity-hint`, `key-part.pity-guaranteed`, `escape.project-previewed`, `escape.project-progressed`, `escape.completed`, `behavior.identity-updated`, `ending.resolved`를 run-local seed·day·stable ID·결과 코드와 함께 기록한다. 이름, 계정, IP, 자유 입력은 수집하지 않는다.

플레이테스트에서는 다음을 묻는다.

1. 직전 5일 동안 선택이 달라진 가장 큰 이유는 무엇이었는가?
2. 지역 잠금·자원 부족·장비 부족·위험 회피 중 현재 막힘을 구분할 수 있었는가?
3. 위험은 언제 알았고 어떤 대응이 가능하다고 이해했는가?
4. 탈출 계획을 유지하거나 바꾼 이유와 대체 부품 출처를 설명할 수 있는가?
5. 엔딩의 생활 방식이 자신의 실제 플레이와 맞았는가?

## 9. 한 축 조정 규칙과 확정 금지

한 검증 빌드에서는 다음 중 한 축만 바꾼다.

1. `REGION_ACCESS`: 해금 요구·힌트 시점
2. `TRAVEL_TIME`: short/medium/long과 친숙화 횟수
3. `RESOURCE_FORECAST`: 범주형 ±1 빈도
4. `HAZARD_PRESSURE`: 일일 budget·major/active 상한
5. `RECOVERY_WINDOW`: 평온일·회복 예약
6. `ESCAPE_PREPARATION`: 프로젝트 단계·표본 준비 기간
7. `IDENTITY_THRESHOLD`: 점수·cap·lead

변경 전후에는 같은 seed, 같은 경로 fixture, 같은 ending snapshot을 재실행한다. 기술 결함·정보 이해·발견성과 밸런스를 먼저 분리한다. 유효한 실제 세션 없이 비용·드롭·소모·준비 일수·pity·위험 빈도·엔딩 임계값을 정식 값으로 잠그지 않는다.

확정 금지 항목은 최종 자원 비용과 획득량, 최종 생존 소모, 최종 날씨·위험 빈도, 최종 준비 기간, 사용자 설정 run, 19개 엔딩 최종 아트·음향, Steamworks 업적이다. Wave 15 안정 ID·카탈로그 수·원자성·판정 우선순위·Day 50과 조기 탈출 규칙은 숫자 튜닝 축이 아니다.

## 10. 구현·QA 수용 조건

- 기계 계약은 5개 method, 6개 region, 7개 hazard, 19개 ending과 정확히 같은 안정 ID를 참조하며 새 동의어 ID를 만들지 않는다.
- Day band 경계는 1/11/21/36/50이고 Day 50만 deadline terminal이다. 조기 탈출·조기 지역 해금을 날짜가 막지 않는다.
- 3개 시작 지역은 Day 1 preview 가능하고 3개 확장 지역은 primary·alternative·잠금 사유·친숙화 travel band를 가진다.
- 같은 seed·day·world state는 같은 지도 전망, 위험 arm, pity와 해금 결과를 낸다.
- 위험 budget·평온일·회복 창·도난/파손 원자성이 자동 검증되며 핵심 부품과 완료 프로젝트 단계는 보존된다.
- smoke/radio 각각 30분 fixture가 실제 상호작용 경로로 terminal까지 실행되고, raft/flare/beacon은 data-only 결과와 playable 결과를 분리한다.
- seed 생성·확장 해금·Day 35·49에서 최소 3개 탈출 경로 softlock 검사가 통과한다.
- 행동 snapshot은 Wave 15 SAMPLE 히스테리시스를 사용하고 한 최대 행동으로 established identity가 바뀌지 않는다.
- ko/en은 같은 원인·잠금·위험·결과 의미를 전달하고 qps-long은 QA 전용 레이아웃 검증에만 사용한다.
- 기존 `done` task와 채택 자산 상태는 변경하지 않으며 실제 장기 사용자 run·물리 게임패드·최종 밸런스는 실행 증거 없이 PASS로 기록하지 않는다.

## 11. 열린 질문

현재 구현 smoke를 막는 설계 질문은 없다. 첫 장기 플레이테스트 뒤 어느 단일 축을 먼저 조정할지는 telemetry와 관찰 기록으로 결정하며, 지금 미리 정하지 않는다.

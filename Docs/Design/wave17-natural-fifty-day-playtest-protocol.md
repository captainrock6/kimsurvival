# Wave 17 자연 50일 플레이테스트·튜닝 프로토콜

- 상태: `PROTOCOL COMPLETE / ALL HUMAN SESSIONS UNRUN`
- 기준: `origin/master@a5403173f299abc71ed4724bdaaf30c31ce8cc94`
- 기계 정본: `.forge/design/project.json`의 `naturalFiftyDayPlaytestProtocol`
- 입력 정본: `campaign.wave15.escape-hazard-ending-matrix.v1`, `campaign.wave16.fifty-day-pacing.v1`
- 적용 시점: Wave 15·16 구현과 자동 게이트가 GREEN인 이후

이 문서는 구현·밸런스 결과가 아니라 실행 프로토콜이다. 아직 실제 사용자 50일 세션, 물리 게임패드 실기, Steam 검증은 한 번도 수행하지 않았으며 모두 `UNVERIFIED`다. 첫 6세션은 문제를 찾고 단일 튜닝 축을 고르는 진단 표본이다. 이 여섯 결과만으로 Wave 16 `SAMPLE_ONLY` 값을 정식 밸런스로 승격하지 않는다.

## 1. 고정 조건

- 동일한 서명된 Windows 빌드·콘텐츠 버전·telemetry schema를 6세션 동안 유지한다.
- 입력은 키보드·마우스로 통일한다. 물리 게임패드는 별도 후속 실기이며 이번 결과와 합치지 않는다.
- 한국어 3명, 영어 3명은 게임을 처음 보는 사용자다. 동일인이 두 locale 또는 두 seed를 반복하지 않는다.
- 참가자에게 생존형·탐험형·기술형, 조기 탈출, Day 50 잔류를 목표로 지시하지 않는다. 표의 성향은 분석자가 확인할 숨은 관찰 렌즈다.
- 참가자 공통 과제는 “김씨가 섬에서 살아가도록 선택하고, 결말에 도달해 주세요.” 한 문장뿐이다. 정답·추천 경로·위험 해법을 말하지 않는다.
- run은 새 게임에서 지정 seed로 시작한다. 저장·재개는 허용하지만 같은 save와 build를 유지한다.
- 플레이 중 pause·휴식은 일일 정산 뒤에만 권장하며 휴식 시간은 active play time에서 뺀다.

## 2. 6세션 배치

seed는 locale 비교를 위해 쌍으로 사용한다. 숫자는 프로토콜 fixture ID이며 세션 전 자동 seed audit가 최소 3개 완성 가능 탈출 경로를 확인해야 한다. audit 실패 시 좋은 seed로 몰래 교체하지 않고 해당 build를 P0로 중단한다.

| 세션 | locale | seed | 숨은 관찰 렌즈 | 반드시 기록할 기회 | 참가자에게 공개 여부 |
|---|---|---:|---|---|---|
| `w17.s01` | ko | 170101 | 초보 생존형 | 초반 허기, 첫 위험 예고, 회복 성공 또는 미시도, 첫 프로젝트 preview | seed·렌즈 비공개 |
| `w17.s02` | ko | 170202 | 탐험형 | 지역 다양성, 확장 해금, 조기 탈출 시도 여부, 위험 회복 실패 또는 우회 | 비공개 |
| `w17.s03` | ko | 170303 | 기술형 | radio/beacon 노출, 설비 milestone, Day 50 잔류 또는 기술 경로 탈출 | 비공개 |
| `w17.s04` | en | 170101 | 초보 생존형 | `s01`과 같은 seed에서 의미·행동 차이, 회복 이해 | 비공개 |
| `w17.s05` | en | 170202 | 탐험형 | `s02`와 같은 seed에서 지역·조기 탈출 판단 차이 | 비공개 |
| `w17.s06` | en | 170303 | 기술형 | `s03`과 같은 seed에서 요구조건·엔딩 이해 차이 | 비공개 |

성향은 결과 분류값이지 플레이 지시가 아니다. 실제 행동이 렌즈와 다르면 관찰값을 고치지 않고 `observedProfile`을 별도로 기록한다. 조기 탈출, Day 50, 회복 성공·실패가 자연스럽게 발생하지 않으면 `NOT_OBSERVED`로 남긴다.

## 3. 금지된 디버그 보조

- 자원·부품 `grant`, 좌표·지역 `warp`, 날짜 skip, invulnerability, hunger/hazard off
- seed·숨은 loot·ending resolver 공개, debug map, console, save 편집
- 좋은 결과가 나올 때까지 restart·reroll 또는 session 중 seed 교체
- 진행자 해법 힌트, 추천 지역·제작·탈출법, 외부 공략
- 실패 transaction 재시도 전 save 되돌리기
- 사용자 자유 메모를 telemetry에 복사하거나 계정·이름·연락처를 저장하기

조작키나 기술 오류 해결은 알려 줄 수 있으나 `facilitator_technical_assist`로 수동 기록하고 active time을 잠시 중지한다. 이 도움으로 경제·위험·진행 상태를 바꾸면 세션은 invalid다.

## 4. 세션 종료와 유효성

정상 종료 코드는 다음 중 하나다.

| 종료 코드 | 조건 | 유효성 |
|---|---|---|
| `escape_complete` | Day 50 이전 또는 당일 탈출 ending 확정 | terminal valid |
| `day50_settlement` | Day 50 정산 뒤 잔류 ending 확정, Day 51 없음 | terminal valid |
| `survival_terminal` | 자연 생존 실패가 terminal state로 확정 | terminal valid, 실패 원인 기록 |
| `timebox_incomplete` | 누적 active play 180분에 terminal 미도달 | 비terminal valid observation, 완주 증거 아님 |
| `participant_stop` | 참가자가 언제든 중단 요청 | 즉시 종료, 이유 수집 강요 금지 |
| `p0_protocol_stop` | P0 기술·데이터·softlock 조건 발생 | build invalid, 남은 cohort 중단 |

run은 정산 지점에서 같은 save로 재개할 수 있다. 총 active time만 누적한다. `timebox_incomplete`, 중도 중단 또는 P0 세션을 실제 50일 사용자 완주로 세지 않는다.

## 5. 날짜 밴드별 관찰표

| 밴드 | 자동 기록 | 사람이 관찰할 판단 | 막힘 분류 |
|---|---|---|---|
| Day 1–10 | 첫 지도 open day/time, 선택 지역, 귀환 수, same-region streak, hunger·hazard telegraph, project preview | 자원 전망과 위험을 보고 지역을 골랐는가, 첫 회복 수단을 알아차렸는가 | `discoverability / comprehension / economy / survival` |
| Day 11–20 | 첫 확장 preview·unlock day, primary/alternative rule, travel band, unique regions, pity counter | 잠금 이유와 대체 경로를 구분하는가, 이동 시간이 선택을 바꾸는가 | `region_access / travel / equipment / random_discovery` |
| Day 21–35 | 6지역 노출 수, project preview·commit·pivot, 반복 수색률, hazard 대비·완화·회복, softlock audit | 같은 수색이 습관인지 강제인지, 생존·캠프·탈출 투자의 긴장이 읽히는가 | `resource_forecast / hazard_pressure / recovery / escape_preparation` |
| Day 36–49 | 활성 project milestone, missing requirement, pity 3/5, 완료·수리·pivot, 남은 날 경고 | 막힘과 대체 출처를 설명할 수 있는가, 압박이 공정한가 | `finish_pressure / softlock / recovery / comprehension` |
| Day 50 | terminal 우선순위, ending ID, identity snapshot, panel sequence, Day 51 방지 | 엔딩과 우세 생활 방식이 자신의 플레이를 설명하는가 | `terminal / identity / ending_fit / localization` |

### 핵심 파생 지표

- `uniqueRegionCount`: terminal 또는 중단 전 성공 귀환한 서로 다른 region 수
- `sameRegionStreakMax`: 중간에 다른 region 귀환 없이 같은 region을 연속 확정한 최대 횟수
- `repeatedSearchRate`: 직전 수색과 region·주요 목표가 같은 수색 / 비교 가능한 전체 수색
- `meaningfulChoiceRate`: 지도에서 전망·위험·project 요구 중 하나를 이유로 선택을 바꾼 관찰 횟수 / 지도 확정 횟수
- `hazardPreparationRate`: telegraphed hazard 중 발생 전에 대비·우회한 수 / 대응 가능 hazard 수
- `hazardRecoveryRate`: 실제 결과 뒤 recovery 완료 수 / recovery가 필요했던 hazard 수
- `escapeExposureCount`: 요구조건을 실제로 열어 본 서로 다른 `escape.*` method 수, 최대 5
- `softlockMinimumRoutes`: seed 생성·확장 해금·Day 35·Day 49 audit에서 완성 가능한 method 최솟값
- `identityFlipByFinalAction`: 마지막 의미 행동 하나로 established identity가 바뀌었는지 여부, 허용값 false
- `endingFitRating`: 결말이 자신의 플레이와 맞는지 사후 1–5 응답

반복 수색은 횟수만으로 실패가 아니다. 참가자가 안전·부품·project 목표 때문에 의도적으로 반복했다고 설명하면 `chosen_repeat`, 다른 합리적 선택이나 정보를 찾지 못해 반복하면 `forced_repeat`로 나눈다.

## 6. 5개 탈출 경로와 softlock 기록

각 session에서 `escape.raft/smoke/radio/flare/beacon`에 대해 `not_seen / previewed / requirement_understood / committed / completed / abandoned` 중 최종 상태를 하나 기록한다. data-only 구현 상태인 method는 사용자 세션에 넣지 않으며, 다섯 method가 실제 플레이 가능해진 동일 build에서만 이 프로토콜을 실행한다.

softlock audit는 다음 네 시점에 자동 수행한다.

1. seed 생성 직후
2. 확장 지역 해금 직후
3. Day 35 정산
4. Day 49 정산

각 시점에 primary+alternative+pity chain으로 완성 가능한 method가 최소 3개여야 한다. 2개 이하, audit 누락, 같은 seed의 audit 결과 불일치는 P0다. 플레이어가 세 경로 중 하나도 선택하지 않은 것은 softlock이 아니라 선택 결과다.

## 7. 최소 telemetry와 수동 정성 메모

허용된 구조화 필드만 로컬 로그에 기록한다.

| 필드 | 규칙 |
|---|---|
| `sessionSlotId` | `w17.s01`~`w17.s06`, 사람 식별자가 아님 |
| `localeCode` | `ko` 또는 `en` |
| `seed` | 지정 정수 seed |
| `day` | 1~50 |
| `pacingBandId` | Wave 16 band ID |
| `eventId` | Wave 16 telemetry event ID 재사용 |
| `stableEntityId` | region/hazard/escape/stat/ending의 안정 ID |
| `resultCode` | 정해진 enum, 자유문 없음 |
| `elapsedSegmentMs` | 직전 밴드 또는 상호작용 구간의 active time |

이름, 계정, IP, 장치 ID, 원시 입력열, 음성, 채팅, 좌표 궤적, 자유 입력은 저장하지 않는다. `sessionSlotId`와 개인 모집 기록을 연결하는 표도 만들지 않는다.

정성 메모와 1–5 응답은 별도 수동 양식을 쓴다. 세션 전에 선택 동의를 받고, 동의하지 않아도 플레이할 수 있다. 수동 양식에는 session slot만 적고 이름·연락처는 적지 않는다. 인용 허용은 별도 체크이며 거부 시 원문 인용 없이 범주값만 남긴다.

## 8. 사후 질문

1. 가장 자주 간 지역과 그 이유는 무엇이었나요?
2. 반복 수색이 스스로 택한 전략이었나요, 다른 방법을 찾지 못해서였나요?
3. 가장 위험했던 사건을 언제 알았고 어떻게 대비·회복할 수 있다고 생각했나요?
4. 알고 있던 탈출 방법과 포기하거나 선택한 이유를 말해 주세요.
5. 마지막 엔딩과 김씨의 생활 방식이 실제 플레이에 얼마나 맞았나요? `1 전혀 아님–5 매우 잘 맞음`

진행자는 정답을 보충하지 않고 이해한 그대로 기록한다. 영어 질문은 의미와 척도를 동일하게 번역하고 한국어 답안을 기준으로 유도하지 않는다.

## 9. 중단·롤백 기준

### 즉시 P0 중단

- 어느 audit에서든 `softlockMinimumRoutes < 3`
- Day 51 진입, 잘못된 terminal 우선순위, 동일 snapshot의 ending ID 불일치
- 도난·파손·project 차감 중복, 핵심 부품 영구 삭제, save 손상
- crash 또는 진행 불능 UI로 자연 run을 이어갈 수 없음
- 허용 목록 밖 telemetry 또는 개인정보·자유 입력 로그 발견
- 지정 seed·build·locale이 session 도중 바뀜

P0면 현재 세션과 같은 build의 남은 세션을 중단한다. 수정 build는 기존 결과와 합치지 않고 새 6세션 cohort로 시작한다.

### cohort 일시 중단

- 같은 비기술적 진행 막힘으로 연속 2세션이 다음 밴드에 진입하지 못함
- locale 한쪽에서만 같은 요구조건 오해가 2회 발생
- 참가자 안전·피로 문제가 반복됨

발견성·문구 수정도 build 변경이다. 남은 세션에만 적용하지 않는다.

### 롤백

한 축 후보 변경 뒤 P0가 발생하거나, 목표 지표가 기준 build보다 20% 이상 악화되거나, 기존에 존재하던 조기 탈출·Day 50 terminal·최소 3경로 중 하나가 사라지면 즉시 이전 build로 롤백한다. 다른 축을 동시에 보정해 결과를 상쇄하지 않는다.

## 10. 한 빌드·한 튜닝 축 의사결정표

| 축 | 후보로 올리는 관찰 임계 | 먼저 배제할 원인 | 허용 변경 | 유지·보류 조건 |
|---|---|---|---|---|
| `REGION_ACCESS` | 4/6 이상이 Day 20까지 확장 단서를 봤지만 잠금 이유를 찾지 못함 | locale·UI·버그 | 해금 요구 또는 힌트 시점 하나 | 의도적 안전 선택이면 유지 |
| `TRAVEL_TIME` | 4/6 이상에서 이동이 active time 35% 초과이고 반복 피로 4–5 | pathing·입력 지연 | travel band 또는 친숙화 횟수 하나 | 장거리 선택의 보상·긴장이 설명되면 유지 |
| `RESOURCE_FORECAST` | Day 35 도달 session의 median forced repeated-search rate > 50% | 전망 문구 오해·부품 softlock | 범주형 ±1 빈도 하나 | chosen repeat가 다수면 유지 |
| `HAZARD_PRESSURE` | 3/6 이상이 예고를 읽고도 같은 날 중첩으로 회복 불가능 | telegraph 누락·원자성 버그 | budget 또는 active/major 상한 하나 | 대비하지 않은 위험이면 보류 |
| `RECOVERY_WINDOW` | 3/6 이상이 mitigation 성공 뒤 같은 family major 전에 회복 불가 | 회복 UI 발견성·재료 오해 | 평온일 또는 recovery 예약 하나 | 회복 선택을 포기했다면 유지 |
| `ESCAPE_PREPARATION` | 4/6 이상이 3개 method를 이해했지만 Day 35까지 어느 project도 중간 milestone에 못 감 | 지역 해금·전망·softlock | 준비 단계 또는 SAMPLE 준비 기간 하나 | 여러 project 분산 투자면 보류 |
| `IDENTITY_THRESHOLD` | ending fit 1–2가 2/6 이상이고 로그상 마지막 한 행동이 identity를 뒤집음 | ending 문구·resolver 버그 | candidate/cap/lead 중 하나 | mismatch가 설명 문구 문제면 수치 보류 |

여러 축이 임계값을 넘으면 P0·P1 심각도, 영향을 받은 session 수, 가장 이른 발생 day 순으로 하나를 고른다. 선택하지 않은 축은 다음 build까지 보류한다.

## 11. SAMPLE_ONLY 승격 증거

증거 단계는 다음과 같다.

| 단계 | 최소 증거 | 허용 결론 |
|---|---|---|
| `E0_AUTOMATED` | schema·seed·softlock·resolver 자동 테스트 | 구현 계약만 확인, 밸런스는 SAMPLE 유지 |
| `E1_DIAGNOSTIC_6` | 동일 build의 유효 첫 사용자 6세션, ko 3/en 3 | 한 튜닝 축 선택 또는 전부 유지. 정식 승격 금지 |
| `E2_CANDIDATE_6` | 단일 축 후보 build의 새 사용자 6세션, 같은 seed/locale 배치 | 해당 축을 release candidate로 표시 가능 |
| `E3_CONFIRMATION_6` | 변경 없는 후보 build의 독립 새 사용자 6세션 | 아래 게이트 충족 시 그 축만 `PLAYTEST_LOCKED_V1` 승격 가능 |

E3 승격 게이트:

- 총 12개 후보·확인 session이 모두 동일 후보 값으로 실행되고 P0 0건
- 각 cohort ko 3/en 3, 구조화 로그 완전성 95% 이상, 개인정보 필드 0건
- 최소 2개 자연 조기 탈출, 최소 2개 자연 Day 50 terminal, 생존 terminal 또는 timebox 결과를 숨기지 않음
- 모든 softlock audit에서 최소 3경로, duplicate transaction 0건
- Day 35 도달 session의 median forced repeated-search rate 40% 이하
- 대응 가능 hazard의 median preparation rate 50% 이상, mitigation 성공 hazard의 recovery rate 70% 이상
- 5개 method가 catalog·UI에 노출되고 terminal session의 75% 이상이 3개 이상의 차이를 설명
- established identity의 final-action flip 0건, terminal session의 ending fit median 4 이상
- ko/en 쌍 seed에서 terminal·요구조건 의미 불일치 0건

게이트를 통과해도 승격은 관찰한 단일 축에만 적용한다. 비용·드롭·생존 소모·다른 위험·다른 identity 임계값은 계속 SAMPLE_ONLY다. 새 기능 추가나 50일 구조 변경은 이 프로토콜의 튜닝 권한이 아니다.

## 12. 세션 수동 기록지

### 실행 정보

| session slot | locale | seed | build | 시작/종료 active time | terminal code | valid/invalid |
|---|---|---:|---|---:|---|---|
|  |  |  |  |  |  |  |

### 밴드 요약

| 밴드 | 도달 day/time | unique region | same streak / forced repeat | hazard 대비·회복 | escape 노출·진척 | 막힘 코드 | 짧은 동의형 메모 |
|---|---|---:|---|---|---|---|---|
| 1–10 |  |  |  |  |  |  |  |
| 11–20 |  |  |  |  |  |  |  |
| 21–35 |  |  |  |  |  |  |  |
| 36–49 |  |  |  |  |  |  |  |
| 50 |  |  |  |  |  |  |  |

### 결말

| ending ID | identity before final action | identity at terminal | final-action flip | ending fit 1–5 | 이해한 탈출/잔류 이유 |
|---|---|---|---|---:|---|
|  |  |  |  |  |  |

## 13. 현재 미검증

- 6개 첫 사용자 자연 session 전체: `UNRUN`
- 실제 자연 Day 50 사용자 완주: `UNVERIFIED`
- 물리 게임패드 ko/en 실기: `UNVERIFIED`, 자동 입력 증거와 합치지 않음
- Steamworks·App ID·업적·배포: `UNVERIFIED / OUT_OF_SCOPE`
- Wave 16 SAMPLE_ONLY 수치의 정식 밸런스 승격: `NOT_ELIGIBLE_BEFORE_E3`

현재 프로토콜 실행을 막는 설계 질문은 없다. 구현 build와 자동 게이트가 준비된 뒤 세션을 모집·실행하는 일은 별도 승인 범위다.

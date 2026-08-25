# Wave 19 리소스 통합 첫 사용자 플레이테스트 계약

상태: `READY_TO_RUN / HUMAN_RESULT_UNRUN`

계약 ID: `playtest.wave19.resource-integrated-first-user.v1`

기준선: `origin/master@2b985e1cbd08c82661bf71a4f82cbf6d63b4a97f`

자동 증거: `Artifacts/Verification/resource-integrated-playtest/README.md` (`PRODUCT 23/23 PASS`, `qps-long 10/10 PASS`)

대상 빌드: `KimSurvivalIsland.exe`, SHA-256 `93c19f9e7c681845d34407807d33b6438e781dd34c4d8895ebdf2c6fb083711d`

## 1. 목적과 판정 경계

처음 보는 사용자가 디버그 보조 없이 30~45분 동안 Day 1~10 자연 루프를 플레이했을 때 다음 질문에 답한다.

> 직접 캠프를 돌아다니며 준비하고, 지역의 보상과 위험을 비교해 수색하고, 귀환해 성장하며, 장기 생존 또는 조기 탈출을 스스로 계획하고 싶어지는가?

- 이번 세션은 **발견성·이해·선택 동기**를 판단한다. 최종 50일 밸런스를 확정하지 않는다.
- Day 1~10은 관찰 범위이지 Day 10 도달 요구가 아니다. 자연 결말 또는 중단 조건이 먼저 오면 끝낸다.
- 한 번의 owner 세션은 문제 가설을 만들 수 있지만 비용·드롭·위험 빈도·SAMPLE_ONLY 값을 바꾸거나 기능을 영구 삭제하는 근거가 아니다.
- 자동 GREEN은 빌드가 실행되고 정해진 상태를 낸다는 증거다. 인간의 재미·이해·피로도 PASS를 대신하지 않는다.

## 2. 현재 빌드에서 증명할 수 있는 범위

| 영역 | 현재 노출 등급 | 이번 세션의 올바른 판정 |
|---|---|---|
| 베이스캠프 직접 이동·근접·팝업·행동 | PLAYABLE | 실제 행동과 발견 시간을 판정 |
| 모닥불·작업대·빗물받이 제한적 자유 배치/무비용 재배치 | PLAYABLE | 설치 후 직접 다가가 기능을 사용하는지 판정 |
| 구조·탈출 설비의 전용 앵커 | PLAYABLE_CONTRACT | 자유 배치 예외를 이해했는지, 실제 노출 때만 판정 |
| 수색 지도 6개 지역 profile | CATALOG; 시작 3개 우선 노출 | 자원·위험·날씨·장비·특별 발견 중 선택 근거를 판정 |
| 수영·얕은 바다 수색 | PLAYABLE | 선택했을 때 진입→활동→복귀와 추가 소모 이해를 판정 |
| 가방 4칸→6칸 1회 업그레이드, 칸당 2 | PLAYABLE | 가득 참·교체·업그레이드와 탈출 투자 경쟁을 판정 |
| Day 50 기본 종료와 언제든 조기 탈출 | PLAYABLE_CONTRACT | UI에 자연 노출됐을 때 의미를 판정; 45분 내 Day 50 완주를 요구하지 않음 |
| `escape.smoke`, `escape.radio` | PLAYABLE | 서로 다른 현장 상호작용·진행·완료를 판정 |
| `escape.raft`, `escape.flare`, `escape.beacon` | DATA_ONLY | 5종 catalog의 존재·차이만 판정; 플레이 가능 실패로 세지 않음 |
| 위험 7종 catalog | CATALOG | 노출된 예고·발생·완화·회복만 판정; 미발생은 `NOT_OBSERVED` |
| 19 ending ID·행동 누적 판정·3패널 결과 | CATALOG + SAMPLE PLAYABLE | 결말 도달 시 납득도를 판정; 45분 내 19개 노출을 요구하지 않음 |
| 게임 내 앨범 | IMPLEMENTATION_NOT_PROVEN_BY_THIS_BASELINE | 메뉴가 보일 때만 사용성 판정; 없으면 `NOT_EXPOSED_CURRENT_BUILD` |
| Steam 업적 | FUTURE_MAPPING_ONLY / NOT_READY | 현재 기능 PASS 금지; 사후 개념 동기만 별도 평가 |
| KO/EN | SUPPORTED | 선택한 세션 언어의 이해도와 상태 의미 동일성 판정 |
| qps-long | QA_ONLY, 10/10 자동 PASS | 사용자 언어로 선택하지 않음; 텍스트 팽창 회귀 전용 |
| 물리 게임패드 | UNVERIFIED | 이번 키보드·마우스 결과와 합치지 않음 |

## 3. 정본 catalog 요약

### 3.1 지역 선택 카드

지도 카드가 아래 다섯 축을 한 화면에서 비교하게 해야 한다. 사용자가 실제로 말하거나 가리킨 축만 `used_dimensions`에 기록한다.

| region ID | 해금 | 상대 자원 | 위험 | 날씨 | 요구 장비 | 특별 발견 예 |
|---|---|---|---|---|---|---|
| `region.coast.beach` | 시작 | 나무·표류물 풍부, 식량·천 보통 | 허기·부상·재해 | 맑음·비·높은 파도 | 없음 | 조수 보관함·돛천·표류 조명탄 |
| `region.forest.grove` | 시작 | 나무·섬유 풍부, 식량·약 보통 | 허기·질병·야생동물 | 맑음·더위·비 | 돌도끼 | 약초 숲·수지 나무·옛 덫길 |
| `region.sea.shallows` | 시작 | 표류물·식량 풍부, 금속·전선 보통 | 허기·부상·재해 | 맑음·해류·높은 파도 | 수영 준비 | 수중 상자·난파선 지도·구리 묶음 |
| `region.ridge.highland` | 확장 | 돌·나무 풍부, 연료·약 보통 | 부상·재해·야생동물 | 바람·번개·맑음 | 밧줄·방수 장비 | 신호 시야·기상 기록·낡은 렌즈 |
| `region.cove.wreck` | 확장 | 금속·표류물 풍부, 전자·천·약품 보통 | 부상·질병·재해 | 해류·높은 파도·비 | 수영 준비·밧줄 | 무전기 외함·조명탄 보관함·엔진 고철 |
| `region.ruins.relay` | 확장 | 전자·전선 풍부, 금속·연료 보통 | 부상·캠프 파손·재해 | 번개·바람·비 | 밧줄·절연 장비 | 송수신 코어·발전기 코일·중계소 기록 |

합격은 특정 지역 선택이 아니라 선택 전 **서로 다른 두 지역을 보고 두 축 이상을 근거로 설명**하는 것이다. 잠긴 확장 지역은 이유와 대체 경로가 함께 보이면 정보 노출로 센다.

### 3.2 조기 탈출과 장기 동기

| method ID | 판타지·차별 축 | 현재 인간 판정 |
|---|---|---|
| `escape.raft` | 해안·조류·항해 보급·날씨 창 | DATA_ONLY catalog 이해 |
| `escape.smoke` | 고지대·연료 유지·바람/비·고정 연기 설비 | 실제 현장 진행 |
| `escape.radio` | 난파/폐시설 전자부품·전력·주파수 송신 | 실제 현장 진행 |
| `escape.flare` | 단발 자원·짧은 목격 창·발사 실패 위험 | DATA_ONLY catalog 이해 |
| `escape.beacon` | 산등성이 폐시설·구조/발전기 장기 복구 | DATA_ONLY catalog 이해 |

19개 ending ID는 탈출법, Day 50 잔류, 수영·농사·사냥/덫·기계·건설·수색·위험 대응 누적 행동과 특별 사건으로 갈린다. 한 번의 마지막 행동이 기존 우세 생활 방식을 뒤집지 않는다는 계약을 유지한다. 게임 내 앨범과 `achievement.*` 매핑은 반복 플레이 동기 가설이지만 실제 Steamworks 기능은 아니다.

### 3.3 위험 묶음

다음 일곱 ID의 `예고→발생→완화 시도→회복` 중 실제로 발생한 단계만 기록한다: `hazard.hunger`, `hazard.disease`, `hazard.injury`, `hazard.disaster`, `hazard.wildlife`, `hazard.food-theft`, `hazard.camp-damage`. 선택하지 않은 지역이나 발생하지 않은 RNG 결과는 실패가 아니다.

## 4. 세션 설정

### 4.1 사전 점검

- Windows 빌드 SHA를 위 값과 대조하고 1280×800 창 모드로 실행한다.
- fresh save, 기본 seed, KO 기본 또는 별도 EN 세션을 사용한다.
- 금지: grant, warp, day skip, save 편집, 무적, 자원/위험 fixture, 정답이 되는 힌트.
- 허용: 실행·입력 오류 확인, 시간 고지, 참가자의 중단 요청 수용.
- 로컬 JSONL은 개인정보·자유 문장을 기록하지 않는다. 원본은 참가자 PC에 두고 저장소에는 집계표만 옮긴다.
- 시작 전에 물리 게임패드를 연결하지 않는다. 게임패드 실기는 별도 `UNVERIFIED` 양식으로 수행한다.

진행자 시작 문구:

> 이 빌드를 처음 보는 사람처럼 30분 이상, 최대 45분 동안 화면에서 이해한 대로 플레이해 주세요. 정답이나 구체적인 해결 힌트는 드리지 않습니다. 멈추고 싶거나 화면이 진행 불가능하다고 느끼면 바로 말해 주세요.

### 4.2 관찰 시간대

시간대는 진행자용이며 참가자에게 목표 목록으로 읽어주지 않는다.

| 플레이 시간 | 관찰 초점 | 유도 금지 |
|---:|---|---|
| 0~5분 | 첫 이동, 설비 근접, compact 상황 안내, 팝업 열기/닫기, 직접 현장 복귀 | 작업대 위치나 입력을 알려주지 않음 |
| 5~12분 | 수색 지도 발견, 시작 지역 비교, 장비/날씨/위험/특별 발견 해석, 첫 출발 | “얕은 바다로 가라” 같은 지역 지시 금지 |
| 12~25분 | 첫 수색 결과, 가방 4칸/중첩 2, 수영 선택 시 소모, 귀환·보관 | 버릴 자원이나 최적 경로를 제시하지 않음 |
| 25~35분 | 제작·연구, 일반 설비 설치/재배치, 전용 앵커 예외, 가방 6칸 또는 탈출 투자 | 업그레이드 구매·시설 위치를 강요하지 않음 |
| 35~45분 | 두 번째 수색 루프, 위험 대비/회복, 연기·무전 진행 또는 자연 결말, 5종/Day 50/엔딩 동기 노출 | 데이터 전용 탈출법을 플레이 가능하다고 설명하지 않음 |

30분 전에 자연 terminal에 도달하면 종료할 수 있다. 30분이 지나면 참가자가 원할 때 끝낼 수 있고, 45분에는 진행 상황과 무관하게 종료한다.

## 5. 수기 관찰표

각 항목의 상태는 `PASS`, `FAIL`, `NOT_OBSERVED`, `NOT_EXPOSED_CURRENT_BUILD`, `EVIDENCE_INVALID` 중 하나다.

| 키 | 기록 값 | 합격 기준 |
|---|---|---|
| `first.move_s` | 시작 후 초 | 60초 이내 |
| `first.camp_proximity_s` | 초, facility ID | 120초 이내 직접 접근 |
| `first.camp_popup_s` | 초, 열기/닫기 결과 | 180초 이내, 닫으면 같은 현장으로 복귀 |
| `map.first_open_s` | 초 | 480초 이내 |
| `map.comparison` | 비교 region IDs, `used_dimensions[]` | 서로 다른 2지역·2축 이상 |
| `expedition.first_start_s` | 초, region ID | 720초 이내 |
| `expedition.first_return_s` | 초, result ID | 1,200초 이내 완전한 출발→결과→귀환 |
| `expedition.completed_count` | 횟수 | 40분까지 2회 이상 또는 그 전에 유효 terminal |
| `expedition.unique_region_count` | 수 | 2 이상이면 다양성 PASS; 1이면 선택 이유 확인 |
| `camp.growth_action` | craft/research/build/bag/project ID, 시간 | 30분 이내 1회 이상 |
| `placement.general` | preview/confirm/relocate/use | 일반 설비 1개 유효 설치 후 직접 근접 사용 |
| `placement.fixed_anchor` | preview/confirm/reject 이유 | 노출 시 자유 배치 예외와 정확한 실패 이유 이해 |
| `bag.choice` | full/replace/upgrade/decline, 이유 | 노출 시 운반 효율과 성장 투자 경쟁을 설명 |
| `swim.roundtrip` | entered/exited, 자원·소모 인식 | 선택 시 진입과 복귀가 모두 있고 추가 비용을 설명 |
| `hazard.lifecycle` | hazard ID와 도달 단계 | 예고가 노출되면 원인·대비 1개를 설명, 발생 시 대응 시도 |
| `escape.exposure` | 인지 method IDs | UI 노출 시 Day 50 기본과 조기 탈출 가능, 총 5종의 차이를 설명 |
| `escape.live_action` | smoke/radio interaction IDs | 자연 노출 시 각기 다른 현장 행동으로 이해 |
| `ending.result` | ending ID, 근거 회상 | terminal 발생 시 행동/사건과 결과 연결을 1개 이상 설명 |
| `ending.album_surface` | visible/opened/not exposed | 현재 화면에 있을 때만 사용성 판정 |
| `locale.meaning` | locale, 오해 key | KO/EN에서 상태·비용·위험·결말 의미가 동일 |
| `session.end` | elapsed, day, reason | terminal / 45분 / participant stop / blocker |

### 자연 루프 전체 합격선

한 세션은 다음을 모두 만족할 때 `SESSION_CORE_PASS`다.

1. `first.move_s`, `first.camp_popup_s`, `map.first_open_s`, `expedition.first_start_s`, `expedition.first_return_s`, `camp.growth_action` 기준을 만족한다.
2. 40분까지 수색→귀환 루프 2회 또는 유효 조기 terminal 1회를 완료한다.
3. 일반 설비·고정 앵커가 둘 다 노출됐다면 규칙을 서로 바꾸어 이해하지 않는다.
4. 지역 선택의 근거로 최소 두 정보 축을 사용한다.
5. P0가 없고 정답 힌트·debug 보조를 쓰지 않았다.

선택적 기능은 노출되지 않았다는 이유만으로 전체 세션을 FAIL 처리하지 않는다. 대신 `NOT_OBSERVED`와 미노출 이유를 남긴다.

## 6. 막힘·심각도 판정

| 등급 | 판정 | 예 |
|---|---|---|
| P0 | 진행·데이터·원자성을 즉시 위협. 세션 중단 및 빌드 롤백 후보 | crash, 입력 완전 상실, 빠져나올 수 없는 60초 이상 softlock, Day 51, 중복 자원 차감, 실패/취소가 완료 단계를 삭제, 같은 terminal 중복 해금 |
| P1-DISCOVERY | 기능은 있으나 사용자에게 경로나 의미가 보이지 않음 | 8분 내 지도 미발견, 직접 설비보다 전역 메뉴를 찾음, 자유 배치/앵커 반대로 이해, 지역 비교 축을 읽지 못함 |
| P1-ECONOMY | 발견성은 통과했지만 자연 자원·소모 때문에 둘 이상의 루프 뒤에도 모든 성장 경로가 닫힘 | 반복 부족으로 제작·가방·연기/무전 중 어느 것도 전진 불가, 회복 선택 없이 허기 연쇄 실패 |
| P1-LOCALE | KO/EN 의미 차이 또는 qps 회귀가 핵심 행동을 막음 | 비용·위험·완료/부족 상태가 언어마다 반대, 입력 glyph가 행동을 가림 |
| P2 | 진행은 가능하나 가독성·피로·코미디·우선순위가 약함 | 같은 수색 반복 피로, 비차단 문구 혼란, 농담이 상태 피드백을 늦춤 |
| NOT_OBSERVED | 선택·seed·시간 때문에 만나지 못함 | 수영 미선택, 위험 미발생, terminal 미도달 |
| EVIDENCE_INVALID | 세션 결과를 의사결정 표본으로 셀 수 없음 | 다른 SHA, debug/warp 사용, 정답 힌트 오염, 30분 전 비terminal 종료, 로그와 수기 식별자 불일치 |

자원 부족과 발견성은 다음 순서로 분리한다: **행동 위치·입력·비용을 먼저 찾았는가 → 필요한 자원의 출처를 말할 수 있는가 → 두 번의 올바른 수색 뒤에도 선택지가 모두 닫혔는가**. 앞의 두 질문이 아니면 경제 문제가 아니라 발견성 문제로 우선 분류한다.

## 7. 정량 로그 계약

개발 빌드는 `Application.persistentDataPath/PlaytestLogs/kim-survival-playtest-<runId>.jsonl`에 로컬 로그를 남긴다. 참가자명·계정·IP·기기 ID·자유 입력·원시 입력·좌표 궤적은 저장하지 않는다.

| 판단 | 안정 event name |
|---|---|
| 세션·일자 | `session.started`, `day.changed`, `day.survived`, `phase.changed`, `run.completed` |
| 자원·가방 | `resource.changed`, `bag.capacity.upgraded` |
| 현장 설비 | `facility.proximity.entered`, `facility.proximity.exited`, `facility.popup.opened`, `facility.popup.closed`, `facility.action.completed`, `facility.action.rejected` |
| 제작·연구 | `crafting.completed`, `research.completed` |
| 수영·장벽 | `swimming.entered`, `swimming.exited`, `vine_barrier.blocked`, `vine_barrier.cleared` |
| 수색 | `expedition.region.selected`, `expedition.started`, `expedition.result.resolved` |
| 위험 | `hazard.telegraphed`, `hazard.occurred`, `hazard.mitigated`, `hazard.recovered` |
| 탈출·결말 | `escape.project-progressed`, `escape.completed`, `ending.resolved`, `run.completed` |

집계 허용 필드는 `run_id`, `run_seed`, `event_name`, `locale`, `input_device`, `target_kind`, `target_id`, `action`, `outcome`, `resource`, `delta`, `day`, `pacing_band_id`, `region_id`, `profile_id`, `result_id`, `hazard_id`, `project_id`, `escape_id`, `ending_id`, `behavior_score_ids`, `result_code`, `state_before`, `state_after`, 구간 소요 시간이다. 원본 JSONL은 커밋하지 않고 아래 파생값만 세션 시트에 옮긴다.

- 첫 근접·팝업·지도·출발·귀환·성장 행동 시간
- 완전한 수색→귀환 횟수, 고유 지역 수, 같은 지역 연속 선택 최대치
- 가방 유입/폐기/보관/업그레이드, 일반 설치/거절/재배치, 수영 왕복 수
- 위험별 예고/발생/완화/회복 수, 조기 탈출 진행·완료, 최종 day/ending/result code

## 8. 사후 질문

먼저 자유 답변을 받고, 그 뒤 1~5 척도를 묻는다. 기능 이름이나 정답 후보를 먼저 읽어주지 않는다.

1. 방금 가장 먼저 세운 목표는 무엇이었고, 다음 행동을 왜 골랐나요?
2. 수색 지역을 고를 때 어떤 정보들을 비교했나요? 다른 지역 대신 그곳을 고른 이유는 무엇인가요?
3. 캠프의 설비 위치는 플레이에 어떤 영향을 줬나요? 옮길 수 있는 것과 정해진 자리에 놓는 것이 어떻게 달랐나요?
4. 가방과 수영은 무엇을 더 할 수 있게 하거나 포기하게 만들었나요?
5. 어떤 위험이 올 것이라 예상했고, 실제로 무엇을 준비하거나 회복했나요?
6. 이 섬에서 빠져나가는 방법은 몇 가지라고 이해했나요? 지금 실제로 진행할 수 있다고 느낀 방법은 무엇인가요?
7. 이번 행동들이 결말에 어떤 영향을 준다고 느꼈나요? 다른 결말을 보기 위해 다시 한다면 무엇을 바꾸겠나요?

1~5 평정: `loop_clarity`, `region_choice_tension`, `hazard_fairness`, `repetition_fatigue`(5가 가장 피곤함), `comedy_fit`, `replay_intent`.

현재 빌드 기능과 섞지 않는 개념 질문:

> 서로 다른 생활 방식이 19개 이상의 결말·게임 내 앨범 기록으로 남고, 같은 안정 ID가 향후 Steam 업적과 연결된다면 다시 플레이할 동기가 되나요? (1~5, 이유)

이 답은 `CONCEPT_MOTIVATION_ONLY`다. 앨범 구현 PASS나 Steamworks 준비 완료 증거로 사용하지 않는다.

## 9. KO/EN/qps-long 계약

- `ko`는 의미·코미디 톤의 기준 원문이다. KO 세션은 원문 이해와 농담이 상태 판단을 방해하지 않는지 기록한다.
- `en`은 첫 지원 언어다. 별도 첫 사용자 세션에서 같은 상태·비용·위험·결말 의미를 확인한다. KO 세션 도중 EN으로 바꿔 최초 발견 표본을 오염시키지 않는다.
- `qps-long`은 텍스트 팽창·glyph/TMP 분리·잘림·포커스 회귀를 보는 QA 전용이다. 인간 플레이 언어 또는 의미 품질 표본으로 세지 않는다.
- 자유 답변 질문의 안정 키는 `pt.wave19.q01.goal`부터 `pt.wave19.q07.ending`까지다. 번역은 질문 의도를 유지하되 한국어 어순을 직역하지 않는다.
- 공식 영문 게임 제목은 `TBD`이며 시험 문구나 Steam 질문에서 임의로 정하지 않는다.

## 10. 살리기·수정·자르기 결정 규칙

### 단일 owner 세션 직후

- P0는 즉시 `STOP`; 재현 seed/day/event sequence와 함께 구현 롤백 후보로 보낸다.
- 그 외 결과는 `HYPOTHESIS_ONLY`; 밸런스·catalog 수·날짜·기능 삭제를 즉시 적용하지 않는다.
- 미노출 기능은 `NOT_OBSERVED` 또는 `NOT_EXPOSED_CURRENT_BUILD`로 남긴다.

### 최소 반복 표본

| 판정 | 최소 증거 | 다음 행동 |
|---|---|---|
| `KEEP_CORE` | 유효 첫 사용자 3명 중 2명 이상 `SESSION_CORE_PASS`, 같은 P0 없음 | 직접 캠프·지도·수색/귀환·가방·생존·조기 탈출 축 유지 |
| `KEEP_PROVISIONAL` | 선택 기능을 3명 중 2명 이상 자연 사용, 의미/가치 평정 4 이상 | 다음 빌드에도 유지하고 6세션에서 재확인 |
| `FIX_DISCOVERY_FIRST` | reachability는 자동 PASS지만 3명 중 2명 이상 미발견/오해 | 수치가 아니라 안내·정보 구조 한 축만 수정 후 새 3명 재시험 |
| `DEFER_FROM_FIRST_USER_SLICE` | 발견성 1회 수정 뒤에도 새 3명 중 0~1명 사용 또는 가치 중앙값 2 이하 | campaign 정본과 안정 ID는 보존하고 Day 1~10 전면 노출만 뒤로 미룸 |
| `OPEN_ONE_AXIS_TUNE` | 발견성·locale 통과 후 유효 3명 이상이 같은 자원/소모 원인으로 막힘 | 한 빌드에서 비용/드롭/소모/위험 중 한 축만 변경 |
| `PROMOTE_SAMPLE_ONLY` | Wave 17의 KO 3/EN 3 포함 6세션과 자연 장기 증거가 같은 방향 | 별도 승인에서만 정식 밸런스 후보로 승격 |

범위가 급할 때 먼저 자를 후보는 다음과 같다.

1. 앨범/향후 Steam 동기의 Day 1~10 전면 노출: 못 찾거나 가치가 낮아도 ending ID·앨범 정본·achievement mapping은 보존한다.
2. `escape.raft`·`escape.flare`·`escape.beacon`의 즉시 행동 목록 노출: 플레이 가능한 것으로 오해한 사용자가 3명 중 2명 이상이면 catalog는 보존하고 `data-only/future` 상태를 분명히 하거나 첫 화면에서 뒤로 미룬다.
3. 방 모듈 증축: 첫 두 수색 루프 전에 사용자를 반복해서 이탈시키면 첫 사용자 전면 노출을 보류하되 제한적 자유 배치와 현장형 캠프는 유지한다.

한 세션만으로 직접 이동, 지역 비교, 수색→귀환, 수영 지역 차이, 4→6 가방 선택, 생존 위험, Day 50/조기 탈출 계약을 삭제하지 않는다.

## 11. 세션 종료 산출물

- 익명 세션 ID, 빌드 SHA, locale, input device, seed, 시작/종료 시각, 최종 day·terminal
- 수기 관찰표와 P0/P1/P2/NOT_OBSERVED 분류
- JSONL 파생 집계값; 원본 로그·자유 메모는 저장소에 커밋하지 않음
- 자유 답변과 1~5 평정은 별도 동의형 수기 양식에 보관
- 최종 한 줄: `STOP`, `GO_KEEP`, `GO_DISCOVERY_FIX`, `GO_MORE_EVIDENCE` 중 하나

현재 상태는 `GO_MORE_EVIDENCE`: 자동 GREEN은 확보됐고 첫 사용자 인간 세션은 아직 실행하지 않았다.

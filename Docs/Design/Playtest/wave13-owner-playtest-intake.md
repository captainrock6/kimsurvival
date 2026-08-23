# Wave 13 사용자 플레이테스트 접수 시트

- 상태: `READY / HUMAN RESULT UNRUN`
- 정본 기준: `origin/master@386a602f110ebfe2c404685f98f9cacf1b42c1d2`
- 적용 빌드 증거: `Artifacts/ParallelQA/20260823T125000Z_13ecded_release`
- 목적: 사용자가 5일 버전을 한 번 플레이하며 관찰 사실과 사후 해석을 짧게 남긴다. 이 1회 결과만으로 비용·획득량·소모량·날짜를 바꾸지 않는다.

## 1. 진행 원칙

- 권장 상한은 20분이다. 결과가 먼저 나면 즉시 종료한다.
- 새 게임에서 시작하며 `grant`, 좌표 이동, 디버그 조작, 해결 힌트를 쓰지 않는다.
- 관찰 중에는 정답을 알려주거나 “이 기능은 이해됐나요?”처럼 답을 유도하지 않는다.
- 시각은 플레이 시작을 `00:00`으로 한 `분:초`를 쓴다. 일어나지 않은 사건은 `미도달`로 쓴다.
- 참가자 식별 정보는 적지 않는다. 임의 세션 ID만 사용한다.
- 한국어가 의미 기준 원문이다. 영어 플레이는 같은 안정 키와 응답 형식을 사용한다. `qps-long`은 사람 평가 언어가 아니라 QA 전용이다.
- 첫 플레이 의미를 지키기 위해 참가자에게는 이 체크리스트와 사후 질문을 미리 보여주지 않는다. 사용자가 혼자 플레이한다면 플레이 중에는 날짜 진입·결말 시각만 짧게 표시하고 나머지는 종료 직후 기억나는 사실부터 작성한다.

## 2. 세션 머리말

| 키 | 기록 |
|---|---|
| `intake.setup.session_id` | `W13-____` |
| `intake.setup.build_commit` | `13ecded05f48a3a4b6b802502d6016e983f9bac2` / 다르면 실제 값 |
| `intake.setup.executable_sha256` | `93c19f9e7c681845d34407807d33b6438e781dd34c4d8895ebdf2c6fb083711d` / 다르면 실제 값 |
| `intake.setup.locale` | `ko` / `en` |
| `intake.setup.input` | 키보드·마우스 / 물리 게임패드 |
| `intake.setup.resolution` | `1280×800` / 실제 값 |
| `intake.setup.started_at` |  |
| `intake.setup.ended_at` |  |
| `intake.setup.hint_count` | 원칙상 `0`; 불가피했다면 시각과 말한 내용을 메모 |

물리 게임패드 결과는 키보드·마우스 및 자동 입력 결과와 섞지 않고 별도 세션으로 집계한다.

## 3. 사건 시각표

플레이 중에는 시각과 짧은 행동만 기록한다. `이해함/이해 못함`은 행동 근거 없이 미리 판정하지 않는다.

| 키 | 사건 | 시각 | 직전 행동 또는 화면 근거 |
|---|---|---:|---|
| `intake.event.first_move` | 첫 이동 |  |  |
| `intake.event.first_gather` | 첫 채집 성공 |  |  |
| `intake.event.first_facility_prompt` | 첫 설비 근접 안내 노출 |  | 대상: |
| `intake.event.first_facility_popup` | 첫 설비 팝업 열기 |  | 대상: |
| `intake.event.first_craft` | 첫 제작 성공 |  | 제작물: |
| `intake.event.first_research` | 첫 연구 성공 |  | 연구: |
| `intake.event.first_swim_enter` | 첫 물 진입 |  |  |
| `intake.event.first_land_return` | 수영 후 첫 육지 복귀 |  |  |
| `intake.event.vine_seen` | 덩굴/나무 장벽 첫 발견 |  | 첫 시도: |
| `intake.event.vine_opened` | 장벽 제거 성공 |  | 미도달 가능 |
| `intake.event.bag_seen` | 가방 확장 선택 첫 확인 |  |  |
| `intake.event.bag_bought` | 가방 4→6 확정 |  | 미선택 가능 |
| `intake.event.room_preview` | 방 후보 미리보기 |  | 위/옆/지하: |
| `intake.event.room_commit` | 방 하나 확정 |  | 미선택 가능 |
| `intake.event.signal_stage_1` | 구조 신호 1단계 완료 |  |  |
| `intake.event.signal_stage_2` | 구조 신호 2단계 완료 |  |  |
| `intake.event.outcome` | 탈출 또는 실패 |  | 화면에 표시된 이유: |

## 4. 날짜별 장부

수치는 화면에서 확인할 수 있을 때만 적고, 추정치는 앞에 `~`를 붙인다. 자원 칸은 `나무/돌/식량/표류물` 순서다.
전체 반복 레코드의 안정 키는 `intake.day.snapshot`이며 날짜 숫자와 수치는 번역 문자열이 아닌 데이터로 저장한다.

| 날짜 | 진입 시각 | 첫 출발→귀환 | 시작→종료 허기 | 시작→종료 체력 | 종료 자원 W/S/F/D | 가방 | 방 | 신호 | 그날 멈추거나 우회한 이유 |
|---|---:|---|---|---|---|---|---|---|---|
| Day 1 |  |  |  |  |  | 4/6 | 없음/위/옆/지하 | 0/1/2 |  |
| Day 2 |  |  |  |  |  | 4/6 | 없음/위/옆/지하 | 0/1/2 |  |
| Day 3 |  |  |  |  |  | 4/6 | 없음/위/옆/지하 | 0/1/2 |  |
| Day 4 |  |  |  |  |  | 4/6 | 없음/위/옆/지하 | 0/1/2 |  |
| Day 5 |  |  |  |  |  | 4/6 | 없음/위/옆/지하 | 0/1/2 |  |

추가 사실 메모:

- 허기 때문에 바꾼 행동: __________
- 체력 때문에 바꾼 행동: __________
- 가방이 가득 찬 시각과 버리거나 남긴 것: __________
- 수영에서 되돌아온 이유: __________
- 가장 오래 반복한 행동과 횟수: __________

## 5. 현장 관찰

| 키 | 관찰할 사실 | 기록 |
|---|---|---|
| `intake.observe.gathering` | 무엇을 보고 채집 대상으로 판단했고, 첫 실패 뒤 무엇을 했는가 |  |
| `intake.observe.swimming` | 물 진입 전후에 위험을 예상한 근거와 실제 복귀 행동 |  |
| `intake.observe.vine_barrier` | 장벽 앞에서 처음 시도한 행동과 다음 선택 |  |
| `intake.observe.direct_facility` | 월드 설비로 직접 다가가 안내→입력→팝업을 연 행동 |  |
| `intake.observe.craft_research` | 제작과 연구 중 먼저 고른 것, 화면에서 근거로 삼은 정보 |  |
| `intake.observe.bag_tradeoff` | 가방 확장을 보았을 때 함께 포기하거나 미룬 선택 |  |
| `intake.observe.room_tradeoff` | 방 미리보기/확정 때 비교한 후보와 함께 포기하거나 미룬 선택 |  |
| `intake.observe.signal_requirements` | 신호 1·2단계에서 부족 상태 뒤 취한 행동 |  |
| `intake.observe.compact_a_discovery` | compact A를 처음 본 대상·시각, 실제 입력까지 이어졌는지 |  |
| `intake.observe.compact_a_readability` | 안내가 가린 대상, 잘린 글자, 한 번에 읽지 못해 반복한 행동 |  |

compact A는 내레이션 아래 한 줄 안내, 분리된 입력 glyph와 TMP 텍스트를 뜻한다. 이 설명은 진행자용이며 플레이 전에 참가자에게 읽어주지 않는다.

## 6. 사후 질문

질문은 그대로 읽고, 답을 보충하거나 선택지를 제시하지 않는다.

1. `intake.question.goal` — 플레이하는 동안 무엇을 가장 먼저 하려고 했나요? 그렇게 생각한 화면이나 사건은 무엇이었나요?
2. `intake.question.pressure` — 허기와 체력 때문에 원래 하려던 일을 바꾼 순간이 있었나요? 있었다면 무엇을 바꿨나요?
3. `intake.question.tools_world` — 돌도끼, 밧줄, 장벽, 물은 각각 어떤 의미라고 생각했나요?
4. `intake.question.investment` — 가방, 방, 구조 신호 중 무엇에 먼저 자원을 썼거나 쓰지 않았나요? 그 이유는 무엇인가요?
5. `intake.question.outcome` — 마지막 결과가 왜 발생했다고 생각하나요? 다음 한 번에 가장 먼저 다르게 할 일은 무엇인가요?

선택 평점은 플레이 후에만 받는다. `1=매우 낮음`, `5=매우 높음`이다.

| 키 | 항목 | 1–5 | 자유 메모 |
|---|---|---:|---|
| `intake.rating.fun` | 재미 |  |  |
| `intake.rating.confusion` | 혼란 |  |  |
| `intake.rating.blocked` | 막힘 |  |  |
| `intake.rating.repetition` | 반복 피로 |  |  |
| `intake.rating.comedy` | 기억에 남는 코믹 순간 |  | 순간/문구: |
| `intake.rating.prompt_readability` | 상호작용 안내 읽기 쉬움 |  |  |

## 7. 결과 분류

| 키 | 기록 |
|---|---|
| `intake.outcome.type` | `escaped` / `deadline` / `exhausted` / `starved` / `stopped` / `other` |
| `intake.outcome.day` | 1–5 / 해당 없음 |
| `intake.outcome.time` |  |
| `intake.outcome.game_reason` | 게임이 표시한 이유를 그대로 요약 |
| `intake.outcome.player_reason` | 사후 질문에서 사용자가 말한 이유 |
| `intake.outcome.observer_reason` | 행동 기록으로 확인된 직접 원인; 추정이면 `추정` 표시 |

게임 표시, 사용자 해석, 관찰자 추론을 섞지 않는다. Day 3·4에는 기한 실패가 없어야 하며, 구조 신호 2단계 완료는 날짜와 무관하게 즉시 탈출이어야 한다. 이 계약과 다른 결과는 밸런스 피드백이 아니라 구현 P0 후보로 분리한다.

## 8. KO/EN 키 계약

모든 질문과 필드는 `.forge/design/wave13-owner-playtest-intake.json`의 키를 정본으로 사용한다. 키는 언어와 무관하며 저장값은 열거형/수치/자유 문자열로 분리한다.

| 키 | ko 기준 문안 | en 의도 |
|---|---|---|
| `intake.event.first_facility_prompt` | 첫 설비 근접 안내 | First contextual facility prompt |
| `intake.event.room_preview` | 방 후보 미리보기 | First room-module preview |
| `intake.observe.compact_a_readability` | 안내가 가린 대상, 잘린 글자, 반복 행동 | Record occlusion, clipping, or repeated action; do not ask if it was “clear” |
| `intake.question.investment` | 가방·방·신호 중 먼저 투자하거나 미룬 선택과 이유 | Ask what was prioritized or deferred and why, without naming a preferred choice |
| `intake.outcome.game_reason` | 게임이 표시한 결과 이유 | Reason communicated by the game, kept separate from player interpretation |

향후 언어는 동일 키를 유지하고 어순을 문장 단위로 재구성한다. `{day}`, `{time}`, `{resource}`, `{facility}`, `{inputGlyph}`는 번역문 안에서 위치를 바꿀 수 있으며 문자열 접합으로 문장을 만들지 않는다. 공식 영문 제목과 Steam 상점명은 `TBD`다.

## 9. Forge 기준선 감사

증거 기준은 최종 Wave 12 릴리스 폴더이며, 과거 증거 파일 자체는 수정하지 않는다.

### 증거로 완료 처리

| Forge 항목 | 이전→현재 | 직접 근거 |
|---|---|---|
| `task.system.system.phase-flow` | ready→done | `wave12-edit-contracts.json` W12-D01: FinalDay 5, Day 3·4 진행, Day 5 기한, 즉시 구조 |
| `task.feature.feature.phase-cycle` / `task.qa.feature.phase-cycle` | ready·blocked→done | W12-D01와 `wave11-full-regression.txt`의 5일 전이 회귀 |
| `task.feature.feature.escape-outcome` / `task.qa.feature.escape-outcome` | ready·blocked→done | W12-D01의 조기 구조·Day 5 실패 상호 배타 판정 |
| `task.feature.feature.camp-object-interaction` / `task.qa.feature.camp-object-interaction` | ready·blocked→done | `wave12-play-contracts.json` P01–P03: compact A, far/near/popup, KO/EN/qps-long, 키보드/합성 게임패드 의미 |
| `task.qa.feature.camp-building` | review→done | `wave3-visual-gate.txt`: 일반 KO/EN 배치 24/24 PASS; 제한적 자유 배치 회귀 PASS |
| `task.qa.wave9-spatial-camp-contract-gate` | review→done | Wave 11 slot edit/play: 직접 슬롯 3/3, 미리보기·취소·확정·원자 차감, 보행로 3/3 |
| `task.qa.feature.camp-module-expansion` | blocked→done | Wave 11 slot edit/play와 5일 전체 회귀가 W2/D1, run당 1회, 무변경 실패를 증명 |
| `task.wave3.implementation.world-label-readability` | ready→done | 최종 일반 KO/EN placement 24/24 및 exploration/swimming 10/10 PASS |
| `task.design.wave13-owner-playtest-intake` | 신규→done | 이 문서와 기계 판독 계약, 빈 사용자 결과 양식 |

완료는 해당 자동 계약의 통과를 뜻한다. 실제 물리 게임패드 사용성이나 첫 사용자 이해도를 대신 증명하지 않는다.

### 보류 gap

| Gap ID | 유지 상태 | 부족한 근거 / 다음 증거 |
|---|---|---|
| `gap.wave13.owner-five-day-run` | HUMAN UNRUN | 이 시트로 수행할 자연 플레이 1회가 아직 없다. 결과 필드는 비워 둔다. |
| `gap.wave13.external-first-user-cohort` | QA blocked | KO 3명·EN 3명 첫 사용자 6세션이 없다. |
| `gap.wave13.physical-gamepad` | `task.qa.feature.dual-input` ready | 최종 증거는 합성 입력만 통과했고 물리 패드는 `UNVERIFIED`다. |
| `gap.wave13.qps-long-global-layout` | `task.qa.feature.localization` ready | compact A 12/12는 통과했으나 전체 qps-long 시각 게이트는 10개 중 6개 실패다. |
| `gap.wave13.natural-balance-ledger` | `task.wave3.implementation.balance-v0-2` ready | 5일 전이와 비용 동결은 증명됐지만, fresh no-grant/no-warp Day 4–5 자연 경로·완전 장부가 없다. |
| `gap.wave13.bag-natural-route` | `task.qa.feature.inventory-capacity-upgrade` blocked | 4→6 기능 회귀는 통과했지만 구매 후 자연 구조 경로의 독립 증거가 없다. |
| `gap.wave13.integrated-human-gate` | `task.wave3.qa.integrated-three-day` blocked | 이름은 역사적 ID로 유지한다. 자연 경로, 6명 이해도, 물리 패드가 미충족이다. |
| `gap.wave13.art-review` | art review 유지 | PASS는 런타임 계약 증거이며 미채택 art 후보의 사용자 승인이 아니다. |
| `gap.wave13.steam-readiness` | blocked | App ID·상점명·영문 제목·배포 체크가 `NOT_READY/TBD`다. |

## 10. 피드백 이후 변경 규칙

- 이 owner 1회는 문제 후보와 재현 경로를 만드는 자료다. 단독으로 밸런스 수치를 바꾸지 않는다.
- 날짜·비용·획득량·허기·체력 변경은 같은 원인의 재현 가능한 자연 플레이 증거 또는 기존 외부 6세션 판정 규칙을 충족한 뒤 별도 결정으로 연다.
- 계약 위반, 진행 불능, 저장 오염처럼 밸런스와 무관한 결함은 즉시 P0/P1 후보로 분리한다.
- 변경 전에는 `현재값`, `관찰`, `가설`, `한 번에 바꿀 변수`, `예상 효과`를 기록한다.

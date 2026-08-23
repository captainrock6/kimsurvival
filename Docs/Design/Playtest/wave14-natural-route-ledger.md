# Wave 14 5일 자연 플레이 경로·밸런스 장부

- 상태: `DESIGN ARITHMETIC VERIFIED / NATURAL RUNS UNRUN`
- 기준: `origin/master@bd1c580bb53bdd662877efd1600c97500057c3ee`
- 기계 정본: `.forge/design/wave14-natural-route-ledger.json`
- 로그 계약: `Docs/QA/wave13-local-playtest-log.md`
- 목적: 같은 Windows Development Build에서 fresh run, `grant/warp` 없음으로 네 경로를 실제 실행하고, JSONL 자동값과 사람 판단을 섞지 않고 판정한다.

이 문서는 실행 결과가 아니다. 아래 자원 산술과 실제 좌표·가방 적재 가능성만 정적으로 검증했으며, 사용자 세션·물리 게임패드 결과는 `UNRUN/UNVERIFIED`다. 밸런스 v0.2, Day 5 기한, 비용·노드·획득량·E/H/L을 바꾸지 않는다.

## 1. 실행 전 동결 기준

`W/S/F/D`는 나무/돌/식량/표류물, `E/H/L`은 체력/허기/일광이다.

### 1.1 시작·가방·생존

| 항목 | 동결값 |
|---|---:|
| 새 게임 | Day 1, `W2/S1/F0/D0`, `E100/H70/L100`, signal 0 |
| 기본 가방 | 4칸, 칸당 중첩 2, 최대 8개 |
| 가방 확장 | 작업대에서 1회 `W2/D1`, 4→6칸, 중첩 2 유지 |
| 하루 허기 | `H-35` |
| 식량 1개 | `H+35`, 최대 100 |
| H0 페널티 | 회복 전에 `E-35` |
| 기본/모닥불/빗물받이 회복 | `E+20` / 총 `E+38` / 추가 `E+10` |
| 결말 | E0 즉시 `exhausted`; signal 2 즉시 `rescued`; Day 5 종료 미완성만 `deadline` |

### 1.2 이동·채집

| 행동 | E | L |
|---|---:|---:|
| 육상 이동 | `-0.18/s` | `-0.75/s` |
| 수영 이동 | `-0.65/s` | `-1.15/s` |
| 수영 정지 | `-0.22/s` | `-1.15/s` |
| 육상 채집 | `-6/회` | 0 |
| 수상 수색 | `-9/회` | 0 |
| 일몰 강제 귀환 | `E-22`, 최저 1 | 가방 이관 뒤 적용 |

### 1.3 실제 노드 카탈로그

아래 ID는 이 장부의 좌표 별칭이며 새 런타임 ID를 만들지 않는다. 런타임 로그는 개별 node ID를 기록하지 않고 `resource.changed`와 수영 상태를 기록한다.

| 장부 별칭 | x | 접근 | 자원 | 기본 | 돌도끼 |
|---|---:|---|---|---:|---:|
| `node.water.salvage` | -8.2 | 수영 | D | 2 | 2 |
| `node.water.food` | -5.8 | 수영 | F | 2 | 2 |
| `node.near.wood` | -1.1 | 장벽 전 | W | 2 | 3 |
| `node.near.stone` | 1.5 | 장벽 전 | S | 2 | 2 |
| `node.near.food` | 4.1 | 장벽 전 | F | 2 | 2 |
| `node.near.salvage` | 6.8 | 장벽 전 | D | 2 | 2 |
| `node.far.wood` | 10.2 | 돌도끼 뒤 | W | 2 | 3 |
| `node.far.salvage.1` | 12.8 | 돌도끼 뒤 | D | 2 | 2 |
| `node.far.stone` | 15.2 | 돌도끼 뒤 | S | 2 | 2 |
| `node.far.salvage.2` | 17.7 | 돌도끼 뒤 | D | 2 | 2 |

각 원정에서 노드는 다시 생성된다. 돌도끼 전에는 x≤8만 가능하므로 과거 이론 원장의 “도끼 없이 한 원정 W4”는 실제 동선으로 실행할 수 없다. Wave 14 성공 경로는 이 제약을 반영한다.

### 1.4 동결 비용

| 항목 | W | S | F | D | 선행 |
|---|---:|---:|---:|---:|---|
| 모닥불 | 2 | 1 | 0 | 0 | 없음 |
| 작업대 | 2 | 0 | 0 | 1 | 없음 |
| 빗물받이 | 2 | 1 | 0 | 1 | 없음 |
| 돌도끼 연구 / 제작 | 0 / 1 | 1 / 1 | 0 | 1 / 0 | 작업대 / 연구 |
| 밧줄 연구 / 제작 | 0 / 1 | 0 | 0 | 1 / 1 | 작업대 / 연구 |
| 가방 4→6 | 2 | 0 | 0 | 1 | 작업대 |
| 방 모듈 하나 | 2 | 0 | 0 | 1 | preview 자유, commit은 작업대 |
| 신호 1 / 신호 2 | 2 / 2 | 0 | 0 | 2 / 2 | 작업대 / 밧줄 보유 |

## 2. 실행 프로토콜

이 네 경로는 발견성을 재는 첫 사용자 세션이 아니라, 정해진 행동을 자연 입력으로 재현하는 owner/QA 검증 경로다. 참가자에게 해결 순서로 제공하거나 첫 사용자 결과와 합산하지 않는다.

1. 경로마다 Development Build를 완전히 종료한 뒤 다시 실행해 새 JSONL 파일과 새 `run_id`를 만든다.
2. 새 게임의 첫 `session.started.state_after`가 정확히 Day1/Camp/None, `W2/S1/F0/D0`, `E100/H70/L100`, bag4, signal0인지 확인한다.
3. `grant`, 자동 검증 진입, 좌표 `warp`, 디버그 오버레이, 외부 세이브 주입을 사용하지 않는다.
4. 한 경로 안에서는 locale을 `ko` 또는 `en` 하나로 고정하고 입력 장치도 하나로 고정한다. 물리 게임패드는 별도 실기이며 현재 `UNVERIFIED`다.
5. 목표 행동을 실제 이동·근접 안내·팝업·확정으로 수행한다. 원격 메뉴나 강제 상태 전환은 허용하지 않는다.
6. `rescued`, `exhausted`, `deadline`, 20분, crash/lock 중 먼저 온 시점에 종료한다.
7. JSONL 원본은 수정하지 않는다. 사람은 아래 “수동 판단” 칸만 별도 시트에 기록한다.

공통 유효 조건:

- `sequence`가 1씩 증가하고 모든 줄의 `run_id`가 같다.
- 시작부터 `run.completed`까지 1,200초 이하다.
- 성공 경로는 강제 귀환 없이 각 귀환 시 `E>0`, `L>0`이다.
- 자원·날짜·signal·bag·결말은 표와 정확히 같아야 한다. E/L의 참고치는 입력 지연 때문에 ±10까지 진단 범위로만 쓰며, 실제 로그값이 정본이다.
- 설명되지 않는 camp storage 증가, 음수 자원, 중복 비용, Day 3·4 deadline은 즉시 기술 결함 후보다.

## 3. JSONL 자동값과 사람 판단의 경계

| 대상 | 자동 수집 | 사람이 기록 | 판정 주의 |
|---|---|---|---|
| fresh run | `session.started`, 시작 fingerprint | 새 실행 파일을 실제로 다시 켰는지 | 같은 프로세스 Restart는 새 `run_id`가 아니므로 경로 분리에 쓰지 않음 |
| 날짜·결말 | `day.changed`, `day.survived`, `phase.changed`, `run.completed` | 결과 이유를 어떻게 이해했는지 | `run.completed.outcome`은 `rescued/exhausted/deadline`만 사용 |
| 자원 | `resource.changed`의 resource/location/delta와 before/after | 무엇을 남기거나 교체하려 했는지 | 개별 node ID와 pending loot 포기 이유는 자동 확정 불가 |
| 직접 설비 | proximity/popup/action completed·rejected | prompt를 보고 대상으로 판단한 근거 | 자동 이벤트는 발견 이유나 문구 이해를 증명하지 않음 |
| 연구·제작 | `research.completed`, `crafting.completed` | 왜 그 도구를 먼저 골랐는지 | outcome은 stable tech ID |
| 가방 | `bag.capacity.upgraded`, active_bag_slots | 신호 대신 가방을 고른 이유와 5·6번 칸 체감 | 교체·포기 의도는 사람이 기록 |
| 수영·장벽 | `swimming.entered/exited`, `vine_barrier.blocked/cleared` | 위험 예상과 장벽 해결 이해 | 장벽 clear는 원정마다 다시 기록될 수 있어 최초 sequence를 사용 |
| 신호 | `signal.stage1.completed`, `signal.stage2.completed` | 부족 피드백을 이해했는지 | stage2와 `run.completed rescued`가 같은 action transaction이어야 함 |
| 방 preview | `facility.action.completed`, action=`module.preview` | 세 후보를 비교했는지 | preview 이벤트명은 전용 이벤트가 아니라 facility action |
| 방 commit | `resource.changed`, action=`module.commit.<upper|side|basement>`, W-2/D-1 | 실제 확정 module ID, connector·공간 변화 | fingerprint에 committed module 필드가 없고 전용 `module.committed` 이벤트도 없어 사람 확인 필수 |

KO/EN은 동일한 stable event, enum, 수치와 판정식을 쓴다. 번역 문장 일치가 아니라 “같은 부족 자원·선행·결말을 자기 언어로 설명했는가”를 별도 이해도 질문으로 판단한다. `qps-long`은 레이아웃 QA 전용이며 사람 이해도 표본에 넣지 않는다.

## 4. 경로 ① 기본 4칸 가방 → Day 3 구조

경로 ID: `route.wave14.basic-bag4-rescue-d3`
의도: 가방 확장·돌도끼·방 없이 장벽 앞과 수상 노드만으로 구조가 가능한 하한을 검증한다.

| 날 | 시작 | 자연 원정·가방 | 귀환 참고 | 캠프 원자 행동 | 종료/결말 |
|---|---|---|---|---|---|
| D1 | `W2/S1/F0/D0`, `E100/H70` | water D2, water F2, near W2, near D2 = `W2/F2/D4`, 4/4 | `E≈68/L≈94` | 작업대 `W2/D1`; 밧줄 연구 `D1`; 제작 `W1/D1`; 식량1 | `W1/S1/F1/D1`, signal0, bag4, `E≈88/H65`, Day2 |
| D2 | `W1/S1/F1/D1` | D1과 동일 `W2/F2/D4`, 4/4 | `E≈55/L≈94` | 신호1 `W2/D2`; 식량1 | `W1/S1/F2/D3`, signal1, `E≈75/H65`, Day3 |
| D3 | `W1/S1/F2/D3` | near W2만 채집, 1/4 | `E≈69/L≈100` | 신호2 `W2/D2` | `W1/S1/F2/D1`, signal2, 즉시 `rescued` |

필수 자동 증거:

- D1 `swimming.entered`와 `swimming.exited`가 각각 1회 이상이다.
- `research.completed(outcome=rope)`와 `crafting.completed(outcome=rope)`가 D1에 있다.
- `signal.stage1.completed` 뒤 D2→D3의 `day.changed/day.survived`가 있고 deadline은 없다.
- `signal.stage2.completed` 직후 `run.completed(outcome=rescued)`이며 active bag은 끝까지 4다.

## 5. 경로 ② 돌도끼·가방 4→6 → Day 3 구조

경로 ID: `route.wave14.bag6-axe-rescue-d3`
의도: 업그레이드 비용을 내고도 장벽·수영을 포함한 6칸 한 번의 대형 귀환으로 구조가 가능한지 검증한다.

| 날 | 시작 | 자연 원정·가방 | 귀환 참고 | 캠프 원자 행동 | 종료/결말 |
|---|---|---|---|---|---|
| D1 | `W2/S1/F0/D0`, `E100/H70` | near W2/S2/F2/D2, 4/4 | `E≈76/L≈98` | 작업대 `W2/D1`; 돌도끼 연구 `S1/D1`; 제작 `W1/S1`; 식량1 | `W1/S1/F1/D0`, axe, bag4, `E≈96/H65`, Day2 |
| D2 | `W1/S1/F1/D0` | axe near W3 + near D2 + far D2 = `W3/D4`, 4/4 | `E≈77/L≈97` | 밧줄 연구 `D1`; 제작 `W1/D1`; 가방 `W2/D1`; 식량1 | `W1/S1/F0/D1`, axe+rope, bag6, `E≈97/H65`, Day3 |
| D3 | `W1/S1/F0/D1` | water D2, axe near W3, near F2/D2, axe far W3 = `W6/F2/D4`, 정확히 6/6 | `E≈61/L≈94` | 신호1+2 `W4/D4` | `W3/S1/F2/D1`, signal2, 즉시 `rescued` |

D3 적재 순서는 water D → near W → near F → near D → far W로 고정한다. W3 두 노드는 합계 W6을 세 stack으로, D4는 두 stack으로, F2는 한 stack으로 사용해 정확히 6칸이다. 4칸이면 같은 적재가 불가능하다.

필수 자동 증거:

- D1 `research.completed/crafting.completed(outcome=stone_axe)`가 있다.
- D2 `vine_barrier.cleared`와 `bag.capacity.upgraded(outcome=4_to_6)`가 있고 차감은 W-2/D-1 한 번이다.
- D3에는 `swimming.entered/exited`, active bag6, return 직전 bag `W6/F2/D4`가 있다.
- 신호 두 단계와 rescued가 D3에 이어지고 grant로 설명해야 하는 storage 증가가 없다.

## 6. 경로 ③ 방 모듈 1개와 신호 경쟁 → Day 4 구조

경로 ID: `route.wave14.module1-signal-tension-rescue-d4`
기본 module: `room.upper.standard`. side/basement는 같은 `W2/D1`로 대체 가능하다.
의도: 가방 4칸을 유지하고 Day2 module 투자가 기본 경로의 신호1을 한 원정 늦추는 실제 경쟁을 검증한다.

| 날 | 시작 | 자연 원정·가방 | 귀환 참고 | 캠프 원자 행동 | 종료/결말 |
|---|---|---|---|---|---|
| D1 | `W2/S1/F0/D0`, `E100/H70` | water D2/F2 + near W2/D2 = `W2/F2/D4`, 4/4 | `E≈68/L≈94` | 작업대; 밧줄 연구·제작; 식량1 | `W1/S1/F1/D1`, `E≈88/H65`, Day2 |
| D2 | `W1/S1/F1/D1` | D1과 동일 | `E≈55/L≈94` | 세 후보 preview; upper commit `W2/D1`; 식량1 | `W1/S1/F2/D4`, module1, signal0, `E≈75/H65`, Day3 |
| D3 | `W1/S1/F2/D4` | D1과 동일 | `E≈43/L≈94` | 신호1 `W2/D2`; 식량1 | `W1/S1/F3/D6`, signal1, `E≈63/H65`, Day4 |
| D4 | `W1/S1/F3/D6` | near W2만 채집 | `E≈56/L≈100` | 신호2 `W2/D2` | `W1/S1/F3/D4`, signal2, 즉시 `rescued` |

경쟁 판정: D2 귀환 직후 `W3/D5`다. 신호1을 먼저 사면 D3 구조가 가능하지만 module `W2/D1`을 먼저 사면 W1만 남아 같은 날 신호1을 살 수 없다. 모듈은 정확히 한 원정의 W 병목을 만들되 Day4 구조를 막지 않는다.

필수 자동·수동 증거:

- 자동: `facility.action.completed(action=module.preview)` 뒤 `resource.changed(action=module.commit.upper)`의 storage W-2/D-1이 정확히 한 번이다.
- 자동: D2 module 뒤 signal0, D3 signal1, D4 signal2/rescued다.
- 수동: upper가 실제 확정되고 connector와 room이 나타났으며 두 번째 module은 확정되지 않았다.
- 전용 `module.committed` 이벤트와 fingerprint module 필드가 없으므로 JSONL만으로 module 완료 PASS를 선언하지 않는다.

## 7. 경로 ④ 과수색·H0 압박 → Day 5 기한 실패

경로 ID: `route.wave14.oversearch-hunger-deadline-d5`
의도: 구조 투자 없이 수영 시간을 과소비하고 식량을 먹지 않았을 때 Day3·4는 계속되며 Day5만 deadline인지 검증한다.

| 날 | 행동 | 귀환/정산 예상 | 저장·진행 |
|---|---|---|---|
| D1 | water D2/F2를 수색하고 수영 상태로 L50±2까지 움직인 뒤 수동 귀환; 먹지 않음 | 귀환 `E≈54/L≈50`; 정산 `E≈74/H35` | `W2/S1/F2/D2`, signal0, Day2 |
| D2 | D1 반복; 먹지 않음 | 귀환 `E≈28/L≈50`; H0 페널티 뒤 `E20/H0` | `W2/S1/F4/D4`, signal0, Day3 |
| D3 | 출발 후 즉시 수동 귀환; 먹지 않음 | 정산 `E20/H0` | Day4, `Result=None` |
| D4 | 출발 후 즉시 수동 귀환; 먹지 않음 | 정산 `E20/H0` | Day5, `Result=None` |
| D5 | 출발 후 즉시 수동 귀환; 먹지 않고 종료 | H0 페널티·기본 회복 뒤 `E20/H0` | signal0, 정확히 `deadline` |

`EndDay()`는 H0 페널티 뒤 기본 E20을 회복하므로 이 주 경로의 결말은 starvation enum이 아니라 `deadline`이다. 사람이 “굶주림 때문에 실패했다”고 말해도 게임 결과와 원인을 분리해 기록한다.

탈진 분기: D2에 L50에서 돌아오지 않고 계속 수영해 E0이 되면 `run.completed(outcome=exhausted)`가 먼저 나와야 한다. 이 분기는 Day5 ledger와 별도 run으로 기록하며 두 결과를 한 세션처럼 합치지 않는다.

필수 자동 증거:

- D1·D2에 swimming enter/exit가 있고, food storage는 증가하지만 `resource.changed(action=survival.eat, delta=-1)`이 없다.
- Day2 정산부터 H0이며 D2→D3, D3→D4, D4→D5에 `day.changed/day.survived`가 존재한다.
- D3·D4에 `run.completed`가 없고 D5 종료에만 `run.completed(outcome=deadline)`가 한 번 있다.
- signal stage 이벤트와 bag/module 성장 이벤트는 없어야 한다.

## 8. 경로별 합격선

| 경로 | 자원·진행 합격 | 결말 합격 | 현재 상태 |
|---|---|---|---|
| basic bag4 | 표의 일자별 storage, bag4, rope, signal 0→1→2 | Day3 rescued | `UNRUN` |
| bag6 | axe, barrier clear, bag 4→6 W2/D1 1회, D3 bag 6/6 | Day3 rescued | `UNRUN` |
| module1 | module W2/D1 1회, signal0→1→2, bag4 | Day4 rescued | `UNRUN` |
| oversearch | D2부터 H0, signal0, Day3·4 생존 | Day5 deadline | `UNRUN` |

실행 뒤에도 표의 예상 경로와 다르다는 이유만으로 자동 FAIL로 만들지 않는다. 먼저 다음처럼 분류한다.

- `TECHNICAL`: 충분한 자원인데 거부, 중복 차감, 잘못된 날짜·결말, 로그 손상.
- `DISCOVERY`: 대상·prompt·비용·선행을 찾지 못함.
- `MIXED_UX_FIRST`: 늦은 발견 때문에 자원·시간 부족이 뒤따름.
- `RESOURCE`: 대상과 비용을 정확히 이해하고 의도적으로 우선순위를 맞췄는데 같은 자원 부족이 남음.
- `NO_ISSUE`: 계약과 행동 결과가 일치.

## 9. 한 축만 조정하는 결정 규칙

1. owner 1회, 이 문서의 미실행 산술, 자동 fixture는 밸런스 변경 근거가 아니다.
2. 먼저 P0/기술 결함→발견성·prompt→동선·포커스→20분 pacing 순으로 배제한다. 이 단계에서는 v0.2를 동결한다.
3. KO 3명·EN 3명의 유효 외부 6세션, 언어별 의미 이해 통과, 같은 `RESOURCE` 원인 2세션 이상일 때만 제안을 연다.
4. 제안은 자동 적용하지 않고 다음 중 정확히 하나만 선택한다.
   - `RESOURCE_INFLOW`: 특정 자원 노드 수 또는 획득량 한 축
   - `INVESTMENT_COST`: 특정 제작·성장·신호 비용 한 항목
   - `SURVIVAL_PRESSURE`: E/H/L 소모 또는 회복 한 축
5. 한 빌드에서 두 축을 바꾸지 않는다. 전후 같은 네 경로와 같은 KO/EN 판정으로 재실행한다.
6. Day5 기한 변경, 새 자원·콘텐츠·기능 추가는 이 게이트의 조정 후보가 아니며 별도 사용자 결정이 필요하다.

KO/EN 공통 표본 기준은 동일하다. 기능·자원·결말 수치는 locale별로 다르게 허용하지 않으며, 영어 문구가 길다는 이유로 비용이나 시간 합격선을 바꾸지 않는다.

## 10. 결정·질문·블로커

### 새 결정

- `decision.wave14.geometry-aware-ledger`: 실제 x좌표·장벽·4/6칸 중첩으로 가능한 노드 묶음만 자연 경로로 인정한다.
- `decision.wave14.prescribed-route-not-discovery`: 네 경로는 owner/QA 실행용이며 첫 사용자 발견성 표본과 분리한다.
- `decision.wave14.auto-human-boundary`: JSONL 수치와 사람의 의도·이해를 별도 필드로 판정한다.
- `decision.wave14.one-axis-change`: 외부 6세션 임계치 뒤에도 한 번에 한 축만 제안한다.

### 열린 질문

- 실제 Windows build에서 네 경로가 각각 20분 안에 끝나는가?
- 사람의 이동·팝업 체류로 E/L이 참고치에서 얼마나 퍼지는가?
- 향후 전용 `module.committed` event와 fingerprint의 module ID를 추가할 것인가? 이번 문서에서는 구현하지 않는다.

### 현재 블로커

- 네 fresh natural run과 원본 JSONL이 아직 없다. 따라서 기존 자연 경로·가방 QA 항목은 완료 처리할 수 없다.
- 첫 사용자 KO3/EN3와 물리 게임패드 실기는 여전히 없다.
- 전체 qps-long과 Steam 준비는 이 장부 범위 밖의 기존 gap이다.

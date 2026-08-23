# Wave 9 첫 방 모듈 증축 밸런스 v0.2

> 상태: `DESIGN LOCKED / IMPLEMENTATION NOT IN THIS COMMIT`
> 작업 기준점: `b4142df02f3745ea18a72888fdf3b029dbe78886` (`origin/master f95b192` 포함)
> 데이터 정본: `.forge/design/wave9-module-balance.json`
> 공간 정본: `Docs/Design/wave9-spatial-base-camp-spec.md`
> 공식 영문 게임 제목: `TBD`

이 문서는 첫 수직 슬라이스의 위층·옆방·지하실 모듈에 남아 있던 `TBD_BALANCE`만 확정한다. `W/S/F/D`는 나무/돌/식량/표류물, `E/H/L`은 체력/허기/일광이다. 기존 v0.2 경제, 4→6칸 가방, 도구, 신호 2단계와 공간 좌표는 바꾸지 않는다.

## 1. 계산 기준으로 다시 잠그는 현재값

### 1.1 시작·가방·생존

| 항목 | 동결값 | 현재 런타임 근거 |
|---|---:|---|
| 시작 창고 | `W2/S1/F0/D0` | `GameSession.Reset()` |
| 시작 상태 | `E100/H70/L100`, Day 1 | `GameSession.Reset()` |
| 기본 가방 | 4칸, 칸당 중첩 2 | `DefaultBagSlotCount=4`, `StackLimit=2` |
| 가방 확장 | 작업대에서 1회 `W2/D1`, 4→6칸 | `BagUpgradeWoodCost=2`, `BagUpgradeSalvageCost=1` |
| 하루 허기 | H `-35` | `EndDay()` |
| 식량 1 | H `+35`, 최대 100 | `UseFood()` |
| H0 페널티 | E `-35` 뒤 회복 | `EndDay()` |
| 기본 회복 | E `+20` | `EndDay()` |

### 1.2 자원 노드

| 구역 | 노드 | 기본 획득 | 돌도끼 보유 시 |
|---|---|---:|---:|
| 수상 | D 1, F 1 | 각 2 | 동일 |
| 장벽 전 육상 | W 1, S 1, F 1, D 1 | 각 2 | W만 3 |
| 장벽 뒤 육상 | W 1, S 1, D 2 | 각 2 | W만 3 |
| 날짜별 전체 | W 2, S 2, F 2, D 4 | 원시 20 | W 두 노드 `+1`, 최대 22 |

노드는 날짜마다 다시 수색할 수 있다. 돌도끼는 W 노드 `+1`과 숲 장벽 제거만 담당하며 “두 배”가 아니다. 밧줄은 신호 2단계 필수 보유 도구이며 별도 장벽을 열지 않는다.

### 1.3 기존 비용과 원정 소모

| 분류 | 항목 | W | S | F | D | 선행 |
|---|---|---:|---:|---:|---:|---|
| 건설 | 작업대 | 2 | 0 | 0 | 1 | 없음 |
| 연구 | 돌도끼 | 0 | 1 | 0 | 1 | 작업대 |
| 제작 | 돌도끼 | 1 | 1 | 0 | 0 | 연구 완료 |
| 연구 | 밧줄 | 0 | 0 | 0 | 1 | 작업대 |
| 제작 | 밧줄 | 1 | 0 | 0 | 1 | 연구 완료 |
| 성장 | 가방 4→6 | 2 | 0 | 0 | 1 | 작업대, 1회 |
| 구조 | 신호 1 | 2 | 0 | 0 | 2 | 작업대 |
| 구조 | 신호 2 | 2 | 0 | 0 | 2 | 밧줄 보유, 신호 1 |

| 행동 | E | L |
|---|---:|---:|
| 육상 이동 | `-0.18/s` | `-0.75/s` |
| 수영 이동 | `-0.65/s` | `-1.15/s` |
| 수영 정지 | `-0.22/s` | `-1.15/s` |
| 육상 채집 | `-6/회` | 0 |
| 수상 수색 | `-9/회` | 0 |
| 일몰 강제 귀환 | E `-22`, 최저 1 | 가방 귀환 뒤 적용 |

## 2. 결정 기록

| 결정 ID | 확정 내용 | 선택 이유 | 채택하지 않은 안 |
|---|---|---|---|
| `decision.wave9.module-balance-v0.2` | 위층·옆방·지하실 모두 `W2/D1` | 세 방이 모두 `12×5u` 표준 모듈이고 아직 생산·회복·저장 보너스가 없으므로 방향만으로 가격 차를 만들 근거가 없음 | 무료, 방향별 차등 비용 |
| `decision.wave9.module-preview-before-unlock` | 캠프에서 세 후보와 비용·기하 상태는 처음부터 미리보기 | 무엇을 향해 자원을 모으는지 숨기지 않음 | 작업대 전 후보 전체 숨김 |
| `decision.wave9.module-commit-after-workbench` | 실제 확정은 작업대 건설 뒤 | 작업대가 연구·가방·신호와 공유되는 자연 성장 관문이며 임의 Day 잠금이 필요 없음 | Day 2 고정 해금, 별도 연구 |
| `decision.wave9.one-module-per-run` | run당 실제 확정 1개 | 세 방향 비교와 공간 성장만 검증하고 장기 건축 범위 폭발을 막음 | 두 번째 확장, 철거·환불 |

`W2/D1`은 가방 확장과 정확히 같은 자원 투자다. 따라서 증축은 공짜 장식이 아니며, Day 2에 모듈을 먼저 확정할지 가방을 먼저 늘릴지 선택하게 한다. 동시에 기존 가방 포함 자연 경로의 구조 후 잉여 `W3/D3`보다 작아 숙련 플레이의 필수 함정도 아니다.

방향별 차이는 비용이 아니라 connector와 동선이다. upper는 사다리, side는 가로 문, basement는 하향 hatch를 사용한다. 이 문서는 생산 보너스, 침대·휴식 효과, 저장량 증가, 지하 자원 같은 기능을 약속하지 않는다.

## 3. Unity 데이터 계약

### 3.1 모듈 정의

| `moduleId` | 후보 | `slotId` | 크기 | 확정 해금 | 비용 | 기능 보너스 |
|---|---|---|---:|---|---|---|
| `room.upper.standard` | 위층 | `slot.start.upper` | `12×5u` | 작업대 | `W2/D1` | 없음 |
| `room.side.standard` | 옆방 | `slot.start.side` | `12×5u` | 작업대 | `W2/D1` | 없음 |
| `room.basement.standard` | 지하실 | `slot.start.basement` | `12×5u` | 작업대 | `W2/D1` | 없음 |

- preview는 `Phase=Camp`, `Result=None`이면 세 후보 모두 가능하다. 작업대가 없어도 geometry, 전체 비용과 `작업대 필요`를 보여 준다.
- commit은 `Phase=Camp`, `Result=None`, 작업대 보유, geometry/path 유효, 비용 충족, `committedModuleId=null`일 때만 가능하다.
- 확정·미리보기·취소는 E/H/L을 소모하지 않는다.
- 가격은 module ID의 locale 이름이나 화면 순서로 찾지 않고 `.forge/design/wave9-module-balance.json`의 stable ID로 조회한다.

### 3.2 상태 우선순위

| 상태 ID | 조건 | 표시 | Confirm 결과 |
|---|---|---|---|
| `TERMINAL_OR_MODAL` | 결과·다른 modal·transaction 중 | 현재 입력 소유자 표시 | 거부, 변화 0 |
| `PROTOTYPE_LIMIT` | `committedModuleId != null` | 첫 확장은 이미 확정됨 | 두 번째 확정 거부, 변화 0 |
| `NO_CONNECTION_SLOT` | reciprocal slot 정의 없음 | 연결 슬롯 불일치 | 거부, 변화 0 |
| `SLOT_UNAVAILABLE` | slot 비활성·점유·후보와 불일치 | 사용할 수 없는 연결 슬롯 | 거부, 변화 0 |
| `OVERLAP/TERRAIN_BLOCKED/PATH_BLOCKED` | 공간 규칙 실패 | 정확한 기하 원인 | 거부, 변화 0 |
| `LOCKED` | geometry 유효, 작업대 없음 | 작업대 요구+`W2/D1` 전체 비용 | 거부, 변화 0 |
| `SHORT` | 작업대·geometry 유효, 비용 부족 | 보유/필요와 정확한 부족 W/D | 거부, 변화 0 |
| `READY` | 모든 조건 충족 | 대상·비용·결과·Confirm glyph | 원자 확정 1회 |

geometry와 economy 상태는 별도 필드로 계산할 수 있지만 player-facing 주 상태는 위 우선순위에서 하나만 선택한다. `PROTOTYPE_LIMIT`가 비용 부족보다 먼저이며, 이미 방을 지은 뒤 자원을 더 모아도 두 번째 방은 열리지 않는다.

### 3.3 원자 차감과 취소

1. 자원 4종, committed module/slot/day와 room graph snapshot을 만든다.
2. 결과·중복 입력·1개 제한→module/slot ID→reciprocal/점유→겹침·지형→connector/필수 경로→작업대→비용 순으로 검증한다.
3. 모두 통과하면 같은 transaction에서 `W2/D1`을 한 번 차감하고 room, reciprocal connector, placement zone과 committed state를 함께 확정한다.
4. room·connector·zone·route 중 하나라도 생성 실패하면 snapshot 전체를 복원한다.
5. Cancel, Back, LOCKED, SHORT, invalid slot, duplicate Submit은 자원·방·가방·도구·설비·신호·날짜·E/H/L을 바꾸지 않는다.

### 3.4 run 상태

| 필드 | 형식 | 지속·초기화 |
|---|---|---|
| `committedModuleId` | nullable stable module ID | Day·원정 전환 유지, 새 게임 `null` |
| `committedSlotId` | nullable stable slot ID | Day·원정 전환 유지, 새 게임 `null` |
| `committedDay` | `0..3` | 확정 Day 기록, 새 게임 `0` |
| `roomGraph/occupiedModuleSlots` | committed ID에서 복원 가능한 run 상태 | Day·원정 전환 유지 |
| `previewModuleId/previewValidity` | UI transient | 팝업·preview 종료 시 폐기 |
| `transactionInProgress` | UI/runtime transient | 성공·실패 프레임 뒤 false |

이 지속은 현재 run 안의 Day 1~3 전환을 뜻한다. 디스크 세이브·로드, 메타 영구 해금과 환불용 비용 snapshot은 이번 범위 밖이다.

## 4. 최소 원정 수 증명

가방 확장 없이 작업대·돌도끼·밧줄·모듈·신호를 모두 사려면 총 `W10/S2/D9/F1`이 든다. 시작값을 빼도 이론상 채집은 `W8/S1/D9/F1=19`이므로 4칸 최대 8개인 두 원정의 16개보다 많다.

가방을 추가하면 비용이 `W2/D1` 늘어 이론상 순채집 22개가 필요하다. Day 1은 4칸 8개, 가장 빨라도 Day 2만 6칸 12개이므로 두 원정 최대 20개보다 많다. 따라서 **방 하나+구조의 숙련 최소도 3회 원정**이다. 실제 노드 묶음과 도끼 W3을 적용한 아래 경로는 22개를 세 번에 운반한다.

## 5. 세 경로 자원 원장

### 5.1 숙련 최소: 모듈 먼저, 가방 4칸 유지, Day 3 구조

| 시점 | 유입·소모 | W | S | F | D | 진행 |
|---|---|---:|---:|---:|---:|---|
| 시작 | v0.2 | 2 | 1 | 0 | 0 | 가방 4 |
| Day 1 귀환 | `+W2/S2/D4`=8 | 4 | 3 | 0 | 4 | 4/4, 수상 D 1회 |
| Day 1 처리 | 작업대 `W2/D1`, 도끼 연구 `S1/D1`, 제작 `W1/S1` | 1 | 1 | 0 | 2 | 도끼, H35 |
| Day 2 귀환 | `+W3/D2/F2`=7 | 4 | 1 | 2 | 4 | 4/4 |
| Day 2 처리 | 밧줄 연구·제작 `W1/D2`, 모듈 `W2/D1`, 식량 1 | 1 | 1 | 1 | 1 | 방 1, 밧줄, 가방 4, H35 |
| Day 3 귀환 | `+W3/D4`=7 | 4 | 1 | 1 | 5 | 4/4 |
| 구조 | 신호 1·2 `W4/D4` | 0 | 1 | 1 | 1 | 즉시 구조 성공 |

판정: 방향에 관계없이 `W2/D1`이므로 세 모듈 모두 같은 원장으로 성공한다. 최종 W0이라 잘못된 W 선택 여유는 없지만 음수·grant·warp가 없고, 방 확정이 필수 함정은 아니다.

### 5.2 평균 기준: Day 2 가방, Day 3 모듈+구조

| 시점 | 유입·소모 | W | S | F | D | 진행 |
|---|---|---:|---:|---:|---:|---|
| Day 1 처리 뒤 | 숙련 경로와 동일 | 1 | 1 | 0 | 2 | 도끼, 가방 4 |
| Day 2 귀환 | `+W3/D2/F2` | 4 | 1 | 2 | 4 | 4/4 |
| Day 2 처리 | 밧줄 `W1/D2`, 가방 `W2/D1`, 식량 1 | 1 | 1 | 1 | 1 | 밧줄, 가방 6, H35 |
| Day 3 귀환 | `+W6/D6`=12 | 7 | 1 | 1 | 7 | 6/6 |
| Day 3 모듈 | `W2/D1` | 5 | 1 | 1 | 6 | 방 1 |
| 구조 | 신호 1·2 `W4/D4` | 1 | 1 | 1 | 2 | 즉시 구조 성공 |

판정: 가방과 모듈을 모두 사도 구조된다. 그러나 Day 2 처리 직전 밧줄 뒤 자원은 `W3/D2`라 가방+모듈 합계 `W4/D2`를 동시에 낼 수 없다. 평균 플레이는 하나를 먼저 선택하고 다른 하나를 Day 3으로 미뤄야 하므로 성장 투자와 신호 사이 긴장이 분명하다.

### 5.3 욕심 경로 A: 모듈 뒤 Day 3 늦은 가방으로 기한 실패

| 시점 | 유입·소모 | W | S | F | D | 결과 |
|---|---|---:|---:|---:|---:|---|
| Day 2 처리 뒤 | 숙련 경로의 모듈 우선 | 1 | 1 | 1 | 1 | 방 1, 가방 4 |
| Day 3 귀환 | `+W3/D4` | 4 | 1 | 1 | 5 | 신호 두 단계분 확보 |
| 늦은 가방 구매 | `W2/D1` | 2 | 1 | 1 | 4 | 가방 6, 원정 재개 불가 |
| 신호 1 | `W2/D2` | 0 | 1 | 1 | 2 | signal 1/2 |
| 신호 2 시도 | W2 부족 | 0 | 1 | 1 | 2 | `SHORT`, 변화 0 |
| Day 3 정산 | 구조 미완료 | 0 | 1 | 1 | 2 | `Deadline` |

판정: 모듈 비용은 성공 자원을 없애는 함정이 아니지만, 같은 run에 성장 투자를 늦게 하나 더 사면 구조 비용과 실제로 충돌한다. 실패한 신호 시도나 모듈 재확인에서는 추가 차감이 없어야 한다.

### 5.4 욕심 경로 B: 가방 밖 자원까지 수영 수색해 탈진

기존 과수색 benchmark대로 Day 1에 육상 4노드와 수상 2노드를 모두 건드리면 이동·채집 E `74.9`를 써 귀환 E25.1, 회복 뒤 Day 2 시작 E45.1이 된다. Day 2에 방과 가방 재료를 더 모으려고 수영 이동 42초 `E27.3`과 수상 수색 2회 `E18`을 반복하면 총 `E45.3`으로 **가방 선택이나 모듈 확정 전에 Exhausted**가 된다.

판정: 모듈 확정 자체는 E/L을 쓰지 않지만, 두 성장 투자를 한꺼번에 마련하려고 가방 밖 노드까지 욕심내는 기존 탈진 실패는 유지된다.

## 6. 생존·일광 여유 검증

| 경로·일자 | 보수적 E 계산 | 귀환/구조 E | H |
|---|---|---:|---|
| 공통 Day 1 | 기존 혼합 이동·4노드 benchmark `E42` | E58→회복 후 E78 | `70→35` |
| 공통 Day 2 | 육상 이동 90초 `E16.2`+채집 3회 `E18` | E43.8→회복 후 E63.8 | 식량 1로 `35→70→35` |
| 숙련 Day 3 | 육상 이동 90초 `E16.2`+채집 3회 `E18` | E29.6에서 구조 | 정산 전 구조, H35 |
| 평균 Day 3 | 육상 이동 90초 `E16.2`+채집 5회 `E30` | E17.6에서 구조 | 정산 전 구조, H35 |

Day 2·3의 90초 육상 이동은 L67.5를 써 L32.5가 남는다. Day 1 수영 왕복도 기존 benchmark L56.2를 남긴다. 따라서 두 성공 경로는 E/H/L이 모두 양수이고, 별도의 회복 설비 효과나 숨은 지급을 가정하지 않는다.

## 7. 현장형 비용 안내와 현지화

### 7.1 문자열 token

KO가 의미·정보 순서의 기준 원문이고 EN은 자연스러운 어순을 소유한다. 문자열을 module ID나 enum 이름으로 조립하지 않는다.

| 안정 키 | 상황 | KO 기준 | EN 의도 | token |
|---|---|---|---|---|
| `ui.module.preview.cost` | 선택 후보 비용 | `{moduleName} · 나무 {wood} · 표류물 {salvage} · {state}` | `{moduleName} · {wood} Wood · {salvage} Salvage · {state}` | moduleName, wood, salvage, state |
| `interaction.module.locked_workbench` | 작업대 없음 | 작업대 필요 | Workbench Required | 없음 |
| `interaction.module.missing` | 비용 부족 | `{moduleName} 부족 · {missing}` | Missing for {moduleName} · {missing} | moduleName, missing |
| `interaction.module.ready` | 확정 가능 | 설치 가능 | Ready to Build | 없음 |
| `interaction.module.slot_unavailable` | slot 불가 | 이 연결 슬롯은 사용할 수 없다. | This connection slot is unavailable. | 원인 코드 노출 금지 |
| `interaction.module.prototype_limit` | 이미 하나 확정 | 첫 확장은 이미 완성했다. | The first expansion is already complete. | 장기 최대치 의미 금지 |
| `interaction.module.committed` | 확정 성공 | `{moduleName} 완성` | {moduleName} Complete | moduleName |

`{missing}`은 `나무 {count}`→`표류물 {count}` 순으로 0이 아닌 정확한 부족량만 넣는다. EN은 `{count} Wood`, `{count} Salvage`를 사용한다. Confirm/Cancel footer의 `{confirmInputGlyph}`·`{cancelInputGlyph}`는 현재 binding에서 공급하고 `Enter`, `E`, `A/B`를 문자열에 굽지 않는다.

qps-long 최장 예문은 `⟦Ûppër Røøm · 2 Wøød · 1 Sålvågë · Rëådÿ — plëåşë çønfirm nøw⟧`다. 향후 es/ja/zh-Hans/zh-Hant도 같은 key와 token을 사용하되 locale별 어순과 짧은 상태어를 소유한다.

### 7.2 화면 계약

- 모듈 preview 동안 소형 근접 `ContextPrompt`는 숨기고, 선택 후보 하나의 `ModuleCostChip`만 내레이션 카드 아래 overlay lane에 표시한다.
- 1280×800에서 anchor `x=640`, `top=max(294,NarrationBottom+12)`, 최대 `440×64px`, 최대 2줄, 본문 18px 하한이다.
- 첫 줄은 module name·비용, 둘째 줄은 `LOCKED/SHORT/READY`와 현재 Confirm/Cancel glyph다. locale format이 행 순서를 소유한다.
- 세 후보 유령·connector·경로와 김씨를 유지하고 화면 대부분을 덮는 전역 건설 메뉴를 열지 않는다.
- qps-long에서 clipping·tofu·panel 확장·김씨/선택 slot/필수 경로 완전 가림은 각각 0건이어야 한다. 길면 locale short state를 먼저 쓰고 비용 숫자·자원 종류·입력 glyph는 말줄임표로 숨기지 않는다.

## 8. 수용 조건과 위험

### 8.1 구현·QA 수용 조건

- 데이터의 세 module ID, slot ID, `12×5u`, 작업대 unlock과 `W2/D1`이 문서와 정확히 일치한다.
- 새 게임에서 세 후보를 모두 preview할 수 있고 작업대 전 `LOCKED`, 작업대 후 자원 부족 `SHORT`, 충분 `READY`가 된다.
- 세 module 중 어느 하나를 확정하면 정확히 W2/D1이 한 번 차감되고 Day·원정 전환 뒤 유지된다.
- 두 번째 module, 같은 프레임 중복 Submit, Cancel과 모든 실패 상태는 차감·room graph 변화 0이다.
- 최소·평균·기한·탈진 원장이 grant/warp 없이 같은 시작값과 노드로 재현된다. 자동 fixture의 grant는 자연 경제 PASS로 계산하지 않는다.
- ko/en/qps-long과 keyboard/gamepad에서 ID·비용·상태 결과가 같고 glyph·어순만 locale/device에 맞게 달라진다.

### 8.2 위험과 조정 순서

1. 외부 참가자가 module preview나 작업대 잠금을 찾지 못하면 prompt·slot·비용 가독성을 먼저 고치며 가격을 낮추지 않는다.
2. 발견성·경로가 정상인데 W/D 부족만으로 숙련 자연 경로가 반복 실패할 때만 module 한 축을 조정한다. 신호·가방·생존 수치를 동시에 바꾸지 않는다.
3. 세 방향 선택률 차이는 connector·동선 문제로 먼저 본다. 구현하지 않은 보너스를 붙여 선택률을 맞추지 않는다.
4. `W2/D1→무료`는 긴장을 없애고, W 또는 D를 추가하면 평균 경로의 Day 3 여유를 빠르게 지우므로 첫 외부 코호트 전 변경하지 않는다.
5. 실제 물리 게임패드, 인간 20분 성공률과 추가 locale 번역은 증거 전 `UNVERIFIED`다.

## 9. Unity 적용 주의

- 현재 `GameSession`에는 module enum·상태·transaction이 없다. 기존 `Spend()`를 검증 전에 직접 호출하지 말고 snapshot/rollback 또는 먼저 완성한 staged transaction으로 감싼다.
- stable ID는 `room.upper.standard`, `room.side.standard`, `room.basement.standard`를 사용한다. 화면 순서·번역문·GameObject 이름을 저장 ID로 사용하지 않는다.
- `committedModuleId/slotId/day`는 `EndDay`, `BeginSearch`, `ReturnToCamp`에서 유지하고 `Reset()`에서만 초기화한다.
- geometry/path 판정과 economy 판정을 분리하고 최종 player-facing 상태는 3.2의 우선순위로 합친다.
- 비용 data가 누락되면 무료로 fallback하지 말고 후보를 `LOCKED` 또는 개발 오류로 막는다.
- module confirm/cancel에 E/H/L을 연결하지 않는다. 배치 때문에 기존 신호·가방·도구 비용이나 노드 획득량을 바꾸지 않는다.
- 디스크 세이브·로드, 철거·환불과 두 번째 확장을 이번 구현에 암묵적으로 추가하지 않는다.

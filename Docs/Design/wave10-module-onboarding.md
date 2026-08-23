# Wave 10 방 확장 발견성·실패 사유 계약

> 상태: `DESIGN LOCKED / IMPLEMENTATION AND ART UNCHANGED`
> 통합 기준: `origin/master 097cd1cbfa1f434c9836e8393c0b59f18e8d8e09`
> 기계 정본: `.forge/design/wave10-module-onboarding.json`
> 상속 정본: `.forge/design/wave9-module-balance.json`, `wave9-spatial-base-camp-spec.md`, `wave9-module-expansion-balance.md`
> Forge stable IDs: `feature.camp-object-interaction`, `feature.camp-module-expansion`, `screen.camp`
> 공식 영문 게임 제목: `TBD`

이 문서는 Wave 9의 공간·밸런스를 바꾸지 않고, 처음 보는 플레이어가 전역 메뉴 없이 방 확장을 발견하고 실패 이유를 정확히 읽는 경로만 고정한다. 새 게임 시작 창고는 `W2/S1/F0/D0`이므로 표류물 없이 작업대를 90초 안에 자연 건설하는 경로는 없다. 따라서 첫 90초의 목표는 **직접 연결 슬롯에 접근해 세 후보를 보고, 확정 시도에서 작업대 선행과 W2/D1을 이해하는 것**이다. 작업대 건설과 실제 확정은 이후 자연 원정에서 같은 인과를 잇는다.

## 1. 바꾸지 않는 규칙

| 항목 | 동결값 |
|---|---|
| 후보 | `room.upper.standard`, `room.side.standard`, `room.basement.standard` |
| preview | 작업대 전부터 세 후보 모두 가능 |
| commit unlock | 작업대 건설 |
| 비용 | 세 후보 공통 `W2/D1` |
| run 제한 | 세 후보 중 하나만 확정 |
| preview·취소·확정 자체의 생존 소모 | E/H/L 0 |
| 성공 거래 | W2/D1 정확히 1회 차감, room·connector·zone 원자 생성 |
| 실패·취소 | 자원·방·가방·설비·도구·신호·날짜·E/H/L 변화 0 |

추가 비용, 새 자원, 방향별 보너스, 두 번째 확장, 철거·환불, grant, warp, 전역 캠프 메뉴와 아트 채택은 이 계약 밖이다.

## 2. 첫 90초 최소 온보딩

### 2.1 행동 소유권

- `slot.start.upper`, `slot.start.side`, `slot.start.basement`는 각각 직접 다가갈 수 있는 현장 상호작용 대상이다. 기존 `1.25u` 근접·latch 규칙을 사용한다.
- 연결 슬롯 Interact는 작은 대상 팝업을 열고 `ui.module.expand` 한 action에 포커스한다. action을 Submit해야 `ModulePreview`가 열린다. 원거리 Interact는 아무 메뉴도 열지 않는다.
- preview는 접근한 슬롯과 같은 후보에서 시작하고, 이후 안정 순서 `upper → side → basement → upper`로 순환한다.
- 기존 `storage.planning`의 상황형 preview 진입은 보조 경로로 남길 수 있지만 Wave 10의 90초 발견 PASS로 계산하지 않는다.
- 작업대는 비용·commit unlock을 소유하되 원거리 확정 버튼을 소유하지 않는다. 플레이어는 연결 슬롯에서 `작업대 필요`를 본 뒤 자연 원정으로 작업대를 건설하고 같은 슬롯으로 돌아와 확정한다.

이는 작업대를 무료 지급하거나 첫 90초에 실제 확정을 요구하지 않는다. 발견을 위해 전역 메뉴를 켜거나 별도 튜토리얼 카드를 띄우는 것도 허용하지 않는다.

### 2.2 시간 진행표

타이머의 00:00은 새 게임에서 월드 조작이 실제 활성화된 첫 프레임이다. 아래 시간은 자동 진행이 아니라 첫 사용자 5세션에서 관찰할 상한이다.

| 제한 | 이정표 | 달성 정의 | 미달 시 먼저 볼 것 |
|---:|---|---|---|
| 00:30 | `MODULE_SLOT_NEAR` | 세 슬롯 중 하나가 단일 latch 대상이 되고 작은 근접 안내 1개가 표시됨 | camera framing, 슬롯 근접 affordance, 1.25u 대상 판정 |
| 00:40 | `MODULE_SLOT_POPUP` | Interact로 연결 슬롯의 작은 상황형 팝업이 열림 | glyph, 대상명, Interact 의미 |
| 00:50 | `MODULE_PREVIEW_OPEN` | `ui.module.expand` Submit으로 접근 슬롯 후보 preview가 열림 | popup 기본 포커스, Submit/Cancel 의미 |
| 01:15 | `MODULE_ALL_SEEN` | 위층·옆방·지하실의 이름·유령·connector·W2/D1을 각각 한 번 이상 봄 | 후보 순환 입력, 선택 인덱스, 카메라 |
| 01:30 | `MODULE_GATE_UNDERSTOOD` | 확정 시도에서 `작업대 필요`와 W2/D1을 보거나 힌트 없이 같은 조건을 정확히 설명 | 실패 이유 우선순위·비용 가독성 |
| 01:30 | `MODULE_FIELD_RETURN` | Cancel했다면 preview→같은 slot popup→같은 현장 순으로 복귀 | 단계별 Cancel, ReturnSnapshot |

90초 안에 실제 작업대 건설이나 방 확정을 요구하지 않는다. 이를 요구하면 시작 D0 때문에 grant 또는 밸런스 변경이 필요해 현재 자연 루프와 모순된다.

90초 이후 자연 연결은 `LOCKED 확인→현장 복귀→수색·귀환→storage.planning에서 기존 작업대 건설→완성된 작업대에 직접 접근·상호작용해 성장 관문을 확인→같은 연결 슬롯으로 복귀→SHORT 또는 READY 확인→확정`이다. 작업대 상호작용은 관문이 해결됐음을 공간적으로 읽게 하지만 방을 원격 확정하지는 않는다. 이 후속에는 새 시간 합격선을 만들지 않는다.

## 3. 상태 전이와 입력 의미

| 상태 ID | 화면·포커스 | 키보드·게임패드 공통 의미 | 전이 |
|---|---|---|---|
| `prompt.far` | prompt 0, `World`, 이동 허용 | Move만 월드 이동. 대상 없는 Interact는 무효 | 1.25u 안 적격 슬롯→`prompt.near` |
| `prompt.near` | 내레이션 아래 prompt 1, `World`, 이동 허용 | Interact는 latch 슬롯 하나만 소비 | Interact→`popup.active`; 이탈→`prompt.far` |
| `popup.active` | 슬롯 대상 작은 팝업, `Popup`, 이동 잠금 | Navigate는 action 포커스, Submit은 preview, root Cancel은 현장 | Submit `ui.module.expand`→`module.preview`; Cancel→`field.return` |
| `module.preview` | 후보 유령+connector+비용/사유 chip, `ModulePreview`, 이동 잠금 | Navigate는 후보 순환, Submit은 확정 검사, Cancel은 한 단계 뒤 | Submit→`module.confirming`; Cancel→`popup.active` |
| `module.confirming` | 입력 1회 잠금, `System` | 선택 후보 snapshot으로 정확한 geometry/economy를 재검사 | 실패→같은 후보 `module.preview`; READY→`field.return` |
| `field.return` | overlay 제거, `World`, 이동 해제 | ReturnSnapshot의 위치·방향·room·target을 복원 | 대상 적격→`prompt.near`; 아니면 `prompt.far` |
| `camp.terminal` | 결과만 표시, `System` | 캠프 입력 소비 금지 | 복귀 없음 |

공통 입력 action은 `Move`, `Interact`, `Navigate`, `Submit`, `Cancel`이다. 문구에 E/A/B/X 같은 물리 키를 굽지 않는다. 키보드↔게임패드 또는 locale 전환은 같은 target ID, candidate ID, reason ID, popup action ID와 ReturnSnapshot을 유지하고 glyph·번역만 다음 렌더 프레임에 바꾼다.

Cancel은 항상 한 단계만 돌아간다. 실패한 Confirm은 Cancel로 취급하지 않고 같은 후보 preview에 남아 원인을 읽고 다른 후보를 볼 수 있게 한다. 성공·실패 어느 쪽도 김씨를 warp하지 않는다.

## 4. 실패 사유 taxonomy

### 4.1 두 판정 축

`geometryStatus`와 `economyStatus`는 별도 필드로 항상 계산한다.

- geometry가 무효면 정확한 geometry 사유 하나를 우선 노출하고 Confirm을 거부한다. W2/D1 비용은 계속 보이지만 작업대/부족 사유를 같은 문장에 이어 붙이지 않는다.
- geometry가 유효할 때만 economy의 `LOCKED`, `SHORT`, `PROTOTYPE_LIMIT`, `READY`를 주 사유로 노출한다.
- `PROTOTYPE_LIMIT`는 이미 성공한 run의 범위 판정이므로 다른 geometry/economy 사유보다 먼저다.
- 기술 guard인 `NOT_PREVIEWING`, `DUPLICATE_SUBMIT`, `COST_UNSET`, transaction rollback은 플레이어가 해결할 온보딩 사유가 아니다. 외부 후보 빌드에서는 발생 0건이어야 한다.

### 4.2 안정 reason ID와 로컬라이제이션 키

KO가 의미 기준 원문이다. EN과 향후 locale은 key·reason ID·token을 유지하고 자연 어순을 소유한다. 현행 런타임의 `module.geometry.*`·`module.economy.*` 키는 호환 alias이며 `interaction.module.*`가 설계 정본이다. 한 상태에서 canonical과 alias를 동시에 표시하지 않는다.

| 축 | reason ID / runtime 상태 | canonical key | KO 기준 | EN 의도 | 현행 alias |
|---|---|---|---|---|---|
| geometry | `geometry.connection` / `NO_CONNECTION_SLOT` | `interaction.module.no_slot` | 연결 슬롯이 맞지 않는다. | No matching connection slot. | `module.geometry.noconnectionslot` |
| geometry | `geometry.connection` / `SLOT_UNAVAILABLE` | `interaction.module.slot_unavailable` | 이 연결 슬롯은 사용할 수 없다. | This connection slot is unavailable. | 없음, 구현 시 canonical 사용 |
| geometry | `geometry.overlap` / `OVERLAP` | `interaction.module.overlap` | 다른 공간과 겹친다. | Overlaps another room. | `module.geometry.overlap` |
| geometry | `geometry.terrain` / `TERRAIN_BLOCKED` | `interaction.module.terrain_blocked` | 이 방향의 지형에는 붙일 수 없다. | The terrain blocks this room. | `module.geometry.terrainblocked` |
| geometry | `geometry.required_path` / `PATH_BLOCKED` | `interaction.module.path_blocked` | 출입구나 필수 통로를 막는다. | Blocks an entrance or required path. | `module.geometry.pathblocked` |
| economy | `economy.workbench_locked` / `LOCKED` | `interaction.module.locked_workbench` | 작업대 필요 | Workbench Required | `module.economy.locked` |
| economy | `economy.resource_short` / `SHORT` | `interaction.module.missing` | `{moduleName} 부족 · {missing}` | Missing for {moduleName} · {missing} | `module.economy.short` |
| economy | `economy.prototype_limit` / `PROTOTYPE_LIMIT` | `interaction.module.prototype_limit` | 첫 확장은 이미 완성했다. | The first expansion is already complete. | `module.economy.prototypelimit` |

연결 family는 reciprocal 정의가 없는 경우와 정의됐지만 비활성·점유된 경우를 geometry 한 묶음으로 기록하되, 화면 문구는 두 원인을 구분한다. `SHORT`의 `{missing}`에는 `나무 {count}`→`표류물 {count}` 순으로 0보다 큰 정확한 부족량만 넣는다. 이미 1회 확정한 뒤에는 자원이 부족해도 `PROTOTYPE_LIMIT`를 먼저 보여 장기 캠페인 최대치로 오해하지 않게 한다.

### 4.3 preview 문구 구조

| key | locale pattern / token | 계약 |
|---|---|---|
| `interaction.structure.prompt` | ko `{inputGlyph} {objectName} {action}` / en `{inputGlyph} {action} {objectName}` | locale이 어순 소유 |
| `structure.module_connector` | `{moduleName} 출입 연결부` / `{moduleName} Entrance Connector` | 접근한 물리 slot의 후보명을 주입 |
| `interaction.action.preview` | `미리보기` / `Preview` | 사용·건설 완료와 혼동 금지 |
| `ui.module.expand` | `방 확장 미리보기` / `Preview Room Expansion` | 작은 slot popup의 단일 action |
| `ui.module.preview.cost` | `{moduleName} · 나무 {wood} · 표류물 {salvage} · {state}` / locale별 동등 pattern | 모든 실패에서도 W2/D1 숨김 금지 |

qps-long 잠금 예문은 `⟦Ûppër Røøm · 2 Wøød · 1 Sålvågë · Wørkbënçh Rëqûïrëd⟧`다. 비용 자원 종류·숫자·주 실패 사유·현재 Confirm/Cancel glyph는 말줄임표로 숨기지 않는다. 공식 영문 게임 제목은 계속 TBD이며 이 문구 계약에서 만들지 않는다.

## 5. 1280×800 정량 수용 기준

| 검증 항목 | PASS |
|---|---|
| 근접 prompt | x=640, `top=max(294,NarrationBottom+12)`, `220..440×40..44px`, 1줄 |
| preview 비용/사유 chip | x=640, 같은 overlay lane, 최대 `440×64px`, 최대 2줄, 본문 18px 이상 |
| UI 개수 | far/popup/preview에서 ContextPrompt 0, near에서 1, preview에서 ModuleCostReasonChip 1 |
| 레이어 | preview에서는 ContextPrompt를 숨기고 비용/사유 chip만 표시. 전역 대시보드 0 |
| 화면 겹침 | NarrationCard와 수직 간격 12px 이상; TopHUD·BottomHelp와 교차 `0px²` |
| 월드 가독성 | chip의 12px 확장 rect와 김씨·선택 connector hotspot·48px 필수 보행 corridor 교차 `0px²` |
| 후보 가시성 | 선택 connector hotspot 100%와 선택 ghost bounds 75% 이상이 화면 안 |
| 텍스트 | ko/en/qps-long에서 clipping·tofu·2줄 초과·자원/숫자/주 사유를 숨기는 ellipsis 각각 0건 |
| 반응 | reason·locale·glyph 변경은 다음 렌더 프레임에 반영, target/candidate/state snapshot 변화 0 |
| latch | 김씨가 3초 정지한 near 상태에서 target 변경 0회 |
| 입력 동등성 | 같은 시작 snapshot의 keyboard/gamepad 경로가 같은 state·candidate·reason·자원 결과를 냄 |

geometry invalid일 때 유령 outline·connector 또는 통로 강조가 원인을 가리키고, 색 외에 canonical 문구를 반드시 함께 쓴다. 이 표는 구현·QA가 나중에 증거를 만들 수 있는 기준이며 이 설계 커밋은 PASS 증거를 주장하지 않는다.

## 6. 첫 사용자 5세션 관찰표

### 6.1 고정 조건과 세션 매트릭스

- 동일 Windows 개발 빌드와 EXE hash, 1280×800, 새 게임, 정확히 90초다.
- 첫 사용자에게 조작법·방 위치·작업대·비용·해결 힌트를 설명하지 않는다.
- 익명 수기 기록만 사용한다. 이름·연락처·직업·나이·음성·영상·화면 녹화·텔레메트리는 남기지 않는다.
- 결과가 들어오기 전 비용·시간·노드·입력·카메라를 바꾸지 않는다.

| 세션 | locale | 장치 | 상태 |
|---|---|---|---|
| `W10-01` | ko | keyboard/mouse | `UNRUN` |
| `W10-02` | en | keyboard/mouse | `UNRUN` |
| `W10-03` | ko | physical gamepad | `UNRUN/UNVERIFIED` |
| `W10-04` | en | physical gamepad | `UNRUN/UNVERIFIED` |
| `W10-05` | ko | keyboard/mouse | `UNRUN` |

### 6.2 참가자별 한 장 기록지

| 관찰 코드 | 최초 시각 | 값·관찰 |
|---|---:|---|
| `MODULE_SLOT_NEAR` | | upper/side/basement, prompt 대상·glyph |
| `MODULE_SLOT_POPUP` | | 원격 시도 횟수, Interact 이해 여부 |
| `MODULE_PREVIEW_OPEN` | | 진입 slot, 기본 candidate, popup 포커스 |
| `MODULE_UPPER_SEEN` | | name/ghost/connector/W2D1 중 놓친 정보 |
| `MODULE_SIDE_SEEN` | | name/ghost/connector/W2D1 중 놓친 정보 |
| `MODULE_BASEMENT_SEEN` | | name/ghost/connector/W2D1 중 놓친 정보 |
| `MODULE_CONFIRM` | | 선택 후보, 표시된 reason ID, 자원 변화 0 여부 |
| `MODULE_CANCEL` | | preview→popup→field 순서와 누른 입력 |
| `MODULE_FIELD_RETURN` | | 같은 position/facing/room/target 여부 |
| `MODULE_GLOBAL_ATTEMPT` | | 전역·원거리 메뉴를 찾은 횟수; 실제 열림 여부 |
| `MODULE_INPUT_MISMATCH` | | glyph와 실제 input 불일치, focus 상실·잠금 |

90초 직후 힌트 없이 세 문항만 묻고 정답 여부를 적는다.

1. “볼 수 있었던 방 방향은 무엇이었나요?” — 위층·옆방·지하실 세 가지.
2. “지금 확정하지 못한 가장 먼저 표시된 이유는 무엇이었나요?” — 작업대 필요.
3. “방 하나의 비용은 무엇이었나요?” — 나무 2와 표류물 1.

### 6.3 코호트 게이트

| 게이트 | 합격선 |
|---|---|
| preview 발견 | `MODULE_PREVIEW_OPEN ≤00:50` 4/5 이상, locale별·장치별 최소 1명 |
| 세 후보 발견 | `MODULE_ALL_SEEN ≤01:15` 4/5 이상, locale별·장치별 최소 1명 |
| 잠금 이해 | `MODULE_GATE_UNDERSTOOD ≤01:30` 4/5 이상 |
| 사후 의미 동일성 | 세 문항 모두 정답 4/5 이상 |
| 현장성 | 실제 원거리/전역 module open·commit 0/5 |
| 안정성 | crash·input lock·상태가 바뀌는 Cancel·중복 prompt·숨은 주 사유 각각 0/5 |

참가자가 전역 메뉴를 찾으려 한 행동은 발견성 마찰로 기록하되 시스템이 실제 메뉴를 열지 않으면 개인 세션을 무효로 만들지 않는다. 같은 마찰이 2명 이상이면 비용을 바꾸지 않고 슬롯 framing→prompt→popup focus 순으로 P1 UX 검토를 연다.

즉시 중단 조건은 개인정보·녹화 사고, 서로 다른 빌드/해상도 혼합, 두 세션의 힌트 오염, 두 세션에서 반복되는 crash/input lock, 한 세션이라도 실제 원거리 module commit 가능이다. 수정 빌드를 쓰면 기존 결과와 섞지 않고 5세션을 다시 시작한다.

## 7. 구현 시 주의와 현재 차이

- 현행 `BeginPreview()`가 항상 Upper로 시작한다면 slot 진입 ID에 대응하는 candidate로 초기화하도록 구현해야 한다. 후보 순환 순서는 기존 upper→side→basement를 유지한다.
- 현행 `InvalidGeometry` generic commit 문구만 보여 주지 말고 `CampModuleEvaluation.Geometry`의 정확한 connection/overlap/terrain/path reason ID를 preview에 유지한다.
- 현행 `CostUnset`은 개발 guard다. 외부 후보 빌드에서 플레이어용 여덟 번째 실패 원인으로 노출하거나 무료 fallback하지 않는다.
- 현행 String Table의 `module.geometry.*`·`module.economy.*`는 이 문서 표의 canonical key로 일대일 adapter할 수 있다. 저장·판정은 enum/reason ID로 하고 번역문을 비교하지 않는다.
- Workbench는 unlock이지 원거리 commit terminal이 아니다. 성공 transaction은 선택한 연결 slot의 snapshot과 room graph를 사용한다.
- physical gamepad와 qps-long의 실제 PASS는 증거 전까지 `UNVERIFIED`다.

## 8. 열린 질문과 제외

### 열린 질문

1. 최종 1280×800 camera framing에서 세 기존 슬롯 중 어느 것이 첫 사용자에게 가장 먼저 보이는지는 실제 5세션 전 미확정이다. 계약은 어느 슬롯으로 시작해도 같은 후보 순환과 결과를 요구하며 새 아트를 채택하지 않는다.
2. `W10-03/04` 실기 전 physical gamepad glyph·focus parity는 `UNVERIFIED`다.
3. 실제 qps-long String Table은 아직 없으므로 이 문서는 레이아웃 기준만 고정하고 구현 PASS를 주장하지 않는다.

### 범위 밖

새 자원·비용, W2/D1 변경, 두 번째 모듈, 새 생산·저장·휴식 효과, 전역 메뉴, 원거리 확정, 튜토리얼 카드, 아트 채택, Runtime·String Table·QA 코드 변경, 텔레메트리, 참가자 섭외와 실제 결과 작성은 모두 제외한다.

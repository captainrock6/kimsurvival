# 《김씨 생존기: 무인도》 통합 프로토타입 계약 감사

- 상태: 설계 감사 완료, 런타임 migration 미적용
- 기준 커밋: `e695c36d4a0a15c19f25630fe177fdd56298c1d6`
- 현지화 정본: `vertical-slice-localization.md`
- 밸런스 정본: `vertical-slice-balance.md`
- 공식 영문 게임 제목: **미정(TBD)**

이 문서는 통합 프로토타입의 코드와 기존 기획 계약을 같은 기준에서 대조한 설계 산출물이다. Unity 코드, String Table, 아트 레지스트리와 QA 증거는 수정하지 않는다. 아래 canonical 키는 다음 현지화 migration의 목표이며 현재 런타임 118개 키가 이미 바뀌었다는 뜻이 아니다.

## 1. 확정 결론

- 기존 기획 계약은 **123개 모두 unique**다. Unity TSV, Shared Data, `ko`, `en` String Table은 각각 **118개이며 ID·키 중복 0개**다.
- 118개 런타임 키는 현재 코드 경로에서 모두 참조된다. 미사용 런타임 키는 0개다. `dev.fallback_probe`의 영어 빈칸 1개는 한국어 fallback을 검증하기 위한 의도된 개발 전용 레코드다.
- 이름까지 정확히 같은 키는 12개뿐이다. 나머지는 86개가 기존 계약 의미로 rename·merge·분해되어야 하고, 19개 런타임 키가 기존 계약에 없던 15개 의미로 합쳐진다. 개발 전용 1개는 플레이어 canonical 목록에서 제외한다.
- canonical 제안은 **기존 123개를 보존 + 새 의미 15개 추가 = 138개**다. runtime별 concrete 버튼과 복합 HUD 문자열을 canonical로 승격하지 않고, 명명 placeholder가 있는 재사용 가능한 계약으로 수렴한다.
- 42개 김씨 독백·상황 문구는 런타임에 1:1로 존재하며 현재 ko/en 길이는 기존 권장 상한을 넘지 않는다. 다만 위치 placeholder 23개, 한국어 조사 결합 4개, 일부 영어 코미디 의도 이탈은 migration 전에 고쳐야 한다.
- 현행 자원 경제는 숙련 **2회 원정 구조**, 일반 **3회 원정 구조**, 욕심 탈진과 3일 기한 실패를 모두 허용한다. 다만 자동 플레이는 이동을 순간이동으로 대체하므로 실제 이동 체력·일광과 20분 인간 플레이는 아직 입증하지 못했다.

## 2. 감사 대상과 신뢰 범위

| 대상 | 확인 결과 | 이 감사에서의 용도 |
|---|---|---|
| `Docs/Design/vertical-slice-localization.md` | 기존 123개 unique, 독백·상황 42개 | 의미·톤·길이·직역 금지 정본 |
| `PrototypeStrings.tsv` | 118행, 중복 0, ko 빈칸 0, en 빈칸 1 | 현재 런타임 문자열 원본 |
| Unity Shared Data / ko / en table | 각각 118 entry | TSV와 실제 String Table 수 일치 확인 |
| `PrototypeLocalization.cs`와 Runtime 호출부 | table 118개 모두 참조 | 미사용·누락 호출 확인 |
| `GameSession.cs` | 시작값, 비용, 가방, 체력·일광, 허기·회복 | 현행 수치 정본 |
| `KimSurvivalPrototype.cs`, `PrototypePlayerTraversal.cs` | 노드 10개, 위치, 장벽, 이동 속도 | 원정 경로와 실제 거리 계산 |
| `Artifacts/ParallelQA/.../playmode-full-loop.txt` | baseline `2a6e9e6`, 3일 성공 | 과거 자연 루프 증거; 현재 HEAD timing 증거로 사용하지 않음 |
| `Artifacts/Verification/*checks.txt` | `e695c36` 통합 검증 커밋에 포함 | 현재 기능 연결·결말 검증; 자원 grant 사용 경로와 분리 |

Parallel QA의 3일 경로는 `Grant` 없이 자원 경제를 통과했지만 `GatherAt`이 좌표를 직접 바꾸고 `TickSearch`를 호출하지 않는다. 현재 소스에는 그 runner가 찾는 예전 private field와 method도 남아 있지 않다. 따라서 해당 결과는 경제 순서의 참고 증거일 뿐 현재 HEAD의 재현 가능한 이동·시간·생존 비용 증거가 아니다.

## 3. 123 대 118 현지화 대조

### 3.1 누락·중복·미사용 요약

| 검사 | 수치 | 판정 |
|---|---:|---|
| 기획 계약 key / unique | 123 / 123 | 중복 0 |
| TSV / Shared / ko / en entry | 118 / 118 / 118 / 118 | 네 원본의 수 일치 |
| 런타임 key unique | 118 | 중복 0 |
| 런타임에서 참조되는 table key | 118 | 미사용 0 |
| 런타임 호출인데 table에 없는 key | 0 | 런타임 누락 0 |
| 이름까지 정확히 같은 교집합 | 12 | 계약과 구현의 naming drift가 큼 |
| 계약에만 있는 exact key | 111 | 대부분 runtime alias·복합 문자열·예약 키 |
| 런타임에만 있는 exact key | 106 | 대부분 canonical rename 대상 |
| 빈 locale 값 | en 1 | `dev.fallback_probe`, 의도된 ko fallback 검사 |

`game.title`은 런타임 table에 없지만 한국어 `PlayerSettings.productName`만 존재하며 공식 영어 제목을 임의로 만든 흔적은 없다. canonical에서는 `game.title/en = TBD`를 계속 blocked로 둔다.

### 3.2 계약 exact 누락의 성격

| 성격 | 대표 key | 처리 |
|---|---|---|
| 의도된 blocked·예약 | `game.title`, `settings.*`, `ui.common.confirm/cancel/back/quit` | canonical 유지. 화면이 생길 때 같은 key를 사용 |
| runtime 복합 HUD가 흡수 | `ui.status.*`, `ui.resource.amount`, `ui.signal.progress` | `hud.status.*`, `hud.resources`를 UI layout과 atomic key로 분해 |
| runtime concrete 버튼이 흡수 | `ui.action.*`, `item.*.name`, `structure.*.name` | 항목별 `button.*`를 generic action template + 명칭 key로 합침 |
| runtime alias로 존재 | `message.run.start`, `result.rescued.title`, `interaction.rope_required` 등 | 아래 migration 규칙으로 이름만 수렴 |
| 기능도 현재 미충족 | `interaction.structure.approach` | 캠프 설비가 전역 버튼으로 작동하므로 spatial-use 수용 조건과 함께 구현 필요 |

예약 키는 “미사용이므로 삭제”하지 않는다. 설정·공통 UI가 생겼을 때 새 이름을 만들지 않게 보존하는 계약이다.

### 3.3 이름·구성 불일치 migration 규칙

| 현재 runtime 패턴 | canonical 패턴 | 처리 원칙 |
|---|---|---|
| `ui.camp.title`, `ui.restart` | `ui.camp.actions_title`, `ui.common.restart` | 화면명보다 의미·재사용 범위 우선 |
| `phase.*` | `ui.phase.*` | UI 상태 표시라는 domain을 명시 |
| `bag.*` | `ui.inventory.*` | 용어를 inventory로 통일 |
| `button.<subject>.*` | `ui.action.*` + `item/structure/resource.*.name` | concrete 중복을 template로 축소 |
| `device.*`, `value.*` | `controls.device.*`, `ui.common.*` | 소유 domain으로 이동 |
| `resource.wood` 등 | `resource.wood.name` 등 | 모든 명칭 key에 `.name` 사용 |
| `structure.campfire` 등 | `structure.campfire.name` 등 | 설비 명칭 규칙 통일 |
| `hud.status.camp/exploring` | `ui.status.*` + `ui.phase.*` | 문장 한 덩어리 대신 UI 요소별 번역·배치 |
| `hud.resources` | `resource.*.name` + `ui.resource.amount` + `ui.signal.progress` | 언어별 어순과 줄바꿈을 layout에서 제어 |
| `world.rope.need/pass`, `world.return` | `interaction.rope_required/open`, `interaction.return_to_camp` | 월드 위치가 아니라 행동 의미 유지 |
| `placement.outside/overlap/path` | `interaction.placement.invalid_zone/overlap/blocked_path` | 기존 계약 이름으로 수렴 |
| `message.reset`, `message.search.*`, `message.bag.*` | `message.run.*`, `message.expedition.*`, `message.inventory.*` | 상태 주체를 정확히 명명 |
| `result.title.*`, `result.detail.*` | `result.<outcome>.title/detail` | 결과 주체를 먼저 두는 순서로 통일 |
| `dev.fallback_probe` | canonical 제외 | 개발·QA namespace에 유지 가능 |

현재 118개 중 12개는 그대로 유지하고, 86개는 위 기존 canonical key로 migration한다. 기존 계약이 표현하지 못한 19개 runtime key는 아래 15개 canonical 레코드로 수렴한다.

| 새 canonical key | 현재 runtime alias | 필요한 이유 |
|---|---|---|
| `settings.language.switch` | `ui.language.switch.ko`, `.en` | 현재·다음 locale을 placeholder로 합침 |
| `controls.placement` | 동일 | 배치 모드 장치 공통 안내 |
| `ui.action.relocate_free` | `button.campfire/workbench/rain.relocate` | 설비 3종 concrete 중복 제거 |
| `structure.generic.name` | `structure.generic` | 잘못된 enum의 안전한 fallback |
| `world.build_zone.general_ground` | `world.build_zone` | 호환 건설 구역을 월드에 표시 |
| `world.keep_clear.entrance` | `world.entrance` | 출입구 보호 라벨 |
| `world.keep_clear.required_path` | `world.required_path` | 필수 동선 보호 라벨 |
| `world.signal_anchor.progress` | `world.signal_anchor` | 고정 앵커와 단계 동시 표시 |
| `world.structure.relocate_free` | `world.structure.relocate` | 설치물의 재배치 가능 라벨 |
| `interaction.placement.valid` | `world.placement.valid` | 일반 유효 상태 |
| `interaction.placement.invalid` | `world.placement.invalid`, `placement.invalid` | 일반 무효 상태 두 key 통합 |
| `character.kim.name` | `world.kim` | 캐릭터 이름을 위치와 분리 |
| `interaction.placement.valid_build` | `placement.valid.build` | 신규 배치 유효 안내 |
| `interaction.placement.valid_move` | `placement.valid.relocate` | 재배치 유효 안내 |
| `interaction.placement.blocked_entrance` | `placement.entrance` | 출입구 차단 원인 |

15개 레코드의 ko/en 문안, 사용 상황, 의도, 길이와 직역 금지 메모는 `vertical-slice-localization.md` 8절이 정본이다.

### 3.4 문안·placeholder 정합성

| 항목 | 현재 결과 | canonical 판정 |
|---|---|---|
| 독백·상황 coverage | 42/42 | 유지 |
| 기존 Max 초과 | ko 0, en 0 | 길이는 통과하나 실제 1280×800 layout은 별도 검증 |
| 월드 라벨 가독성 | Parallel QA가 1280×800 환산 약 1.4 px로 시각 실패 기록 | ko/en 모두 실제 캡처에서 읽히는 크기로 재검증 |
| 위치 placeholder | 23 key가 `{0}`, `{1}` 형식 | `{resource}`, `{day}`, `{device}` 등 명명 placeholder로 migration |
| 한국어 조사 결합 | 4 key가 `을(를)` 또는 `은(는)` 사용 | locale별 완성형 문장으로 교체 |
| 가방 교체 정보 | `message.bag.replace`가 버린 자원만 받음 | canonical `message.inventory.replace`의 `{discarded}`, `{resource}` 모두 필요 |
| 상태 용어 | 런타임 en HUD가 `Energy`, 계약은 `Stamina` | 행동 체력 의미인 `Stamina`로 통일하거나 용어 결정을 별도 승인 |
| 입력 안내 | `controls.*` 본문에 `A/D`, `Space`, `F1` 등이 직접 들어감 | `{move}`, `{jump}`, `{language}` 같은 action placeholder와 장치 glyph 사용 |
| 탈진 제목 영어 | `Mr. Kim Needs a Lie-Down` | `result.exhausted.title`의 “lies down 직역 금지”와 충돌; 승인 문안으로 복귀 |
| 코미디 의도 | 일부 영어가 기능문으로 축약 | `message.research.rope_success`, `message.gather.no_target` 등은 intent와 transcreation note 기준 재검토 |

조사 결합 4개는 `message.gather.water`, `message.gather.land`, `message.bag.replace`, `message.bag.discard`다. canonical 문안은 한국어 조사를 자원명 뒤에 붙이지 않으며 향후 `es`, `ja`, `zh-Hans`, `zh-Hant`도 같은 명명 placeholder와 메타데이터를 사용한다.

## 4. canonical key 목록 138개

아래가 하나의 canonical 제안 목록이다. 기존 123개와 새 15개를 합친 것이며 locale별 파생 key를 만들지 않는다.

### `character` 1

`character.kim.name`

### `controls` 5

`controls.camp`, `controls.device.gamepad`, `controls.device.keyboard_mouse`, `controls.explore`, `controls.placement`

### `game` 1

`game.title`

### `interaction` 19

`interaction.gather`, `interaction.placement.blocked_entrance`, `interaction.placement.blocked_path`, `interaction.placement.confirm`, `interaction.placement.fixed_anchor`, `interaction.placement.invalid`, `interaction.placement.invalid_zone`, `interaction.placement.move`, `interaction.placement.move_free`, `interaction.placement.overlap`, `interaction.placement.place`, `interaction.placement.valid`, `interaction.placement.valid_build`, `interaction.placement.valid_move`, `interaction.return_to_camp`, `interaction.rope_open`, `interaction.rope_required`, `interaction.structure.approach`, `interaction.water_search`

### `item` 2

`item.rope.name`, `item.stone_axe.name`

### `message` 39

`message.build.campfire_success`, `message.build.duplicate`, `message.build.insufficient`, `message.build.rain_success`, `message.build.workbench_success`, `message.craft.axe_success`, `message.craft.rope_success`, `message.craft.unavailable`, `message.day.search_required`, `message.day.start`, `message.expedition.already_done`, `message.expedition.start`, `message.expedition.unavailable`, `message.explore.rope_barrier`, `message.food.eaten`, `message.food.full`, `message.food.none`, `message.gather.axe_wood`, `message.gather.land_success`, `message.gather.no_target`, `message.gather.water_requires_swim`, `message.gather.water_success`, `message.inventory.discard_pending`, `message.inventory.full`, `message.inventory.replace`, `message.inventory.replace_empty`, `message.research.axe_success`, `message.research.rope_success`, `message.research.unavailable`, `message.research.workbench_required`, `message.return.forced`, `message.return.safe`, `message.run.start`, `message.signal.materials_required`, `message.signal.rope_required`, `message.signal.stage1_success`, `message.signal.stage2_success`, `message.swim.enter`, `message.swim.exit`

### `resource` 4

`resource.food.name`, `resource.salvage.name`, `resource.stone.name`, `resource.wood.name`

### `result` 6

`result.deadline.detail`, `result.deadline.title`, `result.exhausted.detail`, `result.exhausted.title`, `result.rescued.detail`, `result.rescued.title`

### `settings` 13

`settings.apply`, `settings.controls`, `settings.fullscreen`, `settings.language`, `settings.language.option.chinese_simplified`, `settings.language.option.chinese_traditional`, `settings.language.option.english`, `settings.language.option.japanese`, `settings.language.option.korean`, `settings.language.option.spanish`, `settings.language.switch`, `settings.master_volume`, `settings.title`

### `structure` 5

`structure.campfire.name`, `structure.generic.name`, `structure.rain_collector.name`, `structure.signal_tower.name`, `structure.workbench.name`

### `ui` 38

`ui.action.build_with_cost`, `ui.action.completed`, `ui.action.craft_with_cost`, `ui.action.eat_with_count`, `ui.action.expedition_start`, `ui.action.final_day_settle`, `ui.action.item_owned`, `ui.action.next_day`, `ui.action.relocate_free`, `ui.action.research_with_cost`, `ui.action.researched`, `ui.action.signal_complete`, `ui.action.signal_with_cost`, `ui.camp.actions_title`, `ui.common.back`, `ui.common.cancel`, `ui.common.confirm`, `ui.common.none`, `ui.common.owned`, `ui.common.quit`, `ui.common.restart`, `ui.cost.resource_amount`, `ui.inventory.camp_storage`, `ui.inventory.expedition`, `ui.inventory.full`, `ui.inventory.slot_empty`, `ui.inventory.slot_item`, `ui.phase.camp_prepare`, `ui.phase.island_search`, `ui.phase.post_return`, `ui.phase.result`, `ui.phase.shallow_swim`, `ui.resource.amount`, `ui.signal.progress`, `ui.status.day`, `ui.status.daylight`, `ui.status.hunger`, `ui.status.stamina`

### `world` 5

`world.build_zone.general_ground`, `world.keep_clear.entrance`, `world.keep_clear.required_path`, `world.signal_anchor.progress`, `world.structure.relocate_free`

합계는 `1+5+1+19+2+39+4+6+13+5+38+5 = 138`이다.

## 5. 현행 3일 경제 재검토

### 5.1 코드에서 확인한 현행 핵심값

| 축 | 현행 `e695c36` |
|---|---|
| 시작 | `W2/S1/F1/D0`, 허기 75, 체력 100, 일광 100 |
| 가방 | 4칸, 동종 중첩 2, 최대 8, 도구 점유 0 |
| 노드 | W 2, S 2, F 2, D 4; 모두 기본 2, 총 10개/20자원 |
| 밧줄 전/후 | 육상 4 + 수상 2 / 후반 육상 4 추가 |
| 도끼 | 나무 노드만 2→3 |
| 구조 필수 누적 비용 | 작업대+밧줄 연구·제작+신호 2단계 = `W7/D7` |
| 이동 | 육상 4.20 unit/s, 수영 2.65 unit/s |
| 초당 소모 | 육상 이동 `E0.18/L0.75`, 수영 이동 `E0.65/L1.15`, 수영 정지 `E0.22/L1.15` |
| 채집 | 육상 E6, 수상 E9, 별도 일광 고정비 없음 |
| 하루 | 허기 -25, 식량 +35, 허기 0이면 E-35 후 회복 |
| 회복 | 기본 +20, 모닥불이면 총 +38, 빗물받이 +10 |
| 결말 | 체력 0 즉시 탈진, 일몰은 E-22 후 강제 귀환, 3일차 정산 미완성은 기한 실패 |

모든 제작·건설·연구·구조 비용의 상세 표는 `vertical-slice-balance.md` 4절을 따른다. 통합 뒤 비용 자체는 바뀌지 않았다.

### 5.2 구조 단계 목표와 현재 판정

| 목표 | 이상적 최소 누적 원정 | 목표 평균 누적 원정 | 현재 판정 |
|---|---:|---:|---|
| 신호 1단계 | 1 | 2 | 첫 가방 `W2/D4`면 작업대 뒤 1단계 가능 |
| 신호 2단계 | 2 | 3 | 필수 채집 12가 1회 운반 8을 넘고, 2회 운반 16 안에는 들어옴 |

신호 비용을 올리면 2회 숙련 경로가 쉽게 깨진다. 첫 인간 플레이 데이터가 나오기 전에는 가방, 노드, 작업대·밧줄, 신호 `W2/D2`×2를 동결한다.

## 6. 세 경로 계산

### 6.1 최소 진행: 2회 원정 성공

| 시점 | 창고·가방 | 행동·잔여 |
|---|---|---|
| 시작 | 창고 `W2/S1/F1/D0` | E100/H75 |
| 1일차 | 가방 `W2/D4/F2` | 귀환 `W4/S1/F3/D4` |
| 1일차 성장 | 작업대, 밧줄 연구·제작 | `W1/S1/F3/D1`; 다음 원정에서 후반 숲 개방 |
| 2일차 | 가방 `W4/D4` | 귀환 `W5/S1/F3/D5` |
| 2일차 성장 | 신호 1·2단계 | `W1/S1/F3/D1`, 즉시 구조 성공 |

노드 좌표를 실제 속도로 최단 연결하면 1일차 이동은 육상 약 12.2 unit, 수영 약 8.0 unit이다. 이동 소모 약 `E2.5/L5.7`에 채집 `E30`을 더해도 귀환 E는 약 67.5다. 2일차는 육상 4회 채집과 짧은 이동으로 충분하다. 수치상 2회 `수색→귀환→성장` 뒤 구조가 확실히 가능하다.

### 6.2 평균 진행: 현재 자동 경로의 3회 원정 성공

| 일차 | 운반·투자 | 귀환/정산 결과 |
|---|---|---|
| 1 | `W2/S2/D4`; 작업대, 밧줄·돌도끼 연구와 제작 | 창고 `W0/S1/F1/D0`, 이동을 제외하면 E93/H50 |
| 2 | 도끼 나무 2노드와 D 2노드, overflow 교체 뒤 `W4/D4` | 창고 `W4/S1/F1/D4`, 이동을 제외하면 E86/H25 |
| 3 | overflow 교체 뒤 `W4/S2/D2` | 창고 `W8/S3/F1/D6`; 모닥불·빗물받이·신호 2단계 후 `W0/S1/F1/D1`, 구조 |

이 경로는 가방 교체, 작업대, 두 연구·도구, 후반 숲, 선택 설비와 신호를 모두 거쳐 성장 폭은 충분하다. 그러나 자동 경로는 좌표 warp 때문에 표의 E에 이동 비용이 없고, 구조 직전에 선택 설비를 지어 회복 기능을 실제로 쓰지 않는다. 따라서 “평균 인간 경로”가 아니라 경제 coverage 경로로만 취급한다.

### 6.3 욕심내다 실패

1일차에 육상 60초, 수영 34초, 육상 4회, 수상 2회를 수색하면 `E74.9/L84.1`을 쓰고 E25.1에 귀환한다. 모닥불 없이 기본 회복을 받으면 2일차 E45.1이다. 2일차에 수영 이동 42초와 수상 2회를 다시 감행하면 `E27.3+18=45.3`을 써 일광이 남아 있어도 탈진한다. 별도로 아무 신호도 짓지 않고 매일 즉시 귀환·정산하면 3일차에 기한 실패한다.

결론은 같은 3일 규칙 안에서 성공과 두 실패가 모두 가능하다는 것이다. 다만 실패가 일반 플레이에서도 충분히 발생하는지는 자동화가 아니라 인간 플레이 분포로 확인해야 한다.

## 7. 추천 수치 변경과 기대 효과

이번 작업에서는 런타임을 바꾸지 않는다. 아래는 적용 순서이며 한 단계씩 적용한 뒤 다시 측정한다.

| 우선순위 | 변경 제안 | 기대 효과 | 보호 조건 |
|---:|---|---|---|
| 1 | 시작 식량 `1→0`, 시작 허기 `75→70`, 하루 허기 `-25→-35` | 3회 경로가 2일차 전에 식량을 선택하게 하고, 현재 결말 뒤에야 오는 허기 페널티를 실제 원정 압박으로 이동 | 2회 최단 구조는 `F2`를 함께 가져오면 유지 |
| 2 | 인간 플레이에서 수영 위험 인지가 약할 때만 수상 채집 E `9→12` | 수상 2노드 선택에 E+6을 더해 육상과 수영의 체감 차이를 분명히 함 | 2회 최단의 1일차 귀환 E 60 이상 유지 |
| 3 | 중앙 경로 귀환 일광 중앙값이 60 초과일 때만 육상 `L0.75→0.90/s`, 수영 `L1.15→1.35/s` | 맵 길이나 자원 비용을 부풀리지 않고 귀환 결정을 앞당김 | 일몰 강제 귀환율 20~35%, 최단 경로 L 20 이상 유지 |

가방 4×2, 노드 수·획득량, 작업대·밧줄과 신호 비용은 우선 동결한다. 이 값들이 현재 “1회 불가·2회 숙련 가능·3회 일반 가능”을 만드는 임계값이기 때문이다. 빗물받이는 현재 3일 경로에서 열세 선택이지만, 사용률을 측정하기 전에 회복·비용을 동시에 바꾸지 않는다.

## 8. 플레이테스트 기록과 조정 순서

| 순서 | 기록 지표 | 통과 기준 |
|---:|---|---|
| 1 | 원정별 실제 시간, 귀환 E/L, 강제 귀환·탈진 원인 | 20분 이내 결말, 중앙 귀환 E25~60/L20~60 |
| 2 | 원정별 가방 구성, full·교체·포기 횟수 | 첫 플레이에서 4칸 도달과 실제 교체/포기 각 1회 이상 |
| 3 | 작업대·밧줄·신호 1/2단계 원정 번호 | 숙련 1/2, 중앙 2/3 |
| 4 | 식량 사용 시점, 2일차 허기, 선택 설비 사용 횟수 | v0.2에서 3회 경로의 50~80%가 2일차 전 식사 |
| 5 | ko/en key 노출·fallback·잘림, 다음 행동/비용/실패 원인 설명 | 플레이어 문구 fallback 0, 잘림 0, 의미 불일치 0 |

배치 플레이테스트에는 별도로 일반 설비의 실제 접근·사용 여부를 기록한다. 현재 구현은 자유 배치와 무비용 재배치는 제공하지만 캠프 행동 버튼이 김씨의 위치와 무관하게 실행되므로, `interaction.structure.approach`와 “직접 다가가 사용하는 공간형 거점” 계약은 아직 충족하지 않는다. 이 차이를 수치 조정으로 가리지 않는다.

## 9. 남은 위험

- 인간 20분 완료 시간은 아직 측정되지 않았다. 자동 실행 0.17초와 warp 경로는 duration 근거가 아니다.
- 현행 허기는 성공 전에 식량을 요구하지 않아 3일 생존 압박의 한 축이 비어 있다.
- current String Table을 canonical 138개로 migration할 때 alias를 한 번에 제거하면 저장 데이터나 참조가 끊길 수 있다. 기존 runtime key를 한 버전 deprecated alias로 유지한 뒤 삭제한다.
- 복합 HUD를 atomic key로 나누려면 UI layout 변경이 필요하다. 영어·es의 길이를 감당하도록 문자열 migration과 layout migration을 같은 검증 단위로 묶는다.
- 42개 문안이 글자 수 상한 안이어도 1280×800 월드 라벨의 실제 글자 크기가 부족할 수 있다. 길이 검사와 시각 가독성 검사를 별개로 통과해야 한다.
- 공식 영어 제목은 사용자 결정 전까지 계속 TBD다. `Kim Survival`, `Mr. Kim's ...` 같은 내부 namespace를 제품명으로 승격하지 않는다.
- 깊은 잠수, 전투, 상점, 여러 섬과 장기 캠페인은 이 감사와 canonical 목록에 추가하지 않는다.

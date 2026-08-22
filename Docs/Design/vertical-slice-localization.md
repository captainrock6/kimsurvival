# 《김씨 생존기: 무인도》 수직 슬라이스 현지화 명세

- 상태: 기획 기준 `localization v0.1`
- 기준 커밋: `f60a456698b327209607254f9d2fb50cfa7eb6ed`
- 기준 원문: 한국어 `ko`
- 첫 지원 언어: 영어 `en`
- 후속 언어: 스페인어 `es`, 일본어 `ja`, 중국어 간체 `zh-Hans`, 중국어 번체 `zh-Hant`
- 공식 영문 게임 제목: **미정(TBD)**. 별도 사용자 결정 전 번역·음역·스토어 표기를 확정하지 않는다.

이 문서는 현재 3일 수직 슬라이스에서 플레이어에게 보이는 문자열의 기획 정본이다. 한국어가 의미, 정보 우선순위, 김씨의 코미디 톤을 판정하는 기준 원문이며 영어는 한국어를 직역하지 않고 같은 상황·행동 유도·웃음의 기능을 재현한다.

## 1. 안정적인 문자열 키 체계

### 1.1 키 형식

형식은 `<domain>.<area>.<subject>.<state>`다.

- 영문 소문자 ASCII, 숫자, 밑줄과 점만 사용한다: `^[a-z][a-z0-9_]*(\.[a-z0-9_]+)+$`.
- 언어 코드는 키에 넣지 않는다. `message.swim.enter` 하나가 모든 locale에서 동일하다.
- 문구가 바뀌어도 의미가 같으면 키를 바꾸지 않는다. 의미가 달라질 때만 새 키를 만들고 기존 키는 `deprecated`로 남긴다.
- 화면 위치나 구현 클래스명을 키에 넣지 않는다. UI 이동이나 코드 리팩터링이 번역 자산을 깨뜨리지 않게 한다.
- 숫자·자원명·키 입력은 문자열 결합 대신 `{day}`, `{resource}`, `{count}`, `{stage}`, `{input}` 같은 명명 placeholder를 사용한다.
- 모든 locale은 같은 placeholder 이름·개수·자료형을 유지하되 어순은 자유롭게 바꿀 수 있다.

권장 domain:

| Domain | 책임 | 예시 |
|---|---|---|
| `game` | 제목과 제품 메타 | `game.title` |
| `ui` | HUD, 메뉴, 버튼, 가방 | `ui.inventory.full` |
| `settings` | 설정 화면 | `settings.language` |
| `resource` | 자원 이름 | `resource.salvage.name` |
| `item` | 도구·소모품 이름 | `item.stone_axe.name` |
| `structure` | 설비 이름 | `structure.workbench.name` |
| `interaction` | 행동 안내와 배치 피드백 | `interaction.placement.invalid_zone` |
| `controls` | 장치별 조작 안내 | `controls.explore` |
| `message` | 김씨 독백·상황 피드백 | `message.return.forced` |
| `result` | 성공·실패 제목과 설명 | `result.exhausted.title` |

### 1.2 문자열 레코드 메타데이터

향후 모든 언어는 다음 필드를 공유한다.

| 필드 | 규칙 |
|---|---|
| `key` | 불변 문자열 키 |
| `source_locale` | 항상 `ko` |
| `context` | 발생 화면·상태·사용 조건 |
| `intent` | 플레이어가 이해하거나 느껴야 하는 것 |
| `ko`, `en`, 이후 locale | locale별 실제 문안 |
| `max_chars` | locale별 공백 포함 권장 상한. placeholder 치환 뒤 측정 |
| `placeholders` | 이름, 자료형, 예시 값 |
| `tone` | `functional`, `dry_comedy`, `warning`, `outcome` 중 하나 이상 |
| `transcreation_note` | 직역 금지 지점, 살려야 할 농담과 금지 해석 |
| `status` | `approved`, `draft`, `blocked` |

## 2. 번역·톤 규칙

- `김씨`는 영어 대사에서 기본적으로 `Mr. Kim`이다. 영웅명이나 이름처럼 취급하지 않고 평범한 사람의 거리감을 유지한다.
- 코미디는 건조한 자기평가, 엉성한 공학, 과한 진지함과 사소한 결과의 대비에서 만든다. 밈, 유행어, 특정 작품 인용을 추가하지 않는다.
- 위험 경고는 농담보다 행동과 원인을 먼저 전달한다. 영어 문안이 웃기더라도 필요한 자원·실패 원인·다음 행동을 삭제하지 않는다.
- 한국어 조사 결합(`을(를)`)을 다른 언어에 노출하지 않는다. 자원명이 들어가는 문장은 locale별 완성형 template로 번역한다.
- UI는 명사와 동사를 짧게 유지한다. 영어에서 한국어보다 길어질 때 글자 크기를 줄이기보다 짧은 자연어를 우선한다.
- 공식 영문 제목은 `game.title`의 `en`을 `TBD`로 유지한다. `Mr. Kim's ...`, `Kim Survival ...` 같은 임시 제목도 공식 제목으로 사용하지 않는다.

## 3. 공통 UI·설정 문안

`Max`는 `ko/en` 공백 포함 권장 글자 수다. `TBD`는 번역 승인 전 제품 제목으로 노출할 수 없다는 차단 상태다.

### 3.1 제품·공통·설정

| Key | 사용 상황 | ko | en | Max ko/en | 상태·메모 |
|---|---|---|---|---:|---|
| `game.title` | 타이틀·창 제목 | 김씨 생존기: 무인도 | TBD | 20/40 | `en` blocked; 공식 영문 제목 결정 필요 |
| `ui.common.confirm` | 공통 확인 | 확인 | Confirm | 6/12 | approved |
| `ui.common.cancel` | 공통 취소 | 취소 | Cancel | 6/12 | approved |
| `ui.common.back` | 이전 화면 | 뒤로 | Back | 6/12 | approved |
| `ui.common.restart` | 결과 화면 | 다시 시작 | Restart | 10/16 | approved |
| `ui.common.quit` | 종료 행동 | 종료 | Quit | 6/12 | approved |
| `ui.common.owned` | 도구 보유 상태 | 보유 | Owned | 6/12 | approved |
| `ui.common.none` | 미보유 상태 | 없음 | None | 6/12 | approved |
| `settings.title` | 설정 제목 | 설정 | Settings | 8/16 | 예약; 현재 구현 없음 |
| `settings.language` | 언어 선택 | 언어 | Language | 8/16 | 예약 |
| `settings.language.option.korean` | 언어 옵션 | 한국어 | Korean | 10/16 | `ko` 지원 |
| `settings.language.option.english` | 언어 옵션 | 영어 | English | 10/16 | `en` 지원 |
| `settings.language.option.spanish` | 후속 언어 옵션 | 스페인어 | Spanish | 12/16 | `es` future, 비활성 |
| `settings.language.option.japanese` | 후속 언어 옵션 | 일본어 | Japanese | 12/16 | `ja` future, 비활성 |
| `settings.language.option.chinese_simplified` | 후속 언어 옵션 | 중국어(간체) | Chinese (Simplified) | 16/24 | `zh-Hans` future, 비활성 |
| `settings.language.option.chinese_traditional` | 후속 언어 옵션 | 중국어(번체) | Chinese (Traditional) | 16/24 | `zh-Hant` future, 비활성 |
| `settings.master_volume` | 음량 설정 | 전체 음량 | Master Volume | 12/20 | 예약 |
| `settings.fullscreen` | 화면 설정 | 전체 화면 | Fullscreen | 12/16 | 예약 |
| `settings.controls` | 조작 안내 진입 | 조작 | Controls | 8/16 | 예약 |
| `settings.apply` | 설정 적용 | 적용 | Apply | 6/12 | 예약 |

### 3.2 상태·자원·아이템·설비

| Key | 사용 상황 | ko | en | Max ko/en | Placeholder/메모 |
|---|---|---|---|---:|---|
| `ui.status.day` | HUD 날짜 | DAY {day}/{final_day} | DAY {day}/{final_day} | 14/14 | 정수 2개 |
| `ui.status.hunger` | HUD | 허기 | Hunger | 8/12 | - |
| `ui.status.stamina` | HUD | 체력 | Stamina | 8/12 | 생명력보다 행동 체력 의미 |
| `ui.status.daylight` | HUD | 일광 | Daylight | 8/12 | 남은 낮 시간 |
| `ui.phase.camp_prepare` | 캠프 진입 | 캠프 준비 | Camp Prep | 12/16 | - |
| `ui.phase.post_return` | 귀환 뒤 | 귀환 후 정비 | Post-Search | 14/18 | 정비 의미, repair 직역 금지 |
| `ui.phase.island_search` | 육상 원정 | 섬 수색 | Island Search | 12/18 | - |
| `ui.phase.shallow_swim` | 수영 중 | 얕은 연안 수영 | Shallow-Water Swim | 16/24 | 깊은 잠수 아님 |
| `ui.phase.result` | 결말 | 결과 | Result | 8/12 | - |
| `resource.wood.name` | 전역 자원명 | 나무 | Wood | 6/10 | 물질명 |
| `resource.stone.name` | 전역 자원명 | 돌 | Stone | 6/10 | - |
| `resource.food.name` | 전역 자원명 | 식량 | Food | 6/10 | 개별 요리명이 아님 |
| `resource.salvage.name` | 전역 자원명 | 표류물 | Salvage | 8/12 | driftwood로 한정 금지 |
| `item.stone_axe.name` | 도구명 | 돌도끼 | Stone Axe | 8/16 | - |
| `item.rope.name` | 도구명 | 밧줄 | Rope | 6/10 | - |
| `structure.campfire.name` | 설비명 | 모닥불 | Campfire | 8/14 | - |
| `structure.workbench.name` | 설비명 | 작업대 | Workbench | 8/14 | - |
| `structure.rain_collector.name` | 설비명 | 빗물받이 | Rain Collector | 10/18 | - |
| `structure.signal_tower.name` | 설비명 | 구조 신호대 | Rescue Signal | 12/20 | tower로 고정하지 않음 |
| `ui.signal.progress` | HUD·월드 라벨 | 신호대 {stage}/2 | Signal {stage}/2 | 14/18 | 정수 `{stage}` |
| `ui.resource.amount` | HUD 자원 수량 | {resource} {count} | {resource} {count} | 14/18 | 자원명·정수 |
| `ui.inventory.slot_empty` | 빈 가방 칸 | {slot}. 빈칸 | {slot}. Empty | 12/16 | 정수 `{slot}` |
| `ui.inventory.slot_item` | 가방 칸 | {slot}. {resource} ×{count} | {slot}. {resource} ×{count} | 24/32 | 자원명·정수 |

## 4. 핵심 루프 UI·상호작용 문안

### 4.1 캠프·제작·배치

| Key | 사용 상황 | ko | en | Max ko/en | Placeholder/메모 |
|---|---|---|---|---:|---|
| `ui.camp.actions_title` | 캠프 행동 패널 | 베이스캠프 · 제작 / 건설 / 연구 | Base Camp · Craft / Build / Research | 32/44 | - |
| `ui.action.build_with_cost` | 미건설 설비 버튼 | {structure} 건설  {cost} | Build {structure}  {cost} | 32/48 | locale별 어순 허용 |
| `ui.action.completed` | 완료된 설비 버튼 | ✓ {subject} | ✓ {subject} | 18/26 | 설비·행동명 `{subject}` |
| `ui.action.research_with_cost` | 미연구 버튼 | {item} 연구  {cost} | Research {item}  {cost} | 30/46 | - |
| `ui.action.researched` | 연구 완료 상태 | ✓ {item} 연구 | ✓ {item} Researched | 20/30 | - |
| `ui.action.craft_with_cost` | 미제작 버튼 | {item} 제작  {cost} | Craft {item}  {cost} | 30/44 | - |
| `ui.action.item_owned` | 제작 완료 상태 | ✓ {item} 보유 | ✓ {item} Owned | 20/28 | - |
| `ui.cost.resource_amount` | 비용 조각 | {resource} {count} | {count} {resource} | 12/16 | 문자열 결합 대신 template 사용 |
| `ui.action.signal_with_cost` | 신호 버튼 | 구조 신호대 {stage}/2  {cost} | Rescue Signal {stage}/2  {cost} | 34/50 | - |
| `ui.action.signal_complete` | 신호 완료 | ✓ 구조 신호 발신 | ✓ Signal Transmitting | 20/28 | - |
| `ui.action.eat_with_count` | 식사 버튼 | 식량 먹기  보유 {count} | Eat Food  {count} left | 22/28 | 정수 `{count}` |
| `ui.action.expedition_start` | 출발 | 섬 수색 출발 | Start Island Search | 16/24 | - |
| `ui.action.next_day` | 일과 종료 | 다음 날로 | End Day | 12/16 | 영어는 행동 결과를 명확히 함 |
| `ui.action.final_day_settle` | 3일차 정산 | 마지막 날 정산 | Finish Final Day | 16/22 | - |
| `interaction.placement.place` | 신규 배치 | 배치 | Place | 6/10 | - |
| `interaction.placement.move` | 일반 설비 재배치 | 재배치 | Move | 8/10 | 비용 없음은 별도 안내 |
| `interaction.placement.move_free` | 재배치 안내 | 재배치 비용 없음 | Move for Free | 14/18 | - |
| `interaction.placement.confirm` | 위치 확정 | 이곳에 놓기 | Place Here | 12/16 | - |
| `interaction.placement.invalid_zone` | 호환 구역 아님 | 이 설비를 놓을 수 없는 구역이다. | This structure can't go here. | 24/40 | 규칙 우선, 농담 없음 |
| `interaction.placement.overlap` | 설비 중첩 | 다른 설비와 겹친다. | Another structure is in the way. | 20/40 | - |
| `interaction.placement.blocked_path` | 접근 공간 차단 | 사용할 공간을 남겨야 한다. | Leave room to use it. | 22/34 | 직접 사용 의도 전달 |
| `interaction.placement.fixed_anchor` | 신호대 앵커 | 구조 신호대는 이 앵커에만 세울 수 있다. | The rescue signal only fits its anchor. | 32/48 | fixed를 기술어처럼 쓰지 않음 |
| `interaction.structure.approach` | 원거리 사용 | 설비 가까이 가야 사용할 수 있다. | Move closer to use this structure. | 26/42 | 공간형 거점 규칙 |

### 4.2 가방·수색·조작 안내

| Key | 사용 상황 | ko | en | Max ko/en | Placeholder/메모 |
|---|---|---|---|---:|---|
| `ui.inventory.camp_storage` | 캠프 가방 패널 | 캠프 창고\n(수색 중에는 4칸 가방) | Camp Storage\n(4-slot bag during searches) | 28/44 | 최대 2줄 |
| `ui.inventory.expedition` | 수색 가방 | 수색 가방 4칸\n한 묶음 최대 2개 | 4-Slot Search Bag\n2 per stack | 28/40 | 최대 2줄 |
| `ui.inventory.full` | 교체 선택 | 가방이 꽉 찼습니다\n버릴 슬롯을 선택 | Bag Full\nChoose a stack to leave | 28/42 | 최대 2줄 |
| `interaction.gather` | 육상 노드 | 채집 · {resource} ×{count} | Gather · {resource} ×{count} | 24/32 | - |
| `interaction.water_search` | 수상 노드 | 헤엄쳐 수색\n{resource} ×{count} | Swim to Search\n{resource} ×{count} | 24/34 | 깊은 잠수 의미 금지 |
| `interaction.rope_required` | 숲 장벽 | 밧줄 필요 | Rope Required | 12/18 | - |
| `interaction.rope_open` | 해금된 장벽 | 밧줄로 통과 | Cross with Rope | 14/20 | - |
| `interaction.return_to_camp` | 귀환 지점 | 캠프로 귀환 | Return to Camp | 14/20 | - |
| `controls.device.keyboard_mouse` | 장치명 | 키보드·마우스 | Keyboard & Mouse | 16/22 | - |
| `controls.device.gamepad` | 장치명 | 게임패드 | Gamepad | 10/14 | - |
| `controls.camp` | 캠프 하단 안내 | {device} · 버튼을 선택해 캠프를 정비하세요 · {navigate} 이동 · {confirm} 선택 | {device} · Choose a camp action · {navigate} Move · {confirm} Select | 74/108 | 입력 glyph는 별도 placeholder |
| `controls.explore` | 수색 하단 안내 | {device} · {move} 이동 · 해안에서 자동 수영 · {jump} 점프 · {gather} 수색 · {return} 귀환 | {device} · {move} Move · Auto-swim at shore · {jump} Jump · {gather} Search · {return} Return | 86/124 | 최대 1줄 또는 명시적 2줄 |

### 4.3 성공·실패

| Key | 사용 상황 | ko | en | Max ko/en | 메모 |
|---|---|---|---|---:|---|
| `result.rescued.title` | 구조 성공 | 구조 성공! | Rescued! | 12/16 | outcome |
| `result.exhausted.title` | 체력 0 | 김씨, 잠시 누움 | Mr. Kim Takes Five | 14/24 | “lies down” 직역 금지; 가벼운 탈진 코미디 |
| `result.deadline.title` | 3일 기한 | 구조 신호 미완성 | Signal Incomplete | 16/24 | 원인 명시 |

결과 상세 문안과 번역 메타데이터는 아래 5.40~5.42의 단일 레코드를 사용한다.

## 5. 김씨 독백·상황 문구

모든 행은 `ko`의 정보와 코미디 기능을 기준으로 승인한다. `Max`는 `ko/en` 공백 포함 권장 상한이며 동적 자원명 치환 뒤에도 넘지 않아야 한다.

| # | Key | 사용 상황 | 의도 | ko | en | Max ko/en | 직역 금지 메모 |
|---:|---|---|---|---|---|---:|---|
| 1 | `message.run.start` | 새 게임 초기화 | 평범한 김씨의 마지못한 결심 | 파도는 열심히 친다. 김씨도 일단 뭐라도 해보기로 했다. | The waves are doing their part. Mr. Kim decides he probably should too. | 42/84 | 파도가 “일한다”는 대비를 살리고 영웅적 결의로 바꾸지 않음 |
| 2 | `message.build.duplicate` | 이미 지은 설비 재건설 | 중복 불가+건조한 핀잔 | 이미 지어 둔 물건이다. 김씨도 같은 걸 두 번 만들 만큼 한가하지 않다. | Already built. Even Mr. Kim has better things to do than make two. | 48/86 | “한가하지 않다”를 busy로만 직역하지 말고 자기빈정거림 유지 |
| 3 | `message.build.insufficient` | 건설 재료 부족 | 부족 원인+주머니 농담 | 재료가 모자란다. 주머니를 털어도 모래만 나온다. | Not enough materials. He checks his pockets. Still sand. | 40/76 | 실제 모래 자원을 암시하지 않음 |
| 4 | `message.build.campfire_success` | 모닥불 완성 | 기능 성취+연기 불편 | 모닥불 완성. 불은 문명이고, 연기는 눈물이다. | Campfire built. Fire is civilization. Smoke is tears. | 38/72 | “tears”는 연기 탓의 과장, 슬픔 서사 금지 |
| 5 | `message.build.workbench_success` | 작업대 완성 | 엉성하지만 기능함 | 작업대 완성. 수평은 아니지만 물건은 올라간다. | Workbench built. Not level, but things stay on it. | 40/72 | 정교한 제작대처럼 미화하지 않음 |
| 6 | `message.build.rain_success` | 빗물받이 완성 | 날씨와의 소소한 승부 | 빗물받이 완성. 비가 오면 김씨가 이긴다. | Rain collector built. If it rains, Mr. Kim wins. | 38/76 | 거대한 생존 승리로 과장 금지 |
| 7 | `message.research.workbench_required` | 작업대 없이 연구 | 선행 조건 전달 | 연구하려면 먼저 작업대가 필요하다. | Build a workbench before researching. | 30/52 | 농담보다 조건 우선 |
| 8 | `message.research.unavailable` | 재료 부족 또는 완료 | 두 원인 확인 유도 | 연구 재료가 부족하거나 이미 알아낸 방법이다. | Not enough research materials—or he already cracked this one. | 38/82 | cracked는 해결했다는 가벼운 표현, 파괴 의미 금지 |
| 9 | `message.research.axe_success` | 돌도끼 연구 | 급조 제작법의 불확실함 | 연구 완료: 돌과 나무를 묶으면 제법 도끼처럼 보인다. | Research complete: tie stone to wood until it looks axe-ish. | 44/86 | 정식 공학 설명처럼 번역 금지 |
| 10 | `message.research.rope_success` | 밧줄 연구 | 매듭이 풀리지 않는 데 초점 | 연구 완료: 줄은 묶는 법보다 안 풀리게 하는 법이 중요했다. | Research complete: the trick isn't tying a knot. It's stopping it from escaping. | 48/104 | rope가 실제 도망간다는 가벼운 의인화 유지 |
| 11 | `message.craft.unavailable` | 제작 불가 | 레시피·재료 재확인 | 제작법이나 재료를 다시 확인해야 한다. | Check the recipe and materials. | 30/46 | 모호한 오류 코드로 바꾸지 않음 |
| 12 | `message.craft.axe_success` | 돌도끼 제작 | 생산 증가를 나무 관점으로 농담 | 돌도끼 완성. 나무가 두 배로 억울해진다. | Stone axe built. Trees now have twice the reason to complain. | 38/86 | 실제 정확한 2배 수치 약속으로 읽히지 않게 “reason” 유지 |
| 13 | `message.craft.rope_success` | 밧줄 제작 | 새 구역 해금+과장된 관할 | 밧줄 완성. 이제 숲 안쪽도 김씨 관할이다. | Rope built. The inner forest is now under Mr. Kim's jurisdiction. | 40/92 | ownership·colonization 의미 강화 금지 |
| 14 | `message.signal.rope_required` | 2단계에 밧줄 없음 | 필요 도구 명시 | 마지막 안테나를 세우려면 밧줄이 필요하다. | The final antenna needs rope. | 34/46 | final은 2단계 의미, 게임 최종 콘텐츠 확대 금지 |
| 15 | `message.signal.materials_required` | 신호 건설 불가 | 작업대와 비용 명시 | 작업대와 나무 2, 표류물 2가 필요하다. | Need a workbench, 2 wood, and 2 salvage. | 34/62 | 숫자와 자원 의미 변경 금지 |
| 16 | `message.signal.stage1_success` | 신호 1단계 | 외형 진척+멀리서만 그럴듯함 | 구조 신호대 골격 완성. 멀리서 보면 꽤 그럴듯하다. | Signal frame complete. From far away, it almost looks professional. | 44/92 | genuinely professional로 칭찬 금지 |
| 17 | `message.signal.stage2_success` | 신호 2단계·성공 | 엉성함이 실제 발신으로 전환 | 구조 신호 발신! 김씨의 엉성함이 마침내 주파수를 탔다. | Signal transmitting! Mr. Kim's questionable engineering has found a frequency. | 48/102 | “bad signal”로 실패처럼 번역 금지 |
| 18 | `message.food.none` | 식량 없음 | 부족+상상 식사 농담 | 먹을 것이 없다. 코코넛 그림이라도 그려 볼까. | No food. Maybe draw a coconut and call it dinner. | 40/76 | 실제 코코넛 아이템 존재를 암시하지 않음 |
| 19 | `message.food.full` | 허기 100 | 지금 식사 불필요 | 지금은 배가 충분히 부르다. | He's full enough for now. | 24/40 | 영구 포만으로 과장 금지 |
| 20 | `message.food.eaten` | 식량 사용 | 회복+정체불명 메뉴 | 식사 완료. 메뉴 이름은 ‘그냥 익힌 것’이다. | Meal complete. Tonight's dish: “Cooked Something.” | 38/76 | 음식 종류를 새로 발명하지 않음 |
| 21 | `message.expedition.already_done` | 하루 원정 뒤 재출발 | 하루 1회 제한 | 오늘 수색은 끝났다. 캠프를 정리하고 다음 날로 넘어가자. | Today's search is done. Sort camp and call it a day. | 44/80 | day 종료 행동을 유지 |
| 22 | `message.expedition.unavailable` | 기타 출발 불가 | 상태 불가 알림 | 지금은 수색을 시작할 수 없다. | Can't start a search right now. | 28/48 | 원인 없는 일반 오류로 유지 |
| 23 | `message.expedition.start` | 수색 출발 | 일몰 목표+낮은 기대치 | 김씨 출발. 해 지기 전에는 돌아오는 것이 소박한 목표다. | Mr. Kim heads out. Making it back before dark seems ambitious enough. | 44/94 | confident hero line 금지 |
| 24 | `message.swim.enter` | 해안 입수 | 수영 위험+가방 무게 | 김씨 입수. 물은 생각보다 차갑고 가방은 생각보다 무겁다. | Mr. Kim takes the plunge. The water is colder—and the bag heavier—than expected. | 48/108 | plunge는 깊은 잠수 의미가 아니라 입수 표현 |
| 25 | `message.swim.exit` | 육지 복귀 | 안도+땅의 신뢰성 | 육지 복귀. 땅이 이렇게 믿음직스러울 줄은 몰랐다. | Back on land. Ground has never felt so trustworthy. | 40/78 | dramatic salvation으로 확대 금지 |
| 26 | `message.gather.water_requires_swim` | 육지에서 수상 노드 시도 | 수영 필요 조건 | 저 물건은 물에 떠 있다. 발만 담가서는 닿지 않는다. | It's floating out there. Wet feet won't be enough. | 42/76 | 잠수·산소 규칙 추가 금지 |
| 27 | `message.inventory.full` | 가방 초과 | 교체 필요 | 가방이 꽉 찼다. 하나를 버려야 새 물건을 챙길 수 있다. | Bag's full. Leave a stack behind to take the new one. | 44/82 | item 1개가 아니라 slot/stack 교체 의미 유지 |
| 28 | `message.gather.water_success` | 수상 노드 성공 | 획득+높은 체력비 | 파도와 씨름해 건졌다: {resource}. 체력도 같이 떠내려갔다. | Fished {resource} out of the waves. Some stamina drifted off with it. | 52/100 | `{resource}` 유지; 실제 낚시 시스템 암시 금지 |
| 29 | `message.gather.axe_wood` | 도끼로 나무 획득 | 추가 나무 피드백 | 돌도끼가 활약했다. 나무를 하나 더 챙겼다. | The stone axe delivered. One extra wood comes home. | 38/78 | wood를 countable log로 임의 변경 금지 |
| 30 | `message.gather.land_success` | 일반 육상 채집 | 단순 획득 확인 | 챙긴 자원: {resource}. | Picked up: {resource}. | 24/44 | locale별 완성형 template, 한국어 조사 문자열 재사용 금지 |
| 31 | `message.inventory.replace_empty` | 보류 자원을 빈칸에 배치 | 빈칸 사용 확인 | 빈칸에 물건을 넣었다. | Put it in the empty slot. | 22/40 | - |
| 32 | `message.inventory.replace` | 기존 칸 교체 | 버린 것·가져온 것 확인 | 두고 온 것: {discarded}. 새로 챙긴 것: {resource}. | Left {discarded} behind and took {resource}. | 42/76 | 두 placeholder 모두 유지, 어순 변경 가능 |
| 33 | `message.inventory.discard_pending` | 새 자원 포기 | 포기 확인+약한 아쉬움 | 아쉽지만 두고 간다: {resource}. | Left {resource} behind. Regretfully. | 34/64 | melodrama 금지 |
| 34 | `message.return.forced` | 일몰 강제 귀환 | 페널티+게 농담 | 해가 져서 뛰어 돌아왔다. 게 한 마리가 끝까지 응원했다. 아마도. | Sunset sent him sprinting home. A crab cheered the whole way. Probably. | 50/98 | crab을 적·동료 시스템으로 해석 금지 |
| 35 | `message.return.safe` | 자발 귀환 | 안전+무거운 가방/표정 대비 | 무사 귀환. 가방은 무겁고 김씨의 표정은 가볍지 않다. | Back safe. Heavy bag, heavier expression. | 44/70 | light/heavy 말장난을 자연스러운 대비로 재창작 |
| 36 | `message.day.search_required` | 원정 전 일과 종료 | 선행 행동 안내 | 수색을 마쳐야 오늘을 정산할 수 있다. | Finish today's search before ending the day. | 32/64 | settlement 같은 경제 용어 직역 금지 |
| 37 | `message.day.start` | 2·3일차 아침 | 반복 생존+섬과 김씨 역전 | {day}일차 아침. 김씨는 아직도 섬이고, 섬도 아직 김씨다. | Day {day}. Mr. Kim is still on the island. The island is still on Mr. Kim. | 48/100 | 두 문장의 반전 구조 유지 |
| 38 | `message.explore.rope_barrier` | 밧줄 없는 숲 장벽 | 해금 방법 암시 | 숲이 너무 빽빽하다. 밧줄을 만들면 넘어갈 방법이 생길 것 같다. | The forest's too dense. With rope, there might be a way over. | 52/92 | 전투·벌목 해법 추가 금지 |
| 39 | `message.gather.no_target` | 수색 범위에 노드 없음 | 실패 원인+공기 농담 | 손을 뻗어 봤지만 잡히는 건 공기뿐이다. | He reaches out and grabs a generous handful of air. | 36/78 | air item을 얻은 것처럼 시스템 의미 변경 금지 |
| 40 | `result.rescued.detail` | 구조 성공 설명 | 기적 같은 성공+사진 우선 | 급조 안테나가 기적처럼 작동했다. 김씨는 구조대보다 먼저 사진부터 찍었다. | The improvised antenna somehow works. Mr. Kim takes a photo before the rescuers can. | 64/120 | SNS·카메라 기능을 암시하지 않음 |
| 41 | `result.exhausted.detail` | 체력 0 실패 설명 | 원인과 다음 행동 | 체력을 모두 소진했다. 다음에는 먹고 쉬고, 해 지기 전에 돌아오자. | He's completely spent. Next time: eat, rest, and get home before dark. | 64/110 | 실패 원인과 세 조언을 삭제하지 않음 |
| 42 | `result.deadline.detail` | 3일 기한 실패 설명 | 원인과 표류물·밧줄 우선순위 | 3일 안에 구조 신호를 완성하지 못했다. 필요한 표류물과 밧줄을 더 일찍 준비해야 한다. | The signal isn't ready by Day 3. Start gathering salvage—and make the rope—earlier. | 72/130 | Day 3, salvage, rope의 의미를 모두 유지 |

## 6. 후속 언어 확장 규칙

- `es`, `ja`, `zh-Hans`, `zh-Hant`는 위 키를 그대로 사용하고 `context`, `intent`, placeholder, 길이 등급, 직역 금지 메모를 복사해 번역을 시작한다.
- locale마다 새 키를 만들지 않는다. 특정 언어에만 필요한 문법 분기는 같은 의미 아래 ICU-style plural/select variant로 둔다.
- `zh-Hans`와 `zh-Hant`는 한쪽을 자동 변환해 승인하지 않는다. 같은 키에서 별도 번역·검수한다.
- `ja`와 중국어는 `김씨`의 호칭 관계를 임의로 친근하게 만들지 않는다. 평범한 성인에 대한 약간의 거리감을 보존한다.
- `es`는 성별·주어 생략 때문에 김씨의 행위자가 사라지지 않도록 context를 따른다.
- 후속 locale 추가 전에 key coverage 100%, placeholder 집합 동일, fallback 0건을 자동 검사한다.

## 7. 플레이테스트·언어 QA 수용 조건

- `ko`와 `en` 각각으로 새 게임→캠프 준비→배치·제작·연구→육상·수영 수색→가방 교체→귀환→성공 또는 실패→설정 복귀의 핵심 흐름을 완료한다.
- 1280×800과 1920×1080에서 HUD·버튼·가방·월드 라벨·결과 문구가 잘리거나 겹치지 않는다. 독백은 최대 2줄, 결과 설명은 최대 3줄을 목표로 한다.
- 같은 사건을 본 `ko`·`en` 플레이테스터가 필요한 다음 행동, 소비 자원, 성공·실패 원인을 동일하게 설명한다. 핵심 규칙 오해는 locale별 0건이어야 한다.
- 영어 코미디는 한국어의 `intent`와 `transcreation_note`를 충족하되 어순·비유·말장난을 재창작할 수 있다. 역번역의 단어 일치보다 상황 의미와 감정 강도의 동일성을 우선한다.
- 모든 동적 문자열은 locale별 완성형 template를 사용하고 placeholder 누락·추가·자료형 불일치가 0건이어야 한다.
- 언어 변경 뒤 현재 화면의 모든 문자열이 선택 locale로 갱신되고, 이전 locale 잔존·키 노출·fallback이 없어야 한다.

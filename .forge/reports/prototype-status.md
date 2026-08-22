# 김씨 생존기: 무인도 프로토타입 현황

무인도에 조난된 평범한 김씨가 낮에는 황당하지만 쓸모 있는 생존 설비를 만들고, 해가 지기 전 2D 횡스크롤 섬을 수색해 결국 구조 신호를 완성하는 코믹 생존 게임.

## 버티컬 슬라이스

플레이어가 20분 이내에 베이스캠프 준비와 육상·수면 수색을 최소 두 번 반복하고, 제한된 가방 때문에 실제 자원 선택을 한 뒤 제작·건설·연구가 다음 원정을 바꾸는 경험을 거쳐 3일 안에 구조 신호를 완성하거나 생존에 실패하도록 한다.

## 실행 단계

| 단계 | 상태 | 메모 |
|---|---|---|
| design | complete |  |
| art | in_progress | Base Mr. Kim character is adopted and Unity packaged; swimming animation is planned as a separate dependent asset. |
| implementation | planned |  |
| verification | planned |  |

## 다음 작업

- **김씨 수영 애니메이션 제작** (art, high) — task.art.animation.mr-kim.swim
- **해변·숲 수색 구역 제작** (art, high) — task.art.background.coast-forest
- **무인도 베이스캠프 배경 제작** (art, high) — task.art.background.island-camp
- **캠프 설비 분리 파츠 제작** (art, high) — task.art.object.camp-structures
- **PC·Steam 프로토타입 UI 제작** (art, high) — task.art.ui.survival-hud

## 작업

| ID | 레인 | 우선순위 | 상태 | 작업 |
|---|---|---|---|---|
| task.system.system.run-state | implementation | critical | done | 플레이 상태 관리 구현 |
| task.system.system.phase-flow | implementation | critical | done | 캠프·수색·일몰 흐름 구현 |
| task.system.system.inventory | implementation | critical | done | 자원·도구·가방 구현 |
| task.system.system.crafting-tech | implementation | critical | done | 제작법과 연구 구현 |
| task.system.system.camp-structures | implementation | critical | done | 캠프 설비 구현 |
| task.system.system.island-search | implementation | critical | done | 섬 수색과 일광 구현 |
| task.feature.feature.phase-cycle | implementation | critical | done | 캠프·수색·일몰 루프 구현 |
| task.qa.feature.phase-cycle | qa | critical | done | 캠프·수색·일몰 루프 검증 |
| task.feature.feature.inventory-choice | implementation | critical | done | 4칸 가방 선택 구현 |
| task.qa.feature.inventory-choice | qa | critical | done | 4칸 가방 선택 검증 |
| task.feature.feature.crafting-research | implementation | critical | done | 제작과 간단한 연구 구현 |
| task.qa.feature.crafting-research | qa | critical | done | 제작과 간단한 연구 검증 |
| task.feature.feature.camp-building | implementation | critical | done | 베이스캠프 건설 구현 |
| task.qa.feature.camp-building | qa | critical | done | 베이스캠프 건설 검증 |
| task.feature.feature.island-exploration | implementation | critical | done | 횡스크롤 섬 수색 구현 |
| task.qa.feature.island-exploration | qa | critical | done | 횡스크롤 섬 수색 검증 |
| task.art.background.island-camp | art | high | ready | 무인도 베이스캠프 배경 제작 |
| task.art.background.coast-forest | art | high | ready | 해변·숲 수색 구역 제작 |
| task.art.character.mr-kim | art | high | done | 김씨 2D 캐릭터 제작 |
| task.art.object.camp-structures | art | high | ready | 캠프 설비 분리 파츠 제작 |
| task.art.ui.survival-hud | art | high | ready | PC·Steam 프로토타입 UI 제작 |
| task.system.system.survival | implementation | high | done | 허기·체력과 하루 정산 구현 |
| task.system.system.comedy-feedback | implementation | high | done | 상황형 코믹 피드백 구현 |
| task.system.system.input-actions | implementation | high | done | Unity 입력 액션 구현 |
| task.system.system.responsive-ui | implementation | high | done | PC·휴대형 UI 가독성 구현 |
| task.feature.feature.survival-pressure | implementation | high | done | 허기·체력 생존 정산 구현 |
| task.qa.feature.survival-pressure | qa | high | done | 허기·체력 생존 정산 검증 |
| task.feature.feature.escape-outcome | implementation | high | done | 구조 신호와 결말 구현 |
| task.qa.feature.escape-outcome | qa | high | done | 구조 신호와 결말 검증 |
| task.feature.feature.dual-input | implementation | high | done | PC 이중 입력 구현 |
| task.qa.feature.dual-input | qa | high | ready | PC 이중 입력 검증 |
| task.art.animation.mr-kim.swim | art | high | ready | 김씨 수영 애니메이션 제작 |
| task.system.system.swimming | implementation | high | ready | 수영 이동과 수상 위험 구현 |
| task.feature.feature.swimming | implementation | high | ready | 해안 수영과 수상 수색 구현 |
| task.qa.feature.swimming | qa | high | planned | 해안 수영과 수상 수색 검증 |
| task.art.icon.resource-tool-set | art | medium | ready | 자원·도구 아이콘 세트 제작 |
| task.art.effect.comedy-feedback | art | medium | ready | 코믹 피드백 효과 세트 제작 |

## 채택 대기 또는 플레이스홀더 아트

- background.island-camp: needed · 무인도 베이스캠프 배경
- background.coast-forest: needed · 해변·숲 수색 구역
- object.camp-structures: needed · 캠프 설비 분리 파츠
- icon.resource-tool-set: needed · 자원·도구 아이콘 세트
- ui.survival-hud: needed · PC·Steam 프로토타입 UI
- effect.comedy-feedback: needed · 코믹 피드백 효과 세트
- animation.mr-kim.swim: needed · 김씨 수영 애니메이션

## 검증 증거

- task.system.system.run-state: Artifacts/Verification/editmode-checks.txt
- task.system.system.run-state: Artifacts/Verification/playmode-checks.txt
- task.system.system.run-state: Artifacts/Verification/windows-build.txt
- task.system.system.phase-flow: Artifacts/Verification/editmode-checks.txt
- task.system.system.phase-flow: Artifacts/Verification/playmode-checks.txt
- task.system.system.phase-flow: Artifacts/Verification/windows-build.txt
- task.system.system.inventory: Artifacts/Verification/editmode-checks.txt
- task.system.system.inventory: Artifacts/Verification/playmode-checks.txt
- task.system.system.inventory: Artifacts/Verification/windows-build.txt
- task.system.system.crafting-tech: Artifacts/Verification/editmode-checks.txt
- task.system.system.crafting-tech: Artifacts/Verification/playmode-checks.txt
- task.system.system.crafting-tech: Artifacts/Verification/windows-build.txt
- task.system.system.camp-structures: Artifacts/Verification/editmode-checks.txt
- task.system.system.camp-structures: Artifacts/Verification/playmode-checks.txt
- task.system.system.camp-structures: Artifacts/Verification/windows-build.txt
- task.system.system.island-search: Artifacts/Verification/editmode-checks.txt
- task.system.system.island-search: Artifacts/Verification/playmode-checks.txt
- task.system.system.island-search: Artifacts/Verification/windows-build.txt
- task.feature.feature.phase-cycle: Artifacts/Verification/editmode-checks.txt
- task.feature.feature.phase-cycle: Artifacts/Verification/playmode-checks.txt
- task.feature.feature.phase-cycle: Artifacts/Verification/windows-build.txt
- task.qa.feature.phase-cycle: Artifacts/Verification/editmode-checks.txt
- task.qa.feature.phase-cycle: Artifacts/Verification/playmode-checks.txt
- task.qa.feature.phase-cycle: Artifacts/Verification/kim-survival-exploration-1280x800.png
- task.qa.feature.phase-cycle: Artifacts/Verification/kim-survival-playmode-1280x800.png
- task.feature.feature.inventory-choice: Artifacts/Verification/editmode-checks.txt
- task.feature.feature.inventory-choice: Artifacts/Verification/playmode-checks.txt
- task.feature.feature.inventory-choice: Artifacts/Verification/windows-build.txt
- task.qa.feature.inventory-choice: Artifacts/Verification/editmode-checks.txt
- task.qa.feature.inventory-choice: Artifacts/Verification/playmode-checks.txt
- task.qa.feature.inventory-choice: Artifacts/Verification/kim-survival-exploration-1280x800.png
- task.qa.feature.inventory-choice: Artifacts/Verification/kim-survival-playmode-1280x800.png
- task.feature.feature.crafting-research: Artifacts/Verification/editmode-checks.txt
- task.feature.feature.crafting-research: Artifacts/Verification/playmode-checks.txt
- task.feature.feature.crafting-research: Artifacts/Verification/windows-build.txt
- task.qa.feature.crafting-research: Artifacts/Verification/editmode-checks.txt
- task.qa.feature.crafting-research: Artifacts/Verification/playmode-checks.txt
- task.qa.feature.crafting-research: Artifacts/Verification/kim-survival-exploration-1280x800.png
- task.qa.feature.crafting-research: Artifacts/Verification/kim-survival-playmode-1280x800.png
- task.feature.feature.camp-building: Artifacts/Verification/editmode-checks.txt
- task.feature.feature.camp-building: Artifacts/Verification/playmode-checks.txt
- task.feature.feature.camp-building: Artifacts/Verification/windows-build.txt
- task.qa.feature.camp-building: Artifacts/Verification/editmode-checks.txt
- task.qa.feature.camp-building: Artifacts/Verification/playmode-checks.txt
- task.qa.feature.camp-building: Artifacts/Verification/kim-survival-exploration-1280x800.png
- task.qa.feature.camp-building: Artifacts/Verification/kim-survival-playmode-1280x800.png
- task.feature.feature.island-exploration: Artifacts/Verification/editmode-checks.txt
- task.feature.feature.island-exploration: Artifacts/Verification/playmode-checks.txt
- task.feature.feature.island-exploration: Artifacts/Verification/windows-build.txt
- task.qa.feature.island-exploration: Artifacts/Verification/editmode-checks.txt
- task.qa.feature.island-exploration: Artifacts/Verification/playmode-checks.txt
- task.qa.feature.island-exploration: Artifacts/Verification/kim-survival-exploration-1280x800.png
- task.qa.feature.island-exploration: Artifacts/Verification/kim-survival-playmode-1280x800.png
- task.art.character.mr-kim: Forge asset character.mr-kim adopted via job_20260822085926_374033c5
- task.system.system.survival: Artifacts/Verification/editmode-checks.txt
- task.system.system.survival: Artifacts/Verification/playmode-checks.txt
- task.system.system.survival: Artifacts/Verification/windows-build.txt
- task.system.system.comedy-feedback: Artifacts/Verification/editmode-checks.txt
- task.system.system.comedy-feedback: Artifacts/Verification/playmode-checks.txt
- task.system.system.comedy-feedback: Artifacts/Verification/windows-build.txt
- task.system.system.input-actions: Artifacts/Verification/editmode-checks.txt
- task.system.system.input-actions: Artifacts/Verification/playmode-checks.txt
- task.system.system.input-actions: Artifacts/Verification/windows-build.txt
- task.system.system.responsive-ui: Artifacts/Verification/editmode-checks.txt
- task.system.system.responsive-ui: Artifacts/Verification/playmode-checks.txt
- task.system.system.responsive-ui: Artifacts/Verification/windows-build.txt
- task.feature.feature.survival-pressure: Artifacts/Verification/editmode-checks.txt
- task.feature.feature.survival-pressure: Artifacts/Verification/playmode-checks.txt
- task.feature.feature.survival-pressure: Artifacts/Verification/windows-build.txt
- task.qa.feature.survival-pressure: Artifacts/Verification/editmode-checks.txt
- task.qa.feature.survival-pressure: Artifacts/Verification/playmode-checks.txt
- task.qa.feature.survival-pressure: Artifacts/Verification/kim-survival-exploration-1280x800.png
- task.qa.feature.survival-pressure: Artifacts/Verification/kim-survival-playmode-1280x800.png
- task.feature.feature.escape-outcome: Artifacts/Verification/editmode-checks.txt
- task.feature.feature.escape-outcome: Artifacts/Verification/playmode-checks.txt
- task.feature.feature.escape-outcome: Artifacts/Verification/windows-build.txt
- task.qa.feature.escape-outcome: Artifacts/Verification/editmode-checks.txt
- task.qa.feature.escape-outcome: Artifacts/Verification/playmode-checks.txt
- task.qa.feature.escape-outcome: Artifacts/Verification/kim-survival-exploration-1280x800.png
- task.qa.feature.escape-outcome: Artifacts/Verification/kim-survival-playmode-1280x800.png
- task.feature.feature.dual-input: Artifacts/Verification/editmode-checks.txt
- task.feature.feature.dual-input: Artifacts/Verification/playmode-checks.txt
- task.feature.feature.dual-input: Artifacts/Verification/windows-build.txt

## 차단 요소

- 없음

## 미결 질문

- 없음

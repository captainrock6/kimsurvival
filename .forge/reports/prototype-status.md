# 김씨 생존기: 무인도 프로토타입 현황

무인도에 조난된 평범한 김씨가 낮에는 황당하지만 쓸모 있는 생존 설비를 만들고 직접 배치하며, 해가 지기 전 2D 횡스크롤 섬을 수색해 결국 구조 신호를 완성하는 코믹 생존 게임.

## 버티컬 슬라이스

플레이어가 한국어 또는 영어로 20분 이내에 베이스캠프에서 일반 설비를 직접 배치하고 육상·수면 수색을 최소 두 번 반복하며, 제한된 가방 때문에 실제 자원 선택을 한 뒤 제작·건설·연구가 다음 원정을 바꾸는 경험을 거쳐 3일 안에 구조 신호를 완성하거나 생존에 실패하도록 한다.

## 실행 단계

| 단계 | 상태 | 메모 |
|---|---|---|
| design | complete |  |
| art | in_progress | 가방 UI 후보 품질 점수 100 및 비교 보드 생성 완료. 사용자 승인 전이므로 review 상태를 유지하고 runtime 채택은 보류. |
| implementation | complete | 4→6칸 1회 가방 확장, 비용·잠금·날짜 지속·새 게임 초기화·KO/EN UI 구현 완료. |
| verification | complete | 최종 HEAD 8f16dad 기준 PRODUCT/INFRASTRUCTURE/기존 회귀 PASS. 물리 게임패드는 별도 실기 미검증. |

## 다음 작업

- **Wave 3 밸런스 v0.2 적용** (implementation, critical) — task.wave3.implementation.balance-v0-2
- **Wave 3 공간형 캠프 사용 정합화** (implementation, critical) — task.wave3.implementation.spatial-camp-use
- **Wave 3 P1 월드 라벨 가독성 수정** (implementation, critical) — task.wave3.implementation.world-label-readability
- **PC·Steam 다국어 프로토타입 UI 제작** (art, high) — task.art.ui.survival-hud
- **Wave 3 P2 제3 로케일·저장 독립성 구현** (implementation, high) — task.wave3.implementation.third-locale

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
| task.feature.feature.camp-building | implementation | critical | done | 베이스캠프 건설과 배치 구현 |
| task.qa.feature.camp-building | qa | critical | review | 베이스캠프 건설과 배치 검증 |
| task.feature.feature.island-exploration | implementation | critical | done | 횡스크롤 섬 수색 구현 |
| task.qa.feature.island-exploration | qa | critical | done | 횡스크롤 섬 수색 검증 |
| task.system.system.camp-placement | implementation | critical | done | 제한적 자유 배치 구현 |
| task.wave3.implementation.balance-v0-2 | implementation | critical | ready | Wave 3 밸런스 v0.2 적용 |
| task.wave3.implementation.spatial-camp-use | implementation | critical | ready | Wave 3 공간형 캠프 사용 정합화 |
| task.wave3.implementation.world-label-readability | implementation | critical | ready | Wave 3 P1 월드 라벨 가독성 수정 |
| task.wave3.qa.integrated-three-day | qa | critical | blocked | Wave 3 3일 통합 플레이테스트 승인 |
| task.art.background.island-camp | art | high | done | 무인도 베이스캠프 배경 제작 |
| task.art.background.coast-forest | art | high | review | 해변·숲 수색 구역 제작 |
| task.art.character.mr-kim | art | high | done | 김씨 2D 캐릭터 제작 |
| task.art.object.camp-structures | art | high | done | 캠프 설비 분리 파츠 제작 |
| task.art.ui.survival-hud | art | high | ready | PC·Steam 다국어 프로토타입 UI 제작 |
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
| task.art.animation.mr-kim.swim | art | high | review | 김씨 수영 애니메이션 제작 |
| task.system.system.swimming | implementation | high | done | 수영 이동과 수상 위험 구현 |
| task.feature.feature.swimming | implementation | high | done | 해안 수영과 수상 수색 구현 |
| task.qa.feature.swimming | qa | high | done | 해안 수영과 수상 수색 검증 |
| task.system.system.localization | implementation | high | done | Unity 국제화와 현지화 구현 |
| task.feature.feature.localization | implementation | high | done | 한국어·영어와 확장 가능한 국제화 구현 |
| task.qa.feature.localization | qa | high | ready | 한국어·영어와 확장 가능한 국제화 검증 |
| task.wave3.implementation.third-locale | implementation | high | ready | Wave 3 P2 제3 로케일·저장 독립성 구현 |
| task.system.system.bag-capacity-upgrade | implementation | high | done | 가방 용량 성장 구현 |
| task.feature.feature.inventory-capacity-upgrade | implementation | high | done | 가방 4→6칸 확장 구현 |
| task.qa.feature.inventory-capacity-upgrade | qa | high | done | 가방 4→6칸 확장 검증 |
| task.design.wave7-bag-capacity-balance | design | high | done | Wave 7 가방 확장 비용과 3일 경로 검증 |
| task.art.icon.resource-tool-set | art | medium | done | 자원·도구 아이콘 세트 제작 |
| task.art.effect.comedy-feedback | art | medium | done | 코믹 피드백 효과 세트 제작 |
| task.art.ui.bag-capacity-upgrade | art | medium | review | 가방 확장 UI 상태 세트 제작 |
| task.postslice.steam-release-readiness | implementation | low | blocked | 수직 슬라이스 이후 Steam 출하 준비 |

## 채택 대기 또는 플레이스홀더 아트

- background.coast-forest: review · 해변·숲 수색 구역
- ui.survival-hud: needed · PC·Steam 다국어 프로토타입 UI
- animation.mr-kim.swim: review · 김씨 수영 애니메이션
- ui.bag-capacity-upgrade: review · 가방 확장 UI 상태 세트

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
- task.qa.feature.camp-building: Artifacts/Verification/kim-survival-placement-ko-invalid-1280x800.png
- task.qa.feature.camp-building: Artifacts/Verification/kim-survival-placement-en-valid-gamepad-1280x800.png
- task.qa.feature.camp-building: Artifacts/ParallelQA/20260822T113642Z_e695c36/visual-review.txt
- task.feature.feature.island-exploration: Artifacts/Verification/editmode-checks.txt
- task.feature.feature.island-exploration: Artifacts/Verification/playmode-checks.txt
- task.feature.feature.island-exploration: Artifacts/Verification/windows-build.txt
- task.qa.feature.island-exploration: Artifacts/Verification/editmode-checks.txt
- task.qa.feature.island-exploration: Artifacts/Verification/playmode-checks.txt
- task.qa.feature.island-exploration: Artifacts/Verification/kim-survival-exploration-1280x800.png
- task.qa.feature.island-exploration: Artifacts/Verification/kim-survival-playmode-1280x800.png
- task.system.system.camp-placement: Artifacts/Verification/editmode-checks.txt
- task.system.system.camp-placement: Artifacts/Verification/playmode-checks.txt
- task.system.system.camp-placement: Artifacts/Verification/kim-survival-playmode-1280x800.png
- task.wave3.implementation.balance-v0-2: Docs/Design/integrated-prototype-contract-audit.md
- task.wave3.implementation.balance-v0-2: Artifacts/ParallelQA/20260822T113642Z_e695c36/edit-checks.txt
- task.wave3.implementation.spatial-camp-use: Artifacts/Verification/editmode-checks.txt
- task.wave3.implementation.spatial-camp-use: Artifacts/Verification/playmode-checks.txt
- task.wave3.implementation.spatial-camp-use: Artifacts/Verification/kim-survival-placement-ko-invalid-1280x800.png
- task.wave3.implementation.spatial-camp-use: Artifacts/Verification/kim-survival-placement-en-valid-gamepad-1280x800.png
- task.wave3.implementation.world-label-readability: Artifacts/ParallelQA/20260822T113642Z_e695c36/playmode-layout-metrics.txt
- task.wave3.implementation.world-label-readability: Artifacts/ParallelQA/20260822T113642Z_e695c36/visual-review.txt
- task.wave3.implementation.world-label-readability: Artifacts/Verification/kim-survival-exploration-1280x800.png
- task.wave3.implementation.world-label-readability: Artifacts/Verification/kim-survival-swimming-1280x800.png
- task.wave3.qa.integrated-three-day: Artifacts/ParallelQA/20260822T1345Z_ec79cf3_integrated/run-summary.txt
- task.wave3.qa.integrated-three-day: Artifacts/ParallelQA/20260822T1345Z_ec79cf3_integrated/playmode-full-loop.txt
- task.art.background.island-camp: Forge asset background.island-camp adopted and packaged via job_20260822130341_c082e4b6
- task.art.character.mr-kim: Forge asset character.mr-kim adopted via job_20260822085926_374033c5
- task.art.object.camp-structures: Forge asset object.camp-structures adopted and packaged via job_20260822130400_6d786a69
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
- task.qa.feature.dual-input: Artifacts/ParallelQA/20260822T113642Z_e695c36/input-code-path-audit.txt
- task.qa.feature.dual-input: Artifacts/ParallelQA/20260822T113642Z_e695c36/playmode-full-loop.txt
- task.qa.feature.dual-input: Artifacts/Verification/kim-survival-placement-en-valid-gamepad-1280x800.png
- task.art.animation.mr-kim.swim: Assets/_Project/Art/Generated/sprite_animation/job_20260822091448_251bc2a1/quality-report.json
- task.system.system.swimming: Artifacts/Verification/editmode-checks.txt
- task.system.system.swimming: Artifacts/Verification/kim-survival-swimming-1280x800.png
- task.feature.feature.swimming: Artifacts/Verification/playmode-checks.txt
- task.feature.feature.swimming: Artifacts/Verification/kim-survival-swimming-1280x800.png
- task.qa.feature.swimming: Artifacts/Verification/editmode-checks.txt
- task.qa.feature.swimming: Artifacts/Verification/playmode-checks.txt
- task.qa.feature.swimming: Artifacts/Verification/windows-build.txt
- task.qa.feature.swimming: Artifacts/Verification/kim-survival-swimming-1280x800.png
- task.system.system.localization: Artifacts/Verification/editmode-checks.txt
- task.system.system.localization: Artifacts/Verification/playmode-checks.txt
- task.system.system.localization: Artifacts/Verification/windows-build.txt
- task.feature.feature.localization: Artifacts/Verification/playmode-checks.txt
- task.feature.feature.localization: Artifacts/Verification/kim-survival-playmode-1280x800.png
- task.feature.feature.localization: Artifacts/Verification/windows-build.txt
- task.qa.feature.localization: Artifacts/ParallelQA/20260822T113642Z_e695c36/hardcoded-player-strings.txt
- task.qa.feature.localization: Artifacts/ParallelQA/20260822T113642Z_e695c36/locale-relaunch-persistence.txt
- task.qa.feature.localization: Artifacts/ParallelQA/20260822T113642Z_e695c36/visual-review.txt
- task.qa.feature.localization: Artifacts/ParallelQA/20260822T113642Z_e695c36/playmode-layout-metrics.txt
- task.wave3.implementation.third-locale: Artifacts/ParallelQA/20260822T113642Z_e695c36/visual-review.txt
- task.wave3.implementation.third-locale: Artifacts/ParallelQA/20260822T113642Z_e695c36/locale-relaunch-persistence.txt
- task.wave3.implementation.third-locale: Artifacts/ParallelQA/20260822T113642Z_e695c36/hardcoded-player-strings.txt
- task.system.system.bag-capacity-upgrade: Artifacts/ParallelQA/20260823T031500Z_wave7_bag_release_gate/wave7-edit-contracts.txt
- task.system.system.bag-capacity-upgrade: Artifacts/ParallelQA/20260823T031500Z_wave7_bag_release_gate/wave7-play-contracts.txt
- task.feature.feature.inventory-capacity-upgrade: Artifacts/ParallelQA/20260823T031500Z_wave7_bag_release_gate/wave7-summary.txt
- task.feature.feature.inventory-capacity-upgrade: Artifacts/ParallelQA/20260823T031500Z_wave7_bag_release_gate/wave7-layout-metrics.txt
- task.qa.feature.inventory-capacity-upgrade: Artifacts/ParallelQA/20260823T031500Z_wave7_bag_release_gate/wave7-summary.txt
- task.qa.feature.inventory-capacity-upgrade: Artifacts/ParallelQA/20260823T031500Z_wave7_bag_release_gate/windows-hidden-smoke.txt
- task.design.wave7-bag-capacity-balance: Docs/Design/wave7-bag-capacity-upgrade.md#4-업그레이드-전-가방-선택-보존-검증
- task.design.wave7-bag-capacity-balance: Docs/Design/wave7-bag-capacity-upgrade.md#6-w2d1-구매-포함-자연-3일-구조-장부
- task.design.wave7-bag-capacity-balance: Artifacts/ParallelQA/20260823T004700Z_642a73c_wave6_integrated/wave6-edit-contracts.txt
- task.art.icon.resource-tool-set: Forge asset icon.resource-tool-set adopted via job_20260822141317_caf8e11d
- task.art.effect.comedy-feedback: Forge asset effect.comedy-feedback adopted via job_20260822224357_275de712
- task.postslice.steam-release-readiness: Artifacts/ParallelQA/20260822T113642Z_e695c36/steam-readiness-audit.txt
- task.postslice.steam-release-readiness: Artifacts/ParallelQA/20260822T113642Z_e695c36/windows-development-build.txt

## 차단 요소

- task.wave3.qa.integrated-three-day: fed5066 자동 기능 경로는 PASS지만 화면 가독성 P1/P2, 처음 보는 참가자 6명의 외부 세션과 별도 물리 게임패드 ko/en 실기가 남아 있다.
- task.postslice.steam-release-readiness: 수직 슬라이스 이후 별도 사용자 승인, 공식 영문 제목, Steam App ID와 배포 권한이 필요하다.

## 미결 질문

- 없음

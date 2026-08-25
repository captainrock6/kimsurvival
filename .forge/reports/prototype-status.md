# 김씨 생존기: 무인도 프로토타입 현황

무인도에 조난된 평범한 김씨가 50일의 기한 동안 위험을 견디고 수집 지역을 골라 물자를 모으며, 다섯 가지 이상의 탈출 계획과 자신이 살아온 방식에 따라 정상적이거나 황당한 결말을 맞는 코믹 2D 거점 생존 게임.

## 버티컬 슬라이스

실제 기본 데드라인이 Day 50인 빌드에서 김씨가 캠프 지도 오브젝트를 직접 사용해 7개 수집 지역의 42개 유한 node를 수색하고, 귀환·제작·위험 대응·다층 캠프 확장·세 탈출 준비를 거쳐 행동 기반 코믹 엔딩에 도달하는 25~35분 GAME JAM 대표 루프를 검증한다.

## 실행 단계

| 단계 | 상태 | 메모 |
|---|---|---|
| design | complete | 통합 GDD와 GAME JAM 완료 매트릭스가 7/21/42/144, 질병, 다층 캠프, 세 탈출과 코믹 엔딩을 정본으로 고정한다. |
| art | complete | 현재 채택 runtime 리소스는 유지하며 미채택 Wave C 후보는 review-only다. |
| implementation | complete | Wave A~C의 7/21/42/144, 질병, 다층 캠프, 보호 부품, 실제 세 탈출, 21개 ending catalog, Day 20 장기 체류 2종, modifier comic ending과 composite save v2가 통합됐다. |
| verification | human required | e4bbc03의 Wave C 14/14와 Day 20 장기 체류 독립 게이트 15/15가 product/infrastructure GREEN이다. 동일 Windows 후보에서 인간 시간·가방 체감·물리 gamepad·KO 3명/EN 3명 세션만 남았다. |

## 다음 작업

- **커밋 기준 전체 Wave C·Day 20 장기 체류 회귀와 Windows 후보 빌드 고정** (qa, critical)
- **동일 Windows 후보로 실제 5~10분 첫 loop·25~35분 대표 탈출 측정** (qa, critical, HUMAN_REQUIRED)
- **가방 4→6 투자 선택 체감 기록** (qa, critical, HUMAN_REQUIRED)
- **실제 게임패드와 KO 3명·EN 3명 플레이테스트** (qa, critical, HUMAN_REQUIRED)

## 작업

| ID | 레인 | 우선순위 | 상태 | 작업 |
|---|---|---|---|---|
| task.system.system.run-state | implementation | critical | ready | 50일 run·행동 기록 상태 구현 |
| task.system.system.phase-flow | implementation | critical | done | 50일 캠프·지도·수색·정산 흐름 구현 |
| task.system.system.inventory | implementation | critical | done | 자원·도구·가방 구현 |
| task.system.system.crafting-tech | implementation | critical | done | 제작법과 연구 구현 |
| task.system.system.camp-structures | implementation | critical | done | 캠프 설비 구현 |
| task.system.system.island-search | implementation | critical | done | 선택 지역 수색과 일광 구현 |
| task.feature.feature.phase-cycle | implementation | critical | done | 50일 캠프·수색·정산 주기 구현 |
| task.qa.feature.phase-cycle | qa | critical | ready | 50일 캠프·수색·정산 주기 검증 |
| task.feature.feature.inventory-choice | implementation | critical | done | 4칸 가방 선택 구현 |
| task.qa.feature.inventory-choice | qa | critical | done | 4칸 가방 선택 검증 |
| task.feature.feature.crafting-research | implementation | critical | done | 제작과 간단한 연구 구현 |
| task.qa.feature.crafting-research | qa | critical | done | 제작과 간단한 연구 검증 |
| task.feature.feature.camp-building | implementation | critical | done | 베이스캠프 건설과 배치 구현 |
| task.qa.feature.camp-building | qa | critical | done | 베이스캠프 건설과 배치 검증 |
| task.feature.feature.island-exploration | implementation | critical | planned | 선택 지역 횡스크롤 수색 구현 |
| task.qa.feature.island-exploration | qa | critical | planned | 선택 지역 횡스크롤 수색 검증 |
| task.system.system.camp-placement | implementation | critical | done | 제한적 자유 배치 구현 |
| task.wave3.implementation.balance-v0-2 | implementation | critical | done | Wave 12 5일 기한 재기준선 적용 (v0.2 동결) |
| task.wave3.implementation.spatial-camp-use | implementation | critical | done | Wave 3 공간형 캠프 사용 정합화 |
| task.wave3.implementation.world-label-readability | implementation | critical | done | Wave 3 P1 월드 라벨 가독성 수정 |
| task.wave3.qa.integrated-three-day | qa | critical | blocked | Wave 12 5일 통합 플레이테스트 승인 |
| task.design.wave8-external-playtest-package | design | critical | done | 첫 사용자 20분 플레이테스트 실행 패키지 |
| task.art.ui.survival-hud | art | critical | review | 최소 생존 HUD 제작 |
| task.art.ui.camp-contextual-interaction | art | critical | done | 캠프 근접 안내와 설비 전용 팝업 제작 |
| task.feature.feature.camp-object-interaction | implementation | critical | done | 공간형 설비 직접 상호작용 구현 |
| task.qa.feature.camp-object-interaction | qa | critical | done | 공간형 설비 직접 상호작용 검증 |
| task.design.wave9-spatial-camp-spec | design | critical | done | Wave 9 공간형 베이스캠프 상세 계약 |
| task.qa.wave9-spatial-camp-contract-gate | qa | critical | done | Wave 9 공간형 캠프 레드 퍼스트 계약 게이트 |
| task.system.system.survival | implementation | critical | ready | 생존 수치·상태 이상·회복 구현 |
| task.feature.feature.survival-pressure | implementation | critical | planned | 지속 생존 압박과 회복 구현 |
| task.qa.feature.survival-pressure | qa | critical | planned | 지속 생존 압박과 회복 검증 |
| task.feature.feature.escape-outcome | implementation | critical | planned | 다중 탈출과 행동 기반 결말 구현 |
| task.qa.feature.escape-outcome | qa | critical | planned | 다중 탈출과 행동 기반 결말 검증 |
| task.design.wave12-five-day-rebaseline | design | critical | done | Wave 12 5일 수직 슬라이스 재기준선 |
| task.design.wave13-owner-playtest-intake | design | critical | done | Wave 13 사용자 플레이테스트 접수·Forge 기준선 감사 |
| task.design.wave14-natural-route-ledger | design | critical | done | Wave 14 5일 자연 플레이 경로·밸런스 장부 |
| task.art.ui.expedition-map | art | critical | done | 수집 지역 선택 지도 UI 제작 |
| task.art.icon.expedition-resource-risk-set | art | critical | review | 수집 자원·위험·날씨 아이콘 세트 제작 |
| task.system.system.expedition-selection | implementation | critical | done | 수집 지도와 지역 선택 구현 |
| task.system.system.region-loot-rng | implementation | critical | ready | 지역 loot seed와 softlock 보호 구현 |
| task.system.system.hazard-director | implementation | critical | ready | 위험 예고·발생·완화 디렉터 구현 |
| task.system.system.escape-projects | implementation | critical | ready | 다섯 탈출 프로젝트 구현 |
| task.system.system.ending-resolution | implementation | critical | ready | 행동 기반 엔딩 판정 구현 |
| task.feature.feature.expedition-map | implementation | critical | done | 수집 지역 선택 지도 구현 |
| task.qa.feature.expedition-map | qa | critical | ready | 수집 지역 선택 지도 검증 |
| task.feature.feature.resource-randomization | implementation | critical | planned | 시드형 지역 자원·핵심 부품 분배 구현 |
| task.qa.feature.resource-randomization | qa | critical | planned | 시드형 지역 자원·핵심 부품 분배 검증 |
| task.feature.feature.survival-hazards | implementation | critical | planned | 예고·대응 가능한 생존 위험 구현 |
| task.qa.feature.survival-hazards | qa | critical | planned | 예고·대응 가능한 생존 위험 검증 |
| task.design.wave15-fifty-day-campaign-rebaseline | design | critical | done | Wave 15 50일 캠페인·수집 지도·위험·다중 엔딩 재기준선 |
| task.design.wave15-escape-hazard-ending-matrix | design | critical | done | Wave 15 탈출법·위험·18개 이상 엔딩 콘텐츠 매트릭스 |
| task.implementation.wave15-campaign-map-foundation | implementation | critical | done | Wave 15 Day 50·수집 지도·시드형 지역 선택 기반 |
| task.implementation.wave15-hazard-ending-foundation | implementation | critical | done | Wave 15 위험·다중 탈출·행동 엔딩 기반 |
| task.qa.wave15-campaign-map-redfirst | qa | critical | done | Wave 15 Day 50·수집 지도·RNG red-first 게이트 |
| task.design.wave16-fifty-day-pacing | design | critical | done | Wave 16 50일 SAMPLE_ONLY 페이싱 계약 |
| task.design.wave17-natural-fifty-day-playtest-protocol | design | critical | done | Wave 17 자연 50일 플레이테스트·튜닝 프로토콜 |
| task.art.background.island-camp | art | high | done | 무인도 베이스캠프 배경 제작 |
| task.art.background.coast-forest | art | high | review | 해변·숲 수색 구역 제작 |
| task.art.character.mr-kim | art | high | done | 김씨 2D 캐릭터 제작 |
| task.art.object.camp-structures | art | high | done | 캠프 설비 분리 파츠 제작 |
| task.system.system.comedy-feedback | implementation | high | done | 상황형 코믹 피드백 구현 |
| task.system.system.input-actions | implementation | high | done | Unity 입력 액션 구현 |
| task.system.system.responsive-ui | implementation | high | done | PC·휴대형 UI 가독성 구현 |
| task.feature.feature.dual-input | implementation | high | done | PC 이중 입력 구현 |
| task.qa.feature.dual-input | qa | high | ready | PC 이중 입력 검증 |
| task.art.animation.mr-kim.swim | art | high | review | 김씨 수영 애니메이션 제작 |
| task.system.system.swimming | implementation | high | done | 수영 이동과 수상 위험 구현 |
| task.feature.feature.swimming | implementation | high | done | 해안 수영과 수상 수색 구현 |
| task.qa.feature.swimming | qa | high | done | 해안 수영과 수상 수색 검증 |
| task.system.system.localization | implementation | high | done | Unity 국제화와 현지화 구현 |
| task.feature.feature.localization | implementation | high | done | 한국어·영어와 확장 가능한 국제화 구현 |
| task.qa.feature.localization | qa | high | done | 한국어·영어와 확장 가능한 국제화 검증 |
| task.wave3.implementation.third-locale | implementation | high | done | Wave 3 P2 제3 로케일·저장 독립성 구현 |
| task.system.system.bag-capacity-upgrade | implementation | high | done | 가방 용량 성장 구현 |
| task.feature.feature.inventory-capacity-upgrade | implementation | high | done | 가방 4→6칸 확장 구현 |
| task.qa.feature.inventory-capacity-upgrade | qa | high | blocked | 가방 4→6칸 확장 검증 |
| task.design.wave7-bag-capacity-balance | design | high | done | Wave 7 가방 확장 비용과 3일 경로 검증 |
| task.art.background.modular-island-camp | art | high | review | 측면 절개형 모듈 베이스캠프 제작 |
| task.art.ui.camp-module-expansion | art | high | review | 방 모듈 증축 상태 UI 제작 |
| task.feature.feature.camp-module-expansion | implementation | high | done | 위·옆·지하 방 모듈 증축 구현 |
| task.qa.feature.camp-module-expansion | qa | high | done | 위·옆·지하 방 모듈 증축 검증 |
| task.art.ui.escape-project-progress | art | high | done | 다중 탈출 프로젝트 상태 UI 제작 |
| task.art.ui.ending-comic | art | high | done | 엔딩 코믹북 컷신 프레임 제작 |
| task.art.effect.survival-hazards | art | high | done | 생존 위험·완화 피드백 세트 제작 |
| task.feature.feature.behavioral-endings | implementation | high | ready | 누적 행동 기반 엔딩 판정 구현 |
| task.qa.feature.behavioral-endings | qa | high | planned | 누적 행동 기반 엔딩 판정 검증 |
| task.qa.wave15-hazard-ending-redfirst | qa | high | ready | Wave 15 위험·탈출·엔딩 red-first 게이트 |
| task.art.icon.resource-tool-set | art | medium | done | 자원·도구 아이콘 세트 제작 |
| task.art.effect.comedy-feedback | art | medium | done | 코믹 피드백 효과 세트 제작 |
| task.art.ui.bag-capacity-upgrade | art | medium | review | 가방 확장 UI 상태 세트 제작 |
| task.art.ui.ending-gallery | art | medium | ready | 김씨의 생존 앨범 UI 제작 |
| task.system.system.ending-gallery | implementation | medium | ready | 엔딩 해금과 갤러리 구현 |
| task.feature.feature.ending-gallery | implementation | medium | planned | 김씨의 생존 앨범 구현 |
| task.qa.feature.ending-gallery | qa | medium | planned | 김씨의 생존 앨범 검증 |
| task.postslice.steam-release-readiness | implementation | low | blocked | 수직 슬라이스 이후 Steam 출하 준비 |
| task.feature.feature.custom-run-settings | implementation | low | planned | 사용자 설정 생존 플레이 구현 |
| task.qa.feature.custom-run-settings | qa | low | planned | 사용자 설정 생존 플레이 검증 |

## 채택 대기 또는 플레이스홀더 아트

- background.coast-forest: review · 해변·숲 수색 구역
- ui.survival-hud: review · 최소 생존 HUD
- animation.mr-kim.swim: review · 김씨 수영 애니메이션
- ui.bag-capacity-upgrade: review · 가방 확장 UI 상태 세트
- background.modular-island-camp: review · 측면 절개형 모듈 베이스캠프
- ui.camp-module-expansion: review · 방 모듈 증축 상태 UI
- icon.expedition-resource-risk-set: review · Wave 15 icon.expedition-resource-risk-set 로컬 품질 정리 child job. 부모 job_20260823144003_552c87b1의 24개 투명 아이콘 atlas와 형태 문법을 변
- ui.ending-gallery: needed · 김씨의 생존 앨범 UI

## 검증 증거

- task.system.system.run-state: Artifacts/Verification/editmode-checks.txt
- task.system.system.run-state: Artifacts/Verification/playmode-checks.txt
- task.system.system.run-state: Artifacts/Verification/windows-build.txt
- task.system.system.phase-flow: Artifacts/Verification/editmode-checks.txt
- task.system.system.phase-flow: Artifacts/Verification/playmode-checks.txt
- task.system.system.phase-flow: Artifacts/Verification/windows-build.txt
- task.system.system.phase-flow: Artifacts/ParallelQA/20260823T125000Z_13ecded_release/wave12-edit-contracts.json
- task.system.system.phase-flow: Artifacts/ParallelQA/20260823T125000Z_13ecded_release/wave11-full-regression.txt
- task.system.system.phase-flow: Artifacts/ParallelQA/20260824_integrated_0aae8a2_wave15/wave15-edit-contracts.txt
- task.system.system.phase-flow: Artifacts/ParallelQA/20260824_integrated_0aae8a2_wave15/wave15-play-contracts.txt
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
- task.system.system.island-search: Artifacts/ParallelQA/20260824_integrated_0aae8a2_wave15/wave15-play-contracts.txt
- task.system.system.island-search: Artifacts/Verification/wave15-playmode/playmode-checks.txt
- task.feature.feature.phase-cycle: Artifacts/Verification/editmode-checks.txt
- task.feature.feature.phase-cycle: Artifacts/Verification/playmode-checks.txt
- task.feature.feature.phase-cycle: Artifacts/Verification/windows-build.txt
- task.feature.feature.phase-cycle: Artifacts/ParallelQA/20260823T125000Z_13ecded_release/wave12-edit-contracts.json
- task.feature.feature.phase-cycle: Artifacts/ParallelQA/20260823T125000Z_13ecded_release/wave11-full-regression.txt
- task.feature.feature.phase-cycle: Artifacts/ParallelQA/20260824_integrated_0aae8a2_wave15/wave15-edit-contracts.txt
- task.feature.feature.phase-cycle: Artifacts/ParallelQA/20260824_integrated_0aae8a2_wave15/wave15-play-contracts.txt
- task.qa.feature.phase-cycle: Artifacts/Verification/editmode-checks.txt
- task.qa.feature.phase-cycle: Artifacts/Verification/playmode-checks.txt
- task.qa.feature.phase-cycle: Artifacts/Verification/kim-survival-exploration-1280x800.png
- task.qa.feature.phase-cycle: Artifacts/Verification/kim-survival-playmode-1280x800.png
- task.qa.feature.phase-cycle: Artifacts/ParallelQA/20260823T125000Z_13ecded_release/wave12-edit-contracts.json
- task.qa.feature.phase-cycle: Artifacts/ParallelQA/20260823T125000Z_13ecded_release/wave11-full-regression.txt
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
- task.qa.feature.camp-building: Artifacts/ParallelQA/20260823T125000Z_13ecded_release/wave3-visual-gate.txt
- task.qa.feature.camp-building: Artifacts/ParallelQA/20260823T125000Z_13ecded_release/wave11-full-regression.txt
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
- task.wave3.implementation.balance-v0-2: Artifacts/ParallelQA/20260823T125000Z_13ecded_release/wave12-edit-contracts.json
- task.wave3.implementation.balance-v0-2: Artifacts/ParallelQA/20260823T125000Z_13ecded_release/wave11-full-regression.txt
- task.wave3.implementation.balance-v0-2: Artifacts/ParallelQA/20260823T141000Z_bd1c580_wave14_final/wave12-summary.txt; superseded as active campaign target by decision.wave15-default-fifty-day-deadline
- task.wave3.implementation.spatial-camp-use: Artifacts/Verification/editmode-checks.txt
- task.wave3.implementation.spatial-camp-use: Artifacts/Verification/playmode-checks.txt
- task.wave3.implementation.spatial-camp-use: Artifacts/Verification/kim-survival-placement-ko-invalid-1280x800.png
- task.wave3.implementation.spatial-camp-use: Artifacts/Verification/kim-survival-placement-en-valid-gamepad-1280x800.png
- task.wave3.implementation.spatial-camp-use: PASS: Unity 6000.4.9f1 compile 0 errors/0 warnings; deterministic Edit spatial contracts; full Play survival loop with 1.25-unit camp use and KO/EN; Wave 7 Edit 12/12 and Play 3/3 product checks; Windows x64 Development build 0 errors/0 warnings and 1280x800 player responsive smoke.
- task.wave3.implementation.world-label-readability: Artifacts/ParallelQA/20260822T113642Z_e695c36/playmode-layout-metrics.txt
- task.wave3.implementation.world-label-readability: Artifacts/ParallelQA/20260822T113642Z_e695c36/visual-review.txt
- task.wave3.implementation.world-label-readability: Artifacts/Verification/kim-survival-exploration-1280x800.png
- task.wave3.implementation.world-label-readability: Artifacts/Verification/kim-survival-swimming-1280x800.png
- task.wave3.implementation.world-label-readability: Artifacts/ParallelQA/20260823T125000Z_13ecded_release/wave3-visual-gate.txt
- task.wave3.qa.integrated-three-day: Artifacts/ParallelQA/20260822T1345Z_ec79cf3_integrated/run-summary.txt
- task.wave3.qa.integrated-three-day: Artifacts/ParallelQA/20260822T1345Z_ec79cf3_integrated/playmode-full-loop.txt
- task.wave3.qa.integrated-three-day: Artifacts/ParallelQA/20260823T031500Z_wave7_bag_release_gate/wave7-summary.txt
- task.wave3.qa.integrated-three-day: Artifacts/ParallelQA/20260823T125000Z_13ecded_release/wave12-summary.json
- task.wave3.qa.integrated-three-day: Artifacts/ParallelQA/20260823T125000Z_13ecded_release/wave12-play-contracts.json
- task.wave3.qa.integrated-three-day: Artifacts/ParallelQA/20260823T125000Z_13ecded_release/windows-development-build.json
- task.wave3.qa.integrated-three-day: Artifacts/ParallelQA/20260823T125000Z_13ecded_release/wave11-full-regression.txt
- task.design.wave8-external-playtest-package: Docs/Design/wave8-external-playtest-package.md
- task.art.ui.camp-contextual-interaction: Forge asset ui.camp-contextual-interaction adopted via job_20260823073121_f5da3402
- task.feature.feature.camp-object-interaction: Artifacts/ParallelQA/20260823T0443Z_d088cbd_wave9_contextual_camp/wave9-summary.md · deterministic Edit PASS; product Play full loop PASS; 1280x800 ko/en far/proximity/facility popups PASS; Windows x64 Development build 0 errors/0 warnings; hidden player smoke alive at 8s
- task.feature.feature.camp-object-interaction: Artifacts/ParallelQA/20260823T125000Z_13ecded_release/wave12-play-contracts.json
- task.feature.feature.camp-object-interaction: Artifacts/ParallelQA/20260823T125000Z_13ecded_release/wave11-full-regression.txt
- task.qa.feature.camp-object-interaction: Artifacts/ParallelQA/20260823T125000Z_13ecded_release/wave12-play-contracts.json
- task.qa.feature.camp-object-interaction: Artifacts/ParallelQA/20260823T125000Z_13ecded_release/wave11-full-regression.txt
- task.design.wave9-spatial-camp-spec: Docs/Design/wave9-spatial-base-camp-spec.md
- task.design.wave9-spatial-camp-spec: Docs/Design/References/approved-spatial-base-camp-concept.png
- task.design.wave9-spatial-camp-spec: .forge/packets/wave9-spatial-camp-detail.json
- task.qa.wave9-spatial-camp-contract-gate: Artifacts/ParallelQA/20260823T044500Z_d088cbd_wave9_spatial_camp_red/wave9-summary.json
- task.qa.wave9-spatial-camp-contract-gate: Artifacts/ParallelQA/20260823T044500Z_d088cbd_wave9_spatial_camp_red/wave9-play-contracts.json
- task.qa.wave9-spatial-camp-contract-gate: Artifacts/ParallelQA/20260823T044500Z_d088cbd_wave9_spatial_camp_red/wave9-command-results.json
- task.qa.wave9-spatial-camp-contract-gate: Artifacts/ParallelQA/20260823T052053Z_246b1b8_wave9_spatial_camp_playtest/wave9-summary.json
- task.qa.wave9-spatial-camp-contract-gate: Artifacts/ParallelQA/20260823T052053Z_246b1b8_wave9_spatial_camp_playtest/wave9-play-contracts.json
- task.qa.wave9-spatial-camp-contract-gate: Artifacts/ParallelQA/20260823T052053Z_246b1b8_wave9_spatial_camp_playtest/windows-development-build.json
- task.qa.wave9-spatial-camp-contract-gate: Artifacts/ParallelQA/20260823T125000Z_13ecded_release/wave11-slot-edit-evidence.json
- task.qa.wave9-spatial-camp-contract-gate: Artifacts/ParallelQA/20260823T125000Z_13ecded_release/wave11-slot-play-evidence.json
- task.system.system.survival: Artifacts/Verification/editmode-checks.txt
- task.system.system.survival: Artifacts/Verification/playmode-checks.txt
- task.system.system.survival: Artifacts/Verification/windows-build.txt
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
- task.feature.feature.escape-outcome: Artifacts/ParallelQA/20260823T125000Z_13ecded_release/wave12-edit-contracts.json
- task.qa.feature.escape-outcome: Artifacts/Verification/editmode-checks.txt
- task.qa.feature.escape-outcome: Artifacts/Verification/playmode-checks.txt
- task.qa.feature.escape-outcome: Artifacts/Verification/kim-survival-exploration-1280x800.png
- task.qa.feature.escape-outcome: Artifacts/Verification/kim-survival-playmode-1280x800.png
- task.qa.feature.escape-outcome: Artifacts/ParallelQA/20260823T125000Z_13ecded_release/wave12-edit-contracts.json
- task.design.wave12-five-day-rebaseline: Docs/Design/wave12-five-day-rebaseline.md
- task.design.wave12-five-day-rebaseline: .forge/design/wave12-five-day-rebaseline.json
- task.design.wave13-owner-playtest-intake: Docs/Design/Playtest/wave13-owner-playtest-intake.md
- task.design.wave13-owner-playtest-intake: .forge/design/wave13-owner-playtest-intake.json
- task.design.wave13-owner-playtest-intake: Artifacts/ParallelQA/20260823T125000Z_13ecded_release/wave12-summary.json
- task.design.wave13-owner-playtest-intake: Artifacts/ParallelQA/20260823T125000Z_13ecded_release/wave12-edit-contracts.json
- task.design.wave13-owner-playtest-intake: Artifacts/ParallelQA/20260823T125000Z_13ecded_release/wave12-play-contracts.json
- task.design.wave13-owner-playtest-intake: Artifacts/ParallelQA/20260823T125000Z_13ecded_release/wave3-visual-gate.txt
- task.design.wave14-natural-route-ledger: Docs/Design/Playtest/wave14-natural-route-ledger.md
- task.design.wave14-natural-route-ledger: .forge/design/wave14-natural-route-ledger.json
- task.design.wave14-natural-route-ledger: Assets/_Project/Scripts/Runtime/GameSession.cs
- task.design.wave14-natural-route-ledger: Assets/_Project/Scripts/Runtime/KimSurvivalPrototype.cs
- task.design.wave14-natural-route-ledger: Assets/_Project/Scripts/Runtime/PrototypePlayerTraversal.cs
- task.design.wave14-natural-route-ledger: Assets/_Project/Scripts/Runtime/PrototypePlaytestEventLog.cs
- task.design.wave14-natural-route-ledger: Artifacts/ParallelQA/20260823T131000Z_8eecfa2_integrated/wave11-full-regression.txt
- task.design.wave14-natural-route-ledger: Artifacts/ParallelQA/20260823T123000Z_386a602_wave13_release/wave13-playtest-package.json
- task.art.ui.expedition-map: Forge asset ui.expedition-map adopted via job_20260823150636_e3b39abc
- task.system.system.expedition-selection: Artifacts/ParallelQA/20260824_integrated_0aae8a2_wave15/wave15-play-contracts.txt
- task.feature.feature.expedition-map: Wave16 selected-only right-rail A GUID contract PASS; Unity compile 0 error/0 warning; deterministic EditMode PASS; full PlayMode ko/en/qps-long 1280x800 PASS with overflow/offscreen/rail/node overlap all 0; Windows x64 Development build and 6-second hidden smoke PASS. Evidence: Artifacts/Verification/wave16-expedition-map-a-runtime/verification-summary.txt
- task.design.wave15-fifty-day-campaign-rebaseline: .forge/design/project.json; .forge/design/vertical-slice.json; .forge/project.json
- task.design.wave15-escape-hazard-ending-matrix: .forge/packets/wave15-fifty-day-campaign-rebaseline.json
- task.design.wave15-escape-hazard-ending-matrix: Docs/Design/wave15-escape-hazard-ending-matrix.md
- task.implementation.wave15-campaign-map-foundation: PASS: Artifacts/Verification/wave15-campaign-map/wave15-campaign-map-contracts.txt; Artifacts/Verification/wave15-playmode/playmode-checks.txt; Artifacts/ParallelQA/20260824T001300Z_7796cf5_wave15/wave14-qps-global-layout-gate.txt; Windows development build and 1280x800 hidden smoke PASS
- task.implementation.wave15-campaign-map-foundation: Artifacts/ParallelQA/20260824_integrated_0aae8a2_wave15/wave15-summary.txt
- task.implementation.wave15-campaign-map-foundation: Artifacts/ParallelQA/20260824_integrated_0aae8a2_wave15/wave15-edit-contracts.txt
- task.implementation.wave15-campaign-map-foundation: Artifacts/ParallelQA/20260824_integrated_0aae8a2_wave15/wave15-play-contracts.txt
- task.implementation.wave15-hazard-ending-foundation: Artifacts/ParallelQA/20260824T_wave17_7c14ab8_full/wave16-summary.json; Wave15 GREEN, product failures 0, infrastructure PASS, compile 0 errors/0 warnings, KO/EN/qps-long 1280x800 3-panel capture, Windows development build and hidden smoke PASS at 7c14ab8415149b76eeaab9fc02d4c965a2d9af68
- task.qa.wave15-campaign-map-redfirst: RED recorded: Artifacts/Verification/wave15-campaign-map-red/wave15-campaign-map-contracts.txt; GREEN PASS: Artifacts/Verification/wave15-campaign-map/wave15-campaign-map-contracts.txt; Edit/Play regression, ko/en/qps-long 1280x800 captures, Windows x64 development build and hidden smoke PASS
- task.qa.wave15-campaign-map-redfirst: Artifacts/ParallelQA/20260824_integrated_0aae8a2_wave15/wave15-summary.txt
- task.qa.wave15-campaign-map-redfirst: Artifacts/ParallelQA/20260824_integrated_0aae8a2_wave15/wave15-edit-contracts.txt
- task.qa.wave15-campaign-map-redfirst: Artifacts/ParallelQA/20260824_integrated_0aae8a2_wave15/wave15-play-contracts.txt
- task.design.wave16-fifty-day-pacing: .forge/packets/wave16-fifty-day-pacing.json
- task.design.wave16-fifty-day-pacing: Docs/Design/wave16-fifty-day-pacing.md
- task.design.wave16-fifty-day-pacing: Forge status/report PASS; contract validation PASS: catalog 5/6/7/19, bands 5, routes 2+3, pity 3/5, min routes 3, stats 7; assets hash preserved 3a8d0d976e9c9f53720a25e41496997fb72a1cfe
- task.design.wave17-natural-fifty-day-playtest-protocol: .forge/packets/wave17-natural-fifty-day-playtest-protocol.json
- task.design.wave17-natural-fifty-day-playtest-protocol: Docs/Design/wave17-natural-fifty-day-playtest-protocol.md
- task.design.wave17-natural-fifty-day-playtest-protocol: Forge status/report PASS; contract validation PASS: sessions 6 (ko 3/en 3), paired seeds 3, all UNRUN, bands 5, metrics 10, axes 7, E0-E3, catalog 5/6/7/19, pity 3/5, minimum routes 3; Wave 15/16 and assets/vertical/implementation preserved.
- task.art.background.island-camp: Forge asset background.island-camp adopted and packaged via job_20260822130341_c082e4b6
- task.art.character.mr-kim: Forge asset character.mr-kim adopted via job_20260822085926_374033c5
- task.art.object.camp-structures: Forge asset object.camp-structures adopted and packaged via job_20260822130400_6d786a69
- task.system.system.comedy-feedback: Artifacts/Verification/editmode-checks.txt
- task.system.system.comedy-feedback: Artifacts/Verification/playmode-checks.txt
- task.system.system.comedy-feedback: Artifacts/Verification/windows-build.txt
- task.system.system.input-actions: Artifacts/Verification/editmode-checks.txt
- task.system.system.input-actions: Artifacts/Verification/playmode-checks.txt
- task.system.system.input-actions: Artifacts/Verification/windows-build.txt
- task.system.system.responsive-ui: Artifacts/Verification/editmode-checks.txt
- task.system.system.responsive-ui: Artifacts/Verification/playmode-checks.txt
- task.system.system.responsive-ui: Artifacts/Verification/windows-build.txt
- task.feature.feature.dual-input: Artifacts/Verification/editmode-checks.txt
- task.feature.feature.dual-input: Artifacts/Verification/playmode-checks.txt
- task.feature.feature.dual-input: Artifacts/Verification/windows-build.txt
- task.qa.feature.dual-input: Artifacts/ParallelQA/20260822T113642Z_e695c36/input-code-path-audit.txt
- task.qa.feature.dual-input: Artifacts/ParallelQA/20260822T113642Z_e695c36/playmode-full-loop.txt
- task.qa.feature.dual-input: Artifacts/Verification/kim-survival-placement-en-valid-gamepad-1280x800.png
- task.qa.feature.dual-input: Artifacts/ParallelQA/20260823T125000Z_13ecded_release/wave12-play-contracts.json
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
- task.qa.feature.localization: Artifacts/ParallelQA/20260823T125000Z_13ecded_release/wave12-play-contracts.json
- task.qa.feature.localization: Artifacts/ParallelQA/20260823T125000Z_13ecded_release/wave3-visual-gate.txt
- task.qa.feature.localization: Artifacts/ParallelQA/20260823T141000Z_bd1c580_wave14_final/wave3-visual-gate.txt
- task.qa.feature.localization: Artifacts/ParallelQA/20260823T141000Z_bd1c580_wave14_final/wave12-play-contracts.json
- task.qa.feature.localization: Artifacts/ParallelQA/20260823T141000Z_bd1c580_wave14_final/wave11-full-regression.txt
- task.qa.feature.localization: Artifacts/Verification/wave14-qps-global-layout-runtime-final/editmode-checks.txt
- task.wave3.implementation.third-locale: Artifacts/ParallelQA/20260822T113642Z_e695c36/visual-review.txt
- task.wave3.implementation.third-locale: Artifacts/ParallelQA/20260822T113642Z_e695c36/locale-relaunch-persistence.txt
- task.wave3.implementation.third-locale: Artifacts/ParallelQA/20260822T113642Z_e695c36/hardcoded-player-strings.txt
- task.wave3.implementation.third-locale: Artifacts/ParallelQA/20260823T100500Z_bcf31dd_integrated/wave10-module-play-contracts.json
- task.system.system.bag-capacity-upgrade: Artifacts/ParallelQA/20260823T031500Z_wave7_bag_release_gate/wave7-edit-contracts.txt
- task.system.system.bag-capacity-upgrade: Artifacts/ParallelQA/20260823T031500Z_wave7_bag_release_gate/wave7-play-contracts.txt
- task.feature.feature.inventory-capacity-upgrade: Artifacts/ParallelQA/20260823T031500Z_wave7_bag_release_gate/wave7-summary.txt
- task.feature.feature.inventory-capacity-upgrade: Artifacts/ParallelQA/20260823T031500Z_wave7_bag_release_gate/wave7-layout-metrics.txt
- task.qa.feature.inventory-capacity-upgrade: Artifacts/ParallelQA/20260823T031500Z_wave7_bag_release_gate/wave7-summary.txt
- task.qa.feature.inventory-capacity-upgrade: Artifacts/ParallelQA/20260823T031500Z_wave7_bag_release_gate/windows-hidden-smoke.txt
- task.qa.feature.inventory-capacity-upgrade: Artifacts/ParallelQA/20260823T125000Z_13ecded_release/wave11-full-regression.txt
- task.design.wave7-bag-capacity-balance: Docs/Design/wave7-bag-capacity-upgrade.md#4-업그레이드-전-가방-선택-보존-검증
- task.design.wave7-bag-capacity-balance: Docs/Design/wave7-bag-capacity-upgrade.md#6-w2d1-구매-포함-자연-3일-구조-장부
- task.design.wave7-bag-capacity-balance: Artifacts/ParallelQA/20260823T004700Z_642a73c_wave6_integrated/wave6-edit-contracts.txt
- task.feature.feature.camp-module-expansion: PASS: deterministic Edit checks, full Play Mode survival regression, 1280x800 KO/EN/qps-long module captures, Windows development build 0 errors/0 warnings, 8-second launch smoke.
- task.qa.feature.camp-module-expansion: Artifacts/ParallelQA/20260823T100500Z_bcf31dd_integrated/wave10-summary.json
- task.qa.feature.camp-module-expansion: Artifacts/ParallelQA/20260823T125000Z_13ecded_release/wave11-slot-edit-evidence.json
- task.qa.feature.camp-module-expansion: Artifacts/ParallelQA/20260823T125000Z_13ecded_release/wave11-slot-play-evidence.json
- task.qa.feature.camp-module-expansion: Artifacts/ParallelQA/20260823T125000Z_13ecded_release/wave11-full-regression.txt
- task.art.ui.escape-project-progress: Forge asset ui.escape-project-progress adopted via job_20260823160324_1de3b748
- task.art.ui.ending-comic: Forge asset ui.ending-comic adopted via job_20260823160342_eceb3933
- task.art.effect.survival-hazards: Forge asset effect.survival-hazards adopted via job_20260823160305_ef04b0f3
- task.art.icon.resource-tool-set: Forge asset icon.resource-tool-set adopted via job_20260822141317_caf8e11d
- task.art.effect.comedy-feedback: Forge asset effect.comedy-feedback adopted via job_20260822224357_275de712
- task.postslice.steam-release-readiness: Artifacts/ParallelQA/20260822T113642Z_e695c36/steam-readiness-audit.txt
- task.postslice.steam-release-readiness: Artifacts/ParallelQA/20260822T113642Z_e695c36/windows-development-build.txt

## 차단 요소

- task.wave3.qa.integrated-three-day: 자동 5일·compact A·Windows 빌드는 PASS다. grant·warp 없는 fresh 자연 경로, 첫 사용자 6세션(ko 3, en 3), 별도 물리 게임패드 ko/en 실기가 아직 없다.
- task.qa.feature.inventory-capacity-upgrade: 4→6 기능과 원자성은 최종 회귀 PASS다. 구매 후 grant·warp 없는 자연 Day 4~5 구조 경로의 독립 증거가 아직 없다.
- task.postslice.steam-release-readiness: 수직 슬라이스 이후 별도 사용자 승인, 공식 영문 제목, Steam App ID와 배포 권한이 필요하다.

## 미결 질문

- 없음

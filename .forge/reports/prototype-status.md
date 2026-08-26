# 김씨 생존기: 무인도 프로토타입 현황

무인도에 조난된 김씨가 내용물을 알 수 없는 수색 오브젝트를 직접 뒤져 필요한 물건만 가방에 챙기고, 상태가 영구히 변하는 일곱 지역과 다층 캠프를 오가며 여러 방법으로 탈출하거나 행동 기반 코믹 엔딩을 맞는 2D 생존 게임.

## 버티컬 슬라이스

처음 10분 안에 환경 수색 오브젝트를 뒤지고 발견물을 선별하는 유한 수색과 다층 캠프 투자를 이해시키고, 약 30분 안에 뗏목·대형 연기·무전 구조 중 하나를 완성해 행동 기반 코믹북 엔딩을 보게 한다.

## 실행 단계

| 단계 | 상태 | 메모 |
|---|---|---|
| design | complete |  |
| art | in_progress | 3d64403의 adopted/runtime art lock은 과거 GREEN 기준선이다. 현재 art/meta·presentation 변경은 새 통합·release 검증 대기이며 review 후보는 계속 미채택이다. |
| implementation | in_progress | 현재 미커밋 작업 트리는 자동 요구 DONE 0/24, PARTIAL 20/24이다. 전체 통합 게이트·Windows build/smoke·package integrity·release manifest가 아직 없다. |
| verification | blocked | 현재 후보 자동 증거가 먼저 필요하다. 그 뒤에도 HUMAN_REQUIRED는 GJC-12, GJC-17, GJC-20, GJC-23 네 항목이다. |

## 다음 작업

- **현재 GAME JAM 작업 트리 통합·release 재검증** (qa, critical) — task.gamejam.qa-thirty-minute-seven-region-slice

## 작업

| ID | 레인 | 우선순위 | 상태 | 작업 |
|---|---|---|---|---|
| task.system.system.run-state | implementation | critical | done | 게임잼 캠페인·지역·다층 캠프 상태 구현 |
| task.system.system.phase-flow | implementation | critical | done | 30분 조기 탈출·장기 체류 흐름 구현 |
| task.system.system.inventory | implementation | critical | done | 자원·도구·가방 구현 |
| task.system.system.crafting-tech | implementation | critical | done | 제작법과 연구 구현 |
| task.system.system.camp-structures | implementation | critical | done | 다층 캠프 설비·연결부 구현 |
| task.system.system.island-search | implementation | critical | done | 7지역 유한 수색과 변화 기록 구현 |
| task.feature.feature.phase-cycle | implementation | critical | done | 50일 캠프·수색·정산 주기 구현 |
| task.qa.feature.phase-cycle | qa | critical | done | 50일 캠프·수색·정산 주기 검증 |
| task.feature.feature.inventory-choice | implementation | critical | done | 4칸 가방 선택 구현 |
| task.qa.feature.inventory-choice | qa | critical | done | 4칸 가방 선택 검증 |
| task.feature.feature.crafting-research | implementation | critical | done | 제작과 간단한 연구 구현 |
| task.qa.feature.crafting-research | qa | critical | done | 제작과 간단한 연구 검증 |
| task.feature.feature.camp-building | implementation | critical | done | 베이스캠프 건설과 배치 구현 |
| task.qa.feature.camp-building | qa | critical | done | 베이스캠프 건설과 배치 검증 |
| task.feature.feature.island-exploration | implementation | critical | done | 선택 지역 횡스크롤 수색 구현 |
| task.qa.feature.island-exploration | qa | critical | done | 선택 지역 횡스크롤 수색 검증 |
| task.system.system.camp-placement | implementation | critical | done | 다층 제한적 자유 배치 구현 |
| task.wave3.implementation.balance-v0-2 | implementation | critical | done | Wave 12 5일 기한 재기준선 적용 (v0.2 동결) |
| task.wave3.implementation.spatial-camp-use | implementation | critical | done | Wave 3 공간형 캠프 사용 정합화 |
| task.wave3.implementation.world-label-readability | implementation | critical | done | Wave 3 P1 월드 라벨 가독성 수정 |
| task.wave3.qa.integrated-three-day | qa | critical | review | Wave 12 5일 통합 플레이테스트 승인 |
| task.design.wave8-external-playtest-package | design | critical | done | 첫 사용자 20분 플레이테스트 실행 패키지 |
| task.art.ui.survival-hud | art | critical | review | 최소 생존 HUD 제작 |
| task.art.ui.camp-contextual-interaction | art | critical | done | 캠프 근접 안내와 설비 전용 팝업 제작 |
| task.feature.feature.camp-object-interaction | implementation | critical | done | 공간형 설비 직접 상호작용 구현 |
| task.qa.feature.camp-object-interaction | qa | critical | done | 공간형 설비 직접 상호작용 검증 |
| task.design.wave9-spatial-camp-spec | design | critical | done | Wave 9 공간형 베이스캠프 상세 계약 |
| task.qa.wave9-spatial-camp-contract-gate | qa | critical | done | Wave 9 공간형 캠프 레드 퍼스트 계약 게이트 |
| task.system.system.survival | implementation | critical | done | 허기·부상·질병 악화와 치료 구현 |
| task.feature.feature.survival-pressure | implementation | critical | done | 지속 생존 압박과 회복 구현 |
| task.qa.feature.survival-pressure | qa | critical | done | 지속 생존 압박과 회복 검증 |
| task.feature.feature.escape-outcome | implementation | critical | done | 재료 추적형 뗏목·연기·무전 탈출 구현 |
| task.qa.feature.escape-outcome | qa | critical | done | 재료 추적형 뗏목·연기·무전 탈출 검증 |
| task.design.wave12-five-day-rebaseline | design | critical | done | Wave 12 5일 수직 슬라이스 재기준선 |
| task.design.wave13-owner-playtest-intake | design | critical | done | Wave 13 사용자 플레이테스트 접수·Forge 기준선 감사 |
| task.design.wave14-natural-route-ledger | design | critical | done | Wave 14 5일 자연 플레이 경로·밸런스 장부 |
| task.art.ui.expedition-map | art | critical | done | 수집 지역 선택 지도 UI 제작 |
| task.art.icon.expedition-resource-risk-set | art | critical | review | 수집 자원·위험·날씨 아이콘 세트 제작 |
| task.system.system.expedition-selection | implementation | critical | done | 수집 지도와 지역 선택 구현 |
| task.system.system.region-loot-rng | implementation | critical | done | 7지역 유한 loot·부품 softlock 보호 구현 |
| task.system.system.hazard-director | implementation | critical | review | 벌레·야생동물·위험 식물·질병 디렉터 구현 |
| task.system.system.escape-projects | implementation | critical | done | 유한 재료형 뗏목·연기·무전 프로젝트 구현 |
| task.system.system.ending-resolution | implementation | critical | done | 탈출·Day 20 장기 체류 판정 구현 |
| task.feature.feature.expedition-map | implementation | critical | done | 7지역 상태형 수집 지도 구현 |
| task.qa.feature.expedition-map | qa | critical | done | 7지역 상태형 수집 지도 검증 |
| task.feature.feature.resource-randomization | implementation | critical | done | 7지역 유한 자원·핵심 부품 분배 구현 |
| task.qa.feature.resource-randomization | qa | critical | done | 7지역 유한 자원·핵심 부품 분배 검증 |
| task.feature.feature.survival-hazards | implementation | critical | review | 지역 위험도·질병 생존 위험 구현 |
| task.qa.feature.survival-hazards | qa | critical | review | 지역 위험도·질병 생존 위험 검증 |
| task.design.wave15-fifty-day-campaign-rebaseline | design | critical | done | Wave 15 50일 캠페인·수집 지도·위험·다중 엔딩 재기준선 |
| task.design.wave15-escape-hazard-ending-matrix | design | critical | done | Wave 15 탈출법·위험·18개 이상 엔딩 콘텐츠 매트릭스 |
| task.implementation.wave15-campaign-map-foundation | implementation | critical | done | Wave 15 Day 50·수집 지도·시드형 지역 선택 기반 |
| task.implementation.wave15-hazard-ending-foundation | implementation | critical | done | Wave 15 위험·다중 탈출·행동 엔딩 기반 |
| task.qa.wave15-campaign-map-redfirst | qa | critical | done | Wave 15 Day 50·수집 지도·RNG red-first 게이트 |
| task.design.wave16-fifty-day-pacing | design | critical | done | Wave 16 50일 SAMPLE_ONLY 페이싱 계약 |
| task.design.wave17-natural-fifty-day-playtest-protocol | design | critical | done | Wave 17 자연 50일 플레이테스트·튜닝 프로토콜 |
| task.design.wave18-green-transition-contract | design | critical | done | Wave 18 15개 FAIL GREEN 전환 계약 |
| task.implementation.wave18-green-transition | implementation | critical | done | Wave 18 15개 제품 FAIL GREEN 전환 구현 |
| task.qa.wave18-green-transition | qa | critical | done | Wave 18 GREEN 전환 통합 검증 |
| task.feature.feature.camp-module-expansion | implementation | critical | done | 2층·지하실 다층 캠프 확장 구현 |
| task.qa.feature.camp-module-expansion | qa | critical | done | 2층·지하실 다층 캠프 확장 검증 |
| task.feature.feature.behavioral-endings | implementation | critical | done | 탈출·장기 체류 행동 기반 엔딩 구현 |
| task.qa.feature.behavioral-endings | qa | critical | done | 탈출·장기 체류 행동 기반 엔딩 검증 |
| task.system.system.region-persistence | implementation | critical | done | 지역별 유한 월드 상태 구현 |
| task.feature.feature.region-persistence | implementation | critical | done | 수색 지역 영구 변화 구현 |
| task.qa.feature.region-persistence | qa | critical | done | 수색 지역 영구 변화 검증 |
| task.gamejam.seven-region-catalog | implementation | critical | done | 게임잼 7지역 42-node 카탈로그 확장 |
| task.gamejam.persistent-region-runtime | implementation | critical | done | 게임잼 지역 영속 상태·유한 자원 런타임 |
| task.gamejam.smoke-radio-material-routes | implementation | critical | done | 게임잼 부싯돌·무전 3부품·pity·seed 감사 |
| task.gamejam.insect-plant-wildlife-disease | implementation | critical | review | 게임잼 환경 위험·jungle-fever lifecycle |
| task.gamejam.upper-basement-both | implementation | critical | done | 게임잼 2층·지하실 동시 확장 |
| task.gamejam.qa-thirty-minute-seven-region-slice | qa | critical | blocked | 게임잼 Wave B/C 42-node·30분 통합 검증 |
| task.system.system.search-node-loot | implementation | critical | done | 결정론적 수색 오브젝트·발견물 transaction 구현 |
| task.feature.feature.searchable-resource-nodes | implementation | critical | done | 환경 수색 오브젝트·발견물 선별 구현 |
| task.qa.feature.searchable-resource-nodes | qa | critical | done | 환경 수색 오브젝트·발견물 선별 검증 |
| task.system.system.raft-escape | implementation | critical | done | 뗏목 단계·출항 창 상태기계 구현 |
| task.feature.feature.raft-escape | implementation | critical | done | 뗏목 제작·해안 진수 탈출 구현 |
| task.qa.feature.raft-escape | qa | critical | done | 뗏목 제작·해안 진수 탈출 검증 |
| task.design.gamejam-seven-region-search-node-contract | design | critical | done | 게임잼 7지역 수색 node·유한 자원 계약 |
| task.design.gamejam-completion-matrix | design | critical | review | GAME JAM 제출 완료 기준 감사 |
| task.art.background.island-camp | art | high | done | 무인도 베이스캠프 배경 제작 |
| task.art.background.coast-forest | art | high | review | 해변·숲 수색 구역 제작 |
| task.art.character.mr-kim | art | high | done | 김씨 2D 캐릭터 제작 |
| task.art.object.camp-structures | art | high | done | 캠프 설비 분리 파츠 제작 |
| task.system.system.comedy-feedback | implementation | high | done | 상황형 코믹 피드백 구현 |
| task.system.system.input-actions | implementation | high | done | Unity 입력 액션 구현 |
| task.system.system.responsive-ui | implementation | high | done | PC·휴대형 UI 가독성 구현 |
| task.feature.feature.dual-input | implementation | high | done | PC 이중 입력 구현 |
| task.qa.feature.dual-input | qa | high | blocked | PC 이중 입력 검증 |
| task.art.animation.mr-kim.swim | art | high | review | 김씨 수영 애니메이션 제작 |
| task.art.environment.expedition-region-kit | art | high | review | 7지역 지속 수색 환경 키트 제작 |
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
| task.art.ui.escape-project-progress | art | high | done | 다중 탈출 프로젝트 상태 UI 제작 |
| task.art.ui.ending-comic | art | high | done | 엔딩 코믹북 컷신 프레임 제작 |
| task.art.effect.survival-hazards | art | high | done | 생존 위험·완화 피드백 세트 제작 |
| task.qa.wave15-hazard-ending-redfirst | qa | high | done | Wave 15 위험·탈출·엔딩 red-first 게이트 |
| task.design.wave19-resource-playtest-contract | design | high | done | Wave 19 리소스 통합 첫 사용자 플레이테스트 계약 |
| task.gamejam.long-stay-two-endings | implementation | high | done | 게임잼 장기 체류 엔딩 2종 |
| task.art.object.searchable-resource-node-kit | art | high | review | 7지역 환경 수색 오브젝트 키트 제작 |
| task.art.ui.search-loot-tray | art | high | review | 발견물 선별 트레이 UI 제작 |
| task.art.icon.resource-tool-set | art | medium | done | 자원·도구 아이콘 세트 제작 |
| task.art.effect.comedy-feedback | art | medium | done | 코믹 피드백 효과 세트 제작 |
| task.art.ui.bag-capacity-upgrade | art | medium | review | 가방 확장 UI 상태 세트 제작 |
| task.art.ui.ending-gallery | art | medium | done | 김씨의 생존 앨범 UI 제작 |
| task.system.system.ending-gallery | implementation | medium | done | 엔딩 해금과 갤러리 구현 |
| task.feature.feature.ending-gallery | implementation | medium | done | 김씨의 생존 앨범 구현 |
| task.qa.feature.ending-gallery | qa | medium | done | 김씨의 생존 앨범 검증 |
| task.postslice.steam-release-readiness | implementation | low | planned | 수직 슬라이스 이후 Steam 출하 준비 |
| task.feature.feature.custom-run-settings | implementation | low | ready | 사용자 설정 생존 플레이 구현 |
| task.qa.feature.custom-run-settings | qa | low | planned | 사용자 설정 생존 플레이 검증 |

## 채택 대기 또는 플레이스홀더 아트

- background.coast-forest: review · 해변·숲 수색 구역
- ui.survival-hud: review · 최소 생존 HUD
- animation.mr-kim.swim: review · 김씨 수영 애니메이션
- ui.bag-capacity-upgrade: review · 가방 확장 UI 상태 세트
- background.modular-island-camp: review · 측면 절개형 모듈 베이스캠프
- ui.camp-module-expansion: review · 방 모듈 증축 상태 UI
- icon.expedition-resource-risk-set: review · Wave 15 icon.expedition-resource-risk-set 로컬 품질 정리 child job. 부모 job_20260823144003_552c87b1의 24개 투명 아이콘 atlas와 형태 문법을 변
- environment.expedition-region-kit: review · 수집 지역 레이어 환경 키트
- object.searchable-resource-node-kit: review · 환경 수색 오브젝트 상태 키트
- ui.search-loot-tray: review · 발견물 선별 compact 트레이

## 검증 증거

- task.system.system.run-state: Artifacts/Verification/editmode-checks.txt
- task.system.system.run-state: Artifacts/Verification/playmode-checks.txt
- task.system.system.run-state: Artifacts/Verification/windows-build.txt
- task.system.system.run-state: Artifacts/Verification/wave19-integrated-96af07d/wave19-summary.json
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
- task.qa.feature.phase-cycle: Artifacts/Verification/wave19-integrated-96af07d/wave19-summary.json
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
- task.feature.feature.island-exploration: Artifacts/ParallelQA/20260827T190000Z_gamejam_waveb_3d64403/gamejam-search-node-summary.json
- task.feature.feature.island-exploration: Artifacts/ParallelQA/20260827T190000Z_gamejam_waveb_3d64403/gamejam-wave-b-play-contracts.json
- task.qa.feature.island-exploration: Artifacts/Verification/editmode-checks.txt
- task.qa.feature.island-exploration: Artifacts/Verification/playmode-checks.txt
- task.qa.feature.island-exploration: Artifacts/Verification/kim-survival-exploration-1280x800.png
- task.qa.feature.island-exploration: Artifacts/Verification/kim-survival-playmode-1280x800.png
- task.qa.feature.island-exploration: Artifacts/ParallelQA/20260827T190000Z_gamejam_waveb_3d64403/gamejam-search-node-summary.json
- task.qa.feature.island-exploration: Artifacts/ParallelQA/20260827T190000Z_gamejam_waveb_3d64403/gamejam-search-node-play-observation-evidence.json
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
- task.system.system.survival: Artifacts/Verification/wave19-integrated-96af07d/wave19-summary.json
- task.feature.feature.survival-pressure: Artifacts/Verification/editmode-checks.txt
- task.feature.feature.survival-pressure: Artifacts/Verification/playmode-checks.txt
- task.feature.feature.survival-pressure: Artifacts/Verification/windows-build.txt
- task.feature.feature.survival-pressure: Artifacts/ParallelQA/20260827T190000Z_gamejam_waveb_3d64403/gamejam-wave-b-play-contracts.json
- task.feature.feature.survival-pressure: Artifacts/ParallelQA/20260827T180000Z_gamejam_wavec_3d64403/gamejam-wave-c-play-contracts.json
- task.qa.feature.survival-pressure: Artifacts/Verification/editmode-checks.txt
- task.qa.feature.survival-pressure: Artifacts/Verification/playmode-checks.txt
- task.qa.feature.survival-pressure: Artifacts/Verification/kim-survival-exploration-1280x800.png
- task.qa.feature.survival-pressure: Artifacts/Verification/kim-survival-playmode-1280x800.png
- task.qa.feature.survival-pressure: Artifacts/ParallelQA/20260827T190000Z_gamejam_waveb_3d64403/gamejam-wave-b-summary.json
- task.qa.feature.survival-pressure: Artifacts/ParallelQA/20260827T180000Z_gamejam_wavec_3d64403/gamejam-wave-c-summary.json
- task.feature.feature.escape-outcome: Artifacts/Verification/editmode-checks.txt
- task.feature.feature.escape-outcome: Artifacts/Verification/playmode-checks.txt
- task.feature.feature.escape-outcome: Artifacts/Verification/windows-build.txt
- task.feature.feature.escape-outcome: Artifacts/ParallelQA/20260823T125000Z_13ecded_release/wave12-edit-contracts.json
- task.feature.feature.escape-outcome: Artifacts/ParallelQA/20260827T180000Z_gamejam_wavec_3d64403/gamejam-wave-c-play-contracts.json
- task.feature.feature.escape-outcome: Artifacts/ParallelQA/finalqa_9263705_wave17/wave17-summary.json
- task.qa.feature.escape-outcome: Artifacts/Verification/editmode-checks.txt
- task.qa.feature.escape-outcome: Artifacts/Verification/playmode-checks.txt
- task.qa.feature.escape-outcome: Artifacts/Verification/kim-survival-exploration-1280x800.png
- task.qa.feature.escape-outcome: Artifacts/Verification/kim-survival-playmode-1280x800.png
- task.qa.feature.escape-outcome: Artifacts/ParallelQA/20260823T125000Z_13ecded_release/wave12-edit-contracts.json
- task.qa.feature.escape-outcome: Artifacts/ParallelQA/20260827T180000Z_gamejam_wavec_3d64403/gamejam-wave-c-summary.json
- task.qa.feature.escape-outcome: Artifacts/ParallelQA/finalqa_9263705_wave17/wave17-summary.json
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
- task.system.system.region-loot-rng: Artifacts/Verification/wave19-integrated-96af07d/wave19-summary.json
- task.system.system.hazard-director: Artifacts/Verification/wave19-integrated-96af07d/wave19-summary.json
- task.system.system.escape-projects: Artifacts/ParallelQA/20260827T180000Z_gamejam_wavec_3d64403/gamejam-wave-c-play-contracts.json
- task.system.system.escape-projects: Artifacts/ParallelQA/20260827T190000Z_gamejam_waveb_3d64403/gamejam-wave-b-summary.json
- task.system.system.ending-resolution: Artifacts/Verification/wave19-integrated-96af07d/wave19-summary.json
- task.feature.feature.expedition-map: Wave16 selected-only right-rail A GUID contract PASS; Unity compile 0 error/0 warning; deterministic EditMode PASS; full PlayMode ko/en/qps-long 1280x800 PASS with overflow/offscreen/rail/node overlap all 0; Windows x64 Development build and 6-second hidden smoke PASS. Evidence: Artifacts/Verification/wave16-expedition-map-a-runtime/verification-summary.txt
- task.qa.feature.expedition-map: Artifacts/Verification/wave19-integrated-96af07d/wave19-summary.json
- task.feature.feature.resource-randomization: Artifacts/ParallelQA/20260827T190000Z_gamejam_waveb_3d64403/gamejam-wave-b-edit-contracts.json
- task.feature.feature.resource-randomization: Artifacts/ParallelQA/20260827T190000Z_gamejam_waveb_3d64403/gamejam-wave-b-play-contracts.json
- task.qa.feature.resource-randomization: Artifacts/ParallelQA/20260827T190000Z_gamejam_waveb_3d64403/gamejam-wave-b-summary.json
- task.qa.feature.resource-randomization: Artifacts/ParallelQA/20260827T190000Z_gamejam_waveb_3d64403/gamejam-search-node-summary.json
- task.feature.feature.survival-hazards: Artifacts/ParallelQA/20260827T190000Z_gamejam_waveb_3d64403/gamejam-wave-b-play-contracts.json
- task.feature.feature.survival-hazards: Artifacts/ParallelQA/20260827T180000Z_gamejam_wavec_3d64403/gamejam-wave-c-play-contracts.json
- task.qa.feature.survival-hazards: Artifacts/ParallelQA/20260827T190000Z_gamejam_waveb_3d64403/gamejam-wave-b-summary.json
- task.qa.feature.survival-hazards: Artifacts/ParallelQA/20260827T180000Z_gamejam_wavec_3d64403/gamejam-wave-c-summary.json
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
- task.design.wave18-green-transition-contract: Artifacts/ParallelQA/20260824T_wave17_integrated_d3e71f3_full/wave17-summary.json
- task.design.wave18-green-transition-contract: .forge/packets/wave18-green-transition-contract.json
- task.design.wave18-green-transition-contract: Docs/Design/wave18-green-transition-contract.md
- task.implementation.wave18-green-transition: Artifacts/Verification/wave19-integrated-96af07d/wave19-summary.json
- task.qa.wave18-green-transition: Artifacts/Verification/wave19-integrated-96af07d/wave19-summary.json
- task.feature.feature.camp-module-expansion: PASS: deterministic Edit checks, full Play Mode survival regression, 1280x800 KO/EN/qps-long module captures, Windows development build 0 errors/0 warnings, 8-second launch smoke.
- task.qa.feature.camp-module-expansion: Artifacts/ParallelQA/20260823T100500Z_bcf31dd_integrated/wave10-summary.json
- task.qa.feature.camp-module-expansion: Artifacts/ParallelQA/20260823T125000Z_13ecded_release/wave11-slot-edit-evidence.json
- task.qa.feature.camp-module-expansion: Artifacts/ParallelQA/20260823T125000Z_13ecded_release/wave11-slot-play-evidence.json
- task.qa.feature.camp-module-expansion: Artifacts/ParallelQA/20260823T125000Z_13ecded_release/wave11-full-regression.txt
- task.feature.feature.behavioral-endings: Artifacts/Verification/wave19-integrated-96af07d/wave19-summary.json
- task.qa.feature.behavioral-endings: Artifacts/Verification/wave19-integrated-96af07d/wave19-summary.json
- task.system.system.region-persistence: Artifacts/ParallelQA/20260826T053000Z_gamejam_search_node_integrated_green/gamejam-search-node-summary.json
- task.feature.feature.region-persistence: Artifacts/ParallelQA/20260826T053000Z_gamejam_search_node_integrated_green/gamejam-search-node-summary.json
- task.qa.feature.region-persistence: Artifacts/ParallelQA/20260826T053000Z_gamejam_search_node_integrated_green/gamejam-search-node-summary.json
- task.gamejam.seven-region-catalog: Artifacts/ParallelQA/20260826T053000Z_gamejam_search_node_integrated_green/gamejam-search-node-summary.json
- task.gamejam.seven-region-catalog: Artifacts/ParallelQA/20260826T082000Z_gamejam_wave_b_committed_green/gamejam-wave-b-edit-contracts.json
- task.gamejam.seven-region-catalog: Artifacts/ParallelQA/20260826T082000Z_gamejam_wave_b_committed_green/gamejam-wave-b-play-contracts.json
- task.gamejam.seven-region-catalog: Docs/Design/gamejam-wave-bc-design.md
- task.gamejam.persistent-region-runtime: Artifacts/ParallelQA/20260826T053000Z_gamejam_search_node_integrated_green/gamejam-search-node-summary.json
- task.gamejam.smoke-radio-material-routes: Artifacts/ParallelQA/20260826T081500Z_wavec_red_dirty/gamejam-wave-c-edit-contracts.json
- task.gamejam.smoke-radio-material-routes: Docs/Design/gamejam-wave-bc-design.md
- task.gamejam.smoke-radio-material-routes: Artifacts/ParallelQA/20260826T160000Z_gamejam_wave_c_committed/gamejam-wave-c-summary.json
- task.gamejam.smoke-radio-material-routes: Artifacts/ParallelQA/20260826T160000Z_gamejam_wave_c_committed/gamejam-wave-c-play-contracts.json
- task.gamejam.insect-plant-wildlife-disease: Artifacts/ParallelQA/20260826T082000Z_gamejam_wave_b_committed_green/gamejam-wave-b-play-contracts.json
- task.gamejam.insect-plant-wildlife-disease: Docs/Design/gamejam-wave-bc-design.md
- task.gamejam.upper-basement-both: Artifacts/ParallelQA/20260826T160000Z_gamejam_wave_c_committed/gamejam-wave-c-summary.json
- task.gamejam.upper-basement-both: Artifacts/ParallelQA/20260826T160000Z_gamejam_wave_c_committed/gamejam-wave-c-play-contracts.json
- task.gamejam.qa-thirty-minute-seven-region-slice: Docs/Design/gamejam-wave-bc-design.md
- task.gamejam.qa-thirty-minute-seven-region-slice: Artifacts/ParallelQA/20260826T160000Z_gamejam_wave_c_committed/gamejam-wave-c-summary.json
- task.gamejam.qa-thirty-minute-seven-region-slice: Artifacts/ParallelQA/20260826T174500Z_gamejam_long_stay_integrated/gamejam-long-stay-summary.json
- task.gamejam.qa-thirty-minute-seven-region-slice: Docs/QA/gamejam-wave-c-integrated-green-e4bbc03.md
- task.gamejam.qa-thirty-minute-seven-region-slice: Artifacts/ParallelQA/20260827T190000Z_gamejam_waveb_3d64403/gamejam-wave-b-summary.json
- task.gamejam.qa-thirty-minute-seven-region-slice: Artifacts/ParallelQA/20260827T180000Z_gamejam_wavec_3d64403/gamejam-wave-c-summary.json
- task.gamejam.qa-thirty-minute-seven-region-slice: Artifacts/ParallelQA/finalqa_9263705_longstay/gamejam-long-stay-summary.json
- task.gamejam.qa-thirty-minute-seven-region-slice: Artifacts/ParallelQA/finalqa_9263705_wave17/wave17-summary.json
- task.gamejam.qa-thirty-minute-seven-region-slice: Artifacts/ParallelQA/pkg_2c9f36a_verified/gamejam-package-integrity-summary.json
- task.gamejam.qa-thirty-minute-seven-region-slice: Docs/QA/gamejam-final-windows-candidate-30m-human-checklist-ko.md
- task.gamejam.qa-thirty-minute-seven-region-slice: Docs/Design/Playtest/gamejam-seven-region-search-node-results.md
- task.system.system.search-node-loot: Artifacts/ParallelQA/20260826T053000Z_gamejam_search_node_integrated_green/gamejam-search-node-summary.json
- task.feature.feature.searchable-resource-nodes: Artifacts/ParallelQA/20260826T053000Z_gamejam_search_node_integrated_green/gamejam-search-node-summary.json
- task.qa.feature.searchable-resource-nodes: Artifacts/ParallelQA/20260826T053000Z_gamejam_search_node_integrated_green/gamejam-search-node-summary.json
- task.system.system.raft-escape: Artifacts/ParallelQA/20260825T145418Z_wave20_release_candidate/wave20-summary.json
- task.feature.feature.raft-escape: Artifacts/ParallelQA/20260825T145418Z_wave20_release_candidate/wave20-summary.json
- task.feature.feature.raft-escape: Artifacts/ParallelQA/20260826T053000Z_gamejam_search_node_integrated_green/wave20-summary.json
- task.feature.feature.raft-escape: Artifacts/ParallelQA/20260826T053000Z_gamejam_search_node_integrated_green/gamejam-search-node-summary.json
- task.qa.feature.raft-escape: Artifacts/ParallelQA/20260825T145418Z_wave20_release_candidate/wave20-summary.json
- task.qa.feature.raft-escape: Artifacts/ParallelQA/20260826T053000Z_gamejam_search_node_integrated_green/wave20-summary.json
- task.qa.feature.raft-escape: Artifacts/ParallelQA/20260826T053000Z_gamejam_search_node_integrated_green/gamejam-search-node-summary.json
- task.design.gamejam-seven-region-search-node-contract: Docs/Design/gamejam-seven-region-search-node-contract.md
- task.design.gamejam-seven-region-search-node-contract: .forge/packets/gamejam-seven-region-search-node-contract.json
- task.design.gamejam-completion-matrix: Docs/Design/gamejam-completion-matrix.md
- task.design.gamejam-completion-matrix: .forge/packets/gamejam-completion-matrix-design.json
- task.design.gamejam-completion-matrix: Artifacts/ParallelQA/20260825T160510Z_gamejam_search_node_integrated/gamejam-search-node-summary.json
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
- task.qa.feature.dual-input: Artifacts/ParallelQA/20260827T180000Z_gamejam_wavec_3d64403/gamejam-wave-c-play-contracts.json
- task.qa.feature.dual-input: Artifacts/ParallelQA/pkg_2c9f36a_verified/gamejam-package-integrity-summary.json
- task.qa.feature.dual-input: Docs/QA/gamejam-final-windows-candidate-30m-human-checklist-ko.md
- task.qa.feature.dual-input: Docs/Design/Playtest/gamejam-seven-region-search-node-results.md
- task.art.animation.mr-kim.swim: Assets/_Project/Art/Generated/sprite_animation/job_20260822091448_251bc2a1/quality-report.json
- task.art.animation.mr-kim.swim: Assets/_Project/Art/Generated/sprite_animation/job_20260824152541_e93cada7/quality-report.json
- task.art.animation.mr-kim.swim: Assets/_Project/Art/Generated/sprite_animation/job_20260824152541_e93cada7/mr-kim-swim-visual-qa.json
- task.art.environment.expedition-region-kit: Assets/_Project/Art/Generated/separated_parts/job_20260824152619_30c0f7dd/quality-report.json
- task.art.environment.expedition-region-kit: Assets/_Project/Art/Generated/separated_parts/job_20260824152619_30c0f7dd/expedition-region-kit-visual-qa.json
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
- task.qa.feature.inventory-capacity-upgrade: Artifacts/ParallelQA/20260827T180000Z_gamejam_wavec_3d64403/gamejam-wave-c-play-contracts.json
- task.qa.feature.inventory-capacity-upgrade: Artifacts/ParallelQA/pkg_2c9f36a_verified/gamejam-package-integrity-summary.json
- task.qa.feature.inventory-capacity-upgrade: Docs/QA/gamejam-final-windows-candidate-30m-human-checklist-ko.md
- task.qa.feature.inventory-capacity-upgrade: Docs/Design/Playtest/gamejam-seven-region-search-node-results.md
- task.design.wave7-bag-capacity-balance: Docs/Design/wave7-bag-capacity-upgrade.md#4-업그레이드-전-가방-선택-보존-검증
- task.design.wave7-bag-capacity-balance: Docs/Design/wave7-bag-capacity-upgrade.md#6-w2d1-구매-포함-자연-3일-구조-장부
- task.design.wave7-bag-capacity-balance: Artifacts/ParallelQA/20260823T004700Z_642a73c_wave6_integrated/wave6-edit-contracts.txt
- task.art.ui.escape-project-progress: Forge asset ui.escape-project-progress adopted via job_20260823160324_1de3b748
- task.art.ui.ending-comic: Forge asset ui.ending-comic adopted via job_20260823160342_eceb3933
- task.art.effect.survival-hazards: Forge asset effect.survival-hazards adopted via job_20260823160305_ef04b0f3
- task.qa.wave15-hazard-ending-redfirst: Artifacts/Verification/wave19-integrated-96af07d/wave19-summary.json
- task.design.wave19-resource-playtest-contract: Artifacts/Verification/resource-integrated-playtest/README.md
- task.design.wave19-resource-playtest-contract: Artifacts/Verification/resource-integrated-playtest/wave18-summary.json
- task.design.wave19-resource-playtest-contract: Artifacts/Verification/resource-integrated-playtest/wave14-qps-global-layout-gate.json
- task.gamejam.long-stay-two-endings: Docs/Design/kim-survival-island-gdd.md#17-게임잼-범위
- task.gamejam.long-stay-two-endings: Docs/Design/gamejam-completion-matrix.md#2-제출-완료-매트릭스
- task.gamejam.long-stay-two-endings: Artifacts/ParallelQA/20260826T174500Z_gamejam_long_stay_integrated/gamejam-long-stay-summary.json
- task.art.icon.resource-tool-set: Forge asset icon.resource-tool-set adopted via job_20260822141317_caf8e11d
- task.art.effect.comedy-feedback: Forge asset effect.comedy-feedback adopted via job_20260822224357_275de712
- task.art.ui.ending-gallery: Forge asset ui.ending-gallery adopted via job_20260824133802_f43c6431
- task.system.system.ending-gallery: Artifacts/Verification/wave19-integrated-96af07d/wave19-summary.json
- task.feature.feature.ending-gallery: Artifacts/Verification/wave19-integrated-96af07d/wave19-summary.json
- task.qa.feature.ending-gallery: Artifacts/Verification/wave19-integrated-96af07d/wave19-summary.json
- task.postslice.steam-release-readiness: Artifacts/ParallelQA/20260822T113642Z_e695c36/steam-readiness-audit.txt
- task.postslice.steam-release-readiness: Artifacts/ParallelQA/20260822T113642Z_e695c36/windows-development-build.txt

## 차단 요소

- task.gamejam.qa-thirty-minute-seven-region-slice: 현재 미커밋 작업 트리의 전체 통합 게이트·Windows build/smoke·package integrity·release manifest가 먼저 필요하다. 이후 정확한 새 후보에서 GJC-17 실제 시간과 GJC-23 KO 3명+EN 3명 세션을 수행한다.
- task.qa.feature.dual-input: 새 후보의 자동 선행 게이트 뒤 GJC-20 물리 XInput 또는 Steam Input 호환 게임패드 실기가 필요하다.
- task.qa.feature.inventory-capacity-upgrade: 새 후보의 자동 선행 게이트 뒤 GJC-12 fresh-user 4→6칸 선택 압박 관찰이 필요하다.

## 미결 질문

- 없음

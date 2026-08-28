# 김씨 생존기: 무인도 프로토타입 현황

폭풍에 표류해 무인도에 고립된 평범한 김씨가 환경 수색물을 뒤져 필요한 것만 챙기고 다층 캠프와 세 탈출 프로젝트에 투자해 자신의 생활 방식이 반영된 코믹북 결말에 도달하는 2D 생존 게임

## 버티컬 슬라이스

폭풍 표류 도입부터 실제 아트·애니메이션·UI·오디오가 연결된 30분 게임잼 콘텐츠 베타까지 완성한다.

## 실행 단계

| 단계 | 상태 | 메모 |
|---|---|---|
| design | complete | O11 P0 2/P1 5 completion conditions and evidence boundaries are reconciled at integration HEAD 2c88100b. |
| art | in_progress | O11 production blocker complete: user-adopted seven regions and six search nodes are packaged/formally connected; ladder and swim now use polished four-frame runtime strips. Broader game-jam ending/audio art remains outside this candidate. |
| implementation | in_progress | O11 exact source 1344f6c2 passes all seven product checks and produced a non-development Windows x64 candidate. Broader content-beta remains in review pending human 30-minute play. |
| verification | in_progress | O11 automated product gate 7/7, compile infrastructure, non-development build, original smoke and extracted-ZIP smoke pass. Human natural 30-minute and physical gamepad verification remain. |

## 다음 작업

- **O1 P1 교정 통합·새 후보 재검증** (qa, critical) — task.gamejam.o1-corrective-integration-retest
- **김씨 설비 사용 애니메이션 제작** (art, critical) — task.art.animation.mr-kim.facility-use
- **김씨 idle 애니메이션 제작** (art, critical) — task.art.animation.mr-kim.idle
- **김씨 사다리 등반 애니메이션 제작** (art, critical) — task.art.animation.mr-kim.ladder
- **김씨 뒤지기 애니메이션 제작** (art, critical) — task.art.animation.mr-kim.search

## 작업

| ID | 레인 | 우선순위 | 상태 | 작업 |
|---|---|---|---|---|
| task.system.system.run-state | implementation | critical | done | 게임잼 캠페인·지역·다층 캠프 상태 구현 |
| task.system.system.phase-flow | implementation | critical | done | 30분 조기 탈출·장기 체류 흐름 구현 |
| task.system.system.inventory | implementation | critical | done | 자원·도구·컴팩트 가방 구현 |
| task.system.system.crafting-tech | implementation | critical | done | 제작법과 연구 구현 |
| task.system.system.camp-structures | implementation | critical | done | 다층 캠프 설비·사다리·수직 카메라 구현 |
| task.system.system.island-search | implementation | critical | done | 7지역 유한 수색과 변화 기록 구현 |
| task.feature.feature.phase-cycle | implementation | critical | done | 50일 캠프·수색·정산 주기 구현 |
| task.qa.feature.phase-cycle | qa | critical | done | 50일 캠프·수색·정산 주기 검증 |
| task.feature.feature.inventory-choice | implementation | critical | done | 컴팩트 상시 가방 선택 구현 |
| task.qa.feature.inventory-choice | qa | critical | done | 컴팩트 상시 가방 선택 검증 |
| task.feature.feature.crafting-research | implementation | critical | done | 제작과 간단한 연구 구현 |
| task.qa.feature.crafting-research | qa | critical | done | 제작과 간단한 연구 검증 |
| task.feature.feature.camp-building | implementation | critical | done | 하이브리드 베이스캠프 건설과 자유 배치 구현 |
| task.qa.feature.camp-building | qa | critical | done | 하이브리드 베이스캠프 건설과 자유 배치 검증 |
| task.feature.feature.island-exploration | implementation | critical | done | 선택 지역 횡스크롤 수색 구현 |
| task.qa.feature.island-exploration | qa | critical | done | 선택 지역 횡스크롤 수색 검증 |
| task.system.system.camp-placement | implementation | critical | done | 다층 하이브리드 자유 배치 구현 |
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
| task.system.system.expedition-selection | implementation | critical | done | 순차 해금 수집 지도와 지역 선택 구현 |
| task.system.system.region-loot-rng | implementation | critical | done | 단계 보장형 7지역 유한 loot·부품 softlock 보호 구현 |
| task.system.system.hazard-director | implementation | critical | done | 벌레·야생동물·위험 식물·질병 디렉터 구현 |
| task.system.system.escape-projects | implementation | critical | done | 유한 재료형 뗏목·연기·무전 프로젝트 구현 |
| task.system.system.ending-resolution | implementation | critical | done | 탈출·Day 20 장기 체류 판정 구현 |
| task.feature.feature.expedition-map | implementation | critical | done | 순차 해금형 7지역 수집 지도 구현 |
| task.qa.feature.expedition-map | qa | critical | done | 순차 해금형 7지역 수집 지도 검증 |
| task.feature.feature.resource-randomization | implementation | critical | done | 단계 보장형 7지역 유한 자원·핵심 부품 분배 구현 |
| task.qa.feature.resource-randomization | qa | critical | done | 단계 보장형 7지역 유한 자원·핵심 부품 분배 검증 |
| task.feature.feature.survival-hazards | implementation | critical | done | 지역 위험도·질병 생존 위험 구현 |
| task.qa.feature.survival-hazards | qa | critical | done | 지역 위험도·질병 생존 위험 검증 |
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
| task.feature.feature.camp-module-expansion | implementation | critical | done | 자유 배치형 2층·지하실 다층 캠프 확장 구현 |
| task.qa.feature.camp-module-expansion | qa | critical | done | 자유 배치형 2층·지하실 다층 캠프 확장 검증 |
| task.feature.feature.behavioral-endings | implementation | critical | done | 탈출·장기 체류 행동 기반 엔딩 구현 |
| task.qa.feature.behavioral-endings | qa | critical | done | 탈출·장기 체류 행동 기반 엔딩 검증 |
| task.system.system.region-persistence | implementation | critical | done | 지역별 유한 월드 상태 구현 |
| task.feature.feature.region-persistence | implementation | critical | done | 수색 지역 영구 변화 구현 |
| task.qa.feature.region-persistence | qa | critical | done | 수색 지역 영구 변화 검증 |
| task.gamejam.seven-region-catalog | implementation | critical | done | 게임잼 7지역 42-node 카탈로그 확장 |
| task.gamejam.persistent-region-runtime | implementation | critical | done | 게임잼 지역 영속 상태·유한 자원 런타임 |
| task.gamejam.smoke-radio-material-routes | implementation | critical | done | 게임잼 부싯돌·무전 3부품·pity·seed 감사 |
| task.gamejam.insect-plant-wildlife-disease | implementation | critical | done | 게임잼 환경 위험·jungle-fever lifecycle |
| task.gamejam.upper-basement-both | implementation | critical | done | 게임잼 2층·지하실 동시 확장 |
| task.gamejam.qa-thirty-minute-seven-region-slice | qa | critical | blocked | 게임잼 Wave B/C 42-node·30분 통합 검증 |
| task.system.system.search-node-loot | implementation | critical | done | 결정론적 수색 오브젝트·발견물 transaction 구현 |
| task.feature.feature.searchable-resource-nodes | implementation | critical | done | 환경 수색 오브젝트·발견물 선별 구현 |
| task.qa.feature.searchable-resource-nodes | qa | critical | done | 환경 수색 오브젝트·발견물 선별 검증 |
| task.system.system.raft-escape | implementation | critical | done | 진수대 단일 팝업 단계·출항 상태기계 구현 |
| task.feature.feature.raft-escape | implementation | critical | done | 설명 가능한 뗏목 제작·해안 진수 탈출 구현 |
| task.qa.feature.raft-escape | qa | critical | done | 설명 가능한 뗏목 제작·해안 진수 탈출 검증 |
| task.design.gamejam-seven-region-search-node-contract | design | critical | done | 게임잼 7지역 수색 node·유한 자원 계약 |
| task.design.gamejam-completion-matrix | design | critical | done | GAME JAM 제출 완료 기준 감사 |
| task.gamejam.o1-ui-resource-semantics | implementation | critical | done | O1 UI·재료 의미 일관성 교정 |
| task.gamejam.o1-camp-expansion-ux | implementation | critical | done | O1 다층 캠프 증축 UX 교정 |
| task.gamejam.o1-character-ground-animation | implementation | critical | done | O1 김씨 지면 접지·지상 애니메이션 교정 |
| task.gamejam.o1-corrective-integration-retest | qa | critical | in_progress | O1 P1 교정 통합·새 후보 재검증 |
| task.gamejam.o2-bag-swap-visibility-p0 | implementation | critical | done | O2 가방 교체 UI 진행 불가 교정 |
| task.gamejam.anchor-shelter-placement | implementation | critical | done | 고정 설치 지점형 다층 쉘터 배치 |
| task.gamejam.anchor-shelter-roof-living-furniture | implementation | critical | done | 1층 지붕·위층·지하·침대·소파 |
| task.gamejam.o2-anchor-shelter-integration-retest | qa | critical | review | O2 P0·고정 쉘터 통합 재검증 |
| task.gamejam.o4-stable-resource-ledger-p0 | implementation | critical | ready | O3 표시·소모 자원 단일 원장화 |
| task.gamejam.o4-three-route-discoverability-p0 | implementation | critical | planned | 세 탈출 경로 발견성과 상태 안내 |
| task.gamejam.o4-natural-resource-budget-p0 | qa | critical | planned | 유한 수색 자원·세 탈출 경로 가능성 검증 |
| task.gamejam.o4-shore-launch-popup-p1 | implementation | critical | planned | 해안 진수대 단계·진척·비용 UI 재구성 |
| task.gamejam.o4-ladder-camera | implementation | critical | ready | 사다리 직접 등반과 수직 카메라 |
| task.gamejam.o4-integration-release | qa | critical | planned | O4 통합·Windows 후보 동결 |
| task.art.ui.gamejam.interface-kit | art | critical | review | 게임잼 통합 UI 컴포넌트 세트 제작 |
| task.art.background.expedition-seven-region-set | art | critical | done | 일곱 수색 지역 production 배경 세트 제작 |
| task.art.object.searchable-resource-node-production-set | art | critical | done | 환경 수색 오브젝트 production 세트 제작 |
| task.art.ui.interactable-marker-set | art | critical | review | 공통 상호작용 표식 production 세트 제작 |
| task.gamejam.o5-triple-resource-budget-p0 | implementation | critical | done | O4-H1 실측 기반 일반 수색 자원 3배 |
| task.gamejam.o5-region-remaining-map-p1 | implementation | critical | done | 수집 지도 지역별 자원 잔량 퍼센트 |
| task.gamejam.o5-staged-escape-facilities-p1 | implementation | critical | done | 탈출 설비 후행 건설·등장 |
| task.gamejam.o5-spaced-camp-icon-affordance-p1 | implementation | critical | done | 캠프 재배치와 공통 상호작용 아이콘 |
| task.gamejam.o5-art-production-wave | art | critical | review | 게임잼 UI·7지역·수색 오브젝트·탈출 설비 아트 제작 |
| task.gamejam.o5-integration-release | qa | critical | review | O5 통합·자연 경로·Windows 후보 |
| task.gamejam.o6-camp-modal-furniture | implementation | critical | done | 증축 발견성과 캠프 팝업 연속성·다층 가구 |
| task.gamejam.o6-search-bag-node-density | implementation | critical | done | 상시 가방·전체 발견물·4→10 성장·84 node |
| task.gamejam.o6-world-scale-layout | art | critical | review | 넓은 카메라와 작은 캐릭터·캠프 설비 배치 |
| task.gamejam.o6-raft-terminal-parts | implementation | critical | done | 뗏목 출항 완료 흐름과 핵심 부품 0/1 |
| task.gamejam.o6-integration-release | qa | critical | review | O6 병렬 통합·전체 회귀·Windows 후보 |
| task.gamejam.o7-camp-expansion-feedback-furniture | implementation | critical | done | O7 캠프 증축 표식·팝업 피드백·다층 설비 |
| task.gamejam.o7-search-space-resource-economy | implementation | critical | done | O7 수색 공간·나무 경제·지역 자원 가중치 |
| task.gamejam.o7-bag-survival-onboarding | implementation | critical | done | O7 컴팩트 가방·생존 상태 온보딩 |
| task.gamejam.o7-integration-release | qa | critical | done | O7 통합·회귀·후보 빌드 |
| task.gamejam.o8-sequential-unlock-early-economy | implementation | critical | done | O8 순차 지역 해금·초반 수집 경제 |
| task.gamejam.o8-compact-bag-raft-feedback | implementation | critical | done | O8 초소형 가방·진수대 출항 피드백 |
| task.gamejam.o8-hybrid-free-placement | implementation | critical | done | O8 일반 설비 다층 자유 배치 |
| task.gamejam.o8-integration-release | qa | critical | review | O8 통합·회귀·후보 빌드 |
| task.art.character.mr-kim | art | critical | done | 김씨 production model과 기준 포즈 제작 |
| task.art.object.camp-structures | art | critical | done | 캠프 생활·제작 설비 production 세트 제작 |
| task.art.background.modular-island-camp | art | critical | review | 게임잼 production 다층 캠프 제작 |
| task.art.object.escape-project-build-set | art | critical | review | 세 탈출 프로젝트 production 설비 세트 제작 |
| task.art.ui.gamejam-title-opening | art | critical | ready | 타이틀·도입·크레딧 UI 세트 제작 |
| task.art.animation.mr-kim.idle | art | critical | ready | 김씨 idle 애니메이션 제작 |
| task.art.animation.mr-kim.walk | art | critical | ready | 김씨 걷기 애니메이션 제작 |
| task.art.animation.mr-kim.search | art | critical | ready | 김씨 뒤지기 애니메이션 제작 |
| task.art.animation.mr-kim.facility-use | art | critical | ready | 김씨 설비 사용 애니메이션 제작 |
| task.art.animation.mr-kim.ladder | art | critical | ready | 김씨 사다리 등반 애니메이션 제작 |
| task.art.icon.gamejam-item-resource-part-set | art | critical | ready | 게임잼 자원·부품·도구 아이콘 19종 제작 |
| task.art.ending.comic-core-five-set | art | critical | ready | 게임잼 core ending 5종 코믹 패널 제작 |
| task.system.system.narrative-presentation | implementation | critical | ready | 도입·진행 비트 프레젠테이션 구현 |
| task.system.system.character-animation-presentation | implementation | critical | ready | 김씨 애니메이션 상태기 구현 |
| task.feature.feature.gamejam-narrative-framing | implementation | critical | ready | 게임잼 도입과 진행 서사 구현 |
| task.qa.feature.gamejam-narrative-framing | qa | critical | planned | 게임잼 도입과 진행 서사 검증 |
| task.feature.feature.gamejam-presentation-shell | implementation | critical | review | 게임잼 최종 방향 UI와 월드 프레젠테이션 구현 |
| task.qa.feature.gamejam-presentation-shell | qa | critical | planned | 게임잼 최종 방향 UI와 월드 프레젠테이션 검증 |
| task.feature.feature.gamejam-character-animation | implementation | critical | review | 김씨 핵심 행동 애니메이션 구현 |
| task.qa.feature.gamejam-character-animation | qa | critical | planned | 김씨 핵심 행동 애니메이션 검증 |
| task.feature.feature.gamejam-authored-endings | implementation | critical | ready | 게임잼 제작 엔딩 묶음 구현 |
| task.qa.feature.gamejam-authored-endings | qa | critical | planned | 게임잼 제작 엔딩 묶음 검증 |
| task.o9.design.content-lock | design | critical | done | O9 시놉시스·도입·엔딩 beat·아트 범위 잠금 |
| task.o9.art.style-benchmark | art | critical | review | O9 김씨·캠프·해변·HUD style benchmark 채택 |
| task.o9.art.first-loop-production | art | critical | review | O9 첫 루프 production UI·월드·아이템 |
| task.o9.art.character-core-animation | art | critical | ready | O9 김씨 core animation production |
| task.o9.implementation.presentation-alpha | implementation | critical | review | O9 콘텐츠·프레젠테이션 알파 통합 |
| task.o9.qa.first-loop-presentation | qa | critical | review | O9 5~10분 first-loop 콘텐츠·프레젠테이션 검증 |
| task.o10.art.content-beta-production | art | critical | review | O10 일곱 지역·다층 캠프·세 탈출·필수 엔딩 production |
| task.o10.implementation.content-beta | implementation | critical | review | O10 30분 콘텐츠 베타 통합 |
| task.o10.qa.thirty-minute-content-beta | qa | critical | review | O10 30분 자연 플레이 재개 게이트 |
| task.art.background.island-camp | art | high | done | 무인도 베이스캠프 배경 제작 |
| task.art.background.coast-forest | art | high | review | 해변·숲 수색 구역 제작 |
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
| task.art.ui.camp-module-expansion | art | high | review | 방 모듈 증축 상태 UI 제작 |
| task.art.ui.escape-project-progress | art | high | done | 다중 탈출 프로젝트 상태 UI 제작 |
| task.art.ui.ending-comic | art | high | done | 엔딩 코믹북 컷신 프레임 제작 |
| task.art.effect.survival-hazards | art | high | done | 생존 위험·완화 피드백 세트 제작 |
| task.qa.wave15-hazard-ending-redfirst | qa | high | done | Wave 15 위험·탈출·엔딩 red-first 게이트 |
| task.design.wave19-resource-playtest-contract | design | high | done | Wave 19 리소스 통합 첫 사용자 플레이테스트 계약 |
| task.gamejam.long-stay-two-endings | implementation | high | done | 게임잼 장기 체류 엔딩 2종 |
| task.art.object.searchable-resource-node-kit | art | high | done | 7지역 환경 수색 오브젝트 키트 제작 |
| task.art.ui.search-loot-tray | art | high | review | 발견물 선별 트레이 UI 제작 |
| task.art.object.camp-ladder | art | high | review | 표류물 사다리와 지하 해치 세트 제작 |
| task.gamejam.o4-ladder-art-review | art | high | review | 사다리·지하 해치 오리지널 아트 후보 |
| task.art.animation.mr-kim.hurt-sick | art | high | ready | 김씨 부상·질병·탈진 반응 세트 제작 |
| task.art.animation.mr-kim.rest-eat | art | high | ready | 김씨 휴식·식사 회복 애니메이션 제작 |
| task.art.ending.comic-variant-panel-set | art | high | ready | 코믹·희귀·행동 modifier 교체 패널 제작 |
| task.system.system.gamejam-audio-presentation | implementation | high | ready | 게임잼 기능 오디오 구현 |
| task.o9.art.ending-preview-audio | art | high | ready | O9 대표 엔딩 1종과 첫 루프 기능 오디오 |
| task.o11.art.replayability-variants | art | high | ready | O11 코믹·희귀 변형과 행동 modifier panel |
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
- background.modular-island-camp: review · 게임잼 production 다층 캠프
- ui.camp-module-expansion: review · 방 모듈 증축 상태 UI
- icon.expedition-resource-risk-set: review · 수집 자원·위험·날씨 아이콘 세트
- environment.expedition-region-kit: review · 7지역 지속 수색 환경 키트
- ui.search-loot-tray: review · 발견물 선별 트레이 UI
- object.camp-ladder: review · 표류물 사다리와 지하 해치 세트
- ui.gamejam.interface-kit: review · 게임잼 통합 UI 컴포넌트 세트
- ui.interactable-marker-set: review · 공통 상호작용 표식 production 세트
- object.escape-project-build-set: review · 세 탈출 프로젝트 production 설비 세트
- ui.gamejam-title-opening: needed · 타이틀·도입·크레딧 UI 세트
- animation.mr-kim.idle: needed · 김씨 idle 애니메이션
- animation.mr-kim.walk: needed · 김씨 걷기 애니메이션
- animation.mr-kim.search: needed · 김씨 뒤지기 애니메이션
- animation.mr-kim.facility-use: needed · 김씨 설비 사용 애니메이션
- animation.mr-kim.ladder: needed · 김씨 사다리 등반 애니메이션
- animation.mr-kim.hurt-sick: needed · 김씨 부상·질병·탈진 반응 세트
- animation.mr-kim.rest-eat: needed · 김씨 휴식·식사 회복 애니메이션
- icon.gamejam-item-resource-part-set: needed · 게임잼 자원·부품·도구 아이콘 19종
- ending.comic-core-five-set: needed · 게임잼 core ending 5종 코믹 패널
- ending.comic-variant-panel-set: needed · 코믹·희귀·행동 modifier 교체 패널

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
- task.gamejam.qa-thirty-minute-seven-region-slice: Docs/Design/Playtest/Sessions/O1-2026-08-26.md
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
- task.design.gamejam-completion-matrix: Artifacts/ParallelQA/20260826T223000Z_gamejam_waveb_2bdea61/gamejam-wave-b-summary.json
- task.design.gamejam-completion-matrix: Artifacts/ParallelQA/20260826T220000Z_gamejam_wavec_2bdea61/gamejam-wave-c-summary.json
- task.design.gamejam-completion-matrix: Artifacts/ParallelQA/20260826T233000Z_gamejam_package_2bdea61/gamejam-package-integrity-summary.json
- task.gamejam.o1-ui-resource-semantics: Docs/Design/Playtest/Sessions/O1-2026-08-26.md
- task.gamejam.o1-ui-resource-semantics: Docs/Design/gamejam-o1-human-p1-corrective-wave.md
- task.gamejam.o1-ui-resource-semantics: Artifacts/ParallelQA/20260826T214500Z_o1_p1_integrated_r11/gamejam-search-node-play-contracts.txt
- task.gamejam.o1-ui-resource-semantics: Artifacts/ParallelQA/20260826T214500Z_o1_p1_integrated_r11/wave19-play-contracts.txt
- task.gamejam.o1-camp-expansion-ux: Docs/Design/Playtest/Sessions/O1-2026-08-26.md
- task.gamejam.o1-camp-expansion-ux: Docs/Design/gamejam-o1-human-p1-corrective-wave.md
- task.gamejam.o1-camp-expansion-ux: Artifacts/ParallelQA/20260826T214500Z_o1_p1_integrated_r11/wave11-slot-play-contracts.txt
- task.gamejam.o1-character-ground-animation: Docs/Design/Playtest/Sessions/O1-2026-08-26.md
- task.gamejam.o1-character-ground-animation: Docs/Design/gamejam-o1-human-p1-corrective-wave.md
- task.gamejam.o1-character-ground-animation: Artifacts/ParallelQA/20260826T214500Z_o1_p1_integrated_r11/wave19-play-contracts.txt
- task.gamejam.o1-corrective-integration-retest: Docs/Design/gamejam-o1-human-p1-corrective-wave.md
- task.gamejam.o1-corrective-integration-retest: Artifacts/ParallelQA/20260826T214500Z_o1_p1_integrated_final/compile-result.txt
- task.gamejam.o1-corrective-integration-retest: Artifacts/ParallelQA/20260826T214500Z_o1_p1_integrated_r11/gamejam-wave-c-play-contracts.txt
- task.gamejam.o1-corrective-integration-retest: Artifacts/ParallelQA/20260826T214500Z_o1_p1_integrated_r11/gamejam-search-node-play-contracts.txt
- task.gamejam.o1-corrective-integration-retest: Artifacts/ParallelQA/20260826T214500Z_o1_p1_integrated_r11/wave11-slot-play-contracts.txt
- task.gamejam.o1-corrective-integration-retest: Artifacts/ParallelQA/20260826T214500Z_o1_p1_integrated_r11/wave19-play-contracts.txt
- task.gamejam.o2-bag-swap-visibility-p0: Docs/Design/Playtest/Sessions/O2-2026-08-26.md
- task.gamejam.o2-bag-swap-visibility-p0: Artifacts/ParallelQA/manual-wave9/o2-p0-bag-swap-ko-1280x800.png
- task.gamejam.o2-bag-swap-visibility-p0: Artifacts/ParallelQA/manual-wave9/wave9-play-contracts.txt
- task.gamejam.anchor-shelter-placement: Docs/Design/gamejam-o2-p0-anchor-shelter-rebaseline.md
- task.gamejam.anchor-shelter-placement: Artifacts/ParallelQA/manual-wave9/o2-anchor-shelter-cutaway-ko-1280x800.png
- task.gamejam.anchor-shelter-placement: Artifacts/ParallelQA/manual-wave9/wave9-play-contracts.txt
- task.gamejam.anchor-shelter-roof-living-furniture: Docs/Design/gamejam-o2-p0-anchor-shelter-rebaseline.md
- task.gamejam.anchor-shelter-roof-living-furniture: Artifacts/ParallelQA/manual-wave9/o2-anchor-shelter-cutaway-ko-1280x800.png
- task.gamejam.anchor-shelter-roof-living-furniture: Artifacts/ParallelQA/manual-wave9/wave9-play-contracts.txt
- task.gamejam.o2-anchor-shelter-integration-retest: Docs/Design/gamejam-o2-p0-anchor-shelter-rebaseline.md
- task.gamejam.o2-anchor-shelter-integration-retest: Artifacts/ParallelQA/manual-wave9/wave9-play-contracts.txt
- task.gamejam.o2-anchor-shelter-integration-retest: Artifacts/Verification/windows-build.txt
- task.gamejam.o2-anchor-shelter-integration-retest: Builds/Windows/KimSurvivalIsland.exe
- task.art.background.expedition-seven-region-set: Forge asset background.expedition-seven-region-set adopted via job_20260826165624_448aecdc
- task.art.object.searchable-resource-node-production-set: Forge asset object.searchable-resource-node-production-set adopted via job_20260826165625_8961b353
- task.gamejam.o5-triple-resource-budget-p0: Artifacts/ParallelQA/20260827T023500Z_o5_seed_regression_r4/o4-stable-resource-escape-seed-report.json
- task.gamejam.o5-region-remaining-map-p1: Artifacts/ParallelQA/20260827T024000Z_o5_human_correction_r2/o5-human-resource-art-correction-report.json
- task.gamejam.o5-region-remaining-map-p1: Artifacts/ParallelQA/20260827T030500Z_o5_runtime_visual_r4/o5-human-correction-play.txt
- task.gamejam.o5-staged-escape-facilities-p1: Artifacts/ParallelQA/20260827T024000Z_o5_human_correction_r2/o5-human-resource-art-correction-report.json
- task.gamejam.o5-staged-escape-facilities-p1: Artifacts/ParallelQA/20260827T030500Z_o5_runtime_visual_r4/o5-human-correction-play.txt
- task.gamejam.o5-spaced-camp-icon-affordance-p1: Artifacts/ParallelQA/20260827T030500Z_o5_runtime_visual_r4/o5-human-correction-play.txt
- task.gamejam.o5-art-production-wave: .forge/assets.json
- task.gamejam.o5-integration-release: Artifacts/ParallelQA/20260827T024000Z_o5_human_correction_r2/o5-human-resource-art-correction-report.json
- task.gamejam.o5-integration-release: Artifacts/ParallelQA/20260827T023500Z_o5_seed_regression_r4/o4-stable-resource-escape-seed-report.json
- task.gamejam.o5-integration-release: Artifacts/ParallelQA/20260827T030500Z_o5_runtime_visual_r4/o5-human-correction-play.txt
- task.gamejam.o5-integration-release: Artifacts/ParallelQA/20260827T033000Z_o5_release_553cbc5/gamejam-release-build.json
- task.gamejam.o6-camp-modal-furniture: Artifacts/ParallelQA/20260827T083500Z_o6_integrated_root/camp/o6-camp-modal-furniture-edit.txt
- task.gamejam.o6-camp-modal-furniture: Artifacts/ParallelQA/20260827T083500Z_o6_integrated_root/camp/o6-camp-modal-furniture-play.txt
- task.gamejam.o6-search-bag-node-density: Artifacts/ParallelQA/20260827T083500Z_o6_integrated_root/search/o6-search-bag-resource-report.json
- task.gamejam.o6-search-bag-node-density: Artifacts/ParallelQA/o6-search-bag-play/o6-search-bag-play.txt
- task.gamejam.o6-world-scale-layout: Artifacts/ParallelQA/20260827T083500Z_o6_integrated_root/world/o6-world-presentation-edit.txt
- task.gamejam.o6-world-scale-layout: Artifacts/ParallelQA/20260827T083500Z_o6_integrated_root/world/o6-world-presentation-play.txt
- task.gamejam.o6-raft-terminal-parts: Artifacts/ParallelQA/O6RaftTerminalParts/o6-raft-terminal-parts-report.json
- task.gamejam.o6-integration-release: Artifacts/ParallelQA/20260827T083500Z_o6_integrated_root
- task.gamejam.o6-integration-release: Artifacts/ParallelQA/20260827T090500Z_o6_release_d10d7c3/gamejam-release-build.json
- task.gamejam.o6-integration-release: Artifacts/ParallelQA/20260827T091500Z_o6_package_d10d7c3/gamejam-package-integrity-summary.json
- task.gamejam.o6-integration-release: Docs/Design/Playtest/Sessions/O6-H1-2026-08-27.md
- task.gamejam.o7-camp-expansion-feedback-furniture: Artifacts/ParallelQA/20260827T132000Z_o7_integrated_r1/camp/o7-camp-correction-edit.txt
- task.gamejam.o7-camp-expansion-feedback-furniture: Artifacts/ParallelQA/20260827T132000Z_o7_integrated_r1/camp/o7-camp-correction-play.txt
- task.gamejam.o7-search-space-resource-economy: Artifacts/ParallelQA/20260827T132000Z_o7_integrated_r1/search/o7-search-space-economy-report.json
- task.gamejam.o7-bag-survival-onboarding: Artifacts/ParallelQA/20260827T132000Z_o7_integrated_r1/bag-survival/o7-bag-survival-edit.txt
- task.gamejam.o7-bag-survival-onboarding: Artifacts/ParallelQA/20260827T132000Z_o7_integrated_r1/bag-survival/o7-bag-survival-play.txt
- task.gamejam.o7-integration-release: Artifacts/ParallelQA/20260827T132000Z_o7_integrated_r1/compile-result.txt
- task.gamejam.o7-integration-release: Artifacts/ParallelQA/20260827T134000Z_o7_release_1df5c7d/gamejam-release-build.json
- task.gamejam.o7-integration-release: Artifacts/ParallelQA/20260827T134500Z_o7_package_1df5c7d/gamejam-package-integrity-summary.json
- task.gamejam.o8-sequential-unlock-early-economy: Docs/Design/Playtest/Sessions/O8-Implementation-Verification-2026-08-28.md
- task.gamejam.o8-sequential-unlock-early-economy: Artifacts/ParallelQA/O7SearchSpaceEconomy/o7-search-space-economy-report.json
- task.gamejam.o8-compact-bag-raft-feedback: Docs/Design/Playtest/Sessions/O8-Implementation-Verification-2026-08-28.md
- task.gamejam.o8-compact-bag-raft-feedback: work/ParallelQA/20260827T153000Z_o8_candidate_6d03413/bag/o7-bag-survival-play.txt
- task.gamejam.o8-compact-bag-raft-feedback: Artifacts/ParallelQA/O6RaftTerminalParts/o6-raft-terminal-parts-report.json
- task.gamejam.o8-hybrid-free-placement: Docs/Design/Playtest/Sessions/O8-Implementation-Verification-2026-08-28.md
- task.gamejam.o8-hybrid-free-placement: work/ParallelQA/20260827T153000Z_o8_candidate_6d03413/camp/o7-camp-correction-play.txt
- task.gamejam.o8-integration-release: Docs/Design/Playtest/Sessions/O8-Implementation-Verification-2026-08-28.md
- task.gamejam.o8-integration-release: Artifacts/ParallelQA/20260827T161000Z_o8_release_03dae26/gamejam-release-build.json
- task.gamejam.o8-integration-release: work/ParallelQA/20260827T153700Z_o8_h1_owner/o8-h1-player.log
- task.art.character.mr-kim: Forge asset character.mr-kim adopted via job_20260822085926_374033c5
- task.art.object.camp-structures: Forge asset object.camp-structures adopted and packaged via job_20260822130400_6d786a69
- task.art.animation.mr-kim.ladder: Assets/_Project/Art/Runtime/Resources/O11/mr-kim-ladder-strip-v2.png
- task.art.animation.mr-kim.ladder: Artifacts/ParallelQA/20260829T011500Z_o11_adopted_traversal_visuals/o11-runtime-visual-validation.json
- task.art.animation.mr-kim.ladder: Docs/QA/o11-formal-candidate-report.md
- task.feature.feature.gamejam-presentation-shell: commit 2c88100b1cb103ba7d603177dfbbad286729cdf6
- task.feature.feature.gamejam-presentation-shell: adopted V2 grammar job_20260828122852_c9ccf2aa
- task.feature.feature.gamejam-character-animation: commit 2c88100b1cb103ba7d603177dfbbad286729cdf6
- task.o9.design.content-lock: User locked storm drift and ink-Kim simplified-world direction; document defines stable KO/EN beats for opening, first loop, seven regions, five core endings and three variants.
- task.o9.art.style-benchmark: Docs/Art/O9/o9-style-benchmark-review.md
- task.o9.art.style-benchmark: 사용자 ADOPT · Forge job_20260828122852_c9ccf2aa engine_ready · 큰 색면 배경/얇은 HUD/좌측 하단 가방/중앙 플레이 공간 기준
- task.o9.art.style-benchmark: Forge feedback adopted and Unity package manifest generated
- task.o9.art.first-loop-production: Docs/Art/O9/o9-style-benchmark-review.md
- task.o9.art.character-core-animation: Artifacts/ParallelQA/20260828T120000Z_o9_o10/source-compile-result.txt
- task.o9.implementation.presentation-alpha: Artifacts/ParallelQA/20260828T120000Z_o9_o10/source-compile-result.txt
- task.o9.qa.first-loop-presentation: Artifacts/ParallelQA/20260828T120000Z_o9_o10/unity-compile-escalated.log
- task.o9.qa.first-loop-presentation: Docs/QA/o10-candidate-gate-20260828.md
- task.o10.art.content-beta-production: Docs/QA/o9-o10-continuous-production-gate-20260828.md
- task.o10.art.content-beta-production: commit 2c88100b1cb103ba7d603177dfbbad286729cdf6
- task.o10.art.content-beta-production: O10-H1-P1-002 runtime connection complete; O10-H1-P1-005 adoption blocked
- task.o10.art.content-beta-production: .forge/feedback.json
- task.o10.art.content-beta-production: Assets/_Project/Art/Runtime/Resources/O11/Regions/o11-region-runtime-manifest.json
- task.o10.art.content-beta-production: Assets/_Project/Art/Runtime/Resources/O11/SearchNodes/o11-search-node-runtime-manifest.json
- task.o10.art.content-beta-production: Docs/QA/o11-formal-candidate-report.md
- task.o10.implementation.content-beta: Artifacts/ParallelQA/20260828T120000Z_o9_o10/source-compile-result.txt
- task.o10.implementation.content-beta: Artifacts/ParallelQA/o11-survival-balance-gate.log
- task.o10.implementation.content-beta: commits f3701d4a, 097a02a8, b3779b37, 2c88100b
- task.o10.implementation.content-beta: O10-H1-P0-001 through P1-004 implementation complete; human retest pending
- task.o10.implementation.content-beta: Artifacts/ParallelQA/O11_formal_1344f6c2_20260829T0158Z/O11-summary.json
- task.o10.implementation.content-beta: Artifacts/ParallelQA/O11_formal_1344f6c2_20260829T0158Z/windows-release-build.txt
- task.o10.implementation.content-beta: Docs/QA/o11-formal-candidate-report.md
- task.o10.qa.thirty-minute-content-beta: Docs/QA/o9-o10-continuous-production-gate-20260828.md
- task.o10.qa.thirty-minute-content-beta: Artifacts/ParallelQA/20260828T124000Z_o10_candidate/o10-verification-summary.txt
- task.o10.qa.thirty-minute-content-beta: Artifacts/ParallelQA/O11_20260829T003000Z_aa67a12_ps51_red/O11-summary.json
- task.o10.qa.thirty-minute-content-beta: Artifacts/ParallelQA/o11-survival-balance-gate.log
- task.o10.qa.thirty-minute-content-beta: Owner feedback 2026-08-28 23:41 KST; player log clean of crash/managed exception
- task.o10.qa.thirty-minute-content-beta: Artifacts/ParallelQA/O11_formal_1344f6c2_20260829T0158Z/O11-product-report.json
- task.o10.qa.thirty-minute-content-beta: Artifacts/ParallelQA/O11_formal_1344f6c2_20260829T0158Z/windows-smoke-result.txt
- task.o10.qa.thirty-minute-content-beta: Builds/KimsSurvivalIsland-O11FormalCandidate-1344f6c2.zip
- task.art.background.island-camp: Forge asset background.island-camp adopted and packaged via job_20260822130341_c082e4b6
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
- task.art.animation.mr-kim.swim: Assets/_Project/Art/Runtime/Resources/O11/mr-kim-swim-strip-v2.png
- task.art.animation.mr-kim.swim: Artifacts/ParallelQA/20260829T011500Z_o11_adopted_traversal_visuals/o11-runtime-visual-validation.json
- task.art.animation.mr-kim.swim: Docs/QA/o11-formal-candidate-report.md
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
- task.qa.feature.inventory-capacity-upgrade: Docs/Design/Playtest/Sessions/O1-2026-08-26.md
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
- task.art.object.searchable-resource-node-kit: Forge asset object.searchable-resource-node-kit adopted via job_20260825150605_49020784
- task.o9.art.ending-preview-audio: Docs/QA/o9-o10-continuous-production-gate-20260828.md
- task.art.icon.resource-tool-set: Forge asset icon.resource-tool-set adopted via job_20260822141317_caf8e11d
- task.art.effect.comedy-feedback: Forge asset effect.comedy-feedback adopted via job_20260822224357_275de712
- task.art.ui.ending-gallery: Forge asset ui.ending-gallery adopted via job_20260824133802_f43c6431
- task.system.system.ending-gallery: Artifacts/Verification/wave19-integrated-96af07d/wave19-summary.json
- task.feature.feature.ending-gallery: Artifacts/Verification/wave19-integrated-96af07d/wave19-summary.json
- task.qa.feature.ending-gallery: Artifacts/Verification/wave19-integrated-96af07d/wave19-summary.json
- task.postslice.steam-release-readiness: Artifacts/ParallelQA/20260822T113642Z_e695c36/steam-readiness-audit.txt
- task.postslice.steam-release-readiness: Artifacts/ParallelQA/20260822T113642Z_e695c36/windows-development-build.txt

## 차단 요소

- task.gamejam.qa-thirty-minute-seven-region-slice: O1 human session failed GJC-17 and opened H-001 through H-005. Correct and freeze a new candidate before timing/cohort retest.
- task.qa.feature.dual-input: GJC-20 requires a physical XInput or Steam Input compatible gamepad playthrough on the exact final Release candidate.
- task.qa.feature.inventory-capacity-upgrade: O1 found the upgrade mandatory and material identity unreadable. Fix H-002 and rebalance/retest GJC-12 on a new candidate.

## 미결 질문

- 없음

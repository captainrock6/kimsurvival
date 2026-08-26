# GAME JAM 제출 완료 기준 감사

> O1 교정 자동 GREEN 제품 코드 소스: b3f980ff14db0075ed4a290038f518d658783425
>
> 수기 테스트에서 거절된 후보 소스: `0cfcc02ac661392e03a75898831fb0a89ae82bd1`
>
> 감사 계약: gamejam.completion-matrix.v2
>
> 판정일: 2026-08-26
>
> 제출 판정: RETEST · O2_READY · AUTOMATED_GREEN · NOT_SUBMISSION_READY

> O2 후보 EXE: `work/ParallelQA/20260826T221000Z_o2_release_b3f980f/KimSurvivalIsland-gamejam-win64-release-b3f980f/KimSurvivalIsland.exe`
>
> 자동 GREEN 기준 EXE SHA-256: `a197542ad0d026c5c3bc7aead606b6b0184adad7b4ee3635326c575b25a5b423`
>
> O2 후보 게임 코드 DLL SHA-256: `dfff3680c5e41440555226c0a1dc9309fad516e5c00adc5483fd2f27016789ef`
>
> O2 후보 배포 ZIP SHA-256: `293a123d5fbbea721bb5ee798ed9879f48a6fe022987646349fadf71e1340830`
>
> 패키지 무결성: `Artifacts/ParallelQA/20260826T222000Z_o2_package_b3f980f/gamejam-package-integrity-summary.json`

이 문서는 통합 GDD와 Forge 수직 슬라이스가 요구하는 기능을 현재 실행 증거와 대조한 제출 게이트다. backlog의 done 표시는 작업 이력을 뜻할 뿐, 이 표의 DONE을 자동으로 보장하지 않는다. O1에서 확인된 다섯 P1의 제품 교정과 자동·시각 검증은 새 후보에서 GREEN이지만, 사람이 실제로 다시 확인하기 전까지 판정은 `RETEST`다.

## 1. 범위와 증거 우선순위

설계 정본:

- Docs/Design/kim-survival-island-gdd.md
- .forge/design/vertical-slice.json
- .forge/design/project.json의 sevenRegionSearchNodeContract

현재 자동 GREEN 증거:

- Artifacts/ParallelQA/20260826T223000Z_gamejam_waveb_2bdea61
- Artifacts/ParallelQA/20260826T220000Z_gamejam_wavec_2bdea61
- Artifacts/ParallelQA/20260826T230000Z_gamejam_release_2bdea61
- Artifacts/ParallelQA/20260826T233000Z_gamejam_package_2bdea61
- Artifacts/ParallelQA/20260826T214500Z_o1_p1_integrated_r11
- Artifacts/ParallelQA/20260826T214500Z_o1_p1_integrated_final
- Artifacts/ParallelQA/20260826T221000Z_o2_release_b3f980f
- Artifacts/ParallelQA/20260826T222000Z_o2_package_b3f980f

현재 사람 반증:

- Docs/Design/Playtest/Sessions/O1-2026-08-26.md
- Docs/Design/Playtest/gamejam-seven-region-search-node-results.md
- Docs/Design/gamejam-o1-human-p1-corrective-wave.md

과거 검증 기준선 증거(이력 추적용):

- Artifacts/ParallelQA/20260827T190000Z_gamejam_waveb_3d64403/gamejam-wave-b-summary.json
- Artifacts/ParallelQA/20260827T190000Z_gamejam_waveb_3d64403/gamejam-wave-b-edit-contracts.json
- Artifacts/ParallelQA/20260827T190000Z_gamejam_waveb_3d64403/gamejam-wave-b-play-contracts.json
- Artifacts/ParallelQA/20260827T190000Z_gamejam_waveb_3d64403/gamejam-search-node-summary.json
- Artifacts/ParallelQA/20260827T190000Z_gamejam_waveb_3d64403/gamejam-search-node-edit-observation-evidence.json
- Artifacts/ParallelQA/20260827T190000Z_gamejam_waveb_3d64403/gamejam-search-node-play-observation-evidence.json
- Artifacts/ParallelQA/20260827T180000Z_gamejam_wavec_3d64403/gamejam-wave-c-summary.json
- Artifacts/ParallelQA/20260827T180000Z_gamejam_wavec_3d64403/gamejam-wave-c-edit-contracts.json
- Artifacts/ParallelQA/20260827T180000Z_gamejam_wavec_3d64403/gamejam-wave-c-play-contracts.json
- Artifacts/ParallelQA/20260827T180000Z_gamejam_wavec_3d64403/wave19-summary.json
- Artifacts/ParallelQA/20260827T180000Z_gamejam_wavec_3d64403/wave20-summary.json
- Artifacts/ParallelQA/20260827T180000Z_gamejam_wavec_3d64403/compile-result.txt
- Artifacts/ParallelQA/20260827T180000Z_gamejam_wavec_3d64403/windows-development-build.json
- Artifacts/ParallelQA/20260827T180000Z_gamejam_wavec_3d64403/windows-hidden-smoke.json
- Artifacts/ParallelQA/20260827T200000Z_gamejam_longstay_3d64403/gamejam-long-stay-summary.json
- Artifacts/ParallelQA/finalqa_9263705_wave17/wave17-summary.json
- Artifacts/ParallelQA/finalqa_9263705_longstay/gamejam-long-stay-summary.json
- Artifacts/ParallelQA/pkg_2c9f36a_verified/gamejam-package-integrity-summary.json

인간 검증 절차와 누적 결과:

- Docs/QA/gamejam-final-windows-candidate-30m-human-checklist-ko.md
- Docs/Design/Playtest/gamejam-seven-region-search-node-results.md

소스 `2bdea61`은 Wave B/C 통합 게이트, Release Windows build, hidden smoke, source-clean 검사와 `PKG-I01~I07` 패키지 무결성을 모두 통과했다. 이후 문서·Forge 상태만 갱신한 최종 패키지는 `BUILD-INFO.txt`의 정확한 source SHA와 해시를 사용한다. 증거 우선순위는 현재 통합 probe, 같은 빌드의 세부 관찰, 과거 기준선 PASS, backlog 작업 이력, 설계 존재 순이다.

Git에는 위 증거에서 이 매트릭스가 직접 인용하는 핵심 JSON/TXT와 최종 패키지 게이트 전체를 보존한다. 중복 용량이 큰 PNG 및 원시 로그는 운영 PC의 로컬 QA 산출물로 유지한다.

상태 의미:

| 상태 | 의미 |
|---|---|
| DONE | 현재 빌드와 현재 수용 증거가 제출 요구를 충족하며 추가 인간 판정이 필요 없다. |
| PARTIAL | 기반 구현 또는 일부 증거는 있으나 필수 연결·상태·표면·최신 probe가 남아 있다. |
| MISSING | 플레이어가 사용할 live 경로 또는 구조화된 관찰 대상이 현재 빌드에 없다. |
| HUMAN_REQUIRED | 사람 또는 물리 장치 결과가 본질적으로 필요한 항목이다. 자동 선행 게이트는 GREEN이며 동일 최종 후보 빌드에서 실행한다. |

## 2. 제출 완료 매트릭스

| ID | 제출 요구 | 상태 | 현재 판정 | 닫힘 조건 |
|---|---|---|---|---|
| GJC-01 | 컴파일·Windows 빌드·hidden smoke·Addressables·방화벽 | DONE | Release build와 source-clean, hidden smoke, Addressables, 방화벽, `PKG-I01~I07`이 PASS다. | 닫힘. |
| GJC-02 | 김씨의 직접 이동·근접 설비 상호작용 | DONE | canonical camp/module/map lock과 직접 설비 선택·popup 복귀가 PASS다. | 닫힘. |
| GJC-03 | 일반 설비 제한적 자유 배치·특수 anchor | DONE | 이동 통로·일반 배치·전용 anchor·취소 무변경 회귀가 PASS다. | 닫힘. |
| GJC-04 | 수영 수색·복귀 | DONE | grant/warp 없이 수영 수색·귀환 경로가 통합 회귀에서 PASS다. | 닫힘. |
| GJC-05 | 행동 기반 ending resolver·album 기록 | DONE | ending resolver·stable ID·album·exactly-once 저장이 PASS다. | 닫힘. |
| GJC-06 | 안정 ID를 가진 7개 지역 | DONE | 정확히 7 region·21 archetype·42 instance·일반 자원 144와 결정성이 PASS다. | 닫힘. |
| GJC-07 | 환경 수색 node의 live 오브젝트·tray | DONE | 실제 node 접근→수색→tray→현장 복귀와 GSN 15/15가 PASS다. | 닫힘. |
| GJC-08 | 최초 정보 은폐·take/leave/swap·원자 가방 거래 | DONE | hidden/partial/depleted와 take/leave/swap/cancel 원자성이 PASS다. | 닫힘. |
| GJC-09 | 유한 자원·부분 잔류·고갈·재방문 보존 | DONE | 유한 잔여·고갈·재방문·save 복원·새 run 초기화가 PASS다. | 닫힘. |
| GJC-10 | 부순 장벽·제거 위험의 지속 | DONE | persistent 장벽·제거 위험과 transient 위험 계약이 PASS다. | 닫힘. |
| GJC-11 | 벌레·위험 식물 등 환경 위험 및 질병의 예고→노출/효과→대응→완화/치료 | DONE | 최소 2종 환경 위험과 질병 lifecycle·snapshot·원자성이 PASS다. | 닫힘. |
| GJC-12 | 가방 4→6·중첩 2 | PARTIAL | 기능과 12개 canonical 재료 의미 연결은 자동 GREEN이다. O1에서 업그레이드가 필수로 느껴진 체감 판정은 아직 유효하다. | O2 fresh session에서 4칸 선별과 4→6 투자 선택을 다시 관찰한다. |
| GJC-13 | 같은 run의 위층·지하실 확장 | PARTIAL | 목적·추천 용도·용량·정확한 비용·부족분·선행 조건·완료 공간을 구분하는 presenter와 KO/EN/qps 계약은 GREEN이다. | O2에서 설명 없이 목적·비용·잠금·완료·열린 공간을 확인한다. |
| GJC-14 | 뗏목 탈출 | DONE | 독립 해안 launcher와 ordered stage·ending exactly-once 경로가 PASS다. | 닫힘. |
| GJC-15 | 대형 연기 탈출 | DONE | 부싯돌·비용·점화·가시성·대기·재시도·ending 경로가 PASS다. | 닫힘. |
| GJC-16 | 무전 탈출 | DONE | 분산된 무전 3부품·수리·주파수·응답·ending 경로가 PASS다. | 닫힘. |
| GJC-17 | 5–10분 loop·25–35분 대표 탈출·30분 profile | PARTIAL | 새 후보의 grant/warp 없는 합성 프로필 28.98분과 전체 loop 회귀는 PASS다. 사람 O1 시간은 이해 실패 상태의 1분 미만/21분 50초이므로 대체되지 않는다. | O2에서 실제 첫 loop와 대표 탈출 시간을 다시 측정한다. |
| GJC-18 | KO 기본·EN 지원·qps-long QA | PARTIAL | KO/EN/qps-long 1280×800에서 수색 tray, 캠프 HUD, 증축 카드 overflow와 비중첩은 GREEN이며 12개 stable ID 이름도 일치한다. | O2에서 한국어 의미·위계를 설명 없이 읽는지 확인한다. |
| GJC-19 | 키보드/마우스·synthetic gamepad 의미 동등 | DONE | 수색·가방·세 탈출·ending stable action/result 동등성이 PASS다. | 닫힘. |
| GJC-20 | 물리 게임패드 실기 | HUMAN_REQUIRED | 자동화와 섞지 않기로 한 실제 장치 결과가 UNVERIFIED다. | 동일 Windows 후보 빌드에서 실제 게임패드로 별도 체크리스트를 수행한다. |
| GJC-21 | ending 3–5장 코믹북 panel | DONE | core 3장+modifier, KO/EN/qps, album 재진입과 exactly-once가 PASS다. | 닫힘. |
| GJC-22 | 수색 node 시각 리소스의 런타임 채택 | PARTIAL | engine_ready/adopted 캐릭터·구조물·네 자원 아이콘 계열의 live surface 연결과 1280×800 증거는 GREEN이다. review-only 리소스는 제외됐다. | O2에서 자원·설비·상태를 실제로 시각 구분하는지 확인한다. |
| GJC-23 | 첫 사용자 30분 검증 | HUMAN_REQUIRED | KO 3명·EN 3명의 자연 세션은 실행되지 않았다. 결과를 만들어낼 수 없다. | 자동화 P0가 모두 GREEN인 동일 빌드에서 6세션을 수행하고 성공률·막힘·이해도를 기록한다. |
| GJC-24 | Day 20 게임잼 장기 체류 엔딩 2종 | DONE | 카탈로그 21개, 두 Day 20 terminal, 조기 탈출 우선, 결정론, exactly-once, KO/EN/qps comic과 Day 50 회귀가 PASS다. | 닫힘. |

현재 집계는 DONE 17, PARTIAL 5, MISSING 0, HUMAN_REQUIRED 2이다. 후보 `0cfcc02`는 폐기됐다. 새 후보 `b3f980f`에서 H-001~H-005 제품 교정, 컴파일, 7지역·3탈출·28.98분 회귀, KO/EN/qps 시각 검토, Release build와 패키지 무결성 7/7이 GREEN이다. 다섯 결함은 `FIXED_AUTOMATED_RETEST_PENDING`이며 O2 사람 판정 전에는 제출하지 않는다.

## 3. 과거 기준선에서 완료된 3개 구현 wave 기록

세 wave는 기존 task를 닫은 의존 순서다. 각 wave는 RED-first probe 뒤 GREEN으로 전환됐고, Wave C는 후보 소스 `3d64403`의 `20260827T180000Z_gamejam_wavec_3d64403`에서 전체 회귀와 함께 닫혔다.

### Wave A — live 수색 node 세로 골격

의존: 현재 compile/build/camp 녹색 lock.

기존 task:

- task.system.system.search-node-loot
- task.feature.feature.searchable-resource-nodes
- task.qa.feature.searchable-resource-nodes

red-first 수용 기준:

1. 실제 scene node가 하나도 등록되지 않으면 실패하고 placeholder data만으로 통과하지 않는다.
2. 한 시작 지역 node에서 far→near→popup→search commit→tray→field return을 관찰한다.
3. 동일 seed·node ID는 같은 구조화 발견물을 만들고 다른 seed는 하나 이상 달라진다.
4. hidden→partial→depleted, leave·take·swap, 취소·transition·revisit가 같은 node ID를 유지한다.
5. 4/6칸·중첩 2 거래는 보존 법칙을 지키며 실패·취소 때 비용·가방·node가 모두 무변경이다.
6. 수색 비용은 commit당 한 번만 차감되고 hazard는 tray가 열린 동안 진행되지 않는다.
7. KO/EN/qps-long 1280×800, 키보드/마우스와 synthetic gamepad 결과 코드가 같다.
8. grant, warp, skip 또는 fixture-only runtime path가 감지되면 실패한다.

Wave A 종료에는 GSN-P01/P02/P03/P04/P07/P08/P09와 단일-node 결정론 probe가 GREEN이어야 한다. 7지역 총량과 보호 부품은 Wave B/C에서 닫는다.

### Wave B — 7지역 유한 world·위험 지속

의존: Wave A GREEN.

기존 task:

- task.gamejam.seven-region-catalog
- task.system.system.region-persistence
- task.feature.feature.region-persistence
- task.qa.feature.region-persistence
- task.gamejam.persistent-region-runtime
- task.gamejam.insect-plant-wildlife-disease

red-first 수용 기준:

1. 정확한 7 region/21 archetype/42 node/유한 일반 자원 144가 단일 catalog에서 로드되지 않으면 실패한다.
2. 같은 seed와 revision은 byte-equivalent placement/content를, 다른 seed는 ID를 바꾸지 않은 variation을 만든다.
3. hidden/partial/depleted/known remainder가 귀환·강제 귀환·재방문·save restore에 유지된다.
4. 부순 장벽과 제거 가능한 persistent hazard만 남고 transient hazard는 계약대로 다시 발생할 수 있다.
5. 7지역 각각의 위험 조합이 catalog와 일치하고 최소 한 질병이 예고→노출→효과→완화/치료 전 과정을 통과한다.
6. 자원 소진 뒤 재방문이 node를 재생성하지 않으며 새 게임에서만 초기화한다.

Wave B 종료에는 GSN-E01/E02/E03, GSN-P02/P03/P06/P07과 별도 disease lifecycle probe가 GREEN이어야 한다.

### Wave C — 보호 부품·3탈출·30분 ending 통합

의존: Wave B GREEN.

기존 task:

- task.gamejam.smoke-radio-material-routes
- task.feature.feature.raft-escape
- task.qa.feature.raft-escape
- task.gamejam.upper-basement-both
- task.gamejam.qa-thirty-minute-seven-region-slice

red-first 수용 기준:

1. 돛천·부싯돌·무전 3부품은 선언된 eligible node에서만 나오며 bag full·도난·일반 피해로 소실되지 않는다.
2. eligible completed miss만 3/5 pity를 세고, 이미 알려진 loot를 덮어쓰지 않는다.
3. seed 감사에서 raft/smoke/radio를 포함해 최소 3개 탈출법이 동시에 completable이다.
4. raft는 해안 launcher와 ordered stage, smoke는 점화·가시성, radio는 수리·주파수라는 서로 다른 직접 상호작용을 자연 trace로 보인다.
5. 각 경로의 실패·취소·날씨 대기·재시도는 원자적이며 ending과 album은 한 번만 기록된다.
6. 같은 run에서 위층과 지하실을 확정·재진입해도 3탈출 자원·save state를 손상하지 않는다.
7. ending은 live core panel 3장 이상과 modifier panel을 렌더하고 KO/EN/qps-long에서 필수 행동이 잘리지 않는다.
8. grant/warp/skip 없이 대표 seed가 25–35분 synthetic profile을 통과한다. 이 수치는 인간 6세션 전 HUMAN_REQUIRED를 DONE으로 바꾸지 않는다.

Wave C 종료 조건인 GSN-E05/P05/P10, 세 escape 자연 trace, live comic panel, same-run upper+basement는 `b3f980f` O1 교정 후보에서도 GREEN이다. O2 소유자 재테스트를 먼저 실행하고, 통과 뒤 GJC-12·17·20·23의 확장 사용자/물리 장치 게이트를 실행한다.

## 4. 회귀·중단 규칙

- 각 wave는 compile 0/0, Windows build, hidden smoke, Addressables, canonical camp/module/map, Wave 20 raft 16/16을 보존한다.
- 원자 거래에서 item 생성·소실, 알려진 node reroll, protected part 소실, save/load state 손실 중 하나라도 나오면 P0로 중단한다.
- KO/EN 의미 불일치, 필수 prompt clipping, 입력 수단별 결과 불일치는 P1이며 해당 wave를 GREEN으로 처리하지 않는다.
- 30분 체감, 재미, 70% 성공 목표, 물리 게임패드는 자동화 결과로 대신하지 않는다.
- Steam App ID·상점명·공식 영문 제목은 이 GAME JAM 제출 게이트 밖이며 계속 TBD다.

## 5. O1 교정 인계 요약

Wave A→B→C의 상태기계·저장·탈출 회귀는 기준선으로 보존했다. 후보 `0cfcc02`는 사람 사용성 P1 다섯 건 때문에 폐기했고, 새 후보 `b3f980f`에는 UI·재료 의미, 증축 UX, 캐릭터 접지·애니메이션, live 리소스 연결이 통합됐다. 자동·시각·Release·패키지 게이트는 GREEN이다. 다음 단일 게이트는 소유자 O2 재테스트이며, 그 뒤에만 물리 게임패드와 KO 3명·EN 3명 첫 사용자 세션을 진행한다.

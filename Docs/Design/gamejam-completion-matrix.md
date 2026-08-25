# GAME JAM 제출 완료 기준 감사

> 정본 기준: 078661e653851802aa97d86fd691337411ac345c
>
> 감사 계약: gamejam.completion-matrix.v1
>
> 판정일: 2026-08-26
>
> 제출 판정: PARTIAL

이 문서는 통합 GDD와 Forge 수직 슬라이스가 요구하는 기능을 현재 실행 증거와 대조한 제출 게이트다. backlog의 done 표시는 작업 이력을 뜻할 뿐, 이 표의 DONE을 자동으로 보장하지 않는다. 현재 통합 게이트의 독립 probe가 과거 개별 PASS와 충돌하면 현재 통합 게이트를 우선한다.

## 1. 범위와 증거 우선순위

설계 정본:

- Docs/Design/kim-survival-island-gdd.md
- .forge/design/vertical-slice.json
- .forge/design/project.json의 sevenRegionSearchNodeContract

현재 통합 증거:

- Artifacts/ParallelQA/20260826T053000Z_gamejam_search_node_integrated_green/gamejam-search-node-summary.json
- 같은 폴더의 gamejam-search-node-edit-observation-evidence.json
- 같은 폴더의 gamejam-search-node-play-observation-evidence.json
- 같은 폴더의 wave19-summary.json, wave20-summary.json, wave16-summary.json
- 같은 폴더의 compile-result.txt, windows-development-build.json, windows-hidden-smoke.json
- 같은 폴더의 wave11-slot-play-evidence.json, wave19-play-observation-evidence.json

이 증거는 현재 통합 브랜치에서 다시 생성한 GREEN 산출물이다. 우선순위는 현재 통합 probe, 같은 빌드의 세부 관찰, 과거 개별 wave PASS, backlog 상태, 설계 존재 순이다.

상태 의미:

| 상태 | 의미 |
|---|---|
| DONE | 현재 빌드와 현재 수용 증거가 제출 요구를 충족하며 추가 인간 판정이 필요 없다. |
| PARTIAL | 기반 구현 또는 일부 증거는 있으나 필수 연결·상태·표면·최신 probe가 남아 있다. |
| MISSING | 플레이어가 사용할 live 경로 또는 구조화된 관찰 대상이 현재 빌드에 없다. |
| HUMAN_REQUIRED | 자동화 가능한 계약은 충족했고 실제 사용자 또는 물리 장치 판단만 남았다. |

## 2. 제출 완료 매트릭스

| ID | 제출 요구 | 상태 | 현재 증거와 판정 | 닫힘 조건 |
|---|---|---|---|---|
| GJC-01 | 컴파일·Windows 빌드·hidden smoke·Addressables·방화벽 | DONE | 컴파일 0 error/0 warning, Windows 빌드와 6초 이상 hidden smoke, Addressables·방화벽 검사가 모두 PASS이며 overall infrastructure도 PASS다. | 현재 녹색 lock을 이후 wave에서 회귀시키지 않는다. |
| GJC-02 | 김씨의 직접 이동·근접 설비 상호작용 | DONE | current canonical camp/module/map lock과 Wave 11 직접 슬롯 상호작용 증거가 PASS다. 전역 메뉴 회귀 증거가 없다. | compact 문맥 prompt와 popup 복귀 회귀 0건을 유지한다. |
| GJC-03 | 일반 설비 제한적 자유 배치·특수 anchor | DONE | 캠프/모듈 통합 lock이 PASS이고 기존 배치 계약이 현재 정본에 유지된다. | 이동 통로·anchor·취소 무변경 회귀 0건을 유지한다. |
| GJC-04 | 수영 수색·복귀 | DONE | 현행 통합 회귀에서 수영과 생존 loop가 기존 녹색 lock으로 보존된다. | 자연 trace에서 grant/warp 없이 입수·복귀가 가능해야 한다. |
| GJC-05 | 행동 기반 ending resolver·album 기록 | DONE | Wave 19에서 resolver·album·저장·KO/EN/qps 표면이 통과했다. comic panel 렌더는 GJC-21로 분리한다. | 동일 ending을 한 번만 해금·기록하고 stable ID를 보존한다. |
| GJC-06 | 안정 ID를 가진 7개 지역 | PARTIAL | 정확한 7 region ID와 28개 live 유한 node가 로드된다. 다만 동결된 BALANCE_PROVISIONAL 표본의 21 archetype·42 instance·일반 자원 144 총량은 아직 충족하지 않는다. | 7 region, 21 archetype, 42 instance, 유한 일반 자원 144가 같은 카탈로그에서 로드된다. |
| GJC-07 | 환경 수색 node의 live 오브젝트·tray | DONE | 실제 Scene에서 월드 node 접근, 근접 prompt, 발견물 tray, 취소와 현장 복귀가 grant·warp·skip 없이 관찰됐고 GSN-P01/P10이 PASS다. | 현재 15/15 GREEN을 이후 통합에서 유지한다. |
| GJC-08 | 최초 정보 은폐·take/leave/swap·원자 가방 거래 | DONE | hidden→partial→depleted와 take/leave/replace/cancel이 실제 ledger와 4칸 가방에서 총량 보존·원자 rollback을 통과했다. | 6칸 업그레이드 자연 경로에서도 같은 계약을 유지한다. |
| GJC-09 | 유한 자원·부분 잔류·고갈·재방문 보존 | DONE | 동일 seed 결정성, 취소·화면 전환·재방문 무재추첨과 snapshot 직렬화 복원이 모두 PASS다. | 새 run에서만 seed와 stock을 초기화한다. |
| GJC-10 | 부순 장벽·제거 위험의 지속 | DONE | region snapshot이 부순 장벽과 persistent 제거 위험 ID를 저장·복원하고 새 run 초기화 표면을 제공한다. | transient 위험만 계약대로 재발할 수 있게 유지한다. |
| GJC-11 | 질병의 예고→노출→효과→완화/치료 | PARTIAL | hazard catalog/director 데이터는 있으나 survival-hazards feature와 QA가 계획 상태이며 같은 빌드의 완전한 질병 lifecycle 증거가 없다. | 한 질병이 실제 자연 경로에서 네 단계를 모두 보이고 취소·강제 귀환·회복 원자성을 통과한다. |
| GJC-12 | 가방 4→6·중첩 2 | HUMAN_REQUIRED | 기능과 자동화 계약은 기존 녹색이나 자연 grant/warp 없는 구매→수색→구조 경로와 실제 선택 긴장은 아직 사람이 검증하지 않았다. 수색 tray 거래는 GJC-08과 별도다. | fresh save 자연 trace와 사용자 관찰에서 업그레이드·날짜 지속·새 게임 4칸 초기화를 확인한다. |
| GJC-13 | 같은 run의 위층·지하실 확장 | PARTIAL | 위/옆/지하 direct slot 3/3, prompt/popup/cancel은 확인됐지만 gamejam.upper-basement-both는 ready이며 같은 run에서 위층과 지하실을 모두 확정·사용한 증거가 없다. | fresh run에서 위층 1개와 지하실 1개를 각각 확정하고 통로·저장·재진입을 보존한다. |
| GJC-14 | 뗏목 탈출 | PARTIAL | 보호 돛천은 적격 live node에서 보호 inventory로 연결되고 Wave 20 상태기계도 16/16 PASS다. 아직 node 획득부터 ending까지 하나로 이어진 no-grant 통합 trace가 없다. | node 돛천→보호 inventory→선체/돛/보급→날씨 창→ending을 grant 없이 한 trace로 통과한다. |
| GJC-15 | 대형 연기 탈출 | PARTIAL | 기존 smoke 직접 상호작용은 개별 회귀가 있으나 부싯돌의 실제 유한 node 획득과 보호·pity 연결이 없다. | node 부싯돌과 기존 재료→점화 설비→유효 날씨/가시성→ending을 grant 없이 통과한다. |
| GJC-16 | 무전 탈출 | PARTIAL | radio 경로 기반은 있으나 전자기판·트랜지스터 등 3부품의 서로 다른 지역 분산과 live node 거래가 없다. | 세 보호 부품을 서로 다른 지역에서 확보하고 수리·주파수 조작·응답→ending을 자연 trace로 통과한다. |
| GJC-17 | 5–10분 loop·25–35분 대표 탈출·30분 profile | PARTIAL | 수색 node의 grant·warp·skip 없는 자연 이동·상호작용 trace는 PASS로 전환됐다. 하지만 첫 loop와 대표 탈출을 실제 분 단위로 잰 cheat-free 통합 trace가 없다. | debug 보조 없이 첫 loop 5–10분, 대표 탈출 25–35분의 기계 trace를 먼저 만들고 이후 6명 사용자가 검증한다. |
| GJC-18 | KO 기본·EN 지원·qps-long QA | PARTIAL | 수색 의미와 compact tray는 KO/EN/qps-long 1280×800에서 overflow 0으로 PASS다. 질병·연기·무전·최종 다중 패널 ending까지 같은 빌드의 전체 표면 검증이 남았다. | 수색·질병·세 탈출·ending에서 KO/EN 의미가 같고 qps-long 1280×800 overflow 0건이다. |
| GJC-19 | 키보드/마우스·synthetic gamepad 의미 동등 | PARTIAL | live 수색 node/action/focus는 GSN-P09 PASS다. 연기·무전 자연 경로와 최종 ending 탐색까지 동일 stable action 증거에 포함해야 한다. | node·가방 교체·세 탈출·ending에서 동일 stable action과 결과 코드를 낸다. |
| GJC-20 | 물리 게임패드 실기 | HUMAN_REQUIRED | 자동화와 섞지 않기로 한 실제 장치 결과가 UNVERIFIED다. | 동일 Windows 후보 빌드에서 실제 게임패드로 별도 체크리스트를 수행한다. |
| GJC-21 | ending 3–5장 코믹북 panel | PARTIAL | 채택된 shell/capture는 있으나 current Wave 16 probe가 panels=0으로 P0 FAIL이다. | live ending에서 core panel 3장 이상, modifier panel, KO/EN/qps 캡처와 album 재진입을 확인한다. |
| GJC-22 | 수색 node 시각 리소스의 런타임 채택 | DONE | 기존 채택 Wood/Stone/Food/Salvage source GUID가 실제 compact tray Image에 연결되어 Wave 19 21/21을 통과했다. 신규 node/tray 후보는 review 상태를 유지하며 이 판정에 사용하지 않았다. | 채택되지 않은 review 후보를 자동 연결하지 않고 현재 adopted GUID 회귀를 유지한다. |
| GJC-23 | 첫 사용자 30분 검증 | HUMAN_REQUIRED | KO 3명·EN 3명의 자연 세션은 실행되지 않았다. 결과를 만들어낼 수 없다. | 자동화 P0가 모두 GREEN인 동일 빌드에서 6세션을 수행하고 성공률·막힘·이해도를 기록한다. |

집계는 DONE 10, PARTIAL 10, MISSING 0, HUMAN_REQUIRED 3이다. live 수색 경로 부재는 해소됐고, 다음 자동화 우선순위는 7/21/42/144 카탈로그, 질병 lifecycle, 같은 run 위층+지하실, 연기·무전과 세 탈출 통합 trace, 30분 profile과 3장 이상 ending comic이다. 따라서 아직 제출 가능한 완성 빌드로 판정하지 않는다.

## 3. 다음 3개 구현 wave

세 wave는 새 기능 목록이 아니라 기존 task를 닫는 의존 순서다. 각 wave는 probe를 먼저 실패시키고, 지정된 acceptance가 모두 GREEN일 때만 다음 wave로 넘어간다. 과거 task의 done 표시는 되돌리지 않되 현재 통합 게이트의 실패는 별도 제출 매트릭스에 남긴다.

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

Wave C 종료에는 GSN-E05/P05/P10, 세 escape 자연 trace, live comic panel, same-run upper+basement가 GREEN이어야 한다. 그 뒤에만 GJC-12·20·23의 사용자/물리 장치 게이트를 실행한다.

## 4. 회귀·중단 규칙

- 각 wave는 compile 0/0, Windows build, hidden smoke, Addressables, canonical camp/module/map, Wave 20 raft 16/16을 보존한다.
- 원자 거래에서 item 생성·소실, 알려진 node reroll, protected part 소실, save/load state 손실 중 하나라도 나오면 P0로 중단한다.
- KO/EN 의미 불일치, 필수 prompt clipping, 입력 수단별 결과 불일치는 P1이며 해당 wave를 GREEN으로 처리하지 않는다.
- 30분 체감, 재미, 70% 성공 목표, 물리 게임패드는 자동화 결과로 대신하지 않는다.
- Steam App ID·상점명·공식 영문 제목은 이 GAME JAM 제출 게이트 밖이며 계속 TBD다.

## 5. 구현·QA 인계 요약

가장 짧은 경로는 수색 node 골격을 먼저 실제 scene에 노출하고, 그 snapshot을 7지역과 위험 지속에 확장한 뒤, 보호 부품과 세 탈출·ending을 자연 trace로 묶는 것이다. 세 단계를 병렬로 섞으면 fixture 기반 과거 PASS가 다시 live 경로 부재를 가릴 수 있으므로 Wave A→B→C 순서를 바꾸지 않는다. 사람 플레이테스트와 물리 게임패드는 Wave C 자동화 GREEN 이후의 독립 증거다.

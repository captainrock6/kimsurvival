# GAME JAM 제출 완료 기준 감사

> 과거 검증 기준선 소스: 3d64403813493d8de8e05f0844a5e616e164c6d4
>
> 현재 후보 소스: 미지정(미커밋 작업 트리)
>
> 감사 계약: gamejam.completion-matrix.v2
>
> 판정일: 2026-08-26
>
> 제출 판정: PARTIAL · AUTOMATED_EVIDENCE_PENDING · RELEASE_REVALIDATION_REQUIRED · HUMAN_REQUIRED

> 과거 검증 기준선 EXE: `work/ParallelQA/KimSurvivalIsland-gamejam-win64-3d64403/KimSurvivalIsland.exe`
>
> 과거 기준선 EXE SHA-256: `93c19f9e7c681845d34407807d33b6438e781dd34c4d8895ebdf2c6fb083711d`
>
> 과거 기준선 게임 코드 DLL SHA-256: `b7e216c2892ee952905ccc2cbb37caa7050c355da38d6582e2d4c5d1b762f7e6`
>
> 과거 기준선 배포 ZIP SHA-256: `68f639c10f74982ba2b2fd813d158de61217242252baed436286fa322433b5f4`
>
> 배포 매니페스트: `Docs/QA/gamejam-final-windows-candidate-release-manifest-3d64403.md`

이 문서는 통합 GDD와 Forge 수직 슬라이스가 요구하는 기능을 현재 실행 증거와 대조한 제출 게이트다. backlog의 done 표시는 작업 이력을 뜻할 뿐, 이 표의 DONE을 자동으로 보장하지 않는다. 현재 통합 게이트의 독립 probe가 과거 개별 PASS와 충돌하면 현재 통합 게이트를 우선한다.

## 1. 범위와 증거 우선순위

설계 정본:

- Docs/Design/kim-survival-island-gdd.md
- .forge/design/vertical-slice.json
- .forge/design/project.json의 sevenRegionSearchNodeContract

과거 검증 기준선 증거(현재 미커밋 작업 트리의 증거로 재사용 금지):

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

이 증거는 소스 `3d64403` 패키지의 과거 GREEN 기준선이다. 이후 현재 작업 트리에 런타임·아트·현지화·QA 변경이 존재하므로 이 산출물은 현재 후보의 DONE 근거가 아니다. 현재 후보는 새 소스 commit, 전체 통합 게이트, Windows build·hidden smoke, 패키지 무결성, release manifest가 모두 생성될 때까지 자동 20개 요구를 PARTIAL로 유지한다. 기존 해시와 경로는 이력 추적용이며 새 증거처럼 인용하지 않는다. 우선순위는 현재 후보의 새 통합 probe, 같은 빌드의 세부 관찰, 과거 기준선 PASS, backlog 작업 이력, 설계 존재 순이다.

Git에는 위 증거에서 이 매트릭스가 직접 인용하는 핵심 JSON/TXT와 최종 패키지 게이트 전체를 보존한다. 중복 용량이 큰 PNG 및 원시 로그는 운영 PC의 로컬 QA 산출물로 유지한다.

상태 의미:

| 상태 | 의미 |
|---|---|
| DONE | 현재 빌드와 현재 수용 증거가 제출 요구를 충족하며 추가 인간 판정이 필요 없다. |
| PARTIAL | 기반 구현 또는 일부 증거는 있으나 필수 연결·상태·표면·최신 probe가 남아 있다. |
| MISSING | 플레이어가 사용할 live 경로 또는 구조화된 관찰 대상이 현재 빌드에 없다. |
| HUMAN_REQUIRED | 사람 또는 물리 장치 결과가 본질적으로 필요한 항목이다. 현재 후보의 자동 선행 게이트가 다시 GREEN이 된 뒤에만 실행한다. |

## 2. 제출 완료 매트릭스

| ID | 제출 요구 | 상태 | 과거 기준선 증거와 현재 판정 | 닫힘 조건 |
|---|---|---|---|---|
| GJC-01 | 컴파일·Windows 빌드·hidden smoke·Addressables·방화벽 | PARTIAL | 컴파일 0 error/0 warning, Windows 빌드와 6초 이상 hidden smoke, Addressables·방화벽 검사가 모두 PASS이며 overall infrastructure도 PASS다. 별도 `PKG-I01~I06`이 내부 manifest 265/265, ZIP↔폴더 266/266, 해제본 6.123초 생존·응답, 잔존 프로세스 0, 원본·테스트 payload 불변을 확인했다. LocalLow JSONL은 허용 경로·이번 실행 신규 생성·시간 범위·5줄 JSON 유효성·원본/증거 복사본 SHA 일치까지 검증했다. | 현재 작업 트리의 전체 게이트·build·smoke·package를 같은 새 source identity로 다시 통과한다. |
| GJC-02 | 김씨의 직접 이동·근접 설비 상호작용 | PARTIAL | 기준선의 canonical camp/module/map lock과 Wave 11 직접 슬롯 상호작용 증거가 PASS다. 전역 메뉴 회귀 증거가 없다. | 새 후보에서 compact 문맥 prompt와 popup 복귀 회귀 0건을 확인한다. |
| GJC-03 | 일반 설비 제한적 자유 배치·특수 anchor | PARTIAL | 기준선의 캠프/모듈 통합 lock이 PASS이고 기존 배치 계약이 현재 정본에 유지된다. | 새 후보에서 이동 통로·anchor·취소 무변경 회귀 0건을 확인한다. |
| GJC-04 | 수영 수색·복귀 | PARTIAL | 기준선 통합 회귀에서 수영과 생존 loop가 녹색 lock으로 보존됐다. | 새 후보 자연 trace에서 grant/warp 없이 입수·복귀를 재확인한다. |
| GJC-05 | 행동 기반 ending resolver·album 기록 | PARTIAL | 기준선 Wave 19에서 resolver·album·저장·KO/EN/qps 표면이 통과했다. comic panel 렌더는 GJC-21로 분리한다. | 새 후보에서 동일 ending 1회 해금·기록과 stable ID를 재확인한다. |
| GJC-06 | 안정 ID를 가진 7개 지역 | PARTIAL | 기준선 Wave B Edit/Play에서 정확히 7 region·21 archetype·42 instance(기존 28+신규 14)·12종 일반 자원 144, stable ID 중복 0, seed/revision 결정성과 새 게임 전용 stock 생성이 5/5 PASS다. | 새 후보에서 카탈로그와 기존 enum ordinal을 재검증한다. |
| GJC-07 | 환경 수색 node의 live 오브젝트·tray | PARTIAL | 기준선 Scene에서 월드 node 접근, 근접 prompt, 발견물 tray, 취소와 현장 복귀가 grant·warp·skip 없이 관찰됐고 GSN-P01/P10이 PASS다. | 새 후보에서 GSN 15/15를 다시 통과한다. |
| GJC-08 | 최초 정보 은폐·take/leave/swap·원자 가방 거래 | PARTIAL | 기준선에서 hidden→partial→depleted와 take/leave/replace/cancel이 실제 ledger와 4칸 가방의 총량 보존·원자 rollback을 통과했다. | 새 후보와 6칸 업그레이드 자연 경로에서 같은 계약을 재확인한다. |
| GJC-09 | 유한 자원·부분 잔류·고갈·재방문 보존 | PARTIAL | 기준선에서 동일 seed 결정성, 취소·화면 전환·재방문 무재추첨과 snapshot 직렬화 복원이 모두 PASS다. | 새 후보에서 새 run 전용 seed·stock 초기화를 재확인한다. |
| GJC-10 | 부순 장벽·제거 위험의 지속 | PARTIAL | 기준선 region snapshot이 부순 장벽과 persistent 제거 위험 ID를 저장·복원하고 새 run 초기화 표면을 제공했다. | 새 후보에서 persistent/transient 위험 계약을 재확인한다. |
| GJC-11 | 벌레·위험 식물 등 환경 위험 및 질병의 예고→노출/효과→대응→완화/치료 | PARTIAL | 기준선 Wave B Play는 질병 자연 lifecycle과 원자성을 통과했다. 현재 작업 트리는 질병과 별도인 환경 위험 production lifecycle을 추가했으나 아직 통합 Play·release 증거가 없다. | 새 후보의 실제 수색 경로에서 최소 2종 환경 위험과 질병 snapshot·원자성·KO/EN/qps를 함께 검증한다. |
| GJC-12 | 가방 4→6·중첩 2 | HUMAN_REQUIRED | 기능과 자동화 계약은 기존 녹색이나 자연 grant/warp 없는 구매→수색→구조 경로와 실제 선택 긴장은 아직 사람이 검증하지 않았다. 수색 tray 거래는 GJC-08과 별도다. | fresh save 자연 trace와 사용자 관찰에서 업그레이드·날짜 지속·새 게임 4칸 초기화를 확인한다. |
| GJC-13 | 같은 run의 위층·지하실 확장 | PARTIAL | 기준선 GWC-P05가 같은 run 위층·지하실 확정·재진입과 composite save v2 복원, 탈출 자원 delta 0을 확인했다. | 새 후보에서 save/re-entry 계약을 재확인한다. |
| GJC-14 | 뗏목 탈출 | PARTIAL | 기준선 GSN-E05/P05와 GWC-P03/P04가 실제 적격 node부터 ending까지 grant·warp·skip 0으로 통과했다. | 새 후보에서 독립 뗏목 상호작용과 ending 1회 기록을 재확인한다. |
| GJC-15 | 대형 연기 탈출 | PARTIAL | 기준선 GWC-P01/P03/P04가 적격 node 부싯돌·3/5 pity·보호 획득과 점화·가시성·대기·재시도·ending을 자연 입력으로 통과했다. | 새 후보에서 연기 비용·원자 대기·ending 1회 기록을 재확인한다. |
| GJC-16 | 무전 탈출 | PARTIAL | 기준선 GWC-E01~03/P01~04가 서로 다른 지역의 무전 3부품, 보호 획득, 수리·주파수 조정·응답·ending의 독립 경로를 통과했다. | 새 후보에서 세 부품 지역 분산과 독립 무전 상호작용을 재확인한다. |
| GJC-17 | 5–10분 loop·25–35분 대표 탈출·30분 profile | HUMAN_REQUIRED | GWC-P07이 grant·warp·skip 0인 대표 seed의 합성 플레이 프로필 28.98분을 PASS했다. 실제 사람이 첫 loop를 5–10분 안에 이해하고 대표 탈출을 25–35분에 끝내는지는 아직 측정하지 않았다. | 동일 Windows 후보에서 실제 사용자 세션 시간을 기록한다. |
| GJC-18 | KO 기본·EN 지원·qps-long QA | PARTIAL | 기준선 GSN-E04/P08과 GWC-E06/P06이 수색·질병·세 탈출·core comic 3장+modifier를 KO/EN/qps-long 1280×800에서 필수 clipping·overflow·offscreen 0으로 통과했다. | 새 후보의 locale 의미와 레이아웃을 다시 캡처·검증한다. |
| GJC-19 | 키보드/마우스·synthetic gamepad 의미 동등 | PARTIAL | 기준선 GSN-P09와 GWC-P03/P04가 수색·가방 거래·세 독립 탈출·ending에서 동일 stable action/result를 확인했다. 물리 장치는 GJC-20으로 분리한다. | 새 후보에서 synthetic 입력 의미 동등을 재확인한다. |
| GJC-20 | 물리 게임패드 실기 | HUMAN_REQUIRED | 자동화와 섞지 않기로 한 실제 장치 결과가 UNVERIFIED다. | 동일 Windows 후보 빌드에서 실제 게임패드로 별도 체크리스트를 수행한다. |
| GJC-21 | ending 3–5장 코믹북 panel | PARTIAL | 기준선 GWC-E06/P06이 live core panel 3장과 행동 modifier 1장, KO/EN/qps-long 레이아웃, album 재진입과 ending/album exactly-once 기록을 통과했다. | 새 후보에서 core 3장+modifier와 stable ending ID를 재확인한다. |
| GJC-22 | 수색 node 시각 리소스의 런타임 채택 | PARTIAL | 기준선에서 채택 Wood/Stone/Food/Salvage source GUID가 실제 compact tray Image에 연결되어 Wave 19 21/21을 통과했다. 현재 작업 트리의 아트·meta 변경은 아직 release 검증 전이다. | 새 후보에서 adopted GUID와 미채택 review 후보 분리를 재검증한다. |
| GJC-23 | 첫 사용자 30분 검증 | HUMAN_REQUIRED | KO 3명·EN 3명의 자연 세션은 실행되지 않았다. 결과를 만들어낼 수 없다. | 자동화 P0가 모두 GREEN인 동일 빌드에서 6세션을 수행하고 성공률·막힘·이해도를 기록한다. |
| GJC-24 | Day 20 게임잼 장기 체류 엔딩 2종 | PARTIAL | 기준선 카탈로그 21개와 독립 long-stay 게이트 15/15가 두 Day 20 terminal, 조기 탈출 우선, 결정론, exactly-once, KO/EN/qps comic, Day 50, grant·warp·skip 0을 PASS했다. | 새 후보에서 21개 카탈로그와 session save v2 회귀를 재검증한다. |

현재 작업 트리 집계는 DONE 0, PARTIAL 20, MISSING 0, HUMAN_REQUIRED 4이다. 소스 `3d64403` 기준선은 Wave C 14/14, GSN 15/15, Wave 19 21/21, Wave 20 16/16, 보강 Wave 17 17/17, Day 20 장기 체류 15/15, 패키지 PKG-I01~I06을 통과한 이력으로만 보존한다. 현재 변경을 커밋하고 같은 소스 정체성으로 전체 게이트·Windows 빌드·smoke·패키지·release 검증을 다시 생성하기 전에는 자동 항목을 DONE으로 승격하지 않는다. 그 뒤에도 GJC-12, GJC-17, GJC-20, GJC-23은 동일 새 후보 빌드에서 사람이 수행해야 한다.

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

Wave C 종료 조건인 GSN-E05/P05/P10, 세 escape 자연 trace, live comic panel, same-run upper+basement는 `3d64403` 기준선에서 GREEN이었다. 현재 작업 트리에서는 새 통합 증거가 생성될 때까지 이 wave와 GJC-24를 PARTIAL로 본다. 자동 선행 게이트가 다시 GREEN이 된 뒤 GJC-12·17·20·23의 사용자/물리 장치 게이트를 실행한다.

## 4. 회귀·중단 규칙

- 각 wave는 compile 0/0, Windows build, hidden smoke, Addressables, canonical camp/module/map, Wave 20 raft 16/16을 보존한다.
- 원자 거래에서 item 생성·소실, 알려진 node reroll, protected part 소실, save/load state 손실 중 하나라도 나오면 P0로 중단한다.
- KO/EN 의미 불일치, 필수 prompt clipping, 입력 수단별 결과 불일치는 P1이며 해당 wave를 GREEN으로 처리하지 않는다.
- 30분 체감, 재미, 70% 성공 목표, 물리 게임패드는 자동화 결과로 대신하지 않는다.
- Steam App ID·상점명·공식 영문 제목은 이 GAME JAM 제출 게이트 밖이며 계속 TBD다.

## 5. 구현·QA 인계 요약

Wave A→B→C와 GJC-24 Day 20 장기 체류 엔딩 2종은 `3d64403` 기준선에서 GREEN으로 닫힌 이력이 있다. 현재 미커밋 구현은 전체 게이트와 release 검증 전이므로 자동 20개 요구를 다시 PARTIAL로 연다. 새 후보가 자동 GREEN이 된 뒤에만 같은 Windows 빌드로 인간 시간 측정·가방 선택 체감·물리 게임패드·KO 3명/EN 3명 첫 사용자 세션을 수행하며, 자동화 결과로 네 인간 검증을 대신하지 않는다.

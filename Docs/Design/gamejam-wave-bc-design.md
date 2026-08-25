# GAME JAM Wave B 구현 정본과 Wave C 보호 부품 경계

기준: origin/codex/gamejam-director-search-node-integration@00e0d5a9df597ab4a9f54bff665291f367d40c92

계약 ID: gamejam.wave-bc.catalog-disease-parts.v1

밸런스 상태: BALANCE_PROVISIONAL

## 1. 현재값과 이번 목표

현재 통합 증거 Artifacts/ParallelQA/20260826T053000Z_gamejam_search_node_integrated_green은 실제 7지역·28 node, GSN 15/15, Wave 19 21/21, Wave 20 16/16과 compile/build/smoke를 GREEN으로 증명한다. 현재 node는 지역마다 네 개이며 일반 loot는 네 ResourceKind 중 두 stack을 seed로 결정한다.

이번 계약은 그 GREEN을 폐기하지 않고 다음 차이를 구현 대상으로 고정한다.

| 항목 | 현행 GREEN | Wave B/C 목표 |
|---|---:|---:|
| region stable ID | 7 | 7, 이름 변경 없음 |
| live node stable ID | 28 | 42: 기존 28 유지 + 신규 14 |
| archetype ID | runtime 미분리 | 기존 정본 21 |
| 일반 자원 총량 | seed별 가변, 4종 enum | 144단위 고정, 12종 stable resource ID |
| 보호 부품 | 돛천 1종 live | 돛천 회귀 유지 + 부싯돌·무전 3부품 |
| 질병 | generic hazard 데이터 | jungle-fever 한 종류의 완전한 lifecycle |

기존 28 ID는 save/snapshot 호환을 위해 절대 바꾸지 않는다. 이전 정본의 node.instance.* 형식은 신규 runtime ID로 강제하지 않는다. archetype ID는 경제·위험 역할이고 instance ID는 현재 node.{region}.{visual-kind}.{ordinal02} 형식을 계속 쓴다.

## 2. 안정 ID와 자원 표현

- region ID 7개와 node.archetype.* 21개는 기존 정본 그대로다.
- 현재 28 node ID는 canonical이다. 지역당 ordinal 02의 신규 ID 두 개만 추가한다.
- visual kind는 placeholder carrier이며 archetype 경제 역할과 동일할 필요가 없다. 아트 채택 상태나 source GUID는 이 계약이 바꾸지 않는다.
- 기존 ResourceKind ordinal Wood=0, Stone=1, Food=2, Salvage=3은 재배열하지 않는다.
- loot와 bag/save에는 StableResourceId를 추가한다. 기존 네 enum은 각각 resource.wood, resource.stone, resource.food, resource.salvage로 읽고, 나머지 여덟 자원은 stable string ID로만 저장한다.
- 이전 snapshot에 StableResourceId가 없으면 기존 enum에서 한 번 변환한다. 새 snapshot은 stable ID를 쓰며 동일 node의 알려진 잔여물과 고갈 상태를 유지한다.
- 일반 자원 수량은 instance 표의 고정 finiteYield다. run seed는 stack 표시 순서와 보호 부품 배치만 바꾸며 총량을 바꾸지 않는다.

## 3. 7지역·21 archetype·42 instance

표의 기존은 현재 GREEN node, 신규는 Wave B에서 더할 node다. 수량은 해당 instance의 전체 run finiteYield다. BC 부품은 이번 작업에서 새로 연결할 부싯돌·무전 부품만 표시한다.

| 지역 | archetype | instance stable ID | 출처 | visual kind | finiteYield | 비용 | 질병/BC 부품 |
|---|---|---|---|---|---|---|---|
| coast.beach | driftline | node.coast.beach.drift-pile.01 | 기존 | DriftPile | salvage 2, wood 1 | low | - |
| coast.beach | driftline | node.coast.beach.tree-hollow.01 | 기존 | TreeHollow | salvage 2, wood 1 | low | - |
| coast.beach | tide-cache | node.coast.beach.grass-patch.01 | 기존 | GrassPatch | food 2, fabric 1 | low | - |
| coast.beach | tide-cache | node.coast.beach.grass-patch.02 | 신규 | GrassPatch | food 2, fabric 1 | low | - |
| coast.beach | storm-wrack | node.coast.beach.rock-crevice.01 | 기존 | RockCrevice | wood 2, salvage 1 | medium | - |
| coast.beach | storm-wrack | node.coast.beach.rock-crevice.02 | 신규 | RockCrevice | wood 2, salvage 1 | medium | - |
| sea.shallows | reef-pocket | node.sea.shallows.rock-crevice.01 | 기존 | RockCrevice | food 2, stone 1 | medium | - |
| sea.shallows | reef-pocket | node.sea.shallows.grass-patch.01 | 기존 | GrassPatch | food 2, stone 1 | medium | - |
| sea.shallows | submerged-crate | node.sea.shallows.drift-pile.01 | 기존 | DriftPile | salvage 2, metal 1 | medium | transceiver |
| sea.shallows | submerged-crate | node.sea.shallows.drift-pile.02 | 신규 | DriftPile | salvage 2, metal 1 | medium | transceiver |
| sea.shallows | wreck-scatter | node.sea.shallows.wreck-locker.01 | 기존 | WreckLocker | wire 2, salvage 1 | high | circuit-board |
| sea.shallows | wreck-scatter | node.sea.shallows.wreck-locker.02 | 신규 | WreckLocker | wire 2, salvage 1 | high | circuit-board |
| forest.grove | deadfall | node.forest.grove.tree-hollow.01 | 기존 | TreeHollow | wood 4 | medium | exposure +1, flint |
| forest.grove | deadfall | node.forest.grove.drift-pile.01 | 기존 | DriftPile | wood 4 | medium | exposure +1, flint |
| forest.grove | forage-patch | node.forest.grove.grass-patch.01 | 기존 | GrassPatch | food 2, medicine 1 | low | treatment source |
| forest.grove | forage-patch | node.forest.grove.grass-patch.02 | 신규 | GrassPatch | food 2, medicine 1 | low | treatment source |
| forest.grove | vine-hollow | node.forest.grove.rock-crevice.01 | 기존 | RockCrevice | fiber 3, wood 1 | medium | - |
| forest.grove | vine-hollow | node.forest.grove.rock-crevice.02 | 신규 | RockCrevice | fiber 3, wood 1 | medium | - |
| ridge.highland | rockfall | node.ridge.highland.rock-crevice.01 | 기존 | RockCrevice | stone 4, metal 1 | high | - |
| ridge.highland | rockfall | node.ridge.highland.rock-crevice.02 | 신규 | RockCrevice | stone 4, metal 1 | high | - |
| ridge.highland | windfall | node.ridge.highland.grass-patch.01 | 기존 | GrassPatch | wood 3, fiber 1 | medium | flint |
| ridge.highland | windfall | node.ridge.highland.tree-hollow.01 | 기존 | TreeHollow | wood 3, fiber 1 | medium | flint |
| ridge.highland | signal-overlook | node.ridge.highland.facility-cabinet.01 | 기존 | FacilityCabinet | fuel 1, medicine 1 | high | transistor |
| ridge.highland | signal-overlook | node.ridge.highland.facility-cabinet.02 | 신규 | FacilityCabinet | fuel 1, medicine 1 | high | transistor |
| cave.island | mineral-seam | node.cave.island.rock-crevice.01 | 기존 | RockCrevice | stone 3, metal 1 | high | - |
| cave.island | mineral-seam | node.cave.island.rock-crevice.02 | 신규 | RockCrevice | stone 3, metal 1 | high | - |
| cave.island | dry-cache | node.cave.island.drift-pile.01 | 기존 | DriftPile | chemicals 2, fuel 1 | medium | flint, transistor |
| cave.island | dry-cache | node.cave.island.facility-cabinet.01 | 기존 | FacilityCabinet | chemicals 2, fuel 1 | medium | flint, transistor |
| cave.island | fungus-ledge | node.cave.island.tree-hollow.01 | 기존 | TreeHollow | stone 1, medicine 1 | high | - |
| cave.island | fungus-ledge | node.cave.island.tree-hollow.02 | 신규 | TreeHollow | stone 1, medicine 1 | high | - |
| cove.wreck | cargo-locker | node.cove.wreck.wreck-locker.01 | 기존 | WreckLocker | salvage 3, metal 2 | medium | - |
| cove.wreck | cargo-locker | node.cove.wreck.drift-pile.01 | 기존 | DriftPile | salvage 3, metal 2 | medium | - |
| cove.wreck | rigging-locker | node.cove.wreck.grass-patch.01 | 기존 | GrassPatch | fabric 2, fiber 1 | medium | - |
| cove.wreck | rigging-locker | node.cove.wreck.grass-patch.02 | 신규 | GrassPatch | fabric 2, fiber 1 | medium | - |
| cove.wreck | engine-bay | node.cove.wreck.rock-crevice.01 | 기존 | RockCrevice | electronics 2, chemicals 1 | high | transceiver, circuit-board |
| cove.wreck | engine-bay | node.cove.wreck.rock-crevice.02 | 신규 | RockCrevice | electronics 2, chemicals 1 | high | transceiver, circuit-board |
| ruins.relay | control-cabinet | node.ruins.relay.facility-cabinet.01 | 기존 | FacilityCabinet | electronics 3, wire 1 | high | circuit-board |
| ruins.relay | control-cabinet | node.ruins.relay.facility-cabinet.02 | 기존 | FacilityCabinet | electronics 3, wire 1 | high | circuit-board |
| ruins.relay | cable-duct | node.ruins.relay.rock-crevice.01 | 기존 | RockCrevice | wire 3, metal 1 | high | transistor |
| ruins.relay | cable-duct | node.ruins.relay.rock-crevice.02 | 신규 | RockCrevice | wire 3, metal 1 | high | transistor |
| ruins.relay | generator-room | node.ruins.relay.grass-patch.01 | 기존 | GrassPatch | fuel 2, metal 1, electronics 1 | high | transceiver |
| ruins.relay | generator-room | node.ruins.relay.grass-patch.02 | 신규 | GrassPatch | fuel 2, metal 1, electronics 1 | high | transceiver |

지역 합계:

| region ID | archetype | instance | 일반 자원 |
|---|---:|---:|---:|
| region.coast.beach | 3 | 6 | 18 |
| region.sea.shallows | 3 | 6 | 18 |
| region.forest.grove | 3 | 6 | 22 |
| region.ridge.highland | 3 | 6 | 22 |
| region.cave.island | 3 | 6 | 18 |
| region.cove.wreck | 3 | 6 | 22 |
| region.ruins.relay | 3 | 6 | 24 |
| 합계 | 21 | 42 | 144 |

전역 일반 자원은 wood 22, salvage 18, food 12, fabric 6, fiber 10, medicine 6, stone 18, metal 14, wire 12, fuel 8, chemicals 6, electronics 12다.

## 4. jungle-fever lifecycle

질병 stable ID는 기존 hazard-profile.disease.jungle-fever를 재사용한다. 전역 family는 hazard.disease다. 같은 질병을 별도 ID로 중복 생성하지 않는다.

| 단계 ID | 진입 조건 | 원자 효과 | 플레이어 정보·행동 |
|---|---|---|---|
| disease.stage.clear | 새 run 또는 회복 완료 | exposure 0 | 상태 없음 |
| disease.stage.telegraphed | forest.grove 지도 선택 또는 deadfall 근접 | 비용·상태 변화 없음 | 모기떼와 열병 가능성, medicine 치료 가능성을 KO/EN으로 예고 |
| disease.stage.exposed | deadfall 두 instance를 각각 처음 commit해 exposure 2 | condition 생성, 즉시 체력 피해 없음 | camp 귀환 전 노출 상태 표시 |
| disease.stage.symptomatic | exposed 뒤 첫 camp 진입 | health -10 정확히 한 번 | 몸살 증상, storage/bag 오브젝트에서 medicine 1 치료 가능 |
| disease.stage.aggravated | symptomatic 상태로 다음 day settlement | health -15 정확히 한 번; 이후 미치료 settlement마다 -15 | 악화 경고와 failure.disease.jungle-fever 원인 표시 |
| disease.stage.recovering | camp에서 medicine 1 치료 transaction 성공 | medicine -1, 추가 악화 중단 | 다음 settlement까지 재노출을 피하라는 표시 |
| disease.stage.cleared | recovering 뒤 신규 노출 없는 다음 settlement | health +5, condition·exposure 제거 | 회복 완료 |

첫 exposure만 받고 두 번째 deadfall을 수색하지 않은 채 하루를 넘기면 exposure가 0으로 감소하며 질병 condition은 생기지 않는다. symptomatic/aggravated 중 같은 node를 다시 열거나 같은 transaction을 재시도해도 체력 피해와 로그가 중복 적용되지 않는다. 치료는 medicine 보유, camp 단계, 올바른 현재 stage를 모두 검증한 뒤 medicine·stage·로그를 한 번에 commit한다. 거절·취소·중복 입력은 모두 무변경이다.

자연 QA 경로는 default seed 15000501, fresh save, grant/warp/skip 없음이다. forest.grove에서 forage-patch 하나를 수색해 medicine을 선택하고 deadfall 두 개를 직접 수색해 exposure를 만든 뒤 귀환한다. 증상 발현 후 치료 경로와, 치료하지 않고 다음 settlement에서 악화되는 경로를 snapshot 분기로 각각 검증한다. 이 질병은 persistentRemoved 대상이 아니며 새 게임에서 초기화된다.

안정 로컬라이제이션 키:

- hazard-profile.disease.jungle-fever.name
- hazard-profile.disease.jungle-fever.telegraph
- hazard-profile.disease.jungle-fever.exposed
- hazard-profile.disease.jungle-fever.symptomatic
- hazard-profile.disease.jungle-fever.aggravated
- hazard-profile.disease.jungle-fever.treatment
- hazard-profile.disease.jungle-fever.recovering
- hazard-profile.disease.jungle-fever.cleared
- failure.disease.jungle-fever

KO는 의미 기준이며 EN은 같은 조건·수치·행동을 전달한다. qps-long은 QA 전용이다.

## 5. 부싯돌·무전 3부품

각 부품은 아래 여섯 instance만 eligible이다. 기존 정본보다 radio alternative를 한 지역씩 늘리는 이유는 3회 hint와 5회 miss 뒤 보장을 실제 유한 node에서 실행할 여섯 후보를 확보하기 위해서다.

| part ID | primary region | alternative regions | eligible instance 6개 |
|---|---|---|---|
| part.smoke.flint | cave.island | ridge.highland, forest.grove | cave drift-pile.01, cave facility-cabinet.01, ridge grass-patch.01, ridge tree-hollow.01, forest tree-hollow.01, forest drift-pile.01 |
| part.radio.transceiver | cove.wreck | sea.shallows, ruins.relay | cove rock-crevice.01/.02, shallows drift-pile.01/.02, ruins grass-patch.01/.02 |
| part.radio.circuit-board | ruins.relay | cove.wreck, sea.shallows | ruins facility-cabinet.01/.02, cove rock-crevice.01/.02, shallows wreck-locker.01/.02 |
| part.radio.transistor | ridge.highland | cave.island, ruins.relay | ridge facility-cabinet.01/.02, cave drift-pile.01, cave facility-cabinet.01, ruins rock-crevice.01/.02 |

배치는 Hash64(runSeed, contractRevision, partId, passIndex, protected-part)로 후보 순위를 정한다. 무전 세 부품은 서로 다른 region에 있어야 하며 초기 배치에서 한 instance에는 최대 한 보호 부품만 둔다. pass 0~15 중 첫 유효 조합을 사용한다. 없으면 transceiver→circuit-board→transistor 순으로, 이미 선택한 region과 node를 제외한 후보 중 hash rank가 가장 낮은 것을 고르는 결정론적 repair를 사용한다.

획득은 일반 bag stack을 차지하지 않고 protected project inventory로 직접 이동한다. 획득 commit 전 취소는 node·part·pity 무변경이며, 성공 뒤 같은 node 재시도는 duplicate delta 0이다. 일반 폐기·가방 교체·도난·캠프 파손은 보호 부품을 삭제하지 않는다.

### 5.1 3/5 pity

- 부품별로 unique eligible node의 완료 수색만 한 번 센다.
- 취소, 비용 부족, 무관 node, 이미 고갈된 node 재열기, 이미 획득한 부품은 miss를 늘리지 않는다.
- miss 3 commit 뒤 hintRevealed=true가 되고 primary/alternative region 단서를 공개한다.
- miss 5 commit 뒤 guaranteeArmed=true가 된다.
- 다음 아직 commit되지 않은 eligible node 결과에 해당 부품을 일반 loot보다 먼저 추가한다. 알려진 loot와 다른 보호 부품을 덮어쓰지 않는다.
- radio pity 후보는 이미 획득한 다른 radio 부품의 region과 달라야 한다.
- 저장 필드는 partId, assignedNodeId, eligibleMissCount, countedNodeIds, hintRevealed, guaranteeArmed, acquired, sourceNodeId, repairState다.

### 5.2 최소 3탈출 seed 감사

GAMEJAM_25_35 profile에서 seed 생성과 확장 지역 해금 직후 escape.raft, escape.smoke, escape.radio 세 경로가 각각 독립 분기에서 completable이어야 한다. data-only flare/beacon은 세 경로 수에 포함하지 않는다.

감사 입력은 42 node의 미고갈 일반 자원, 보호 부품 배치, 지역 해금 그래프, 현재 연구·설비 prerequisite와 선언된 비용이다. 실패 시 아직 공개되지 않은 보호 부품 배치만 repair할 수 있다. 알려진 loot, 남겨둔 일반 자원, 고갈 node와 플레이어가 이미 commit한 비용은 되돌리거나 재생하지 않는다.

## 6. BALANCE_PROVISIONAL 예산

| 경로/압박 | 수량 계약 | 이유 |
|---|---|---|
| 첫 5~10분 | 1지역, 2 node 이상, 6~10단위 공개, 1귀환, 1투자 | 기존 이해 목표 유지 |
| bag 압박 fixture | forest rock-crevice.01 + grass-patch.01 = 7단위, stack cap 2에서 5 stack | 4칸 가방이 한 선택을 요구하지만 귀환은 가능 |
| 대표 smoke | 4~6원정, 10~14수색, 2지역 이상, 22~30단위 이동 | 기존 25~35분 목표 유지 |
| smoke 비용 | wood 12 + fiber 2 + fuel 2 + part.smoke.flint 1 | 이전 정본 그대로, final 아님 |
| radio 표본 비용 | electronics 6 + wire 6 + metal 4 + radio 부품 3 | 두 stage 표본, final 아님 |
| raft GREEN 회귀 | 현행 stage 비용과 rope·sailcloth 계약 | Wave 20 16/16 값을 이 작업에서 변경하지 않음 |
| disease 치료 | medicine 1 | 같은 forest에서 총 2를 얻을 수 있어 별도 강제 원정 없음 |

smoke 비용 commit 뒤 전역 finite stock에는 wood 10, fiber 8, fuel 6, food 12, medicine 6이 남는다. raft와 radio의 재료 범주는 겹침이 제한적이므로 선택 seed는 생존·회복 선택 하나와 다른 두 playable escape의 독립 가능성을 유지해야 한다. 42 node를 전부 수색하게 만들지 않으며 대표 탈출의 목표 수색 수는 10~14에서 늘리지 않는다.

## 7. red-first acceptance와 소유권

| ID | 먼저 실패해야 하는 probe | GREEN 조건 | 미래 구현 소유 task |
|---|---|---|---|
| GWB-CAT-01 | catalog.exact-counts | 7/21/42/144 정확히 일치 | task.gamejam.seven-region-catalog |
| GWB-CAT-02 | catalog.stable-id-preservation | 기존 28 전부 존재, 신규 14, region/archetype/node 중복 0 | task.gamejam.seven-region-catalog |
| GWB-CAT-03 | catalog.resource-ledger | 12 resource 합 144, instance→archetype→region→global 합 일치 | task.gamejam.seven-region-catalog |
| GWB-CAT-04 | catalog.seed-and-snapshot | 42 node 동일 seed 동일 내용, 다른 seed는 보호 배치 variation, 잔량 복원 | task.gamejam.seven-region-catalog |
| GWB-DIS-01 | disease.natural-telegraph-exposure | debug 없이 forest 예고와 두 unique deadfall exposure | task.gamejam.insect-plant-wildlife-disease |
| GWB-DIS-02 | disease.symptom-aggravation | -10과 다음 settlement -15가 단계별 한 번만 적용 | task.gamejam.insect-plant-wildlife-disease |
| GWB-DIS-03 | disease.treatment-atomicity | medicine 1·stage·로그 원자 commit, cancel/duplicate delta 0 | task.gamejam.insect-plant-wildlife-disease |
| GWB-DIS-04 | disease.snapshot-localization | save/restore와 KO/EN/qps 상태 의미·실패 원인 일치 | task.gamejam.insect-plant-wildlife-disease |
| GWB-PART-01 | part.eligible-node-catalog | 네 부품 각각 후보 6, 부품별 후보 내부 중복·dangling 0 | task.gamejam.smoke-radio-material-routes |
| GWB-PART-02 | part.radio-distinct-placement | 무전 3부품 region pairwise distinct, 초기 node collision 0 | task.gamejam.smoke-radio-material-routes |
| GWB-PITY-01 | part.eligible-pity-3-5 | unique eligible miss만 3 hint/5 arm/next guarantee | task.gamejam.smoke-radio-material-routes |
| GWB-SEED-01 | seed.minimum-three-playable | raft/smoke/radio 세 경로 generation audit PASS | task.gamejam.smoke-radio-material-routes |
| GWB-PACE-01 | pacing.gamejam-25-35 | 10~14수색 목표 유지, grant/warp/skip false | task.gamejam.qa-thirty-minute-seven-region-slice |
| GWB-PACE-02 | pacing.post-smoke-reserve | smoke commit 뒤 생존 선택과 두 다른 경로 독립 가능 | task.gamejam.qa-thirty-minute-seven-region-slice |
| GWB-REG-01 | regression.integrated-green-locks | GSN 15/15, Wave19 21/21, Wave20 16/16, build/smoke 유지 | task.gamejam.qa-thirty-minute-seven-region-slice |

미래 구현 파일은 순차 소유한다.

1. catalog 작업이 PrototypeSearchNodeRuntime.cs와 GameSession.cs의 stable resource adapter를 먼저 소유한다.
2. disease 작업은 그 뒤 PrototypeHazardEscapeEnding.cs와 GameSession.cs의 condition transaction만 소유한다.
3. protected part 작업은 catalog 완료 뒤 PrototypeSearchNodeRuntime.cs와 PrototypeHazardEscapeEnding.cs의 candidate/pity/seed audit를 소유한다.
4. QA는 Assets/Editor/ParallelQA 아래 새 red-first probe와 Artifacts/ParallelQA 새 run만 소유한다.

이번 디자인 브랜치는 위 runtime·QA 파일, 씬, Localization Table, 아트 registry/status를 수정하지 않는다. 구현 세 작업은 같은 runtime 파일을 병렬 편집하지 말고 catalog→disease→parts 순으로 통합한다.

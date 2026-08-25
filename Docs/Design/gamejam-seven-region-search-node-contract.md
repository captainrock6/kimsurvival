# 게임잼 7지역 수색 node·유한 자원 계약

계약 ID: `gamejam.seven-region-search-node.v1`

기준 커밋: `5248809018ce934fe328328f194686d8c287734f`

기계 정본: `.forge/packets/gamejam-seven-region-search-node-contract.json` → `.forge/design/project.json#project.sevenRegionSearchNodeContract`

수치 상태: **`BALANCE_PROVISIONAL` — 인간 자연 플레이테스트 전 최종 밸런스로 승격 금지**

## 1. 목적과 경계

이 문서는 이미 정본에 있는 7지역, 5탈출, 환경 수색 오브젝트, 보호 핵심 부품과 3/5 pity를 실제 데이터와 QA fixture로 옮길 수 있게 구체화한 addendum다. 새 탈출법·새 전투·무한 자원 재생을 추가하지 않는다.

고정하는 의미 계약은 다음과 같다.

- 기존 7개 `region.*`, 5개 `escape.*`, 보호 부품 ID와 Day 50/조기 탈출 계약을 바꾸지 않는다.
- 일반 자원과 핵심 부품은 월드 바닥의 작은 개별 아이템이 아니라 환경 수색 node의 발견물이다.
- 같은 run의 node 내용물, 남긴 물건, 고갈, 부순 장벽과 영구 제거 위험은 재방문해도 그대로다.
- 보호 핵심 부품은 일반 가방·폐기·도난·일반 캠프 피해 대상이 아니다.
- 무전의 송수신기·전자기판·트랜지스터는 실제 배치가 세 개의 서로 다른 지역이어야 한다.

이번 표의 node 수, 일반 자원 총량, 수색 비용, 5~10분·25~35분 목표와 연기 경로 재료 예산은 구현 smoke를 위한 `BALANCE_PROVISIONAL`이다. KO/EN 자연 플레이에서 같은 병목이 재현되기 전에는 이 값을 final로 보지 않는다.

## 2. 안정 ID와 결정론

| 대상 | 규칙 | 예시 |
|---|---|---|
| 지역 | 현재 7개 `region.*` ID를 그대로 사용 | `region.coast.beach` |
| archetype | `node.archetype.{regionSlug}.{archetypeSlug}` | `node.archetype.coast-beach.driftline` |
| instance | `node.instance.{regionSlug}.{archetypeSlug}.{ordinal02}` | `node.instance.coast-beach.driftline.01` |
| 배치 seed | `Hash64(runSeed, regionId, contractRevision)` | 지역 배치 전용 stream |
| loot seed | `Hash64(runSeed, regionId, nodeInstanceId, lootTableRevision, "loot")` | node별 격리 stream |
| 위험 seed | `Hash64(runSeed, regionId, nodeInstanceId, visitIndex, "hazard")` | loot stream과 분리 |
| 핵심 부품 seed | `Hash64(runSeed, contractRevision, partId, "protected-part")` | 일반 loot와 분리 |

한 node를 열거나 취소해도 다른 node RNG가 진행되면 안 된다. run snapshot은 `contractRevision`과 `lootTableRevision`을 저장하고, 이어지는 같은 run은 저장된 revision을 사용한다. 새 데이터 revision은 새 run에만 적용한다.

새 run 생성 시 softlock audit가 실패하면 아직 플레이어에게 공개되지 않은 **핵심 부품 배치만** 다시 계산한다. 일반 loot, 알려진 잔여물, 이미 고갈된 node는 어떤 시점에도 재추첨하지 않는다.

## 3. 7지역·21 archetype·42 node 예산

모든 지역은 archetype당 instance 두 개, 지역당 node 여섯 개다. 아래 일반 자원 단위에는 보호 핵심 부품을 포함하지 않는다.

| 지역 안정 ID / KO · EN | 해금 / 위험 | node archetype ×2 | 지역 유한 일반 자원 총량 | 위험 조합 |
|---|---:|---|---|---|
| `region.coast.beach` / 시작 해변 · Starting Beach | 시작 / 1·5 | `node.archetype.coast-beach.driftline`, `.tide-cache`, `.storm-wrack` | 나무 6, 표류물 6, 식량 4, 천 2 = **18** | 모래날벌레, 해안 게, 높은 파도 |
| `region.sea.shallows` / 맹그로브 얕은물 · Mangrove Shallows | 시작 / 2·5 | `node.archetype.sea-shallows.reef-pocket`, `.submerged-crate`, `.wreck-scatter` | 식량 4, 돌 2, 표류물 6, 금속 2, 전선 4 = **18** | 깔따구, 가오리, 쏘는 해조, 수인성 질병, 조수 |
| `region.forest.grove` / 밀림 숲 · Jungle Grove | 시작 / 3·5 | `node.archetype.forest-grove.deadfall`, `.forage-patch`, `.vine-hollow` | 나무 10, 섬유 6, 식량 4, 약품 2 = **22** | 모기떼, 멧돼지, 독성 덩굴, 밀림열, 폭염·비 |
| `region.ridge.highland` / 바위 절벽 · Rocky Highland | 확장 / 4·5 | `node.archetype.ridge-highland.rockfall`, `.windfall`, `.signal-overlook` | 돌 8, 금속 2, 나무 6, 섬유 2, 연료 2, 약품 2 = **22** | 말벌집, 둥지 새, 가시덤불, 상처 감염, 절벽·강풍 |
| `region.cave.island` / 섬 동굴 · Island Cave | 확장 / 4·5 | `node.archetype.cave-island.mineral-seam`, `.dry-cache`, `.fungus-ledge` | 돌 8, 금속 2, 화학재 4, 연료 2, 약품 2 = **18** | 동굴진드기, 박쥐떼, 독성 균류, 포자열, 어둠·낙석 |
| `region.cove.wreck` / 난파선 만 · Wreck Cove | 확장 / 4·5 | `node.archetype.cove-wreck.cargo-locker`, `.rigging-locker`, `.engine-bay` | 표류물 6, 금속 4, 천 4, 섬유 2, 전자부품 4, 화학재 2 = **22** | 바퀴, 쥐떼, 녹슨 덩굴, 녹슨 상처, 날카로운 잔해·침수 |
| `region.ruins.relay` / 폐기상 관측소 · Abandoned Weather Station | 확장 / 5·5 | `node.archetype.ruins-relay.control-cabinet`, `.cable-duct`, `.generator-room` | 전자부품 8, 전선 8, 금속 4, 연료 4 = **24** | 말벌, 뱀 둥지, 전기 덩굴, 곰팡이열, 낙뢰·불안정 계단 |

합계는 **7지역 / 21 archetype / 42 instance / 일반 자원 144단위**다.

### 3.1 archetype별 정확한 총량

| Archetype | instance | 두 instance 전체 유한 발견물 | 수색 비용 band | 핵심 부품 적격 후보 |
|---|---:|---|---|---|
| `node.archetype.coast-beach.driftline` | 2 | 표류물 4, 나무 2 | low | 없음 |
| `node.archetype.coast-beach.tide-cache` | 2 | 식량 4, 천 2 | low | 없음 |
| `node.archetype.coast-beach.storm-wrack` | 2 | 나무 4, 표류물 2 | medium | 돛천, 조명탄 탄약 |
| `node.archetype.sea-shallows.reef-pocket` | 2 | 식량 4, 돌 2 | medium | 없음 |
| `node.archetype.sea-shallows.submerged-crate` | 2 | 표류물 4, 금속 2 | medium | 망가진 무전기, 조명탄 탄약 |
| `node.archetype.sea-shallows.wreck-scatter` | 2 | 전선 4, 표류물 2 | high | 발전기 코일 |
| `node.archetype.forest-grove.deadfall` | 2 | 나무 8 | medium | 부싯돌, 연기 촉매 |
| `node.archetype.forest-grove.forage-patch` | 2 | 식량 4, 약품 2 | low | 없음 |
| `node.archetype.forest-grove.vine-hollow` | 2 | 섬유 6, 나무 2 | medium | 돛천 |
| `node.archetype.ridge-highland.rockfall` | 2 | 돌 8, 금속 2 | high | 없음 |
| `node.archetype.ridge-highland.windfall` | 2 | 나무 6, 섬유 2 | medium | 부싯돌, 연기 촉매 |
| `node.archetype.ridge-highland.signal-overlook` | 2 | 연료 2, 약품 2 | high | 트랜지스터, 발전기 코일 |
| `node.archetype.cave-island.mineral-seam` | 2 | 돌 6, 금속 2 | high | 없음 |
| `node.archetype.cave-island.dry-cache` | 2 | 화학재 4, 연료 2 | medium | 부싯돌, 트랜지스터 |
| `node.archetype.cave-island.fungus-ledge` | 2 | 돌 2, 약품 2 | high | 없음 |
| `node.archetype.cove-wreck.cargo-locker` | 2 | 표류물 6, 금속 4 | medium | 조명탄 탄약 |
| `node.archetype.cove-wreck.rigging-locker` | 2 | 천 4, 섬유 2 | medium | 돛천 |
| `node.archetype.cove-wreck.engine-bay` | 2 | 전자부품 4, 화학재 2 | high | 망가진 무전기, 전자기판, 연기 촉매 |
| `node.archetype.ruins-relay.control-cabinet` | 2 | 전자부품 6, 전선 2 | high | 전자기판 |
| `node.archetype.ruins-relay.cable-duct` | 2 | 전선 6, 금속 2 | high | 없음 |
| `node.archetype.ruins-relay.generator-room` | 2 | 연료 4, 금속 2, 전자부품 2 | high | 발전기 코일 |

### 3.2 전역 유한 일반 자원 합계

| 자원 ID | 수량 | 자원 ID | 수량 |
|---|---:|---|---:|
| `resource.wood` | 22 | `resource.stone` | 18 |
| `resource.salvage` | 18 | `resource.metal` | 14 |
| `resource.food` | 12 | `resource.wire` | 12 |
| `resource.fabric` | 6 | `resource.fuel` | 8 |
| `resource.fiber` | 10 | `resource.chemicals` | 6 |
| `resource.medicine` | 6 | `resource.electronics` | 12 |

수색 비용 표본은 low `일광 1/체력 6`, medium `일광 2/체력 8`, high `일광 2/체력 10`이다. 수색 완료 transaction 때 한 번만 적용하고 발견물 트레이가 열린 동안 추가 위험·시간·체력을 적용하지 않는다. 이 값도 `BALANCE_PROVISIONAL`이다.

## 4. 위험 profile과 제거 범위

`hazard-profile.*`은 9개 기존 전역 위험 family의 지역별 표현이다. 새 전역 위험 종류를 추가하지 않는다.

| 범주 | 전역 family | 예고 | 발생·완화 | 영구 제거 가능 범위 |
|---|---|---|---|---|
| 벌레 | `hazard.insects` | 소리, 둥지, 물린 흔적, 지도 아이콘 | 장비·우회·둥지 제거; 질병 노출 가능 | 각 지역이 선언한 둥지·군락만 |
| 야생동물 | `hazard.wildlife` | 발자국, 울음, 먹이 흔적 | 회피·장비·덫·후퇴 우선, 본격 전투 강제 없음 | 선언된 쥐 둥지 등 고정 원인만 |
| 위험 식물 | `hazard.dangerous-plants` | 고유 실루엣, 포자, 가시 | 보호 장비·도구·우회·제거 | 독성 덩굴·해조·균류·전기 덩굴 등 고정 patch |
| 질병 | `hazard.disease` | 지역 원인과 잠복 상태 | 노출→잠복→증상→악화, 치료 기회 보장 | 질병 자체는 node 제거 상태가 아님 |
| 환경 | `hazard.injury`, `hazard.disaster` | 조수·강풍·낙석·침수·낙뢰 예고 | 장비·날씨 창·경로 선택·회복 | 날씨와 roaming 위험은 재발 가능 |

성공한 영구 제거만 `hazardState=removed`를 한 번 commit한다. 실패·취소·입력 장치 전환은 비용과 상태를 함께 rollback한다. 지도 위험도와 실제 hazard budget은 같은 region profile을 읽어야 한다.

## 5. node 상태와 재방문 snapshot

### 5.1 영구 상태

| 상태 | 플레이어가 보는 것 | 저장 내용 | 재방문 결과 |
|---|---|---|---|
| `node.discovery.hidden` | node 이름·형태, 수색 가능 여부, 감지한 위험 단서; 정확한 발견물·수량·부품명은 숨김 | 안정 ID와 seed, 미수색 | 같은 정보만 보이고 재추첨 없음 |
| `node.discovery.revealed-partial` | 한 번 확인한 정확한 남은 물건과 수량 | `remainingStacks`, `knownRemainingSummary`, 수색 commit | 알려진 잔여물을 바로 표시 |
| `node.discovery.depleted` | 고갈 표식, 보상 없음 | 빈 stack과 고갈 flag | 다시 뒤져도 보상·pity 증가 없음 |
| `barrier.intact` / `.broken` | 장벽 또는 열린 통로 | 장벽 ID별 상태 | 부순 장벽은 복원되지 않음 |
| `hazard.present` / `.removed` | 고정 위험 또는 제거 흔적 | 제거 가능한 위험 ID별 상태 | 제거한 고정 위험은 복원되지 않음 |

필수 snapshot 필드는 `runId`, `runSeed`, 두 revision, `regionId`, `nodeInstanceId`, `discoveryState`, `searchCommitCount`, `remainingStacks`, `knownRemainingSummary`, `barrierState`, `hazardStateById`, `protectedPartReceiptIds`, `lastTransactionId`다. 수색·담기·교체·장벽 파괴·위험 제거가 commit될 때마다 귀환이나 scene unload 전에 저장한다.

### 5.2 일시 UI 상태

`node.ui.near → node.ui.searching → node.ui.loot-open → node.ui.replacing`은 저장하지 않는다. scene 전환 중이라도 마지막 완료 transaction만 복구하고 팝업을 중복 재생하지 않는다.

## 6. 정보 은폐·발견물 선별·가방 교체

1. 첫 수색 전에는 지역 단위의 자원 범주·풍부함과 node archetype만 보인다. exact item, exact quantity, protected part identity는 숨긴다.
2. 수색 완료 시 비용·위험과 node 내용물 확정을 `search` transaction 한 번으로 commit한다.
3. 발견물 트레이에서 정확한 이름·수량, 가방 4칸 또는 6칸, 칸당 중첩 2와 알려진 남은 칸을 함께 본다.
4. `take`는 선택 수량을 node→가방으로, `swap`은 가방 stack→node와 node stack→가방을 한 transaction으로 옮긴다. 한쪽 실패 시 양쪽 모두 원상복구한다.
5. `leave` 또는 트레이 닫기는 일반 발견물을 움직이지 않고 node에 그대로 남긴다. 다음 방문에는 `알려진 잔여물`로 정확히 표시한다.
6. 수색 완료 전 취소는 비용·위험·공개·pity를 전혀 적용하지 않는다. 완료 후 트레이 닫기는 이미 완료된 수색 비용과 공개를 유지하되, 성공한 take/swap 외 물품은 이동하지 않는다.
7. 보호 핵심 부품은 확인 획득 시 일반 가방이 아닌 project inventory로 들어가며 영수증 피드백만 띄운다.

원자성 키는 `{runId}:{nodeInstanceId}:{searchEpoch}:{actionNonce}`다. 중복 입력, locale 변경, 키보드↔게임패드 전환은 같은 키를 두 번 commit하지 않는다.

## 7. 보호 핵심 부품 분산과 pity

| 핵심 부품 | Primary 지역 | Alternative 지역 | 비고 |
|---|---|---|---|
| `part.raft.sailcloth` | 시작 해변 | 난파선 만, 밀림 숲 | 뗏목 돛천 |
| `part.smoke.catalyst` | 밀림 숲 | 바위 절벽, 난파선 만 | 보호 호환 부품; 별도 recipe 선언 없이는 연기 완료의 추가 필수조건으로 만들지 않음 |
| `part.smoke.flint` | 섬 동굴 | 바위 절벽, 밀림 숲 | 대형 연기 부싯돌 |
| `part.radio.transceiver` | 난파선 만 | 맹그로브 얕은물 | 망가진 무전기 |
| `part.radio.circuit-board` | 폐기상 관측소 | 난파선 만 | 망가진 전자기판 |
| `part.radio.transistor` | 바위 절벽 | 섬 동굴 | 트랜지스터 |
| `part.flare.cartridge` | 난파선 만 | 시작 해변, 맹그로브 얕은물 | 조명탄 탄약 |
| `part.beacon.generator-coil` | 폐기상 관측소 | 맹그로브 얕은물, 바위 절벽 | 신호탑 발전기 코일 |

무전 3부품은 alternative와 pity를 사용해도 실제 획득 지역이 서로 달라야 한다.

- 적격 miss 3회: 해당 부품의 alternative 출처 단서를 공개한다.
- 적격 miss 5회 뒤: 다음 **아직 commit되지 않은 적격 검색 결과**에서 일반 선택 loot보다 먼저 해당 부품을 보장한다.
- 적격 검색은 missing 부품의 primary/alternative 지역이면서 archetype의 `eligiblePartIds`에 그 부품이 있고, 수색 완료가 commit된 경우만 센다.
- 취소, 실패, 무관 지역, 무관 archetype, 이미 가진 부품, 다른 중복 부품을 낸 검색은 세지 않는다.
- 보장은 알려진 loot나 다른 보호 부품을 덮어쓰지 않는다.

획득 부품은 일반 폐기·가방 교체·도난·일반 캠프 파손으로 삭제되지 않는다. 선언된 위험이 손상을 일으키면 삭제 대신 `repairable` 상태와 결정론적 수리 경로를 남긴다.

## 8. `SAMPLE_ONLY` 페이싱·자원 예산

### 8.1 5~10분 핵심 루프

| 지표 | 임시 목표 |
|---|---:|
| 진입 지역 | 1곳 |
| 완료 수색 | 2개 이상 |
| 공개 일반 자원 | 6~10단위 |
| 귀환 | 1회 |
| 캠프 투자 | 1회 |

첫 두 node는 최대 8단위를 운반할 수 있지만 서로 다른 일반 자원 stack을 최소 5종 공개해야 한다. 기본 4칸·중첩 2에서 모든 것을 자동으로 담지 못해 `담기/남겨두기/교체` 중 하나를 이해시키되, 첫 귀환 자체를 막지는 않는다. 시작 지역 두 곳 이상과 각 지역 node 하나 이상은 즉시 적격이어야 한다.

### 8.2 25~35분 대표 탈출

대표 경로는 `escape.smoke`다. 이는 다른 네 경로의 최종 밸런스를 확정하지 않는다.

| 지표 | 임시 목표 |
|---|---:|
| 자연 원정 | 4~6회 |
| 완료 수색 | 10~14개 |
| 서로 다른 지역 | 2곳 이상 |
| node→가방·project 이동 일반 자원 | 22~30단위 |
| 보호 부품 | `part.smoke.flint` 1개 |
| 연기 표본 비용 | 나무 12 + 섬유 2 + 연료 2 |

선정 seed에서 이 연기 비용을 commit한 뒤에도 생존·회복 선택 하나와 다른 탈출법 두 개 이상이 완성 가능해야 한다. 목표 시간, 수색 횟수와 재료 비용은 모두 `BALANCE_PROVISIONAL`이다.

## 9. seed softlock 감사와 장기 체류

감사는 새 run 생성, 확장 지역 해금, 핵심 부품 소비·손상, Day 35 정산, Day 49 정산에 실행한다.

감사 입력은 현재 보유 자원, 접근 가능한 미고갈 node, 미공개 적격 node, 완료 milestone, 선언된 해체 환급, 보호 부품·수리 상태와 남은 날이다. 다음 두 조건을 동시에 만족해야 PASS다.

1. 현재 상태에서 완성 가능한 탈출법이 최소 3개다.
2. 5개 탈출법이 동시에 막힌 상태가 아니다.

감사 복구는 미공개 핵심 부품의 적격 node 재배치 또는 pity 진행만 허용한다. 알려진 일반 loot, 남겨둔 물건, 고갈 node를 재생하지 않는다.

유한 expedition node는 run 동안 재생하지 않는다. 장기 체류 엔딩은 모든 node 고갈을 요구하지 않으며, 남겨두거나 아껴 쓰는 플레이도 유효하다. 빗물받이와 이미 정의된 농사·사냥/덫 유지 생산은 finite node stock 밖의 독립 profile이다. 이 문서는 새 생산 기능이나 최종 생산량을 약속하지 않는다. 선택한 장기 체류 profile은 시작 stock + 접근 가능한 식량·약품 + 활성화된 유지 생산의 실제 공식으로 판정일까지 생존·회복이 가능한지 별도 audit한다. 게임잼 임시 장기 체류일과 본편 Day 50은 별도 profile이다.

## 10. 한국어 정본·영어·향후 언어 계약

공식 영문 게임 제목은 `TBD`다. KO가 의미와 코미디 톤의 정본, EN이 첫 지원 언어, `qps-long`은 QA 전용이다. JA, `zh-Hans`, `zh-Hant`, ES는 같은 키와 named token으로 추가한다.

| 안정 키 | KO 정본 | EN 의도 | qps-long 예문 |
|---|---|---|---|
| `ui.search-node.prompt` | `{inputGlyph} {nodeName} 뒤지기` | `{inputGlyph} Search {nodeName}` | `⟦{inputGlyph} Şëårçh thrøügh {nodeName} før üsëfül şüpply ïtëmş⟧` |
| `ui.search-node.state.hidden` | 미확인 | Unsearched | `⟦Ünşëårçhëd çøntëntş⟧` |
| `ui.search-node.state.partial` | 남은 물건 있음 | Items remaining | `⟦Knøwn ïtëmş rëmåïn hërë⟧` |
| `ui.search-node.state.depleted` | 고갈 | Depleted | `⟦Fülly dëplëtëd⟧` |
| `ui.search-node.known-remaining` | `남겨둔 물건: {itemList}` | `Known items left: {itemList}` | `⟦Knøwn ïtëmş ştïll lëft hërë: {itemList}⟧` |
| `ui.search-node.protected-part-received` | `핵심 부품 확보: {partName}` | `Project part secured: {partName}` | `⟦Prøtëçtëd prøjëçt pårt şëçürëd: {partName}⟧` |
| `ui.search-node.error.bag-full` | 가방이 가득 참 · 교체할 물건 선택 | Bag full · Choose an item to swap | 장문 상태에서도 CTA 유지 |
| `ui.search-node.error.equipment` | 필요 장비: `{equipmentName}` | Requires `{equipmentName}` | named token 순서 변경 가능 |

추가 안정 키는 `ui.search-node.action.take`, `.take-all`, `.swap`, `.leave`, `.close`, `ui.search-node.hazard-removed`, `.barrier-broken`, `.error.depleted`, `.error.transaction`이다. region 명칭 키는 `{regionId}.name`, archetype 명칭 키는 `{archetypeId}.name`을 사용한다. 저장 데이터에는 번역 문자열이 아니라 안정 ID만 쓴다. glyph·node명·동사·수량을 코드에서 문자열 결합하지 않고, 언어별 템플릿이 어순을 소유한다.

## 11. 구현·QA acceptance matrix

| Gate | 구현/플레이어 결과 | 결정론 probe·증거 계약 | 현재 상태 |
|---|---|---|---|
| `SN-CAT-01` | 정확히 7지역·21 archetype·42 instance, 일반 자원 합 144 | `catalog.seven-region-search-node` → `Artifacts/Verification/seven-region-search-node/catalog.json` | 계약 완료 |
| `SN-SEED-01` | 같은 seed+revision은 byte-equal, 다른 seed는 ID를 바꾸지 않고 배치 하나 이상 변화 | `seed.determinism-and-variation` → `seed-audit.json` | 구현 필요 |
| `SN-RNG-02` | 한 node의 수색·취소가 다른 node 결과를 바꾸지 않음 | `seed.stream-isolation` → `rng-isolation.json` | 구현 필요 |
| `SN-INFO-01` | 첫 완료 전 exact loot 숨김, 완료 뒤 exact/stable 공개 | `search.hidden-to-revealed` → `info-hiding.json` | 구현 필요 |
| `SN-LOOT-01` | 4/6칸·중첩 2에서 take/leave/swap 보존 법칙과 rollback | `search.selection-atomicity` → `loot-atomicity.json` | 구현 필요 |
| `SN-PERSIST-01` | 미확인·부분·고갈·장벽·제거 위험이 강제 귀환과 재방문에 보존; 새 run 초기화 | `search.revisit-snapshot` → `revisit.json` | 구현 필요 |
| `SN-HAZ-01` | 지역 조합과 예고가 일치하고 선언된 고정 위험만 영구 제거 | `hazard.region-profile` → `hazard-profile.json` | 구현 필요 |
| `SN-PART-01` | 8부품 적격 배치, 무전 3지역 분산, 삭제·복제 없음 | `part.distribution-and-protection` → `protected-parts.json` | 구현 필요 |
| `SN-PITY-01` | 적격 miss만 3 hint/5 guarantee를 정확히 진행 | `part.eligible-search-pity-3-5` → `pity.json` | 구현 필요 |
| `SN-SOFTLOCK-01` | 다섯 checkpoint에서 최소 3탈출법, 5개 동시 차단 없음 | `escape.minimum-three-route-audit` → `softlock.json` | 구현 필요 |
| `SN-PACE-01` | debug 도움 없이 5~10분 안에 2수색→선별→귀환→투자 | 수동 결과지 | **UNRUN** |
| `SN-PACE-02` | 자연 경로 25~35분 연기 탈출, 생존 선택 1·대체 경로 2 유지 | 수동 결과지 | **UNRUN** |
| `SN-LONG-01` | finite node 고갈이 장기 체류 profile을 자동 무효화하지 않음 | `ending.long-stay-finite-stock` → `long-stay.json` | 구현 필요 |
| `SN-L10N-01` | KO/EN 의미 동일, qps-long CTA·상태 무절단, 저장 locale 비의존 | `localization.search-node-contract` → `localization.json` | QA 필요 |
| `SN-INPUT-01` | 키보드/마우스와 게임패드 transaction 결과 동일 | `input.search-node-parity` → `input-parity.json` | 물리 게임패드 **UNVERIFIED** |

자동 probe는 결정론·원자성·카탈로그만 증명한다. 5~10분 이해와 25~35분 자연 탈출, 재미와 피로는 인간 플레이 결과를 대신하지 않는다. 수동 결과 파일은 실제 세션이 생기기 전 생성하거나 PASS로 표시하지 않는다.

## 12. 조정 순서와 위험

플레이테스트 뒤 한 build에서 한 축만 바꾼다.

1. node 발견·known remaining 정보 계층
2. protected part 적격 분산·pity
3. node 수·일반 자원 총량
4. 수색 일광·체력 비용
5. 탈출 프로젝트 재료 예산

먼저 발견성 실패를 고치고, 정보가 전달된 세션에서만 경제 병목을 판단한다. 단일 owner session은 P0 재현 외에는 수치 변경 근거가 아니다. KO/EN에서 같은 경제 막힘이 유효 세션 3개 이상 반복되거나 정해진 cohort 게이트를 만족할 때만 한 축의 변경 후보를 연다.

핵심 위험은 (a) 42 node가 30분 빌드에서 반복처럼 보이는 것, (b) 4칸 가방과 hidden loot가 결합해 임의성으로 느껴지는 것, (c) 대형 연기 나무 소비가 건설과 충돌하는 것, (d) radio 3지역 분산이 unlock 시간을 과도하게 늘리는 것, (e) 유한 food와 장기 체류 profile이 서로 다른 경제 공식을 쓰는 것이다. 각각은 위 matrix의 pacing, seed, long-stay gate가 증거를 만들기 전 수치로 확정하지 않는다.

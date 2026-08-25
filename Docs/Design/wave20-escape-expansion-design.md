# Wave 20 잔여 탈출법 플레이 가능 확장 계약

> 통합 주의: 이 문서는 조명탄·고지대 중계소 설계 당시의 분기 기준선을 기록한다. 현재 정본은 최소 7지역, 질병 포함 위험 계약, 21엔딩, 환경 수색 오브젝트 기반 획득이며 `escape.raft`는 플레이 가능 상태다. 충돌하는 내용은 `wave20-four-result-integration-reconcile.md`와 `.forge/design/`을 우선한다.

상태: `DESIGN_COMPLETE / IMPLEMENTATION_AND_RED_FIRST_UNRUN`

계약 ID: `escape.wave20.flare-beacon-playable.v1`

기준선: `origin/master@09ae2a6d578eb4dcbf11b9c571f57f640b88d969`

밸런스 상태: `SAMPLE_ONLY_NOT_FINAL_FIFTY_DAY_BALANCE`

## 1. 목적과 비변경 경계

이 문서는 `escape.flare`와 `escape.beacon`을 data-only catalog에서 실제 이동·지역 수색·현장 오브젝트 상호작용으로 완료 가능한 경로로 전환할 구현 계약이다.

- `escape.raft`, `facility.shore-launch`, `part.raft.sailcloth`와 뗏목의 선체·날씨 창·항해 보급 계약은 읽기 전용이며 수정하지 않는다.
- Day 50 종료, 어느 날이든 충족된 조기 탈출의 즉시 성공, Day 50 당일 `escape_complete` 우선, Day 51 금지는 고정이다.
- 기존 5개 `escape.*`, 6개 `region.*`, 7개 `hazard.*`, 19개 `ending.*` 안정 ID와 softlock 최소 3경로 보호를 바꾸지 않는다.
- 아래 비용·준비 기간·창 빈도·실패 확률·행동 점수 임계값은 구현 smoke용 `SAMPLE_ONLY`다. 인간 자연 경로 증거 없이 최종 수치로 승격하지 않는다.
- 새 탈출법·새 엔딩·새 자원·전역 완료 메뉴를 추가하지 않는다.

## 2. 구현 순서와 차별화

구현 순서는 **뗏목 계약 보존 → 조명탄 → 고지대 중계소**다.

1. `escape.flare`: 한 개 고정 발사기, 한 발 escrow, 짧은 목격 창과 위험 감수 발사로 단발 transaction을 먼저 검증한다.
2. `escape.beacon`: 검증된 직접 상호작용·window·idempotency 기반 위에 원격 고정 폐시설, 세 물리 milestone과 재해 복구 overlay를 얹는다.

| 경로 | 지역·장소 | 직접 행동 문법 | 시간 창 | 실패 뒤 보존 | 다른 경로와 다른 핵심 축 |
|---|---|---|---|---|---|
| `escape.raft` | 해변·얕은 바다·난파선, `facility.shore-launch` | 선체 제작→보급 적재→출항 | 안전 조류·해상 날씨 | 선체·돛천·보급 stage | 해상 이동, 다량 보급, 선체 |
| `escape.smoke` | 숲·고지대, `facility.smoke-beacon` | 연료 투입→점화→여러 날 유지 | 바람·비와 가시성 | 설비·촉매, 선언된 일일 연료 외 보존 | 다일 유지, 연소, 고지대 |
| `escape.radio` | 난파선·폐중계소, `facility.radio-bench` | 전원→튜닝→반복 송신 | 전력·건조·응답 창 | 송수신기·수리 가능한 부품 | 전자·주파수·반복 송신 |
| `escape.flare` | 해변·얕은 바다·난파선, `facility.flare-launcher` | 발사기 설치→탄두 준비→한 발 장전→목격 창 발사 | confirmed/uncertain 단발 목격 창 | 발사 전 escrow 반환, 발사 뒤 선택한 cartridge만 소비 | **단발 소비**, **짧은 목격 타이밍**, **위험 감수 여부** |
| `escape.beacon` | 고지대·폐중계소·숲, `facility.relay-station` | 접근 복구→구조 보수→발전기→신호두→현장 점등 | 맑은 밤 또는 완화된 폭풍의 눈 | 완료 milestone·coil 보존, 전원/접근 overlay만 복구 | **원격 고정 폐시설**, **세 공간 milestone**, **재해 복구** |

조명탄과 중계소는 어느 쪽도 `재료 제출→같은 제작 버튼→완료`가 아니다. 각각 타이밍을 읽고 한 발을 결정하거나, 지역 안의 서로 다른 설비 지점을 직접 복구해야 한다.

## 3. canonical 데이터

### 3.1 `escape.flare`

| 필드 | 고정 값 |
|---|---|
| 지역 | `region.coast.beach`, `region.sea.shallows`, `region.cove.wreck` |
| 연구 | `research.pyrotechnics`, `research.signal-timing` |
| 설비 | `facility.flare-launcher` |
| 고정 앵커 | `anchor.camp.flare-launcher` — 캠프 해안 가장자리, 자유 배치·재배치 불가 |
| 핵심 부품 | `part.flare.cartridge` |
| primary / alternative | `region.cove.wreck` / `region.coast.beach`, `region.sea.shallows` |
| 재료 범주 | `resource.chemicals`, `resource.metal`, `resource.fabric`, `resource.fuel` |
| 위험 | `hazard.injury`, `hazard.camp-damage`, `hazard.disaster` |
| 실패 사건 | `event.flare.misfire`, `event.flare.no-witness`, `event.flare.launcher-damaged` |
| 변형 사건 | `event.flare.daylight-fireworks`, `event.flare.perfect-window` |
| 준비 기간 | 4~8일 표본, `SAMPLE_ONLY` |

현재 data-only 코드가 얕은 바다 또는 천·연료를 생략해도 그것을 새 정본으로 해석하지 않는다. 구현과 probe는 위 전체 집합을 사용한다.

### 3.2 `escape.beacon`

| 필드 | 고정 값 |
|---|---|
| 지역 | `region.ridge.highland`, `region.ruins.relay`, `region.forest.grove` |
| 연구 | `research.structural-repair`, `research.generator-repair` |
| 설비 | `facility.relay-station` |
| 고정 앵커 | `anchor.region.relay-station` — 폐중계소 지역의 월드 앵커, 자유 배치·재배치 불가 |
| 하위 상호작용 지점 | `target.beacon.access`, `target.beacon.generator`, `target.beacon.signal-head` |
| 핵심 부품 | `part.beacon.generator-coil` |
| primary / alternative | `region.ruins.relay` / `region.sea.shallows`, `region.ridge.highland` |
| 재료 범주 | `resource.wood`, `resource.stone`, `resource.metal`, `resource.wire`, `resource.fuel` |
| 위험 | `hazard.disaster`, `hazard.injury`, `hazard.camp-damage` |
| 실패 사건 | `event.beacon.power-drop`, `event.beacon.stair-collapse`, `event.beacon.lightning-trip` |
| 변형 사건 | `event.beacon.overpowered`, `event.disaster.perfect-mitigation` |
| 준비 기간 | 10~18일 표본, `SAMPLE_ONLY` |

현재 data-only 런타임의 `research.power-grid`, `research.relay-restoration`, `facility.ridge-beacon`, `part.beacon.lens`는 정본 ID가 아니다. 새 save·표시·로그·probe에서 사용하면 red-first FAIL이다. 개발 fixture 호환이 필요하면 로드 시 내부 변환만 허용하고 이후 snapshot은 canonical ID만 기록한다.

## 4. 공통 직접 상호작용·포커스 계약

### 4.1 공간 흐름

`far → near → popup-open → preview/confirm → success|failure|cancel → field-return`을 기존 `feature.camp-object-interaction` 의미 그대로 사용한다.

- far: prompt와 popup 0개.
- near: 거리·가시선·동일 층 조건을 만족한 대상 하나만 latch하고 compact prompt 정확히 1개.
- popup-open: 이동과 월드 Interact를 잠그고 해당 설비의 요구·다음 milestone·창·위험만 표시한다.
- cancel: 자원·RNG·프로젝트·위치·방향을 바꾸지 않고 동일 대상 앞에 복귀한다.
- success/failure: 결과 카드 확인 뒤 동일 현장으로 복귀한다. terminal 성공만 결과 화면으로 전환한다.
- flare는 캠프 해안의 전용 앵커, beacon은 수색 지도에서 폐중계소로 이동한 뒤 현장 고정 앵커를 직접 사용한다. 캠프 전역 메뉴에서 원격 완료할 수 없다.

compact prompt는 채택된 `ui.camp-contextual-interaction.compact-a`를 따른다. 1280×800에서 내레이션 아래 한 줄, 분리 glyph/TMP, qps-long 포함 520×64px 이내이며 플레이어·설비·보행로를 가리지 않는다.

### 4.2 입력·포커스

| 의미 action | 키보드·마우스 | 게임패드 | 규칙 |
|---|---|---|---|
| `input.interact` | 현재 Interact binding | South/Interact binding | 같은 대상과 popup을 연다 |
| `input.navigate` | 방향/포인터 | D-pad/left stick | 같은 focus graph를 사용한다 |
| `input.confirm` | Confirm/click | South/Confirm | enabled와 disabled 의미가 동일하다 |
| `input.cancel` | Escape/Cancel | East/Cancel | mutation 없이 동일 현장 복귀 |
| `input.tab-detail` | 탭/포인터 | shoulder | 요구·창·위험 상세 사이만 이동, 게임 상태 불변 |

locale 또는 입력 장치 전환은 focus 가능한 action ID, latch 대상, window snapshot, RNG stream을 바꾸지 않는다.

## 5. `escape.flare` 상태기계

상태는 조합 폭발을 막기 위해 `projectStage`, `launcherState`, `cartridgeState`, `windowState` 네 축으로 저장하고 아래 우선순위로 한 개의 player state를 resolve한다.

### 5.1 상태 우선순위

| 우선 | player state ID | 진입 조건 | 가능한 직접 행동 | mutation |
|---:|---|---|---|---|
| 1 | `escape.flare.state.completed` | terminal commit 완료 | 결과 보기만 | 없음 |
| 2 | `escape.flare.state.launcher-damaged` | `launcherState=damaged` | `action.flare.repair-launcher` | SAMPLE repair cost와 working 상태를 한 번 commit |
| 3 | `escape.flare.state.locked-research` | 두 연구 중 하나 이상 미완료 | 요구 보기 | 없음 |
| 4 | `escape.flare.state.missing-facility` | 전용 앵커 미설치 | `action.flare.install-launcher` | 설치 비용과 facility state 원자 commit |
| 5 | `escape.flare.state.missing-part` | cartridge 미보유·미escrow | 출처 보기 | 없음 |
| 6 | `escape.flare.state.payload-missing` | 화학재·연료 payload 미준비 | `action.flare.prepare-payload` | payload 비용을 project inventory로 이동 |
| 7 | `escape.flare.state.ready-to-load` | launcher working, payload·cartridge 보유 | `action.flare.load-cartridge` | protected inventory→project escrow 이동, 아직 소비 아님 |
| 8 | `escape.flare.state.window-closed` | loaded, `windowState=closed` | 다음 창 보기, unload | unload는 cartridge·payload를 원소유 위치로 원자 반환 |
| 9 | `escape.flare.state.window-uncertain` | loaded, 불확실한 목격 신호 | `action.flare.fire-risky`, unload | 실패 가능성과 소비 범위를 확인 뒤에만 confirm |
| 10 | `escape.flare.state.window-confirmed` | loaded, 목격 대상·가시성 확인 | `action.flare.fire` | shot resolver 한 번 실행 |
| 11 | `escape.flare.state.resolving` | confirm transaction 진행 중 | 입력 잠금 | 완료 또는 선언된 실패 전부 commit/rollback |

### 5.2 목격·날씨 창

- `window.flare.closed`: 목격 대상이 없거나 비·높은 파도 등 가시성 조건이 미달이다. 발사 confirm을 막고 자원을 소비하지 않는다.
- `window.flare.uncertain`: 멀리 불빛·항적은 있으나 목격이 확정되지 않았다. `fire-risky`만 허용하며 `event.flare.no-witness` 가능성과 cartridge 1개 소비를 명시한다.
- `window.flare.confirmed`: 목격 대상과 허용 가시성이 함께 확인됐다. 일반 발사를 허용한다.
- `window.flare.perfect`: confirmed의 변형으로 `event.flare.perfect-window`를 남긴다. 별도 엔딩 ID나 보너스 생산 기능을 만들지 않는다.
- 지도는 지역별 `part.flare.cartridge` 출처, 현재 날씨, `witness forecast`, 필요 연구·장비를 범주형으로 보여 준다. 정확한 성공 확률은 숨기되 `closed/uncertain/confirmed` 의미는 숨기지 않는다.

### 5.3 실패·재시도

- `event.flare.misfire`: 현재 창에 사전 표시된 misfire 위험이 있을 때만 가능하다. 선택한 cartridge와 payload만 한 번 소비하고 launcher는 결과가 선언한 경우에만 damaged가 된다.
- `event.flare.no-witness`: uncertain 창에서 위험 발사를 선택했을 때만 가능하다. 선택한 cartridge와 payload를 한 번 소비하며 launcher·연구·facility는 보존한다.
- `event.flare.launcher-damaged`: 발사 전에는 confirm을 막고 cartridge escrow를 소비하지 않는다. 수리 뒤 같은 창이 아직 유효하면 재시도할 수 있다.
- spent 뒤 `part.flare.cartridge`가 다시 missing이면 같은 primary/alternative와 eligible-search pity를 사용한다. 실패·취소·무관 수색은 pity를 증가시키지 않는다.
- confirmed 안전 창을 UI가 표시한 뒤 숨겨진 이유로 no-witness를 내면 FAIL이다.

## 6. `escape.flare` 최소 플레이 가능 세로 조각

SAMPLE fixture ID: `slice.escape.flare.playable.v1`

| 구간 | 자연 행동 | 필수 증거 |
|---|---|---|
| 1 | fresh save에서 캠프 지도 오브젝트에 접근하고 해변·얕은 바다·난파선 만의 cartridge 출처·날씨를 비교 | `grant=0`, `warp=0`, region IDs와 forecast |
| 2 | 실제 수색·귀환으로 cartridge와 재료를 획득하고 작업대에서 두 연구를 완료 | protected part, 연구 event, debug 0 |
| 3 | 캠프 해안 앵커에 발사기를 설치하고 직접 접근해 payload 준비·장전 | 서로 다른 interaction IDs, escrow snapshot |
| 4 | confirmed 창까지 자연 행동·정산을 진행하고 발사 | window snapshot과 단일 confirm transaction |
| 5 | 즉시 `escape.flare` terminal과 하나의 flare ending을 표시 | `escape.completed`, ending ID, 중복 소비·해금 0 |

목표 실행 시간은 30분 이하 deterministic smoke 표본이고 준비 4~8일은 `SAMPLE_ONLY`다. 별도 실패 fixture는 uncertain no-witness 또는 misfire 중 하나를 실행한 뒤 replacement cartridge를 자연 수색해 재시도 가능함을 증명한다.

SAMPLE 비용 profile:

| cost ID | 수치 |
|---|---|
| `sample.cost.flare.launcher` | metal 2 + fabric 1 |
| `sample.cost.flare.payload` | chemicals 1 + fuel 1 |
| `sample.cost.flare.shot` | escrow `part.flare.cartridge` 1, resolve 때만 소비 |
| `sample.cost.flare.repair` | metal 1 |

## 7. `escape.beacon` 상태기계

beacon은 `structure`, `power`, `signalHead` milestone과 `access`, `powerOverlay`, `window`를 별도 저장한다. 완료 milestone은 위험·실패·route 전환으로 감소하지 않는다.

### 7.1 상태·직접 행동

| player state ID | 조건 | 현장 target/action | 성공 결과 |
|---|---|---|---|
| `escape.beacon.state.locked-region` | relay anchor 미발견·미해금 | 지도에서 경로·장비 보기 | mutation 없음 |
| `escape.beacon.state.locked-research` | 두 연구 중 하나 이상 미완료 | 요구 보기 | mutation 없음 |
| `escape.beacon.state.access-blocked` | `event.beacon.stair-collapse` active | `target.beacon.access` / `action.beacon.repair-access` | access usable, 기존 milestone 보존 |
| `escape.beacon.state.structure-ready` | structure 미완료, access usable | `target.beacon.access` / `action.beacon.repair-structure` | `milestone.beacon.structure-repaired` |
| `escape.beacon.state.missing-coil` | structure 완료, coil 미보유 | generator에서 출처 보기 | mutation 없음 |
| `escape.beacon.state.power-ready` | coil·재료 보유, power milestone 미완료 | `target.beacon.generator` / `action.beacon.restore-power` | coil을 project inventory에 설치, `milestone.beacon.power-restored` |
| `escape.beacon.state.signal-ready` | structure·power 완료, signal 미완료 | `target.beacon.signal-head` / `action.beacon.align-signal` | `milestone.beacon.signal-head-aligned` |
| `escape.beacon.state.circuit-tripped` | lightning trip 또는 power drop | generator / `action.beacon.reset-power` | powered=true, 모든 milestone·coil 보존 |
| `escape.beacon.state.window-closed` | 세 milestone 완료, 활성 창 아님 | signal head에서 forecast 보기 | mutation 없음 |
| `escape.beacon.state.window-open` | `window.beacon.clear-night` | signal head / `action.beacon.activate` | standard activation resolve |
| `escape.beacon.state.storm-eye-window` | perfect disaster mitigation + rare window | signal head / `action.beacon.activate` | rare candidate flag와 activation resolve |
| `escape.beacon.state.overdrive-ready` | normal window + SAMPLE extra fuel + building 조건 | signal head / `action.beacon.activate-overdrive` | `event.beacon.overpowered`와 activation resolve |
| `escape.beacon.state.completed` | terminal commit 완료 | 결과 보기만 | mutation 없음 |

### 7.2 날씨·위험·복구

- `window.beacon.clear-night`: 맑은 밤의 가시성 창. 정상·comic 후보 활성화를 허용한다.
- `window.beacon.storm-eye`: `event.disaster.perfect-mitigation`가 같은 hazard chain에 있고 폭풍의 눈 안전 구간이 telegraph된 경우에만 허용한다. 희귀 엔딩 조건을 만족하지 않아도 활성화 자체는 정상 성공할 수 있다.
- 비·번개 warning 동안 activation은 막지만 구조 보수 행동을 날짜로 막지 않는다. 실제 lightning resolve만 `event.beacon.lightning-trip`을 적용한다.
- `event.beacon.power-drop`: `powered=false` overlay만 바꾸며 structure/power/signal milestone과 coil을 지우지 않는다.
- `event.beacon.stair-collapse`: access overlay만 blocked로 바꾸며 완료 milestone을 지우지 않는다.
- `event.beacon.lightning-trip`: breaker 상태와 powered만 바꾸며 설치 coil·재료·완료 milestone을 보존한다.
- 핵심 부품은 도난·일반 camp damage 대상이 아니다. repair/reset은 직접 현장 입력이며 전역 메뉴에서 해결하지 않는다.

## 8. `escape.beacon` 최소 플레이 가능 세로 조각

SAMPLE fixture ID: `slice.escape.beacon.playable.v1`

| 구간 | 자연 행동 | 필수 증거 |
|---|---|---|
| 1 | fresh save에서 고지대·폐중계소 해금 단서와 장비를 실제 수색·귀환으로 해결 | primary 또는 alternative unlock, grant/warp 0 |
| 2 | 구조 보수·발전기 수리 연구, coil과 재료를 자연 획득 | canonical research/part IDs, pity semantics |
| 3 | 폐중계소로 이동해 access→generator→signal head 세 지점을 직접 사용 | 세 고유 target/interaction trace |
| 4 | 한 failure overlay를 발생시켜 완료 milestone 보존과 현장 복구를 확인 | before/after snapshot, duplicate mutation 0 |
| 5 | clear-night 또는 storm-eye 창에 signal head에서 점등 | window snapshot과 activation transaction |
| 6 | 즉시 `escape.beacon` terminal과 하나의 beacon ending 표시 | Day lock 0, ending priority·modifier 근거 |

목표 실행 시간은 45분 이하 deterministic smoke 표본이고 준비 10~18일은 `SAMPLE_ONLY`다. 최소 slice는 세 milestone과 실패 overlay 하나를 반드시 실행하지만 comic·rare 엔딩을 강제하지 않는다.

SAMPLE 비용 profile:

| cost ID | 수치 |
|---|---|
| `sample.cost.beacon.structure` | wood 2 + stone 2 + metal 1 |
| `sample.cost.beacon.power` | wire 2 + fuel 1 + escrow `part.beacon.generator-coil` 1 |
| `sample.cost.beacon.signal-head` | metal 1 + wire 1 |
| `sample.cost.beacon.activation` | fuel 1 |
| `sample.cost.beacon.access-repair` | wood 1 + stone 1 |
| `sample.cost.beacon.overdrive-extra` | fuel 1 |

## 9. 원자 transaction·저장·로그

### 9.1 공통 transaction

1. preview·near·popup·locale/input 전환은 read-only다.
2. confirm은 연구, 위치·anchor, 창, facility 상태, 전체 cost vector, inventory/project escrow 수용, terminal 여부를 먼저 검증한다.
3. `resources + protected project inventory + milestone/overlay + behavior score + structured event`를 한 idempotency key로 함께 commit한다.
4. 실패는 선언된 cartridge·payload 또는 overlay만 변경하고 나머지는 rollback한다.
5. 같은 action·stage version retry는 `already_resolved` 또는 `already_completed`를 반환하고 자원·점수·로그·엔딩 해금을 반복하지 않는다.
6. 성공한 조기 탈출은 어느 day band에서도 즉시 terminal이며 Day 50 settlement보다 우선한다.

### 9.2 snapshot 최소 필드

| 경로 | 필드 |
|---|---|
| flare | `escapeMethodId`, canonical research IDs, `facilityState`, `payloadPrepared`, `cartridgeState`, `windowId`, `windowExpiresAt`, `launcherState`, `attemptId`, `lastEventId`, `completed`, `endingId` |
| beacon | `escapeMethodId`, canonical research IDs, `regionUnlockIds`, `accessState`, `structureMilestone`, `powerMilestone`, `signalHeadMilestone`, `coilState`, `powered`, `windowId`, `hazardOverlayId`, `attemptId`, `lastEventId`, `completed`, `endingId` |

구조 로그는 기존 `facility.action.completed/rejected`, `escape.project-progressed`, `escape.completed`, `ending.resolved`, hazard lifecycle event를 사용한다. 필드는 `runSeed, day, escapeId, projectId, targetId, interactionId, milestoneId, windowId, eventId, resultCode, stateBefore, stateAfter, grantUsed=false, warpUsed=false`로 제한하고 개인정보·자유 입력·원시 입력·좌표 궤적을 저장하지 않는다.

## 10. 지역화·qps-long

`interaction.structure.prompt`가 locale별 token 순서를 소유한다. 런타임은 `{inputGlyph}`, `{objectName}`, `{action}`만 전달하며 문자열을 직접 이어 붙이지 않는다.

| key | ko 기준 | en 의도 | qps-long 검증 예 |
|---|---|---|---|
| `object.escape.flare-launcher.name` | 조명탄 발사기 | Flare Launcher | ⟦Flårë Läünchër⟧ |
| `object.escape.relay-station.name` | 폐중계소 | Relay Station | ⟦Åbåndønëd Rëlåy Ståtïøn⟧ |
| `action.escape.flare.open` | 조명탄 준비 | Prepare flare | ⟦Prëpårë thë flårë nøw⟧ |
| `action.escape.beacon.open` | 중계소 복구 | Restore relay | ⟦Rëştørë thë rëlåy ståtïøn⟧ |
| `ui.escape.requirements` | 필요: {requirements} | Requires: {requirements} | ⟦Rëqüïrëş: {requirements}⟧ |
| `ui.escape.flare.window-closed` | 목격 대상 없음 · 다음 창 {windowTime} | No witness · Next window {windowTime} | ⟦Nø wïtnëşş ïn şïght · Nëxt wïndøw {windowTime}⟧ |
| `ui.escape.flare.window-uncertain` | 목격 불확실 · 발사하면 한 발을 잃을 수 있음 | Witness uncertain · Firing may spend the shot | ⟦Wïtnëşş üncërtåïn · Fïrïng mäy şpënd thë ønë shøt⟧ |
| `ui.escape.flare.missing-part` | 조명탄 탄약 없음 · 출처 {sourceRegion} | Flare cartridge missing · Source {sourceRegion} | ⟦Flårë cärtrïdgë mïşşïng · Søürcë {sourceRegion}⟧ |
| `ui.escape.beacon.next-stage` | 다음 복구: {milestone} | Next repair: {milestone} | ⟦Nëxt rëpåïr mïlëştønë: {milestone}⟧ |
| `ui.escape.beacon.circuit-tripped` | 낙뢰 차단기 작동 · 완료 단계는 보존됨 | Breaker tripped · Completed stages preserved | ⟦Brëåkër trïppëd by lïghtnïng · Cømplëtëd stågëş prëşërvëd⟧ |
| `ui.escape.beacon.window-closed` | 점등 창 아님 · {weather}/{timeOfDay} | Signal window closed · {weather}/{timeOfDay} | ⟦Şïgnål wïndøw cløşëd · {weather}/{timeOfDay}⟧ |
| `ui.escape.ready` | 탈출 준비 완료 | Escape ready | ⟦Ëşcåpë rëådy⟧ |

- KO는 의미와 코미디 톤의 원문, EN은 같은 상태·비용·위험 의미를 자연스러운 어순으로 전달한다.
- qps-long은 플레이 언어가 아니라 150% 팽창, 폰트 fallback, prompt 한 줄, popup 2줄/스크롤, glyph 분리를 검증한다.
- LOCKED·SHORT·WINDOW_CLOSED도 focus 가능하며 선택하면 정확한 선행·부족·창 사유를 보이고 mutation은 0이다.
- 공식 영문 게임 제목은 여전히 `TBD`다.

## 11. 엔딩·modifier 계약

| 조건 우선순위 | ending ID | modifier | 규칙 |
|---:|---|---|---|
| 300 | `ending.rare.beacon.storm-eye` | `modifier.event.scar` | `event.disaster.perfect-mitigation` during beacon completion + building/hazard-response SAMPLE 임계 |
| 200 | `ending.comic.beacon.brightest-address` | `modifier.stat.building` | `event.beacon.overpowered` + building SAMPLE 임계 |
| 200 | `ending.comic.flare.daylight-fireworks` | `modifier.behavior.dominant` | daylight-fireworks event 뒤 valid witness response |
| 100 | `ending.escape.flare.one-shot` | `modifier.behavior.dominant` | flare 성공, 상위 flare ending 없음 |
| 100 | `ending.escape.beacon.ridge-light` | `modifier.behavior.dominant` | beacon 성공, 상위 beacon ending 없음 |

- 같은 terminal snapshot에서 rare→comic→normal 우선순위를 적용하고 tie-break·hysteresis는 기존 정본을 사용한다.
- `event.flare.perfect-window`는 새 ending이나 achievement ID를 만들지 않고 result evidence만 남긴다.
- 실패·취소는 ending 후보·앨범·future-only achievement mapping을 해금하지 않는다.
- 실제 Steamworks 설정은 범위 밖이다.

## 12. red-first 수용 게이트

red-first는 현재 data-only 상태에서 반드시 FAIL해야 한다. 문자열 reflection이나 catalog load만으로 PASS하면 테스트 자체가 실패다.

| gate ID | 초기 RED 이유 | GREEN 수용 조건 |
|---|---|---|
| `W20-F01.flare-canonical-data` | playable=false, 지역·재료 일부 생략 가능 | canonical region/research/facility/part/material/risk/event 집합과 `playableState=playable` |
| `W20-F02.flare-direct-state-machine` | 발사기 object·popup·escrow·window 행동 없음 | far→near→popup→load→window→fire→field/terminal의 실제 interaction trace, grant/warp 0 |
| `W20-F03.flare-window-atomic-retry` | success/no-witness/misfire transaction 미구현 | closed 무소비, uncertain 명시, cartridge 최대 1 소비, retry 중복 0, replacement path 존재 |
| `W20-F04.flare-natural-slice` | 자연 playable terminal 증거 없음 | fresh save에서 지도→수색→귀환→연구→설치→발사→flare ending, 30분 SAMPLE fixture |
| `W20-B01.beacon-canonical-data` | 임시 alias와 playable=false | canonical IDs만 저장·표시·로그, alias 공개 0, 세 target과 `playableState=playable` |
| `W20-B02.beacon-spatial-milestones` | 원격 object와 세 milestone 행동 없음 | access→generator→signal head 직접 trace, 각 milestone 원자 commit·취소 불변 |
| `W20-B03.beacon-hazard-preservation` | failure overlay·weather activation 미구현 | power-drop/stair-collapse/lightning-trip이 완료 stage·coil 보존, 현장 복구·retry 중복 0 |
| `W20-B04.beacon-natural-slice` | 자연 playable terminal 증거 없음 | fresh save에서 해금→수색→연구→세 복구→창 점등→beacon ending, 45분 SAMPLE fixture |
| `W20-X01.day50-ending-locale-input` | 두 경로 live 증거 없음 | 어느 day band든 완료, Day50 우선, KO/EN 의미 동일, qps-long 비가림, keyboard/gamepad focus parity |

red 증거는 예상 FAIL ID와 실제 실패 assertion을 기록한다. 구현 뒤 동일 probe를 바꾸지 않고 GREEN으로 전환하며, natural slice는 grant·warp·skip을 사용하지 않는다.

## 13. 제외와 승격 조건

제외: 뗏목 수정, 새 탈출법, 새 엔딩, 최종 확률·드롭·소모, 전체 50일 인간 완주, 실제 Steam 업적, 최종 아트·연출, 깊은 잠수·전투, 전역 프로젝트 완료 메뉴.

SAMPLE 비용·준비 기간·창 빈도·실패 확률은 flare와 beacon 자연 세션이 각각 최소 3회 있고 발견성·locale·원자성이 통과한 뒤 한 축씩만 조정한다. 최종 밸런스 승격은 기존 KO 3/EN 3 장기 프로토콜과 별도 사용자 승인을 요구한다.

현재 설계를 막는 열린 질문은 없다. 구현 파일 배치와 내부 클래스 구조는 기존 Runtime 소유자가 정하되 공개 ID, state/action, transaction, 증거 경계는 바꾸지 않는다.

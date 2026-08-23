# Wave 9 공간형 베이스캠프 상세 계약

> 상태: `DESIGN LOCKED / COMPACT PROMPT UX ADDENDUM / RUNTIME UNCHANGED IN THIS COMMIT`
> 작업 기준점: `b4142df02f3745ea18a72888fdf3b029dbe78886` (`origin/master f95b192` 포함)
> Forge stable IDs: `feature.camp-object-interaction`, `screen.camp`
> 정본 입력: `Docs/Design/References/approved-spatial-base-camp-concept.png`, `.forge/packets/wave9-spatial-base-camp-rebaseline.json`
> 화면 증거: 사용자 제공 1280×800 캡처 (`evidence-only`, 저장소 정본 아트·지시문 아님)
> 공식 영문 게임 제목: `TBD`

이 문서는 승인된 방향 목업과 Wave 9 rebaseline을 구현자가 추가 해석 없이 상태 기계와 배치 데이터로 옮길 수 있게 만든 canonical addendum다. 기존 자원 경제·생존 수치·연구·가방·구조 신호 계약은 `vertical-slice-balance.md`, `wave6-progression-clarity.md`, `wave7-bag-capacity-upgrade.md`를 상속한다. 이 문서와 충돌하는 기존의 상시 대형 캠프 대시보드·원거리 버튼 사용 전제는 폐기한다.

## 1. 승인 이미지에서 고정하는 것과 고정하지 않는 것

| 구분 | 고정 계약 | 고정하지 않는 것 |
|---|---|---|
| 공간 | 측면 절개형 캠프에서 김씨를 직접 이동시키고 위·옆·지하 공간을 한 월드로 읽음 | 이미지의 정확한 방 장식·색·소품 위치·화면 비율 |
| 상호작용 | 설비에 접근하면 대상 하나의 근접 안내가 나타나고 입력 뒤 작은 설비 전용 팝업이 열림 | 래스터에 그려진 `E`, 한국어 버튼, 특정 버튼 모양 |
| HUD | 날짜·일광·허기·체력·가방 요약만 지속 표시 | 제작·연구·건설 목록을 상시 펼치는 전역 패널 |
| 증축 | 위층·옆방·지하실 후보를 기존 캠프 위에 유령으로 미리보고 유효·무효 원인을 구분 | 픽셀 단위 벽·바닥 그리기, 임의 크기, 물리 적층 |
| 설비 | 작업대·모닥불·빗물받이·저장함·구조 신호대가 실제 공간과 행동 소유권을 가짐 | 이미지 속 설비를 런타임 아트로 직접 채택 |

첫 프로토타입은 세 후보를 모두 미리보기하고 그중 최소 하나를 확정하는 데 한정한다. 두 번째 방 증축, 해체·환불, 모듈 재배치, 계단 커스터마이즈, 전기·배관·구조 안정성은 범위 밖이다.

## 2. 상태 축과 불변 조건

구현은 화면 이름이나 번역 문구가 아니라 아래 안정 상태를 저장한다.

| 축 | 값 | 불변 조건 |
|---|---|---|
| `CampMode` | `FreeRoam`, `ObjectPopup`, `FacilityPlacement`, `ModulePreview`, `Terminal` | 동시에 하나만 활성 |
| `FocusOwner` | `World`, `Popup`, `Placement`, `ModulePreview`, `System` | 입력은 소유자 하나만 소비 |
| `InteractionTargetId` | 안정 설비·앵커 ID 또는 `null` | locale 문구를 ID로 사용하지 않음 |
| `PopupPath` | root와 하위 목록의 안정 action ID 스택 | 닫으면 비우고 게임 상태에는 영향 없음 |
| `ReturnSnapshot` | 김씨 위치·방향·방 ID·대상 ID | 팝업·미리보기 취소 뒤 현장 복귀 기준 |
| `TransactionGuard` | `Idle`, `Validating`, `Committed` | Submit 중복 프레임의 이중 차감 금지 |

공통 불변 조건:

- `FreeRoam` 이외 상태에서는 일반 이동을 잠그고 김씨 속도를 0으로 만든다.
- 캠프 직접 이동, 팝업, 배치와 증축 미리보기는 E/H/L을 새로 소모하지 않는다. 기존 일광·생존 산술을 바꾸지 않는다.
- 자원은 성공한 거래의 확정 프레임에서만 정확히 한 번 바뀐다.
- 실패·뒤로·취소·무효 위치·중복 Submit은 자원, 설비, 모듈, 연구, 가방 용량, 신호 단계를 바꾸지 않는다.
- 팝업 또는 미리보기가 닫히면 김씨는 `ReturnSnapshot`의 같은 현장 위치와 방향으로 돌아간다. 성공한 배치 때문에 그 위치가 막히면 배치를 거부해야지 김씨를 순간이동시키지 않는다.
- 입력 장치 또는 locale 변경은 `CampMode`, 대상, 포커스 action ID와 게임 데이터를 바꾸지 않고 glyph와 문자열만 갱신한다.

## 3. 직접 이동부터 현장 복귀까지 상태표

| 상태 ID | 진입 조건 | 화면·입력 | 이동/포커스 | 성공·실패 전이 |
|---|---|---|---|---|
| `camp.free` | 캠프 진입·팝업 종료 | 최소 HUD와 월드만 표시. `Move`, `Interact` 수신 | 이동 허용, `World` | 후보가 생기면 `camp.target`; 출구는 수색 전환 |
| `camp.target` | 사용 가능 후보가 1.25u 안 | 대상 이름+`{interact}` 안내 한 개 | 이동 허용, 대상 latch | Interact→`popup.opening`; 범위 이탈→`camp.free` |
| `popup.opening` | 대상 Interact 1회 | 대상별 root 구성, 현재 상태 재계산 | 이동 잠금, `Popup` | 유효 기본 포커스 지정 뒤 `popup.active`; 대상 소멸 시 현장 복귀 |
| `popup.active` | root 또는 하위 목록 | 대상 소유 action, 비용·조건·상태, 확인·취소 | 팝업 안 focus trap | Submit→`action.validating`; Back은 하위→root, root→`field.return` |
| `action.validating` | action Submit | 스냅샷 기준 선행·비용·중복·결말 재검사 | 입력 잠금, `System` | 불충족→`action.failed`; 거래→`action.succeeded`; 배치형→각 preview |
| `action.failed` | 조건·비용 불충족 | 정확한 부족·잠금·무효 원인을 inline 표시 | 시도 action에 포커스 복원 | 자원 변화 0, 다음 Submit 때 재계산, Back 가능 |
| `action.succeeded` | 원자 거래 완료 | 성공 피드백 1회와 최신 상태 | 완료 action에 포커스 | 일반 거래→`popup.active`; 배치 완료→`field.return`; 신호 2→`Terminal` |
| `placement.preview` | 신규 건설 또는 재배치 선택 | 김씨·공간을 보존한 유령, 유효 원인, 비용, 확인·취소 | 이동 잠금, `Placement` | 유효 Confirm→원자 건설/이동→`field.return`; Cancel→진입 원점 |
| `module.preview` | 저장/설계 action에서 증축 선택 | 위·옆·지하 슬롯과 선택 유령, 기하·비용 상태 | 이동 잠금, `ModulePreview` | 유효 Confirm→모듈 1개 확정→`field.return`; Cancel→root popup |
| `field.return` | 완료·취소·팝업 root Back | 팝업/오버레이 제거, 최소 HUD 복원 | `World`, 이동 잠금 해제 | 같은 대상이 여전히 범위 안이면 `camp.target`, 아니면 `camp.free` |
| `camp.terminal` | 구조 성공·탈진·기한 실패 | 결과 화면만 활성 | `System` | 캠프 팝업·배치로 복귀 불가 |

### 3.1 후보 하나 선택 규칙

1. 후보는 활성 상태이고, 김씨와 같은 연결 공간에 있으며, 상호작용 지점이 1.25u 안이고, 벽·바닥 collider에 가려지지 않아야 한다.
2. 현재 latch 대상이 여전히 적격이면 유지한다. 매 프레임 가장 가까운 대상으로 흔들지 않는다.
3. 새로 고를 때는 바라보는 반평면 안의 후보를 우선하고, 그 안에서 거리 제곱이 작은 순, 마지막으로 안정 `InteractionTargetId` 오름차순을 사용한다.
4. 대상이 범위를 벗어나거나 비활성화되면 latch를 해제하고 같은 규칙으로 다시 고른다.
5. 안내는 항상 하나만 표시한다. 포커스 대상이 아닌 설비의 비용·버튼·월드 라벨은 펼치지 않는다.
6. 후보가 없을 때 Interact는 거래나 대형 캠프 메뉴를 열지 않는다.

`1.25u`는 기존 공간형 거점 동결값이다. 별도 진입/이탈 반경과 switch margin은 실제 흔들림 증거가 생기기 전까지 추가하지 않는다.

### 3.2 입력 의미 계약

| 의미 action | 키보드·마우스 기본 | 게임패드 기본 | 상태별 소비자 |
|---|---|---|---|
| `Move` | 이동 키 | 왼쪽 스틱/D-pad | `World`; 팝업 중 소비 금지 |
| `Interact` | 현재 binding glyph | 현재 binding glyph | `World`에서 대상 팝업 열기만 담당 |
| `Navigate` | 방향키·포인터 | D-pad/스틱 | `Popup`, `Placement`, `ModulePreview` |
| `Submit` | 확인·클릭 | Submit action | 현재 `FocusOwner` 하나만 처리 |
| `Cancel` | Esc·취소·우클릭 | Cancel action | 하위 목록→root→현장 순으로 한 단계씩 |

플레이어 문구에는 `E`, `A`, `B`, `X`, `Y`를 하드코딩하지 않고 action glyph placeholder를 사용한다. 장치 전환 중 포커스는 같은 action ID에 남는다.

## 4. 설비별 행동 소유권

상시 전역 캠프 버튼은 없다. 미건설 일반 설비의 후보 선택과 증축 계획은 시작 방의 고정 `storage.planning` 상호작용점이 맡아 bootstrap 문제를 해결한다. 이 계획점은 새 유료 설비가 아니라 기존 창고/가방 표현의 현장 인터페이스다.

| 대상 | 배치 분류 | 소유 action | 소유하지 않는 action |
|---|---|---|---|
| `storage.planning` 저장/가방 | 시작 방 고정, 비용 없음, 재배치 불가 | 창고·가방 상세 보기, 식량 사용, 미건설 설비 후보 선택, 방 모듈 미리보기 진입 | 제작, 연구, 가방 확장 구매, 신호 투자 |
| 작업대 | `module.general-floor`, 제한적 자유 배치·무비용 재배치 | 돌도끼/밧줄 연구, 연구한 도구 제작, 가방 4→6 1회 확장 | 식량·휴식, 신호 투자, 방 증축 비용 확정 |
| 모닥불 | `module.general-floor`, 제한적 자유 배치·무비용 재배치 | 완성·회복 효과 확인, 무료 재배치, 공간 사용 피드백 | 제작·연구, 가방 확장, 별도 신규 E/H/L 거래 |
| 빗물받이 | `camp.open-sky-ground`, 같은 구역 내 무료 재배치 | 유효 open-sky 상태·하루 +10 효과 확인, 무료 재배치 | 실내 배치, 클릭 때마다 물/회복 생성 |
| 구조 신호대 | 단일 `camp.signal-anchor`, 재배치 불가 | 현재 단계·총 조건 보기, 1단계/2단계 투자, 완료 발신 상태 | 일반 설비 배치, 모듈 증축, 밧줄 소비 |
| 시작 방 침상·출구 | 고정 환경 상호작용점 | 기본 일몰 정산/휴식과 원정 출발·귀환 | 위 다섯 설비의 action |

모닥불과 빗물받이는 클릭 보상기를 새로 만들지 않는다. 기존 하루 정산의 모닥불 총 회복 38, 빗물받이 +10을 그대로 사용하며 팝업은 적용 여부와 위치 조건을 설명한다. 기본 정산은 고정 침상에서 가능해 모닥불 미건설 상태도 진행을 막지 않는다.

## 5. 비용·잠금·가능·완료 상태

### 5.1 공통 표시 상태

| 상태 | 표시 | Submit 결과 | 자원 변화 |
|---|---|---|---:|
| `LOCKED` | action을 숨기지 않고 선행 조건과 전체 비용 표시 | 정확한 선행 부족 피드백 | 0 |
| `SHORT` | 보유/필요 수치와 부족 항목 표시 | 부족 목록 전부 표시 | 0 |
| `READY` | action·비용·예상 결과 표시 | 원자 거래 또는 preview 진입 | 성공 시 1회 |
| `DONE` | `[완료]`와 영구 결과 표시 | 중복 거래 차단, 완료 설명 유지 | 0 |
| `INVALID_SITE` | 비용과 별도로 위치 거부 이유 표시 | preview 유지 또는 원점 복귀 | 0 |

회색 비활성만으로 정보를 숨기지 않는다. `LOCKED`, `SHORT`, `DONE`도 키보드·마우스와 게임패드 Submit으로 선택할 수 있고 정확한 피드백을 준다. 단, 다른 modal·terminal처럼 입력 전체가 잠긴 상태는 예외다.

### 5.2 동결 비용과 action 연결

| 소유 대상 / action ID | 비용 W/S/F/D | 잠금 조건 | `READY` 결과 | `DONE` 기준 |
|---|---|---|---|---|
| 저장 `build.workbench` | `2/0/0/1` | 없음 | 작업대 유령 진입, 유효 Confirm 때 차감·건설 | 작업대 1개 존재 |
| 저장 `build.campfire` | `2/1/0/0` | 없음 | 모닥불 유령 진입, 유효 Confirm 때 차감·건설 | 모닥불 1개 존재 |
| 저장 `build.rain_collector` | `2/1/0/1` | 없음 | open-sky 유령 진입, 유효 Confirm 때 차감·건설 | 빗물받이 1개 존재 |
| 작업대 `research.stone_axe` | `0/1/0/1` | 작업대 건설 | 돌도끼 제작법 해금 | 연구 완료 |
| 작업대 `craft.stone_axe` | `1/1/0/0` | 돌도끼 연구 | 돌도끼 보유, 장벽 제거·W +1 가능 | 돌도끼 보유 |
| 작업대 `research.rope` | `0/0/0/1` | 작업대 건설 | 밧줄 제작법 해금 | 연구 완료 |
| 작업대 `craft.rope` | `1/0/0/1` | 밧줄 연구 | 밧줄 보유 | 밧줄 보유 |
| 작업대 `upgrade.bag.4_to_6` | `2/0/0/1` | 작업대 건설, 미구매 | 활성 가방 6칸, run 동안 지속 | 이미 6칸 |
| 신호 `signal.stage1` | `2/0/0/2` | 작업대 건설 | stage 0→1 | stage≥1 |
| 신호 `signal.stage2` | `2/0/0/2` | stage 1, 밧줄 보유 | W2/D2만 차감, 밧줄 유지, stage 2·구조 | stage 2 |

도끼·밧줄 역할과 신호 부족 우선순위는 Wave 6, 가방 상태·문구·원자성은 Wave 7을 그대로 따른다. `repair`는 현행 제작법·비용·내구도 계약이 없으므로 작업대 팝업에 노출하지 않으며 수직 슬라이스 이후다.

### 5.3 작업대 정보 계층

작업대 root는 최대 세 행 `제작`, `연구`, `가방 확장`만 보인다. 제작·연구를 선택하면 같은 작은 팝업 안에서 하위 목록으로 들어가며 Back은 root로 돌아간다. 다른 설비 action은 섞지 않는다.

가방 상세는 저장/가방 팝업에서 읽지만 구매 transaction은 작업대만 소유한다. 저장/가방 화면에서 구매 버튼으로 점프하거나 원거리 작업대 action을 실행하지 않는다.

## 6. 모듈 좌표 계약

### 6.1 기준 좌표와 격자

- 월드 `+X`는 오른쪽, `+Y`는 위다.
- 기준점 `(0,0)`은 시작 방 본체의 왼쪽 아래 구조선이다.
- 모든 구조 원점과 바닥 배치는 기존 `0.5u` 격자에 맞춘다.
- 방 점유는 `[minX,maxX) × [minY,maxY)` AABB로 계산한다. 경계만 맞닿는 것은 겹침이 아니지만 선언된 reciprocal slot 없이 통로가 생기지는 않는다.
- 시작 방은 본체 12u와 야외 데크 6u가 결합된 고유 모듈 `18u×5u`다. 증축 방은 표준 `12u×5u`다.
- 아래 좌표는 첫 프로토타입의 구현 기준이며 자원 밸런스 수치가 아니다.

| 모듈 ID | 원점 | 크기 W×H | 월드 AABB | 지형 분류 | 첫 프로토타입 상태 |
|---|---|---|---|---|---|
| `room.start` | `(0,0)` | `18×5u` | `[0,18)×[0,5)` | 지상, 본체 `[0,12)`, 데크 `[12,18)` | 항상 존재 |
| `room.upper.standard` | `(0,5)` | `12×5u` | `[0,12)×[5,10)` | 지상 상부 | 후보 미리보기 |
| `room.side.standard` | `(18,0)` | `12×5u` | `[18,30)×[0,5)` | 지상 우측 | 후보 미리보기 |
| `room.basement.standard` | `(0,-5)` | `12×5u` | `[0,12)×[-5,0)` | 지하 | 후보 미리보기 |

`MaxCommittedExpansion=1`은 첫 프로토타입 범위 제한이다. 세 후보를 모두 순회·미리보기한 뒤 하나를 확정할 수 있어야 하며, 하나를 확정한 뒤 나머지는 `PROTOTYPE_LIMIT` 상태로 남긴다. 이를 장기 캠페인의 최대 방 수로 해석하지 않는다.

### 6.2 연결 슬롯과 출입부

| 슬롯 ID | 소유 모듈 / 중심 좌표 | reciprocal | 연결부 | 예약 통로 |
|---|---|---|---|---|
| `slot.start.exit.left` | start `(0,1.5)` | 섬 전환 | 가로 문 `1.5×2.5u` | start `[0,1.5)×[0,2.5)` |
| `slot.start.upper` | start `(2,5)` | `slot.upper.down` | 천장 hatch `1.5u` + 사다리 | start `[1.25,2.75)×[0,5)`, upper 같은 X landing |
| `slot.start.side` | start `(18,1.5)` | `slot.side.left` | 가로 문 `1.5×2.5u` | start `[16.5,18)×[0,2.5)`, side local `[0,1.5)` |
| `slot.start.basement` | start `(9,0)` | `slot.basement.up` | 바닥 hatch `1.5u` + 사다리 | start `[8.25,9.75)×[0,5)`, basement 같은 X landing |

가로 문은 두 모듈의 동일 높이 reciprocal slot이 모두 있어야 한다. 사다리는 위·아래 landing과 세로 이동 strip 전체가 비어 있어야 한다. 계단 connector는 데이터 모델에서 허용하지만 최소 `3u×2.5u`의 연속 footprint와 양끝 landing을 요구하며 첫 프로토타입은 사다리만 구현한다. 계단 장식이나 선택 UI는 범위 밖이다.

예시 `room.basement.right` 후보 `(12,-5)`는 시작 방에 reciprocal 바닥 슬롯이 없으므로 `NO_CONNECTION_SLOT`로 빨간 무효 미리보기를 표시한다. 승인 목업 오른쪽 아래의 거부 사례를 이 계약으로 재현할 수 있다.

### 6.3 모듈 안 배치 구역과 특수 앵커

좌표는 각 모듈의 local origin 기준이다. 실제 설비 footprint는 asset/runtime 데이터에서 읽으며 구역 숫자로 설비 크기를 추정하지 않는다.

| 구역·앵커 | local 범위·예시 | 허용 대상 | 규칙 |
|---|---|---|---|
| `module.general-floor.start` | start 본체 바닥 `x=[3.0,8.0)`, `y=0` | 모닥불·작업대 | 0.5u 스냅, 방향·높이 고정, 두 설비와 각 1u 접근 공간이 함께 남아야 함 |
| `module.general-floor.upper` | upper `x=[3.0,11.0)`, `y=0` | 모닥불·작업대 | 사다리 landing 제외 |
| `module.general-floor.side` | side `x=[2.0,11.0)`, `y=0` | 모닥불·작업대 | 왼쪽 문 통로 제외 |
| `module.general-floor.basement` | basement `x=[1.0,7.75)`, `y=0` | 모닥불·작업대 | 오른쪽 사다리 landing 제외 |
| `storage.anchor.start` | start local `(10.75,0)` | 저장/가방 planning | 고정, 비용·재배치 없음, 상호작용 접근 공간 유지 |
| `camp.open-sky-ground` | start 데크 local `x=[12.5,14.5)`, `y=0` | 빗물받이 | 상부 module AABB/차양이 없는 고정 야외 strip, 0.5u 스냅 |
| `camp.signal-anchor` | start 데크 상부 플랫폼 중심 `(15.75,4.5)` | 구조 신호대 | 단일 고정 앵커, stage 0~2 위치 유지, 데크 사다리로 접근 |
| `camp.keep-clear` | 출구·문·hatch·사다리·저장함 접근 AABB 합집합 | 없음 | 모든 설비 footprint와 상호작용 point 진입 금지 |

예시 유효 배치는 start local 작업대 `(3.5,0)`, 모닥불 `(6.5,0)`이다. 이는 설비 footprint가 구역과 1u 접근 조건을 통과할 때의 좌표 예시이며 최종 아트 폭을 확정하지 않는다.

일반 설비는 이미 확정된 모듈 사이의 호환 `module.general-floor.*`로 무비용 재배치할 수 있다. 취소하면 원래 room ID·좌표로 돌아가고 건설·연구·회복 상태를 유지한다. 빗물받이는 `camp.open-sky-ground` 안에서만 옮길 수 있고 구조 신호대·저장함·방 모듈은 재배치할 수 없다.

## 7. 증축 후보 판정과 거래

기하 유효성과 경제 가능성을 분리한다. 색만으로 상태를 전달하지 않고 outline 형태·아이콘·문구를 함께 쓴다.

| 판정 층 | 상태 | 표시 | Confirm |
|---|---|---|---|
| 기하 | `GEOMETRY_VALID` | 실선 유령+연결부·통로 표시 | 경제 판정으로 이동 |
| 기하 | `NO_CONNECTION_SLOT` | 끊긴 connector 아이콘+원인 문구 | 거부 |
| 기하 | `SLOT_UNAVAILABLE` | 비활성·점유·불일치 slot 원인 문구 | 거부 |
| 기하 | `OVERLAP` | 겹친 AABB 강조+원인 문구 | 거부 |
| 기하 | `TERRAIN_BLOCKED` | 지형/수면/암반 원인 문구 | 거부 |
| 기하 | `PATH_BLOCKED` | 막히는 출구·설비 접근 경로 표시 | 거부 |
| 경제 | `LOCKED` | 전체 unlock 조건·비용 표시 | 정확한 잠금 피드백 |
| 경제 | `SHORT` | 보유/필요와 부족 항목 표시 | 자원 변화 0 |
| 경제 | `READY` | 비용·결과 표시 | 원자 차감+모듈 생성 1회 |
| 범위 | `PROTOTYPE_LIMIT` | “첫 프로토타입은 1개 확장” 표시 | 추가 생성 거부 |

### 7.1 유효성 순서

1. 결과 화면·다른 modal·거래 중복 여부.
2. `MaxCommittedExpansion` 범위.
3. 선택 슬롯과 reciprocal slot 존재·미점유 여부.
4. 후보 AABB와 기존 모듈·설비 금지 parcel·지형 차단의 양의 면적 겹침 여부.
5. 지상 규칙: upper/side는 지하 terrain mask에 들어가지 않음. 지하 규칙: basement AABB가 지하 허용 mask 안이고 수면·암반 mask와 겹치지 않음.
6. connector footprint와 landing이 비어 있는지.
7. `slot.start.exit.left`에서 모든 기존·후보 room과 필수 설비 interaction point로 이어지는 경로 graph가 유지되는지.
8. 작업대 보유와 선택 module의 `W2/D1` 데이터 존재·충족 여부.
9. 모두 통과하면 snapshot 자원을 한 번 차감하고 room instance, connector, placement zone을 같은 transaction에서 생성한다.

일부 자원 차감 뒤 생성 실패, room 생성 뒤 connector 누락, 후보 확정 뒤 김씨 warp는 허용하지 않는다. transaction이 실패하면 snapshot으로 전부 복원한다.

### 7.2 밸런스 v0.2 확정값

| module ID | preview | commit unlock | 비용 | run 제한 |
|---|---|---|---|---|
| `room.upper.standard` | 캠프에서 처음부터 | 작업대 | `W2/D1` | 세 후보 중 1개 |
| `room.side.standard` | 캠프에서 처음부터 | 작업대 | `W2/D1` | 세 후보 중 1개 |
| `room.basement.standard` | 캠프에서 처음부터 | 작업대 | `W2/D1` | 세 후보 중 1개 |

세 방은 같은 `12×5u` 표준 모듈이고 이번 슬라이스에서 생산·침대·회복·저장 보너스가 없으므로 방향별 가격 차를 두지 않는다. 비용은 가방 확장과 같은 `W2/D1`로 고정해 성장 선택과 경쟁시키되, 작업대·가방·모듈·신호를 모두 포함한 자연 Day 3 구조 경로는 유지한다. 전체 계산, transaction과 run state 정본은 `wave9-module-expansion-balance.md`와 `.forge/design/wave9-module-balance.json`이다.

## 8. 최소 HUD와 상황형 팝업 정보 구조

| 계층 | 항상/상황 | 포함 정보 | 금지 |
|---|---|---|---|
| 지속 HUD | 항상 | Day·일광, H/E, 활성 가방 `4/6`과 compact 슬롯 요약 | 창고 전체, 제작·연구·건설 목록, 상시 대형 가방 패널 |
| 근접 안내 | 대상 1개가 1.25u 안 | 내레이션 카드 아래 중앙 상단의 소형 한 줄 안내: `{inputGlyph}`, `{objectName}`, `{action}` | 월드 부착형 대형 카드, 비용·상태 목록, 다른 설비 버튼 |
| 설비 팝업 header | Interact 뒤 | 설비명, 건설/단계/가방 상태 | 캠프 전체 제목·다른 설비 탭 |
| action row | 대상 소유 action | action명, `LOCKED/SHORT/READY/DONE`, 비용·핵심 조건 | 색만으로 상태 구분, 조건 숨김 |
| inline feedback | Submit 실패·성공 | 정확한 부족·무효·완료 원인 | toast 하나로 나머지 부족 항목 숨김 |
| footer | 팝업/preview | 현재 장치의 Navigate/Submit/Cancel glyph | `E`, `A/B/X/Y` 하드코딩 |
| 현장 overlay | 배치·증축 중 | 선택 대상 유령, 구역·slot·경로, 기하/비용 상태 | 캠프를 가리는 전역 관리 화면 |

1280×800 기준 설비 팝업은 최대 `420×360px`, 화면 높이 45% 이내를 1차 레이아웃 기준으로 사용한다. 이는 경제 밸런스가 아닌 가독성 수용값이다. 대상과 김씨를 동시에 가리면 좌우 반대편으로 anchor를 바꾼다. 본문 18px, action·핵심 수치 20px 미만으로 축소하지 않고 qps-long은 action 영역만 스크롤하며 header·현재 포커스·footer를 유지한다. 근접 안내는 아래의 별도 고정 계약을 사용하며 설비 팝업 크기나 월드 anchor 규칙을 공유하지 않는다.

### 8.1 근접 안내 재기준 결정

사용자 제공 1280×800 화면 증거에서는 `[E] 모닥불 사용` 카드가 플레이어와 지상 이동 경로 위를 넓게 덮었다. 이미지는 문제 재현 증거일 뿐 레이아웃·문구 지시문이나 아트 정본이 아니다.

정본 방향은 **월드 중앙의 대상 부착형 대형 카드가 아니라, 내레이션 카드 바로 아래 중앙 상단의 소형 상황별 안내 하나**다. 안내가 화면 상단에 있어도 표시 대상은 오직 김씨가 직접 1.25u 안으로 걸어간 설비이므로 원거리 전역 메뉴가 아니다. 안내 선택이나 포인터 클릭으로 다른 설비를 원격 실행할 수 없다.

이는 This War of Mine에서 참고한 “인물이 공간을 걸어 설비를 직접 사용한다”는 방향을 강화하되, 해당 작품의 고유 UI·레이아웃·문구를 복제하는 계약은 아니다.

### 8.2 화면 계층과 1280×800 배치 수치

좌표는 1280×800 Canvas의 좌상단을 `(0,0)`, 우하단을 `(1280,800)`으로 한다. 다른 해상도는 이 해상도를 CanvasScaler reference로 사용하되 아래 비율 상한을 보존한다.

| 계층 ID | draw order | 표시 규칙 |
|---|---:|---|
| `World` | 0 | 김씨, 설비, 지형, 이동 경로. 근접 안내를 이 계층이나 대상 머리 위에 붙이지 않음 |
| `BottomHelp` | 10 | 현재 장치 공통 도움말. 근접 안내 때문에 위로 밀거나 두 줄을 추가하지 않음 |
| `TopHUD` | 20 | Day·H/E·자원·가방 요약 |
| `NarrationCard` | 30 | 중앙 상단 김씨 독백·상황 문구 |
| `ContextPrompt` | 31 | `NarrationCard` 아래의 근접 대상 1개 안내. 클릭 가능한 전역 설비 선택기가 아님 |
| `FacilityPopup/Preview` | 40 | 설비 팝업·배치·증축. 열려 있는 동안 `ContextPrompt` 숨김 |
| `Terminal/SystemModal` | 50 | 성공·실패·설정·일시정지. 하위 캠프 안내 전부 숨김 |

| 항목 | 1280×800 계약 | 확장 규칙 |
|---|---|---|
| 중앙 anchor | `x=640`, top-center | 화면 너비 50% 고정 |
| 내레이션 기본 영역 | `y=146..282` | 카드 bottom은 동시 표시 시 `282px` 이하 |
| 내레이션 간격 | `12px` 이상 | `PromptTop = max(294, NarrationBottom + 12)` |
| 안내 top/bottom | 기본 `y=294..334`, 절대 최대 bottom `338` | 내레이션이 없거나 짧아도 top `294`를 유지해 수직 점프 방지 |
| 폭 | 내용 기반 `220..440px` | 최대 화면 폭 `34.375%`, 좌우 내부 padding 각 `16px` |
| 높이 | 권장 `40px`, 최대 `44px` | 두 줄 확장·세로 스크롤 금지 |
| token 간격 | glyph 뒤 `8px`, 나머지는 locale format의 공백 | 번역문을 세 개의 고정 순서 칸으로 분할하지 않음 |
| 월드 임계선 | `WorldCriticalBandTop=350px` | 안내 최대 bottom과 플레이어·주요 동선 사이 `12px` 이상 확보 |
| safe margin | 화면 좌우·상단 `24px` | prompt rect가 safe area 밖으로 나가면 실패 |

`ContextPrompt`는 김씨나 설비의 screen position을 따라가지 않는다. 상층 방을 비출 때도 카메라 framing이 김씨, 활성 설비의 interaction bounds와 주 이동 통로를 `ContextPrompt` 아래 또는 좌우로 보존해야 한다. 이 조건을 맞추기 위해 안내를 다시 월드 중앙으로 내리거나 크기를 키우지 않는다.

### 8.3 글자 크기와 긴 문자열 처리

| 요소 | 크기·스타일 | 절대 하한 |
|---|---|---:|
| 내레이션 본문 | 현재 계층 유지, 기준 `20px` | `20px` |
| `{inputGlyph}` | inline glyph `20..22px`, 정사각 slot 최대 `24px` | `20px` |
| `{objectName}` | `18px`, medium | `18px` |
| `{action}` | `18px`, semibold | `18px` |
| 한 줄 line box | `22px` 이하 | 축소로 해결하지 않음 |

안내는 항상 한 줄이며 word wrap을 끈다. 레이아웃은 locale별 완성 문자열을 먼저 18px로 측정한 뒤 다음 순서만 사용한다.

1. 전체 `{objectName}`과 `{action}`으로 `440px` 안에 배치한다.
2. 넘치면 해당 locale이 제공한 의미 동일 `objectName.short` 또는 `action.short`를 사용한다. 다른 언어의 단어를 재사용하지 않는다.
3. 그래도 넘치면 `{inputGlyph}`과 `{action}`은 온전히 보존하고 `{objectName}`만 끝 말줄임표로 줄인다. 최소 6개 grapheme cluster를 보존한다.
4. 높이를 늘리거나 18px 아래로 축소하거나 두 줄로 만들거나 패널을 `440×44px`보다 키우지 않는다.

실제 지원 locale인 ko/en에서 모닥불·작업대·빗물받이·구조 신호대는 말줄임표 0건이어야 한다. `qps-long`은 overflow 경로를 강제로 검증할 수 있지만 glyph·action 누락, clipping, 두 줄, panel 확장, 월드 가림은 허용하지 않는다. 향후 locale이 말줄임표 없이는 의미를 유지하지 못하면 그 locale의 짧은 설비명·행동 번역을 추가하기 전 지원 완료로 판정하지 않는다.

### 8.4 표시 우선순위와 상태 전이

| prompt 상태 | 진입 조건 | 표시·입력 | 다음 상태·보존 계약 |
|---|---|---|---|
| `prompt.far` | 적격 후보 0개 | 숨김, Interact로 전역 메뉴를 열지 않음 | 후보 생성→`prompt.near` |
| `prompt.near` | latch 대상 1개가 1.25u 안 | 중앙 상단 안내 정확히 1개, 이동·Interact 허용 | Interact→`prompt.popup-open`; 범위 이탈→`prompt.far` |
| `prompt.popup-open` | 대상 팝업·배치·증축·system modal 활성 | 숨김, popup/preview가 입력 소유 | close/cancel 뒤 동일 대상 적격→`prompt.return`; 아니면 `prompt.far` |
| `prompt.return` | popup close/cancel·일반 action 완료 | 같은 `InteractionTargetId`와 최신 문자열로 한 개 복원 | 다음 프레임 `prompt.near`; 위치·방향·자원 불변 |
| `prompt.locale-change` | near 상태에서 locale 전환 | 같은 target을 유지하고 locale pattern·폭을 원자 재측정 | 한 프레임 안에 `prompt.near`; 팝업 자동 열림·대상 재선택 금지 |
| `prompt.device-change` | keyboard↔gamepad 또는 binding 변경 | 같은 target·문장 의미를 유지하고 `{inputGlyph}`만 현재 binding으로 교체 | 한 프레임 안에 `prompt.near`; 깜빡임·중복 prompt 금지 |

표시 우선순위는 `Terminal/SystemModal > FacilityPopup/Preview > ContextPrompt`다. `NarrationCard`는 prompt를 숨기지 않고 바로 위 slot을 소유한다. `TopHUD`와 `BottomHelp`는 계속 보이되 prompt와 rect가 겹치지 않는다. 대상 action의 성공·실패 feedback은 팝업 안에서 처리하며 별도의 대형 근접 카드를 다시 만들지 않는다.

다중 근접 설비 선택은 §3.1을 그대로 사용한다. 즉 현재 적격 latch 유지 → 바라보는 반평면 → 거리 제곱 → 안정 `InteractionTargetId` 오름차순이다. 새 switch margin은 추가하지 않으며, 모닥불·작업대가 동시에 가까워도 prompt는 선택된 한 대상만 표시한다.

### 8.5 겹침 및 일반화 수용 기준

| 검증 대상 | PASS | FAIL |
|---|---|---|
| 내레이션 | 두 rect 간 수직 간격 `≥12px` | 접촉·겹침 또는 prompt가 내레이션 위로 이동 |
| TopHUD·BottomHelp | prompt와 screen-space 교차 면적 `0px²` | HUD/도움말을 덮거나 밀어냄 |
| 김씨·활성 설비 | prompt 확장 rect(사방 `12px`)와 각 bounds 교차 `0px²` | 얼굴·몸·상호작용 지점·설비 기능부 가림 |
| 주 이동 경로 | 두께 `48px`의 현재 room 보행 corridor와 교차 `0px²` | 안내가 걷는 길 한가운데 배치됨 |
| 크기 | `220..440 × 40..44px`, 1줄 | 기존 화면처럼 큰 중앙 카드, 2줄, 자동 높이 증가 |
| 대상 수 | near에서 prompt `1`, far/popup에서 `0` | 다중 설비 안내, 숨은 prompt의 raycast/input 점유 |
| 네 설비 | 모닥불·작업대·빗물받이·구조 신호대가 같은 pattern·상태 머신 사용 | 설비별 하드코딩 panel 위치·입력 문자·문장 조립 |

1280×800 ko/en 각 네 설비와 qps-long 최장 설비명, keyboard/gamepad glyph를 캡처해 위 AABB·크기·한 줄 조건을 판정한다. 공간 충돌이 재현되면 우선순위는 `카메라 framing → 내레이션 높이/상단 예약 → locale short variant`이며, 월드 대형 카드 복원이나 전역 설비 메뉴 추가는 해결책이 아니다.

## 9. KO/EN과 향후 로케일 문자열 계약

KO가 의미·정보 우선순위·코미디 톤의 기준 원문이다. EN은 자연스럽게 재구성하되 대상, 행동, 상태, 비용, 방향과 숫자를 바꾸지 않는다. 아래 키는 기존 테이블에 추가하거나 기존 키를 새 공간형 경로에 재사용하는 계약이며, 이 작업에서 실제 String Table을 수정하지 않는다.

| 안정 키 | 상황 | KO 기준 | EN 의도 | 길이·메타 |
|---|---|---|---|---|
| `controls.camp.spatial` | 자유 이동 | `{move} 이동 · {interact} 상호작용 · {bag} 가방` | `{move} Move · {interact} Interact · {bag} Bag` | glyph placeholder, 최대 2줄 |
| `interaction.structure.prompt` | 대상 근접 pattern | `{inputGlyph} {objectName} {action}` | `{inputGlyph} {action} {objectName}` | locale별 어순 소유, 한 줄, 대상 1개 |
| `interaction.action.use` | prompt 행동 token | 사용 | Use | 설비명과 런타임 문자열 결합 금지; pattern placeholder로 주입 |
| `interaction.action.use.short` | 440px overflow 전용 | 사용 | Use | base와 의미 동일, 없으면 base fallback |
| `ui.structure.popup.title` | 팝업 header | `{structure} · {state}` | `{structure} · {state}` | KO≤22, EN≤34 |
| `ui.action.state.locked` | 잠금 | 잠김 · {requirement} | Locked · {requirement} | 선행 조건 숨김 금지 |
| `ui.action.state.short` | 비용 부족 | 부족 · {missing} | Missing · {missing} | 부족 항목 전부, 최대 3줄 |
| `ui.action.state.ready` | 실행 가능 | 가능 · {cost} | Ready · {cost} | 비용 0이면 빈 토큰 숨김 |
| `ui.action.state.done` | 완료 | `[완료] {result}` | `[Done] {result}` | 재구매·반복 암시 금지 |
| `ui.storage.planning` | 저장/가방 root | 보관함 · 가방 · 설비 계획 | Storage · Bag · Facility Plans | 전역 대시보드로 번역 금지 |
| `ui.module.expand` | 증축 진입 | 방 확장 미리보기 | Preview Room Expansion | confirm 전 건설 완료 의미 금지 |
| `ui.module.name.upper` | 후보명 | 위층 | Upper Room | 방향 의미 유지 |
| `ui.module.name.side` | 후보명 | 옆방 | Side Room | left/right 임의 추가 금지 |
| `ui.module.name.basement` | 후보명 | 지하실 | Basement | 던전·깊은 지하 의미 금지 |
| `ui.module.preview.cost` | 선택 후보 비용 | `{moduleName} · 나무 {wood} · 표류물 {salvage} · {state}` | `{moduleName} · {wood} Wood · {salvage} Salvage · {state}` | locale별 어순, 비용 숨김 금지 |
| `interaction.module.locked_workbench` | 작업대 없음 | 작업대 필요 | Workbench Required | preview와 W2/D1은 계속 표시 |
| `interaction.module.missing` | 비용 부족 | `{moduleName} 부족 · {missing}` | Missing for {moduleName} · {missing} | 정확한 부족 W→D 순서 |
| `interaction.module.ready` | 확정 가능 | 설치 가능 | Ready to Build | geometry valid와 비용 충족 모두 필요 |
| `interaction.module.valid` | 기하 유효 | 연결·통로 유효 | Connection and Path Valid | 비용 충족과 혼동 금지 |
| `interaction.module.no_slot` | reciprocal 없음 | 연결 슬롯이 맞지 않는다. | No matching connection slot. | 기술 ID 노출 금지 |
| `interaction.module.slot_unavailable` | 비활성·점유 slot | 이 연결 슬롯은 사용할 수 없다. | This connection slot is unavailable. | no-slot과 원인 구분 |
| `interaction.module.overlap` | AABB 겹침 | 다른 공간과 겹친다. | Overlaps another room. | 색 외 텍스트 필수 |
| `interaction.module.path_blocked` | 경로 차단 | 출입구나 필수 통로를 막는다. | Blocks an entrance or required path. | 원인 우선, 농담 없음 |
| `interaction.module.terrain_blocked` | 지형 거부 | 이 방향의 지형에는 붙일 수 없다. | The terrain blocks this room. | 지상/지하 실제 상태 기반 |
| `interaction.module.prototype_limit` | 1개 확정 뒤 | 첫 확장은 이미 완성했다. | The first expansion is already complete. | 캠페인 최대치로 번역 금지 |
| `interaction.module.committed` | 확정 성공 | `{moduleName} 완성` | `{moduleName} Complete` | 환불·철거 action 암시 금지 |

기존 `ui.camp.actions_title`과 `controls.camp`는 migration alias로 보존할 수 있지만 정상 공간형 캠프 경로에서는 호출하지 않는다. 기존 `structure.*`, `interaction.placement.*`, Wave 6 신호 키와 Wave 7 가방 키는 재사용한다.

#### 9.1 prompt token과 locale 표

`interaction.structure.prompt`는 locale마다 완성 format을 소유한다. 런타임은 `{inputGlyph}`, `{objectName}`, `{action}` 세 의미 token을 제공하지만 `objectName + action`처럼 고정 순서로 이어 붙이지 않는다.

`objectName.short`는 선택 키 `structure.<id>.name.short`, `action.short`는 `interaction.action.use.short`를 뜻한다. short 키가 없으면 base token을 사용하고, short 번역은 대상·행동 의미를 바꾸거나 정보 우선순위를 뒤집을 수 없다.

| token | 공급원 | ko 예 | en 예 | qps-long 예 |
|---|---|---|---|---|
| `{inputGlyph}` | 현재 `Interact` binding의 inline glyph | `[E]` | `[E]` | `[E]` 또는 실제 gamepad glyph |
| `{objectName}` | 기존 `structure.*.name` | 모닥불 | Campfire | Çåmpfïrë |
| `{action}` | `interaction.action.use` | 사용 | Use | Ûşë |
| pattern | `interaction.structure.prompt` | `{inputGlyph} {objectName} {action}` | `{inputGlyph} {action} {objectName}` | `⟦{inputGlyph} {action} {objectName} — plëåşë nøw⟧` |

| 설비 key | ko 결과 | en 결과 | qps-long 최장 검증 예 |
|---|---|---|---|
| `structure.campfire.name` | `[E] 모닥불 사용` | `[E] Use Campfire` | `⟦[E] Ûşë Çåmpfïrë — plëåşë nøw⟧` |
| `structure.workbench.name` | `[E] 작업대 사용` | `[E] Use Workbench` | `⟦[E] Ûşë Wørkbënçh — plëåşë nøw⟧` |
| `structure.rain_collector.name` | `[E] 빗물받이 사용` | `[E] Use Rain Collector` | `⟦[E] Ûşë Måkëshïft Råïnwåtër Çøllëçtør — plëåşë nøw⟧` |
| `structure.signal_tower.name` | `[E] 구조 신호대 사용` | `[E] Use Rescue Signal` | `⟦[E] Ûşë Tåll Rësçûë Sïgnål Plåtførm — plëåşë nøw⟧` |

표의 `[E]`는 keyboard 예시일 뿐 문자열에 굽지 않는다. 게임패드 전환 시 같은 `{inputGlyph}` 자리에 현재 `Interact` action glyph를 넣는다. qps-long의 장식 괄호와 확장 문구는 레이아웃 시험용이며 실제 지원 언어의 의미나 코미디 문안을 추가한 것으로 취급하지 않는다.

향후 `es`, `ja`, `zh-Hans`, `zh-Hant`와 `qps-long`은 같은 key, context, intent, placeholder, 최대 줄 수, 직역 금지 메타데이터를 사용한다. action 가능 여부, 비용, 포커스와 저장 데이터는 문자열 비교로 판정하지 않는다. locale 전환 뒤에도 같은 팝업·action·module slot ID와 ReturnSnapshot이 유지되어야 한다.

## 10. 키보드·게임패드 포커스 계약

- 팝업을 열 때 `READY`가 있으면 첫 READY, 없으면 첫 LOCKED/SHORT, 전부 완료면 첫 DONE에 포커스한다.
- 하위 목록에서 Back하면 진입시킨 부모 action으로 돌아간다. root Cancel은 현장으로 돌아간다.
- mouse hover는 포커스를 바꿀 수 있지만 pointer가 팝업 밖으로 나갔다고 focus trap을 해제하지 않는다.
- 게임패드 Navigate는 화면상의 시각 순서와 같은 안정 action list를 순회하고 숨김 행을 건너뛴다. LOCKED/SHORT/DONE은 피드백을 위해 순회 대상이다.
- 스크롤된 action은 포커스와 함께 자동 노출한다. 현재 포커스 행을 말줄임표나 viewport 밖에 숨기지 않는다.
- Placement와 ModulePreview에서는 D-pad/스틱 한 입력이 같은 0.5u grid 또는 다음 안정 slot로 이동한다. pointer와 게임패드는 같은 validity 함수를 사용한다.
- 장치 전환은 현재 선택을 초기화하지 않는다. physical gamepad 전체 경로는 기존대로 실제 장치 증거 전 `UNVERIFIED`다.

## 11. Wave 8 외부 플레이테스트 변경 계약

`wave8-external-playtest-package.md`의 동일 빌드, ko 3/en 3, 정확히 20분, 구체적 힌트 금지, 익명 수기, 3/6/10/12/14분 핵심 시간과 결말·구조 분포는 유지한다. 아래 항목만 공간형 흐름으로 대체하며 Wave 8 원문은 역사적 패킷으로 보존한다.

### 11.1 시간 이벤트 정의 변경

| Wave 8 이벤트 | Wave 9 정의 | 시간 게이트 |
|---|---|---|
| `R1` 첫 귀환 | 귀환 뒤 창고 이전되고 김씨가 시작 방 `FreeRoam`에서 직접 이동 가능 | 기존 06:00 유지 |
| `C1` 첫 제작/연구 | 작업대 접근→근접 안내→Interact→작업대 팝업→원자 제작/연구 중 최초 성공 | 기존 10:00 유지 |
| `P1` 첫 유효 설치 | 저장/설계 접근→설비 선택→현장 유령→유효 Confirm으로 일반 설비가 실제 생성 | 기존 12:00 유지 |
| `CAMP_APPROACH` | 생성된 작업대/모닥불로 김씨가 직접 걸어가 안내를 띄우고 팝업을 열어 대상 action을 사용 | 기존 5/6·locale 2/3 유지 |
| `S1` 신호 1단계 | 고정 signal anchor까지 직접 이동→팝업→stage 1 원자 성공 | 기존 17:00 유지 |

신규 진단 timestamp `PROMPT_FIRST`, `POPUP_FIRST_OPEN`, `POPUP_FIRST_CANCEL`, `MODULE_UPPER_SEEN`, `MODULE_SIDE_SEEN`, `MODULE_BASEMENT_SEEN`, `MODULE_CONFIRM`을 추가하되 외부 실제 데이터 전 새 분 단위 합격선은 만들지 않는다. 고정 비용 `W2/D1`의 자동 산술 PASS를 인간 발견성·밸런스 PASS로 사용하지 않는다.

### 11.2 관찰 코드 변경

| 기존/신규 코드 | Wave 9 기록 |
|---|---|
| `CAMP_REMOTE` | 대상 안내가 없는데 Interact하거나 상시 대시보드/원거리 버튼을 찾은 횟수. 원거리 버튼이 실제 열리면 P1 계약 실패 |
| `CAMP_INVALID` | 설비 배치의 zone/overlap/path 거부 원인과 이해 여부 |
| `CAMP_VALID` | 저장 popup부터 유효 Confirm까지의 전체 경로와 설비/좌표 |
| `CAMP_APPROACH` | 배치 설비에 직접 접근→단일 prompt→전용 popup→행동 완료 여부 |
| `CAMP_FOCUS` | popup 중 김씨 이동·월드 Interact 누출, focus 상실, Cancel 단계 오류 횟수 |
| `CAMP_RETURN` | 성공·실패·취소 뒤 같은 위치·방향의 현장으로 복귀했는지 |
| `CAMP_ANCHOR` | 신호를 일반 floor에 두려 한 시도와 전용 anchor 이해 |
| `CAMP_MOVE` | 무료 재배치 뒤 room/좌표·자원·기능 보존 |
| `MODULE_ALL_SEEN` | 위·옆·지하 세 후보를 모두 순회해 유효·무효 원인을 본 여부 |
| `MODULE_CONFIRM` | `W2/D1`로 하나를 확정하고 connector로 실제 진입한 여부. 자동 산술과 별개로 인간 발견성·선택 긴장을 진단 |

Q5는 다음 의미로 교체한다.

- KO: “캠프에서 설비와 새 방을 쓰려면 김씨의 위치, 팝업, 연결 슬롯이 왜 중요했나요?”
- EN: “Why did Mr. Kim’s position, the contextual popup, and connection slots matter when using facilities and adding a room?”
- 1점: 설비에 직접 접근해 대상 팝업을 열며, 일반 설비는 호환 구역, 신호는 전용 앵커, 방은 연결 슬롯·통로 규칙을 따른다는 의미 중 세 요소를 모두 설명.

기존 시간 게이트가 실패하면 순서는 `보행 거리·prompt 발견성 → 후보 선택 안정성 → popup 포커스·문구 → 배치/connector 피드백 → 마지막으로 경제 비용`이다. 대형 대시보드를 다시 켜서 시간을 맞추거나 근거 없이 이동·자원 수치를 바꾸지 않는다.

## 12. 구현·QA 수용 조건

### 12.1 결정적 상태·거래

- 원거리, 1.25u 경계 안, 후보 중첩, 대상 비활성에서 단일 prompt와 안정 latch 결과가 표와 같다.
- 팝업 open/하위/Back/root Cancel에서 movement lock, focus owner와 ReturnSnapshot이 정확하다.
- LOCKED/SHORT를 Submit하면 전체 부족 피드백과 자원 변화 0, READY 중복 Submit은 차감·결과 1회다.
- 작업대·모닥불·빗물받이·저장/가방·신호 action이 소유권 표 밖에 0건이다.
- 기존 연구·제작·가방 4→6·신호 1→2의 비용·도구 보유·날짜 지속이 바뀌지 않는다.

### 12.2 배치·모듈

- start/upper/side/basement AABB와 slot 중심이 6절 좌표와 일치한다.
- 위·옆·지하 후보가 모두 preview되고, `room.basement.right` 예시는 `NO_CONNECTION_SLOT`이다.
- geometry valid/invalid와 cost ready/short가 별도 상태로 표시된다.
- `W2/D1` 충분/부족 상태로 하나를 확정·하나를 거부하고 자원 원자성을 확인한다. grant fixture는 자연 경제 증거가 아니다.
- 확정 room에 connector와 general-floor zone이 함께 생성되고 섬 출구에서 room·설비까지 경로가 있다.
- 일반 설비 2종을 서로 다른 유효 위치에 놓고 하나를 다른 확정 room으로 무료 재배치해도 자원·연구·기능이 보존된다.
- 신호 anchor, storage anchor, room instance는 재배치·복제되지 않는다.

### 12.3 UI·로케일·입력

- 정상 캠프에 `ui.camp.actions_title` 대형 패널과 상시 대형 가방 패널이 0개다.
- 1280×800 ko/en/qps-long에서 HUD, prompt, 최장 작업대 팝업과 module invalid 문구가 겹침·잘림·tofu·의미 숨김 말줄임표 0건이다.
- 김씨와 현재 대상이 팝업 또는 overlay에 동시에 완전히 가려지지 않는다.
- locale/device 전환 전후 상태 snapshot이 같고 glyph·문구만 바뀐다.
- 키보드·마우스 자동 경로와 합성 게임패드 경로는 같은 상태 결과를 내며, physical gamepad는 실기 전 `UNVERIFIED`다.

### 12.4 회귀와 증거 경로

예정 증거는 실제 구현·QA가 만든 뒤에만 연결한다.

| 증거 | 예정 경로 |
|---|---|
| 상태/소유권/원자성 | `Artifacts/Wave9/<run-id>/spatial-camp-state-contract.txt` |
| 모듈 좌표·slot·경로 | `Artifacts/Wave9/<run-id>/module-layout-contract.json` |
| ko/en/qps-long 1280×800 | `Artifacts/Wave9/<run-id>/spatial-camp-layout-summary.txt`와 PNG |
| 기존 루프 회귀 | `Artifacts/Wave9/<run-id>/three-day-regression.txt` |
| 실제 게임패드 | `Docs/QA/Results/wave9-physical-gamepad.md` 또는 명시적 `UNVERIFIED` |

이 설계 작업의 완료 증거는 본 문서, 승인 reference, Forge 상세 패킷과 계산 정본이다. 사람이 하지 않은 플레이테스트, 구현 PASS와 인간 구조 성공 분포를 만들어내지 않는다.

## 13. 남은 가정·위험과 조정 순서

1. 모듈 preview는 처음부터, commit은 작업대 뒤, 세 방향 비용은 모두 `W2/D1`로 잠갔다. 발견성 실패를 가격 문제로 오인하지 않고, 외부 증거 전 방향별 차등 비용을 만들지 않는다.
2. start `18×5u`, standard `12×5u`는 첫 프로토타입 레이아웃 기준이다. 1280×800에서 보행·가독성 문제가 재현되면 module size가 아니라 camera framing과 동선부터 조정한다.
3. general-floor가 두 설비와 접근 폭을 수용하지 못하면 zone 폭→connector 위치→마지막으로 room 크기 순으로 한 축만 조정한다.
4. 후보 흔들림이 재현되면 latch 해제 조건과 switch margin을 UX 변수로 추가할 수 있으나 실제 증거 전 숫자를 만들지 않는다.
5. 빗물받이는 경제상 열세 선택일 가능성이 남아 있지만 공간형 전환을 이유로 비용 `W2/S1/D1`이나 +10을 바꾸지 않는다.
6. 공식 영문 제목, Steam App ID, 실제 es/ja/zh 번역, 장기 캠페인 방 수와 저장/로드는 계속 `TBD/OUT_OF_SCOPE`다.

# Wave 11 직접 연결 슬롯 RED-first QA 게이트

- 공통 제품 기준: `origin/master` `72ec967a9009635fbeccbc758563183a67a4b311`
- QA 브랜치: `codex/wave11-slot-discovery-gate`
- 정본 RunId: `20260823T140000Z_72ec967_wave11_redfirst`
- Unity: `6000.4.9f1`
- Forge 대상: `task.qa.wave11-spatial-camp-contract-gate` 계열의 독립 Wave 11 직접 슬롯 계약
- 제품 런타임·씬·현지화·디자인·아트 변경: 없음

## 전체 판정

- 전체: **RED**
- 제품: **RED_EXPECTED_FAIL**
- 인프라: **PASS**
- 직접 슬롯 게이트: **RED_EXPECTED_FAIL (0/4 상위 계약 PASS)**
- 예상 밖 제품 실패: **0**
- `storage.planning` 보조 경로: **PASS_NOT_SUBSTITUTE**
- 전체 생존 회귀: **PASS**
- 물리 게임패드: **UNVERIFIED**
- Steam: **NOT_READY** — READY 주장 없음

현재 기준에는 `slot.start.upper`, `slot.start.side`, `slot.start.basement` 정의 자체는 있지만 시작 방의 독립 proximity target으로 등록되지 않는다. `ui.module.expand` 정본 문자열 키도 없다. 이 두 공백을 제품 예상 실패로 기록했으며, 기존 현장 계획 지점이 정상이라는 사실로 직접 슬롯 발견을 PASS 처리하지 않았다.

## 검증 행렬

| ID / 항목 | 결과 | 독립 관찰 |
|---|---|---|
| `W11-E01` 세 canonical slot/reciprocal ID | PASS | `slot.start.upper↔slot.upper.down`, `side↔left`, `basement↔up` |
| `W11-E02` 런타임 직접 슬롯 surface + `ui.module.expand` | EXPECTED_FAIL P0 | catalog-driven target 등록 없음, action key 없음 |
| `W11-P01` far/near/popup/첫 후보/cancel snapshot | EXPECTED_FAIL P0 | far 0 PASS, registered/near/popup-action-first-cancel 모두 0/3 |
| `W11-P02` 직접 접근·preview 1280×800와 보행 통로 | EXPECTED_FAIL P1 | 직접 접근 시도 3/3, 직접 preview 0/3, clear 0/3 |
| `W11-E03` 후보 순환·geometry/economy reason | PASS | Upper→Side→Basement, 9 canonical ID, LOCKED→SHORT(W1/D1)→READY(W2/D1) |
| `W11-E04` 원자성·1회 한도 | PASS | 취소/무효/부족/중복/두 번째 무차감, 성공 W-2/D-1 1회, PROTOTYPE_LIMIT |
| `W11-E05` 키보드/합성 게임패드 | PASS | interact/cycle/confirm/cancel 동일 의미, 장치별 prompt key 유지 |
| `W11-E06` ko/en/qps-long | EXPECTED_FAIL P1 | 기존 12개 module 이름·사유·placeholder와 qps 데이터는 정상, 직접 action `ui.module.expand`만 누락 |
| `W11-P03` `storage.planning` 보조 경로 | PASS_NOT_SUBSTITUTE | 세 후보 순환·중립 취소·같은 popup 복귀 PASS, 직접 슬롯 분자에서 제외 |
| `W11-P04` 전체 플레이 회귀 | PASS | 설비 prompt, 배치, 가방 4→6, 신호 1·2단계, 수색, 수영, 육지 복귀, 장벽/제작/연구, 3일 구조 |
| Unity 컴파일 | PASS | compiler errors 0, warnings 0 |
| Windows x64 Development build | PASS | `Succeeded`, errors 0, warnings 0 |
| 숨김 Player 스모크 | PASS | 1280×800 windowed, 6.318초 생존·응답 |
| Addressables | PASS | preflight/build/post-smoke, temporary link ownership/cleanup 안정 |
| 물리 게임패드 | UNVERIFIED | Unity batch에서 비어 있지 않은 joystick name 없음, 사람 실기 미실행 |

## 주요 결함

### P0 — W11-01 시작 방 직접 연결 슬롯을 발견할 수 없음

- 재현:
  1. 새 게임 캠프의 시작 방으로 진입한다.
  2. `slot.start.upper`, `slot.start.side`, `slot.start.basement`의 catalog display 위치로 각각 이동한다.
  3. 활성 proximity target ID와 안내 개수를 기록한다.
  4. 상호작용하여 슬롯 전용 popup 진입을 시도한다.
- 실제: canonical slot 정의는 있으나 `campInteractionTargets`에 해당 ID가 하나도 등록되지 않는다. near 0/3, 슬롯 popup 0/3이다. 위층 위치에서는 `storage.planning`, 지하실 위치에서는 인접 작업대 등 기존 대상이 선택될 수 있다.
- 예상 영향: 플레이어가 방 확장 위치를 공간에서 발견하거나 접근 슬롯에 맞는 후보로 시작할 수 없다. 보조 계획 지점만 사용하게 되어 Wave 11의 직접 슬롯 UX가 성립하지 않는다.
- 권장 수정 파일: `Assets/_Project/Scripts/Runtime/KimSurvivalPrototype.cs`, `Assets/_Project/Scripts/Runtime/PrototypeCampInteraction.cs`

### P1 — W11-02 직접 슬롯 action/localization/layout 증거 없음

- 재현:
  1. `PrototypeStrings.tsv`와 String Table에서 `ui.module.expand`를 조회한다.
  2. ko/en/qps-long으로 각 슬롯 popup의 단일 action 라벨을 포맷한다.
  3. 1280×800에서 세 슬롯 approach와 preview를 캡처한다.
- 실제: `ui.module.expand` 키가 없으며 직접 슬롯 popup 자체가 열리지 않아 preview 0/3, 보행 통로 clear 0/3이다. 기존 module 이름·reason ID·qps-long 데이터는 정상이다.
- 예상 영향: 후속 직접 슬롯 구현이 기존 `button.module.preview` 또는 보조 popup에 묶이면 action 소유권과 다국어 계약이 모호해질 수 있다.
- 권장 수정 파일: `Assets/_Project/Scripts/Localization/**`, `Assets/_Project/Scripts/Runtime/KimSurvivalPrototype.cs`

## 캡처 육안 검토

- `wave11-slot-upper-ko-approach-1280x800.png`: 직접 Upper가 아니라 `창고·증축 계획 지점 사용` 안내가 보인다.
- `wave11-slot-side-en-approach-1280x800.png`: Side 직접 안내/진입점이 없다.
- `wave11-slot-basement-qps-long-approach-1280x800.png`: Basement 직접 안내 대신 Workbench가 선택된다.
- 보조 `storage.planning`의 Upper/Side/Basement ko/en/qps-long preview 세 장은 모두 생성됐지만 `PASS_NOT_SUBSTITUTE`로만 사용한다.
- 상세 1:1 검토: `Artifacts/ParallelQA/20260823T140000Z_72ec967_wave11_redfirst/wave11-visual-review.md`

## GREEN 전환 조건

동일 러너는 타입명이나 새 enum 값에 의존하지 않고 catalog의 canonical `StartSlotId`로 런타임 target 목록을 조회한다. 후속 Unity 구현이 다음을 충족하면 현재 EXPECTED_FAIL 네 항목이 PASS로 전환된다.

1. 세 `StartSlotId`를 독립 target으로 등록하고 far 0/near 정확히 1을 유지한다.
2. popup의 활성 제품 action은 `ui.module.expand` 하나이며 Cancel은 별도 제어로 유지한다.
3. 접근한 slot과 첫 `CampModuleArchetype`이 일치한다.
4. preview 취소 후 위치·방·방향·target ID·candidate와 동일 popup이 복원된다.
5. ko/en/qps-long 직접 approach/preview 6장이 1280×800이고 prompt가 플레이어·슬롯·하단 보행 band를 가리지 않는다.

## 재현 명령

현재 제품 기준 RED 재현:

```powershell
& '.\Assets\Editor\ParallelQA\Invoke-Wave11SlotDiscoveryGate.ps1' `
  -RunId '<NEW_RUN_ID>' `
  -BaselineCommit '72ec967a9009635fbeccbc758563183a67a4b311' `
  -MinimumSmokeSeconds 6
```

후속 Unity 구현 통합 후에는 `-BaselineCommit`을 그 결합 HEAD로 바꾸고 같은 명령을 실행한다. 모든 Unity Editor/build와 Windows Player 과정은 `Docs/QA/unity-codex-sandbox-licensing.md`에 따라 샌드박스 밖에서 실행하며 `-noUpm`은 사용하지 않는다.

## 증거 경로

- 정본 폴더: `Artifacts/ParallelQA/20260823T140000Z_72ec967_wave11_redfirst/`
- 핵심 기계 판독 파일:
  - `wave11-summary.json`
  - `wave11-slot-edit-contracts.json`
  - `wave11-slot-play-contracts.json`
  - `wave11-slot-play-evidence.json`
  - `wave11-command-results.json`
  - `windows-development-build.json`
  - `windows-hidden-smoke.json`
  - `addressables-link-build-contract.json`
  - `addressables-link-post-smoke-contract.json`
- 전체 자동 회귀 텍스트: `wave11-full-regression.txt`

이 QA 브랜치에서는 제품 결함을 수정하지 않았으며 런타임·현지화·씬·아트·디자인·Forge 채택 상태를 변경하지 않았다.

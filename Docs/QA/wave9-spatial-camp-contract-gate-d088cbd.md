# Wave 9 공간형 캠프 레드 퍼스트 QA 계약 게이트

- 기준점: `d088cbdf021765a811ed88af9b22b58db49b917c` (`origin/master`)
- 브랜치: `codex/wave9-spatial-camp-contract-gate`
- 최종 독립 run: `20260823T044500Z_d088cbd_wave9_spatial_camp_red`
- Unity: `6000.4.9f1`
- Forge 작업: `task.qa.wave9-spatial-camp-contract-gate`
- 전체 판정: **RED**
- 제품: **RED_EXPECTED_FAIL**
- 테스트·빌드 인프라: **PASS**
- 물리 게임패드: **UNVERIFIED**
- Steam: **NOT_READY**

이 게이트의 RED는 현재 기준에 설비별 팝업과 모듈 증축이 아직 완성되지 않았다는 승인된 제품 사실이다. 컴파일, 기존 진행/가방 회귀, 접근 우선 전체 루프, Windows 빌드와 스모크는 별도 인프라 축으로 PASS했다. 이전 Wave 증거는 설계 감사에만 사용했으며 판정에는 새 run의 산출물만 사용했다.

## 검증 행렬

| 영역 | 판정 | 분류 | 핵심 관찰 |
|---|---|---|---|
| Unity 컴파일 | PASS | INFRASTRUCTURE | compiler error 0, warning 0 |
| 정상 캠프 전역 `campActions` 부재 | EXPECTED_FAIL | PRODUCT P0 | `campActionsActive=true` |
| 정상 캠프 대형 가방 패널 부재 | EXPECTED_FAIL | PRODUCT P1 | 활성 reference-canvas 면적 265,625 |
| 원거리 안내·팝업 부재 | PASS | PRODUCT | far prompt 0, popup 0 |
| 근거리 단일 대상 안내 | EXPECTED_FAIL | PRODUCT P0 | 모닥불/작업대/빗물받이/신호대 모두 prompt 0 |
| 상호작용 후 올바른 설비 팝업 | EXPECTED_FAIL | PRODUCT P0 | 네 대상 모두 popup 0; 기존 단문 feedback key만 발생 |
| 모달 중 이동 잠금 | EXPECTED_FAIL | PRODUCT P0 | 모달/이동 잠금 상태 API 없음 |
| 확인·취소 후 월드 복귀 | EXPECTED_FAIL | PRODUCT P0 | 설비 팝업과 confirm/cancel 경로 없음 |
| 대상별 행동 소유권 | EXPECTED_FAIL | PRODUCT P0 | 전역 행동판 활성, 대상별 팝업 없음 |
| 원거리 거절·근거리 실행 자원 원자성 | PASS | INFRASTRUCTURE | 원거리 연구 무차감, 근거리 연구 성공 |
| 상·옆·지하 모듈 후보 | EXPECTED_FAIL | PRODUCT P1 | discoverable module type 없음 |
| 모듈 유효/무효 원인 | EXPECTED_FAIL | PRODUCT P1 | validity reason 계약 없음 |
| 연결 슬롯·비용 | EXPECTED_FAIL | PRODUCT P1 | slot/cost 계약 없음 |
| 겹침·필수 통로 보존 | EXPECTED_FAIL | PRODUCT P0 | overlap/required-route 계약 없음 |
| 키보드 E·합성 게임패드 X 공통 경로 | PASS | INFRASTRUCTURE | 둘 다 `InteractPressed`로 수렴 |
| 물리 게임패드 실기 | UNVERIFIED | HARDWARE | Unity batch에서 장치명 0, 사람 조작 증거 없음 |
| KO/EN 1280×800 캡처 | EXPECTED_FAIL | PRODUCT P1 | 4개 새 캡처 생성; 정상 KO 프레임에서 TMP overflow 2, 전역 패널이 월드 점유 |
| 접근 우선 전체 생존·수색·수영·육지 복귀 | PASS | INFRASTRUCTURE | 현재 `RunAutomatedVerification` 전체 통과 |
| Wave 7 가방 회귀 | PASS | INFRASTRUCTURE | Edit/Play product+infrastructure PASS |
| Wave 6 신호·도끼·3일 진행 회귀 | PASS | INFRASTRUCTURE | Edit/Play product+infrastructure PASS |
| Addressables load/build/post-smoke | PASS | INFRASTRUCTURE | 임시 `link.xml` 소유권 연속성 PASS |
| Windows x64 Development build | PASS | INFRASTRUCTURE | 180,390,743 bytes, error 0, warning 0 |
| 숨김 Windows Player 스모크 | PASS | INFRASTRUCTURE | 최소 6초 생존·응답 PASS |
| Steamworks 출시 구성 | NOT_READY | RELEASE | SDK/API, App ID, Depot, Input, Cloud, Achievements 근거 없음 |

## 주요 결함과 재현

### P0 · 전역 대시보드가 공간형 캠프 계약을 대체하고 있음

1. Unity에서 `Assets/_Project/Scenes/KimSurvivalPrototype.unity`를 실행한다.
2. 새 게임의 정상 캠프 첫 프레임에서 이동하지 않는다.
3. 좌측 전역 제작/건설/연구 판과 우측 대형 가방 판이 활성인지 확인한다.
4. 승인 목업의 월드 우선 상태와 비교한다.

예상 영향: 월드 탐색과 설비 접근이 아니라 원거리 버튼 선택이 주 상호작용처럼 보이며 승인된 공간형 캠프 UX를 충족하지 못한다.

권장 수정 파일: `Assets/_Project/Scripts/Runtime/KimSurvivalPrototype.cs` 및 향후 설비 상호작용 런타임. 이 QA 브랜치에서는 수정하지 않았다.

### P0 · 네 설비의 근거리 안내·팝업·모달 복귀가 없음

1. 모닥불, 작업대, 빗물받이를 건설하고 구조 신호대 위치를 유지한다.
2. 각 설비의 1.25 unit 범위 안으로 이동한다.
3. 상호작용 전 활성 contextual prompt 수를 센다.
4. 공통 상호작용을 입력한 뒤 활성 facility popup 수와 대상명을 센다.

실제 결과: 네 대상 모두 prompt 0/popup 0이다. `message.camp.use.campfire`, `message.camp.use.workbench`, `message.camp.use.rain` 같은 기존 단문 피드백 또는 즉시 행동만 발생한다. 따라서 모달 이동 잠금과 확인/취소 복귀도 실행할 대상 UI가 없다.

예상 영향: 작업대·모닥불·빗물받이·신호대별 행동 소유권, 오입력 방지, 자원 소비 전 확인 계약을 검증할 수 없다.

권장 수정 파일: `Assets/_Project/Scripts/Runtime/KimSurvivalPrototype.cs`, `Assets/_Project/Scripts/Runtime/PrototypeCampUse.cs`, 향후 설비별 팝업/입력 상태 구현.

### P0/P1 · 모듈 증축 계약 미구현

1. 캠프 모듈 증축 진입점 또는 런타임 타입을 찾는다.
2. 위층·옆방·지하실 후보를 열고 연결 슬롯, 비용, 유효/무효 원인을 확인한다.
3. 기존 모듈 겹침과 입구/필수 통로 차단 후보를 확정해 본다.

실제 결과: module candidate, validity reason, connection slot, cost, overlap, required-route 계약을 발견하지 못했다.

예상 영향: 승인된 단계적 공간 확장을 플레이하거나 회귀 검증할 수 없다.

권장 수정 파일: 향후 Wave 9 모듈 배치 런타임, 현지화 키, 경로 보존 검증 구현.

### P1 · 1280×800 정상 캠프 가독성/월드 점유

1. `wave9-ko-normal-camp-1280x800.png`과 `wave9-en-normal-camp-1280x800.png`을 1:1로 연다.
2. 상단 상태/자원 헤더와 좌우 전역 패널을 비교한다.
3. 설비 근접 후 캡처에서도 설비별 소형 팝업 대신 동일한 전역 패널이 남는지 확인한다.

실제 결과: 자동 TMP 검사에서 정상 KO 프레임 overflow 2, EN 0을 기록했다. 육안상 두 언어 모두 좌우 패널이 캠프 월드의 큰 면적을 가리며, 영어 상단 상태와 자원 블록은 경계가 매우 촘촘하다. 근접 후에도 승인 목업의 소형 대상 팝업은 나타나지 않는다.

예상 영향: 1280×800에서 공간형 상호작용의 대상·맥락을 파악하기 어렵고 영어 확장 여백이 부족하다.

권장 수정 파일: `Assets/_Project/Scripts/Runtime/KimSurvivalPrototype.cs`와 향후 설비별 팝업 레이아웃. 기존 시각 게이트 수치는 변경하지 않았다.

## 회귀 및 격리 정책

- 기존 `ParallelQaRunner`의 전용 신호대 Edit 검사는 pre-proximity delegate 문자열을 찾고, 자연 Play 루프는 일부 작업대/신호 버튼을 대상 접근 없이 누른다. 이는 `STALE_TEST_ASSUMPTION`으로 격리했다.
- 격리된 구 테스트 실패는 전역 대시보드 복원의 근거가 아니다. 새 게이트와 현재 런타임의 접근 우선 `RunAutomatedVerification`이 대체 경로다.
- Wave 4 자산 게이트는 역사적 `wave5-current-visual-facts.json`과 671c4e9 identity를 요구한다. 핵심 자산 261건은 PASS했고, 네 개의 오래된 시각 증거 어댑터 실패만 별도 격리했다. 새 Wave 9 캡처를 과거 파일로 가장하지 않았다.
- 첫 진단 run `20260823T044000Z_d088cbd_wave9_spatial_camp_red`의 인프라 FAIL은 위 Wave 4 증거 결합을 발견한 기록이다. 최종 판정은 보정 후 전체 재실행한 `20260823T044500Z_d088cbd_wave9_spatial_camp_red`만 사용한다.

## 증거와 재실행

최종 증거 루트:

`Artifacts/ParallelQA/20260823T044500Z_d088cbd_wave9_spatial_camp_red`

핵심 파일:

- `wave9-summary.json` / `wave9-summary.txt`
- `wave9-edit-contracts.json` / `wave9-play-contracts.json`
- `wave9-spatial-play-evidence.json`
- `wave9-command-results.json`
- `wave9-legacy-harness-isolation.json`
- `wave9-wave4-visual-bridge-isolation.json`
- `compile-result.txt`
- `wave7-edit-contracts.json` / `wave7-play-contracts.json`
- `wave6-edit-contracts.json` / `wave6-play-contracts.json`
- `windows-development-build.json` / `windows-hidden-smoke.json`
- `addressables-link-build-contract.json` / `addressables-link-post-smoke-contract.json`
- `steam-readiness.json`
- `wave9-ko-normal-camp-1280x800.png`
- `wave9-en-normal-camp-1280x800.png`
- `wave9-ko-workbench-after-interact-1280x800.png`
- `wave9-en-rain-after-interact-1280x800.png`

Unity 시스템 브랜치 통합 후 같은 계약을 다시 실행하는 정확한 명령:

```powershell
& '.\Assets\Editor\ParallelQA\Invoke-Wave9SpatialCampContractGate.ps1' -RunId '<NEW_RUN_ID>' -BaselineCommit '<INTEGRATED_HEAD_SHA>' -MinimumSmokeSeconds 6
```

반환 코드 계약:

- `0`: 제품·인프라 모두 초록
- `2`: 인프라 PASS, 제품 EXPECTED_FAIL/RED
- `3`: 인프라 실패 또는 예상 밖 제품 실패

모든 Unity Editor/build/Windows Player 단계는 `Docs/QA/unity-codex-sandbox-licensing.md`에 따라 샌드박스 밖에서 실행하며 `-noUpm`을 사용하지 않는다.

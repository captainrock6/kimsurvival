# Wave 9 상호작용 안내 레이아웃 독립 QA 게이트

- 기준 커밋: `f95b192d45f04e36f173ae274e29a3684cce7bf0`
- 브랜치: `codex/camp-interaction-prompt-layout-gate`
- 판정 실행 ID: `20260823T073956Z_f95b192_prompt_layout`
- Unity: `6000.4.9f1`
- 실행 정책: `Docs/QA/unity-codex-sandbox-licensing.md`에 따라 Unity Editor, Windows 빌드와 Player 스모크를 Codex 샌드박스 밖에서 실행했다. `-noUpm`은 사용하지 않았다.
- 전체 / 제품 / 인프라: **RED / RED_EXPECTED_FAIL / PASS**
- 물리 게임패드: **UNVERIFIED**
- Steam: **NOT_READY**

현재 RED는 첨부 플레이테스트 화면에서 보고된 근접 안내의 크기·위치·월드 가림과 실제 `qps-long` 로케일 부재를 새 실행에서 재현한 예상 제품 실패다. 네 설비의 far/near/popup/cancel 상태 전이, 합성 키보드·게임패드 의미, Unity 컴파일, 접근 우선 전체 루프, Windows Development 빌드와 숨김 스모크는 별도 축으로 PASS했다. 런타임·씬·현지화 테이블·디자인 원본·아트·Forge 채택 상태는 수정하지 않았다.

## 판정 행렬

| 영역 | 판정 | 분류 | 새 실행 관찰 |
|---|---|---|---|
| Unity 컴파일 | PASS | INFRASTRUCTURE | compiler error 0, warning 0 |
| far 안내/팝업 | PASS | PRODUCT | prompt 0, popup 0 |
| 네 설비 near 안내 | PASS | PRODUCT | 모닥불·작업대·빗물받이·구조 신호대 각각 prompt 정확히 1, 대상 ID 일치 |
| popup-open | PASS | PRODUCT | 네 설비 모두 prompt 0, matching popup 1 |
| cancel/close 복귀 | PASS | PRODUCT | 네 설비 모두 같은 대상 prompt 1로 복귀 |
| 1280×800 안내 크기·위치 | EXPECTED_FAIL | PRODUCT P1 | KO/EN/qps stress 모두 `537.6×88px`, 허용 `≤512×50px`; 내레이션 하단과 `232px` 이격 |
| HUD·월드 가림 | EXPECTED_FAIL | PRODUCT P1 | 상단 HUD·하단 도움말은 비겹침, 플레이어·설비·필수 이동 통로는 겹침 |
| KO/EN TMP | PASS | PRODUCT | 화면 안, prompt TMP overflow 0 |
| 실제 qps-long | EXPECTED_FAIL | PRODUCT P1 | 데이터 로케일 미등록; 142% 합성 장문 캡처는 PASS를 대신하지 않음 |
| 키보드/합성 게임패드 | PASS | PRODUCT/AUTOMATED | 같은 `Campfire`/Interact 의미, KO `[E]`, EN `[E]`, EN gamepad `[X]` 전환 |
| 물리 게임패드 | UNVERIFIED | HARDWARE | Unity batch에서 비어 있지 않은 장치명과 사람 조작 증거 없음 |
| 접근 우선 생존·배치·수색·수영·가방·신호 루프 | PASS | INFRASTRUCTURE | 현재 `RunAutomatedVerification` 전체 통과 |
| Wave 6/7 옛 대시보드 시각 픽스처 | ISOLATED_BY_DESIGN | STALE_TEST_ASSUMPTION | 원거리 전역 대시보드 전제를 제품 회귀로 사용하지 않음; Wave 9 대체 전체 루프 PASS |
| Addressables load/build/post-smoke | PASS | INFRASTRUCTURE | 임시 `link.xml` 소유권 연속성 PASS |
| Windows x64 Development build | PASS | INFRASTRUCTURE | 0 errors, 0 warnings, EXE SHA-256 `93c19f9e7c681845d34407807d33b6438e781dd34c4d8895ebdf2c6fb083711d` |
| 숨김 Windows Player 스모크 | PASS | INFRASTRUCTURE | 1280×800 windowed, 6.301초 생존·응답 |
| Steam 출시 준비 | NOT_READY | RELEASE GAP | SDK/API, App ID, Depot, Input, Cloud, Achievements 근거 없음 |

## 주요 결함

### P1 · 근접 안내가 너무 크고 낮아 플레이어·설비·이동 통로를 가림

재현:

1. Windows 개발 빌드 또는 Unity Play Mode를 1280×800으로 실행한다.
2. 한국어·키보드 상태에서 모닥불 1.25 unit 안으로 이동한다.
3. `prompt-near-campfire-ko-keyboard-1280x800.png`을 1:1로 열고 `prompt-layout-evidence.json`의 KO Rect와 비교한다.
4. 영어·게임패드 상태와 qps 장문 스트레스 캡처에서도 같은 측정을 반복한다.

기대: 안내는 중앙 상단 내레이션 카드 바로 아래에 있고 카드와 8px 이상 떨어진다. 1280×800에서 폭은 약 40% 이하(`≤512px`), 높이는 약 `50px` 이하이며 상단 HUD·하단 도움말·플레이어·설비·바닥/필수 이동 통로를 가리지 않는다.

실제: 세 캡처 모두 안내 Rect가 `x=371.2, y=200.0, w=537.6, h=88.0`이다. 내레이션 카드 하단과 간격은 `232px`라 바로 아래가 아니며, 자동 투영과 육안 확인 모두 플레이어·캠프 설비·바닥 이동 통로 겹침을 기록했다. 첨부 원본 플레이테스트 화면의 약 `540×88px` 관찰과 독립 캡처가 일치한다.

예상 영향: 접근 대상과 이동 경로를 확인하는 순간 핵심 월드 정보가 가려지고, 안내가 실제 상호작용 대상보다 강한 시각 우선순위를 가져 공간형 캠프 경험을 훼손한다.

권장 수정 파일: `Assets/_Project/Scripts/Runtime/KimSurvivalPrototype.cs`의 `BuildUi` 내 `campProximityPrompt` 앵커·높이·텍스트 크기. 후속 구현은 새 QA 게이트를 바꾸지 않고 같은 명령으로 PASS 전환해야 한다. 이 브랜치에서는 제품 코드를 수정하지 않았다.

### P1 · 실제 qps-long 로케일이 없어 향후 장문 로케일 PASS를 주장할 수 없음

재현:

1. `Assets/_Project/Scripts/Localization/PrototypeStrings.tsv` 헤더와 Unity `AvailableLocales`를 확인한다.
2. 같은 캠프 진행 상태에서 `qps-long` 선택을 시도한다.
3. 실제 선택 코드와 prompt TMP 결과를 `prompt-layout-edit-contracts.json`, `prompt-layout-play-contracts.json`에서 확인한다.

실제: `qps-long` 데이터 열·선택 가능한 로케일이 없다. 게이트는 EN 안내를 기존 `Wave3VisualGate.ExpandPseudoLong`으로 142% 팽창해 별도 캡처했지만 이를 실제 로케일 PASS로 계산하지 않았다.

예상 영향: 향후 스페인어 등 장문 로케일에서 placeholder·글리프·레이아웃이 실제 Unity Localization 경로를 통과한다는 출하 근거가 없다.

권장 수정 파일: 향후 `Assets/_Project/Scripts/Localization/**`의 비출하 QA 로케일 데이터와 locale/font 설정. 런타임 locale별 분기는 추가하지 않는다.

## 예상 실패와 예상 밖 회귀 분리

- prompt 전용 예상 제품 실패: 4건 — 실제 qps-long 부재, 크기/내레이션 인접성, 월드/통로 가림, 실제 qps-long TMP 실행 불가.
- 기존 f95b192 통합 기준의 별도 예상 제품 실패: 모듈 증축 계약 4건, 한국어 정상 캠프 TMP overflow 1건. prompt 결함과 섞지 않고 `wave9-summary.json`에 그대로 보존했다.
- 예상 밖 제품 회귀: 0건.
- 테스트/빌드 인프라 실패: 0건.
- 첫 진단 실행 `20260823T073740Z_f95b192_prompt_layout`은 자식 회귀를 구형 Windows PowerShell로 잘못 선택해 `Get-FileHash`를 찾지 못한 러너 호스트 오류였다. 제품 판정에 사용하지 않았고, 호스트 선택을 고친 새 실행만 최종 판정으로 사용했다.

## 증거

- 최종 원시 증거: `Artifacts/ParallelQA/20260823T073956Z_f95b192_prompt_layout/`
- 최종 요약: `interaction-prompt-layout-summary.json`, `interaction-prompt-layout-summary.txt`
- 전용 계약: `prompt-layout-edit-contracts.json`, `prompt-layout-play-contracts.json`
- 픽셀·겹침 원자료: `prompt-layout-evidence.json`
- 핵심 캡처: `prompt-near-campfire-ko-keyboard-1280x800.png`, `prompt-near-campfire-en-gamepad-1280x800.png`, `prompt-near-campfire-qps-long-1280x800.png`
- 기존 전체 회귀: 같은 폴더의 `wave9-summary.json`, `wave9-play-contracts.json`, `wave9-approach-first-regression.txt`
- 빌드·스모크: `windows-development-build.json`, `windows-hidden-smoke.json`, `addressables-link-post-smoke-contract.json`
- 실행 명령·Unity 로그 경로: `interaction-prompt-command-results.json`

후속 Unity 구현 통합 뒤 정확한 재실행 명령:

```powershell
& '.\Assets\Editor\ParallelQA\Invoke-Wave9InteractionPromptLayoutGate.ps1' -RunId '<NEW_RUN_ID>' -BaselineCommit '<INTEGRATED_HEAD_SHA>' -MinimumSmokeSeconds 6
```

게이트가 초록으로 전환되려면 제품 checks가 모두 PASS하고 인프라가 PASS여야 한다. 물리 게임패드는 별도 사람 조작 증거가 생기기 전 계속 `UNVERIFIED`다.

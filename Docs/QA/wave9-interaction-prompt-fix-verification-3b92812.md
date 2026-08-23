# Wave 9 소형 근접 안내 수정 `3b92812` 독립 검증

- 공통 제품 기준: `f95b192d45f04e36f173ae274e29a3684cce7bf0`
- 보존한 QA 게이트: `0a43a448722bb6d27166a4c565399226d62ec059`
- 검증 대상 제품 수정: `3b92812e09a045950e634c5c9bfa21e1dc59f36c`
- 격리 결합 브랜치: `codex/isolated-verify-prompt-3b92812-20260823`
- 격리 결합 HEAD: `974abfa4fa1b7d9e0722da56e393b3c8ba67d658`
- 최종 QA 실행 ID: `20260823T075748Z_974abfa_prompt_fix_verify`
- Unity: `6000.4.9f1`
- 통합 권고: **PASS — `3b92812`의 targeted 제품 수정은 통합 가능**
- 전체 게이트 / 제품 / 인프라: **RED / RED_EXPECTED_FAIL / PASS**
- 물리 게임패드: **UNVERIFIED**
- Steam: **NOT_READY**

전체 게이트가 RED인 이유는 실제 `qps-long` 데이터 로케일, 기존 캠프 모듈 증축, 한국어 정상 캠프의 별도 TMP overflow 등 승인된 잔여 항목이다. `3b92812`가 목표로 한 근접 안내 geometry와 가림은 모두 PASS했고 새 제품 회귀와 테스트/빌드 인프라 실패는 0건이다. 따라서 제품 수정의 통합 여부와 전체 릴리스 준비도를 분리해 판정했다.

## 격리 결합 방법

제품 개발 중인 다른 Unity worktree는 검사하지 않았다. 새 로컬 worktree를 `0a43a44`에서 만들고, 공통 부모가 같은 `3b92812`를 cherry-pick했다.

```text
f95b192
└─ 0a43a44  qa: add compact camp prompt red-first gate
   └─ 974abfa  fix: compact camp proximity prompt  (3b92812 patch)
```

`3b92812` 자체가 남긴 과거 캡처와 PASS 문서는 판정에 재사용하지 않았다. 새 격리 worktree는 최초 실행 전 Unity `Library/PackageCache` 초기화가 필요했으며, 초기화가 완전히 종료된 뒤 다른 RunId로 정식 게이트를 실행했다. 최종 판정은 `20260823T075748Z_974abfa_prompt_fix_verify` 산출물만 사용한다.

## f95b192 RED와 수정 적용 결과 비교

| 항목 | f95b192 + QA `0a43a44` | `3b92812` 적용 결합 HEAD `974abfa` | 분류 |
|---|---:|---:|---|
| far 안내/팝업 | 0 / 0 PASS | 0 / 0 PASS | 유지 성공 |
| 네 설비 near prompt | 각각 정확히 1 PASS | 각각 정확히 1 PASS | 유지 성공 |
| popup-open prompt/popup | 0 / 1 PASS | 0 / 1 PASS | 유지 성공 |
| cancel/close 동일 대상 복귀 | 네 설비 PASS | 네 설비 PASS | 유지 성공 |
| prompt Rect | `x=371.2, y=200.0, 537.6×88.0px` | `x=384.0, y=464.0, 512.0×48.0px` | 예상 수정 성공 |
| 내레이션 카드와 간격 | `232px`, 바로 아래 아님 | `8px`, 바로 아래 | 예상 수정 성공 |
| 폭/높이 상한 | `>512×50`, FAIL | `≤512×50`, PASS | 예상 수정 성공 |
| 상단 HUD / 하단 도움말 겹침 | false / false | false / false | 유지 성공 |
| 플레이어 / 설비 / 필수 이동 통로 겹침 | true / true / true | false / false / false | 예상 수정 성공 |
| KO/EN prompt 화면 이탈·TMP overflow | 0 / 0 PASS | 0 / 0 PASS | 유지 성공 |
| 합성 장문 geometry/overflow | geometry FAIL / overflow 0 | geometry PASS / overflow 0 | 예상 수정 성공, 실제 locale 아님 |
| 실제 qps-long 데이터 locale | NOT_IMPLEMENTED | NOT_IMPLEMENTED | 별도 EXPECTED_FAIL 유지 |
| 키보드/합성 게임패드 action·대상 의미 | PASS | PASS | 유지 성공 |
| Unity 컴파일 | PASS, 0/0 | PASS, 0/0 | 회귀 없음 |
| 접근 우선 전체 루프 | PASS | PASS | 회귀 없음 |
| Windows Development build | PASS, warning 0 | PASS, warning 0 | 회귀 없음 |
| 1280×800 숨김 스모크 | PASS | PASS, 6.291초 생존·응답 | 회귀 없음 |

## 독립 검증 행렬

| 영역 | 판정 | 새 실행 근거 |
|---|---|---|
| far / 네 설비 near / popup / close | PASS 4/4 | `prompt-layout-play-contracts.json`의 `W9P-P01`~`P04` |
| 1280×800 크기·내레이션 인접성 | PASS | KO/EN/합성 장문 모두 `512×48px`, gap `8px` |
| HUD·플레이어·설비·통로 가림 | PASS | 세 레이아웃 모두 다섯 overlap 플래그 false |
| KO/EN TMP·화면 경계 | PASS | overflow false, insideScreen true |
| 키보드/합성 게임패드 | PASS | Campfire/Interact 의미 유지, `[E]`↔`[X]` 전환 |
| 실제 qps-long | EXPECTED_FAIL P1 | 데이터 locale 부재; 142% 합성 문자열은 PASS 대체 아님 |
| 물리 게임패드 | UNVERIFIED | 비어 있지 않은 장치명과 사람 actuation 증거 없음 |
| 접근 우선 생존·배치·가방·신호·수색·수영 | PASS | `wave9-approach-first-regression.txt` |
| Wave 6/7 옛 대시보드 시각 픽스처 | ISOLATED_BY_DESIGN | 공간형 캠프 이전 원거리 대시보드 전제; 현재 대체 루프 PASS |
| Addressables load/build/post-smoke | PASS | 세 ownership 계약 모두 PASS |
| Windows x64 Development build | PASS | 0 errors, 0 warnings |
| 숨김 Windows Player 스모크 | PASS | 1280×800 windowed, 6.291초 alive/responding |

## 예상 수정 성공, 남은 실패, 새 회귀

### 예상 수정 성공

- prompt가 내레이션 바로 아래 `8px`에 위치한다.
- 실제 높이는 `48px`, 폭은 정확히 화면의 40%인 약 `512px`다.
- KO keyboard, EN gamepad와 합성 장문 모두 동일 geometry를 유지한다.
- 플레이어, 설치 설비, 구조 신호대, 바닥/필수 이동 통로를 가리지 않는다.
- popup-open 숨김과 cancel/close 동일 대상 복귀가 유지된다.

### 남은 예상 실패

- 실제 `qps-long` Unity Localization 데이터와 선택 가능한 locale은 없다. 합성 `Wave3VisualGate.ExpandPseudoLong` 캡처는 geometry stress일 뿐 실제 locale PASS가 아니다.
- 기존 Wave 9 모듈 증축 계약 4건은 계속 RED다.
- 한국어 정상 캠프의 prompt 외 TMP 두 개가 overflow 플래그를 내는 기존 P1은 그대로다.
- 물리 게임패드는 `UNVERIFIED`, Steam은 `NOT_READY`다.

### 새 회귀

- 예상 밖 제품 회귀: 0건.
- 테스트/빌드 인프라 실패: 0건.
- 컴파일/Windows 빌드 경고: 0건.

## 통합 권고와 주의점

**`3b92812` 통합 권고는 PASS다.** 독립 게이트가 수정 목표를 수치와 캡처로 증명했고 상태 전이·입력 의미·전체 루프·빌드에 새 회귀가 없다.

- 제품 통합에는 원본 제품 커밋 `3b92812`를 사용한다. 격리 결합 커밋 `974abfa`는 QA 부모 `0a43a44`를 포함한 로컬 검증용이므로 제품 브랜치에 cherry-pick하지 않는다.
- 제품 통합 뒤에는 실제 통합 HEAD를 `-BaselineCommit`으로 넘기고 새 RunId로 같은 게이트를 재실행한다.
- 실제 qps-long과 물리 게임패드가 해결되지 않았으므로 이 PASS를 전체 로컬라이제이션·하드웨어·Steam 릴리스 PASS로 확대하지 않는다.
- 이 QA 작업은 runtime, scene, localization table, design, art와 Forge ledger를 수정하지 않았다.

## 명령과 증거

실행 명령:

```powershell
& '.\Assets\Editor\ParallelQA\Invoke-Wave9InteractionPromptLayoutGate.ps1' -RunId '20260823T075748Z_974abfa_prompt_fix_verify' -BaselineCommit '974abfa4fa1b7d9e0722da56e393b3c8ba67d658' -MinimumSmokeSeconds 6
```

제품 통합 후 재실행:

```powershell
& '.\Assets\Editor\ParallelQA\Invoke-Wave9InteractionPromptLayoutGate.ps1' -RunId '<NEW_RUN_ID>' -BaselineCommit '<ACTUAL_INTEGRATED_HEAD_SHA>' -MinimumSmokeSeconds 6
```

- 원시 증거: `Artifacts/ParallelQA/20260823T075748Z_974abfa_prompt_fix_verify/`
- 최종 요약: `interaction-prompt-layout-summary.json`, `interaction-prompt-layout-summary.txt`
- 상태/제품 계약: `prompt-layout-edit-contracts.json`, `prompt-layout-play-contracts.json`
- 실제 Rect·겹침: `prompt-layout-evidence.json`
- 캡처: `prompt-near-campfire-ko-keyboard-1280x800.png`, `prompt-near-campfire-en-gamepad-1280x800.png`, `prompt-near-campfire-qps-long-1280x800.png`
- 전체 회귀: `wave9-summary.json`, `wave9-play-contracts.json`, `wave9-approach-first-regression.txt`
- 빌드/스모크: `windows-development-build.json`, `windows-hidden-smoke.json`, `addressables-link-post-smoke-contract.json`

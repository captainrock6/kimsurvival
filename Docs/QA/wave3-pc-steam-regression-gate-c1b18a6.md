# Wave 3 독립 PC/Steam 회귀 게이트 · c1b18a6

- 전체 판정: **FAIL / PC·Steam 출시 NOT READY**
- 기준 커밋: `c1b18a6a2208cfbdb8ca5c726d861a4492fffc36`
- 감사 브랜치: `codex/wave3-qa-gates`
- 실행 ID: `20260822T130505Z_c1b18a6_wave3`
- Unity: `6000.4.9f1 (f7258d6eebbe)`
- 증거 루트: `Artifacts/ParallelQA/20260822T130505Z_c1b18a6_wave3/`
- 감사 성격: 진단 및 독립 QA 자동화 전용. 런타임 기능·아트·설계·Steam 설정은 수정하지 않았다.

## 전체 판정

c1b18a6은 Unity 컴파일, 결정적 Edit Check, ko/en 별도 프로세스 언어 유지, 한국어와 영어 각각의 3일 자연 생존 루프, 수영 입수·연안 채집·출수·육지 복귀, 제한적 자유 배치 기능, Windows x64 Development Build 및 8초 시작 스모크를 통과했다.

그러나 새 1280×800 픽셀 게이트는 FAIL이다. c1b18a6의 배치 개선은 실제 화면에서 확인된다. 한국어 상단 배치 상태는 21.2~21.3px로 통과하고, 유효/무효 색·고대비 카드·기기별 하단 안내가 명확해졌다. 반면 영어 상단 상태는 16.7px이며, 배치 월드 배지 20개는 9.1~15.2px로 전부 16px 기준에 미달한다. 특히 신호대 전용 앵커 문구는 화면 오른쪽을 벗어난다. 이 부분적 개선은 탐색/수영의 잔여 P1과 별도로 판정했다.

탐색/수영은 더 심각하다. 새 실행의 14개 핵심 월드 라벨이 모두 실패했고 실제 글자 중앙 높이는 6.0~10.0px다. 물 수색 라벨 대비는 3.8:1, 일부 돌 라벨은 1.1~1.4:1이며 우측 가방 UI와 겹친다. 장문 의사 로케일은 글리프 누락 없이 실행됐지만 10개 중 9개가 화면 경계·높이·overflow 기준을 실패했다. 제3 로케일을 실제 제품에 추가하는 구조도 TSV, 자산 빌더, 언어 순환 UI가 ko/en에 고정되어 `NOT READY`다.

물리 게임패드는 Unity가 비어 있지 않은 joystick name을 0개 보고했으므로 PASS로 간주하지 않았다. 공통 입력 모델, A/B/X/Y 코드 경로, 게임패드 배치 프롬프트, UI Submit·방향 탐색 자동 검증은 PASS이나 실기 판정은 `UNVERIFIED`다. Steamworks SDK, App ID, Depot, Steam Input, Cloud, Achievements는 저장소 근거가 모두 없어 각각 `NOT READY`다.

이 감사는 과거 2a6e9e6/e695c36 PASS·FAIL 파일을 결과 증거로 재사용하지 않았다. 모든 판정은 새 run ID에서 실행한 로그·PNG·계측·빌드로 산출했다.

## 검증 행렬

| 영역 | 판정 | c1b18a6 새 실행 결과 | 증거 |
|---|---|---|---|
| 기준선 정합성 | PASS | clean 상태 확인 후 fetch, `codex/wave3-qa-gates`를 origin/master에서 생성; HEAD/origin/master 일치 | `baseline-integrity.txt` |
| Unity 컴파일 | PASS / P3 경고 | compiler error 0, obsolete API warning 2 | `compile-result.txt`, `windows-build-warning-audit.txt` |
| 결정적 Edit Check | PASS | 자연 3일 구조, 가방 선택, 수영 비용/채집/복귀, 실패 경로 | `edit-checks.txt` |
| 제한적 자유 배치 기능 | PASS | 일반 설비 2종, 경계·겹침·출입구·필수 통로 거부, 취소 불변, 1회 차감, 무료 재배치, 전용 신호 앵커 | `edit-checks.txt`, `playmode-full-loop.txt` |
| 배치 유효/무효 Play Mode | PASS | ko 키보드·마우스 안내, en 게임패드 안내, 유효/무효 유령 캡처 및 자연 루프 내 설비 접근 | 배치 PNG 4장, `playmode-full-loop.txt` |
| 배치 1280×800 화면 게이트 | FAIL / 부분 개선 | 상단 상태 2/4 통과, 월드 배지 0/20 통과; 정상 ko/en overlap/overflow 0 | `wave3-visual-gate.txt`, `wave3-visual-metrics.tsv`, `visual-review.txt` |
| 한국어 기본·ko/en 즉시 전환 | PASS | 저장값 부재 시 ko, UI Submit 전환, UI 즉시 갱신 | `edit-checks.txt`, `playmode-full-loop.txt` |
| 언어 선택 재실행 유지 | PASS / 제한 | 별도 Unity 프로세스에서 en 저장 후 새 프로세스가 en 복원. Windows Player 수동 재실행은 미실행 | `locale-relaunch-stage1.txt`, `locale-relaunch-persistence.txt` |
| 누락 키 한국어 폴백/로그 | PASS | 의도적 en 누락 키가 한국어로 폴백, 서비스 인스턴스당 경고 1회 | `edit-checks.txt` |
| Smart String/수량 | PASS | 다중 인수, 수량 0/1/2/9999, ko/en 토큰 parity | `edit-checks.txt` |
| ko/en 글리프 | PASS | 대표 한글·라틴·`ñ`, 최종 GPU Play 로그의 TMP missing-glyph 0 | `edit-checks.txt`, `visual-review.txt` |
| 장문 의사 로케일 | FAIL | 142% 이상 확장, 10개 중 9개 실패; 경계 이탈과 자원 HUD overflow | qps-long PNG, `wave3-visual-gate.txt` |
| 제3 로케일 추가 구조 | NOT READY | SetLocale/font 목록은 부분 데이터 기반이나 TSV·빌더·cycle/버튼 분기가 ko/en 고정 | `localization-expansion-readiness.txt` |
| 플레이어 노출 하드코딩 스캔 | PASS | 감사한 text sink의 미승인 직접 리터럴 흐름 0 | `hardcoded-player-strings.txt` |
| 한국어 전체 핵심 루프 | PASS | Day 1~3, 수영/연안 채집/육지 복귀, 가방 교체, 제작·연구·배치·구조 | `playmode-full-loop.txt`, ko PNG |
| 영어 전체 핵심 루프 | PASS | 한국어와 같은 자연 경로로 구조 성공 | `playmode-full-loop.txt`, en PNG |
| 탐색/수영 1280×800 | FAIL / P1 | 14/14 실패, 글자 6.0~10.0px, 대비 1.1~3.8:1 사례, UI 가림 | `wave3-visual-metrics.tsv`, 수영/탐색 PNG 4장 |
| 키보드·마우스 | AUTOMATED PASS | 코드 경로, 공통 액션, 키보드·마우스 프롬프트, UI Submit 자동 실행 | `input-code-path-audit.txt`, `edit-checks.txt`, ko 배치 PNG |
| 게임패드 | AUTOMATED PASS / HARDWARE UNVERIFIED | A/B/X/Y, 축, 공통 액션, 게임패드 프롬프트, 방향 탐색 자동 실행; joystick 0 | `input-code-path-audit.txt`, `playmode-full-loop.txt`, en 배치 PNG |
| Windows Development Build | PASS | StandaloneWindows64, Development+AllowDebugging, 154,637,280 bytes, error 0 / warning 2 | `windows-development-build.txt` |
| Windows Player 시작 | PASS / 제한 | 8초 후 프로세스 응답, Input/D3D12 초기화, 예외·크래시 0; 조작 스모크 아님 | `windows-player-smoke.txt` |
| `-nographics` 화면 게이트 | FAIL / P2 QA 인프라 | 첫 Camera.Render에서 Unity Editor native crash; GPU 배치 재실행은 완료 | `headless-playmode-attempt.txt` |
| Steam 출시 준비 | FAIL / NOT READY | SDK, App ID, Depot, Steam Input, Cloud, Achievements 근거 없음 | `steam-readiness-audit.txt` |

## 주요 결함

### QA-W3-001 · P1 · 탐색/수영 핵심 월드 라벨이 1280×800에서 판독 기준 미달

재현 절차:

1. c1b18a6에서 GPU 사용 Play Mode Wave 3 러너를 실행한다.
2. ko/en 각각의 `day1-swimming` 또는 `day2-exploration` 1280×800 PNG를 100% 배율로 연다.
3. `wave3-visual-metrics.tsv`에서 `exploration-world` 행의 글자 높이·대비·가림을 확인한다.

예상: 자원명·수량과 `Swim to Search`/한국어 대응 안내가 최소 18px, 작은 텍스트 대비 4.5:1 이상이며 화면/UI에 가리지 않아야 한다.

실제: 14개가 모두 실패한다. 중앙 글자 높이는 6.0~10.0px다. 수영 안내는 3.8:1, 일부 돌 라벨은 1.1~1.4:1이며 우측 가방 UI에 가린다.

영향: 1280×800 및 휴대형 화면에서 자원 선택·연안 상호작용 발견성이 핵심 루프를 방해한다.

권장 수정 파일: `Assets/_Project/Scripts/Runtime/KimSurvivalPrototype.cs`의 자원 라벨 생성(약 830행), `CreateWorldLabel`(약 1070행), 필요 시 화면 공간 상호작용 프롬프트 경로.

### QA-W3-002 · P2 · 배치 개선은 확인되지만 영어 상태·월드 배지·전용 앵커가 게이트 미완료

재현 절차:

1. ko에서는 키보드·마우스, en에서는 게임패드 안내 픽스처로 모닥불 배치 모드에 진입한다.
2. 유효 위치 `-1.5`와 경계 밖 `-5.0`을 각각 1280×800으로 캡처한다.
3. 상단 상태 카드와 출입구·필수 통로·일반 설비·전용 앵커·유령 배지를 100%에서 비교한다.

예상: 상태는 18px 이상, 월드 배지는 16px 이상이며 모두 4px 화면 여백 안에 있어야 한다.

실제: 한국어 상단 상태 2개는 21.2~21.3px로 통과한다. 영어 상태 2개는 16.7px, 월드 배지 20개는 9.1~15.2px로 실패한다. 신호대 앵커는 최대 x=1296.4px까지 나간다. 대비와 정상 화면의 겹침/overflow는 통과한다.

영향: 큰 카드와 색 유령 덕분에 배치는 가능하지만, 영어 및 색 외 의미 전달·전용 앵커 파악은 작은 화면에서 약하다.

권장 수정 파일: `Assets/_Project/Scripts/Runtime/KimSurvivalPrototype.cs`의 `ApplyPlacementGuidance`(약 327행), 배치 월드 배지(약 414~428행), `CreateReservedCampStrip`(약 845행), `CreatePlacementGhost`(약 885행), `CreateWorldBadge`(약 1033행).

### QA-W3-003 · P2 · 장문/제3 로케일 준비도 미달

재현 절차:

1. `playmode-qps-long-placement-1280x800.png`를 100%로 연다.
2. `wave3-visual-metrics.tsv`의 `pseudo-long` 행을 확인한다.
3. TSV 헤더, Localization asset builder, `CycleLocale`, 언어 버튼 분기를 감사한다.

예상: 142% 확장 문자열이 경계·overflow·최소 16px을 통과하고, 세 번째 locale을 데이터 추가로 등록/선택할 수 있어야 한다.

실제: 10개 중 9개가 실패한다. HUD·자원·조작·언어 버튼·앵커가 화면 밖으로 나가며 자원 HUD가 overflow다. TSV와 builder, cycle/버튼 UI도 ko/en에 고정되어 있다. 최종 장문 픽스처의 누락 글리프는 0이다.

영향: 현재 ko/en 기능은 동작하지만 스페인어 등 제3 언어의 레이아웃·선택·자산 생성 준비를 보증할 수 없다.

권장 수정 파일: `Assets/_Project/Scripts/Localization/PrototypeStrings.tsv`, `Assets/_Project/Scripts/Editor/PrototypeLocalizationAssetBuilder.cs`, `Assets/_Project/Scripts/Runtime/PrototypeLocalization.cs`, `Assets/_Project/Scripts/Runtime/KimSurvivalPrototype.cs`, locale/table/font profile 자산.

### QA-W3-004 · P2 검증 공백 · 물리 게임패드 전체 루프 미실행

재현 절차:

1. 실제 XInput/Steam Input 호환 패드를 연결한다.
2. Windows Player에서 ko/en 전환, 배치 이동·확정·취소, 수영, 채집, 가방 교체, 귀환, 3일 구조를 수행한다.
3. 장치 이름, 매핑, 데드존, 포커스 및 중복 입력을 기록한다.

예상: 실기 입력만으로 두 로케일의 핵심 루프를 완주한다.

실제: Unity가 비어 있지 않은 joystick name 0개를 보고했다. 합성/공통 경로는 PASS지만 물리 입력은 `UNVERIFIED`다.

영향: 실제 A/B/X/Y 배치, 축 데드존, 포커스, Steam Input 변환을 출시 수준으로 보증할 수 없다.

권장 수정 파일: 먼저 실기 재검증. 실패 시 `Assets/_Project/Scripts/Runtime/PrototypePlayerInput.cs`, `ProjectSettings/InputManager.asset`, EventSystem/Steam Input 설정을 조사한다.

### QA-W3-005 · P1 출시 차단 · Steam 출시 구성 6개 영역 미구성

재현 절차:

1. `Assets`, `Packages`, `ProjectSettings`에서 Steam SDK/API, `steam_appid`, depot, Steam Input, Cloud, Achievement를 검색한다.
2. 빌드·업로드·스토어 구성 파일의 존재를 확인한다.

예상: 승인된 SDK와 App ID, Depot/upload, Steam Input, Cloud, Achievements의 테스트 가능한 설정과 배포 절차가 있어야 한다.

실제: 여섯 영역 모두 저장소 근거가 없어 `NOT READY`다. Windows Development Build만 성공했으며 non-development release build는 실행하지 않았다.

영향: 현재 산출물을 Steam에 통합·검증·배포할 수 없다.

권장 수정 파일: 승인 후 `Packages/manifest.json` 또는 SDK 플러그인 경로, App ID 설정, Steam Input action manifest, Cloud/Achievement 코드·설정, depot/build upload 스크립트. 이번 감사는 어느 것도 생성하지 않았다.

### QA-W3-006 · P2 QA 인프라 · `-nographics` 1280×800 캡처에서 Unity Editor native crash

재현 절차:

1. Unity 6000.4.9f1을 `-batchmode -nographics`로 실행한다.
2. `ParallelQA.ParallelQaRunner.RunPlayModeVerification`의 첫 배치 PNG 캡처까지 기다린다.

예상: headless CI에서도 렌더 타깃 PNG와 계측을 생성한다.

실제: `Camera.Render` 중 `GfxDevice::DrawSharedGeometryJobs`에서 native crash가 발생한다. `-nographics`를 제거한 GPU 배치 실행은 성공했다.

영향: 현재 시각 게이트는 GPU가 있는 에이전트가 필요하며 일반 headless CI에 바로 배치할 수 없다.

권장 수정 파일: `Assets/_Project/Scripts/Runtime/KimSurvivalPrototype.cs`의 캡처 helper 또는 `Assets/Editor/ParallelQA`의 별도 오프스크린 캡처 backend. 제품 런타임 결함으로 확정하지 않았다.

### QA-W3-007 · P3 · Unity 6 폐기 예정 API 경고 2건

재현 절차:

1. `BuildWindowsDevelopmentPlayer`로 Windows Development Build를 생성한다.
2. raw Unity 로그에서 `warning CS0618`을 검색한다.

예상: 컴파일 경고 0건.

실제: `KimSurvivalPrototype.cs:798`의 `FindObjectsByType<T>(FindObjectsInactive, FindObjectsSortMode)` 경로에서 서로 다른 CS0618 진단 2건이 발생한다.

영향: 현재 빌드는 성공하지만 향후 Unity API 제거 시 컴파일 위험이 있다.

권장 수정 파일: `Assets/_Project/Scripts/Runtime/KimSurvivalPrototype.cs` 약 798행. 이번 감사에서는 수정하지 않았다.

P0 결함은 발견되지 않았다.

## Steam 출시 준비표

| 항목 | 판정 | 근거 |
|---|---|---|
| Windows Development Build | PASS | x64 Development+AllowDebugging 성공, error 0 |
| Windows Player 시작 | PASS / 제한 | 8초 응답, Input/D3D12 초기화; 조작 스모크 아님 |
| Non-development release build | NOT RUN | 이번 범위는 Development Build로 고정 |
| Steamworks SDK/API | NOT READY | SDK/package/API wrapper 0 |
| App ID | NOT READY | `steam_appid.txt`/설정 0 |
| Depot/upload | NOT READY | depot/build/upload 설정 0 |
| Steam Input | NOT READY | 통합/action manifest 0 |
| Steam Cloud | NOT READY | 통합 근거 0 |
| Achievements | NOT READY | 통합 근거 0 |
| 물리 게임패드/Steam Deck | UNVERIFIED | Unity joystick name 0, 실기 없음 |

## 비결함 관찰과 제외 범위

- 정상 ko/en 탐색에서 TMP가 두 줄 가방 제목을 overflow 후보로 표시하지만, 원본 육안 검토에서는 두 줄이 패널 안에 보인다. 별도 제품 결함으로 승격하지 않았다.
- Unity Editor Search 인덱스가 시작 시 `ArgumentOutOfRangeException`을 기록하지만 제품 Play 루프와 Windows Player에서 재현되지 않았다.
- Windows Player의 D3D12 optional info queue 질의 실패는 초기화를 막지 않았다.
- 완전 자유 회전, 물리 적층, 복층 건축은 제외 범위다.
- 공식 영문 게임 제목은 미정이다.
- Steamworks 통합·배포·스토어 변경은 하지 않았다.

## 증거 경로

핵심 증거:

- `Artifacts/ParallelQA/20260822T130505Z_c1b18a6_wave3/baseline-integrity.txt`
- `Artifacts/ParallelQA/20260822T130505Z_c1b18a6_wave3/compile-result.txt`
- `Artifacts/ParallelQA/20260822T130505Z_c1b18a6_wave3/edit-checks.txt`
- `Artifacts/ParallelQA/20260822T130505Z_c1b18a6_wave3/locale-relaunch-persistence.txt`
- `Artifacts/ParallelQA/20260822T130505Z_c1b18a6_wave3/playmode-full-loop.txt`
- `Artifacts/ParallelQA/20260822T130505Z_c1b18a6_wave3/playmode-layout-metrics.txt`
- `Artifacts/ParallelQA/20260822T130505Z_c1b18a6_wave3/wave3-visual-gate.txt`
- `Artifacts/ParallelQA/20260822T130505Z_c1b18a6_wave3/wave3-visual-metrics.tsv`
- `Artifacts/ParallelQA/20260822T130505Z_c1b18a6_wave3/visual-review.txt`
- `Artifacts/ParallelQA/20260822T130505Z_c1b18a6_wave3/localization-expansion-readiness.txt`
- `Artifacts/ParallelQA/20260822T130505Z_c1b18a6_wave3/input-code-path-audit.txt`
- `Artifacts/ParallelQA/20260822T130505Z_c1b18a6_wave3/hardcoded-player-strings.txt`
- `Artifacts/ParallelQA/20260822T130505Z_c1b18a6_wave3/windows-development-build.txt`
- `Artifacts/ParallelQA/20260822T130505Z_c1b18a6_wave3/windows-build-warning-audit.txt`
- `Artifacts/ParallelQA/20260822T130505Z_c1b18a6_wave3/windows-player-smoke.txt`
- `Artifacts/ParallelQA/20260822T130505Z_c1b18a6_wave3/headless-playmode-attempt.txt`
- `Artifacts/ParallelQA/20260822T130505Z_c1b18a6_wave3/steam-readiness-audit.txt`
- `Artifacts/ParallelQA/20260822T130505Z_c1b18a6_wave3/run-summary.txt`

최신 1280×800 PNG 11장은 같은 증거 디렉터리에 있다. 원시 Unity/Player 로그와 Windows 바이너리는 저장소에 커밋하지 않고 `work/ParallelQA/20260822T130505Z_c1b18a6_wave3/`에 보존했다. 기존 `Artifacts/Verification/**`은 변경하지 않았다.

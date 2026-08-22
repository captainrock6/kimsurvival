# e695c36 독립 PC/Steam 준비도 감사

- 전체 판정: **FAIL**
- 기준 커밋: `e695c36d4a0a15c19f25630fe177fdd56298c1d6`
- 감사 브랜치: `codex/parallel-pc-steam-qa`
- 실행 ID: `20260822T113642Z_e695c36`
- Unity: `6000.4.9f1 (f7258d6eebbe)`
- 증거 루트: `Artifacts/ParallelQA/20260822T113642Z_e695c36/`
- 감사 성격: 진단 전용. 런타임 코드·아트·설계 문서·Steam 설정을 변경하지 않았다.

## 전체 판정

e695c36 통합본은 Unity 컴파일, 결정적 Edit Check, 한국어·영어 각각의 3일 자연 생존 루프, 수영 입수·연안 채집·출수·육지 복귀, Windows x64 Development Build와 시작 스모크를 통과했다. 제한적 자유 배치의 경계·겹침·출입구·필수 통로·비용·취소·무료 재배치·전용 구조 신호 앵커 규칙도 자동 검사에서 통과했다.

그러나 1280×800 원본 캡처에서 핵심 탐색 자원/상호작용 월드 라벨이 약 6.7~7.9 px로 렌더되어 1:1 판독이 어렵다. 이는 성공 기준의 “자원 수량과 상호작용 안내가 읽힌다”를 직접 위반하는 P1이다. 배치 영역·통로·전용 앵커의 월드 텍스트도 약 1.3~1.8 px이며, 큰 상단 안내가 보완하므로 P2로 분류했다. 물리 게임패드가 Unity에 노출되지 않아 실기 전체 루프는 미검증이다. Steamworks/App ID/Depot/Steam Input/Cloud/업적/스토어·업로드 구성도 없으므로 Steam 출하 준비도는 PASS가 아니다.

과거 `2a6e9e6` 감사 보고서는 역사적 기준선으로만 남아 있다. 이 판정은 새 e695c36 실행에서 생성한 로그와 화면만 사용했으며 과거 FAIL이나 PASS 파일을 재사용하지 않았다.

## 검증 행렬

| 영역 | 판정 | 새 실행에서 확인한 내용 | 증거 |
|---|---|---|---|
| 기준선 정합성 | PASS | clean 상태에서 `git fetch origin`, `git merge --ff-only origin/master`; HEAD와 origin/master가 e695c36으로 일치 | `baseline-integrity.txt` |
| Unity 컴파일 | PASS | Unity 실행 메서드 도달, compiler error 0 / warning 0 | `compile-result.txt` |
| 결정적 Edit Check | PASS | 자연 3일 구조, 가방 교체/포기, 수영 비용/채집/복귀, 실패 경로 | `edit-checks.txt` |
| 제한적 자유 배치 규칙 | PASS | 일반 설비 2종 배치, 경계·겹침·출입구·통로 거부, 취소 불변, 1회 차감, 반복 확정 거부, 무료 재배치, 구조 신호 전용 앵커 | `edit-checks.txt` |
| 배치 Play Mode | PASS | ko/en 유효·무효 유령, UI Submit 확정/취소, 자연 루프 내 작업대 배치 후 연구·제작 접근 | `playmode-full-loop.txt`, 배치 PNG 4장 |
| 배치 1280×800 가독성 | FAIL | 큰 상단 안내/색 유령은 보이나 월드 영역·통로·앵커 라벨이 1.3~1.8 px | `visual-review.txt`, `playmode-layout-metrics.txt` |
| 한국어 기본·즉시 전환 | PASS | 저장값 부재 시 ko, UI Submit으로 ko↔en 즉시 전환 | `edit-checks.txt`, `playmode-full-loop.txt` |
| 언어 선택 유지 | PASS / 제한 | 별도 Unity 프로세스에서 en 저장 후 새 프로세스가 en 복원. Windows Player 재실행 실조작은 미검증 | `locale-relaunch-stage1.txt`, `locale-relaunch-persistence.txt` |
| 누락 키 폴백/로그 | PASS | 의도적 en 누락 키가 한국어로 폴백하고 서비스 인스턴스당 1회 경고 | `edit-checks.txt` |
| Smart String/수량 | PASS | 다중 인수 Smart String과 영어 중립 명사 `Wood ×N`의 0/1/2/9999 경계를 결정적으로 확인 | `edit-checks.txt`, ko/en PNG |
| 번역 테이블/글리프 | PASS / 제한 | ko/en 키·포맷 인수 일치, 대표 한글·라틴·`ñ` 글리프와 캡처 tofu 0. 전체 스페인어 확장 글리프 집합은 미실행 | `edit-checks.txt`, `visual-review.txt` |
| qps-long 장문 로케일 | UNVERIFIED | 35~50% 확장 가짜 로케일 캡처를 생성하지 못함 | `visual-review.txt` |
| 플레이어 노출 하드코딩 | PASS | 감사한 text sink의 미승인 리터럴 0; 내부 오브젝트명/검증 진단은 허용 분류 | `hardcoded-player-strings.txt` |
| 한국어 전체 핵심 루프 | PASS | Day 1~3 캠프→수색→수영→채집→가방 교체→귀환→제작/연구/건설→구조 | `playmode-full-loop.txt`, ko PNG 3장 |
| 영어 전체 핵심 루프 | PASS | 한국어와 동일한 자연 경로로 구조 성공 | `playmode-full-loop.txt`, en PNG 3장 |
| 1280×800 주요 UI | PASS | HUD·버튼·가방·배치 상단 안내·결과 패널에 육안상 잘림/겹침 없음 | `visual-review.txt` |
| 1280×800 월드 상호작용 | FAIL | 자원명/수량과 수영 수색 안내가 ko 약 7.9 px, en 약 6.7 px | `visual-review.txt`, `playmode-layout-metrics.txt` |
| 키보드·마우스 | PASS / 제한 | 코드 경로와 자동 공유 액션/UI Submit 통과. Windows Player 물리 조작은 시작 스모크에서 수행하지 않음 | `input-code-path-audit.txt`, `playmode-full-loop.txt` |
| 게임패드 | AUTOMATED PASS / HARDWARE UNVERIFIED | 공통 액션, A/B/X/Y, UI Submit·방향 탐색 통과. Unity joystick name 0 | `input-code-path-audit.txt`, `playmode-full-loop.txt` |
| Windows Development Build | PASS | StandaloneWindows64, Development+AllowDebugging, 154,630,248 bytes, error 0 / warning 2 | `windows-development-build.txt` |
| Windows Player 시작 | PASS / 제한 | 8초 후 실행·응답 상태, Input 초기화, 예외/크래시 0. 조작 스모크는 아님 | `windows-player-smoke.txt` |
| Steam 출하 준비 | FAIL / NOT READY | Steamworks, App ID, Depot, Steam Input, Cloud/업적, 업로드 구성 없음; release build도 미실행 | `steam-readiness-audit.txt` |

## 주요 결함

### QA-E695-001 · P1 · 1280×800 탐색 자원/상호작용 월드 라벨 판독 불가

재현 절차:

1. e695c36에서 Play Mode 전체 루프를 `ko` 또는 `en`으로 시작한다.
2. 1280×800 렌더 타깃에서 1일차 연안 수영 또는 2일차 육상 수색 상태를 캡처한다.
3. PNG를 100% 배율로 열고 자원명·수량과 `Swim to Search`/한국어 대응 문구를 읽는다.

예상: HUD를 가리지 않으면서도 자원 종류·수량·현재 상호작용을 즉시 판독할 수 있어야 한다.

실제: 한국어 자원 라벨은 약 7.9 px, 영어 자원 라벨은 약 6.7 px로 렌더된다. Mr. Kim 라벨은 약 4.8~7.9 px이며 연안 상호작용 접두 문구도 같은 작은 월드 텍스트에 묶여 있다.

영향: 수색·연안 채집의 핵심 정보가 저해상도 PC/Steam Deck 목표 화면에서 읽히지 않아 자원 선택과 상호작용 발견성이 떨어진다.

권장 수정 파일: `Assets/_Project/Scripts/Runtime/KimSurvivalPrototype.cs`의 `CreateWorldLabel` 및 자원 라벨 생성 경로(현재 약 772, 913행). 월드 스케일 확대, 카메라 거리 보정 또는 화면 공간 프롬프트를 검토한다.

증거: `playmode-ko-day1-swimming-1280x800.png`, `playmode-ko-day2-exploration-1280x800.png`, `playmode-en-day1-swimming-1280x800.png`, `playmode-en-day2-exploration-1280x800.png`, `playmode-layout-metrics.txt`.

### QA-E695-002 · P2 · 배치 영역·통로·전용 앵커 월드 안내가 지나치게 작음

재현 절차:

1. ko/en에서 모닥불 배치 모드에 진입한다.
2. 유효 위치와 경계 밖 무효 위치를 각각 1280×800으로 캡처한다.
3. PNG 100% 배율에서 일반 설비 영역, 출입구 보호, 필수 통로, 구조 신호 앵커, 배치 유령 텍스트를 읽는다.

예상: 색을 구분하지 못해도 영역 의미와 전용 앵커 제한을 텍스트로 판독할 수 있어야 한다.

실제: 영역/통로 라벨이 약 1.3~1.8 px이고 영어 유령 라벨도 약 11 px다. 큰 상단 안내에는 ko/en 유효 문구와 경계 밖 거부 사유가 읽히며 유효 유령의 녹색 채움도 보인다. 경계 밖 픽스처의 무효 유령은 월드/UI 뒤로 벗어나지만 상단 사유가 조작을 보완한다.

영향: 경계·통로·앵커의 의미를 월드에서 확인하기 어렵고 저시력·휴대형 화면에서 색 피드백 의존도가 커진다.

권장 수정 파일: `Assets/_Project/Scripts/Runtime/KimSurvivalPrototype.cs`의 `CreateReservedCampStrip`, `CreatePlacementGhost`, 구조 신호 앵커 및 `CreateWorldLabel` 경로(현재 약 390~404, 787~829, 913행).

증거: ko/en placement PNG 4장, `playmode-layout-metrics.txt`, `visual-review.txt`.

### QA-E695-003 · P2 · 향후 로케일 확장/장문 레이아웃 게이트 미충족

재현 절차:

1. 로케일 자산 목록과 `PrototypeLocalization.CycleLocale`을 검사한다.
2. 세 번째 비출하 `qps-long` 로케일을 UI로 선택해 35~50% 확장 문자열을 1280×800에 렌더하려 한다.

예상: 로케일 목록이 데이터 기반이며 테스트 로케일을 런타임 화면 분기 수정 없이 선택·캡처할 수 있어야 한다.

실제: 등록 자산은 ko/en뿐이고 UI 순환은 ko↔en 이진 분기다. 이 실행에는 qps-long 캡처가 없다.

영향: 현재 ko/en 기능은 동작하지만 스페인어 등 장문 로케일의 여백과 세 번째 언어 추가 비용을 독립적으로 보증할 수 없다.

권장 수정 파일: `Assets/_Project/Scripts/Runtime/PrototypeLocalization.cs`, `Assets/_Project/Scripts/Editor/PrototypeLocalizationAssetBuilder.cs`, Localization locale/table/font profile 자산. 이번 감사에서는 수정하지 않았다.

증거: `visual-review.txt`, `Docs/QA/ko-en-localization-test-plan.md`의 qps-long 계약.

### QA-E695-004 · P2 검증 공백 · 물리 게임패드 실행 없음

재현 절차:

1. 현재 QA 환경에서 `Input.GetJoystickNames()` 결과를 확인한다.
2. 실제 XInput/Steam Input 호환 패드로 언어 전환, 배치, 수영, 가방 교체, 귀환, 구조 전체 루프를 시도한다.

예상: 장치 이름/VID·PID를 기록하고 실제 입력만으로 ko/en 핵심 루프를 완주해야 한다.

실제: 비어 있지 않은 joystick name이 0개였다. 코드 경로와 합성 액션/Submit/방향 탐색은 PASS지만 물리 입력은 실행하지 못했다.

영향: 포커스 유실, 축 데드존, 장치별 중복 입력, 실제 A/B/X/Y 매핑을 출하 수준으로 보증할 수 없다.

권장 수정 파일: 관찰된 제품 결함이 아니므로 우선 실기 재검증이 필요하다. 실패 시 `Assets/_Project/Scripts/Runtime/PrototypePlayerInput.cs`, `ProjectSettings/InputManager.asset`, UI EventSystem 설정을 조사한다.

증거: `input-code-path-audit.txt`, `playmode-full-loop.txt`.

### QA-E695-005 · P3 · Windows 개발 빌드에 폐기 예정 API 경고 2건

재현 절차:

1. `ParallelQA.ParallelQaRunner.BuildWindowsDevelopmentPlayer`로 StandaloneWindows64 Development Build를 생성한다.
2. Unity 빌드 로그에서 `warning CS0618`을 검색한다.

예상: 릴리스 준비 빌드는 컴파일 경고 0건을 유지한다.

실제: `KimSurvivalPrototype.cs:740`의 `FindObjectsByType<T>(FindObjectsInactive, FindObjectsSortMode)` 사용으로 CS0618 경고가 2건 집계된다.

영향: 현재 빌드를 막지는 않지만 향후 Unity API 제거 시 검증 경로 컴파일이 깨질 수 있다.

권장 수정 파일: `Assets/_Project/Scripts/Runtime/KimSurvivalPrototype.cs` 약 740행. 이번 감사에서는 수정하지 않았다.

증거: `windows-development-build.txt`, 로컬 원시 `work/ParallelQA/20260822T113642Z_e695c36/windows-build-unity.log`.

## 비결함 관찰 및 범위

- TMP의 `isTextOverflowing`은 캠프/수색의 두 줄 가방 제목을 경고 후보로 표시했지만, 원본 화면에서 두 줄 모두 컨테이너 안에 읽혀 결함으로 승격하지 않았다.
- Unity Editor Search 패키지의 시작 시 예외는 제품 Play 경로와 Windows Player에서 재현되지 않아 제품 결함으로 분류하지 않았다.
- 완전 자유 회전, 물리 적층, 복층 건축은 합의된 제외 범위다.
- 공식 영문 게임 제목은 미정이다. Windows 제품명은 한국어 제목을 유지하며, 이 감사는 임시 영문 UI 표기를 공식 제목 승인으로 취급하지 않는다.
- Steamworks 통합·배포·스토어 수정은 하지 않았다.

## 차단 이슈와 후속 게이트

현재 PC/Steam 준비도 PASS를 막는 항목은 P1 월드 라벨 가독성, 물리 게임패드 미검증, qps-long 장문 레이아웃 미검증, Steam 출하 구성 부재다. 월드 라벨 수정 후 동일 1280×800 상태의 새 캡처를 만들고, 실제 게임패드가 연결된 Windows Player에서 ko/en 전체 루프와 언어 재실행 유지를 다시 실행해야 한다. Steam 출하 판정은 별도 승인된 Steamworks/App ID/Depot/release build 작업이 시작된 뒤에만 재평가한다.

## 증거 경로

핵심 텍스트 증거:

- `Artifacts/ParallelQA/20260822T113642Z_e695c36/baseline-integrity.txt`
- `Artifacts/ParallelQA/20260822T113642Z_e695c36/compile-result.txt`
- `Artifacts/ParallelQA/20260822T113642Z_e695c36/edit-checks.txt`
- `Artifacts/ParallelQA/20260822T113642Z_e695c36/playmode-full-loop.txt`
- `Artifacts/ParallelQA/20260822T113642Z_e695c36/playmode-layout-metrics.txt`
- `Artifacts/ParallelQA/20260822T113642Z_e695c36/input-code-path-audit.txt`
- `Artifacts/ParallelQA/20260822T113642Z_e695c36/hardcoded-player-strings.txt`
- `Artifacts/ParallelQA/20260822T113642Z_e695c36/locale-relaunch-persistence.txt`
- `Artifacts/ParallelQA/20260822T113642Z_e695c36/windows-development-build.txt`
- `Artifacts/ParallelQA/20260822T113642Z_e695c36/windows-player-smoke.txt`
- `Artifacts/ParallelQA/20260822T113642Z_e695c36/visual-review.txt`
- `Artifacts/ParallelQA/20260822T113642Z_e695c36/steam-readiness-audit.txt`

원시 Unity/Player 로그와 빌드 바이너리는 저장소에 커밋하지 않고 `work/ParallelQA/20260822T113642Z_e695c36/`에 보존했다. 기존 `Artifacts/Verification/**`은 변경하지 않았다.

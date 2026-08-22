# 《김씨 생존기: 무인도》 독립 PC/Steam 준비도 감사

- 감사 기준: `master`/HEAD `2a6e9e6a30b8c2c3d0b80df7a6b1b49e9378693c`
- 감사 브랜치: `codex/parallel-pc-steam-qa`
- Unity: `6000.4.9f1 (f7258d6eebbe)`
- 최종 실행 ID: `20260822T100600Z_2a6e9e6`
- 범위: 진단, QA 도구, 별도 증거와 보고서만 작성. 런타임 수정, Steamworks 통합, 배포, 스토어 변경, push, master 병합 없음.

## 전체 판정

**FAIL — Windows 프로토타입의 컴파일·결정적 규칙·자연 3일 Play Mode 루프·Windows 개발 빌드/실행은 통과했지만, 1280×800 상호작용 라벨 가독성 기준이 실패했고 문서화된 D-pad 경로가 입력 설정에 없습니다. 실제 게임패드 하드웨어는 미검증이므로 PC/Steam·Steam Deck 준비 완료로 판정할 수 없습니다.**

Steam 배포 준비도는 현재 선언된 범위대로 **NOT READY**입니다. Steamworks/App ID/Depot/업로드/스토어 작업은 이번 감사에서 변경하거나 실행하지 않았습니다.

## 검증 행렬

| 항목 | 판정 | 독립 검증 결과 |
|---|---|---|
| 기준 커밋/기존 증거 격리 | PASS | HEAD와 `master`가 `2a6e9e6`; `Artifacts/Verification` diff 없음 |
| Unity 컴파일 | PASS | exit 0, compiler error 0, warning 0 |
| 결정적 Edit Check | PASS | 치트 없는 3일 자원 해법, 가방 선택, 수영 비용/채집, 구조·탈진·기한 실패 통과 |
| Play Mode 전체 루프 | PASS | `Grant` 없이 DAY 1→3, 수색·귀환·정산·제작·연구·건설·구조 성공 |
| 수영 입·출수/연안 채집 | PASS | 입수, 연안 표류물, 육지 복귀, 수상 채집의 더 큰 체력 비용 통과 |
| UI Submit/방향 탐색 | PASS | EventSystem Submit 경로와 활성 캠프 버튼 Navigation 그래프(초기 3, DAY 1 후 5) 통과 |
| 1280×800 HUD/패널 | PASS | 상단 HUD, 가방, 독백, 하단 조작, 결과 패널은 캡처에서 겹침·잘림 없이 판독 가능 |
| 1280×800 월드 상호작용 라벨 | **FAIL** | 명목 문자 높이 약 1.4px; 자원/수영/장애물 라벨을 판독할 수 없음 |
| 키보드 코드 경로 | PASS | A/D·방향키, Space/W, E/F, R/Esc, 숫자 슬롯 경로 존재 |
| 키보드 실행 경로 | PARTIAL | EventSystem 실행 및 Windows Input 초기화 통과; 실제 OS 키 입력 E2E 캡처는 미실시 |
| 게임패드 코드 경로 | PARTIAL | 스틱, A/B/X, Submit/Cancel 존재; D-pad 전용 축 매핑 없음 |
| 게임패드 실행 경로 | PARTIAL | 합성 Submit·방향 Navigation 통과; 물리 패드 0개로 실제 하드웨어는 **미검증** |
| Windows x64 개발 빌드 | PASS | Development + AllowDebugging, 148,920,796 bytes, errors/warnings 0 |
| Windows 1280×800 실행 스모크 | PASS | 8초간 응답 유지, Unity/D3D12/Input 초기화, 예외·크래시 0 |
| Steam 출하 준비 | NOT READY | SDK/App ID/Depot/업로드/스토어 검증 없음; 디버그 개발 빌드만 생성 |

## 주요 결함

### QA-001 · P1 · 1280×800 월드 상호작용 라벨 판독 불가

재현:

1. 독립 Play Mode 검증을 실행한다.
2. DAY 1 수영 또는 DAY 2 수색 캡처를 1:1로 연다.
3. 자원 다이아몬드 위의 `헤엄쳐 수색`, 자원명·수량, 장애물 안내를 읽는다.

예상: 1280×800에서 상호작용 대상과 자원/수량을 즉시 읽을 수 있어야 한다.

실제: `TextMesh`가 `localScale = 0.02f`로 생성되어 명목 문자 높이가 약 1.4px이다. 캡처에서는 점 또는 노이즈처럼 보이며, 성공 조건의 “상호작용 안내가 읽힌다”를 충족하지 못한다.

영향: 무엇을 채집하는지, 물에서 어떤 상호작용이 가능한지, 밧줄 장벽의 조건이 무엇인지 시각적으로 파악하기 어렵다. Steam Deck 후보 해상도에서 핵심 탐색 UX가 무너진다.

권장 수정 파일: `Assets/_Project/Scripts/Runtime/KimSurvivalPrototype.cs`의 `CreateWorldLabel`(766행 부근), 특히 771행 스케일. 카메라/해상도에 독립적인 Screen Space 안내나 읽을 수 있는 월드 텍스트 크기로 교체하고 1280×800 캡처를 다시 검증한다.

증거: `playmode-day1-swimming-1280x800.png`, `playmode-day2-exploration-1280x800.png`, `playmode-full-loop.txt`.

### QA-002 · P2 · 문서화된 D-pad 이동/UI 경로에 전용 축 매핑 없음

재현:

1. `README.md` 23–28행에서 이동·수영·UI 이동이 D-pad를 지원한다고 확인한다.
2. `ProjectSettings/InputManager.asset`을 열어 joystick 축 매핑을 확인한다.
3. `Horizontal`/`Vertical`은 주 축 0/1만 있고 일반적인 Windows 레거시 D-pad용 추가 축은 없다.

예상: 문서에 적힌 D-pad가 이동과 UI 탐색에서 동작해야 한다.

실제: 왼쪽 스틱 경로만 명시적으로 매핑되어 있고 D-pad 전용 경로는 확인되지 않는다. 물리 패드가 없어 실제 장치 결과는 미검증이다.

영향: 컨트롤러/Steam Deck 사용자가 D-pad로 이동하거나 메뉴를 탐색하지 못할 가능성이 있다. 스틱 경로 자체는 존재하며 합성 Navigation 검증은 통과했다.

권장 수정 파일: `ProjectSettings/InputManager.asset` 또는 입력 시스템을 액션 기반으로 전환할 경우 해당 Input Actions 자산. Xbox/PlayStation/Steam Input 장치 매트릭스로 재검증한다.

증거: `input-code-path-audit.txt`, `playmode-full-loop.txt`.

### QA-003 · P2 · 실제 게임패드 하드웨어 검증 공백

재현: 독립 Play Mode 보고서에서 `Input.GetJoystickNames()`의 비어 있지 않은 장치 수가 0인지 확인한다.

예상: 키보드와 실제 게임패드 각각으로 메뉴 포함 전체 루프를 완료하고 장치 전환을 검증해야 한다.

실제: 버튼/스틱 코드 경로, EventSystem Submit, Navigation 그래프는 검증했지만 물리 패드는 Unity 배치 실행에 노출되지 않았다.

영향: 버튼 번호 차이, 플랫폼별 레거시 축, 연결 해제/재연결, Steam Input/Steam Deck 동작은 보증할 수 없다.

권장 수정 파일: 우선 코드 수정이 아니라 Windows XInput 패드와 Steam Deck 실기 QA가 필요하다. 실패가 재현될 때 `ProjectSettings/InputManager.asset`과 `KimSurvivalPrototype.cs` 입력 경로를 수정한다.

### QA-004 · P3 · 스틱만 움직이면 현재 입력 장치 안내가 갱신되지 않음

재현:

1. 게임패드를 연결하고 새 게임을 시작한다.
2. 버튼을 누르지 않고 왼쪽 스틱만 움직인다.
3. 하단 장치 안내를 확인한다.

예상: 스틱 입력 즉시 `게임패드` 안내로 바뀐다.

실제: `DetectActiveDevice`는 joystick button down만 검사하고 축 변화는 검사하지 않으므로 초기 `키보드·마우스` 문구가 유지된다.

영향: 조작 안내가 실제 장치와 불일치한다.

권장 수정 파일: `Assets/_Project/Scripts/Runtime/KimSurvivalPrototype.cs` 520–540행.

### QA-005 · P3 · 문서에 없는 게임패드 Y 버튼도 점프를 실행함

재현:

1. `InputManager.asset`의 `Jump`가 `joystick button 3`에 매핑된 것을 확인한다.
2. 런타임은 같은 프레임에 `Input.GetButtonDown("Jump")`와 `JoystickButton0`을 모두 점프로 처리한다.
3. Xbox 계열 패드에서 A와 Y를 각각 누른다.

예상: 문서화된 A 버튼만 점프한다.

실제: 레거시 번호 기준 A와 Y가 모두 점프 경로에 들어간다. 물리 장치 확인은 QA-003 때문에 미검증이다.

영향: 낮은 수준의 조작 일관성 문제이며 플랫폼별 버튼 번호 차이를 숨길 수 있다.

권장 수정 파일: `ProjectSettings/InputManager.asset` 237행 부근과 `Assets/_Project/Scripts/Runtime/KimSurvivalPrototype.cs` 421행.

## Steam/배포 준비도

Windows x64 프로토타입은 빌드 및 실행 가능하다. 그러나 이번 산출물은 `Development + AllowDebugging`이고 player connection/debug 설정과 PDB를 포함한다. 저장소에는 Steamworks SDK, `steam_appid`, App ID, depot/upload 설정, 업적, 클라우드 저장 구현이 없다. 이는 현재 수직 슬라이스의 명시적 제외 범위와 일치하지만 Steam 출하 준비 완료를 뜻하지 않는다.

## 비제품성 관찰

Play Mode 종료 후 Unity Editor Search 인덱서가 `ArgumentOutOfRangeException`을 한 번 기록했다. QA 실행은 exit 0으로 완료됐고 Windows Player 로그에는 같은 예외가 없으므로 게임 결함으로 분류하지 않았다. 반복되면 로컬 `Library`/Search 인덱스 환경을 별도로 정리해 확인한다.

## 증거 경로

최종 증거 루트: `Artifacts/ParallelQA/20260822T100600Z_2a6e9e6/`

- `baseline-integrity.txt`
- `compile-result.txt`
- `edit-checks.txt`
- `input-code-path-audit.txt`
- `playmode-full-loop.txt`
- `playmode-day1-swimming-1280x800.png`
- `playmode-day2-exploration-1280x800.png`
- `playmode-rescue-result-1280x800.png`
- `windows-development-build.txt`
- `windows-player-smoke.txt`
- `steam-readiness-audit.txt`

빌드 바이너리와 Unity 원시 로그는 커밋하지 않으며 로컬 ignored 경로 `work/ParallelQA/20260822T100600Z_2a6e9e6/`에 보존한다.

# Wave 8 국제화 릴리스 게이트 독립 검증

- 작업 제목: 국제화 릴리스 게이트 독립 검증
- 독립 기준점: `origin/master` `2a542be0c2c9fa0a49f501bab1965bb59b5f06f3`
- Unity: `6000.4.9f1`
- 최종 run ID: `20260823T032400Z_2a542be_wave8_localization_release_gate`
- Forge 대상: `task.qa.feature.localization`
- 전체 판정: **FAIL**
- 제품 / 테스트 인프라: **FAIL / PASS**
- 물리 게임패드: **UNVERIFIED**
- Steam: **NOT_READY**. Steam READY를 주장하지 않는다.

이 실행은 기존 Wave 7 PASS 파일을 현재 판정 증거로 재사용하지 않았다. 과거 결과는 게이트 설계를 감사하는 데만 읽었고, 컴파일·Edit/Play·시각 회귀·Windows 빌드·숨김 스모크·별도 프로세스 로케일 복원을 새 run ID에서 다시 실행했다. 런타임, 현지화 테이블, 씬, 아트와 Forge 원장은 수정하지 않았다.

## 판정 행렬

| 영역 | 결과 | 독립 관찰 |
|---|---|---|
| Unity 컴파일 | PASS | 오류 0, 경고 0 |
| canonical 키 | FAIL · P1 | Forge 계약은 138키이나 현재 TSV는 중복 없는 144행이다. Wave 7 추가 6키를 반영한 새 canonical 버전 선언이 없다. |
| ko/en 값·placeholder 집합 | PASS | 의도적 `dev.fallback_probe`를 제외한 빈 값 0, 키별 토큰 집합 불일치 0 |
| named placeholder | FAIL · P1 | 43개 키가 위치형 `{0}` 계열을 사용하고 named placeholder는 0개다. |
| 실제 `qps-long` 데이터 로케일 | NOT_IMPLEMENTED · P2 | TSV 열, Locale asset, String Table, 폰트 매핑이 모두 없다. |
| `qps-long` 35~50%·토큰·글리프 | NOT_IMPLEMENTED · P2 | 실제 qps 데이터가 없어 판정 불가다. 사후 문자열 변형 픽스처는 제품 로케일 증거가 아니다. |
| QA 진행 snapshot 4/6 복원 도구 | PASS · 인프라 | 새 게임 4칸과 Day 2·6칸·연구·설비·신호 1단계·배치 좌표를 정규화해 동일 fingerprint로 복원했다. |
| ko→en→qps-long snapshot 불변 | FAIL · P1 | ko/en은 동일 상태로 복원됐으나 qps-long 요청은 ko로 폴백됐다. 진행 데이터 자체는 변하지 않았다. |
| 합성 키보드/게임패드 prompt 전환 | PASS | prompt만 바뀌고 `locale=en`과 진행 fingerprint는 유지됐다. |
| action placeholder | FAIL · P1 | action token 0개, 장치별 번역 키 분기와 `A/D`, `Space`, `Enter`, `Esc`, `left stick`, `D-pad` 본문 고정이 확인됐다. |
| 한국어 폴백·개발 로그 | PASS | 영어에서 누락 키를 두 번 조회해 한국어를 두 번 표시하고 식별 가능한 경고는 1건만 남겼다. |
| 언어 별도 Editor 프로세스 재실행 | PASS | `en` 저장 뒤 새 Unity 프로세스가 영어를 복원했다. |
| Windows Player 언어 재실행 | UNVERIFIED | 빌드·스모크는 실행했지만 Player UI에서 언어 선택→정상 종료→첫 프레임 재실행을 작동시키지는 않았다. |
| 1280×800 배치 / 탐색·수영 | PASS | 새 실행 기준 24/24, 10/10 |
| ko/en 가방 1280×800·1920×1080 | PASS | Wave 7 독립 레이아웃 계약 PASS |
| 합성 qps 1280×800 독립 중첩 | FAIL · P2 | 기존 15% 임계값은 10/10 PASS지만 새 5% 월드 라벨 블록 게이트와 1:1 육안 검토에서 겹침을 재현했다. |
| 실제 qps 1280×800·1920×1080 | NOT_IMPLEMENTED | 기존 10/10은 실제 로케일이 아니라 QA 문자열 변형 픽스처다. |
| Wave 7 전체 회귀 | PASS | Wave 7 Edit/Play/Layout, Wave 6, 배치, 수영, Addressables가 모두 새 실행에서 PASS |
| Windows x64 Development build / 숨김 스모크 | PASS / PASS | 빌드 경고 0, 6초 숨김 실행 응답 PASS |
| 물리 게임패드 | UNVERIFIED | 실제 장치의 사람 입력 증거가 없다. 합성 입력으로 승격하지 않았다. |

## 주요 결함

### W8-LOC-01 · P1 · canonical 계약 드리프트

- 재현: `PrototypeStrings.tsv`를 탭 구분으로 읽고 고유 Key 수를 센 뒤 Forge `task.qa.feature.localization`의 138키 계약과 비교한다.
- 기대: 현재 canonical 버전과 모든 로케일 테이블이 같은 명시적 키 집합을 사용한다.
- 실제: 중복 없는 144행이며 계약 대비 +6이다.
- 영향: 이후 qps 또는 제3 로케일의 완전성을 138과 144 중 어느 수로 승인해야 하는지 자동 게이트가 모호해진다.
- 권장 수정 파일: `.forge/backlog.json`, `Assets/_Project/Scripts/Localization/PrototypeStrings.tsv`. Wave 8 QA 브랜치에서는 수정하지 않았다.

### W8-LOC-02 · P1 · named placeholder 계약 미충족

- 재현: 전체 ko/en 문자열의 `{…}` 토큰을 추출해 이름이 숫자로 시작하는지 검사한다.
- 기대: 수량·날짜·자원·단계·입력은 `{day}`, `{count}`, `{resource}`, `{stage}`, `{action:confirm}` 같은 안정적인 이름을 쓴다.
- 실제: 43키가 `{0}` 계열이고 named placeholder는 0개다.
- 영향: 로케일별 어순 변경, Smart String 검증과 action prompt 치환이 인수 순서에 결합된다.
- 권장 수정 파일: `Assets/_Project/Scripts/Localization/PrototypeStrings.tsv`, `Assets/_Project/Scripts/Runtime/PrototypeLocalization.cs`.

### W8-LOC-03 · P2 · 실제 qps-long 데이터/글리프/레이아웃 미구현

- 재현: TSV header, Locale asset, String Table, 폰트 profile에서 `qps-long`을 찾고 실제 로케일을 선택해 35~50% 팽창·토큰·숫자·태그·확장 글리프를 검사한다.
- 기대: 비출하 qps-long이 데이터로 등록되고 1280×800 및 1920×1080 화면을 실제 로케일 경로로 렌더한다.
- 실제: 네 데이터 요소가 모두 없고 `SetLocale("qps-long")`은 ko로 폴백한다.
- 영향: 현재의 pseudo 10/10은 런타임 텍스트를 사후 변형한 QA 픽스처라 신규 로케일 등록, 폰트 매핑, 저장과 실제 레이아웃을 보증하지 못한다.
- 권장 수정 파일: `Assets/_Project/Scripts/Localization/**`, `Assets/_Project/Scripts/Editor/PrototypeLocalizationAssetBuilder.cs`, `Assets/_Project/Scripts/Runtime/PrototypeLocaleFontProfile.cs`.

### W8-LOC-04 · P1 · qps-long snapshot 복원 경로 부재

- 재현: 동일한 정규화 snapshot을 ko, en, qps-long에서 각각 복원하고 활성 로케일과 상태 fingerprint를 비교한다.
- 기대: 세 로케일이 각각 선택되고 Day·자원·연구·설비·신호·가방·배치가 같은 fingerprint다.
- 실제: 상태 fingerprint는 모두 동일하지만 qps-long 요청의 관찰 로케일은 ko다.
- 영향: 언어가 진행을 손상시키지는 않지만 제3 로케일의 저장 독립성을 실제로 검증할 수 없다.
- 권장 수정 파일: `Assets/_Project/Scripts/Runtime/PrototypeLocalization.cs`, `Assets/_Project/Scripts/Editor/PrototypeLocalizationAssetBuilder.cs`, `Assets/_Project/Scripts/Localization/**`.

### W8-LOC-05 · P1 · action placeholder 대신 장치별 본문/키 고정

- 재현: `controls.*` 행과 `PrototypeInputPromptKeys`를 검사하고 합성 장치 전환 전후 prompt를 비교한다.
- 기대: 안정적인 action ID가 `{move}`, `{confirm}`, `{cancel}`, `{language}`로 치환되며 locale과 진행은 불변이다.
- 실제: 합성 장치 전환과 상태 불변은 PASS지만 action token은 0개이고 장치별 키 분기와 구체 바인딩 본문이 존재한다.
- 영향: 리바인딩, 새 컨트롤러 glyph와 새 로케일에서 번역 본문을 함께 수정해야 한다.
- 권장 수정 파일: `Assets/_Project/Scripts/Runtime/PrototypePlayerInput.cs`, `Assets/_Project/Scripts/Localization/PrototypeStrings.tsv`.

### W8-LOC-06 · P2 · 1280×800 합성 qps 월드 라벨 겹침

- 재현: `playmode-qps-long-placement-1280x800.png`를 1:1로 열고 `wave3-visual-metrics.tsv`의 `qps-long placement valid` 월드 라벨 투영 경계를 비교한다.
- 기대: 작은 텍스트 블록 면적 기준 5% 미만 중첩이며 글자끼리 겹치지 않는다.
- 실제: 전용 앵커/일반 설비 라벨과 인접 구역 라벨에서 새 블록 게이트가 유의한 중첩을 검출했다. 기존 15% 기준은 10/10 PASS로 놓쳤다.
- 영향: 장문 로케일에서 설치 규칙과 전용 앵커 상태를 동시에 읽기 어렵다. 실제 qps 로케일이 아직 없다는 결함과 별개의 레이아웃 위험이다.
- 권장 수정 파일: `Assets/_Project/Scripts/Runtime/KimSurvivalPrototype.cs`, `Assets/Editor/ParallelQA/Wave3VisualGate.cs`. Wave 8은 기존 게이트 임계값을 바꾸지 않고 독립 5% 결과를 병기한다.

## 과거 통합 증거와 현재 판정 분리

과거 Wave 7 summary의 `qps-long PASS 10/10`은 `Wave3VisualGate.ExpandPseudoLong`으로 이미 렌더된 영어 UI 문자열을 변형한 시각 스트레스 픽스처다. 이 픽스처의 픽셀 기준 PASS 자체는 보존한다. 그러나 이번 Forge 수용 조건은 실제 qps-long table/locale/font/저장 경로를 요구하므로 제품 qps 준비도는 `NOT_IMPLEMENTED`다. 두 결과는 서로 모순되지 않으며 하나를 다른 하나의 PASS 증거로 사용하지 않는다.

## 증거와 재실행

주요 기계 판독 증거:

- `Artifacts/ParallelQA/20260823T032400Z_2a542be_wave8_localization_release_gate/wave8-summary.json`
- `Artifacts/ParallelQA/20260823T032400Z_2a542be_wave8_localization_release_gate/wave8-edit-contracts.json`
- `Artifacts/ParallelQA/20260823T032400Z_2a542be_wave8_localization_release_gate/wave8-progress-snapshot.json`
- `Artifacts/ParallelQA/20260823T032400Z_2a542be_wave8_localization_release_gate/wave8-missing-key-fallback-log.txt`
- `Artifacts/ParallelQA/20260823T032400Z_2a542be_wave8_localization_release_gate/wave8-qps-world-block-overlap.json`
- `Artifacts/ParallelQA/20260823T032400Z_2a542be_wave8_localization_release_gate/wave7-summary.json`
- `Artifacts/ParallelQA/20260823T032400Z_2a542be_wave8_localization_release_gate/wave3-visual-gate.txt`
- `Artifacts/ParallelQA/20260823T032400Z_2a542be_wave8_localization_release_gate/windows-development-build.json`
- `Artifacts/ParallelQA/20260823T032400Z_2a542be_wave8_localization_release_gate/windows-hidden-smoke.json`

Unity 라이선싱 정책상 다음 명령은 Codex 샌드박스 밖에서 실행한다. 새 run ID를 사용해야 하며 기존 증거 디렉터리를 덮어쓰지 않는다.

```powershell
& '.\Assets\Editor\ParallelQA\Invoke-Wave8LocalizationReleaseGate.ps1' `
  -RunId '<NEW_RUN_ID>' `
  -BaselineCommit '2a542be0c2c9fa0a49f501bab1965bb59b5f06f3'
```

## 남은 수동 게이트

- 실제 물리 게임패드로 언어 설정과 ko/en 핵심 루프를 사람 입력으로 완주하고 장치명·VID/PID를 기록한다.
- Windows Player에서 언어 선택→정상 종료→재실행 첫 프레임 복원을 확인한다.
- 실제 qps-long 구현 뒤 캠프/HUD/배치/수영/가방/결과의 1280×800·1920×1080 원본 PNG를 1:1 육안 검토한다.
- 공식 영문 게임 제목과 영어 원어민 문안 검수는 별도 사용자 결정으로 남긴다.

# Wave 13 플레이테스트 배포 키트 및 QA 실행기 호환성

## 판정

- 브랜치: `codex/wave13-playtest-kit`
- 제품 기준: `386a602f110ebfe2c404685f98f9cacf1b42c1d2`
- 최종 Run ID: `20260823T123000Z_386a602_wave13_release`
- Wave 12 전체/Product/Infrastructure: **PASS / PASS / PASS**
- Windows x64 Development 플레이테스트 패키지: **PASS**
- 한국어/영어 현재 시각 게이트: **PASS**
- qps-long 전체 월드/배치 시각 게이트: **FAIL 6/10** — 패키지 생성 실패와 분리된 향후 로케일 릴리스 한계
- 물리 게임패드: **UNVERIFIED**
- Steam: **NOT_READY** (`READY` 주장 없음)

런타임·씬·아트·현지화 테이블은 수정하지 않았다. QA PowerShell 실행기, 새 패키지 생성기, 체크리스트, 독립 증거만 변경했다.

## PowerShell 호환성 수정

통합 중 재현된 최초 실패는 `Capture-Wave5Preflight.ps1`의 `Set-Content -Encoding utf8NoBOM`이었다. 이 값은 PowerShell 7에는 있지만 Windows PowerShell 5.1의 `FileSystemCmdletProviderEncoding`에는 없어 Unity 시작 전에 매개변수 바인딩이 중단됐다.

수정 후 계약은 다음과 같다.

1. preflight와 hidden-smoke의 JSON/TXT 쓰기는 `.NET UTF8Encoding(false)`를 사용한다. 기존 파일명·JSON 스키마·BOM 없는 UTF-8 형식은 유지한다.
2. JSON/TXT 읽기는 `-Encoding UTF8`을 명시한다. PS 5.1이 BOM 없는 JSON의 한글 작업 경로를 ANSI로 오해하지 않는다.
3. Wave3 보고서의 가운데점 구분자는 소스 인코딩과 무관한 정규식 `\u00B7`로 판정한다.
4. `Invoke-Wave12FiveDayUiGate.ps1 -PreflightOnly`는 Unity나 work 폴더를 만들지 않고 두 preflight 파일의 UTF-8 no-BOM 여부와 셸 버전을 별도 증거로 남긴다.

독립 실행 결과:

| 호스트 | 모드 | 결과 | UTF-8 no-BOM |
|---|---|---:|---:|
| PowerShell Core 7.6.4 | PreflightOnly | PASS | JSON/TXT PASS |
| Windows PowerShell Desktop 5.1.26100.9168 | Full Wave 12 | PASS | JSON/TXT PASS |

## 검증 행렬

| 항목 | 결과 | 최종 증거 |
|---|---|---|
| Unity 6000.4.9f1 컴파일 | PASS | errors/warnings `0/0` |
| Wave 12 전체 제품 계약 | PASS | 5일 기한, compact-a, glyph/TMP 분리, 직접 슬롯, 전체 생존 회귀 |
| compact-a ko/en/qps 12상태 1280×800 | PASS | `12/12`, prompt `440×44`, narration gap `12`, overflow `0` |
| 기존 Wave3 ko/en 배치 | PASS | `24/24` |
| 기존 Wave3 수색·수영 | PASS | `10/10` |
| 기존 Wave3 qps-long | FAIL | `4/10` 통과, `6/10` 높이·경계·overflow·겹침 실패 |
| Addressables | PASS | load/build/post-smoke 및 임시 link 소유권 |
| Windows x64 Development build | PASS | errors/warnings `0/0` |
| 1280×800 hidden smoke | PASS | alive/responding `6.422s` |
| PowerShell 5.1/7 진입점 | PASS | 각각 새 RunId, BOM 없음 |
| 키보드·마우스 | PASS(자동 경로) + 수동 체크리스트 제공 | 기존 전체 Wave 12 입력/루프 계약 |
| 합성 게임패드 | PASS(자동 경로) | locale/target/progress 불변 및 동일 action 의미 |
| 물리 게임패드 | UNVERIFIED | 실제 장치/사람 조작 없음 |
| Steam 출시 | NOT_READY | App ID/권한/배포 증거 없음 |

## 플레이테스트 패키지 계약

생성기는 같은 RunId와 기준 커밋의 `windows-development-build.json`, `windows-hidden-smoke.json`, `wave12-summary.json`이 모두 성공한 경우에만 패키지를 만든다. Windows PowerShell 5.1의 경로 길이 제한을 피하기 위해 비추적 staging만 짧은 결정적 경로를 사용하며, manifest에는 원래 RunId·기준·빌드 경로를 모두 기록한다.

필수 구성요소:

| 구성요소 | 결과 |
|---|---:|
| `KimSurvivalIsland.exe` | PASS, 원본/패키지 SHA-256 일치 |
| `KimSurvivalIsland_Data/` | PASS, 파일별 SHA-256 및 디렉터리 집계 SHA-256 |
| `UnityPlayer.dll` | PASS, 원본/패키지 SHA-256 일치 |
| `MonoBleedingEdge/` | PASS, 파일별 SHA-256 및 디렉터리 집계 SHA-256 |

- payload: `266` files, `180,593,897` bytes
- ZIP entries: `268`, 필수 엔트리 `9/9` PASS
- ZIP bytes: `69,146,634`
- ZIP SHA-256: `b4d49b04faa92c2438afad19d7d5d0ee0a4beb5db3d60199d516221d2ead59c4`
- manifest SHA-256: `2ba85992d917ca768038a8abc90928717dac6f235718ec331333e5eab91c6625`

ZIP은 대용량 빌드 산출물이므로 Git에 넣지 않고 `work/W13/20260823T123_e13_release/KimSurvivalIsland-Windows-x64-Development.zip`에 생성한다. 커밋에는 재생성기, 파일별 manifest/SHA-256, 빠른 시작과 체크리스트, 전체 Unity 증거를 포함한다.

## 남은 수동/릴리스 게이트

1. 실제 물리 게임패드로 ko/en 전체 핵심 루프와 장치 glyph 전환을 사람이 완료하기 전 `PHYSICAL_GAMEPAD=UNVERIFIED`를 유지한다.
2. qps-long compact-a 근접 안내 자체는 PASS지만 기존 월드/배치 장문 프레임은 `6/10` 실패한다. 향후 제3 로케일 출시 전 별도 레이아웃 작업과 재검증이 필요하다.
3. Steamworks SDK/App ID/Depot/Input/Cloud/Achievements 및 배포 권한 증거가 없으므로 `STEAM=NOT_READY`다.

## 재실행 명령

모든 Unity/Player 실행은 `Docs/QA/unity-codex-sandbox-licensing.md`에 따라 Codex 샌드박스 밖에서 실행하고 `-noUpm`을 사용하지 않는다.

```powershell
# Windows PowerShell 5.1 호환성 preflight
& 'C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe' -NoProfile -ExecutionPolicy Bypass -File `
  '.\Assets\Editor\ParallelQA\Invoke-Wave12FiveDayUiGate.ps1' `
  -RunId '<NEW_PS51_RUN_ID>' -BaselineCommit '386a602f110ebfe2c404685f98f9cacf1b42c1d2' -PreflightOnly

# PowerShell 7 호환성 preflight
& pwsh -NoProfile -File '.\Assets\Editor\ParallelQA\Invoke-Wave12FiveDayUiGate.ps1' `
  -RunId '<NEW_PS7_RUN_ID>' -BaselineCommit '386a602f110ebfe2c404685f98f9cacf1b42c1d2' -PreflightOnly

# 전체 Wave 12: Unity를 포함하므로 샌드박스 밖에서 실행
& 'C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe' -NoProfile -ExecutionPolicy Bypass -File `
  '.\Assets\Editor\ParallelQA\Invoke-Wave12FiveDayUiGate.ps1' `
  -RunId '<NEW_FULL_RUN_ID>' -BaselineCommit '386a602f110ebfe2c404685f98f9cacf1b42c1d2' -MinimumSmokeSeconds 6

# 같은 RunId의 성공 빌드로 플레이테스트 ZIP 생성
& 'C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe' -NoProfile -ExecutionPolicy Bypass -File `
  '.\Assets\Editor\ParallelQA\New-Wave13PlaytestPackage.ps1' `
  -RunId '<NEW_FULL_RUN_ID>' -BaselineCommit '386a602f110ebfe2c404685f98f9cacf1b42c1d2'
```

## 증거 경로

- PS 5.1 전체/패키지: `Artifacts/ParallelQA/20260823T123000Z_386a602_wave13_release`
- PS 7 final preflight: `Artifacts/ParallelQA/20260823T124000Z_386a602_wave13_ps7_final`
- 로컬 ZIP: `work/W13/20260823T123_e13_release/KimSurvivalIsland-Windows-x64-Development.zip`

# Wave 4 자산 릴리스·Steam 준비 QA — fed5066

## 전체 판정

- **전체: FAIL / Steam 출시 준비: NOT READY**
- 기준선: `origin/master`의 `fed50669a37a66aec436c2174445e5107b74d57c`
- 브랜치: `codex/wave4-asset-release-qa`
- 독립 실행 ID: `20260822T141021Z_fed5066_wave4`
- Unity: `6000.4.9f1 (f7258d6eebbe)`
- 실행 시각: 2026-08-22 14:10:21Z–14:13:00Z
- 물리 게임패드: **UNVERIFIED** — Unity 배치 모드에 노출된 비어 있지 않은 장치명이 없었고 사람의 실제 장치 조작을 수행하지 않았다.
- 범위 통제: QA harness, `Artifacts/ParallelQA`, 이 문서만 추가했다. 런타임·씬·아트·기획 문서·Wave 3 시각 게이트 수치는 수정하지 않았다. Unity가 재생성한 `link.xml`과 메타는 증거를 남긴 뒤 fed5066 원본으로 복원했다.

Windows 실행 파일은 성공적으로 만들어져 숨김 상태로 6.279초 동안 살아 있고 응답했지만, 채택 자산 계약 7건과 Addressables GUID 안정성 계약이 실패했다. 또한 Steamworks의 출시 필수 구성은 저장소에서 확인되지 않았다. 따라서 빌드 성공만으로 출시 가능 판정을 내릴 수 없다.

## 검증 행렬

| 항목 | 결과 | 독립 관찰 |
| --- | --- | --- |
| git 기준선 | PASS | 새 브랜치 생성 전 `origin/master`가 정확히 fed5066임을 확인 |
| Unity 스크립트 컴파일 | PASS | 오류 0, 경고 0; QA execute method 진입 |
| `.forge/assets.json` / engine-ready 패키지 | PASS | currentJobId, 패키지 디렉터리, manifest identity, quality `pass`/errors 0, 선언 PNG와 PNG ledger bytes를 자동 대조 |
| 배경 3레이어 importer | PASS | Sprite/Single, PPU, center pivot, Bilinear, mipmap/alpha/compression/max size, 1672×941 캔버스와 layer manifest 계약 통과 |
| 구조물 importer 공통 계약 | PASS | atlas와 구조물 PNG의 Sprite/Single, alpha, PPU, Bilinear, mipmap/compression/max size 통과 |
| 구조물 전용 pivot | **FAIL (P1)** | 분리 구조물 4종이 metadata의 bottom-center 값 대신 `(0.5,0.5)` |
| 최신 currentJobId 빌드 도달성 | **FAIL (P1)** | 최신 배경 3 PNG와 구조물 5 PNG 중 enabled scene/Addressables에서 도달 가능한 소스 0 |
| Addressables preflight→Unity | **FAIL (P1)** | 첫 Unity 로드 후 추적 중인 `link.xml`과 메타가 사라짐 |
| Addressables build 전→후 | **FAIL (P1)** | 빌드가 link 파일을 재생성하고 메타 GUID를 `6cc9…`에서 `3e44…`로 변경 |
| Windows x64 Development build | PASS | `Succeeded`, errors 0, warnings 0, 161,721,511 bytes; EXE SHA-256 기록 |
| Windows 숨김 스모크 | PASS | 1280×800 windowed, hidden, 최소 6초 요구에 6.279초, alive/responding |
| 물리 게임패드 | **UNVERIFIED (P2 gap)** | 자동/코드 경로가 실제 하드웨어 조작을 대체하지 않음 |
| 1280×800 픽셀 게이트 기준선 | **FAIL (기존 P1)** | Wave 3의 배치/탐색·수영 픽셀 게이트 FAIL과 임계값·소스 해시를 그대로 보존 |
| Steamworks SDK/API | NOT READY | 표적 저장소 검색 결과 없음 |
| Steam App ID | NOT READY | 표적 저장소 검색 결과 없음 |
| Depot/upload | NOT READY | 표적 저장소 검색 결과 없음 |
| Steam Input | NOT READY | 표적 저장소 검색 결과 없음 |
| Steam Cloud | NOT READY | 표적 저장소 검색 결과 없음 |
| Steam Achievements | NOT READY | 표적 저장소 검색 결과 없음 |

자산 계약의 기계 판독 집계는 **151 PASS / 7 FAIL / 1 UNVERIFIED**다.

## 주요 결함

### W4-AR-001 — P1 — Addressables link GUID가 Unity 로드/빌드에서 안정적이지 않음

재현:

1. fed5066에서 `Capture-Wave4Preflight.ps1`을 실행한다.
2. `ParallelQA.ParallelQaRunner.RecordCompilePass` 또는 자산 계약 method로 Unity를 처음 배치 실행한다.
3. `Assets/AddressableAssetsData/link.xml`과 `.meta`가 사라진 상태를 확인한다.
4. `ParallelQA.Wave4AssetReleaseGate.BuildWindowsDevelopmentPlayer`를 실행한다.
5. `addressables-link-build-contract.json`의 preflight/before/after를 비교한다.

예상: 추적 중인 link 파일의 존재·해시·메타 GUID가 Unity 로드와 Windows 빌드 전후에 유지된다.

실제: preflight GUID는 `6cc923a77c080d942b9683f3b4ad54f7`이지만, 빌드 후 `3e44edc7227fd0f499688d59b124e8cb`로 재생성됐다. link 본문도 마지막 개행 차이로 해시가 달라졌다.

영향: GUID 참조 안정성, 재현 가능한 빌드, 소스 제어 청결성과 Addressables/링커 산출물 신뢰성이 훼손될 수 있다.

권장 수정 파일/영역: `Assets/AddressableAssetsData/AddressableAssetSettings.asset`, `Assets/AddressableAssetsData/link.xml`, `Assets/AddressableAssetsData/link.xml.meta`, link 생성·체크인 소유권을 정하는 Editor build hook. 이번 감사에서는 수정하지 않았다.

### W4-AR-002 — P1 — 구조물 4종의 Unity pivot이 채택 metadata와 불일치

재현:

1. `ParallelQA.Wave4AssetReleaseGate.RunAssetContracts`를 실행한다.
2. currentJobId `job_20260822130400_6d786a69`의 `camp-structures-metadata.json` pivot과 Unity `TextureImporter.spritePivot`을 비교한다.

예상/실제:

| 구조물 | metadata 예상 | Unity 실제 |
| --- | ---: | ---: |
| campfire | `(0.5,0.07494)` | `(0.5,0.5)` |
| workbench | `(0.5,0.09846)` | `(0.5,0.5)` |
| rain_collector | `(0.5,0.05112)` | `(0.5,0.5)` |
| rescue_signal | `(0.5,0.0401)` | `(0.5,0.5)` |

영향: 바닥 스냅, 배치 유령과 확정 오브젝트 정렬, 콜라이더/상호작용 위치가 시각 자산과 어긋날 수 있다.

권장 수정 파일: current structure package의 `campfire.png.meta`, `workbench.png.meta`, `rain_collector.png.meta`, `rescue_signal.png.meta`. 이번 감사에서는 아트/메타를 수정하지 않았다.

### W4-AR-003 — P1 — 최신 채택 배경·구조물 패키지가 Windows 빌드 입력에서 도달 불가

재현:

1. `ParallelQA.Wave4AssetReleaseGate.RunAssetContracts`를 실행한다.
2. enabled build scene의 재귀 dependency와 Addressables group의 GUID를 current manifest source GUID와 대조한다.
3. `build_reachability.background.island-camp`와 `build_reachability.object.camp-structures`를 확인한다.

예상: 각 currentJobId 패키지에서 최소 한 소스가 enabled build scene 또는 Addressables를 통해 빌드에 포함된다.

실제: 최신 배경 3개, 구조물 5개 모두 도달 가능한 소스가 0개다. `PrototypeProjectBuilder.CampBackgroundPath`도 이전 `job_20260822115849_82b3b250`을 가리킨다.

영향: `.forge/assets.json`이 engine-ready로 선언한 최신 정리 패키지가 실제 Windows 결과물에 포함되지 않거나, 이전 배경/placeholder 구조물이 계속 표시될 수 있다.

권장 수정 파일/영역: `Assets/_Project/Scripts/Editor/PrototypeProjectBuilder.cs`, `Assets/_Project/Scenes/KimSurvivalPrototype.unity`, 필요한 경우 `Assets/AddressableAssetsData/AssetGroups/Default Local Group.asset`. 이번 감사에서는 런타임·씬·Addressables group을 수정하지 않았다.

### W4-AR-004 — P2 검증 공백 — 물리 게임패드 미실행

재현: 이번 run의 `asset-contracts.json`에서 `input.physical_gamepad`와 `wave4-release-summary.json`의 `physicalGamepad`를 확인한다.

예상: 실제 Windows PC에서 물리 게임패드로 이동·상호작용·배치·언어 설정 경로를 사람이 조작하고 장치/결과를 기록한다.

실제: 배치 모드에서 비어 있지 않은 joystick name이 없으며 물리 조작을 수행하지 않았다. 상태는 PASS가 아니라 **UNVERIFIED**다.

권장 영역: 출시 후보 체크리스트와 물리 장치 테스트 증거. 자동화 결과로 승격하지 않는다.

## 기존 기준선 사실과 이번 결과의 구분

Wave 3의 1280×800 시각 게이트는 이번 범위에서 재조정하거나 수정하지 않았다. preflight가 `Wave3VisualGate.cs`와 기존 결과 파일의 SHA-256을 기록했고, 기존 결과의 `OVERALL: FAIL` 및 임계값 문자열을 보존했다. 이 기존 P1은 이번 Wave 4에서 새로 발생한 자산 도달성/피벗/Addressables P1과 별개다.

## 재현 명령

아래 명령은 프로젝트 루트의 PowerShell에서 실행한다. 같은 run ID 디렉터리를 재사용하지 말고 새 값을 사용한다.

```powershell
$runId = '<new-utc-run-id>_fed5066_wave4'
& 'Assets/Editor/ParallelQA/Capture-Wave4Preflight.ps1' -RunId $runId -BaselineCommit 'fed50669a37a66aec436c2174445e5107b74d57c'
$env:KIM_PARALLEL_QA_RUN_ID = $runId
$env:KIM_PARALLEL_QA_BASELINE = 'fed50669a37a66aec436c2174445e5107b74d57c'
& 'C:\Program Files\Unity\Hub\Editor\6000.4.9f1\Editor\Unity.exe' -batchmode -nographics -quit -projectPath (Get-Location).Path -executeMethod ParallelQA.ParallelQaRunner.RecordCompilePass -logFile "work/ParallelQA/$runId/unity-compile.log"
& 'C:\Program Files\Unity\Hub\Editor\6000.4.9f1\Editor\Unity.exe' -batchmode -nographics -quit -projectPath (Get-Location).Path -executeMethod ParallelQA.Wave4AssetReleaseGate.RunAssetContracts -logFile "work/ParallelQA/$runId/unity-asset-contracts.log"
& 'C:\Program Files\Unity\Hub\Editor\6000.4.9f1\Editor\Unity.exe' -batchmode -nographics -quit -projectPath (Get-Location).Path -executeMethod ParallelQA.Wave4AssetReleaseGate.BuildWindowsDevelopmentPlayer -logFile "work/ParallelQA/$runId/unity-windows-development-build.log"
& 'Assets/Editor/ParallelQA/Invoke-Wave4WindowsSmoke.ps1' -RunId $runId -BaselineCommit $env:KIM_PARALLEL_QA_BASELINE -MinimumSeconds 6
```

계약 method는 결함을 JSON/TXT로 먼저 기록한 뒤 FAIL이면 비영(非零) 종료한다. 따라서 위 기준선에서 자산 계약과 Addressables 계약의 비영 종료는 숨긴 오류가 아니라 재현된 게이트 실패다.

## 증거 경로

- 실행 요약: `Artifacts/ParallelQA/20260822T141021Z_fed5066_wave4/wave4-release-summary.json`
- 프리플라이트: `Artifacts/ParallelQA/20260822T141021Z_fed5066_wave4/wave4-preflight.json`
- Unity 컴파일: `Artifacts/ParallelQA/20260822T141021Z_fed5066_wave4/compile-result.txt`
- 자산/importer 계약: `Artifacts/ParallelQA/20260822T141021Z_fed5066_wave4/asset-contracts.json`
- 자산 해시: `Artifacts/ParallelQA/20260822T141021Z_fed5066_wave4/asset-files.sha256`
- Addressables 빌드 전후: `Artifacts/ParallelQA/20260822T141021Z_fed5066_wave4/addressables-link-build-contract.json`
- Windows Development build: `Artifacts/ParallelQA/20260822T141021Z_fed5066_wave4/windows-development-build.json`
- 숨김 실행 스모크: `Artifacts/ParallelQA/20260822T141021Z_fed5066_wave4/windows-hidden-smoke.json`
- Steam 준비표: `Artifacts/ParallelQA/20260822T141021Z_fed5066_wave4/steam-readiness.json`
- 보존한 기존 픽셀 게이트: `Artifacts/ParallelQA/20260822T130505Z_c1b18a6_wave3/wave3-visual-gate.txt`

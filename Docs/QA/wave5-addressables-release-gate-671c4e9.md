# Wave 5 Addressables·PC/Steam 릴리스 게이트 — 671c4e9

## 전체 판정

- **QA/빌드 인프라 게이트: PASS**
- **릴리스 전체: FAIL / Steam 출시 준비: NOT READY**
- 기준선: `origin/master`의 `671c4e9df8c144a22421e9eeab6693617aa2b3b1`
- 브랜치: `codex/wave5-addressables-release-gate`
- 독립 실행 ID: `20260822T225717Z_671c4e9_wave5_verified`
- Unity: `6000.4.9f1`
- 실행 시각: 2026-08-22 22:57:27Z–23:00:50Z
- 물리 게임패드: **UNVERIFIED** — Unity 배치 모드에 노출된 장치가 없고 사람의 실제 장치 조작을 수행하지 않았다.
- 범위 통제: `Assets/Editor/ParallelQA/**`, `Assets/AddressableAssetsData/**`, `Artifacts/ParallelQA/**`, 이 문서만 변경했다. 런타임 코드·씬·아트/Forge 원장·게임 밸런스·Wave 3 시각 임계값은 수정하지 않았다.

Addressables `link.xml` 소유권 문제는 해결됐다. 에디터 로드, Windows Development build, 숨김 Player 스모크 후의 내구 상태는 모두 **파일 부재 / SHA-256 빈 값 / meta GUID 빈 값**으로 동일하다. Windows 빌드는 오류·경고 `0/0`으로 성공했고 숨김 1280×800 실행도 6.383초 동안 살아 있고 응답했다. 릴리스 FAIL은 Addressables나 빌드 인프라가 아니라 qps-long 10개 중 8개 실패, 병렬 구현 대상인 최신 배경 reachability 실패, Steamworks 미구성에 의해 유지된다.

## 원인과 수정

설치된 `com.unity.addressables@2.9.1`의 `AddressablesPlayerBuildProcessor`는 생성된 `Library/.../AddressablesLink/link.xml`을 Player 빌드 직전에 `AddressableAssetSettings.ConfigFolder`로 복사하고, 에디터 로드 시 임시 복사본을 제거한다. Unity의 공식 Addressables 문서도 생성 link 파일을 빌드 산출물로 설명하고, Unity 2021.2 이상에서는 빌드 중 `ConfigFolder`로 임시 복사한다고 명시한다.

671c4e9가 추적하던 `Assets/AddressableAssetsData/link.xml`은 현재 생성본과 26개 내용 행이 모두 같아 별도 사용자 linker 선언이 아니었다. `.meta`까지 소스처럼 추적한 것이 Unity 로드 시 삭제와 빌드별 GUID 재생성을 변경으로 노출한 직접 원인이다. 또한 `AssetDatabase.AssetPathToGUID(path)` 기본 조회는 최근 삭제된 에셋을 포함하므로, 삭제 직후 물리 파일이 없어도 캐시 GUID가 남아 있는 것처럼 관찰됐다.

수정은 다음으로 제한했다.

1. 생성 임시물인 `Assets/AddressableAssetsData/link.xml`과 `.meta`를 추적 대상에서 제거하고 동일 경로를 전용 `.gitignore`에 등록했다.
2. 마지막 Player build postprocessor 뒤에 ConfigFolder 임시 복사본만 제거하고, 생성 시점의 SHA-256/GUID와 제거 후 상태를 JSON으로 기록하는 QA Editor hook을 추가했다.
3. 게이트의 AssetDatabase 검사는 `OnlyExistingAssets` 옵션을 사용해 실제 존재 파일만 판정한다.
4. preflight→에디터 로드→빌드 전/후→post-smoke를 canonical `ABSENT` 상태로 비교한다. Library의 Addressables 생성물이나 라이선스/캐시, 바이너리는 삭제·교체하지 않는다.

근거: [Addressables 빌드 산출물](https://docs.unity3d.com/kr/Packages/com.unity.addressables%401.21/manual/build-artifacts-included.html), [Addressables CI 빌드](https://docs.unity3d.com/kr/Packages/com.unity.addressables%401.21/manual/ContinuousIntegration.html), `addressables-ownership-research.txt`.

## 검증 행렬

| 항목 | 결과 | 독립 관찰 |
| --- | --- | --- |
| git 기준선/신규 브랜치 | PASS | fetch 후 요청한 40자 커밋과 일치하는 `origin/master`에서 새 브랜치 생성 |
| Unity 스크립트 컴파일 | PASS | compiler errors 0, warnings 0 |
| 결정적 Edit Check | PASS | 생존, 수영, 배치, 입력, ko/en, Smart String·폰트, 전용 앵커 9개 경로 PASS |
| KO/EN 3일 전체 루프 | PASS | 각 언어에서 캠프/수색/복귀/정착/구조, 연안 입수·물 채집·출수 완료 |
| 제한적 자유 배치 | PASS | 유효·무효, 경계·겹침·출입구·필수 통로, 전용 앵커, 취소·1회 차감·무료 재배치 계약 PASS |
| 자동 키보드·마우스/게임패드 경로 | PASS | raw action 수렴, EventSystem Submit, 방향 탐색과 프롬프트 경로 자동 실행 |
| 물리 게임패드 | **UNVERIFIED (P2 gap)** | 비어 있지 않은 joystick name 0; 사람의 실제 actuation 없음 |
| 1280×800 KO/EN 일반 배치 | PASS | 24 targets, 0 failures |
| 1280×800 탐색·수영 | PASS | 10 targets, 0 failures |
| 1280×800 qps-long | **FAIL (P1)** | 10 targets 중 8 failures; 화면 경계·높이·overflow 문제를 현재 시스템 작업 입력으로 보존 |
| Addressables 에디터 로드 안정성 | PASS | preflight와 계약 실행 모두 canonical ABSENT, SHA/GUID 빈 값 |
| Addressables 빌드 안정성 | PASS | preflight→before와 before→after 동일; 임시 복사본 정리 PASS |
| Addressables post-smoke 안정성 | PASS | preflight와 post-smoke 모두 canonical ABSENT |
| Windows x64 Development build | PASS | `Succeeded`, errors 0, warnings 0, total 165,403,744 bytes |
| Windows 숨김 스모크 | PASS | 1280×800 windowed/hidden, 요구 6초에 6.383초 alive/responding |
| 채택 자산/importer 계약 | PASS | engine-ready ledger, PNG/manifest/quality, 배경·구조물 importer와 구조물 pivot 계약 통과 |
| 최신 배경 빌드 reachability | **FAIL (P1)** | currentJobId 배경 소스 3개 중 scene/Addressables 도달 0; 병렬 구현 브랜치 대상 |
| Steamworks SDK/App ID/Depot/Input/Cloud/Achievements | NOT READY | 표적 저장소 검사에서 구성 근거 없음; 추정으로 PASS 처리하지 않음 |

자산·릴리스 계약의 기계 판독 집계는 **205 PASS / 2 FAIL / 1 UNVERIFIED**다. `RunAssetContracts`는 이 두 P1을 JSON/TXT로 쓴 뒤 비영 종료하는 것이 정상이며, 후속 Windows build와 smoke 결과와 혼동하지 않는다.

## 잔여 결함과 검증 공백

### W5-QA-001 — P1 — qps-long 배치 화면 10개 중 8개 실패

재현:

1. 새 run ID로 `ParallelQA.ParallelQaRunner.RunPlayModeVerification`을 실행한다.
2. `playmode-qps-long-placement-1280x800.png`와 `wave3-visual-gate.txt`를 확인한다.
3. `PSEUDO_LONG_GATE: FAIL · targets=10 · failures=8` 및 개별 height/bounds/overflow 실패를 확인한다.

예상: 142% 장문 의사 로케일의 10개 대상이 최소 픽셀 높이, 4px 화면 여백, overflow·겹침·대비 계약을 모두 통과한다.

실제/영향: 상단 상태·자원, 입력 안내, 언어 전환, 배치 카드와 월드 배지가 화면 밖으로 나가거나 너무 작고 일부 카드가 넘친다. 향후 영어보다 긴 로케일에서 조작 안내와 배치 상태를 읽지 못할 위험이 있다.

권장 수정 파일: `Assets/_Project/Scripts/Runtime/KimSurvivalPrototype.cs`, `Assets/_Project/Scripts/Localization/PrototypeStrings.tsv` 및 생성되는 String Table. 이번 QA 브랜치에서는 런타임 레이아웃을 수정하지 않았다.

### W5-QA-002 — P1 — 최신 배경 currentJobId가 Windows 빌드 입력에서 도달 불가

재현:

1. `ParallelQA.Wave4AssetReleaseGate.RunAssetContracts`를 실행한다.
2. `asset-contracts.json`의 `build_reachability.background.island-camp`를 확인한다.
3. enabled scene 재귀 dependency와 Addressables GUID를 current manifest의 배경 PNG 3개와 비교한다.

예상: 최신 engine-ready 배경 패키지에서 하나 이상의 소스가 enabled scene 또는 Addressables를 통해 빌드에 포함된다.

실제/영향: currentJobId 배경 3개 모두 도달 가능한 소스가 0개다. 채택 원장과 실제 Windows 표현이 갈라질 수 있다.

권장 수정 파일/영역: `Assets/_Project/Scripts/Editor/PrototypeProjectBuilder.cs`, `Assets/_Project/Scenes/KimSurvivalPrototype.unity`, 필요 시 Addressables group. 이 항목은 병렬 구현 브랜치가 수정 중이므로 이번 브랜치는 사실만 기록했다.

### W5-QA-003 — P2 검증 공백 — 물리 게임패드 미실행

재현: `playmode-full-loop.txt`, `asset-contracts.json`, `wave5-release-summary.json`에서 joystick name 0과 `UNVERIFIED`를 확인한다.

예상: 실제 Windows PC에서 물리 게임패드로 이동·상호작용·배치 이동/확정/취소·언어 전환을 사람이 조작하고 장치명과 결과를 기록한다.

실제: 자동화와 코드 경로는 PASS지만 물리 장치 actuation은 없었다. 자동화 결과로 상태를 승격하지 않는다.

### W5-QA-004 — P1 출시 차단 — Steam 구성 근거 없음

저장소 표적 검사에서 Steamworks SDK/API, App ID, Depot/upload, Steam Input, Cloud, Achievements의 구성 근거가 발견되지 않았다. 각 항목은 `NOT_READY`이며 실제 통합·배포·스토어 변경은 이번 범위에서 수행하지 않았다.

### W5-QA-005 — P3 도구 노이즈 — Play 종료 시 Unity Search 인덱서 예외

`unity-play-mode-verification.log`에서 모든 Play 증거를 쓴 뒤 에디터가 종료되는 동안 `UnityEditor.Search.SearchDatabase`의 `ArgumentOutOfRangeException`이 한 번 기록됐다. 동일 현상은 과거 독립 QA에도 기록됐고 이번 Windows Player 로그에는 재현되지 않았으며 Play 검증 산출물과 프로세스 종료는 정상이다. 제품 결함으로 승격하지 않지만, 향후 Unity/Search 패키지 또는 로컬 Search 인덱스 환경을 별도 점검해 자동화 로그 노이즈를 제거할 것을 권장한다. 런타임 코드는 수정하지 않았다.

## 재현 명령

프로젝트 루트 PowerShell에서 매번 새 run ID를 사용하고 `Docs/QA/unity-codex-sandbox-licensing.md`에 따라 Unity Editor/build와 Windows Player를 반드시 Codex 샌드박스 밖에서 실행한다. `-noUpm`은 사용하지 않는다.

```powershell
$runId = '<new-utc-run-id>_671c4e9_wave5'
$baseline = '671c4e9df8c144a22421e9eeab6693617aa2b3b1'
& 'Assets/Editor/ParallelQA/Capture-Wave5Preflight.ps1' -RunId $runId -BaselineCommit $baseline
$env:KIM_PARALLEL_QA_RUN_ID = $runId
$env:KIM_PARALLEL_QA_BASELINE = $baseline
$unity = 'C:\Program Files\Unity\Hub\Editor\6000.4.9f1\Editor\Unity.exe'
& $unity -batchmode -nographics -quit -projectPath (Get-Location).Path -executeMethod ParallelQA.ParallelQaRunner.RecordCompilePass -logFile "Artifacts/ParallelQA/$runId/unity-compile.log"
& $unity -batchmode -nographics -quit -projectPath (Get-Location).Path -executeMethod ParallelQA.ParallelQaRunner.RunEditChecks -logFile "Artifacts/ParallelQA/$runId/unity-edit-check.log"
& $unity -batchmode -force-d3d11 -projectPath (Get-Location).Path -executeMethod ParallelQA.ParallelQaRunner.RunPlayModeVerification -logFile "Artifacts/ParallelQA/$runId/unity-play-mode.log"
& $unity -batchmode -nographics -quit -projectPath (Get-Location).Path -executeMethod ParallelQA.Wave4AssetReleaseGate.RunAssetContracts -logFile "Artifacts/ParallelQA/$runId/unity-asset-contracts.log"
& $unity -batchmode -nographics -quit -projectPath (Get-Location).Path -executeMethod ParallelQA.Wave4AssetReleaseGate.BuildWindowsDevelopmentPlayer -logFile "Artifacts/ParallelQA/$runId/unity-windows-build.log"
& 'Assets/Editor/ParallelQA/Invoke-Wave5WindowsSmoke.ps1' -RunId $runId -BaselineCommit $baseline -MinimumSeconds 6
```

## 증거 경로

- 실행 요약: `Artifacts/ParallelQA/20260822T225717Z_671c4e9_wave5_verified/wave5-release-summary.json`
- preflight/소유권 정책: `Artifacts/ParallelQA/20260822T225717Z_671c4e9_wave5_verified/wave5-preflight.json`
- 원인 연구: `Artifacts/ParallelQA/20260822T225717Z_671c4e9_wave5_verified/addressables-ownership-research.txt`
- 컴파일/Edit/Play: `compile-result.txt`, `edit-checks.txt`, `playmode-full-loop.txt`
- 현재 1280×800 사실: `wave5-current-visual-facts.json`, `wave3-visual-gate.txt`, `playmode-*-1280x800.png`
- Addressables 임시물 정리: `addressables-generated-link-cleanup.json`
- Addressables build/post-smoke: `addressables-link-build-contract.json`, `addressables-link-post-smoke-contract.json`
- 에셋 계약: `asset-contracts.json`, `asset-files.sha256`
- Windows build/smoke: `windows-development-build.json`, `windows-hidden-smoke.json`, `windows-player.log`
- Steam 준비표: `steam-readiness.json`

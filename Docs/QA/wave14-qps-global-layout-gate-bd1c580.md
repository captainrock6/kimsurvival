# Wave 14 qps-long 전역 레이아웃 RED-first 게이트

## 판정

- 기준: `origin/master` `bd1c580bb53bdd662877efd1600c97500057c3ee`
- 브랜치: `codex/wave14-qps-global-layout-gate`
- 실행: `20260823T153000Z_bd1c580_wave14`, Unity `6000.4.9f1`, Windows PowerShell `5.1.26100.9168`
- 전체 / 제품 / 인프라: **RED / RED_EXPECTED_GAP / PASS**
- qps-long 1280×800: **4 PASS / 6 EXPECTED_GAP / 0 unexpected FAIL**
- ko/en 잠금: 배치 `24/24 PASS`, 수색·수영 `10/10 PASS`
- 컴파일 `0 errors / 0 warnings`, Windows x64 Development build `PASS`, 숨김 1280×800 smoke `6.327s PASS`, Addressables load/build/post-smoke `PASS`
- 물리 게임패드: **UNVERIFIED**. 합성 입력이나 장치 이름 탐지를 사람 실기로 간주하지 않는다.
- Steam: **NOT_READY**. 이 게이트는 App ID/Depot/Input/Cloud/Achievements/권한/스토어 증거를 만들거나 추정하지 않는다.

제품 런타임·씬·현지화 표·아트는 수정하지 않았다. 변경은 독립 QA 코드, 진입 스크립트, 문서와 새 실행 증거뿐이다.

## 좌표 계측 교정

기존 Wave 3 보고의 qps-long `6/10 FAIL`에는 캡처 후 카메라가 에디터 4:3 종횡비로 돌아간 뒤 1280×800 좌표를 투영한 오차가 있었다. 그 결과 언어 버튼 우측이 `1276.2px`로 보고됐지만, 캡처와 같은 1280×800 RenderTexture를 고정한 실제 값은 `[916.9,50.6 → 1170.2,76.2]`로 4px 안전영역을 통과한다.

Wave 14는 캡처 종횡비를 고정하고 현재 `playerRoot` 렌더러 Rect와 전폭 보행 band `x=0.0, w=1280.0`을 함께 기록한다. 교정 결과 기존 언어 버튼 거짓 양성은 PASS가 됐고, 대신 `증축 계획` 배지가 김씨 실루엣의 `5.87%`를 가리는 실제 실패가 추가됐다. 따라서 정확한 현재 판정도 총 **6/10 RED**이지만 실패 구성은 아래와 같다. 과거 수치를 새 실행 증거로 재사용하지 않았다.

## 검증 행렬

| ID | 대상 | 판정 | 실제 수치 / 사유 |
|---|---|---|---|
| W14-QPS-01 | 최소 HUD 날짜·상태 | EXPECTED_GAP · P1 | 중앙 글리프 `15.6px < 16px`; 2/2행, block `50/64px` |
| W14-QPS-02 | 최소 HUD 자원 | EXPECTED_GAP · P1 | 중앙 글리프 `15.6px < 16px`; 2/2행, block `46.6/64px` |
| W14-QPS-03 | 언어 버튼 | PASS | 1/1행, `17.1px`, 실제 우측 `1170.2px`, overflow 0 |
| W14-QPS-04 | 하단 도움말 | PASS | 3/3행, block `78.2/96px`, overflow 0 |
| W14-QPS-05 | 입구 월드 배지 | PASS | 김씨 `2.30%`, 보행 band `12.89%`; 허용 `5%/20%` 이내 |
| W14-QPS-06 | 필수 통로 월드 배지 | PASS | 김씨 `0%`, 보행 band `3.52%`, overflow 0 |
| W14-QPS-07 | 신호대 앵커 배지 | EXPECTED_GAP · P1 | TMP overflow; 일반 바닥 배지와 `23.7%` 텍스트 겹침 |
| W14-QPS-08 | 배치 상태 | EXPECTED_GAP · P1 | TMP overflow; 김씨 실루엣 `28.47% > 5%` 차폐 |
| W14-QPS-09 | 증축 계획 월드 배지 | EXPECTED_GAP · P1 | 김씨 실루엣 `5.87% > 5%` 차폐 |
| W14-QPS-10 | 일반 바닥 월드 배지 | EXPECTED_GAP · P1 | 5/4행, block `166.3/144px`, TMP overflow, 앵커와 `23.7%` 겹침, 보행 band `22.36% > 20%` |

공통 계약은 글리프 `≥16px`, 화면 가장자리 `≥4px`, 일반/대형 대비 `≥4.5:1/3.0:1`, TMP overflow 0, 유의미한 텍스트 겹침 0, 대상별 허용 행·block 높이 이내다. 김씨 차폐는 실루엣 Rect의 `≤5%`, 보행로 차폐는 전폭 camp walking band의 `≤20%`다. 각 수치와 실제 Rect는 `wave14-qps-global-layout-targets.tsv` 및 JSON에 보존한다.

## 결함 재현과 권장 수정 위치

공통 재현:

1. 아래 전체 명령을 새 RunId로 샌드박스 밖에서 실행한다. 기준선에서는 종료 코드 `2`가 의도한 RED다.
2. `playmode-qps-long-placement-valid-1280x800.png`를 1:1로 연다.
3. `wave14-qps-global-layout-targets.tsv`에서 해당 ID의 bounds, line, overflow, player/walking 비율과 failures를 확인한다.

| 심각도 | 결함 | 예상 영향 | 권장 수정 파일 |
|---|---|---|---|
| P1 | 날짜·상태/자원 HUD 글리프 15.6px | 향후 장문 로케일에서 최소 HUD 판독성 저하 | `Assets/_Project/Scripts/Runtime/KimSurvivalPrototype.cs`, `PrototypeLocalization.cs` |
| P1 | 신호 앵커와 일반 바닥 배지 overflow·23.7% 겹침 | 목표/구역 의미 혼동, 월드 읽기 실패 | `Assets/_Project/Scripts/Runtime/KimSurvivalPrototype.cs` |
| P1 | 배치 상태가 김씨 28.47% 차폐 | 배치 중 위치·피드백 동시 확인 방해 | `KimSurvivalPrototype.cs`, `PrototypeCampPlacement.cs` |
| P1 | 증축 계획 배지가 김씨 5.87% 차폐 | 접근 대상과 플레이어 위치 식별 저하 | `Assets/_Project/Scripts/Runtime/KimSurvivalPrototype.cs` |
| P1 | 일반 바닥 배지 5행/166.3px 및 보행 band 22.36% 차폐 | 주요 이동로와 배치 가능 바닥 판단 방해 | `Assets/_Project/Scripts/Runtime/KimSurvivalPrototype.cs` |

제품 수정은 이 QA 브랜치에서 하지 않는다. 후속 Unity 브랜치는 동일 명령에서 10개 대상 모두 PASS, ko/en 잠금 PASS, infrastructure PASS, EXPECTED_GAP/FAIL 0일 때만 GREEN이다.

## 재실행

Windows PowerShell 5.1 또는 PowerShell 7에서 실행할 수 있다. 모든 Unity/빌드/Player 프로세스는 `Docs/QA/unity-codex-sandbox-licensing.md`에 따라 샌드박스 밖에서 실행하며 `-noUpm`을 쓰지 않는다.

```powershell
& '.\Assets\Editor\ParallelQA\Invoke-Wave14QpsGlobalLayoutGate.ps1' `
  -RunId '<NEW_RUN_ID>' `
  -BaselineCommit 'bd1c580bb53bdd662877efd1600c97500057c3ee' `
  -MinimumSmokeSeconds 6
```

종료 코드:

- `0`: GREEN — 인프라 PASS, ko/en 잠금 PASS, qps 10/10 PASS
- `2`: RED_EXPECTED_GAP — 현재 승인된 제품 갭만 남음
- `1`: 예상 밖 제품 회귀 또는 인프라 실패

## 증거

- `Artifacts/ParallelQA/20260823T153000Z_bd1c580_wave14/wave14-qps-global-layout-gate.json`
- `Artifacts/ParallelQA/20260823T153000Z_bd1c580_wave14/wave14-qps-global-layout-targets.tsv`
- `Artifacts/ParallelQA/20260823T153000Z_bd1c580_wave14/playmode-qps-long-placement-valid-1280x800.png`
- `Artifacts/ParallelQA/20260823T153000Z_bd1c580_wave14/wave3-visual-metrics.tsv`
- `Artifacts/ParallelQA/20260823T153000Z_bd1c580_wave14/wave12-summary.json`
- `Artifacts/ParallelQA/20260823T153000Z_bd1c580_wave14/compile-result.txt`
- `Artifacts/ParallelQA/20260823T153000Z_bd1c580_wave14/windows-development-build.json`
- `Artifacts/ParallelQA/20260823T153000Z_bd1c580_wave14/windows-hidden-smoke.json`
- `Artifacts/ParallelQA/20260823T153000Z_bd1c580_wave14/wave12-powershell-compatibility.json`

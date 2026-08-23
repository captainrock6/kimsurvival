# Wave 6 진행·장벽 독립 회귀 계약 — 473c082

> 통합 결과: `642a73c4e2fe8c236fb40b6d07288b933b020fb0`에서 제품·인프라·릴리스 회귀가 모두 PASS로 전환됐다. 최종 증거는 `Artifacts/ParallelQA/20260823T004700Z_642a73c_wave6_integrated/`에 있으며, 아래 본문은 QA 브랜치가 결함을 처음 검출한 red baseline 기록으로 보존한다. 물리 게임패드는 UNVERIFIED, Steamworks는 NOT_READY다.

## 전체 판정

- **제품 계약: FAIL (red baseline)**
- **테스트 인프라: PASS**
- **기존 릴리스 회귀 게이트: PASS**
- 기준선: `origin/master`의 `473c0824096d02589c46dce92cb1d2264dfb23a4`
- 브랜치: `codex/wave6-progression-regression`
- 독립 실행 ID: `20260822T235713Z_473c082_wave6_progression_verified`
- Unity: `6000.4.9f1`
- 실행 시각: 2026-08-22 23:57:13Z–23:58:19Z
- 물리 게임패드: **UNVERIFIED** — 비어 있지 않은 장치명이 없고 사람의 실제 조작을 수행하지 않았다.
- Steam: **NOT READY** — SDK/API, App ID, Depot, Input, Cloud, Achievements 구성 근거가 없다.
- 범위 통제: 새 `Assets/Editor/ParallelQA` 러너/스크립트, 이 문서, 새 `Artifacts/ParallelQA` 증거만 추가했다. Runtime, Localization Tables, 씬, 아트, `Docs/Design`, `.forge` 원장은 수정하지 않았다.

한 명령 실행은 컴파일, Wave 6 Edit/Play red-first 계약, 기존 KO/EN 전체 Play, qps-long, Addressables, Windows Development build, 숨김 Player smoke를 순서대로 실행한다. 예상 제품 FAIL 때문에 Wave 6 Edit/Play Unity 프로세스는 각각 exit 1이지만, JSON을 먼저 썼고 `infrastructureOverall=PASS`다. 최종 집계도 제품 FAIL과 테스트 인프라 FAIL을 별도 필드와 종료 메시지로 구분한다.

## 필수 매트릭스

| # | 계약 | 결과 | 기준선 관찰 |
| --- | --- | --- | --- |
| 1 | 작업대 + 나무2 + 표류물2, 밧줄 없음 → 신호 1단계 | PASS | stage `0→1`, 자원 각 2 정확히 차감, 밧줄 불필요 |
| 2 | 1단계 + 나무2 + 표류물2, 밧줄 없음 → 2단계 거부/밧줄 피드백 | PASS | stage 1 유지, 자원 불변, `message.signal.rope` |
| 3 | 밧줄 + 나무2 + 표류물2 → 2단계/구조 | PASS | stage 2, `Rescued`, `Result` phase |
| 4 | 작업대/나무/표류물 각각 부족 원인 | PASS 3/3 | KO/EN `LastMessage`가 각 부족 개념을 포함 |
| 5 | 밧줄만 있고 돌도끼 없음 → 장벽 통과 불가 | **FAIL P0** | 밧줄만으로 x `7.7→11.9`, blocked notice 없음 |
| 6 | 돌도끼만 보유 → 장벽 통과 | **FAIL P0** | 돌도끼가 있어도 x `8.0`에 clamp, blocked notice 발생 |
| 7 | 돌도끼 채집 +1 및 2배 오인 문구 없음 | **부분 FAIL** | 수량은 base 1→2로 정확히 +1 PASS; KO “두 배”/EN “twice” 문구 P1 FAIL |
| 8 | KO/EN 신호·장벽 의미와 1280×800 가독성 | **FAIL** | 신호 의미는 동일하지만 일부 12–14px; 장벽은 KO “밧줄 필요”/EN “Rope Required”이고 캡처에 요구 라벨이 보이지 않음 |
| 9 | F0/H70/일일 -35와 Day 1–3 자연 결과 | **부분 FAIL** | 실제 F1/H75/-25 P0 FAIL; grant/warp 없는 3일 구조, 탈진, 기한 실패는 PASS |
| 10 | Addressables/qps-long/Windows build/smoke 회귀 없음 | PASS | qps-long 10/10, Addressables load/build/post-smoke PASS, Windows build/smoke PASS |

추가 기존 게이트는 일반 KO/EN 배치 **24/24 PASS**, 탐색·수영 **10/10 PASS**, qps-long **10/10 PASS**다. 자산·릴리스 계약 집계는 **244 PASS / 0 FAIL / 1 UNVERIFIED**로 배경 reachability를 포함해 모두 회귀가 없었다.

## 주요 제품 결함

### W6-PROG-001 — P0 — 장벽 해제 조건이 돌도끼가 아니라 밧줄에 연결됨

재현:

1. 밧줄만 제작한 세션과 돌도끼만 제작한 세션을 각각 만든다.
2. 수색을 시작하고 x=7.7에서 오른쪽 이동 step을 적용한다.
3. `PrototypePlayerTraversal.X`와 `ReachedBlockedPath`를 비교한다.
4. KO/EN 장벽 캡처의 월드 문구도 확인한다.

예상: 밧줄만 있으면 x≤8에 막히고, 돌도끼만 있으면 x>8로 통과한다. 장벽은 KO/EN 모두 돌도끼 필요를 말한다.

실제: 밧줄만으로 x=11.9까지 통과하고 돌도끼만 있으면 x=8.0에 막힌다. 화면 문구도 “밧줄 필요”/“Rope Required”다.

영향: 도구 진행의 원인과 효과가 뒤집혀 플레이어가 잘못된 도구를 제작하며, 자연 3일 계획과 자원 투자가 왜곡된다.

권장 수정 파일: `Assets/_Project/Scripts/Runtime/PrototypePlayerTraversal.cs`, `Assets/_Project/Scripts/Runtime/KimSurvivalPrototype.cs`, 실제 구현 브랜치의 Localization Tables/TSV. 이번 QA 브랜치는 수정하지 않았다.

### W6-PROG-002 — P0 — 목표 생존 밸런스 F0/H70/-35가 기준선에 없음

재현: 새 `GameSession`의 Food/Hunger를 읽고 빈 수색 후 `EndDay` 전후 Hunger를 비교한다.

예상: Food 0, Hunger 70, 일일 Hunger 감소 35.

실제: Food 1, Hunger 75, 일일 감소 25.

영향: 승인된 자연 경로의 생존 압박과 탈진 시점이 다른 밸런스로 실행된다.

권장 수정 파일: `Assets/_Project/Scripts/Runtime/GameSession.cs`. 이번 브랜치는 Runtime을 수정하지 않았다.

### W6-PROG-003 — P1 — 돌도끼 +1 효과를 2배로 오해할 문구

재현: KO/EN의 돌도끼 연구·제작·채집 플레이어 문구를 금칙어 `두 배`, `2배`, `twice`, `double`, `2x`로 검사한다.

예상: 정확한 “나무 +1” 효과만 전달한다.

실제: 채집 계산과 채집 피드백은 +1이지만 제작 완료 문구가 KO “나무가 두 배로…”, EN “twice…”를 사용한다.

권장 수정 파일: `Assets/_Project/Scripts/Localization/PrototypeStrings.tsv`와 생성 String Tables. QA 브랜치에서는 변경하지 않았다.

### W6-PROG-004 — P1 — 신호 2단계 요구조건 일부가 1280×800 최소 18px 미달

재현: `wave6-ko/en-signal-stage2-1280x800.png`와 `wave6-play-contracts.json`의 projected TMP metric을 확인한다.

실제: KO 신호 버튼 14.0px, EN 밧줄 피드백 11.6px, EN 신호 버튼 12.0px다. 화면 경계·대비·overflow는 통과하지만 최소 18px 기준을 충족하지 못한다.

권장 수정 파일: `Assets/_Project/Scripts/Runtime/KimSurvivalPrototype.cs`. 이번 QA 브랜치는 레이아웃을 수정하지 않았다.

### W6-PROG-005 — P1 — 장벽 요구 라벨이 1280×800 캡처에서 판독 불가

재현: 도구 없는 수색 상태에서 카메라를 장벽에 맞추고 `wave6-ko/en-axe-barrier-1280x800.png`를 확인한다.

실제: 런타임 TMP 값은 “밧줄 필요”/“Rope Required”로 존재하지만 projected metric 대상이 0개이며 캡처에서도 장벽 요구 라벨을 읽을 수 없다.

권장 수정 파일: `Assets/_Project/Scripts/Runtime/KimSurvivalPrototype.cs`와 Localization Tables. 의미를 돌도끼로 고친 뒤 동일 18px/경계/대비 게이트를 통과해야 한다.

## 통과한 진행 계약

- 신호 1단계는 밧줄 없이 정확한 재료로 성공한다.
- 신호 2단계는 밧줄이 없으면 자원을 쓰지 않고 거부하며 밧줄 필요 키를 남긴다.
- 밧줄과 정확한 재료가 있으면 2단계와 구조 결과가 성공한다.
- 작업대·나무·표류물 부족 피드백은 KO/EN에서 각 원인을 식별할 수 있다.
- 돌도끼의 실제 나무 획득량은 base 대비 정확히 +1이다.
- 독립 모델 경로는 소스 감사상 `Grant`/`Warp` 호출 없이 Day 3 구조에 도달한다.
- 탈진과 3일 기한 실패 결과가 유지된다.
- 기존 전체 KO/EN Play도 수영·연안 채집·복귀·배치·구조까지 PASS다.

## 릴리스 회귀와 한계

- 컴파일: PASS, compiler errors/warnings `0/0`.
- Addressables: canonical `ABSENT`; preflight→load→build→post-smoke 모두 SHA/GUID 빈 상태로 PASS.
- Windows x64 Development build: `Succeeded`, errors/warnings `0/0`, total 179,574,252 bytes.
- 숨김 Player: 1280×800 windowed, 6.424초 alive/responding, PASS.
- 물리 게임패드: **UNVERIFIED**. 자동 공통 입력 경로 PASS를 하드웨어 PASS로 승격하지 않는다.
- Steam: **NOT READY**. 구성 작업은 수행하지 않았다.
- P3 도구 노이즈: 두 Play Editor 종료 시 기존 `UnityEditor.Search.SearchDatabase` 예외가 1회씩 기록됐으나 Windows Player에는 없고 증거 생성/종료를 막지 않았다. `wave6-tooling-notes.txt`에 분리했다.

## 한 명령 재실행

프로젝트 루트 PowerShell에서 새 run ID와 현재 통합 커밋을 전달한다. Unity/Player 자식 프로세스까지 `Docs/QA/unity-codex-sandbox-licensing.md`에 따라 Codex 샌드박스 밖에서 실행해야 하며 `-noUpm`은 사용하지 않는다.

```powershell
$runId = '<new-utc-run-id>_wave6_progression'
$baseline = (git rev-parse HEAD).Trim()
& 'Assets/Editor/ParallelQA/Invoke-Wave6ProgressionRegression.ps1' -RunId $runId -BaselineCommit $baseline -MinimumSmokeSeconds 6
```

출력의 `PRODUCT`, `INFRASTRUCTURE`, `RELEASE_REGRESSION`을 별도로 읽는다. 제품 계약이 red이면 요약과 개별 계약 JSON을 쓴 뒤 비영 종료하며, Unity 실행·증거 생성 자체가 실패하면 `INFRASTRUCTURE=FAIL`로 분리한다.

## 증거 경로

- 최종 집계: `Artifacts/ParallelQA/20260822T235713Z_473c082_wave6_progression_verified/wave6-summary.json`
- 명령·시각·종료 코드: `wave6-command-results.json`
- 진행 Edit 계약: `wave6-edit-contracts.json`
- KO/EN Play·가독성 계약: `wave6-play-contracts.json`
- KO/EN 신호/장벽 캡처: `wave6-*-signal-stage2-1280x800.png`, `wave6-*-axe-barrier-1280x800.png`
- 기존 전체 Play: `playmode-full-loop.txt`
- 기존 시각 회귀: `wave5-current-visual-facts.json`, `wave3-visual-gate.txt`
- Addressables: `wave5-preflight.json`, `addressables-link-build-contract.json`, `addressables-link-post-smoke-contract.json`
- 자산 계약: `asset-contracts.json`
- Windows build/smoke: `windows-development-build.json`, `windows-hidden-smoke.json`, `windows-player.log`
- Steam: `steam-readiness.json`
- 도구 노이즈: `wave6-tooling-notes.txt`

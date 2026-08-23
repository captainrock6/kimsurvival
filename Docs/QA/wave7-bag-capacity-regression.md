# Wave 7 가방 확장 독립 회귀 게이트

## 목적과 기준

- 정본: `Docs/Design/wave7-bag-capacity-upgrade.md`
- Forge 검증 항목: `task.qa.feature.inventory-capacity-upgrade`
- 최초 red-first 기준: `origin/master` `19f050a69759e5715d1f8a2eaa72fade72164b4b`
- Unity: `6000.4.9f1`
- 소유 범위: `Assets/Editor/ParallelQA/**`, `Docs/QA/**`, `Artifacts/ParallelQA/<run-id>/**`

이 게이트는 런타임 구현을 고치지 않는다. `GameSession`에서 인스턴스별 활성 가방 용량과 가방 업그레이드 동작을 이름이 아니라 `bag + capacity/slots`, `bag + upgrade` 의미로 반사 탐색한다. 구현이 없는 기준에서도 QA 어셈블리는 컴파일되며, 구현 종속 행렬은 `PASS` 대신 `NOT_IMPLEMENTED · PRODUCT_GAP`으로 기록된다.

## 판정 어휘

| 상태 | 의미 |
| --- | --- |
| `PASS` | 새 실행의 관측값이 계약을 충족 |
| `FAIL` | 구현 표면은 있으나 관측값이 계약과 다름 |
| `NOT_IMPLEMENTED` | 계약을 실행할 제품 표면이 없음. PASS로 간주하지 않음 |
| `UNVERIFIED` | 자동화로 대체할 수 없는 물리 장치·권한 검증 미실행 |
| `INFRA_FAIL` | Unity/러너/증거 생성 자체가 완료되지 않음 |

`NOT_IMPLEMENTED`와 `FAIL`은 제품 판정을 FAIL로 만든다. 물리 게임패드는 합성 입력이 PASS여도 별도의 사람 실기 증거가 없으면 항상 `UNVERIFIED`다.

## 필수 매트릭스

| ID | 우선순위 | 자동 검증 |
| --- | ---: | --- |
| W7-01 | P0 | 새 `GameSession`의 활성 용량 4, `StackLimit` 2 |
| W7-02 | P0 | 나무 2·표류물 1을 보유해도 작업대 없이는 실패, 용량·자원 불변 |
| W7-03a | P0 | 작업대가 있어도 비용 부족 시 실패, 용량·자원 불변 |
| W7-03b | P0 | 업그레이드 시작 뒤 취소 시 용량·자원 불변. 시작/취소 표면이 없으면 `NOT_IMPLEMENTED` |
| W7-04 | P0 | 작업대 + 나무 2 + 표류물 1에서 한 번만 차감하고 정확히 6칸 |
| W7-05 | P0 | 두 번째 시도 실패, 추가 차감·상태 변경 없음 |
| W7-06 | P0 | 날짜 전환 후 6칸 유지, `Reset`/새 게임은 4칸 |
| W7-07a | P0 | 업그레이드 전 가방 만석에서 슬롯 인덱스 4·5 교체 거부 |
| W7-07b | P0 | 슬롯 5·6 획득·중첩·교체·포기·귀환 이전 및 가방 비우기 |
| W7-08 | P1 | 행동에서 실제로 나온 키의 ko/en 의미, 키 존재, placeholder 집합, 42% qps-long 확장 보존 |
| W7-08U | P1 | 1280×800·1920×1080의 4칸/6칸 ko/en 및 qps-long 픽셀 게이트 |
| W7-09A | P1 | 키보드와 합성 게임패드가 슬롯 6, 확인, 취소를 같은 액션으로 전달 |
| W7-09U | P1 | 포인터 클릭 슬롯 5와 EventSystem 합성 Submit 슬롯 6의 동일 교체 결과 |
| W7-HW | P1 | 실제 패드로 구매·슬롯 5/6·교체·포기·귀환·구조. 실기 없으면 `UNVERIFIED` |
| W7-10 | P0/P1 | Wave 6 신호/돌도끼/장벽, 배치 24/24, 탐색·수영 10/10, qps-long 10/10, Addressables, Windows 빌드/스모크 |
| W7-11 | P0 | 러너 경로 본문에 `Grant`/`Warp` 호출 없이 Day 1 작업대·업그레이드, Day 2 도구·신호 1, Day 3 신호 2·구조 |

W7-11은 게임 모델의 정상 `TryGather`·귀환·제작 API만 사용한다. 좌표 이동을 직접 주입하지 않는다. 별도의 실제 플레이어 사용성은 이 자동 계약을 대체할 수 없는 수동 범위다.

## 화면 픽셀 게이트

업그레이드 구현이 발견되면 Play Mode 러너가 다음 원본 PNG를 생성한다.

- ko/en, 4칸 기준 및 6칸 완료 상태, 1280×800·1920×1080
- 영어 가방 관련 텍스트를 결정적으로 약 42% 확장한 qps-long, 두 해상도

각 활성 가방 텍스트와 업그레이드 버튼 문구는 다음을 만족해야 한다.

- 가시 글리프 중앙값 높이 16 px 이상
- 화면 경계 여백 4 px 이상
- TMP overflow 없음
- 24 px 미만 텍스트 대비 4.5:1 이상, 그 이상은 3:1 이상
- 텍스트 블록 간 15% 이상 면적 겹침 없음

구현 전 기준에서는 현재 4칸 ko/en 화면을 두 해상도로 캡처해 증거 생성 경로만 검증하고, 6칸/qps-long 결과는 `NOT_IMPLEMENTED`로 유지한다.

## 한 명령 재실행

Unity와 Windows Player는 `Docs/QA/unity-codex-sandbox-licensing.md`에 따라 Codex 샌드박스 밖에서 실행한다. `-noUpm`은 사용하지 않는다.

```powershell
$runId = (Get-Date).ToUniversalTime().ToString('yyyyMMddTHHmmssZ') + '_wave7_bag'
& '.\Assets\Editor\ParallelQA\Invoke-Wave7BagCapacityRegression.ps1' `
  -RunId $runId `
  -BaselineCommit (git rev-parse HEAD)
```

스크립트는 새 `Artifacts/ParallelQA/<run-id>`만 만들며 같은 run ID를 덮어쓰지 않는다. 단계는 다음 순서다.

1. Addressables 임시 `link.xml` 소유권 preflight
2. Unity 컴파일 진입과 오류/경고 계수
3. Wave 7 Edit/Play red-first 계약과 두 해상도 캡처
4. Wave 6 신호·돌도끼·장벽 Edit/Play 회귀
5. 기존 결정적 Edit Check와 전체 Play Mode·배치·수영·qps-long 시각 회귀
6. 아트/Addressables/Steam 계약 스캔
7. Windows x64 Development build와 숨김 1280×800 6초 스모크

종료 코드는 `0=전체 자동 게이트 녹색`, `2=제품 FAIL/NOT_IMPLEMENTED`, `3=테스트 인프라 실패`다. red-first 기준에서는 정상적으로 종료 코드 2가 예상되며 `wave7-summary.json`의 `infrastructureOverall=PASS`가 함께 있어야 유효한 제품 red다.

## 수동·권한 범위

- 물리 게임패드: 제품명, VID/PID, 연결 방식, Unity joystick 이름과 사람의 실제 입력을 별도 기록하기 전 `UNVERIFIED`
- Steam: Steamworks SDK, App ID, Depot, Input, Cloud, Achievements와 배포 권한의 독립 증거가 모두 없으면 `NOT_READY`; Windows 빌드·합성 패드 PASS로 READY를 주장하지 않음
- 공식 영문 게임 제목: 미정 상태 유지

## 증거 색인

한 실행의 핵심 파일은 다음과 같다.

- `wave7-summary.json`, `wave7-summary.txt`: 전체/red-first/회귀/하드웨어/Steam 판정
- `wave7-edit-contracts.json`, `wave7-play-contracts.json`: 항목별 상태, P등급, 재현, 권장 수정 파일
- `wave7-layout-metrics.json`, `wave7-layout-metrics.txt`, `*.png`: 화면 픽셀·경계·대비·겹침 증거
- `wave7-command-results.json`: Unity 버전, 명령, 단계별 종료 코드와 로그 경로
- `wave6-*.json`, `edit-checks.txt`, `playmode-full-loop.txt`, `wave3-visual-*`: 기존 게임 회귀
- `windows-development-build.json`, `windows-hidden-smoke.json`: Windows 빌드/실행
- `steam-readiness.json`: Steam 구성의 저장소 근거

실행 결과와 실제 run ID는 검증 완료 뒤 이 문서의 다음 절에 추가한다.

## 최신 독립 실행

### 전체 판정

**FAIL · 예상된 red-first 제품 공백 재현. 테스트 인프라와 기존 회귀는 PASS.**

- run ID: `20260823T021548Z_19f050a_wave7_bag_final`
- 관측 시각: `2026-08-23T02:17:33Z`
- 기준/HEAD: `19f050a69759e5715d1f8a2eaa72fade72164b4b`
- Unity: `6000.4.9f1`
- 제품: `FAIL`
- 인프라: `PASS`
- red-first: `EXPECTED_RED_PRODUCT_GAP_REPRODUCED`

### 검증 행렬 결과

| 영역 | 결과 | 새 실행 근거 |
| --- | --- | --- |
| Unity 컴파일 | PASS | 오류 0, 경고 0 |
| W7-01 새 게임 | PASS | 활성 4칸, 중첩 2 |
| W7-02~07, 11 | NOT_IMPLEMENTED | 인스턴스 활성 용량, 원자적 가방 업그레이드, begin/cancel 표면 모두 미발견 |
| W7-08 ko/en/qps-long | NOT_IMPLEMENTED | 업그레이드 행동 키가 없어 의미·placeholder 검사를 실행할 수 없음 |
| W7-08 4칸 화면 | P2 FAIL | 4개 프레임, 20개 텍스트 중 16개가 높이 또는 TMP overflow 기준 실패 |
| W7-08 6칸 화면 | NOT_IMPLEMENTED | 6칸 상태와 업그레이드 UI 없음 |
| W7-09 자동 입력 | NOT_IMPLEMENTED | 슬롯 5·6 및 구매 UI 상태에 도달 불가 |
| 물리 게임패드 | UNVERIFIED | Unity batch Play Mode에서 비어 있지 않은 joystick 이름 0, 사람 실기 없음 |
| Wave 6 신호·장벽·밸런스 | PASS | Edit 15/15, Play 8/8; 물리 패드는 별도 UNVERIFIED |
| 배치 / 탐색·수영 / qps-long | PASS | 24/24 · 10/10 · 10/10 |
| Addressables | PASS | load/build/post-smoke 모두 PASS |
| Windows x64 Development build | PASS | 오류 0, 경고 0, 180,372,374 bytes |
| 숨김 Player 스모크 | PASS | 1280×800, 6.338초 생존·응답 |
| Steam | NOT_READY | SDK/App ID/Depot/Input/Cloud/Achievements 저장소 근거 0, READY 주장 안 함 |

### 발견 사항

| 심각도 | ID | 발견 | 재현 | 권장 수정 파일 |
| ---: | --- | --- | --- | --- |
| P0 | `W7-API.capacity_upgrade` | 현재 기준선에 인스턴스별 활성 용량과 가방 업그레이드 행동이 없다. W7-02~07·11은 개별 PASS가 아니라 `NOT_IMPLEMENTED`다. | 새 실행에서 `RunEditContracts`; JSON의 반사 표면 `capacity/upgrade/begin/cancel=<missing>` 확인 | `GameSession.cs`, `KimSurvivalPrototype.cs` |
| P1 | `W7-08b`, `W7-09` | 6칸 ko/en/qps-long UI와 슬롯 5·6 키보드/합성 패드 조작을 실행할 상태가 없다. | 새 실행에서 `RunPlayContracts`; 6칸 레이아웃·UI submit 항목 확인 | `KimSurvivalPrototype.cs`, `PrototypePlayerInput.cs`, `PrototypeStrings.tsv` |
| P2 | `W7-08a.baseline_4slot_layout` | 현재 4칸 슬롯 글리프가 1280×800에서 KO 15.1 px, EN 11.0 px이고 1920×1080 EN도 14.8 px로 16 px 하한 미달이다. 가방 제목은 네 프레임 모두 TMP overflow를 보고했다. 화면 경계, 대비와 텍스트 간 겹침은 통과했고 원본 육안에서도 경계 잘림은 보이지 않았다. | `baseline-4slot-{ko,en}-{1280x800,1920x1080}.png`를 1:1로 열고 `wave7-layout-metrics.json`의 16개 실패 측정 확인 | `KimSurvivalPrototype.cs` |

기존 신호 단계, 돌도끼/덩굴 장벽, 자연 3일 구조·탈진·기한 실패, 배치, 수영과 장문 현지화에서 새 회귀는 발견되지 않았다. W7-11의 업그레이드 포함 자연 경로는 제품 표면이 생긴 뒤 활성화되며, 러너 경로 본문에는 `Grant`와 `Warp` 호출이 없다.

### 최종 증거와 재실행

- 증거 루트: `Artifacts/ParallelQA/20260823T021548Z_19f050a_wave7_bag_final`
- 전체 요약: `wave7-summary.json`
- 항목별 결과: `wave7-edit-contracts.json`, `wave7-play-contracts.json`
- 픽셀 메트릭/원본: `wave7-layout-metrics.json`, `baseline-4slot-*.png`
- 기존 회귀: `wave6-edit-contracts.json`, `wave6-play-contracts.json`, `wave3-visual-gate.txt`
- 빌드/스모크: `windows-development-build.json`, `windows-hidden-smoke.json`

Unity 구현 브랜치를 합친 뒤에는 병합 결과의 정확한 HEAD를 기준으로 새 run ID를 사용한다.

```powershell
$runId = (Get-Date).ToUniversalTime().ToString('yyyyMMddTHHmmssZ') + '_wave7_bag_after_unity_merge'
& '.\Assets\Editor\ParallelQA\Invoke-Wave7BagCapacityRegression.ps1' `
  -RunId $runId `
  -BaselineCommit (git rev-parse HEAD)
```

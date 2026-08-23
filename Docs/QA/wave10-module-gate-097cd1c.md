# Wave 10 방 확장 독립 RED→GREEN QA 게이트

- 공통 제품 기준: `origin/master` `097cd1cbfa1f434c9836e8393c0b59f18e8d8e09`
- QA 브랜치: `codex/wave10-module-gate`
- 하니스 기준 커밋: `73f4f3621a41272f5052360a519aa46f9d74e79c`
- Unity: `6000.4.9f1`
- 변경 전 RED RunId: `20260823T083100Z_097cd1c_wave10_redfirst`
- 변경 후 GREEN RunId: `20260823T092100Z_73f4f36_wave10_green`
- 제품 런타임·씬·현지화·아트 변경: 없음

## 판정

- 방 확장 계약: **GREEN (8/8 PASS)**
- 인프라: **PASS**
- 전체 제품 게이트: **RED_EXPECTED_FAIL**
- 예상 밖 제품 실패: **0**
- 실제 qps-long: **EXPECTED_FAIL** — 데이터 locale 부재
- 일반 캠프 한국어 TMP overflow: **EXPECTED_FAIL P1** — visible overflow 2
- 물리 게임패드: **UNVERIFIED** — 비어 있지 않은 장치명·사람의 실기 입력 증거 없음
- Steam: **NOT_READY**

전체 판정이 RED인 이유는 방 확장 실패가 아니라 위 두 개의 독립 현지화 공백이다. 합성 장문 캡처는 실제 qps-long PASS를 대신하지 않는다.

## RED→GREEN 비교

| 구분 | 097cd1c 변경 전 | 데이터 기반 하니스 적용 후 | 분류 |
|---|---|---|---|
| 모듈 후보 | 3종 발견 PASS | 위층·옆방·지하실과 방/슬롯/connector 직접 검증 PASS | GREEN |
| 유효·무효 사유 | `hasValidityReason=False`, EXPECTED_FAIL | 각 3종에서 Valid/NoConnectionSlot/Overlap/TerrainBlocked/PathBlocked 직접 평가 PASS | STALE_HARNESS → GREEN |
| 해금·비용 | 이름 기반 cost 발견 PASS | 작업대 전 LOCKED, 작업대 후 W1/D1 SHORT, 정확한 W2/D1 READY PASS | GREEN |
| 원자성·한도 | 전체 회귀 문구에만 포함 | 취소·무효·부족·중복 무차감, 성공 W2/D1 1회 차감, 두 번째 방 PROTOTYPE_LIMIT PASS | GREEN |
| 연결·이동 | 슬롯 이름 발견 PASS | reciprocal pair, Door/Ladder, `room.start→room.upper.standard→room.start`, 시작 방 상태 보존 PASS | GREEN |
| 새 방 일반 바닥 | 전체 회귀 문구에만 포함 | Upper `-2.0`, Side `-2.5`, Basement `-4.0` 일반 설비 배치 PASS | GREEN |
| 입력 | 기존 공통 코드 경로 PASS | keyboard/mouse와 synthetic gamepad cycle/confirm/cancel 결과 동일 PASS | GREEN |
| 모듈 ko/en 1280×800 | 산출물은 있었으나 모듈 판정에 미연결 | KO 위층·실내와 EN 옆방 실제 캡처 1280×800, 모듈 TMP 정책 PASS | GREEN |
| 일반 캠프 KO overflow | `koOverflow=2` EXPECTED_FAIL | `visibleOverflow=2` EXPECTED_FAIL 유지 | PRODUCT P1 |
| 실제 qps-long | 명시적 데이터 판정 없음 | locale absent EXPECTED_FAIL; synthetic-long은 별도 표기 | PRODUCT P1 |
| 인프라 | PASS | PASS | 회귀 없음 |

변경 전 오판의 원인은 `Wave9SpatialCampContractGateRunner.DiscoverModuleContract()`가 타입·멤버 이름에 `validityReason` 같은 토큰이 있는지만 검색한 것이다. 실제 구현은 `CampModuleGeometryStatus`와 `module.geometry.*` 키로 계약을 제공하므로 제품 API를 호출하지 않은 옛 reflection은 이를 놓쳤다.

## 필수 행렬

| ID | 결과 | 실제 결과 |
|---|---|---|
| `W10-M01` 세 후보·유효/무효 사유 | PASS | Upper/Side/Basement 각각 Valid, NoConnectionSlot, Overlap, TerrainBlocked, PathBlocked |
| `W10-M02` LOCKED→SHORT→READY | PASS | pre-workbench Locked; post-workbench W1/D1 Short; exact W2/D1 Ready |
| `W10-M03` 자원 원자성·1개 한도 | PASS | cancel stable, invalid rejected, short rejected, success W2/D1 1회, duplicate NotPreviewing/무차감, second PrototypeLimit |
| `W10-M04` reciprocal connector·왕복·시작 방 보존 | PASS | Upper/Basement Ladder, Side Door; start→module→start; 시작 방 작업대·모닥불 위치 불변 |
| `W10-M05` 새 방 general-floor | PASS | 세 모듈 모두 Campfire 유효 위치 배치·room ownership 확인 |
| `W10-M06` 키보드/합성 게임패드 | PASS | cycle `+1/-1`, confirm, cancel 공통 action 결과 동일 |
| `W10-L02` ko/en 의미·입력 안내 | PASS | 14 canonical module key, W2/D1 placeholder, 두 장치 confirm/cancel 의미 일치 |
| `W10-P01` 접근 우선 전체 회귀 | PASS | 프롬프트·배치·가방·신호·수색·수영·육지 복귀·3일 구조 포함 |
| `W10-P02` 1280×800 캡처 | PASS | 실제 KO/EN 3장과 synthetic-long 1장 모두 1280×800 PNG |
| `W10-L01` 일반 캠프 KO overflow | EXPECTED_FAIL P1 | active TMP overflow 2 |
| `W10-L03` 실제 qps-long | EXPECTED_FAIL P1 | locale absent |
| `W10-HW01` 물리 게임패드 | UNVERIFIED | batch Play Mode 장치명 없음, 사람의 실기 미실행 |

## 1:1 캡처 육안 검토

- `wave10-module-upper-ko-1280x800.png`: 위층 후보, W2/D1 비용, 유효 원인, 키보드 안내가 화면 안에서 읽힌다.
- `wave10-module-side-en-1280x800.png`: Side room, validity, cost, controls가 잘리거나 후보/connector를 숨기지 않는다.
- `wave10-module-interior-ko-1280x800.png`: explicit connector 안내, 시작 방 복귀 경로와 general-floor band가 읽힌다.
- `wave10-module-basement-synthetic-long-1280x800.png`: 합성 W 장문 스트레스는 ellipsis 정책만 확인한다. 실제 locale 검증 증거가 아니다.

## 빌드·릴리스 인프라

| 항목 | 결과 | 근거 |
|---|---|---|
| Unity 컴파일 | PASS | errors 0, warnings 0 |
| Windows x64 Development build | PASS | errors 0, warnings 0, `Succeeded` |
| 숨김 Player 스모크 | PASS | 1280×800 windowed, 6.377초 생존·응답 |
| Addressables | PASS | load/build/post-smoke, temporary link ownership 안정 |
| 자산 핵심 계약 | PASS | 261 checks |
| 과거 Wave 4 시각 어댑터 | ISOLATED_BY_DESIGN | `visual.current_*` 4개는 과거 파일명·baseline 결합으로 MISSING; 이번 신규 캡처와 모듈 계약으로 대체 |
| Steam | NOT_READY | App ID/SDK/depot/input/cloud/achievement 실증 없음 |

상위 도구가 출력한 `RELEASE_OVERALL=FAIL`은 위의 낡은 `visual.current_*` 어댑터 4건을 포함한 원시 Wave 4 결과다. 새 Wave 10 집계는 핵심 자산·Addressables·빌드·스모크와 현재 캡처를 독립 판정하며 이 어댑터를 제품 회귀로 사용하지 않는다.

## 재현 명령

변경 전 RED:

```powershell
& '.\Assets\Editor\ParallelQA\Invoke-Wave9SpatialCampContractGate.ps1' `
  -RunId '<NEW_RED_RUN_ID>' `
  -BaselineCommit '097cd1cbfa1f434c9836e8393c0b59f18e8d8e09' `
  -MinimumSmokeSeconds 6
```

데이터 기반 Wave 10:

```powershell
& '.\Assets\Editor\ParallelQA\Invoke-Wave10ModuleGate.ps1' `
  -RunId '<NEW_RUN_ID>' `
  -BaselineCommit '73f4f3621a41272f5052360a519aa46f9d74e79c' `
  -MinimumSmokeSeconds 6
```

모든 Unity Editor/build와 Windows Player 과정은 `Docs/QA/unity-codex-sandbox-licensing.md`에 따라 샌드박스 밖에서 실행했고 `-noUpm`은 사용하지 않았다.

## 증거 경로

- 변경 전: `Artifacts/ParallelQA/20260823T083100Z_097cd1c_wave10_redfirst/`
- 변경 후: `Artifacts/ParallelQA/20260823T092100Z_73f4f36_wave10_green/`
- 핵심 기계 판독 파일:
  - `wave10-summary.json`
  - `wave10-module-edit-contracts.json`
  - `wave10-module-play-contracts.json`
  - `wave10-module-edit-evidence.json`
  - `wave10-module-play-evidence.json`
  - `windows-development-build.json`
  - `windows-hidden-smoke.json`
  - `addressables-link-build-contract.json`
  - `addressables-link-post-smoke-contract.json`

## 권장 수정 파일 — 잔여 EXPECTED_FAIL만

1. P1 한국어 normal-camp overflow 2: `Assets/_Project/Scripts/Runtime/KimSurvivalPrototype.cs`
2. P1 실제 qps-long 부재: `Assets/_Project/Scripts/Localization/**`, 필요 시 `PrototypeLocalization.cs`

이 QA 브랜치에서는 위 제품 파일을 수정하지 않았다.

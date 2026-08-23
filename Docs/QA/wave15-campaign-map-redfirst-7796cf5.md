# Wave 15 50일 캠페인·수집 지도 RED-first 독립 게이트

## 판정

- 기준: `origin/master` `7796cf57568d0bad24595379e833e1dd9b4d8d3f`
- 브랜치: `codex/wave15-campaign-map-redfirst`
- 실행: `20260823T145800Z_7796cf5_wave15_red`, Unity `6000.4.9f1`, Windows PowerShell `5.1.26100.9168`
- 전체 / 제품 / 인프라: **RED / RED_EXPECTED_GAP / PASS**
- Wave 15 제품 계약: `1 PASS / 13 EXPECTED_GAP / 0 unexpected FAIL`
- Wave 15 인프라 체크: `3 PASS / 0 INFRA_FAIL`
- 인프라: 기준 SHA·정본·fresh evidence, Unity 컴파일, Windows Development build, hidden smoke, Addressables 모두 PASS
- 물리 게임패드: **UNVERIFIED**. 합성 입력은 실기 PASS로 계산하지 않았다.
- Steam: **NOT_READY**. App ID, Depot, Input, Cloud, Achievements, 권한·스토어 증거를 생성하거나 추정하지 않았다.

제품 런타임·씬·현지화 테이블·아트·Forge 원장은 수정하지 않았다. 변경은 QA Editor 러너, PowerShell 진입점, 문서와 신규 증거뿐이다.

## RED 기준선

현재 런타임은 정본과 달리 `FinalDay=5`이고 지도 근접 대상·세 지역 카탈로그·seed loot 생성기가 없다. 따라서 실패를 새 50일 기능의 PASS로 가리지 않고, 정확한 기준 SHA에서만 `PRODUCT_EXPECTED_GAP`으로 분류한다. 다른 SHA에서 동일 주장이 실패하면 같은 러너가 `PRODUCT_REGRESSION`과 종료 코드 `1`을 낸다.

| ID | 심각도 | 판정 | 실제 관찰 |
|---|---:|---|---|
| W15-D01 | P0 | EXPECTED_GAP | 새 run이 `D1/5/Camp/None` |
| W15-D02 | P0 | EXPECTED_GAP | Day 49 정산이 `D49/Result/Deadline`; Day 50 계속 불가 |
| W15-D03 | P0 | EXPECTED_GAP | Day 50에 Deadline은 나오지만 `FinalDay=5`이므로 50일 종료 계약은 미충족 |
| W15-D04 | P0 | PASS | 현재 신호대 2단계는 기한일에도 `Rescued` 우선 |
| W15-M01 | P0 | EXPECTED_GAP | 반사 검색 catalog entry `0`; beach/forest/shallow-sea 모두 없음 |
| W15-R01 | P0 | EXPECTED_GAP | 동일 seed+region+action 재현을 호출할 런타임 생성기 없음 |
| W15-R02 | P0 | EXPECTED_GAP | 다른 seed 변동과 선언된 min/max 범위를 검증할 계약 없음 |
| W15-R03 | P0 | EXPECTED_GAP | `viableEscapeRouteCount>=3`과 보장·대체 획득·장기 미발견 보호 증거 없음 |
| W15-L01 | P1 | EXPECTED_GAP | TSV 238행 중 지도/지역 계약 키 `0`; ko/en/qps-long 검증 불가 |
| W15-O01 | P1 | EXPECTED_GAP | PII 필드는 없지만 seed/region 로그 필드도 없음 |
| W15-P01 | P0 | EXPECTED_GAP | 근접 target enum에 map/expedition 멤버 없음; `>1.25m→near→popup→cancel` 실행 불가 |
| W15-P02 | P0 | EXPECTED_GAP | 세 지역 카드·미확인·정확한 수량 비공개 UI 없음 |
| W15-P03 | P1 | EXPECTED_GAP | 지도 region/action focus가 없어 키보드/합성 게임패드 동등성 실행 불가 |
| W15-P04 | P1 | EXPECTED_GAP | map far/near/popup이 없어 ko/en/qps-long 1280×800 지도 레이아웃 검증 불가 |

P0 재현 절차·영향·권장 파일과 P1의 세부 값은 `wave15-summary.json` 안의 `expectedGaps` 배열에 기계 판독 가능하게 보존했다.

## 기계 판독 계약

- 날짜: 새 run `Day 1/50`, Day 49 정산 후 Day 50 계속, Day 50 미탈출 terminal, 조기 탈출 우선.
- 근접: map target의 1.26m 지점에서 map 안내 숨김, 1.00m에서 정확히 1개, Interact 후 popup, Cancel 후 위치·Day·자원·신호·가방·phase/result fingerprint 불변.
- 지역: beach/forest/shallow-sea 안정 ID와 resource category, relative abundance, travel time, risk, weather, gear, special discovery, unknown 멤버. 예고 카드의 `ExactAmount/ExactQuantity` 노출은 실패.
- RNG: seed `41017` 동일 호출 2회 완전 일치, `41018/51017` 중 하나 이상 차이, 선언된 min/max 범위 내 결과.
- softlock: 생성 결과의 viable/protected/guaranteed escape route count `>=3`, critical-part guarantee, alternative acquisition, long-missing protection 세 플래그 모두 true.
- 현지화/입력: ko/en/qps-long 키, qps 팽창, region/action focus fingerprint, 장치 변경의 글리프 외 진행 불변.
- 레이아웃: 지도 루트 내 TMP만 정확한 1280×800 RenderTexture 좌표로 계측하여 overflow, 4px safe area 이탈, 의미 있는 텍스트 중첩을 0으로 요구.
- 로그: seed, stable region/action ID는 있고 user/machine/home path/email/IP/account 식별자는 없어야 함.

현재 RED 기준에서는 map이 없어 각 locale의 1280×800 캠프 스크린샷을 `map-absent` 증거로 남겼다. 1:1 육안 검토에서 지도 팝업 대신 기존 위층 연결 슬롯 안내가 표시되는 것을 ko/en/qps-long 모두에서 확인했다. 이 캡처는 지도 레이아웃 PASS가 아니라 제품 부재 RED의 시각 증거다.

## 잠금 회귀

- qps-long 전역 레이아웃 `10/10 PASS`.
- ko/en 배치 `24/24 PASS`, 수색·수영 `10/10 PASS`.
- 캠프 근접 안내, 제한적 자유 배치, 모듈 증축, 가방 `4→6`, 신호대, 수영 회귀 PASS.
- Unity 컴파일 `0 errors / 0 warnings`.
- Windows x64 Development build `PASS`, build `0 errors / 0 warnings`.
- 숨김 1280×800 Player smoke `6.347s PASS`; 최소 시간 시점 alive/responding true.
- Addressables load/build/post-smoke `PASS`.
- PowerShell 5.1 UTF-8 no-BOM JSON/TXT `PASS`.

Wave 14 보고서의 `exactSixOfTenBaselineReproduced=false`와 예전 6-RED를 설명하는 문구는 과거 기준 필드다. 현재 잠금 판정은 fresh target `10/10 PASS`, `productOverall=PASS`, `infrastructureOverall=PASS`를 직접 사용한다.

## 재실행

Windows PowerShell 5.1 또는 PowerShell 7에서 새 RunId로 실행한다. Unity/Player는 `Docs/QA/unity-codex-sandbox-licensing.md`에 따라 샌드박스 밖에서 실행하고 `-noUpm`을 쓰지 않는다.

```powershell
& '.\Assets\Editor\ParallelQA\Invoke-Wave15CampaignMapGate.ps1' `
  -RunId '<NEW_RUN_ID>' `
  -BaselineCommit '7796cf57568d0bad24595379e833e1dd9b4d8d3f' `
  -MinimumSmokeSeconds 6
```

종료 코드:

- `0`: GREEN — 인프라 PASS, fresh Wave 14 10/10 PASS, Wave 15 EXPECTED_GAP/FAIL 0.
- `2`: RED_EXPECTED_GAP — 정확한 `7796cf5` 기준에서 승인된 미구현 제품 계약만 실패.
- `1`: 예상 밖 제품 회귀 또는 인프라 실패.

후속 Unity 구현 통합 후에는 `-BaselineCommit '<INTEGRATED_HEAD_SHA>'`로 같은 명령을 실행한다. 그 SHA에서 남은 주장 실패는 EXPECTED_GAP이 아니라 FAIL이며, 0-failure일 때만 GREEN이다.

## 증거

정식 fresh 증거 루트: `Artifacts/ParallelQA/20260823T145800Z_7796cf5_wave15_red`

- `wave15-summary.json` / `wave15-summary.txt`
- `wave15-edit-contracts.json` / `wave15-play-contracts.json`
- `wave15-edit-evidence.json` / `wave15-play-evidence.json`
- `wave15-ko-camp-map-absent-1280x800.png`
- `wave15-en-camp-map-absent-1280x800.png`
- `wave15-qps-long-camp-map-absent-1280x800.png`
- `wave14-qps-global-layout-gate.json` / `wave14-qps-global-layout-targets.tsv`
- `wave3-visual-gate.txt` / `wave3-visual-metrics.tsv`
- `compile-result.txt`
- `windows-development-build.json`
- `windows-hidden-smoke.json`
- `addressables-link-build-contract.json` / `addressables-link-post-smoke-contract.json`
- `wave15-powershell-compatibility.json`

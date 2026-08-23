# Wave 9 공간형 캠프 플레이테스트 통합 기준

- 검증 기준 커밋: `246b1b881e145f74addd0c7a594ac48eb6d5ffff`
- 실행 ID: `20260823T052053Z_246b1b8_wave9_spatial_camp_playtest`
- Unity: `6000.4.9f1`
- 실행 정책: `Docs/QA/unity-codex-sandbox-licensing.md`에 따라 Unity Editor와 Windows Player를 제한 샌드박스 밖에서 실행했다.
- 최종 판정: `OVERALL=RED`, `PRODUCT=RED_EXPECTED_FAIL`, `INFRASTRUCTURE=PASS`

## 플레이테스트 가능한 범위

- 정상 캠프에서 전역 행동 대시보드와 대형 가방 패널이 숨고 월드가 먼저 보인다.
- 설비에서 멀 때 안내와 팝업이 없고, 가까이 가면 대상 하나의 안내만 나타난다.
- 상호작용 뒤 접근한 작업대·모닥불·빗물받이·구조 신호대의 전용 팝업만 열린다.
- 팝업 중 이동이 잠기며 확인 또는 취소 뒤 같은 위치의 직접 조작으로 돌아온다.
- 작업대 연구·제작·가방 확장, 구조 신호, 제한적 자유 배치·재배치, 수색·수영·장벽·귀환·3일 생존 루프가 접근 우선 자동 검증에서 PASS했다.
- 한국어·영어 1280×800 캡처, Windows x64 Development 빌드, 6초 숨김 실행 스모크와 Addressables load/build/post-smoke가 PASS했다.

## 알려진 미구현·미검증 범위

- 위층·옆방·지하실 모듈 후보, 연결 슬롯·비용, 겹침·필수 통로 검증은 설계와 레드 퍼스트 계약만 있으며 런타임은 아직 없다.
- 한국어 정상 캠프에서 TMP 두 개가 overflow 플래그를 내므로 1280×800 가독성은 P1 후속 확인 대상이다. 현재 캡처에서 헤더 간 겹침은 없다.
- 물리 게임패드 실기는 `UNVERIFIED`, Steam 출하 준비는 `NOT_READY`다.
- Wave 6/7의 옛 Play 시각 픽스처 중 전역 대시보드·상시 가방을 직접 측정하는 항목은 새 공간형 UI와 계약이 달라 `ISOLATED_BY_DESIGN`으로 분리했다. 현재 대체 증거는 Wave 9 접근 우선 전체 루프다.

## 증거와 실행 파일

- 원시 증거: `Artifacts/ParallelQA/20260823T052053Z_246b1b8_wave9_spatial_camp_playtest/`
- 요약: `wave9-summary.json`, `wave9-summary.txt`
- 상황형 플레이 계약: `wave9-play-contracts.json`, `wave9-spatial-play-evidence.json`
- 옛 픽스처 격리 근거: `wave9-legacy-play-layout-isolation.json`
- 빌드·스모크: `windows-development-build.json`, `windows-hidden-smoke.json`
- 로컬 플레이테스트 빌드: `Builds/Playtest-246b1b8/KimSurvivalIsland.exe`
- 실행 파일 SHA-256: `93C19F9E7C681845D34407807D33B6438E781DD34C4D8895EBDF2C6FB083711D`

## 키보드·마우스 조작

- 캠프: `A/D` 이동, `E` 가까운 설비 사용, 방향키+`Enter` 또는 클릭으로 행동, `Esc` 팝업 취소, `F1` 언어 전환
- 배치: 마우스로 위치 이동, 클릭/`Enter` 확정, 우클릭/`Esc` 취소
- 수색: `A/D` 이동, 해안 자동 수영, `Space` 점프, `E` 수색, `R` 귀환, `1`~`6` 가방 교체, `Esc` 포기


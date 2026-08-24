# 김씨 생존기: 무인도 · 리소스 통합 플레이테스트

- 빌드 기준: `b51683440c27063bc335cb0f9de3e6bd27302b26`
- Unity: `6000.4.9f1`
- 전체 게이트: `GREEN` (`PRODUCT=PASS`, `INFRASTRUCTURE=PASS`)
- 강화 제품 검사: `23/23 PASS`
- qps-long 레이아웃: `10/10 PASS`
- 컴파일: `0 errors`
- Windows Development Build: `PASS`, warnings `0`
- 숨김 실행: `PASS`, `6.572s`
- Addressables: `PASS`
- 현재 고정 실행 경로의 인바운드 방화벽 차단 규칙: `PASS`
- 실행 파일 SHA-256: `93c19f9e7c681845d34407807d33b6438e781dd34c4d8895ebdf2c6fb083711d`

## 실행

`C:\Users\dev\Documents\ChatGPT\신규 개발 본부\work\ParallelQA\StableWindowsBuild\KimSurvivalIsland.exe`

방화벽 규칙은 위 고정 경로에 연결되어 있다. 실행 파일을 다른 경로로 옮기면 Windows가 새 프로그램 경로로 판단해 방화벽 확인을 다시 표시할 수 있다.

## 이번 테스트에서 볼 내용

1. 캠프에서 A/D로 김씨를 직접 이동하고, 가까운 모닥불·작업대·구조물에서 E로 상호작용한다.
2. 상황형 팝업에서 제작·건설·증축·구조 프로젝트를 진행한다.
3. 수색 지도를 열어 지역별 예상 자원·위험·날씨·장비·특별 발견을 비교하고 출발한다.
4. 수색 지역에서 나무·돌·식량·표류물 아이콘을 직접 수색하고 가방 한도를 확인한다.
5. 위험 이벤트, 서로 다른 구조 신호/무전 경로, 50일 캠페인과 행동 기반 엔딩 흐름을 확인한다.
6. 화면 하단의 현재 입력 장치 안내를 우선하며, F1 또는 언어 버튼으로 한국어/영어를 전환한다.

## 연결된 시각 리소스

- 김씨 캐릭터 스프라이트와 이동/수영 상태
- 모닥불·작업대·빗물받이·구조 신호대 구조물
- 나무·돌·식량·표류물 수색 아이콘
- 베이스캠프 상황형 상호작용 UI
- 수집 지역 선택 지도 A
- 3컷 엔딩 코믹 및 엔딩 앨범 A 패키지

## 증거

- `wave18-summary.json`: 최종 전체 판정
- `wave14-qps-global-layout-gate.json`: 한국어/영어 확장 대비 레이아웃 판정
- `windows-development-build.json`: 빌드 경로·SHA·경고 수
- `windows-hidden-smoke.json`: 실제 Player 숨김 실행 결과
- `wave18-windows-firewall-contract.json`: 빌드·스모크·방화벽 경로 일치
- `camp-ko-1280x800.png`, `exploration-ko-1280x800.png`, `expedition-map-ko-1280x800.png`, `ending-comic-ko-1280x800.png`: 실제 Play 캡처

## 아직 수동 확인이 필요한 것

- 물리 게임패드 실제 조작: `UNVERIFIED`
- Steamworks 배포·업적·Cloud·Input 연동: `NOT_READY`

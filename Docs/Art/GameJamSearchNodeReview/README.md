# 환경 수색 노드·발견물 트레이 검토 패킷

이 패킷은 사용자 승인 전까지 `review-only`다. 두 후보 모두 `selectedCandidate=null`, `runtimeAllowlist=[]`, `packageAllowed=false`, `runtimeConnectAllowed=false`이며 런타임·씬·Addressables에 연결하지 않는다.

## 후보 인덱스

- `object.searchable-resource-node-kit.state-language-a`
  - Forge job: `job_20260825150605_49020784`
  - 풀숲, 바위틈, 표류물 더미, 나무 구멍, 난파선 수납함, 폐시설 캐비닛의 `unknown / known-partial / depleted` 상태다.
  - 검토 보드: `Assets/_Project/Art/Generated/separated_parts/job_20260825150605_49020784/search-node-review-board-1920x1080.png`
  - 실제 화면: `Assets/_Project/Art/Generated/separated_parts/job_20260825150605_49020784/search-node-actual-size-1280x800.png`
  - Forge 자동 점수: 62점, 오류 1·경고 1. 불투명한 실제 화면 증거만 범용 알파 규칙에 걸렸고 투명 런타임 후보는 모두 통과했다.
  - 사용자 검토: 난파선 수납함의 해골형 장식이 위험 표식처럼 보이는지, 나무 구멍의 미확인/부분 잔류 차이가 충분한지 판단한다.

- `ui.search-loot-tray.compact-bottom-a`
  - Forge job: `job_20260825150608_cb65726e`
  - 발견물 카드 3개, 4/6칸 가방 요약, 담기·남기기·교체용 엔진 슬롯을 한 contextual bottom tray로 묶는다.
  - 검토 보드: `Assets/_Project/Art/Generated/ui_set/job_20260825150608_cb65726e/search-loot-tray-review-board-1920x1080.png`
  - 실제 화면: `Assets/_Project/Art/Generated/ui_set/job_20260825150608_cb65726e/search-loot-tray-actual-1280x800.png`
  - 현지화/포커스: `Assets/_Project/Art/Generated/ui_set/job_20260825150608_cb65726e/search-loot-tray-localization-focus-1280x800.png`
  - Forge 자동 점수: 70점, 오류 1·경고 0. 불투명한 실제 화면 증거만 범용 알파 규칙에 걸렸고 투명 컴포넌트와 SVG는 통과했다.
  - 사용자 검토: 한 번에 발견물 카드 3개가 적절한지, 트레이가 기존 가방 요약을 잠시 덮는 구성이 허용되는지 판단한다.

## 공통 전달 규칙

- 본문·수량·행동명은 TMP로 넣고 래스터에 굽지 않는다.
- KO 300px, EN 380px, qps-long 520px 안전 폭과 18px 이상 본문을 기준으로 한다.
- 키보드/마우스와 게임패드는 같은 44×44 glyph focus 슬롯을 교체한다.
- 상태는 색과 함께 닫힘/열림/빈 실루엣, 빗금·노치·방패·경고 삼각형으로 구분한다.
- 수색 노드는 PPU 100, bottom-center pivot이며 세 상태가 같은 바닥선과 크기를 사용한다.

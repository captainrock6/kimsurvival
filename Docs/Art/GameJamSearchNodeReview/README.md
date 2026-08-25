# 환경 수색 노드·발견물 트레이 검토 패킷

이 패킷은 사용자 승인 전까지 `review-only`다. 두 후보 모두 Forge registry에 `decision=review`, `selectedCandidate=null`, `runtimeAllowlist=[]`, `packageAllowed=false`, `runtimeConnectAllowed=false`, `runtimeConnected=false`, `engine={}`로 명시되어 있다. 런타임·씬·Addressables에 연결하지 않는다.

통합 회귀 방어 계약은 [integration-regression-guard.json](integration-regression-guard.json)에, 발견물 표면의 기존 채택 아이콘 재사용 계약은 [loot-surface-adopted-icon-guid-binding.json](loot-surface-adopted-icon-guid-binding.json)에 기계 판독 형태로 고정했다.

## 통합 회귀 진단

- 조사 증거: `Artifacts/ParallelQA/20260825T160510Z_gamejam_search_node_integrated`
- 실패: `W19-P02.resource_nodes_adopted_icons`가 `no Wood node exists`에서 중단됐다.
- 기존 채택 아이콘의 GUID 판정은 네 종류 모두 PASS였다: Wood `5ba05e4e569ab6745bff72d0b9ba9151`, Stone `c881b7198e647ad40b32c63ce18e27a2`, Food `59695c50812722b458c210b4cfb02c12`, Salvage `cb8829a17d9cc9049aa16fbf4393097a`.
- 직접 원인은 아트 GUID 교체가 아니라 새 수색 런타임이 기존 `ResourceNodeMarker` 기반 Wood/Stone/Food/Salvage 월드 노드를 생성하지 않는 런타임 소유권 변화다. loose resource를 수색 node로 의도적으로 교체한 설계에 대한 구형 회귀 판정인지는 QA 담당이 결정한다. 이 아트 패스에서는 Runtime C#과 QA acceptance를 수정하지 않았다.
- 두 review job 경로는 runtime reject 범위다. 기존 채택 `icon.resource-tool-set`의 current job과 네 GUID만 보호 대상으로 유지하며 새 상태 아이콘은 이를 대체할 수 없다.
- Unity 6000.4.9f1 재임포트로 두 후보 job의 누락 meta 57개와 job 폴더 meta 2개를 생성했다. 두 번째 재임포트에서 후보 meta 57개와 기존 채택 아이콘 meta 13개의 SHA-256 변경은 모두 0개였다.

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

## 1280×800 통합 검토본

대표 검토본은 `Docs/Art/GameJamSearchNodeReview/search-node-loot-tray-adopted-icon-binding-1280x800.png`이다. 같은 1280×800 프레임에서 상태형 수색 노드, 김씨, 우측 4/6 가방 요약, compact bottom tray의 점유율과 채택된 Food/Salvage/Stone/Wood 아이콘 슬롯 연결을 함께 확인한다. 이 이미지는 actual-size 합성 증거이며 런타임 allowlist 대상이 아니다.

검토본에 보이는 한국어는 배치 검증용 엔진 화면 캡처다. 재사용 가능한 런타임 조각인 `search-loot-tray-components.png`, `search-loot-tray-components-editable.svg`, 수색 노드 상태 PNG에는 KO/EN/qps-long 본문을 굽지 않았고 TMP safe rect만 제공한다. 대표 보드의 아이콘은 기존 채택 원본을 그대로 축소 합성했으며 새 아이콘을 만들지 않았다.

## 공통 전달 규칙

- 본문·수량·행동명은 TMP로 넣고 래스터에 굽지 않는다.
- KO 300px, EN 380px, qps-long 520px 안전 폭과 18px 이상 본문을 기준으로 한다.
- 키보드/마우스와 게임패드는 같은 44×44 glyph focus 슬롯을 교체한다.
- 상태는 색과 함께 닫힘/열림/빈 실루엣, 빗금·노치·방패·경고 삼각형으로 구분한다.
- 수색 노드는 PPU 100, bottom-center pivot이며 세 상태가 같은 바닥선과 크기를 사용한다.
- 기존 채택 Wood/Stone/Food/Salvage 아이콘은 자원 결과 표시용이다. review 수색 노드의 상태 실루엣·상태 아이콘과 stable ID/GUID 역할을 합치지 않는다.
- compact tray의 `loot-card.resource-icon`, `bag-summary.stack-icon`, `known-remainder.resource-icon` 슬롯은 기존 채택 아이콘 GUID만 참조한다. review job의 위험·보호 부품·잔여물 상태 아이콘은 상태 보조 표식이며 자원 아이콘을 대체하지 않는다.

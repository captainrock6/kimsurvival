# Wave 15 수집 지역 선택 지도 UI · review-only 인계

## 상태

- 브랜치: `codex/wave15-expedition-map-review`
- 지도 UI 자산: `ui.expedition-map`
- 지도 job: `job_20260823144023_146e6ff7`
- 아이콘 자산: `icon.expedition-resource-risk-set`
- 최신 아이콘 job: `job_20260823145302_c4c41491`
- 아이콘 부모 job: `job_20260823144003_552c87b1`
- 두 자산 상태: `review`
- `selectedCandidate`: `null`
- package, engine-ready, runtime-connect: 금지 / 실행하지 않음

부모 아이콘 job의 24개 원본 atlas에는 문제가 없었으나 불투명 QA 읽기성 보드가 `missing-alpha`와 `low-contrast`로 판정되어 62점이었다. 새 그림 생성 없이 QA 보드만 투명 외곽 여백과 고대비 교대 패널로 교정한 child job이 100점, 오류 0, 경고 0으로 통과했다. 부모 job은 immutable 이력으로 보존한다.

## 후보

### A — `ui.expedition-map.right-rail-a` · 추천

- 지도 왼쪽, 상세 카드 오른쪽.
- 지역 노드→상세 정보→출발 행동의 스캔 순서가 가장 안정적이다.
- 세로 방향으로 TMP 행을 재배치할 수 있어 ko/en/qps-long에 가장 유리하다.
- 단점은 B보다 지도 폭이 작다는 점이다.

### B — `ui.expedition-map.bottom-drawer-b`

- 넓은 지도 위, 상세 drawer 아래.
- 섬 전체와 세 노드의 공간 관계가 가장 크게 보인다.
- qps-long에서 하단 행의 높이가 부족해지기 쉬워 2단 재배치가 필요하다.

### C — `ui.expedition-map.compact-right-c`

- 지도 폭을 넓히고 우측 카드를 좁힌 고밀도안.
- 지도와 선택 노드는 크게 읽히지만 상세 정보의 첫 인지와 장문 확장 여유가 가장 약하다.

추천은 A지만 이는 제작 추천일 뿐 사용자 채택이 아니다.

## 정보와 상호작용 계약

- 캠프 지도 오브젝트에 근접해 상호작용하는 동안만 열린다.
- 닫기 또는 취소는 같은 캠프 직접 조작으로 복귀한다.
- 해변, 숲, 얕은 바다의 세 노드를 제공한다.
- 상세 카드에는 자원 범주와 상대 풍부함, 이동 시간, 현재 위험, 날씨, 필요 장비, 특별 발견 가능성만 표시한다.
- 정확한 수량과 미발견 결과는 숨긴다.
- idle, selected, locked, danger-warning, equipment-short, departure-ready, unknown을 색상 외 실루엣·이중 링·빗금·잠금·체크·슬래시로 구분한다.
- 실제 지역명, 설명, 버튼 본문과 입력 글리프는 TMP·런타임 슬롯으로 교체한다.
- 입력 포커스 최소 크기는 44×44px이다.

## 엔진 인계

- 캔버스: 1280×800.
- 바깥 안전 영역: L36 / R36 / T38 / B38.
- popup 9-slice: L32 / R32 / T28 / B28.
- detail card 9-slice: L24 / R24 / T20 / B20.
- action button 9-slice: L18 / R18 / T14 / B14.
- 최소 TMP 크기: 18px. qps-long 1.5배 확장 시 축소보다 줄바꿈·세로 재배치를 우선한다.
- 지도 원화, 경로, 노드, 상태 overlay, 카드, 아이콘, TMP safe rect, glyph, focus outline을 독립 레이어로 유지한다.
- `background.coast-forest.gameplay-band-contrast`는 팔레트·명암 방향만 참조한다.
- `job_20260823132501_b4a82bed/coast-forest-clean-selected-b.png`, review board, QA overlay는 runtime allowlist에 넣지 않는다.

## 주요 파일

- 지도 비교 보드: `Assets/_Project/Art/Generated/ui_set/job_20260823144023_146e6ff7/expedition-map-review-board.png`
- 지도 manifest: `Assets/_Project/Art/Generated/ui_set/job_20260823144023_146e6ff7/expedition-map-review-manifest.json`
- 지도 QA: `Assets/_Project/Art/Generated/ui_set/job_20260823144023_146e6ff7/expedition-map-visual-qa.json`
- 지도 Forge 품질: `Assets/_Project/Art/Generated/ui_set/job_20260823144023_146e6ff7/quality-report.json`
- 최신 아이콘 atlas: `Assets/_Project/Art/Generated/ui_set/job_20260823145302_c4c41491/expedition-icons-atlas.png`
- 아이콘 실제 크기 보드: `Assets/_Project/Art/Generated/ui_set/job_20260823145302_c4c41491/expedition-icons-readability-16-24-32-48.png`
- 아이콘 manifest: `Assets/_Project/Art/Generated/ui_set/job_20260823145302_c4c41491/expedition-icons-manifest.json`
- 아이콘 Forge 품질: `Assets/_Project/Art/Generated/ui_set/job_20260823145302_c4c41491/quality-report.json`

유료 외부 API는 호출하지 않았다. 지도 원화는 Codex ImageGen을 한 번만 사용했고 나머지는 로컬 결정론 도구로 제작했다.

# Wave 15 수집 지도 A안 selected-only handoff

사용자가 `ui.expedition-map.right-rail-a`를 명시적으로 채택했다. 런타임 구현의 유일한 레이아웃 기준 이미지는 `candidate-a-right-rail-1280x800.png`다. B/C, 비교 보드, 로컬라이제이션 오버레이와 QA 프리뷰는 선택되지 않았으며 런타임 로드 대상이 아니다.

## 정확한 허용 범위

- `candidate-a-right-rail-1280x800.png`: 채택된 1280×800 우측 상세 rail 레이아웃 기준본
- `island-map-art.png`: A가 사용하는 공통 편집 지도 원화
- `expedition-map-components.svg`: 패널·노드·버튼을 엔진 UI로 재구성하기 위한 공통 편집 소스

파일 해시와 바이트 크기는 같은 폴더의 `wave15-expedition-map-a-selected-only-manifest.json`을 정본으로 삼는다. 패키지의 `forge-import.json`에서 위 세 파일 외 원본 이미지가 보이면 통합하지 않는다.

## Unity 전달 규칙

- 기준 캔버스: 1280×800, PPU 100, Bilinear, sRGB, max size 2048, compression None
- 9-slice: popup `L32 R32 T28 B28`, detail `L24 R24 T20 B20`, action `L18 R18 T14 B14`
- 포커스와 입력 glyph 슬롯: 최소 44×44px
- TMP: ko/en/qps-long 교체형 텍스트만 사용하고 번역 본문을 비트맵에 굽지 않는다.
- qps-long은 150% 팽창을 가정해 줄바꿈 후 세로 재배치하며, 1280×800에서 글자를 18px 미만으로 축소하지 않는다.
- 외곽 safe inset은 `L36 R36 T38 B38`, 내용 safe inset은 `L68 R70 T76 B74`다.

## 선택되지 않은 항목

- `ui.expedition-map.bottom-drawer-b`
- `ui.expedition-map.compact-right-c`
- 로컬라이제이션/입력 오버레이, 상태·포커스 비교 보드, QA 프리뷰와 품질 보고서
- `icon.expedition-resource-risk-set`: A 채택과 독립된 review 자산이며 이번 패키지에서 채택하지 않는다.

이 handoff는 엔진 준비 패키지의 허용 목록을 고정하지만 씬 또는 런타임 연결을 승인하거나 수행하지 않는다.

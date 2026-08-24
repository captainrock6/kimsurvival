# Wave 18 김씨의 생존 앨범 UI 리뷰

- 기준: `origin/master@fac8545148e1422fc6258f57cab2205cbb4596a9`
- 브랜치: `codex/wave18-ending-gallery-review`
- Forge asset: `ui.ending-gallery`
- Forge job: `job_20260824133802_f43c6431`
- 상태: `review`
- 선택: `selectedCandidate=null`
- 런타임: `runtimeAllowlist=[]`, package/runtime/scene/Addressables 연결 없음
- 생성 경로: 로컬 결정론 SVG + PIL 래스터, ImageGen 및 유료 외부 API 미사용

## 후보

### A — `ui.ending-gallery.album-spread-a`

펼친 앨범의 왼쪽 페이지에 normal 5, comic 5, rare 4, day50 5를 네 줄로 배치하고 오른쪽 페이지에서 선택한 엔딩의 대표 3패널, TMP 제목·요약·플레이 근거와 재생 액션을 확인한다.

- 장점: 캠프의 앨범/기록 오브젝트와 가장 자연스럽게 이어지고, 전체 19개 현황과 선택 상세의 균형이 좋다.
- 우려: 작은 왼쪽 카드가 흐트러지지 않도록 범주별 행 규칙을 유지해야 한다.
- 추천: **1순위**. 추천은 채택이 아니다.

### B — `ui.ending-gallery.card-index-b`

19개를 5×4 인덱스로 한눈에 보여 주고 선택 상세를 하단 drawer에 연다.

- 장점: 전체 해금 현황을 가장 빠르게 비교하며 마우스 탐색이 쉽다.
- 우려: 정보 밀도가 높고 실제 생존 앨범이라는 촉감은 A보다 약하다.

### C — `ui.ending-gallery.filmstrip-c`

왼쪽 범주 spine, 중앙 세 줄 filmstrip, 오른쪽 상세 panel로 순차 탐색한다.

- 장점: 게임패드 포커스 흐름과 미해금 실루엣의 분위기가 강하다.
- 우려: 19개 전체 현황을 한 번에 파악하는 속도는 A/B보다 느리다.

## 콘텐츠·국제화 계약

- 19개 안정 ending ID를 정본 순서대로 수용: normal 5 / comic 5 / rare 4 / day50 5.
- 해금 항목: 대표 패널, 제목·짧은 요약·플레이 근거 TMP, 재생 액션.
- 미해금 항목: 비스포일러 실루엣과 `{endingId}.hint` TMP만 노출.
- 실제 후보 PNG/SVG에는 `<text>` 또는 번역 본문이 없다.
- KO 기본, EN 지원, qps-long 150%, 최소 18px, wrap 후 vertical reflow.
- keyboard/mouse·gamepad glyph와 포커스는 최소 44×44.
- normal은 이중 둥근선, comic은 버스트+점, rare는 별+광선, day50은 스티치+해칭으로 색상 외 구분.

## 품질 결과

- 육안·콘텐츠 QA: 100점, 경고 0, 오류 0.
- Forge 기계 게이트: **40점 / 오류 2 / 경고 0**.
- 두 오류는 실제 후보가 아니라 의도적으로 불투명한 `review-board`와 `localization-accessibility-qa`에 UI 타입의 투명도 규칙이 적용된 `missing-alpha`다.
- 실제 후보 PNG 3개는 1280×800 true alpha, 가장자리 여백과 대비 정상이며 SVG 3개는 유효하고 `<text>` 요소가 0개다.
- 실패 후 자동 재생성·재import하지 않았으며 최초 job과 실패 증거를 보존했다.
- Forge가 같은 job 폴더의 기존 파일을 import하면서 추적 파일명에 `-2`를 붙였지만 unsuffixed 원본과 SHA-256이 동일하며 별도 후보나 재생성 결과가 아니다.

## 사용자 선택 문구

추천 A를 고를 경우 정확히 다음처럼 답하면 된다.

```text
ui.ending-gallery.album-spread-a를 채택한다.
```

B 또는 C를 고를 때는 안정 ID만 바꾸면 된다. 수정이 필요하면 `ui.ending-gallery.[ID]: 수정 — [변경점]`으로 답한다.

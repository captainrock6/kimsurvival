# Wave 17 위험·탈출·엔딩 아트 선택 가이드

이 패킷은 서로 독립적인 세 자산의 사용자 결정을 받기 위한 **review-only** 자료다. 세 후보를 한 게임 화면으로 합친 것이 아니며, 기존 원본 PNG 픽셀을 수정하거나 새 변형을 생성하지 않았다.

통합 보드: `wave17-hazard-escape-ending-selection-board.png`

> 2026-08-24 사용자 결정: 아래 세 안정 후보를 모두 명시적으로 채택했다. 보드 안의 `REVIEW ONLY` 표시는 선택 당시의 승인 게이트를 보존한 기록이다. 현재 Forge 상태는 `engine_ready`이며 selected-only Unity import 계약까지 만들었지만, 씬·런타임·Addressables 연결은 하지 않았다.

## 선택 당시 공통 승인 게이트

- 현재 결정: `review`
- 선택 후보: `null`
- 런타임 allowlist: `[]`
- 패키징 허용: `false`
- 런타임 연결 허용: `false`
- 이번 작업에서 ImageGen, 유료 외부 API, Forge 생성·수정 job을 호출하지 않음
- 사용자가 각 안정 ID에 대해 새로 명시한 판단만 후속 승인 근거가 됨

## 확정된 선택

- `effect.survival-hazards.phase-silhouette-a`: 채택
- `ui.escape-project-progress.route-signature-a`: 채택
- `ui.ending-comic.triptych-a`: 채택
- 실제 후보 PNG/SVG와 필요한 manifest만 패키지 대상으로 허용하며 리뷰·판독성 보드는 제외
- 런타임 allowlist는 계속 비어 있고 `runtimeConnectAllowed=false`

## 1. effect.survival-hazards.phase-silhouette-a

Forge job: `job_20260823160305_ef04b0f3`

비교할 것:

- 부상·폭우/폭풍·식량 도난이 32px와 64px에서도 서로 다른 실루엣으로 읽히는가
- 예고(점선 삼각형·파동), 발생(톱니 폭발), 완화(방패·체크), 회복(원형장·상승 표식)이 색상 없이도 구분되는가
- 효과가 짧고 코믹하며 비잔혹적인가
- 실제 적용 시 피해 행동 주변 또는 HUD 사건 알림에 붙어도 플레이어와 보행 동선을 가리지 않을 것 같은가

예상 런타임 위치: 월드 사건 지점 또는 관련 HUD 사건 알림 주변의 짧은 상태 효과. 상시 전체 화면 패널로 쓰지 않는다.

## 2. ui.escape-project-progress.route-signature-a

Forge job: `job_20260823160324_1de3b748`

비교할 것:

- 실제 플레이 경로인 연기 신호와 라디오가 동일 버튼의 색상 변형이 아니라 서로 다른 작업으로 보이는가
- 데이터 경로인 뗏목·조명탄·비컨도 선체·발사 타이밍·탑 구조 문법으로 구분되는가
- 진행 단계, 준비 조건, 위험과 날씨 창을 확인한 뒤 출발/작업 판단을 내리기 쉬운가
- KO/EN/qps-long 텍스트가 TMP 영역에서 늘어나도 경로 표식과 44×44 입력 포커스를 침범하지 않는가

예상 런타임 위치: 탈출 설비를 직접 상호작용할 때만 열리는 중앙 상황형 팝업. 상시 캠프 대시보드로 쓰지 않는다.

## 3. ui.ending-comic.triptych-a

Forge job: `job_20260823160342_eceb3933`

비교할 것:

- 3장 구성의 설정 → 클로즈업 → 결과 흐름이 1280×800에서 즉시 읽히는가
- 일반·코믹·희귀·50일 판정이 색상 외에도 이중선·버스트·별·스티치/해칭 형태로 구분되는가
- 하단 판정 근거 영역이 만화의 핵심 장면을 압박하지 않으면서 탈출 경로·행동·특별 사건·동률 판정을 설명할 수 있는가
- KO/EN/qps-long 본문과 키보드/게임패드 글리프를 래스터 밖 TMP 슬롯으로 교체하기 쉬운가

예상 런타임 위치: 플레이 종료 시의 1280×800 전체 화면 결과 오버레이. 캠프 HUD에는 사용하지 않는다.

## 당시 사용한 답변 형식

각 안정 ID를 따로 판단해 아래 형식으로 답하면 된다.

```text
effect.survival-hazards.phase-silhouette-a: 채택
ui.escape-project-progress.route-signature-a: 수정 — [바꾸고 싶은 점]
ui.ending-comic.triptych-a: 보류
```

가능한 결정은 `채택`, `수정 — 변경점`, `보류`, `거절 — 이유`다. 한 항목의 채택은 다른 두 항목의 채택이나 런타임 연결을 뜻하지 않는다.

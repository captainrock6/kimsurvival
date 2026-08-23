# Wave 10 아트 사용자 선택 인덱스

- Forge asset: `ui.wave10-art-review-board`
- Forge job: `job_20260823085134_0094a8e6`
- 상태: `review-only`
- 현재 선택: 없음
- 공통 화면 문맥: `background.modular-island-camp@job_20260823044425_d2c6dc6b`, `ui.camp-module-expansion@job_20260823080005_174af5ec`

이 보드의 A/B/C는 **상호작용 프롬프트 모양만** 비교한다. 방 배경과 증축 비용 벨트는 세 안에 공통으로 적용되는 review 문맥이며 서로 다른 후보가 아니다. 근접 프롬프트와 증축 비용 벨트는 실제 게임에서 서로 다른 상태에 표시된다.

| 표기 | 안정 후보 ID | 기본 프롬프트 점유율 | 장점 | 우려 |
|---|---|---:|---|---|
| A | `ui.camp-contextual-interaction.compact-a` | 1.725% | 기존 남색·청록·크림 HUD와 가장 자연스럽고 사각 입력 슬롯의 44×44 포커스가 명확하다. | C보다 조금 크고 B보다 수제 질감이 얌전하다. |
| B | `ui.camp-contextual-interaction.compact-b` | 2.031% | 밧줄·황색 테두리가 가장 급조한 코믹 생존 분위기이며 기본 TMP 행동명 영역이 가장 넓다. | 세 안 중 가장 크고 밝아 내레이션과 경쟁할 수 있다. |
| C | `ui.camp-contextual-interaction.compact-c` | 1.509% | 가장 작아 방·사다리·캐릭터 가시성이 좋고 원형 입력 캡이 빠르게 읽힌다. | 원형 캡이 기존 사각 HUD와 덜 일관적이며 기본 TMP 영역이 가장 좁다. |

공통 검토 수치:

- 프롬프트 TMP 안전 폭: `ko 300px / en 380px / qps-long 520px`
- 증축 비용 벨트: `ko 430×82 / en 520×82 / qps-long 640×96`
- 비용 벨트 기본 안전영역 점유율: `3.748%`; qps-long 최대 점유율: `6.000%`
- 프롬프트는 내레이션 카드 아래 12px 간격, 화면 중앙 `x=640`, `top=293`에 고정한다.
- 실제 번역 본문과 키보드·게임패드 glyph는 래스터에 없으며 런타임 TMP/Sprite Asset 슬롯을 사용한다.
- 세 후보 모두 방 외곽, 지하 사다리, 계단·연결부, `y=508.75` 이동 기준선 가시성 검토를 통과해야 한다.

채택을 원할 때는 위의 **안정 후보 ID 하나를 정확히 명시**해야 한다. 이 문서와 보드 작성 자체는 승인이나 채택으로 간주하지 않는다. 사용자 승인 전에는 `adopt`, `engine_ready`, `package`, `runtime-connect`를 수행하지 않는다.

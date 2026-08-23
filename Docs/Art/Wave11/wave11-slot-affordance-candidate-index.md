# Wave 11 연결 슬롯 시각 후보 인덱스

- Forge asset: `ui.camp-connection-slot-affordance`
- 최종 검토 job: `job_20260823094919_b97f9f61`
- QA 보정 부모 job: `job_20260823094023_cdb015ec`
- 상태: `review-only`
- 현재 선택: 없음
- 품질 게이트: 100점, 오류 0, 경고 0

이 패키지는 시작 방에서 직접 접근하는 위층·옆방·지하실 연결 슬롯의 월드 시각 신호만 비교한다. 기존 상호작용 프롬프트 `compact-a/b/c`의 후보 체계와 독립적이며, 프롬프트나 방 모듈 UI를 채택·교체하지 않는다.

| 안정 후보 ID | 방향 인지 | 상태 인지 | 장점 | 검토 우려 |
|---|---|---|---|---|
| `ui.camp-connection-slot-affordance.lashed-hardware` | 묶은 목재 해치·벽틀·바닥틀 | 확장 코너, 이중 방향 표시, 교차 버팀목 | 기존 대나무·표류목 캠프와 가장 자연스럽다. | 작은 크기에서 밧줄 매듭이 가장 복잡하다. |
| `ui.camp-connection-slot-affordance.cutline-bracket` | 절취선과 열린 브래킷 | 점선 리듬, 네 모서리 포커스, 끊긴 패턴 | 상태 문법이 가장 선명하고 월드 아트 점유가 작다. | 다른 후보보다 기술적이며 수제 질감이 약하다. |
| `ui.camp-connection-slot-affordance.salvage-tab` | 리벳 금속판과 힌지 | 돌출 탭, 열린 플랩, 대각 버팀목 | 가장 압축된 실루엣이며 근접·열림 상태가 빠르게 읽힌다. | 정지 상태만 보면 수리 지점처럼 보일 수 있다. |

공통 상태 규칙:

- `idle`: 닫힌 해치·임시 막음 실루엣과 반복 힌지/매듭/리벳 패턴
- `near-focus`: 바깥으로 확장되는 네 모서리 또는 돌출 탭
- `preview-valid`: 열린 연결 실루엣과 위·오른쪽·아래 이중 방향 표시
- `invalid`: 교차 버팀목과 끊긴 위험 패턴
- 모든 상태는 색상 없이 실루엣·패턴·표식만으로도 구분한다.

화면·현지화 계약:

- 실제 화면 검토 해상도는 1280×800이며 연결 중심은 위 `(622,278)`, 옆 `(775,505)`, 지하 `(443,509)`다.
- 위층 표식의 하단은 `y=290` 이내이며 내레이션 아래 prompt의 시작선 `y=294`와 최소 4px를 둔다.
- 슬롯 파츠에는 번역 본문과 입력 glyph가 없다. 근접 안내는 별도의 TMP 행동명 영역과 44×44 키보드/게임패드 glyph 슬롯을 사용한다.
- `ko 300px / en 380px / qps-long 440px`의 prompt 안전 폭을 예약하고 의미 있는 말줄임표를 사용하지 않는다.
- 월드 슬롯은 물리 해치·프레임 범위만 교체하며 방 전체 틴트나 채워진 포커스 사각형을 사용하지 않는다.

검토 자료:

- `wave11-slot-affordance-comparison-board.png`: 세 후보의 1280×800 실제 크기 화면을 나란히 비교
- `wave11-slot-affordance-state-matrix.png`: 세 슬롯 × 네 상태의 비색상 단독 형태 신호 비교
- `slot-affordance-unity-manifest.json`: 레이어, 피벗, 접속 좌표, 아틀라스 슬라이스, TMP/glyph 슬롯과 Unity import 규격

사용자가 위의 안정 후보 ID 하나를 명시적으로 선택하기 전에는 `adopted`, `engine_ready`, `package`, `runtime-connect`를 수행하지 않는다.

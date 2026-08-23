# Wave 13 공간 확장 연결부 review-only 인계

- Forge asset: `ui.camp-connection-slot-affordance`
- Wave 13 job: `job_20260823120036_3f045106`
- Parent: `job_20260823094919_b97f9f61`
- 상태: `review`
- 선택 후보: 없음
- Runtime allowlist: 비어 있음

기존 Wave 11 후보와 최종 통과 1280×800 캡처만 재사용했다. ImageGen과 유료 외부 API를 호출하지 않았다. 비교 보드에 보이는 한글·영어·의사언어는 통과 캡처 증거이며 런타임 스프라이트가 아니다.

## 사용자가 고를 안정 후보 ID

| 후보 ID | 장점 | 우려 |
|---|---|---|
| `ui.camp-connection-slot-affordance.lashed-hardware` | 급조 목재·밧줄 캠프와 가장 자연스럽다. | 작은 크기의 매듭이 복잡하다. |
| `ui.camp-connection-slot-affordance.cutline-bracket` | 상태 문법이 가장 선명하고 월드 점유가 작다. | 다른 후보보다 기술적으로 보인다. |
| `ui.camp-connection-slot-affordance.salvage-tab` | 가장 압축된 실루엣이며 근접·열림이 빠르게 읽힌다. | idle이 수리 지점처럼 보일 수 있다. |

기능 기준 추천은 `cutline-bracket`이다. 절취선, 열린 모서리, 끊긴 invalid 패턴이 색 없이도 가장 빨리 구분되고 김씨·통로·설비를 가리는 면적이 가장 작다. 추천은 채택이 아니다.

## 상태 매핑

- 접근 가능: `near-focus`. 물리 해치·벽틀·바닥틀 외곽만 확장한다.
- 선택: `preview-valid`. 열린 연결 실루엣과 방향 표식, 방 ghost, 상태 outline을 서로 다른 레이어로 둔다.
- 비용 부족: `invalid`와 별도 비용 belt를 사용한다. 교차 버팀목·끊긴 패턴으로 색 이외의 차이를 유지한다.
- 건설 완료: preview marker와 비용 belt를 제거하고 확정 방 및 영구 사다리·계단·해치·문틀을 남긴다. 별도 완료 sprite를 이 job에서 만들거나 로드하지 않는다.

## 1280×800 safe-zone

좌표 원점은 화면 왼쪽 위다.

- Narration: `(230.4,144,819.2,136)`
- Compact prompt: `(420,292,440,44)`, 내레이션 아래 정확히 12px
- Walking band: `(0,535.1,1280,81.8)`
- Player: 위 `(309.3,411.3,85.4,152.6)`, 옆 `(1169.8,411.3,85.4,152.6)`, 지하 `(700.4,411.3,85.4,152.6)`
- Runtime target: 위 `(316.4,503.1,78.2,103.1)`, 옆 `(1176.9,503.1,78.2,103.1)`, 지하 `(707.6,503.1,78.2,103.1)`
- Art connector: 위 `(570,264,104,26)`, 옆 `(763,453,24,104)`, 지하 `(391,496,104,26)`
- 위층 표식 하단 `y=290`, art prompt 상단 `y=294`: 최소 4px

Prompt는 세 방향 모두 player·target·walking band와 겹침 0이다. 슬롯 오버레이는 connector footprint만 교체하며 방 전체 tint, 채워진 focus 사각형, 설비·김씨 위 표식을 금지한다.

## Unity 규격

- 파일: 후보별 `slot-affordance-<candidate>-atlas.png`
- Atlas: 512×512 RGBA, 128×128 cell, 4열 `idle/near-focus/preview-valid/invalid`, 3행 `upper/side/basement`, padding 12px
- Pivot: 모든 cell `(0.5,0.5)`
- Import: Sprite (2D and UI), Multiple, Bilinear, PPU 100, alphaIsTransparency on, mipmap off, max 2048, Full Rect
- Review 중 compression: Uncompressed. 사용자가 하나를 선택한 뒤에만 플랫폼 압축을 검토한다.

레이어 순서는 `room shell → slot idle → slot focus → slot previewState → room ghost → state outline → permanent connector → facility/player → foreground → compact prompt → cost belt`다.

비용 belt는 기존 review 자산을 참조할 뿐 이번 runtime allowlist에 포함하지 않는다. 9-slice는 L28/R28/T24/B24이며 TMP 폭은 ko 430, en 520, qps-long 640을 검토한다. Direct prompt는 채택된 compact-a의 별도 TMP/glyph 구조를 사용하며 ko 300, en 380, qps-long 최대 520, glyph focus 44×44를 유지한다.

세 후보 중 정확한 후보 ID 하나를 사용자가 선택하기 전에는 atlas, editable source, actual-size 화면, 비교 보드, 비용 belt 어느 것도 Resources·Addressables·씬에서 로드하지 않는다.

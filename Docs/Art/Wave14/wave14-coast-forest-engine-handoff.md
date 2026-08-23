# Wave 14 해변·숲·얕은 바다 배경 review-only 인계

- Forge asset: `background.coast-forest`
- Wave 14 job: `job_20260823130453_32a9debf`
- Parent: `job_20260822095339_18288994`
- 상태: `review`
- 선택 후보: 없음
- Runtime allowlist: 비어 있음

기존 배경 원화, 채택된 자원·장벽 아트, 최신 1280×800 런타임 캡처만 로컬에서 결정론적으로 합성했다. 새 회화 비트맵, ImageGen, 유료 외부 API는 사용하지 않았다. 비교 보드의 글자와 런타임 캡처 속 한글은 검토 증거일 뿐 런타임 배경 소스가 아니다.

## 사용자가 고를 안정 후보 ID

| 후보 ID | 장점 | 우려 |
|---|---|---|
| `background.coast-forest.current-balanced` | 기존 따뜻한 원화와 해변·숲·바다 비율을 그대로 보존한다. | 우측 숲에서 장벽이 묻히고, 원화에 이미 그려진 통나무와 독립 장벽이 중복된다. |
| `background.coast-forest.gameplay-band-contrast` | 김씨·네 자원·수영 경계·장벽 실루엣이 가장 명확하며 기존 구도를 유지한다. | 깊이가 약간 평평해질 수 있어 전 화면 tint가 아닌 분리 레이어 grading이 필요하다. |
| `background.coast-forest.shoreline-first` | 얕은 바다와 수영 진입이 가장 빨리 읽힌다. | 숲 동선과 후반 노드 여백이 줄고 우측 UI에 가까워진다. |

기능 기준 추천은 `background.coast-forest.gameplay-band-contrast`다. 추천은 채택이 아니다. 사용자가 정확한 후보 ID 하나를 새로 선택하기 전에는 어떤 PNG도 패키징하거나 런타임에 연결하지 않는다.

## 감사 결론

- 기존 Forge 원화는 좌측 얕은 바다 → 중앙 해변·바위턱 → 우측 숲 입구가 한 화면에 명확하다.
- 중앙 모래 지면에서 김씨의 주황·초록 비율과 네 자원 아이콘은 1280×800에서 읽힌다.
- 수영 전환은 해안 경사로 읽히며 C안이 가장 넓게 보여준다.
- 우측 숲은 장벽과 색·질감이 비슷하다. A안은 장벽 실루엣이 가장 쉽게 묻힌다.
- 현재 런타임 캡처는 단색 프로토타입 배경이다. 탐색·수영·장벽 상태 전이는 보여주지만 최종 배경 합성 품질을 증명하지 않는다.
- 원화 우측에는 쓰러진 통나무와 덩굴이 이미 구워져 있다. 이 상태로 채택된 독립 `blocked/interactable/cleared` 장벽을 올리면 장벽이 두 개처럼 보이고 cleared 뒤에도 길이 막힌 그림이 남는다.

## 선택 이후에만 적용할 레이어 규격

공통 캔버스는 1672×941, 공통 원점은 bottom-center `(0.5,0.0)`이다.

1. `coastForest.far.skySeaIslands` — 불투명 원경.
2. `coastForest.mid.forestRocks` — 투명 중경. 장벽이 놓일 숲 입구에는 구워진 통나무를 남기지 않는다.
3. `coastForest.ground.playableShore` — 해안 경사·모래·바위 지면. 충돌은 별도 엔진 데이터다.
4. `coastForest.objects.resourceNodes` — 독립 true-alpha, bottom-center 접점 피벗.
5. `coastForest.objects.vineWoodBarrier` — 기존 채택된 3상태 true-alpha 자산.
6. `coastForest.player.mrKim` — 배경 grading의 영향을 받지 않는다.
7. `coastForest.foreground.edgeFoliage` — 화면 가장자리만 덮고 발·수영 경계를 가리지 않는다.
8. HUD·내레이션·compact prompt·가방·하단 도움말은 모두 별도 UI 레이어다.

## 1280×800 safe-zone

좌표 원점은 화면 왼쪽 위다.

- Top HUD: `(16,14,1247,119)`
- Narration: `(230,144,819,136)`
- Compact prompt: `(420,292,440,44)`
- 현재 캡처의 우측 가방 rail: `(961,241,297,440)`
- Bottom help: `(16,685,1247,101)`
- 핵심 월드 피사체 권장 영역: `(64,336,876,349)`
- 김씨 검토 envelope: `(616,475,90,150)`
- 자원 노드 검토 지름: 78px, 김씨와 최소 20px 간격
- 우측 가방 rail이 계속 보이면 장벽 오른쪽 끝은 `x≤940`이어야 한다. 그렇지 않으면 탐색 중 rail을 접는 별도 런타임 결정을 먼저 해야 한다.

수영 경계는 엔진 상태·foam/mask로 표현하고 글자를 배경에 굽지 않는다. 자원, 장벽, 김씨는 HUD·내레이션·prompt·하단 도움말과 겹치지 않는다.

## Unity import 권장값

- Texture Type: Sprite (2D and UI), Single
- Pivot: Bottom Center `(0.5,0.0)`
- Filter: Bilinear
- Wrap: Clamp
- PPU: 100
- Color space: sRGB
- Mipmaps: Off
- Max Texture Size: 2048
- 불투명 원경: alphaIsTransparency Off
- 분리된 중경·지면·전경: RGBA, alphaIsTransparency On
- Review 중 compression: Uncompressed
- 선택·실제 합성 검증 뒤에만 플랫폼 고품질 압축 검토

비교 보드와 A/B/C 프리뷰는 EditorOnly 검토 증거다. Resources, Addressables, 씬, 런타임 로더에 넣지 않는다. Wave 11 연결 슬롯 후보는 독립 `review` 상태이며 이번 작업에서 선택하지 않았다.

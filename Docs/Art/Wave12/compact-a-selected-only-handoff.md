# Wave 12 compact-a selected-only handoff

- Asset: `ui.camp-contextual-interaction`
- Selected candidate: `ui.camp-contextual-interaction.compact-a`
- Forge job: `job_20260823073121_f5da3402`
- Forge state: `engine_ready`
- Runtime connection: not performed in this Wave

사용자가 채택한 범위는 A안 하나다. `forge-import.json`이 job의 A/B/C 산출물 22개를 모두 열거하더라도 Unity 런타임이 로드할 수 있는 시각 파일은 다음 하나뿐이다.

`Assets/_Project/Art/Generated/ui_set/job_20260823073121_f5da3402/compact-a.png`

SHA-256: `32bcadf5ec7117eb5d0e2602c8ba8a8dd2e83a56a117e0a6368311831f901d43`

## Unity 구현 allowlist

- Runtime visual: `compact-a.png`만 허용한다.
- GUID 보존 companion: `compact-a.png.meta`는 저장소 동반 파일일 뿐 런타임 로드 대상이 아니다.
- Authoring source: `compact-a-selected-editable.svg`만 A 전용 편집 소스로 사용한다. Resources/Addressables에 넣지 않는다.
- 1280×800 검토본 `actual-size-compact-a-1280x800.png`은 QA 참조이며 런타임 로드 금지다.
- 기계 판독 정본은 `compact-a-selected-only-handoff.json`, 화면 수치 검증은 `compact-a-safe-zone-validation.json`이다.

`compact-b*`, `compact-c*`, `compact-interaction-atlas*`, 전체 후보 `compact-interaction-editable*`, 모든 `*-2.*`, actual-size/board/QA 파일은 런타임 reject다. 전체 exact 목록은 selected-only JSON의 `runtimeRejectExact`를 따른다.

## Sprite와 배치

- PNG: 384×64 RGBA8, true alpha, 가장자리 비투명 픽셀 0
- Pivot: `(0.5, 0.5)`, PPU 100
- 9-slice: Left 70 / Right 30 / Top 12 / Bottom 12
- Glyph: 내용 40×40, focus 44×44
- TMP 기본 safe rect: `(78,17,268,30)`
- 중앙 상단 `x=640`, 내레이션 하단 뒤 12px, visible top `y=293`
- 패널 폭: ko 300 / en 380 / qps-long 최대 520
- Locale 전환은 중앙 slice만 늘리고 높이·피벗·glyph cap·focus rect를 바꾸지 않는다.

Unity import 계약은 Sprite (2D and UI), Single, Bilinear, sRGB, PPU 100, Uncompressed, mipmap off, alphaIsTransparency false다. 현재 `.meta`는 범위 제한 때문에 수정하지 않았고 Texture Type, Sprite Mode, mipmap, compression, spriteBorder가 이 계약과 아직 다르다. 런타임 구현 작업에서 importer를 적용한 뒤 QA가 GUID와 SHA를 다시 확인해야 한다.

## 1280×800 불변 조건

- Top HUD 하단 `y=82` → 내레이션 상단 `y=145`: 63px
- 내레이션 하단 `y=281` → prompt visible top `y=293`: 12px
- qps-long 최대 canvas 하단 `y=349` → 김씨 보수 envelope 상단 `y=380`: 31px
- qps-long 최대 canvas 하단 `y=349` → 48px 보행 band 상단 `y=485`: 136px
- ko/en/qps-long과 keyboard/gamepad 교체 모두 HUD·내레이션·김씨·보행 band 겹침 0

Wave 11의 `ui.camp-connection-slot-affordance` 세 후보는 이 선택과 별개이며 계속 `review`, 선택 없음이다.

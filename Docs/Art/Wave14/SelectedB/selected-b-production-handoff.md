# Wave 14 선택 B 제작용 배경 후보 인계

- 채택된 방향: `background.coast-forest.gameplay-band-contrast`
- 방향 선택 job: `job_20260823130453_32a9debf`
- 새 제작 후보 job: `job_20260823132501_b4a82bed`
- 새 후보 상태: `review`
- 새 비트맵 승인: 아직 없음
- Runtime allowlist: 비어 있음

사용자 선택에 따라 B안의 깨끗한 제작용 파생본을 한 번 생성했다. 기본 Codex ImageGen 편집 경로를 사용했으며 유료 외부 API는 호출하지 않았다. 원화에 구워져 있던 우측 통나무와 덩굴을 제거하고, 독립 장벽이 놓였다가 사라져도 통로가 자연스럽게 유지되는 모래 숲길로 복원했다.

## 검토 결론

- 해변→바위턱→숲, 해변→얕은 바다 동선은 유지된다.
- 보행 지면은 원경·깊은 숲·하단 전경보다 밝게 읽힌다.
- 기존 `blocked` 장벽은 우측 가방 rail 왼쪽에서 읽히고 김씨 proxy와 겹치지 않는다.
- `cleared` 상태에서는 배경에 남은 장벽 그림 없이 길이 열린다.
- 캐릭터, 자원, 장벽, UI, 글자는 제작용 배경에 구워지지 않았다.
- ImageGen이 우측 식생과 통로 세부를 다시 그렸으므로, 방향 채택과 별개로 이 정확한 비트맵의 사용자 승인이 필요하다.

## 파일 역할

- `coast-forest-clean-selected-b.png`: 1672×941 불투명 RGB 제작 후보.
- `gameplay-band-mask.png`: R 채널이 보행·상호작용 가독성 보호 영역을 뜻하는 마스크.
- `depth-subdue-mask.png`: R 채널이 원경·전경 선택 grading 허용량을 뜻하는 마스크.
- `selected-b-blocked-review-1280x800.png`: blocked 합성 검토 증거. 런타임 금지.
- `selected-b-cleared-review-1280x800.png`: cleared 합성 검토 증거. 런타임 금지.
- `selected-b-production-review-board.png`: 전후 및 상태 비교 보드. 런타임 금지.

현재 레이어 분리는 `불투명 배경 + grading mask 2개 + 기존 독립 장벽`까지다. 하늘·바다, 숲 중경, 지면, 전경 식생 자체는 아직 한 장의 회화 배경에 합쳐져 있다. 실제 parallax가 필요하면 이 정확한 후보 승인 뒤 별도 분리 패스를 진행한다.

## Unity 권장값

배경:

- Sprite (2D and UI), Single
- Pivot bottom-center `(0.5,0.0)`
- PPU 100, Bilinear, Clamp, sRGB On
- Alpha Is Transparency Off, mipmaps Off, max 2048
- 검토 중 Uncompressed

마스크:

- Texture Type Default, R 채널 사용
- sRGB Off, Alpha Source None
- Bilinear, Clamp, mipmaps Off, max 2048, Uncompressed
- 명시적 런타임 연결 전에는 shader/material에서 사용하지 않는다.

새 제작 후보를 명시적으로 승인하기 전에는 Forge package, Addressables, Resources, 씬 연결을 하지 않는다.

# 김씨 생존기: 무인도

Unity 6 기반의 코믹 2D 횡스크롤 생존 수직 슬라이스입니다. 현재 버전은 정식 아트가 아닌 런타임 도형과 편집 가능한 Unity UI를 사용해 전체 핵심 루프를 검증합니다.

## 플레이 루프

1. 베이스캠프에서 모닥불, 작업대, 빗물받이와 구조 신호대를 건설합니다.
2. 작업대에서 돌도끼와 밧줄 제작법을 연구하고 도구를 만듭니다.
3. 해변과 숲을 수색해 나무, 돌, 식량과 표류물을 4칸 가방에 담습니다.
4. 해가 지기 전에 귀환해 생존과 성장에 자원을 투자합니다.
5. 3일 안에 구조 신호대를 2단계까지 완성하면 구조에 성공합니다.

## 실행

- Unity: `6000.4.9f1`
- 시작 씬: `Assets/_Project/Scenes/KimSurvivalPrototype.unity`
- Windows 빌드: `Builds/Windows/KimSurvivalIsland.exe`

## 조작

| 행동 | 키보드·마우스 | 게임패드 |
|---|---|---|
| 이동 | A/D 또는 방향키 | 왼쪽 스틱/D-pad |
| 점프 | Space/W | A |
| 채집·수색 | E/F | X |
| 귀환/취소 | R/Esc | B |
| UI 이동 | Tab/방향키 | D-pad/스틱 |
| UI 선택 | Enter/클릭 | A |
| 가방 교체 | 숫자 1~4 또는 버튼 | 슬롯 선택 후 A |

## 검증 증거

Unity 편집기 자동화는 순수 규칙 검사, Play Mode 전체 루프 검사, 1280×800 캡처와 Windows 개발 빌드를 생성합니다. 결과는 `Artifacts/Verification/`에 기록됩니다.

## 임시 리소스

현재 화면은 다음 Forge 안정 자산 ID의 교체 가능한 플레이스홀더입니다.

- `background.island-camp`
- `background.coast-forest`
- `character.mr-kim`
- `object.camp-structures`
- `ui.survival-hud`
- `icon.resource-tool-set`
- `effect.comedy-feedback`


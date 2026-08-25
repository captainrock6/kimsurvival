# 김씨 생존기: 무인도

Unity 6 기반의 코믹 2D 횡스크롤 거점 생존 게임잼 수직 슬라이스입니다. 채택된 김씨·섬 배경·캠프 설비·UI 리소스와 엔진 텍스트를 사용하며, 미채택 Forge 후보는 review-only로 런타임과 분리합니다.

## 플레이 루프

1. 김씨를 직접 움직여 캠프 설비와 수집 지도에 접근하고 상황형 팝업으로 상호작용합니다.
2. 위험도·자원 전망·장비 조건이 다른 7개 지역 중 오늘 수색할 곳을 고릅니다.
3. 풀숲·바위틈·표류물 더미 같은 환경 수색물을 뒤져 숨겨진 발견물을 공개합니다.
4. 4칸 가방에 필요한 것만 담거나 교체하고, 남은 발견물·고갈 node·장벽·위험 상태를 재방문까지 보존합니다.
5. 캠프로 귀환해 생존·치료·제작·가방 6칸 확장·2층·지하실·탈출 프로젝트 중 어디에 투자할지 결정합니다.
6. 뗏목·대형 연기·무전 중 하나를 완성하면 행동 기록에 따른 3장 core+modifier 코믹북 엔딩을 봅니다.

표준 캠페인은 Day 50 이전 조기 탈출을 허용합니다. 게임잼의 약 30분 대표 프로필과 Day 20 전후 장기 체류 프로필은 별도 데이터로 관리하며 수치는 자연 플레이테스트 전 `PROVISIONAL`입니다.

## 실행

- Unity: `6000.4.9f1`
- 시작 씬: `Assets/_Project/Scenes/KimSurvivalPrototype.unity`
- 최신 자동 검증 Windows 후보: `work/ParallelQA/StableWindowsBuild/KimSurvivalIsland.exe`

## 조작

| 행동 | 키보드·마우스 | 게임패드 |
|---|---|---|
| 이동 | A/D 또는 방향키 | 왼쪽 스틱/D-pad |
| 수영 | 해안에서 바다 방향으로 이동(자동 전환) | 왼쪽 스틱/D-pad |
| 점프 | Space/W | A |
| 채집·수색 | E/F | X |
| 귀환/취소 | R/Esc | B |
| UI 이동 | Tab/방향키 | D-pad/스틱 |
| UI 선택 | Enter/클릭 | A |
| 가방 교체 | 숫자 1~6 또는 버튼 | 슬롯 선택 후 A |

## 검증 증거

Unity 편집기 자동화는 규칙 검사, 실제 Scene Play Mode 경로, KO/EN/qps-long 1280×800 레이아웃, Windows Development 빌드와 hidden smoke를 검증합니다. 최신 Wave C GREEN 기록은 `Docs/QA/gamejam-wave-c-integrated-green-e4bbc03.md`이며, 인간 플레이 시간·가방 선택 체감·물리 게임패드·첫 사용자 세션은 자동 결과와 분리합니다.

## 리소스 상태

현재 런타임은 다음 Forge 안정 자산 ID의 채택 리소스를 사용합니다. 새 후보는 사용자가 별도로 채택하기 전 자동 연결하지 않습니다.

- `background.island-camp`
- `background.coast-forest`
- `character.mr-kim`
- `animation.mr-kim.swim`
- `object.camp-structures`
- `ui.survival-hud`
- `ui.camp-contextual-interaction.compact-a`
- `ui.expedition-map.right-rail-a`
- `ui.escape-project-progress.route-signature-a`
- `ui.ending-comic.triptych-a`
- `ui.ending-gallery.album-spread-a`
- `icon.resource-tool-set`
- `effect.comedy-feedback`

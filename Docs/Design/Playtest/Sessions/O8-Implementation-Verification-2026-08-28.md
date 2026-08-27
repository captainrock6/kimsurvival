# O8 교정 구현·후보 빌드 검증 기록

- 기준 세션: `O7-H1_RETEST_REQUIRED · HUMAN_FAIL · PROGRESSION_BLOCKED`
- 교정 범위: 초반 수집 경제, 순차 지역 해금, 초소형 가방, 진수대 피드백, 일반 설비 다층 자유 배치
- 상태: `UNITY_PASS · RELEASE_BUILD_PASS · O8-H1_IN_PROGRESS`

## 반영 결과

| 항목 | 구현 결과 |
|---|---|
| 순차 지역 해금 | 새 게임은 해변만 열리고 `해변 → 숲 → 얕은 바다 → 바위 능선 → 섬 동굴 → 난파선 만 → 폐중계 관측소` 순서로 첫 진입 때 다음 지역 하나가 저장 가능하게 열린다. |
| 초반 수집 경제 | 돌도끼 이전 구간에서 해변 `식량 6·나무 6·표류물 6`, 숲 `돌 6·나무 18` 단위가 결정적으로 존재한다. 전체 7지역·84 node·일반 자원 432·나무 84 총량은 유지한다. |
| 가방 UI | 좌측 하단 패널 폭을 화면의 `22%`로 줄이고 슬롯·아이콘·글꼴을 함께 축소했다. |
| 해안 진수대 | 모든 비종료 행동의 성공·실패를 팝업 유지 상태의 상단 결과 배너로 전달한다. `출항창` 표현을 없애고 같은 진수대 팝업에서 날씨·조류 확인 후 같은 버튼으로 출항한다고 명시한다. |
| 설비 배치 | 캠프파이어·탈출 설비·사다리/연결부만 고정한다. 작업대·빗물받이·침대·소파는 완성된 1층·2층·지하에서 0.5u 좌표 배치하며 충돌·출입구·사다리 통로를 막을 수 없다. |
| 저장 호환 | 일반 설비 좌표와 층을 안정 ID로 저장·복원하며 O7 고정 지점 저장은 가장 가까운 유효 자유 좌표로 이주한다. O7 수색 ledger도 O8 자원표에서 불러올 수 있다. |

## 검증 결과

| 검증 | 결과 |
|---|---|
| Unity Roslyn runtime compile | `PASS · errors 0` |
| Unity Roslyn editor compile | `PASS · errors 0` |
| O8 수색 경제 계약 | `PASS · 7r/84n · stock 432 · wood 84@5 regions` |
| 도구 없는 초반 최소량 | `PASS · beach(food6, wood6, salvage6) · forest(stone6, wood18)` |
| 순차 해금 도메인 | `PASS · 초기 Forest 잠김 · Beach 첫 진입 뒤 Forest 해금 · frontier 저장/복원` |
| 초기 지도 상태 | `PASS · Beach DepartureReady · 나머지 6지역 Locked` |
| 자유 배치 도메인 | `PASS · 시작층 x=2.5 commit · free stable ID` |
| 다층 설비 배치 | `PASS · 작업대/빗물받이/침대/소파가 upper/basement 각각 commit` |
| 배치 저장 원자성 | `PASS · basement round-trip · 범위 밖 free ID 거부` |
| 가방·생존 도메인 | `PASS · compact-bottom-left · 침대/소파/체력/일광 회귀` |
| 로컬라이제이션 TSV | `PASS · 602 keys · 4 columns · duplicate 0` |
| Unity 라이선스·컴파일 | `PASS · Unity Personal entitlement · errors 0` |
| O8 가방 Play 관찰 | `PASS · 4/10·10/10 슬롯 · 좌하단 compact · 탐색 tray 비중첩` |
| O8 캠프 Play 관찰 | `PASS · 계획 지점만 미리보기 · 실패 팝업 유지 · KO/EN/qps-long` |
| 진수대 자연 경로 | `PASS · 순차 해금 중 해변/숲 수집 · 3단계 · closed-window 원자성 · early terminal` |
| Windows x64 Release | `PASS · source 03dae26 · BuildOptions None · hygiene PASS · 171 payload files` |

## O8 후보

- 소스 커밋: `03dae2620ef3e849ccc242d95fb6309080ad7e98`
- 실행 파일: `work/ParallelQA/20260827T161000Z_o8_release_03dae26/KimSurvivalIsland-gamejam-win64-release-03dae26/KimSurvivalIsland.exe`
- ZIP: `work/ParallelQA/20260827T161000Z_o8_release_03dae26/KimSurvivalIsland-gamejam-win64-release-03dae26.zip`
- ZIP SHA-256: `b8f5aae5c97809c6d296b1c4e6b00f7817fe483aec0d89f8e36a3b5dcf82cb6c`
- 릴리스 증거: `Artifacts/ParallelQA/20260827T161000Z_o8_release_03dae26/gamejam-release-build.json`
- 수기 세션: `O8-H1 · 2026-08-28 00:37 KST · PID 24256 · 1280×800 windowed · keyboard/mouse`

## O8 수기 재검증 우선순위

1. 해변 첫 진입에서 식량·나무·표류물을 돌도끼 없이 실제로 찾는다.
2. 해변 진입 뒤 숲만 새로 열리고, 숲 진입 뒤 얕은 바다가 열린다.
3. 숲 첫 구간의 돌로 돌도끼 연구·제작 흐름에 진입한다.
4. 가방이 캐릭터와 좌측 탐색 공간을 가리지 않는다.
5. 진수대 단계 실패 이유와 최종 출항 행동이 같은 팝업에서 보인다.
6. 작업대·빗물받이·침대·소파를 2층과 지하로 옮기고 저장 후 복원한다.

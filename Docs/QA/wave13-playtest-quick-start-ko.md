# 《김씨 생존기: 무인도》 Wave 13 플레이테스트 빠른 시작

이 패키지는 기준 커밋 `386a602f110ebfe2c404685f98f9cacf1b42c1d2`의 Windows x64 Development 빌드입니다. Steam 배포본이 아니며 Steam READY를 뜻하지 않습니다.

1. ZIP을 새 폴더에 완전히 풉니다. ZIP 내부에서 직접 실행하지 마세요.
2. `PLAYTEST-MANIFEST.json`과 `SHA256SUMS.txt`가 있는지 확인합니다.
3. `KimSurvivalIsland.exe`, `KimSurvivalIsland_Data`, `UnityPlayer.dll`, `MonoBleedingEdge`가 같은 폴더에 있는지 확인합니다.
4. `KimSurvivalIsland.exe`를 실행합니다. Windows 보안 경고가 뜨면 경고 문구와 화면을 기록하고 임의로 권한을 우회하지 마세요.
5. 한국어가 기본으로 표시되는지 확인하고, 1280×800 창에서 플레이합니다.
6. 새 게임으로 Day 1부터 시작해 캠프 접근·설비 팝업·수색·수영·귀환·가방·신호대·방 확장을 확인한 뒤 Day 5 구조 또는 기한 실패까지 진행합니다.
7. 종료 후 체크리스트와 재현 절차, 스크린샷, 사용한 입력 장치를 함께 제출합니다.

문제가 생기면 다음을 함께 기록하세요.

- 패키지 ZIP SHA-256과 `PLAYTEST-MANIFEST.json`의 baselineCommit
- Windows 버전, 화면 배율, 해상도, GPU
- 시작 시각과 문제 발생 Day/장소/대상
- 재현 직전 입력과 기대 결과/실제 결과
- `KimSurvivalIsland_Data` 옆 또는 Unity Player가 안내한 로그 경로의 Player 로그

물리 게임패드는 실제 장치를 사람이 조작해 체크리스트를 완료하기 전까지 `UNVERIFIED`입니다. 자동/합성 게임패드 결과를 실기 PASS로 적지 마세요.

# Unity QA 방화벽 팝업 방지 검증

- 기준 커밋: `2fa1cef6fcceab273a9f37a74ac905e4dbd17289`
- 고정 실행 경로: `work/ParallelQA/StableWindowsBuild/KimSurvivalIsland.exe`
- 빌드 옵션: `Development` (`AllowDebugging` 없음)
- Windows x64 빌드: `Succeeded`, 오류 0, 경고 0
- 빌드/스모크 실행 파일 SHA-256 일치: `93c19f9e7c681845d34407807d33b6438e781dd34c4d8895ebdf2c6fb083711d`
- 숨김 스모크: `PASS`, 6.226초, alive/responding `true`
- Addressables post-smoke: `PASS`
- Wave 15/16 회귀와 qps-long 10/10: `PASS`
- 방화벽 규칙: master·Unity 작업·QA 작업의 고정 플레이어 경로 3개 모두 `Enabled=True`, `Inbound`, `Block`, 프로그램 경로 일치
- 물리 게임패드: `UNVERIFIED`
- Steam: `NOT_READY`

Wave 17 제품 미완성 15건은 본 인프라 수정과 무관하며 기존 상태를 유지한다.

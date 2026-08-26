# GAME JAM Windows 후보 배포 매니페스트

## 후보 식별자

- 게임 소스 커밋: `3d64403813493d8de8e05f0844a5e616e164c6d4`
- 패키지에 포함된 체크리스트 커밋: `7678ab25ddb67af8087faecfb224d61c440459e0`
- 최종 패키지 검증 러너 커밋: `2c9f36a200032d821992234788ef372747b8b925`
- Unity: `6000.4.9f1`
- 대상: `StandaloneWindows64` Development Build
- 최종 빌드 완료: `2026-08-26 13:14:51 KST`

## 배포 파일

- 폴더: `work/ParallelQA/KimSurvivalIsland-gamejam-win64-3d64403`
- ZIP: `work/ParallelQA/KimSurvivalIsland-gamejam-win64-3d64403.zip`
- ZIP SHA-256: `68f639c10f74982ba2b2fd813d158de61217242252baed436286fa322433b5f4`
- ZIP 해시 sidecar: `work/ParallelQA/KimSurvivalIsland-gamejam-win64-3d64403.zip.sha256`

## 실행 코드 동결

- `KimSurvivalIsland.exe`: `93c19f9e7c681845d34407807d33b6438e781dd34c4d8895ebdf2c6fb083711d`
- `KimSurvivalIsland_Data/Managed/Assembly-CSharp.dll`: `b7e216c2892ee952905ccc2cbb37caa7050c355da38d6582e2d4c5d1b762f7e6`

Unity 런처 EXE 해시만으로는 게임 코드 변경을 구분할 수 없으므로, 수기 테스트 시작 전에 EXE와 `Assembly-CSharp.dll` 두 해시를 모두 확인한다.

## 패키지 무결성

- 패키지 파일: 266개
- 내부 `SHA256SUMS.txt` 항목: 265개(매니페스트 자신 제외)
- 압축 해제 후 내부 매니페스트 검증: missing 0 / mismatch 0
- ZIP↔폴더 비교: 266/266, missing 0 / extra 0 / mismatch 0
- 압축 해제 후보 숨김 실행: 6.123초 생존·응답, 조기 종료 없음, 종료 후 잔존 프로세스 0
- 검사 전후 원본 폴더·ZIP 및 실행된 해제본 payload: 불변
- Development player LocalLow JSONL: 경로 공개, 삭제하지 않음, 동일 실행 로그를 증거 폴더로 복사
- Development LocalLow JSONL: 허용 경로, 이번 smoke 신규 생성, 생성·수정 시각 범위, 5줄 JSON 유효성, 원본/복사본 9,863 bytes 및 SHA-256 `c597f4841561acccc1aa4f4f8b690b6e2f2f405333de05ac8784ddba4f15b318` 일치
- 구조화된 증거: `Artifacts/ParallelQA/pkg_2c9f36a_verified/gamejam-package-integrity-summary.json` — `PKG-I01~I06` PASS, exit 0

검증 러너 커밋은 게임 후보 소스 이후 QA 러너·Forge·문서만 변경한다. `3d64403..2c9f36a` 사이에는 `Assets/_Project` 런타임·아트 파일 변경이 없으며, 패키지 게이트가 BUILD-INFO의 게임 소스와 현재 QA HEAD를 분리해 기록했다.

## 자동 증거

- Wave B: `Artifacts/ParallelQA/20260827T190000Z_gamejam_waveb_3d64403` — GREEN, 9/9
- Wave C: `Artifacts/ParallelQA/20260827T180000Z_gamejam_wavec_3d64403` — GREEN, 14/14
- 장기 체류 엔딩: `Artifacts/ParallelQA/20260827T200000Z_gamejam_longstay_3d64403` — GREEN, 15/15
- 보강 Wave 17: `Artifacts/ParallelQA/finalqa_9263705_wave17` — GREEN, Wave 16 기준 17/17, ending catalog 21/21
- 보강 장기 체류: `Artifacts/ParallelQA/finalqa_9263705_longstay` — GREEN, 15/15, aggregate exit 0
- 최종 ZIP 무결성: `Artifacts/ParallelQA/pkg_2c9f36a_verified` — PASS, `PKG-I01~I06` 6/6
- 컴파일: 오류 0 / 경고 0

제출 최종 승인은 `GJC-12`, `GJC-17`, `GJC-20`, `GJC-23` 수기 검증 후에만 가능하다. Steam 출시 준비 상태는 이 게임잼 후보가 주장하지 않는다.

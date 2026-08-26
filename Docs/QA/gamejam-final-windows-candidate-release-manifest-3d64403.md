# GAME JAM Windows 후보 배포 매니페스트

## 후보 식별자

- 게임 소스 커밋: `3d64403813493d8de8e05f0844a5e616e164c6d4`
- 패키지에 포함된 체크리스트 커밋: `7678ab25ddb67af8087faecfb224d61c440459e0`
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
- 압축 해제 후보 숨김 실행: 6초 이상 생존·응답, 조기 종료 없음

## 자동 증거

- Wave B: `Artifacts/ParallelQA/20260827T190000Z_gamejam_waveb_3d64403` — GREEN, 9/9
- Wave C: `Artifacts/ParallelQA/20260827T180000Z_gamejam_wavec_3d64403` — GREEN, 14/14
- 장기 체류 엔딩: `Artifacts/ParallelQA/20260827T200000Z_gamejam_longstay_3d64403` — GREEN, 15/15
- 컴파일: 오류 0 / 경고 0

제출 최종 승인은 `GJC-12`, `GJC-17`, `GJC-20`, `GJC-23` 수기 검증 후에만 가능하다. Steam 출시 준비 상태는 이 게임잼 후보가 주장하지 않는다.

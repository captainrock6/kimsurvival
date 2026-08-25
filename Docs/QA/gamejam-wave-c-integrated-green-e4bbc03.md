# GAME JAM Wave C 통합 GREEN 기록

## 후보 식별자

- 기준 커밋: `e4bbc03531d54e023f7a90f7a608871a47d26d55`
- 실행 ID: `20260826T160000Z_gamejam_wave_c_committed`
- 증거 루트: `Artifacts/ParallelQA/20260826T160000Z_gamejam_wave_c_committed`
- Unity: `6000.4.9f1`
- Windows 실행 파일: `work/ParallelQA/StableWindowsBuild/KimSurvivalIsland.exe`
- 실행 파일 SHA-256: `93c19f9e7c681845d34407807d33b6438e781dd34c4d8895ebdf2c6fb083711d`

## 자동 검증 판정

| 게이트 | 결과 |
|---|---|
| Wave C | 14/14 PASS, expected gap 0, fail 0 |
| Wave C product | PASS |
| Wave C infrastructure | PASS |
| 전체 | GREEN |
| 수색 node | 15/15 PASS |
| Wave 19 | 21/21 PASS |
| Wave 20 | 16/16 PASS |
| 컴파일 | 0 error / 0 warning |
| Windows Development build | PASS |
| hidden smoke | PASS, 6.271초, 응답 상태 확인 |
| Addressables·방화벽 | PASS |

Wave C는 보호 부품과 3/5 pity, 독립적인 뗏목·대형 연기·무전 탈출, 실패·취소·날씨 대기·재시도 원자성, ending/album 1회 기록, 같은 run 위층+지하실 저장 복원, KO/EN/qps-long core comic 3장+modifier 1장, grant·warp·skip 0인 28.98분 대표 합성 프로필을 통과했다.

## 인간 검증 경계

다음 항목은 자동화 GREEN에 포함하지 않는다.

- GJC-12: 실제 가방 4→6 투자 선택과 체감
- GJC-17: 실제 첫 loop 5~10분 및 대표 탈출 25~35분
- GJC-20: 물리 게임패드 실기
- GJC-23: KO 3명·EN 3명 첫 사용자 30분 세션

따라서 이 기록은 Wave C 자동화 GREEN 증거이며 인간 플레이테스트 완료 증거가 아니다. 후속 완료 매트릭스 감사에서 이 게이트가 GDD 필수 항목인 Day 20 게임잼 장기 체류 엔딩 2종을 포함하지 않았음을 확인했으므로, GJC-24 추가 게이트가 GREEN이 되기 전에는 전체 GAME JAM 자동 범위 완료 증거로 사용하지 않는다.

## 재실행

```powershell
& '.\Assets\Editor\ParallelQA\Invoke-GameJamWaveCRedFirstGate.ps1' -RunId '<NEW_RUN_ID>' -BaselineCommit 'e4bbc03531d54e023f7a90f7a608871a47d26d55' -MinimumSmokeSeconds 6
```

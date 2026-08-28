# O11 Review Candidate 검증 보고서

- 기록일: 2026-08-29
- 빌드 소스: `d5f25e33dcb785bf81edeaf2bd3b95d98f91be63`
- Unity: `6000.4.9f1`
- 대상: Windows x64, `BuildOptions.None`, Development 아님
- 분류: 사용자 아트 채택 전 리뷰 후보

## 제품 완료 조건

| ID | 결과 | 실제 관찰 |
|---|---|---|
| `O10-H1-P0-001` | PASS | 작업대·빗물받이·침대·소파의 시작층·2층·지하 12개 배치 조합, 방/통로/충돌 거부, 저장 좌표 계약 |
| `O10-H1-P0-002` | PASS | 뗏목 불가능·중복 행동 zero-delta, 가능 행동 단일 원자 완료 |
| `O10-H1-P1-001` | PASS | 2층·지하 증축 반응 후 idle/move 복귀 |
| `O10-H1-P1-002` | PASS | V2 런타임 UI, KO/EN 1280×800·1920×1080 네 레이아웃 |
| `O10-H1-P1-003` | PASS | 연속 수색 3회와 선택 가능한 회복 행동 |
| `O10-H1-P1-004` | PASS | 세 탈출 경로, 대표 seed 3개, 25~35분 부담 범위 |
| `O10-H1-P1-005` | BLOCKED | 김씨 5상태와 7지역/6노드 런타임 연결은 완료. 지역·노드 아트가 아직 `review/provisional`이므로 production 채택은 미완료 |

총 제품 판정은 `6 PASS / 0 EXPECTED_GAP / 1 FAIL`이며, 실패 원인은 아트 채택 경계 한 건이다.

## 기술 검증

- 깨끗한 worktree 컴파일: PASS, 오류 0건, O11 소유 경고 0건
- Windows 비개발 빌드: PASS, 오류 0건, 경고 0건, 150,468,161 bytes
- 실행 파일 SHA-256: `a197542ad0d026c5c3bc7aead606b6b0184adad7b4ee3635326c575b25a5b423`
- 원본 빌드 숨김 실행: PASS, 1280×800 창 모드, 6초 이상 생존·응답
- ZIP 압축 해제 후 해시 대조: PASS
- 압축 해제본 숨김 실행: PASS, 1280×800 창 모드, 6초 이상 생존·응답
- ZIP SHA-256: `91bd1df04e5198fa7fd069a431e82befa8e9b6f478a8e9ebfcd74c8f5b73397d`

## 산출물

- 실행 폴더: `work/O11FinalCandidate/Builds/O11ReviewCandidate-d5f25e3/`
- ZIP: `work/O11FinalCandidate/Builds/KimsSurvivalIsland-O11ReviewCandidate-d5f25e3-win64.zip`
- SHA 파일: `work/O11FinalCandidate/Builds/KimsSurvivalIsland-O11ReviewCandidate-d5f25e3-win64.zip.sha256`
- O11 제품 증거: `work/O11FinalCandidate/Artifacts/ParallelQA/O11_20260829T035000Z_clean_readiness/`
- 빌드·스모크 증거: `work/O11FinalCandidate/Artifacts/ParallelQA/O11_20260829T040000Z_windows_review_candidate/`

## 남은 승인 게이트

7지역 배경과 6종 수색 노드 세트를 사용자가 채택하면 Forge 원장·패키지 허용 목록을 갱신하고 `O11-P1-005`를 재검증한다. 사다리·수영 포즈는 동작 연결은 되어 있으나 제출 품질 전 추가 폴리시가 필요하다. 물리 게임패드는 인간 검증 전까지 `UNVERIFIED`로 유지한다.

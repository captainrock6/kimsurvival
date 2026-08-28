# O10 후보 빌드 게이트

> 기준일: 2026-08-28
>
> Unity: `6000.4.9f1`
>
> 자동 게이트: `GREEN`
>
> 제출 판정: `HUMAN PLAYTEST + ART ADOPTION PENDING`

## 라이선스와 컴파일

- Unity LicensingClient가 `Unity Personal / Assigned / Unlimited` entitlement를 반환했다.
- runtime 및 editor assembly 컴파일이 오류 없이 완료됐다.
- 기존 라이선스 차단은 해제됐다. 로그 초반의 access-token 갱신 경고 뒤 entitlement가 정상 해석되고 Editor가 계속 실행된다.

## O10 전용 실행 검증

현재 공간형 캠프 계약을 검증하는 `ParallelQA.O10CandidateGateRunner`를 추가했다. 구형 전역 제작 대시보드를 전제로 하는 테스트와 분리했다.

- 폭풍 표류, 도입 5비트, 7지역, 19아이템, 플레이 가능 탈출 3경로, 핵심 엔딩 5종 계약: PASS
- 배치 실행 시 공간형 캠프와 첫 목표 표시: PASS
- 한국어↔영어 즉시 전환: PASS
- 단순화된 상단 HUD KO/EN/qps-long TMP overflow: PASS
- KO 1280×800 캠프 캡처: PASS
- KO 1280×800 타이틀 캡처: PASS
- 자동으로 중앙을 덮던 생존 도움말을 제거하고 명시적 도움말 버튼으로 전환: PASS

증거:

- `Artifacts/ParallelQA/20260828T124000Z_o10_candidate/o10-edit-contracts.txt`
- `Artifacts/ParallelQA/20260828T124000Z_o10_candidate/o10-playmode-result.txt`
- `Artifacts/ParallelQA/20260828T124000Z_o10_candidate/o10-camp-ko-1280x800.png`
- `Artifacts/ParallelQA/20260828T124000Z_o10_candidate/o10-title-ko-1280x800.png`

## 구형 테스트 해석

`ParallelQaRunner`의 언어 전환 검사는 닫힌 팝업 제목도 즉시 현지화하도록 보완해 통과했다. 이후 검사는 캠프 제작 버튼이 전역에서 항상 활성이고 세 탈출 경로 이름·진행도가 시작 화면에 계속 보여야 한다고 가정한다. 이는 사용자가 확정한 “설비에 직접 접근해 팝업을 열고, 탈출 설비는 만든 뒤 나타나며, 텍스트를 상시 표시하지 않는다”는 현재 계약과 충돌하므로 O10 합격 게이트로 사용하지 않았다.

Unity 6의 `-nographics` 모드에서 `Camera.Render()` 캡처가 네이티브 그래픽 크래시를 일으켜 실제 스크린샷 검증은 숨김 GPU 배치 모드로 실행했다. GPU 모드의 동일 Play Mode 검증은 정상 종료했다.

## Windows 후보

- 폴더: `Builds/O10Candidate-20260828T124000Z`
- 실행 파일: `KimsSurvivalIsland.exe`
- BuildPipeline: `Succeeded`, 오류 0, 경고 0
- 실행 파일 SHA-256: `a197542ad0d026c5c3bc7aead606b6b0184adad7b4ee3635326c575b25a5b423`
- 압축 파일: `Builds/KimsSurvivalIsland-O10Candidate-20260828T124000Z.zip`
- 압축 파일 SHA-256: `9d7023332ee20cf9360f0109ca211843ec3921e9971b302a27e19c6d3248209b`
- Windows player 숨김 실행: 6초 동안 정상 생존, 즉시 크래시·managed exception 없음

## 남은 사람 검증

자동 후보 빌드는 완료됐지만 다음은 사람의 관찰 없이는 완료로 주장하지 않는다.

1. 첫 5~10분 무설명 캠프→지도→수색→선별→귀환→첫 투자 이해도.
2. 약 30분 자연 플레이에서 뗏목·대형 연기·무전 중 한 경로 완주 가능성.
3. 정확 후보 빌드의 물리 게임패드 조작.
4. 스타일 벤치마크 V2의 `ADOPT / REVISE / HOLD` 판정.

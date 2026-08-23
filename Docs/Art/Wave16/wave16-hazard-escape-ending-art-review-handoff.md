# Wave 16 위험·탈출·엔딩 아트 리뷰 handoff

이 패키지는 `Docs/Design/wave15-escape-hazard-ending-matrix.md`와 `.forge/packets/wave15-fifty-day-campaign-rebaseline.json`의 1차 smoke 범위를 시각화한다. 모든 후보는 사용자 선택 전까지 `review`이며 `selectedCandidate=null`, runtime allowlist 빈 상태다.

## 안정 후보 ID

- `effect.survival-hazards.phase-silhouette-a` (`job_20260823160305_ef04b0f3`): 부상·폭우/폭풍·식량 도난의 예고→발생→완화→회복 12셀
- `ui.escape-project-progress.route-signature-a` (`job_20260823160324_1de3b748`): smoke/radio 플레이 경로와 raft/flare/beacon 데이터 경로를 서로 다른 실루엣으로 분리한 상황형 패널
- `ui.ending-comic.triptych-a` (`job_20260823160342_eceb3933`): setup→close-up→punchline의 3장 placeholder와 판정 근거 영역

세 작업 모두 Forge 품질 100점, 오류 0, 경고 0으로 통과했다.

## 공통 시각·입력 계약

- 채택된 수집 지도 A의 다크 네이비·틸·크림, 따뜻한 오렌지와 굵은 외곽선을 계승한다.
- 실제 ko/en 본문은 비트맵에 굽지 않고 TMP 영역으로 분리한다.
- qps-long은 150% 팽창, 줄바꿈 후 세로 재배치, 1280×800 최소 18px를 기준으로 한다.
- 키보드·마우스와 게임패드는 같은 44×44px 최소 포커스/입력 glyph 슬롯을 사용한다.
- 상태는 색 외에도 점선·톱니·방패·원형 회복, 경로 실루엣, 프레임 모서리·패턴으로 구분한다.

## 승인 게이트

품질 점수가 높아도 이번 산출물은 자동 채택하지 않는다. `feedback adopted`, `variants`, `package`, runtime/scene 연결은 사용자가 정확한 안정 후보 ID를 선택한 다음 작업으로 남긴다.

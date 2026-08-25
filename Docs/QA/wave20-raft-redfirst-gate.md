# Wave 20 뗏목 탈출 RED-first 독립 QA 게이트

## 판정

- 브랜치 기준: `origin/master` `09ae2a6d578eb4dcbf11b9c571f57f640b88d969`
- 최종 RunId: `20260825T040000Z_09ae2a6_wave20_hardened`
- 전체 / 제품 / 인프라: **RED / RED_EXPECTED_GAP / PASS**
- Wave 18 회귀 잠금: **23/23 PASS**
- Wave 19 회귀 잠금: **21/21 PASS**
- Wave 20 자동 제품 판정: **2 PASS / 14 EXPECTED_GAP / 0 예상 밖 FAIL**
- 물리 게임패드: **UNVERIFIED** — 연결 장치와 사람 실기 증거 없음
- Steam: **NOT_READY** — App ID, depot, Steam Input, Cloud, achievements, 파트너 권한 증거 없음

현재 기준선의 `escape.raft`는 카탈로그·엔딩 데이터만 존재하는 `data-only not playable` 상태다. 이 게이트는 그 상태를 PASS로 꾸미지 않고 RED로 고정한다. 제품 구현 후에는 기준 SHA만 새 통합 SHA로 바꾸어 같은 러너를 실행하며, 모든 `EXPECTED_GAP/FAIL`이 0일 때만 GREEN이다.

## 실행 명령

Unity 라이선싱 문서에 따라 이 명령은 Codex 샌드박스 밖에서 실행한다. `-noUpm` 우회는 사용하지 않는다.

```powershell
& '.\Assets\Editor\ParallelQA\Invoke-Wave20RaftRedFirstGate.ps1' `
  -RunId '<NEW_RUN_ID>' `
  -BaselineCommit '09ae2a6d578eb4dcbf11b9c571f57f640b88d969' `
  -MinimumSmokeSeconds 6
```

종료 코드는 `0=GREEN`, `2=제품 RED`, `1=인프라 FAIL`이다. 현재 기준선의 정상 결과는 `2`다. 진입점은 Windows PowerShell 5.1과 PowerShell 7에서 UTF-8 no-BOM 요약을 유지한다.

## 자동 판정 행렬

| ID | 심각도 | 기준선 | 독립 판정 계약 |
|---|---:|---|---|
| W20-E01 | P0 | PASS | `escape.raft`, 해변/얕은 바다, `facility.shore-launch`, `part.raft.sailcloth`, 재료·위험·창·완료·엔딩 stable ID 보존 |
| W20-E02 | P0 | EXPECTED_GAP | data-only 해제 및 선체→돛→항해 보급 3단계 공개 상태 |
| W20-E03 | P0 | EXPECTED_GAP | sailcloth 보호, 날씨·조류, 취소·실패·중복, 저장·복구 공개 계약 |
| W20-E04 | P1 | EXPECTED_GAP | ko/en/qps-long 진수대·프롬프트·팝업·단계·창·확인/취소 키와 값 |
| W20-E05 | P0 | PASS | 조기 `escape.raft`가 `ending.escape.raft.open-water`로 결정되고 앨범 unlock/duplicate/restore가 1/0/1 |
| W20-P01 | P0 | EXPECTED_GAP | 원거리 0, 근거리 1, 소형 프롬프트, Interact 팝업, Cancel 동일 대상 복귀 |
| W20-P02 | P0 | EXPECTED_GAP | 실제 Play 상호작용 trace의 선체→돛→보급 순서 및 sailcloth 보호 |
| W20-P03 | P0 | EXPECTED_GAP | 불안전 날씨·조류 거절과 허용 창 출항 |
| W20-P04 | P0 | EXPECTED_GAP | 실패·취소 자원/단계 불변, 동일 실패 결과 1회 적용 |
| W20-P05 | P0 | EXPECTED_GAP | 비용 정확히 1회, 중복 Submit 비용 delta 0, 중복 terminal delta 0 |
| W20-P06 | P0 | EXPECTED_GAP | Day 50 이전 실제 뗏목 탈출이 Day 50 미탈출 엔딩보다 우선 |
| W20-P07 | P0 | EXPECTED_GAP | 단계·보호 부품·창·거래 ID 저장/복구 동일성 |
| W20-P08 | P0 | EXPECTED_GAP | 실제 자연 탈출 뒤 raft ending/앨범 unlock 1회 및 복구 |
| W20-P09 | P0 | EXPECTED_GAP | 실제 `escape.raft` route, interaction trace, `grant=false`, `warp=false`, `skip=false` |
| W20-P10 | P1 | EXPECTED_GAP | ko/en/qps-long near/popup 6장, 1280×800 overflow/offscreen 0, 프롬프트 512×50 이하 |
| W20-P11 | P1 | EXPECTED_GAP | 키보드·마우스/합성 게임패드 glyph는 달라도 대상·행동 의미 동일 |
| W20-U01 | P1 | UNVERIFIED | 물리 게임패드 전 경로 사람 실기 |
| W20-U02 | P0 | NOT_READY | Steam 외부 출시 설정과 파트너 증거 |

## 기준선에서 재현된 제품 결손

1. **P0 — data-only 상태**: `escape.raft`의 `PlayableState`가 `data-only not playable`, `RequiredProgress=0`이다.
   - 재현: Edit 계약 E02에서 stable ID로 카탈로그를 조회한다.
   - 영향: 플레이어가 뗏목 프로젝트를 시작하거나 단계 진행할 수 없다.
   - 권장 수정 소유자: 런타임 escape project catalog/state 구현.

2. **P0 — 진수대 대상 부재**: 실제 캠프 대상 12개에 `facility.shore-launch`가 없고 smoke/radio만 있다.
   - 재현: Play 계약 P01에서 캠프 target list를 stable ID로 열거한다.
   - 영향: 근접 안내·팝업·취소 복귀·입력 경로와 캡처를 시작할 수 없다.
   - 권장 수정 소유자: 캠프 대상 등록·진수대 팝업/표시 구현.

3. **P0 — 잘못된 자연 경로 fallback**: live observer에 `escape.raft`를 전달해도 결과의 `EscapeId`가 `escape.smoke`이고 연기 신호 2단계 trace가 반환된다.
   - 재현: P09가 공개 결과 필드와 interaction trace를 독립 검사한다.
   - 영향: 정적 fixture가 뗏목 성공으로 오인될 수 있고 조기 탈출·엔딩 검증이 무효다.
   - 권장 수정 소유자: 실제 raft natural-route 분기와 stable interaction trace 구현.

4. **P0 — 단계·창·원자성·복구 증거 부재**: 선체/돛/보급, sailcloth 보호, 날씨/조류 거절·허용, 실패/취소, 중복 비용/terminal, 저장 복구가 공개 Play 상태나 trace에 없다.
   - 재현: P02–P08을 한 route snapshot에서 각각 분리 판정한다.
   - 영향: 비용 중복, softlock, 잘못된 출항, 저장 손실, 중복 엔딩 가능성을 막을 회귀 보호가 없다.
   - 권장 수정 소유자: raft 단계/거래/forecast-current/save/terminal owners.

5. **P1 — 현지화·레이아웃·입력 표면 부재**: 현재 문자열 표에는 raft catalog/ending 10행만 있고 진수대·단계·행동 키가 없다. 실제 진수대 UI가 없어서 ko/en/qps-long 6장과 입력 parity도 0개다.
   - 재현: E04 키 스캔 및 P10/P11 실제 UI 계측.
   - 영향: 구현이 들어와도 누락 번역, qps-long overflow, 장치별 의미 불일치를 놓칠 수 있다.
   - 권장 수정 소유자: localization table 및 raft prompt/popup presentation owners.

## 인프라 회귀 잠금

- Unity `6000.4.9f1`: compile **0 errors / 0 warnings**
- Windows x64 Development build: **PASS**, warnings **0**, executable SHA-256 `93c19f9e7c681845d34407807d33b6438e781dd34c4d8895ebdf2c6fb083711d`
- 숨김 Windows Player smoke: **PASS 6.275초**, 최소 시점 alive/responding
- Addressables post-smoke link 계약: **PASS**
- 고정 `StableWindowsBuild` 실행 파일과 현재 worktree 인바운드 Block 규칙 일치: **PASS**
- raw `windows-player.log`: **PASS_QUARANTINED** — 커밋 대상 증거 폴더에 없음
- Wave 18: **23/23 PASS**; Wave 19: **21/21 PASS**

## GREEN 전환 조건

다음 조건을 모두 만족해야 한다.

1. E02–E04와 P01–P11이 실제 구현 baseline에서 모두 PASS한다.
2. 실제 Play route가 `escape.raft`를 반환하고 선체→돛→보급, sailcloth 보호, 창 거절/허용, 원자성/중복 방지를 독립 관찰할 수 있다.
3. 조기 탈출·저장 복구·raft ending/앨범 1회 해금이 같은 자연 경로에서 확인된다.
4. 실제 ko/en/qps-long near/popup 6장이 생성되고 overflow/offscreen이 0이며 작은 프롬프트 제한을 지킨다.
5. Wave 18 23/23, Wave 19 21/21, compile/build/smoke/Addressables/firewall이 계속 PASS한다.
6. 물리 게임패드와 Steam은 자동 GREEN과 분리해 각각 `UNVERIFIED`, `NOT_READY`를 유지한다.

## 증거

- 최종 요약: `Artifacts/ParallelQA/20260825T040000Z_09ae2a6_wave20_hardened/wave20-summary.json`
- Edit 판정/관찰: `wave20-edit-contracts.json`, `wave20-edit-observation-evidence.json`
- Play 판정/관찰: `wave20-play-contracts.json`, `wave20-play-observation-evidence.json`
- 선행 회귀: `wave19-summary.json`, `wave18-edit-contracts.json`, `wave18-play-contracts.json`
- 빌드·스모크·방화벽: `windows-development-build.json`, `windows-hidden-smoke.json`, `wave19-windows-firewall-contract.json`
- Addressables: `addressables-link-post-smoke-contract.json`
- PowerShell/인코딩: `wave20-powershell-compatibility.json`

증거 폴더의 각 JSON은 RunId와 baseline을 기록한다. 기존 `Artifacts/Verification`이나 과거 `Artifacts/ParallelQA` 파일은 덮어쓰지 않았다.

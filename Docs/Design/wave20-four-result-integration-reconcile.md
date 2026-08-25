# Wave 20 네 작업 통합 정리

## 현재 정본

- 게임잼 수색 지역은 최소 7개다.
- 위험 계약은 허기·부상·자연재해·식량 도난·캠프 피해·야생동물·벌레·위험 식물·질병을 포함한다.
- 엔딩 카탈로그는 21개 안정 ID를 기준으로 한다.
- 일반 자원과 탈출 핵심 부품의 주 획득 경로는 바닥 개별 줍기가 아니라 환경 수색 오브젝트를 뒤지고 발견물을 선별하는 방식이다.
- 같은 run의 수색 오브젝트 내용물, 남은 발견물, 고갈, 장벽과 영구 위험 상태는 stable node ID로 보존한다.

## 탈출 경로 상태

| 경로 | 현재 상태 | 핵심 획득·진행 방식 |
|---|---|---|
| `escape.raft` | PLAYABLE | 해안 수색 node에서 보호 돛천 발견 → 진수대에서 선체·돛·보급 → 날씨·조류 확인 → 출항·재시도 |
| `escape.smoke` | PLAYABLE | 수색 node에서 부싯돌과 대량 나무 확보 → 고정 연기 설비 단계 진행 → 기상 창 확인 |
| `escape.radio` | PLAYABLE | 서로 다른 지역 수색 node에서 망가진 무전기·전자기판·트랜지스터 확보 → 무전 설비 복구·송신 |
| `escape.flare` | DATA_ONLY | Wave 20 설계·red-first 계약만 유지; 다음 구현 파동에서 플레이 가능화 |
| `escape.beacon` | DATA_ONLY | Wave 20 설계·red-first 계약만 유지; 조명탄 GREEN 이후 플레이 가능화 |

## 아트 채택 경계

`escape.raft` 생성 후보 `escape.raft.shore-launch-a`는 review-only다. 기능 통합은 placeholder 표현으로 검증하며 사용자가 명시적으로 채택하기 전에는 runtime allowlist나 Unity 씬에 연결하지 않는다.

## 이전 Wave 20 문서 해석

`wave20-escape-expansion-design.md`의 6지역·7위험·19엔딩과 뗏목 read-only 문구는 당시 분기 기준선 설명이다. 이후 확정된 7지역·질병 포함 위험·21엔딩·환경 수색 오브젝트 계약과 이번 뗏목 런타임 통합이 현재 정본이며, 충돌할 때 이 문서와 `.forge/design/`을 우선한다.

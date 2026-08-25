# GAME JAM Wave B 통합 계획

기준 커밋은 `00e0d5a9df597ab4a9f54bff665291f367d40c92`이다. 이 문서는 병렬 작업 결과를 통합할 때 제품 범위를 축소하거나 개별 작업의 자체 PASS를 전체 PASS로 오인하지 않기 위한 디렉터 체크리스트다.

## 통합 순서

1. 기획 계약을 먼저 병합하고 7/21/42/144 합계와 stable ID 중복 0을 독립 파싱한다.
2. 아트 결과는 review registry와 미리보기만 병합한다. `selectedCandidate=null`, `runtimeAllowlist=[]`, `runtimeConnectAllowed=false`, 기존 adopted GUID 불변을 확인한다.
3. 독립 QA의 red-first 러너를 병합해 기준 커밋에서 제품 RED, 인프라 PASS가 분리되는지 확인한다.
4. Unity 제품 구현을 마지막에 병합하고 동일 러너를 fresh RunId로 다시 실행한다.
5. GREEN 증거와 현재 완료 매트릭스만 갱신한 뒤 통합 브랜치를 push한다.

## Wave B 제품 완료 조건

- 정확히 7개 region, 21개 archetype, 42개 stable instance가 한 live catalog에서 로드된다.
- 각 region은 3 archetype, 각 archetype은 2 instance를 가진다.
- 일반 자원 총량은 BALANCE_PROVISIONAL 144이며 보호 핵심 부품은 일반 총량과 분리된다.
- 같은 run seed와 revision은 내용물·배치 fingerprint가 같고 다른 seed는 stable ID를 바꾸지 않은 채 하나 이상 변주한다.
- hidden, partial, depleted, 알려진 잔여물, 부순 장벽과 영구 제거 위험이 귀환·강제 귀환·재방문·snapshot 복원에서 유지된다.
- 새 게임만 stock과 지역 상태를 새 seed로 초기화한다.
- 질병 한 종류가 실제 자연 경로에서 telegraph→exposure→effect→worsen→mitigate/treat를 순서대로 보인다.
- 질병 비용·위험·치료는 성공 transaction에 한 번만 적용되며 취소와 강제 귀환은 중복 적용하지 않는다.
- KO/EN/qps-long 1280×800과 keyboard/mouse·synthetic gamepad가 동일 stable action/result 의미를 낸다.

## 회귀 잠금

- 환경 수색 node `15/15`.
- Wave 19 기존 리소스·엔딩·입력 `21/21`.
- Wave 20 뗏목 `16/16`.
- 컴파일 오류·경고 `0/0`.
- Windows Development build, hidden smoke, Addressables, 고정 경로 방화벽 `PASS`.
- 기존 채택 Wood/Stone/Food/Salvage GUID가 실제 compact tray에서 유지된다.
- review-only 아트는 어떤 제품 PASS의 근거로도 사용하지 않는다.

## 중단 조건

- 7/21/42/144 중 하나라도 실제 live 데이터가 아닌 fixture·문자열 assertion으로만 존재한다.
- 제품 실패가 인프라 실패로 분류되거나 반대로 섞인다.
- grant, warp, skip, fixture-only 경로가 자연 플레이 증거에 포함된다.
- 수색 잔량·보호 부품·장벽·영구 위험·질병 상태가 재방문에서 재생성되거나 손실된다.
- 병렬 결과가 기존 사용자 변경이나 untracked 증거를 삭제한다.

물리 게임패드와 첫 사용자 세션은 이 자동 GREEN으로 완료 처리하지 않는다.

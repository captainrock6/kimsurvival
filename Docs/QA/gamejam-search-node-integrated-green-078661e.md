# GameJam 환경 수색 node 통합 GREEN — 078661e

## 판정

- 통합 기준: `078661e653851802aa97d86fd691337411ac345c`
- RunId: `20260826T053000Z_gamejam_search_node_integrated_green`
- 전체 / 제품 / 인프라: `GREEN / PASS / PASS`
- 환경 수색 node: `15/15 PASS`, 예상 공백 `0`, 실패 `0`
- Wave 19 기존 리소스·엔딩·입력 회귀: `21/21 PASS`
- Wave 20 뗏목 제작·출항 회귀: `16/16 PASS`
- Unity 컴파일: 오류 `0`, 경고 `0`
- Windows x64 Development build / hidden smoke: `PASS / PASS`
- Addressables / 고정 경로 방화벽 Block: `PASS / PASS`
- 물리 게임패드: `UNVERIFIED`
- Steamworks: `NOT_READY`

## 이번 GREEN으로 닫힌 제품 계약

- 일곱 지역과 안정 region/node ID 카탈로그
- run seed 기반 구조화 발견물 결정성과 다른 seed 변주
- 미수색·부분 잔류·고갈 상태 및 남은 발견물 저장·복원
- 부순 장벽과 영구 제거 위험의 지역 상태 저장·복원
- 담기·남기기·교체·교체 취소의 원자적 수량 보존
- 수색 비용·위험 1회 적용과 발견물 선택 중 위험 진행 정지
- 보호 돛천의 폐기·복제·중복 소비 방지 및 뗏목 경로 연결
- 한국어·영어·qps-long 1280×800 compact 트레이 레이아웃
- 키보드·마우스와 합성 게임패드의 동일 node/action/focus 의미
- 기존 채택 Wood/Stone/Food/Salvage 아이콘의 실제 발견물 트레이 연결
- grant·warp·skip 없는 실제 Scene owner와 자연 이동·상호작용 trace

## 직접 시각 확인

다음 세 화면을 원본 해상도로 확인했다. 세 locale 모두 오버플로와 화면 밖 이탈이 없고, compact 트레이는 캐릭터와 핵심 보행 영역을 가리지 않는다. 한국어·영어 화면에서 음식과 표류물 아이콘이 실제 발견물 버튼에 연결되어 있으며, qps-long은 의도한 장문 스트레스에서도 트레이와 가방 슬롯이 화면 안에 유지된다.

- `Artifacts/ParallelQA/20260826T053000Z_gamejam_search_node_integrated_green/kim-survival-search-tray-ko-1280x800.png`
- `Artifacts/ParallelQA/20260826T053000Z_gamejam_search_node_integrated_green/kim-survival-search-tray-en-1280x800.png`
- `Artifacts/ParallelQA/20260826T053000Z_gamejam_search_node_integrated_green/kim-survival-search-tray-qps-long-1280x800.png`

## 남은 제출 범위

이번 통합은 GAME JAM Wave A와 수색·지역 영속성의 자동 제품 게이트를 닫는다. 전체 제출 완료를 주장하려면 질병의 예고→노출→효과→치료 자연 경로, 같은 run의 2층+지하실 완주, 연기·무전 탈출의 보호 부품 자연 획득, 25~35분 대표 완주와 코믹북 패널, 물리 게임패드 및 첫 사용자 세션을 계속 검증해야 한다. review 상태의 신규 수색 node·loot tray 아트는 명시적 채택 전까지 런타임에 연결하지 않는다.

## 증거

- `Artifacts/ParallelQA/20260826T053000Z_gamejam_search_node_integrated_green/gamejam-search-node-summary.json`
- `Artifacts/ParallelQA/20260826T053000Z_gamejam_search_node_integrated_green/gamejam-search-node-edit-observation-evidence.json`
- `Artifacts/ParallelQA/20260826T053000Z_gamejam_search_node_integrated_green/gamejam-search-node-play-observation-evidence.json`
- `Artifacts/ParallelQA/20260826T053000Z_gamejam_search_node_integrated_green/wave19-summary.json`
- `Artifacts/ParallelQA/20260826T053000Z_gamejam_search_node_integrated_green/wave20-summary.json`
- `Artifacts/ParallelQA/20260826T053000Z_gamejam_search_node_integrated_green/compile-result.txt`
- `Artifacts/ParallelQA/20260826T053000Z_gamejam_search_node_integrated_green/windows-development-build.json`
- `Artifacts/ParallelQA/20260826T053000Z_gamejam_search_node_integrated_green/windows-hidden-smoke.json`
- `Artifacts/ParallelQA/20260826T053000Z_gamejam_search_node_integrated_green/addressables-link-post-smoke-contract.json`
- `Artifacts/ParallelQA/20260826T053000Z_gamejam_search_node_integrated_green/wave19-windows-firewall-contract.json`

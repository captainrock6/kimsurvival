# Wave 15 탈출·위험·엔딩 매트릭스

- 상태: `DESIGN CONTRACT COMPLETE / IMPLEMENTATION UNRUN`
- 기준: `origin/master@7796cf57568d0bad24595379e833e1dd9b4d8d3f`
- 기계 정본: `.forge/packets/wave15-fifty-day-campaign-rebaseline.json`의 `project.campaignContentContract`
- 밸런스 상태: `SAMPLE_ONLY_NOT_FINAL_FIFTY_DAY_BALANCE`

이 문서는 50일 캠페인의 콘텐츠 데이터 계약이다. 탈출법·지역·위험·엔딩의 ID와 인과관계는 구현 기준으로 고정하지만, 준비 일수·풍부함·pity 횟수·위험 예산·행동 점수 임계값은 smoke용 표본이다. 실제 50일 플레이테스트 전에는 이를 정식 비용·드롭·소모로 인용하지 않는다.

## 1. 탈출 method

| ID | 판타지 | 핵심 지역 | 연구 | 설비 | 핵심 재료 범주·부품 | 준비 기간 | 주요 위험 | 실패·변형 사건 | 분리 축 |
|---|---|---|---|---|---|---|---|---|---|
| `escape.raft` | 직접 만든 뗏목·소형 범선으로 조류를 타고 탈출 | 해변·얕은 바다·난파선 만 | 밧줄 공법·연안 항해 | `facility.shore-launch` | 나무·섬유·천·식량·물, `part.raft.sailcloth` | 6~10일 표본 | 파도·부상·굶주림 | 출항 연기, 표류 귀환, 선체 손상 / 코코넛 평형추, 안전 조류 | 해안 지역, 항해 연구, 출항 앵커, 날씨 창, 항해 보급 |
| `escape.smoke` | 고지대의 대형 연기를 며칠 유지해 배·항공기에 발견 | 숲·고지대 | 신호 연소·풍향 읽기 | `facility.smoke-beacon` | 나무·섬유·연료·천, `part.smoke.catalyst` | 3~6일 표본 | 비·강풍·캠프 화재 | 빗물 소화, 역풍, 방화선 붕괴 / 바비큐 오해, 구름 글씨 | 고지대, 연소 연구, 고정 설비, 다일 연료, 풍우 |
| `escape.radio` | 난파선·폐시설 부품으로 무전기를 복구해 좌표 송신 | 난파선 만·폐중계소·고지대 | 전자공학·주파수 | `facility.radio-bench` | 전자부품·전선·배터리·금속, `part.radio.transceiver` | 8~14일 표본 | 습기·번개·오신호 | 거짓 응답, 합선, 안테나 이탈 / 무인도 DJ, 반복 응답 | 폐시설, 전자 연구, 전원 설비, 주파수, 습기·번개 |
| `escape.flare` | 짧은 구조 대상 노출 창에 조명탄 한 발을 정확히 발사 | 해변·얕은 바다·난파선 만 | 화공·신호 타이밍 | `facility.flare-launcher` | 화학재·금속·천·연료, `part.flare.cartridge` | 4~8일 표본 | 오발·화재·기상 | 불발, 목격자 없음, 발사기 손상 / 대낮 불꽃놀이, 완벽한 창 | 난파선 부품, 화공 연구, 일회 발사기, 목격 창, 오발 |
| `escape.beacon` | 산등성이 폐중계소의 발전기와 신호등을 복구 | 고지대·폐중계소·숲 | 구조 보수·발전기 수리 | `facility.relay-station` | 나무·돌·금속·전선·연료, `part.beacon.generator-coil` | 10~18일 표본 | 추락·폭풍·설비 파손 | 전원 강하, 계단 붕괴, 낙뢰 차단 / 과출력, 완전 재해 대응 | 후기 지역, 이중 연구, 고정 폐시설, 긴 단계, 폭풍 노출 |

각 method는 최소 두 축 이상 달라야 한다. 프로젝트 preview·취소는 자원을 바꾸지 않고, 단계 성공·실패·완성은 선언된 transaction 하나로만 적용한다. 실패는 완성된 다른 단계를 삭제하지 않으며 핵심 부품을 영구 소실시키지 않는다.

## 2. 지역 profile

풍부함은 정확한 수량이 아니라 지도에 노출하는 `풍부/보통/희귀` 범주다. 이동 시간도 우선 `short/medium/long` 범주로 구현한다.

| ID·해금 | 자원 전망 | 위험·날씨 | 필요 장비 | 이동 | 특별 발견 | 핵심 부품 후보·대체 지역 |
|---|---|---|---|---|---|---|
| `region.coast.beach` 시작 | 풍부: 나무·표류물 / 보통: 식량·천 / 희귀: 전자·화학 | 굶주림·부상·재해 / 맑음·비·높은 파도 | 없음 | short | 조수 웅덩이, 돛천, 표류 조명탄 | 돛천·조명탄 / 숲·난파선 만 |
| `region.forest.grove` 시작 | 풍부: 나무·섬유 / 보통: 식량·약품 / 희귀: 금속·전자 | 굶주림·질병·야생동물 / 맑음·폭염·비 | 돌도끼 | short | 약초 군락, 수지 나무, 오래된 덫길 | 연기 촉진제·돛천 / 고지대·해변 |
| `region.sea.shallows` 시작 | 풍부: 표류물·식량 / 보통: 금속·전선 / 희귀: 약품·연료 | 굶주림·부상·재해 / 조류·높은 파도 | 수영 준비 | medium | 잠긴 상자, 난파 지도, 구리선 | 무전기·발전 코일 / 난파선 만·폐중계소 |
| `region.ridge.highland` 확장 | 풍부: 돌·나무 / 보통: 연료·약품 / 희귀: 식량·전자 | 부상·재해·야생동물 / 강풍·낙뢰 | 밧줄·방수 장비 | long | 신호 시야, 기상 기록, 오래된 렌즈 | 연기 촉진제·발전 코일 / 숲·폐중계소 |
| `region.cove.wreck` 확장 | 풍부: 금속·표류물 / 보통: 전자·천·화학 / 희귀: 식량·나무 | 부상·질병·재해 / 조류·파도·비 | 수영 준비·밧줄 | long | 무전기 몸체, 조명탄 보관함, 기관 부품 | 무전기·조명탄·돛천 / 얕은 바다·해변·폐중계소 |
| `region.ruins.relay` 확장 | 풍부: 전자·전선 / 보통: 금속·연료 / 희귀: 식량·섬유 | 부상·설비 피해·재해 / 낙뢰·강풍·비 | 밧줄·절연 장비 | long | 송수신 코어, 발전 코일, 중계 기록 | 무전기·발전 코일 / 난파선 만·얕은 바다·고지대 |

### 핵심 부품 보호와 pity

- seed 생성 뒤 다섯 부품의 primary·alternative 배치를 검사해 최소 세 탈출 method가 완성 가능한지 확인한다. 실패하면 일반 자원이 아니라 핵심 부품 배치만 재추첨하고 이유를 기록한다.
- 획득한 핵심 부품은 보호 project inventory로 이동한다. 식량 도난과 일반 캠프 피해는 삭제할 수 없다. 특별 위험은 `damaged`로 바꿀 수 있지만 결정론적 수리 경로를 함께 제공해야 한다.
- 동일 부품 중복은 다른 미발견 부품의 pity를 초기화하지 않는다.
- `SAMPLE_ONLY`: 미보유 상태에서 primary 또는 alternative 지역의 완료 수색 3회면 대체 출처 힌트를 공개하고 가중치를 올린다. 5회면 다음 eligible 결과의 optional loot보다 먼저 해당 부품을 보장한다.

| 부품 ID | primary | alternatives |
|---|---|---|
| `part.raft.sailcloth` | 해변 | 난파선 만, 숲 |
| `part.smoke.catalyst` | 숲 | 고지대, 난파선 만 |
| `part.radio.transceiver` | 난파선 만 | 폐중계소, 얕은 바다 |
| `part.flare.cartridge` | 난파선 만 | 해변, 얕은 바다 |
| `part.beacon.generator-coil` | 폐중계소 | 얕은 바다, 고지대 |

## 3. 생존 위험

| ID | 예고 | 발생 | 완화 | 회복 | 행동 통계 |
|---|---|---|---|---|---|
| `hazard.hunger` | HUD 추세와 다음 정산 예상 | 정산 임계값에서 한 번만 상태 진행 | 먹기, 농사·보존식 준비, 짧은 원정 | 식량+휴식, critical 이탈 뒤 평온 정산 | 농사, 위험 대응 |
| `hazard.disease` | 지역 노출원과 잠복 표시 | 미처치 노출이 시간형 질병으로 진행 | 약품, 깨끗한 물·건조 장비, 지역 회피 | 치료와 재노출 없는 회복일 | 수색, 위험 대응 |
| `hazard.injury` | 위험 행동 전 지형·도구 표식 | 실패한 위험 행동이 명명된 부상 단계 적용 | 보호구, 밧줄·안전 경로, 행동 취소 | 치료, 휴식, 악화 행동 회피 | 수영, 위험 대응 |
| `hazard.disaster` | 지도·캠프의 주의→경고 | 폭풍·침수·폭염 중 예산 안의 결과 하나 | 지역 변경, 방수 장비, 캠프 보강 | 수리, 건조·평온일, 선언 소모품 교체 | 건설, 위험 대응 |
| `hazard.wildlife` | 발자국·울음·훼손 흔적 | 회피·위협·덫·후퇴 선택을 열고 전투 강제 없음 | 우회, 소리, 덫, 울타리 | 덫 재설치, 울타리 수리, 부상 치료 | 사냥·덫, 위험 대응 |
| `hazard.food-theft` | 다음 정산 전 발자국·저장고 흔적 | 보호되지 않은 식량 batch 하나만 원자 차감 | 보강 저장고, 덫, 보호 보관 | 회수 사건, 식량 보충, 잠금 수리 | 사냥·덫, 건설, 위험 대응 |
| `hazard.camp-damage` | 위협받는 설비와 피해 유형 명시 | 설비 하나만 working→damaged, 다른 설비·핵심 부품 불변 | 보강, 덮개, 예방 정비 | 원자 수리, 일반 재료 대체 | 기계공학, 건설, 위험 대응 |

### 중첩 예산과 원자성

- `SAMPLE_ONLY`: 일일 예산 4, minor/moderate/major 가중치 1/2/3, 새 major 최대 1, 동시 active 최대 2.
- critical hunger는 랜덤 roll이 아니지만 active 한 칸을 예약한다. major 재해(3)와 moderate 부상(2)은 같은 날 새로 발생할 수 없다.
- 한 침입에서 도난과 파손이 함께 보이면 하나의 `hazardInstanceId`가 예산과 손실을 소유한다. 두 개의 독립 차감을 만들지 않는다.
- major 결과 다음 날은 회복에 예산 2 이상을 예약하며 같은 family의 새 major를 금지한다.
- preview·cancel은 무변경이다. `resolve`는 상태·자원·행동 점수·로그를 idempotency key 하나로 원자 적용하고, retry·locale·입력 장치 전환으로 재적용되지 않는다.

## 4. 행동 점수와 엔딩 판정

안정 통계 ID는 `stat.swimming`, `stat.farming`, `stat.hunting-trapping`, `stat.mechanics`, `stat.building`, `stat.search`, `stat.hazard-response`다. 입력 횟수나 이동 시간을 그대로 세지 않고 완료된 의미 행동만 센다.

`SAMPLE_ONLY` 점수 계약:

- 의미 행동 1회는 1~2점, 통계별 하루 최대 4점이다.
- 첫 우세 후보는 누적 12점 이상이며 2위보다 4점 앞서야 한다.
- 이미 정해진 우세 생활 방식을 바꾸려면 새 후보가 6점 앞서야 한다. 단일 행동 최대 2점이므로 마지막 행동 하나로 뒤집을 수 없다.
- 탈출 프로젝트의 마지막 완료 버튼은 생활 방식 점수를 추가하지 않는다.

판정 순서:

1. terminal은 `escape_complete`가 `day50_settlement`보다 우선한다.
2. ending 후보는 `rare → comic → normal → Day50 lifestyle → Day50 fallback` 순이다.
3. 같은 단계 후보는 `priority 내림차순 → 충족 조건 수 내림차순 → 특별 사건 최초 day 오름차순 → ending ID ASCII 오름차순`으로 하나만 고른다.
4. core ending 뒤 우세 행동 modifier 최대 1장과 사건 scar modifier 최대 1장을 삽입할 수 있다. modifier는 `endingId`를 바꾸지 않는다.

## 5. 엔딩 19개

임계값 숫자는 모두 smoke 표본이다. `achievement.*`는 향후 매핑 키일 뿐 Steamworks 업적이 아니다.

| ID·분류·우선 | KO / EN 제목 | trigger·임계값 | KO / EN 요약 |
|---|---|---|---|
| `ending.escape.raft.open-water` 정상 100 | 수평선 너머로 / Beyond the Horizon | raft 완료, 상위 raft 없음 | 직접 엮은 뗏목으로 조류를 타고 탈출 / He rides the current off the island on his own raft. |
| `ending.escape.smoke.seen-from-afar` 정상 100 | 연기는 답을 안다 / Where There's Smoke | smoke 완료, 상위 smoke 없음 | 며칠 지킨 연기가 배를 부름 / The smoke he tended finally draws a ship. |
| `ending.escape.radio.clear-signal` 정상 100 | 여기는 김씨, 응답하라 / Kim Calling | radio 완료, 상위 radio 없음 | 잡음 속 한 문장이 위치를 세상에 알림 / One clear sentence puts him back on the map. |
| `ending.escape.flare.one-shot` 정상 100 | 딱 한 발 / One Good Shot | flare 완료, 상위 flare 없음 | 한 발을 정확한 순간에 발사 / He fires his only flare at the right moment. |
| `ending.escape.beacon.ridge-light` 정상 100 | 불이 켜진 산꼭대기 / The Light on the Ridge | beacon 완료, 상위 beacon 없음 | 폐중계소가 섬을 지도 위 점으로 만듦 / The restored relay makes the island visible again. |
| `ending.comic.raft.coconut-navy` 병맛 200 | 코코넛 해군 / Coconut Navy | 코코넛 평형추 사건 + farming≥8 | 코코넛 화물이 뗏목보다 먼저 유명해짐 / The coconut cargo becomes famous first. |
| `ending.comic.smoke.island-barbecue` 병맛 200 | 무인도 맛집 / Island Barbecue | 바비큐 오해 + farming 또는 hunting≥8 | 구조대가 맛집 연기 소문을 따라옴 / Rescuers follow rumors of legendary barbecue. |
| `ending.comic.radio.island-dj` 병맛 200 | 무인도 FM / Island FM | DJ 사건 + 성공 방송 3회 | 구조 요청 사이 선곡이 청취자를 만듦 / Songs between distress calls gain listeners. |
| `ending.comic.flare.daylight-fireworks` 병맛 200 | 대낮의 불꽃놀이 / Fireworks at Noon | 대낮 오발 뒤 유효 목격 | 축포로 오해한 배가 구경하러 옴 / A curious boat follows what looks like a celebration. |
| `ending.comic.beacon.brightest-address` 병맛 200 | 전기세 없는 야경 / The Brightest Address | 과출력 사건 + building≥8 | 중계소가 항로의 야경 명소가 됨 / The relay becomes a shipping landmark. |
| `ending.rare.raft.current-reader` 희귀 300 | 바다가 길을 열었다 / The Sea Made a Road | 안전 조류 + swimming≥12, hazard≥6 | 반복 수영으로 읽은 조류의 길을 탐 / He catches a current learned through countless swims. |
| `ending.rare.smoke.cloud-letter` 희귀 300 | 구름에 쓴 구조 요청 / SOS in the Clouds | 구름 글씨 + hazard≥12 | 연기가 완벽한 구조 문양을 그림 / Smoke draws a perfect distress sign. |
| `ending.rare.radio.forecast-rescue` 희귀 300 | 첫 응답은 기상청 / Forecast: Rescue | 반복 응답 + mechanics≥12, 방송 3회 | 기상망이 폭풍 사이 구조 항로를 계산 / A weather network calculates a rescue route. |
| `ending.rare.beacon.storm-eye` 희귀 300 | 폭풍의 눈에서 / Inside the Eye | 완전 재해 대응 + building·hazard≥10 | 폭풍의 고요 속에서 중계소를 켬 / He lights the relay inside the calm eye. |
| `ending.stay.green-king` Day50 50 | 김씨 농장 50일째 / Kim Farm, Day 50 | 미탈출 + farming 우세 | 탈출 대신 계절표가 필요한 밭을 만듦 / His farm grows large enough to need a calendar. |
| `ending.stay.fortress-manager` Day50 50 | 무인도 관리사무소 / Island Facilities Office | 미탈출 + building 또는 hazard 우세 | 폭풍보다 점검표가 무서운 관리자가 됨 / Checklists become scarier than storms. |
| `ending.stay.scrap-professor` Day50 50 | 표류물 공학 박사 / Doctor of Driftwood Engineering | 미탈출 + mechanics 또는 search 우세 | 쓸모없는 표류물은 없다는 이론을 증명 / He proves no salvage is useless. |
| `ending.stay.island-ranger` Day50 50 | 섬의 레인저 / Island Ranger | 미탈출 + swimming 또는 hunting 우세 | 물길과 동물 길을 모두 외움 / He learns every current and animal trail. |
| `ending.stay.just-kim` Day50 0 | 그냥 잘 사는 김씨 / Mr. Kim, Doing Fine | 미탈출 + 다른 Day50 후보 없음 | 한 전문가는 아니어도 자기 방식으로 생존 / He masters no single lifestyle but lives his own way. |

### 패널 beat·modifier·갤러리·업적 키

각 행은 3개 core beat에 modifier 0~2장을 삽입해 총 3~5장이 된다.

| ending 축약 | 3개 core panel beat | 기본 modifier | 갤러리 힌트 | achievement mapping ID |
|---|---|---|---|---|
| raft normal | 마지막 매듭 → 섬이 작아짐 → 구조선에서도 설계도 보존 | 우세 행동 | 바다는 벽이 아니라 길일지도 모른다. | `achievement.ending.raft.open-water` |
| smoke normal | 불씨 유지 → 항로가 방향 전환 → 구조선에서도 풍향 확인 | 우세 행동 | 멀리서도 보이는 하루를 만들어 보자. | `achievement.ending.smoke.seen-from-afar` |
| radio normal | 무전기 점등 → 좌표 응답 → 구조 뒤에도 먼저 호출 | 우세 행동 | 잡음도 오래 들으면 문장이 된다. | `achievement.ending.radio.clear-signal` |
| flare normal | 목격 창 확인 → 하늘을 가르는 빛 → 빈 발사기 보존 | 우세 행동 | 기회가 올 때까지 한 발을 아껴라. | `achievement.ending.flare.one-shot` |
| beacon normal | 마지막 케이블 → 밤바다 신호 → 고장 난 전등을 못 지나침 | 우세 행동 | 가장 높은 곳의 죽은 불을 깨워라. | `achievement.ending.beacon.ridge-light` |
| coconut navy | 코코넛 무한 결속 → 코코넛만 수평 → 구조대가 먼저 개수 확인 | farming | 배에는 평형추가 필요하다. | `achievement.ending.comic.raft.coconut-navy` |
| island barbecue | 맛있어 보이는 연료 → 접시 든 배 접근 → 메뉴판 요구 | 우세 행동 | 구조 신호가 너무 맛있어 보일 수도 있다. | `achievement.ending.comic.smoke.island-barbecue` |
| island FM | 잡음에 박자 → 선박 주파수 동조 → 구조 후 첫 방송 | mechanics | 응답이 없으면 방송이라도 해 보자. | `achievement.ending.comic.radio.island-dj` |
| noon fireworks | 해를 향해 발사 → 축제 오해 → 혼자 박수치며 구조 | 우세 행동 | 시간을 잘못 봐도 누군가는 볼 수 있다. | `achievement.ending.comic.flare.daylight-fireworks` |
| brightest address | 불필요한 전구 연결 → 밤바다가 대낮 → 선글라스 구조대 | building | 필요한 빛보다 조금 더 켜 보자. | `achievement.ending.comic.beacon.brightest-address` |
| current reader | 조류 변화 인지 → 고요한 물길 → 항해사가 해류도 대여 | swimming | 같은 물길을 오래 오가면 바다가 말한다. | `achievement.ending.rare.raft.current-reader` |
| cloud letter | 풍향마다 조정 → 구름 아래 문양 → 항공 사진의 작은 김씨 | hazard-response | 바람에 문장을 맡겨 보자. | `achievement.ending.rare.smoke.cloud-letter` |
| forecast rescue | 관측 주파수 응답 → 폭풍 사이 좌표 → 구조선에서 예보 | mechanics | 낡은 기록에는 아직 듣는 귀가 있다. | `achievement.ending.rare.radio.forecast-rescue` |
| storm eye | 캠프가 폭풍 버팀 → 폭풍 눈에서 점등 → 주소 재확인 | event scar | 가장 거센 날 모든 대비가 답한다. | `achievement.ending.rare.beacon.storm-eye` |
| green king | Day50 모종 → 캠프가 밭이 됨 → 신호대가 허수아비 | farming | 심는 계획이 떠나는 계획보다 많아진다면. | `achievement.ending.stay.green-king` |
| fortress manager | 점검 도장 → 폭우가 배수로로 → 관리사무소 영업중 | 우세 행동 | 섬도 관리 구역이 될 수 있다. | `achievement.ending.stay.fortress-manager` |
| scrap professor | 표류물 번호표 → 정체불명 기계 → 조개 박사모 | 우세 행동 | 주운 물건을 끝까지 연구해 보자. | `achievement.ending.stay.scrap-professor` |
| island ranger | 물길·발자국 판독 → 동물 순환로 → 순찰표 이상 없음 | 우세 행동 | 섬의 길을 사람보다 먼저 외워 보자. | `achievement.ending.stay.island-ranger` |
| just Kim | 다양한 도구 → 잠깐 고민 → 웃으며 저녁 준비 | 우세 행동 | 한 가지만 잘할 필요는 없다. | `achievement.ending.stay.just-kim` |

재사용 modifier ID는 `modifier.behavior.dominant` resolver와 실제 panel 8개다.

- `modifier.behavior.dominant`, `modifier.stat.swimming`, `modifier.stat.farming`, `modifier.stat.hunting-trapping`
- `modifier.stat.mechanics`, `modifier.stat.building`, `modifier.stat.search`
- `modifier.stat.hazard-response`, `modifier.event.scar`

화면에 표시되는 제목·요약·힌트 키는 각각 `{endingId}.title`, `{endingId}.summary`, `{endingId}.hint`를 사용한다. 한국어가 의미·코미디 기준 원문이고 영어는 직역보다 같은 상황·펀치라인·결과 원인을 보존한다. 향후 언어도 같은 key와 metadata를 사용한다.

## 6. 다음 구현용 smoke와 조정 필드

| 구분 | smoke에서 실행 | data-only 또는 보류 |
|---|---|---|
| 지역 | 해변·숲·얕은 바다 | 고지대·난파선 만·폐중계소는 catalog load |
| 탈출 | `escape.smoke`, `escape.radio` | raft·flare·beacon은 data validation |
| 위험 | 부상·재해·식량 도난 대표 instance | 나머지는 state catalog와 schema validation |
| 엔딩 | smoke normal, radio normal, island DJ, just Kim | 나머지 15개는 resolver catalog·localization·panel key validation |
| 아트·Steam | placeholder comic 3장, 게임 내 gallery | 최종 원화·음향·Steamworks 업적 없음 |

조정 가능 필드는 준비 일수, 자원 풍부함, 이동 시간 band, pity 3/5회, 위험 weight·일일 budget·동시 active, 행동 점수·cap·lead, 엔딩 threshold다. 구조 ID, primary 인과관계, 원자성, tie-break 순서와 achievement mapping ID는 수치 튜닝과 분리한다.

조정 규칙:

1. smoke 실행 전에는 표본값을 밸런스 PASS로 선언하지 않는다.
2. 기술 결함·발견성·정보 이해를 먼저 분리한다.
3. 플레이테스트에서 반복 원인이 확인되면 한 빌드에서 `지역 유입`, `탈출 준비`, `위험 압력`, `행동 임계값` 중 한 축만 바꾼다.
4. 같은 seed fixture와 ending snapshot을 전후 재실행한다.

## 7. 구현·QA 수용 조건

- catalog count는 method 5, region 6, hazard 7, ending 19, modifier 9이다. ID는 중복되지 않고 모든 참조가 존재한다.
- 같은 seed는 같은 key-part 배치와 hazard 선택을 만든다. seed 검증은 최소 세 탈출법의 primary+alternative chain을 보장한다.
- 위험 preview/cancel은 무변경이며 resolve/recovery는 서로 다른 idempotent transaction이다.
- 같은 ending snapshot은 항상 같은 `endingId`, 판정 근거, panel sequence와 achievement mapping ID를 반환한다.
- 정상 탈출 fallback 5개와 Day50 fallback `ending.stay.just-kim` 때문에 유효 terminal snapshot이 미판정으로 끝나지 않는다.
- ko/en 제목·요약·힌트와 panel key가 모두 존재하고 qps-long은 레이아웃 QA 전용이다.
- 물리 게임패드, 실제 사용자 50일 run, 최종 밸런스와 Steamworks는 실행 증거 없이는 PASS로 기록하지 않는다.

## 8. 열린 질문

현재 데이터 계약과 smoke 구현을 막는 열린 질문은 없다. 최종 50일 비용·드롭·위험 빈도와 custom run의 Steam 업적 인정 범위는 플레이테스트 또는 Steam 연동 단계의 후속 결정이며 현 작업의 blocker가 아니다.

# 《김씨 생존기: 무인도》 O9 콘텐츠 잠금

> 상태: `LOCKED_FOR_O9_O10_PRODUCTION`
>
> 기준일: 2026-08-28
>
> 사용자 확정: 조난 원인은 **폭풍 표류**, 아트 기준은 **기존 김씨의 굵은 잉크선을 유지하고 배경·UI를 단순화**한다.

## 1. 한 페이지 시놉시스

평범한 직장인 김씨는 섬 관광을 마치고 돌아오던 작은 여객선에서 갑작스러운 열대 폭풍을 만난다. 배는 밤새 파도에 휩쓸리고, 김씨는 구명 튜브와 회사 야유회 배낭 하나에 매달린 채 이름 모를 무인도 해변으로 떠밀려 온다. 휴대전화는 바닷물을 잔뜩 먹었고, 남은 배터리는 구조 요청이 아니라 `습기 감지 알림`을 띄우는 데 모두 쓴다.

김씨의 즉시 목표는 거창하지 않다. 오늘 밤을 버틸 지붕, 먹을 것, 마실 물을 마련하고 섬을 뒤져 쓸 만한 물건을 골라 캠프로 가져오는 것이다. 해변에서 표류물과 식량을 모은 뒤 작업대와 도구를 만들면 숲, 얕은 바다, 산등성이, 동굴, 난파선 만, 폐중계소로 수색 범위가 넓어진다. 방문한 장소의 빈 수납함, 부순 장벽, 남겨 둔 물건과 제거한 위험은 그대로 남는다.

김씨는 뗏목을 만들어 바다로 나가거나, 산등성이에서 대형 연기를 올리거나, 흩어진 부품으로 무전을 복구해 조기 탈출할 수 있다. 탈출하지 못하더라도 생활 방식은 사라지지 않는다. 농사와 채집에 매달렸는지, 기계 연구와 건설에 몰두했는지, 물속을 얼마나 헤엄쳤는지가 결과와 후일담을 바꾼다. 생존은 진지하지만 김씨의 임기응변과 예상 밖의 결과는 생활 코미디로 표현한다.

## 2. 톤과 콘텐츠 안전선

- 생존 위험은 긴장을 만들되 선혈·고어 없이 실루엣, 반응 동작, 상태 아이콘과 소리로 표현한다.
- 코미디의 대상은 김씨의 급조 생활, 물건의 엉뚱한 쓰임, 자연과의 타이밍 불일치다.
- 특정 인종·국가·직업·재난 피해자를 조롱하지 않는다.
- 현실 정치, 불법 약물, 노골적인 성적 콘텐츠, 타사 IP와 고유 UI·캐릭터 복제를 사용하지 않는다.
- 규칙과 실패 이유를 먼저 전달하고 농담은 두 번째 줄이나 결과 컷에 배치한다.

## 3. 도입 코믹 5컷

모든 컷은 엔진 텍스트를 사용한다. `다음/이전/전체 스킵`을 제공하며 스킵 여부는 게임 상태와 자원에 영향을 주지 않는다.

| 안정 ID | KO 원문 | EN 의도 | 화면·등장 자산 | 진입/스킵 |
|---|---|---|---|---|
| `story.opening.storm.01` | 퇴근보다 먼저 온 것은 폭풍이었다. | A storm arrives before Mr. Kim can make it home. | 검은 구름, 작은 여객선, 주황 셔츠 김씨 | 새 게임 직후, 즉시 스킵 가능 |
| `story.opening.storm.02` | 김씨는 구명조끼보다 회사 배낭을 먼저 잡았다. 영수증이 많이 들어 있었다. | He saves his work bag first; it is mostly receipts. | 기울어진 갑판, 배낭을 잡는 김씨 | 이전/다음 가능 |
| `story.opening.storm.03` | 밤새 파도와 협상했지만 파도는 답장을 하지 않았다. | The sea refuses to negotiate through the night. | 튜브에 매달린 김씨, 큰 파도, 고어 없음 | 이전/다음 가능 |
| `story.opening.storm.04` | 아침, 김씨는 무인도에 도착했다. 휴대전화는 습기만 감지했다. | Morning brings an island and one useless moisture alert. | 해변, 젖은 휴대전화, 야자수 | 이전/다음 가능 |
| `story.opening.storm.05` | 오늘의 업무: 지붕, 식량, 구조. 결재자는 김씨 한 명. | Today's work: shelter, food, rescue. Mr. Kim is the entire approval chain. | 지붕 있는 시작 캠프의 실루엣과 세 목표 표식 | 다음 입력으로 캠프, 전체 스킵 가능 |

도입 자동 재생 목표는 30~45초이며 사용자가 직접 넘기면 15초 안에도 종료할 수 있다.

## 4. 첫 플레이 진행 비트

| 안정 ID | 발생 조건 | KO 원문 | EN 의도 | 방해 상한 |
|---|---|---|---|---|
| `story.beat.camp-first-control` | 도입 뒤 캠프 첫 조작 | 가까이 가면 표시가 뜬다. 김씨는 멀리서 일하지 않는다. | Walk to marked objects and interact in person. | 4초, 이동 차단 없음 |
| `story.beat.map-first-open` | 수집 지도 첫 사용 | 해변부터 확인하자. 폭풍이 밀어온 것은 쓰레기와 기회다. | The beach is the first safe lead. | 팝업 안 한 줄 |
| `story.beat.search-first-reveal` | 첫 수색물 공개 | 전부 가져갈 수는 없다. 오늘 필요한 것부터 고르자. | Choose what matters; the bag is limited. | 트레이 안 한 줄 |
| `story.beat.return-first` | 첫 귀환 | 주운 물건이 집이 되는 순간부터 표류는 생활이 된다. | Salvage becomes a home after the first return. | 4초, 행동 차단 없음 |
| `story.beat.craft-first` | 첫 제작/연구 성공 | 돌과 나무가 도끼가 됐다. 김씨의 직급이 석기시대로 올랐다. | The first tool unlocks a new tier of searching. | 결과 토스트 3초 |
| `story.beat.expansion-first` | 첫 방 증축 확정 | 위로 갈지 아래로 갈지 정했다. 옆은 여전히 바다다. | A new floor creates real placement space. | 결과 토스트 3초 |
| `story.beat.escape-project-first` | 첫 탈출 설비 생성 | 이제 생존만 하는 게 아니다. 나갈 방법을 만들고 있다. | The run now has a visible escape goal. | 결과 토스트 3초 |

## 5. 일곱 지역 첫 진입 소개

| 지역 안정 ID | KO 소개 | EN 의도 | 주요 시각·자원·위험 | 다음 단서 |
|---|---|---|---|---|
| `region.coast.beach` | 폭풍이 남긴 첫 창고. 표류물 사이에 먹을 것과 쓸 만한 천이 섞여 있다. | Safe opening region with mixed basic supplies. | 밝은 모래, 표류목, 조개·식량, 높은 파도 | 숲길 흔적 |
| `region.forest.grove` | 나무와 먹을 것은 많지만 덩굴 뒤쪽은 돌도끼가 있어야 편하다. | Wood and food with tool-gated depth. | 굵은 수관, 풀숲, 죽은 나무, 벌레·위험 식물 | 얕은 바다 조망 |
| `region.sea.shallows` | 허리까지 잠기는 물 아래에 폭풍 잔해가 반짝인다. 기력과 파도를 함께 봐야 한다. | Swimming search with salvage and current risk. | 청록 수면, 암초, 잠긴 상자, 해파리·급류 | 산등성이 길 |
| `region.ridge.highland` | 멀리서도 보이는 곳이다. 바람은 구조 신호와 김씨를 같은 방향으로 날리려 한다. | Signal site with wind and fall danger. | 절벽, 휘는 풀, 넓은 하늘, 바람·낙상 | 동굴 입구와 폐시설 |
| `region.cave.island` | 햇빛은 적고 돌과 약재는 많다. 안쪽의 소리는 대부분 김씨보다 먼저 움직인다. | Mineral and medicine region with disease/insect pressure. | 단순 암벽 실루엣, 광물맥, 균류, 어둠·벌레 | 난파선 금속 흔적 |
| `region.cove.wreck` | 난파선은 섬의 가장 큰 공구함이다. 문제는 뚜껑이 파도라는 점이다. | Mechanical salvage and protected parts. | 기울어진 선체, 밧줄, 사물함, 날카로운 금속·파도 | 무전 부품과 폐중계소 지도 |
| `region.ruins.relay` | 녹슨 중계소는 죽어 있지만 전선과 회로는 아직 할 말이 많다. | Late electronics region and radio climax. | 콘크리트·철탑 단순형, 캐비닛, 케이블, 감전·질병 | 무전 완성과 산등성이 안테나 |

## 6. 핵심 엔딩 5종의 3컷 비트

### `ending.escape.raft.open-water`

1. `ending.raft.01.setup`: 해안 진수대에서 완성한 뗏목을 밀며 보급과 날씨를 마지막으로 확인한다.
2. `ending.raft.02.escape`: 파도가 뗏목을 삼킬 듯하지만 돛이 바람을 잡고 섬이 멀어진다.
3. `ending.raft.03.aftermath`: 구조선 갑판의 김씨가 멀미 봉투에 다음 여행 계획을 적는다.

### `ending.escape.smoke.seen-from-afar`

1. `ending.smoke.01.setup`: 산등성이 신호대에 부싯돌과 큰 나무 더미를 준비한다.
2. `ending.smoke.02.escape`: 굵은 연기 기둥 너머의 배가 방향을 바꾼다.
3. `ending.smoke.03.aftermath`: 구조된 김씨가 캠핑장에서 작은 불에도 과하게 연기 방향을 확인한다.

### `ending.escape.radio.clear-signal`

1. `ending.radio.01.setup`: 서로 다른 지역에서 모은 무전기·기판·트랜지스터를 작업대에서 연결한다.
2. `ending.radio.02.escape`: 잡음 끝에 정확한 응답과 구조 좌표가 화면에 들어온다.
3. `ending.radio.03.aftermath`: 김씨가 동네 라디오의 임시 DJ가 되어 섬 날씨부터 읽는다.

### `ending.gamejam.stay.natural-kim`

1. `ending.natural.01.setup`: 탈출 준비보다 식량·채집·사냥·회복 설비가 캠프를 가득 채운다.
2. `ending.natural.02.choice`: 구조 기회를 본 김씨가 잘 익은 열매와 튼튼한 지붕을 번갈아 본다.
3. `ending.natural.03.aftermath`: 수년 뒤 탐사대가 섬의 생활 전문가가 된 김씨에게 길을 묻는다.

### `ending.gamejam.stay.island-engineer`

1. `ending.engineer.01.setup`: 2층과 지하가 장치·전선·부품으로 연결된 작은 공방이 된다.
2. `ending.engineer.02.choice`: 무전보다 자동 빗물 장치와 기계 팔을 먼저 완성한다.
3. `ending.engineer.03.aftermath`: 섬 전체가 돌아가는 순간, 김씨는 새 표지판에 `김씨 기술연구소`라고 쓴다.

각 컷의 제목·설명은 KO/EN 엔진 텍스트로 제공하며, 그림은 결과·원인·후일담의 핵심 행동을 텍스트 없이도 구분해야 한다.

## 7. 목표 변형 엔딩 3종

| 엔딩 ID | 교체 컷 | 시각 펀치라인 | 트리거 의도 |
|---|---|---|---|
| `ending.comic.smoke.island-barbecue` | smoke 2·3 | 구조선은 연기가 아니라 거대한 생선구이 냄새를 따라온다. | 연기 경로 + 요리/식량 행동 우세 |
| `ending.comic.radio.island-dj` | radio 2·3 | 구조 응답 전까지 김씨가 섬 음악 방송을 이어 간다. | 무전 경로 + 기계 연구/반복 송신 |
| `ending.rare.raft.current-reader` | raft 2·3 | 김씨가 파도와 새 떼를 읽어 구조선보다 먼저 항구에 도착한다. | 뗏목 경로 + 수영/해상 수색 우세 |

## 8. 스타일 벤치마크 잠금

벤치마크는 `1920×1080` 실제 게임 화면 한 장으로 제작한다.

- 김씨: 기존 주황 티셔츠·짙은 반바지·샌들·급조 배낭, 굵은 잉크 윤곽, 약간 지친 생활인 표정.
- 캠프: 지붕 있는 1층 절개형 쉘터, 모닥불·작업대·빗물받이·지도대가 서로 겹치지 않고 생활 동선이 보인다.
- 배경: 따뜻한 해변과 바다를 큰 색면과 제한된 질감으로 단순화한다. 김씨와 상호작용 물체보다 세부 대비가 낮아야 한다.
- UI: 먹물색 반투명 면, 바랜 천·표류목의 얇은 테두리, 호박색 강조, 청록색 포커스. 광택·거대한 패널·화면 중앙 상시 설명을 피한다.
- 상호작용: 대상 위 작은 흰색 원형 표식, 접근 시 화면 하단 compact prompt. 본문은 엔진 텍스트 영역으로 비운다.
- 카메라: 2D side-view, 김씨의 키는 화면 높이 약 18%, 지면·발 접점이 분명하다.
- 금지: 래스터 한글/영문 본문, 모바일풍 둥근 광택 버튼, 사실적 회화 배경과 만화 캐릭터의 밀도 충돌, 타사 UI 배치 복제, contact sheet.

채택 판정은 다음 다섯 질문으로 한다.

1. 김씨가 배경보다 먼저 보이는가?
2. 지붕·지면·설비 사이의 생활 동선이 한눈에 읽히는가?
3. 상호작용 대상과 UI가 같은 강조색 규칙을 쓰는가?
4. 1280×800으로 축소해도 HUD와 prompt가 플레이 공간을 막지 않는가?
5. 이 규칙을 7지역, 다층 캠프, 엔딩 코믹에 재사용할 수 있는가?

## 9. 제작 범위 경계

- O9 필수: 도입 5컷 텍스트/화면, 첫 진행 비트, 스타일 벤치마크, 캠프·해변·숲 첫 루프, 김씨 idle/walk/search/facility-use/ladder, 대표 엔딩 1종.
- O10 필수: 7지역, 다층 캠프, 19개 필수 아이템, swim/hurt-sick/rest-eat, 세 탈출, 핵심 엔딩 5종, 최소 오디오.
- O10 이후 목표: 코믹 2종·희귀 1종 교체 패널과 행동 modifier 4종.
- 정식 버전 이월: 나머지 13개 엔딩의 고유 컷신, 조명탄·고지대 중계소 플레이 완성, Steamworks 업적 연결.

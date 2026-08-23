# Wave 11 사용자 플레이테스트 관찰·판단 프레임

> 상태: `READY TO USE / HUMAN RESULTS UNRUN`
> 통합 기준: `origin/master 72ec967a9009635fbeccbc758563183a67a4b311`
> 기계 정본: `.forge/design/wave11-playtest-triage.json`
> 상속: Wave 8 20분 외부 테스트, Wave 9 밸런스, Wave 10 모듈 온보딩 계약
> 공식 영문 게임 제목: `TBD`

이 문서는 사용자가 제공된 Windows 빌드를 직접 한 번 플레이하며 짧게 기록하는 양식이다. 한 번의 owner selftest는 버그와 감각 가설을 찾는 진단 표본이지 첫 사용자 발견성 합격이나 밸런스 변경 근거가 아니다. 사람이 하지 않은 결과는 빈칸과 `UNRUN`으로 남긴다.

## 1. 실행 전 2분 빌드 카드

| 필드 | 기록 |
|---|---|
| Session ID | `W11-____` |
| 증거 유형 | `OWNER_SELFTEST / NAIVE_HUMAN / PHYSICAL_GAMEPAD_HUMAN` |
| Build commit / EXE SHA-256 | `________________ / ________________` |
| 실행 파일 | `________________` |
| locale | `ko / en` 중 하나. `qps-long` 선택 금지 |
| 입력 | `KEYBOARD_MOUSE / PHYSICAL_GAMEPAD` 중 하나 |
| 실제 게임패드 모델 | 해당 시 `________________` |
| 해상도·창 모드 | `1280×800 / ________________` |
| 초기 상태 | 새 게임, Day 1, 가방 4칸 확인 `Y/N` |
| 개발 지름길 | grant·warp·console·공략 미사용 `Y/N` |

- 한국어가 의미와 코미디 톤의 기본이다. 첫 owner session은 `ko`를 권장한다.
- 영어는 별도 session으로 확인한다. 한 session 중 locale을 바꾸면 언어 비교 표본으로 쓰지 않는다.
- `qps-long`은 글자 팽창·폰트·클리핑·glyph를 보는 QA 전용이다. 재미·혼란·코미디 점수를 매기거나 사람 locale 표본으로 세지 않는다.
- 키보드·마우스와 실제 게임패드는 별도 기록지를 쓴다. 도중 장치를 바꾼 session은 장치 비교에서 제외한다.
- 자동화 결과는 아래 `Automation reference`에만 적고 인간 session 수·평점·이해도와 합치지 않는다.

## 2. 첫 90초: 빌드 경로와 사용자 발견을 분리

### 2.1 현재 소스 감사 기준

`72ec967` 소스를 읽은 현재 기준은 다음과 같다. 이는 사람 결과가 아니라 빌드 capability 사전 감사다.

| 항목 | 소스상 상태 |
|---|---|
| 확정 전 preview action 소유자 | `StoragePlanning` |
| 확정 전 `slot.start.*` 직접 preview target | 없음 |
| `ModuleConnector` 등록 | 방 하나를 확정한 뒤 |
| 확정 뒤 connector action | 확정 방 출입 |
| 예상 실제 진입 경로 | `STORAGE_PLANNING` |

제공 EXE가 같은 소스인지 먼저 화면에서 확인한다. 직접 슬롯 prompt가 없으면 사용자가 못 찾은 것이 아니라 `DIRECT_SLOT_NOT_AVAILABLE_IN_BUILD`다. 이때 Wave 10의 직접 슬롯 90초 게이트는 사람에게 `N/A`로 두고, 실제 storage planning 발견 시각을 계속 기록한다. direct slot이 실제로 있는데 못 찾은 경우만 발견성 관찰이 된다.

### 2.2 90초 기록

| 필드 | 값 |
|---|---|
| 00:00 기준 | 새 게임 월드 조작 활성 프레임 |
| 첫 이동 | `____:____` |
| direct slot 시각적 단서 | `VISIBLE / NOT_VISIBLE / UNKNOWN` |
| direct slot 근접 prompt | `AVAILABLE / NOT_AVAILABLE / UNKNOWN` |
| 실제 첫 진입 경로 | `DIRECT_SLOT / STORAGE_PLANNING / OTHER_CONTEXTUAL_TARGET / NONE` |
| 실제 진입점 최초 접근 | `____:____` |
| module preview 최초 open | `____:____ / 미도달` |
| 위·옆·지하 모두 확인 | `____:____ / 일부 / 미도달` |
| 작업대 필요·W2/D1 이해 | `Y / N / 미노출` |
| direct-slot 게이트 자격 | `EVALUABLE_DIRECT_SLOT / DIRECT_SLOT_NOT_AVAILABLE_IN_BUILD / TECHNICALLY_BLOCKED / NOT_OBSERVED` |
| 90초 메모 | `________________________________________________` |

판정 원칙:

- `DIRECT_SLOT_NOT_AVAILABLE_IN_BUILD`: 사용자 실패 0건. 별도 build-gap P1 후보이며 실제 경로를 평가한다.
- `EVALUABLE_DIRECT_SLOT`인데 owner가 못 찾음: owner 진단 메모다. naïve 발견성 표본으로 세지 않는다.
- `STORAGE_PLANNING`으로 preview가 열림: 실제 빌드 경로의 시간과 이해 여부를 기록한다.
- target은 보이는데 입력·focus·state가 막음: `TECHNICALLY_BLOCKED`로 분류하고 재현 정보를 남긴다.

## 3. 20분 이하 한 session 관찰표

00:00부터 최대 20:00까지 자연스럽게 플레이한다. 아래 시간 구간은 기록 초점일 뿐 플레이 순서나 해결 힌트가 아니다. 실제 최초 발생 시각을 적는다. 구조·탈진·기한 실패·중단·크래시·softlock이 먼저 나오면 즉시 종료 시각과 결과를 기록한다.

| 관찰 구간 | 기록 초점 |
|---:|---|
| 00:00–01:30 | 첫 이동, 첫 근접 target, 실제 module 진입 capability·경로 |
| 01:30–06:00 | 설비 접근·팝업, 원정 출발, 육상 첫 채집 |
| 06:00–10:00 | 수영 입수·수상 수색·육상 복귀, 캠프 첫 귀환 |
| 10:00–15:00 | 제작/연구, 일반 설비 유효 배치, 가방 4→6 선택 |
| 15:00–20:00 | 방 preview/확정 시도, 구조 신호 진행, 구조 또는 실패 |

| 코드 | 최초 시각 | Day | 관찰 |
|---|---:|---:|---|
| `MOVE_FIRST` | | | 움직임·camera 이해 |
| `FACILITY_NEAR` | | | 대상·prompt·glyph |
| `FACILITY_USE` | | | 대상 popup·선택 action |
| `LAND_GATHER` | | | 자원, 가방 상태 |
| `SWIM_ROUNDTRIP` | | | 입수/수색/복귀 E·L, 미완료 이유 |
| `RETURN_FIRST` | | | 자발/일몰, 이전 자원 |
| `CRAFT_OR_RESEARCH` | | | 연구/제작, 도구 |
| `PLACEMENT_VALID` | | | 설비, 위치, 무효 시도와 이유 |
| `BAG_GROWTH_DECISION` | | | `BUY / POSTPONE / REJECT / NOT_SEEN`, 당시 W/D·신호 |
| `MODULE_PREVIEW` | | | 실제 entry path, 본 후보 |
| `MODULE_COMMIT_ATTEMPT` | | | 후보, `SUCCESS / reason ID`, W2/D1 전후 |
| `END` | | | `RESCUED / EXHAUSTED / DEADLINE / TIMEOUT / PLAYER_STOP / CRASH / SOFTLOCK` |

추가 관찰:

| 항목 | 기록 |
|---|---|
| 첫 60초 이상 무상태 변화 | 시각·화면·반복 행동: `________________` |
| 같은 목적 행동 3회 이상 | 행동·횟수·이유 추정: `________________` |
| 가방 만석·교체/포기 | 시각·내용·선택: `________________` |
| 돌도끼 장벽·나무 +1 | 이해·오해·실제 결과: `________________` |
| 공간형 캠프 | 설비에 직접 접근해 사용 `Y/N`; 원거리 사용 시도 `__회` |
| 가장 강한 코믹 순간 | 시각·짧은 의미 요약: `________________` |
| 종료 후 계속하고 싶은가 | `Y / N / 조건부`; 이유 한 줄: `________________` |

## 4. 1–5 경험 척도

종료 직후 각 항목에 하나만 표시하고 해당 순간을 짧게 적는다. 혼란·막힘·반복 피로는 숫자가 높을수록 나쁘고, 재미·코믹 순간은 높을수록 좋다.

| 척도 | 1 | 3 | 5 | 점수·자유 메모 |
|---|---|---|---|---|
| 재미 `FUN` | 계속할 이유가 없음 | 의미 있는 선택·만족 순간 1회 이상 | 계속하거나 다시 하고 싶은 긴장·보상 | `__/5` · `________________` |
| 혼란 `CONFUSION` | 다음 행동·피드백이 명확 | 회복 가능한 오해 1~2회 | 행동·위치·비용·결과를 반복해서 모름 | `__/5` · `________________` |
| 막힘 `BLOCKAGE` | 진행 차단 없음 | 30~60초 멈췄지만 스스로 회복 | 외부 답·우회·재시작 없이는 진행 불가 | `__/5` · `________________` |
| 반복 피로 `REPETITION_FATIGUE` | 과한 반복 없음 | 한 루프가 루틴처럼 느껴짐 | 반복 이동·수색·팝업 때문에 중단하고 싶음 | `__/5` · `________________` |
| 코믹 순간 `COMEDY_MOMENT` | 기억나는 웃음 없음 | 미소나 재미있는 반응 1회 | 다시 말하고 싶은 기억점 | `__/5` · `________________` |

단일 점수만으로 P등급이나 변경을 결정하지 않는다. 반드시 시각·행동·화면 상태가 있는 관찰 메모와 함께 본다.

## 5. 자원 부족인가, 발견성 문제인가

막힌 순간마다 아래를 순서대로 묻고 하나를 표시한다.

| 질문 | Y/N·근거 |
|---|---|
| 막히기 전에 올바른 대상과 action이 존재한다는 것을 알았나? | |
| 전체 비용·보유량·정확한 부족량을 화면에서 보고 이해했나? | |
| 대상에 접근한 뒤 prompt·focus·실패 문구가 다음 행동을 알려줬나? | |
| 충분한 자원이 있을 때 같은 action이 정확히 한 번 성공했나? | |
| named 자원을 의도적으로 우선 수집한 뒤에도 같은 부족이 반복됐나? | |

| 분류 | 사용 조건 | 다음 판단 |
|---|---|---|
| `DISCOVERY` | target/action을 몰랐거나 prompt·위치·문구를 늦게 발견 | 비용 동결. framing→prompt→focus→문구 순 검토 |
| `RESOURCE` | target/action과 정확한 비용을 알았고 정상 경로로 시도했지만 실제 장부가 부족 | 표본을 더 모으고 동일 부족 원인을 대조 |
| `MIXED_UX_FIRST` | 늦은 발견 때문에 시간·자원이 부족해짐 | 발견성부터 고친 뒤 재검증 |
| `TECHNICAL` | 충분한데 거부, 잘못된/중복 차감, 잘못된 상태·focus | P0/P1 재현. 밸런스 근거로 사용 금지 |
| `NO_ISSUE` | 의도된 선택·실패이고 정보와 transaction이 정확 | 변경하지 않음 |

단일 owner session에서 `RESOURCE`가 나와도 밸런스를 바꾸지 않는다. 발견성·혼합·기술 가능성이 남아 있는 동안에는 항상 UX/기술을 먼저 본다.

## 6. P0/P1/P2와 재현 양식

| 등급 | 조건 | 최소 증거 | 조치 |
|---|---|---|---|
| `P0` | 크래시, run 상태 손상, 영구 softlock, 입력 완전 상실, 회복 불가 진행 차단, 개인정보 사고 | 사람 1회면 즉시 stop. 안전할 때만 같은 build에서 1회 재현 | 빌드 사용 중단→수정→새 build 검증 |
| `P1` | 핵심 루프 차단, 잘못되거나 없는 진입 경로, 오해시키는 비용/사유, ko/en 의미 역전, 실제 게임패드 action 불능 | 독립 사람 2회 또는 사람 1회+동일 build 수동 결정 재현 1회. 소스 확인 build gap은 즉시 후보 등록 가능 | 한 UX·입력·계약 축만 수정, 수치 동결 |
| `P2` | 진행 가능한 망설임, 비핵심 문구/계층, 중단 없는 반복 피로, 약한 코미디 | 같은 패턴 사람 3회. 미달이면 메모 유지 | P0/P1 뒤 보류 또는 작은 개선 후보 |

재현 카드:

| 필드 | 기록 |
|---|---|
| Issue ID / severity 후보 | `W11-ISSUE-__ / P0·P1·P2` |
| Build commit / EXE SHA | `________________ / ________________` |
| locale / 증거 유형 / 입력·장치 | `________________` |
| 해상도·창 모드 | `________________` |
| session 시각 / Day / Phase | `________________` |
| room / InteractionTarget / module candidate | `________________` |
| 창고 W/S/F/D | `__/__/__/__` |
| 가방 칸·내용 | `________________` |
| E/H/L | `__/__/__` |
| 도구·설비·신호·모듈 상태 | `________________` |
| 정확한 재현 단계 | `1.__ 2.__ 3.__` |
| 기대 / 실제 | `________________ / ________________` |
| 재현 횟수 | `__ / __회` |
| 회복·우회 | `없음 / ________________` |
| Human evidence reference | `________________` |
| Automation reference — 별도 | `________________ / 없음` |

자동화 reference는 상태 재현을 보조할 뿐 사람 session이나 평점으로 세지 않는다. 실제 게임패드 결과도 합성 입력·자동화 gamepad snapshot과 합치지 않는다.

## 7. 최소 표본과 stop/go

| 게이트 | 조건 | 결정 |
|---|---|---|
| `STOP_P0` | P0 1회 | 제공 build 사용 중단. 재현 보존 후 수정·새 build |
| `HOLD_BUILD_CAPABILITY` | direct-slot path가 EXE에 없음 | 사람 90초 direct gate는 N/A. 실제 경로 계속 기록, 별도 P1 build-gap 후보. 수치 변경 없음 |
| `GO_COLLECT_MORE` | owner session 1회 완료, P0 없음 | 진단 표본 1개로 보존. 현행 수치·기능 유지 |
| `GO_FIX_P1` | P1 최소 증거 충족 | 가장 작은 UX/입력/계약 축 수정 후 affected human run 재실행 |
| `GO_QUEUE_P2` | 같은 P2 사람 3회 | P0/P1 뒤 backlog 후보. 즉시 루프 변경 없음 |
| `GO_ONE_AXIS_BALANCE_REVIEW` | 기존 Wave 8 방식의 유효 6세션 `ko3/en3`, 발견성·이해도 합격, 같은 RESOURCE 원인 2회 이상 | 하나의 수치 변경안만 사용자 검토에 올림. 자동 변경 금지 |

한 owner session의 종료가 구조 성공이어도 “너무 쉽다”, 실패여도 “너무 어렵다”로 결론내리지 않는다. 현재 session이 줄 수 있는 즉시 결정은 P0 stop, 재현 가능한 P1 후보, 또는 `GO_COLLECT_MORE`뿐이다.

## 8. 결과 요약 — 실행 전 빈칸 유지

| 항목 | 값 |
|---|---|
| Human session status | `UNRUN` |
| 종료·시각 | `________________` |
| 실제 module entry path | `________________` |
| direct-slot gate eligibility | `________________` |
| FUN / CONFUSION / BLOCKAGE / FATIGUE / COMEDY | `__/__/__/__/__` |
| 원인 분류 | `DISCOVERY / RESOURCE / MIXED_UX_FIRST / TECHNICAL / NO_ISSUE` |
| P0/P1/P2 후보 | `________________` |
| stop/go | `NO_DECISION_BEFORE_HUMAN_PLAY` |
| 핵심 자유 메모 | `________________________________________________` |

범위 밖: 새 콘텐츠·메커니즘, 밸런스 수치 변경, Runtime·QA 코드, Art/asset 상태, 실제 사람 결과 생성, 참가자 개인정보, Steam App ID와 영문 제목 확정.

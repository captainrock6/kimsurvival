# GAME JAM Wave B/C 아트·UI 검토 패킷

기준은 `origin/codex/gamejam-director-search-node-integration@00e0d5a9df597ab4a9f54bff665291f367d40c92`다. 이 패킷은 선택을 돕는 `review-only` 증거이며 런타임·씬·Addressables를 바꾸지 않는다.

## 한 장 선택 보드

- `gamejam-wave-bc-selection-board-3840x1856.png`
- 위 행은 통합 GREEN의 KO/EN/qps-long 캡처 세 장을 각각 원본 1280×800 셀로 유지한다.
- 아래 행은 기존 수색 node 후보, 기존 하단 9-slice loot tray 후보, 다음 Wave 재사용/신규 아이콘 감사를 비교한다.
- 보드는 런타임 소스가 아니며 보드의 설명 문구도 게임에 들어가지 않는다.

## 실제 화면 시각 감사

- KO/EN/qps-long 모두 1280×800 안에서 행동 버튼, 발견물 아이콘, 4/6 가방, 언어 전환과 하단 입력 설명이 잘리지 않는다.
- qps-long은 가장 조밀하지만 행동 3개와 가방 6칸이 화면 안에 유지된다.
- 현행 트레이는 대략 `x=30..757, y=143..370`의 큰 불투명 상단 좌측 블록이며, 기능 GREEN이지만 월드 시야를 많이 점유한다.
- `ui.search-loot-tray.compact-bottom-a`는 manifest 기준 `x=176..1104, y=485..684`의 하단 9-slice다. 중앙 캐릭터와 보행 시야는 더 잘 보존하지만 기존 우측 가방 요약과 겹치는 구간을 런타임 구현 전에 조정해야 한다.
- 현행 월드 node는 단색 임시 도형이다. `object.searchable-resource-node-kit.state-language-a`는 unknown / known-partial / depleted를 오브젝트 형태로 구분하지만, 사용자 선택 전에는 교체하지 않는다.

## 재사용 우선 감사

새로 만들지 않은 항목:

- 질병 정체성: `icon.expedition-resource-risk-set/risk.illness`
- 질병 단계: 채택된 `effect.survival-hazards.phase-silhouette-a`의 예고·발생·완화·회복 문법
- 야생동물 경고: `icon.expedition-resource-risk-set/risk.wildlife`
- 일반 전자부품 범주: `icon.expedition-resource-risk-set/resource.electronics`
- 수색 상태 덧표식: 기존 review 후보의 `hazard-exposed`, `protected-part`, `known-remainder`

일반 전자부품 아이콘은 범주 표시에만 쓴다. 망가진 무전기·전자기판·트랜지스터라는 보호 부품의 고유 정체성을 대신할 수 없다.

## 신규 review 후보

- 자산: `icon.gamejam-hazard-part-set`
- 안정 후보: `icon.gamejam-hazard-part-set.silhouette-a`
- Forge job: `job_20260825171815_f7640e25`
- 신규 항목: `risk.insects`, `risk.dangerous-plants`, `part.smoke.flint`, `part.radio.transceiver`, `part.radio.circuit-board`, `part.radio.transistor`
- 검토 보드: `gamejam-hazard-part-review-board-1920x1080.png`
- 엔진 전달 manifest: `Assets/_Project/Art/Generated/ui_set/job_20260825171815_f7640e25/gamejam-hazard-part-manifest.json`
- 결정론적 로컬 원본: `LocalSource/`와 `generate-gamejam-wave-bc-review.ps1`
- Forge 품질: 100점, 오류 0, 경고 0

48/64px에서 벌레는 날개·더듬이·점군, 위험 식물은 세로 줄기·뿌리·가시·포자, 부싯돌은 각진 조각·불꽃 홈, 무전기는 부러진 안테나, 전자기판은 노치·회로, 트랜지스터는 세 다리로 구분한다. 색상은 보조 신호다. 고어와 래스터 현지화 본문은 없다.

## 후보별 사용자 판단

- `object.searchable-resource-node-kit.state-language-a`: 풍부한 상태 표현이 실제 플레이에서 너무 크거나 배경과 경쟁하지 않는지 판단한다. Forge 62점의 오류·경고는 불투명 검토 증거의 범용 alpha 판정이며 투명 런타임 파츠 자체의 실패는 아니다.
- `ui.search-loot-tray.compact-bottom-a`: 하단 위치와 한 번에 3개 카드가 현재 상단 트레이보다 나은지, 우측 가방 요약과의 중첩을 허용할지 판단한다. Forge 70점 오류는 불투명 실제 화면 증거의 alpha 판정이다.
- `icon.gamejam-hazard-part-set.silhouette-a`: 위험 식물과 벌레, 무전기와 전자기판이 48px에서도 충분히 다른지 판단한다. Forge 100점이며 새로 만든 6개 모두 여전히 review다.

추천은 기능 GREEN 캡처를 기준선으로 유지하면서, 수색 node·하단 tray·신규 아이콘을 서로 독립적으로 선택하는 것이다. 특히 하단 tray 선택이 node 또는 아이콘의 자동 채택을 의미하면 안 된다.

## 승인 게이트

세 review 자산은 모두 `decision=review`, `selectedCandidate=null`, `runtimeAllowlist=[]`, `packageAllowed=false`, `runtimeConnectAllowed=false`, `runtimeConnected=false`다. 기존 채택 Wood/Stone/Food/Salvage GUID는 변경하지 않았다. 자세한 기계 판독 기록은 `gamejam-wave-bc-review-manifest.json`을 따른다.

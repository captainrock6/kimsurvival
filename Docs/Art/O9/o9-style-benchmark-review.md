# O9 스타일 벤치마크 검토

> 상태: `REVIEW_CANDIDATE · NOT_ADOPTED`
>
> Forge asset: `ui.gamejam.style-benchmark`
>
> Forge job: `job_20260828103426_790ffb10`

## 후보

- 파일: `Assets/_Project/Art/Generated/ui_set/job_20260828103426_790ffb10/exec-1f8ee5d7-2cc2-4bf2-879d-12d3aab9a5f6.png`
- 생성 기준: 기존 김씨 잉크 atlas, 기존 캠프 배경, 승인된 공간형 베이스캠프 콘셉트.
- Forge 기계 검수: 1672×941, 불투명 단일 화면, 품질 검사 PASS. 이는 아트 채택 판정이 아니다.

## 유지할 규칙

1. 주황 셔츠 김씨의 굵은 잉크선과 표정을 화면의 가장 강한 대비로 둔다.
2. 지붕 있는 절개형 캠프는 하나의 지면선, 빈 이동 통로, 넉넉한 설비 간격으로 읽힌다.
3. 배경은 큰 색면과 낮은 세부 대비, UI는 얇은 먹물 면·호박 강조·청록 focus를 사용한다.
4. 상호작용 표식은 작고 밝은 원형, 행동 안내는 하단 compact prompt로 제한한다.
5. 래스터 안에 본문 문구를 굽지 않고 KO/EN과 향후 언어는 TMP로 표시한다.

## 수정이 필요한 점

- 후보의 하단 요소는 실제 compact prompt보다 4칸 inventory strip에 가깝다. 런타임에서는 기존 compact prompt를 유지한다.
- 캠프와 하늘의 묘사 밀도를 한 단계 더 낮추고 UI 장식도 단순화해야 김씨가 더 먼저 보인다.
- 후보 상단 HUD는 실제 O9의 얇은 두 줄 HUD보다 장식적이다. 런타임 구현을 우선 기준으로 삼는다.
- 정식 채택 뒤 1280×800 crop과 1920×1080 원본 모두에서 설비·발·지면·UI 겹침을 다시 확인한다.

## 채택 선택

- `ADOPT`: 이 후보의 색·잉크·간격 규칙을 최종 production set의 기준으로 사용한다.
- `REVISE`: 위 수정점과 사용자 피드백을 반영한 새 1안을 별도 요청한다.
- `HOLD`: 런타임 구현만 유지하고 래스터 production은 보류한다.

현재는 `HOLD_FOR_USER_REVIEW`이며, 후보 파일을 Addressables나 최종 빌드 아트로 연결하지 않는다.

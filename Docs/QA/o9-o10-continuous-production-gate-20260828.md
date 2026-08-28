# O9 → O10 연속 제작 게이트

> 기준일: 2026-08-28
>
> 소스 기준: 작업 트리 O9/O10 presentation integration
>
> 결과: `SOURCE_GREEN · UNITY_PLAYER_BLOCKED_BY_LICENSE · ART_ADOPTION_PENDING`

## O9 콘텐츠·프레젠테이션

| 항목 | 상태 | 증거 |
|---|---|---|
| 폭풍 표류 시놉시스·5컷 도입 | PASS | `PrototypeGameJamNarrative.Opening` 5개 stable beat |
| 한국어 기본 타이틀·KO/EN 전환 | PASS · source | `KimSurvivalPrototype.O9O10Presentation.cs` |
| 조작·크레딧·스킵 | PASS · source | 같은 submission shell |
| 첫 5~10분 목표 안내 | PASS · source | camp/map/search/return/invest objective state |
| UI 겹침 방지 | PASS · source | objective/message 좌우 분리, modal 동안 objective 숨김 |
| 김씨 core 표현 | PASS · source | 8 atlas cell + walk/breathe/search/use/climb/hurt/rest/eat states |
| 기능 오디오 | PASS · source | surf + 7 one-shot cue, 외부 API·유료 음원 없음 |
| 스타일 벤치마크 | REVIEW | Forge `job_20260828103426_790ffb10`, 사용자 채택 전 |

## O10 콘텐츠 베타

| 항목 | 상태 | 증거 |
|---|---|---|
| 7개 수색 지역 | PASS · retained | 기존 region catalog와 지역별 engine-native silhouette |
| 1층·2층·지하·사다리·카메라 | PASS · retained | O4 vertical traversal + O6/O7 camp contracts |
| 19종 필수 아이템 | PASS · source | 12 자원 + 5 보호 부품 + 돌도끼·밧줄, stable-ID icon cache |
| 수영·부상/질병·휴식·먹기 표현 | PASS · source | player pose state와 기존 survival/disease runtime 연결 |
| 뗏목·대형 연기·무전 | PASS · retained | 기존 세 playable escape project/ending contract |
| 핵심 엔딩 5종 | PASS · retained/source lock | raft/smoke/radio/natural/engineer 3-panel comic 표면 |
| 30분 경제 | PROVISIONAL | 합성 프로필 외 사용자 자연 플레이 재검증 필요 |

## 검증 결과

- Unity 6000.4.9f1의 기존 Bee `Assembly-CSharp.rsp`와 Roslyn으로 수정된 runtime 전체 및 새 파일을 정적 컴파일했다.
- 결과 DLL: `Artifacts/ParallelQA/20260828T120000Z_o9_o10/Assembly-CSharp-static.dll`.
- 컴파일러 출력: 오류·경고 없음.
- Unity batch 실행은 Package Manager 샌드박스 재시도 뒤 라이선스 서버에 연결했으나 `No valid Unity Editor license found`, return code 198로 중단됐다.
- 따라서 실제 Play Mode, KO/EN 1280×800·1920×1080 캡처, Windows x64 후보 빌드, 5~10분 및 30분 자연 플레이는 이번 게이트에서 PASS로 표시하지 않는다.

## 다음 해제 조건

1. Unity Hub 로그인 계정에서 6000.4.9f1 Editor entitlement를 다시 활성화한다.
2. 스타일 후보를 `ADOPT / REVISE / HOLD` 중 하나로 사용자 판정한다.
3. compile → edit contracts → play capture → Windows development build를 정확 소스에서 다시 실행한다.
4. O9 5~10분 무설명 first-loop를 먼저 통과한 뒤 O10 30분 세 탈출 자연 플레이를 재개한다.

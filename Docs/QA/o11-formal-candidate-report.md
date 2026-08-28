# O11 정식 후보 보고서

## 결과

- 결과: **O11 정식 후보 통과**
- 소스 커밋: `1344f6c2ed84fa9bedebdc9d384c0b915aae6af8`
- Unity: `6000.4.9f1`
- 대상: Windows x86_64, `BuildOptions.None` 비개발 빌드
- O11 독립 제품 게이트: `7 PASS / 0 GAP / 0 FAIL`
- 인프라: 컴파일 오류 0, O11 소유 경고 0
- 원본/ZIP 해제본 실행 스모크: 각각 8초 조기 종료 없음

## 정식 채택

- 7개 수색 지역: `background.expedition-seven-region-set`, `job_20260826165624_448aecdc`
- 6종 수색 노드: `object.searchable-resource-node-kit.state-language-a`, `job_20260825150605_49020784`
- production 노드 패키지: `object.searchable-resource-node-production-set`, `job_20260826165625_8961b353`
- 지역 런타임 allowlist 7개와 노드 allowlist 6개를 정식 연결했으며 `provisionalReviewBuildConnection=false`이다.

## 사다리·수영 폴리시

- 사다리: 바위/절벽이 포함된 단일 자세를 제거하고, 손·발이 교차하는 투명 배경 4프레임 스트립으로 교체했다.
- 수영: 점프 자세 재사용을 제거하고, reach/pull/recover/glide가 읽히는 수평 영법 4프레임으로 교체했다.
- 코드 계약은 코어 자세, 수영 프레임, 사다리 프레임의 상태별 피벗 일관성과 각 4개 고유 프레임 순환을 검증한다.
- 실제 런타임 시각 계약: `PASS`, 37개 캡처 생성.

## O11 게이트

| 항목 | 결과 |
|---|---|
| P0-001 3개 층 자유 배치·이동·저장·거절 원자성 | PASS |
| P0-002 출항 가능/불가능/중복 입력의 상태·비용 일치 | PASS |
| P1-001 증축 반응 후 idle 복귀·이동 | PASS |
| P1-002 V2 UI, KO/EN, 1280×800·1920×1080 무겹침 | PASS |
| P1-003 3회 수색·회복 기력 페이싱 결정성 | PASS |
| P1-004 뗏목·연기·무전 3경로 실행 가능성과 부담 밴드 | PASS |
| P1-005 7지역·김씨 5상태 실 GUID/클립과 정식 채택 | PASS |

## 빌드 산출물

- 폴더: `Builds/O11FormalCandidate-1344f6c2/`
- ZIP: `Builds/KimsSurvivalIsland-O11FormalCandidate-1344f6c2.zip`
- 실행 파일 SHA-256: `A197542AD0D026C5C3BC7AEAD606B6B0184ADAD7B4EE3635326C575B25A5B423`
- ZIP SHA-256: `B7C400EFBD06CE527B0352F6BE43AC8F082EDE2E6B20A7BC6E2CB87B94229622`

## 남은 수기 검증

- 실제 키보드·마우스와 물리 게임패드의 조작 감각은 사람이 확인해야 한다.
- Steam 업로드와 업적 연동은 아직 후보 범위 밖이다.
- 자동 게이트는 제품 계약과 초기 실행을 검증했지만 30분 자연 플레이의 재미·밸런스를 대신하지 않는다.

## 이미지 생성 기록

- 도구: Codex 내장 ImageGen, 기존 `mr-kim-core-atlas.png`를 정체성·잉크선 참조로 사용했다.
- 사다리 최종 프롬프트 요약: 동일 김씨의 복장·비율을 유지하고, 투명 배경 한 줄 4프레임 교차 등반, 사다리·절벽·텍스트 제외.
- 수영 최종 프롬프트 요약: 동일 김씨의 복장·비율을 유지하고, 투명 배경 한 줄 4프레임 수평 영법, 물·파도·점프 자세·텍스트 제외.

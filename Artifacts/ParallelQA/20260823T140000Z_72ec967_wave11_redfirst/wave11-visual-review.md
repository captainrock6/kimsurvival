# Wave 11 1280×800 육안 검토

- RunId: `20260823T140000Z_72ec967_wave11_redfirst`
- 제품 기준: `72ec967a9009635fbeccbc758563183a67a4b311`
- Unity: `6000.4.9f1`
- 검토 방식: PNG 원본 1280×800을 1:1로 열어 대상 안내, 언어, 화면 경계, 보행 영역 가림을 비교

| 캡처 | 판정 | 관찰 |
|---|---|---|
| `wave11-slot-upper-ko-approach-1280x800.png` | RED_EXPECTED_FAIL | `slot.start.upper` 전용 안내가 아니라 기존 `창고·증축 계획 지점 사용` 안내가 표시된다. 직접 슬롯 발견 증거가 아니다. |
| `wave11-slot-side-en-approach-1280x800.png` | RED_EXPECTED_FAIL | `slot.start.side` 전용 안내와 팝업 진입점이 없다. 기존 Facilities Expansion 월드 표지만 보인다. |
| `wave11-slot-basement-qps-long-approach-1280x800.png` | RED_EXPECTED_FAIL | `slot.start.basement` 대신 인접 Workbench 안내가 선택된다. qps-long 글리프는 표시되지만 직접 슬롯 의미는 없다. |
| `wave11-storage-planning-aux-upper-ko-preview-1280x800.png` | PASS_NOT_SUBSTITUTE | 보조 `storage.planning`에서 위층 후보·유효 geometry·LOCKED/W2/D1·취소 안내가 화면 안에 표시된다. |
| `wave11-storage-planning-aux-side-en-preview-1280x800.png` | PASS_NOT_SUBSTITUTE | 보조 경로에서 Side 후보·connector·경제/geometry 문구가 화면 안에 표시된다. |
| `wave11-storage-planning-aux-basement-qps-long-preview-1280x800.png` | PASS_NOT_SUBSTITUTE | 실제 qps-long 데이터로 Basement 후보가 표시되고 화면 밖 이탈은 없다. 이 캡처는 직접 슬롯 PASS를 대신하지 않는다. |

직접 슬롯 미구현 때문에 요구된 직접 접근 3장과 직접 preview 3장 중 접근 시도 3장만 생성되었다. 따라서 직접 슬롯의 보행 통로 비가림 수치 판정은 `0/3`, 캡처 계약은 `3/6`으로 RED를 유지한다. 보조 경로의 preview 캡처 3장은 제품 회귀 감시용으로만 분리했다.

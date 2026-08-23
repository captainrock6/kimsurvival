# Wave 9 공간형 베이스캠프 통합 게이트

- 기준 커밋: `6ac27bf6f4370ea2642b56d02d6366862387f539`
- 실행 ID: `20260823T131600Z_6ac27bf_wave8_integration_gate`
- Unity: `6000.4.9f1`
- 실행 정책: `Docs/QA/unity-codex-sandbox-licensing.md`에 따라 Unity Editor와 Windows Player를 Codex 제한 샌드박스 밖에서 실행했다.
- 목적: Wave 8 Unity 공간 사용 결과와 기존 기획·QA 결과를 통합한 뒤, 새 공간형 캠프 재기준이 기존 플레이 루프를 컴파일·빌드 가능한 상태로 보존하는지 확인한다.

## 통과

- Unity 스크립트 컴파일: 오류 0, 경고 0
- Wave 7 가방 확장 Edit / Play / Layout 계약: PASS / PASS / PASS
- Wave 6 진행 Edit / Play 계약: PASS / PASS
- Editor 별도 프로세스의 ko/en 언어 설정 복원: PASS
- Addressables load / build / post-smoke 계약: PASS
- Windows x64 Development build / 숨김 스모크: PASS / PASS, 빌드 경고 0

## 새 방향과 충돌한 옛 계약

- Legacy Edit의 구조 신호 전용 앵커 검사는 김씨가 신호대 근처에 있지 않은 상태에서 전역 버튼을 눌러 즉시 업그레이드되기를 기대해 FAIL했다.
- Legacy 자연 플레이 루프는 캠프 설비와 거리가 먼 상태에서 비활성화된 전역 행동 버튼을 제출해 FAIL했다.
- 위 두 실패는 `PrototypeCampUse`가 도입한 근접 사용 계약과 옛 대시보드형 테스트 전제가 충돌한 결과다. 제품 코드를 원거리 사용으로 되돌리지 않고 Wave 9 공간형 계약 게이트에서 접근·안내·상호작용·팝업 순서로 테스트를 교체한다.
- Legacy Play가 중단되어 그 뒤의 기존 배치·수색/수영 시각 리포트가 생성되지 않았고, 자산 게이트는 해당 리포트 누락을 인프라 FAIL로 집계했다.

## 별도 미해결 제품 항목

- 현지화 canonical 키 계약: Forge 기대 138, 현재 153
- 플레이어 문구의 named placeholder와 action placeholder 미구현
- 실제 데이터 로케일 `qps-long` 미구현
- 물리 게임패드 실기: UNVERIFIED
- Steam 릴리스 준비: NOT_READY

## 판정

선별 통합물은 컴파일과 Windows 빌드가 가능한 다음 개발 기준점으로 사용할 수 있다. 다만 현재 캠프 UI는 최종 의도에 도달하지 않았고 옛 회귀 도구도 새 공간형 상호작용을 이해하지 못하므로 릴리스 준비 상태로 판정하지 않는다. 다음 Wave 9의 Unity 구현과 레드 퍼스트 QA 게이트가 두 항목을 함께 교체한다.


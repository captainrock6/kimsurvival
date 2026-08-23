# Wave 13 로컬 플레이테스트 로그

이 로그는 Windows **Development Build에서만** 재현 순서를 확인하기 위한 로컬 JSONL 기록이다. 외부 전송, 네트워크 호출, 계정·사용자 이름·하드웨어 식별자 수집은 하지 않는다. Editor와 일반 Release Build에서는 로그 객체와 파일 sink가 생성되지 않는다.

## 파일 위치

게임 실행 시 Unity의 `Application.persistentDataPath` 아래 `PlaytestLogs` 폴더에 세션별 파일 하나가 만들어진다.

Windows 기본 위치:

```text
%USERPROFILE%\AppData\LocalLow\Kim Survival Studio\김씨 생존기_ 무인도\PlaytestLogs\
```

Windows가 제품명의 문장 부호를 다르게 정리한 경우 `%USERPROFILE%\AppData\LocalLow\Kim Survival Studio`에서 가장 최근 제품 폴더의 `PlaytestLogs`를 찾는다. Development Player의 `Player.log`에도 `[Kim Survival Playtest] Development-only local JSONL:` 뒤에 이번 파일의 정확한 절대 경로가 한 번 출력된다.

파일명 형식은 `kim-survival-playtest-<UTC 세션 시각>-<임의 8자>.jsonl`이다. 각 줄은 독립적인 JSON 객체이므로 한 줄이 한 이벤트다.

## 문제 보고에 첨부하는 방법

1. 문제를 재현한 뒤 게임을 정상 종료한다.
2. `PlaytestLogs`에서 수정 시각이 가장 최근인 `.jsonl` 파일을 고른다.
3. 파일을 수정하지 않고 그대로 첨부한다. 용량 제한이 있으면 해당 파일만 ZIP으로 압축한다.
4. 보고 본문에 대략적인 현지 시각, 사용 언어, 키보드·마우스 또는 게임패드, 재현 직전 행동 2~3개를 함께 적는다.

로그에는 자유 입력 텍스트가 없으며 게임 상태와 안정적인 코드 값만 들어간다. 그래도 공유 전에 다른 파일을 실수로 함께 압축하지 않았는지 확인한다.

## 기록 계약

모든 이벤트는 `schema_version`, 세션 내 단조 증가 `sequence`, `run_id`, UTC 시각, 안정적인 `event_name`, 별도 `locale`과 `input_device`, `state_before`와 `state_after`를 가진다. 두 상태에는 게임 진행값과 그 값을 정규화해 계산한 SHA-256 `fingerprint`가 포함된다.

주요 이벤트 이름:

- 날짜·페이즈·결과: `day.changed`, `day.survived`, `phase.changed`, `run.completed`
- 자원: `resource.changed` (`resource`, `resource_location`, 부호 있는 `delta` 포함)
- 설비: `facility.proximity.entered|exited`, `facility.popup.opened|closed`, `facility.action.completed|rejected`
- 성장: `research.completed`, `crafting.completed`, `bag.capacity.upgraded`
- 탐색: `swimming.entered|exited`, `vine_barrier.blocked|cleared`
- 구조: `signal.stage1.completed`, `signal.stage2.completed`

이벤트 기록 실패는 경고 후 해당 세션의 로깅만 중단하며 게임 행동이나 자원 처리는 되돌리거나 재시도하지 않는다.

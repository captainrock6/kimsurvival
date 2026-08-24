# Windows 방화벽 반복 팝업 방지

자동 Unity 플레이어는 모든 검증에서 프로젝트별 고정 경로 `work/ParallelQA/StableWindowsBuild/KimSurvivalIsland.exe`를 사용한다. RunId는 증거 폴더와 로그에만 남고 실행 파일 경로에는 들어가지 않는다.

- Windows 개발 빌드는 `BuildOptions.Development`만 사용하며 일반 스모크에 불필요한 `AllowDebugging`은 사용하지 않는다.
- 새 빌드 전 고정 출력 폴더를 비워 이전 파일이 섞이지 않게 한다.
- 스모크는 RunId, baseline commit, 실행 파일 경로와 SHA-256이 현재 빌드 증거와 모두 같을 때만 플레이어를 실행한다.
- 플레이테스트 패키징도 같은 고정 빌드와 SHA-256을 검증한다.

현재 프로토타입은 인바운드 네트워크 기능을 사용하지 않는다. Windows가 Unity 플레이어의 인바운드 허용 여부를 반복 질문하는 환경에서는 관리자 PowerShell에서 다음 스크립트를 한 번 실행한다.

```powershell
& '.\Assets\Editor\ParallelQA\Set-KimSurvivalQaFirewallRule.ps1'
```

이 규칙은 Unity Editor나 다른 프로그램을 허용하지 않는다. 해당 프로젝트의 고정 QA 플레이어 경로 하나에 대해서만 모든 프로필의 인바운드 연결을 차단하며, 아웃바운드 연결과 향후 Steam 빌드에는 적용하지 않는다.

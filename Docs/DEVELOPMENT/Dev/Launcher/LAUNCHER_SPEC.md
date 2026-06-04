# ClaudeCodeHelper 요구 명세

## 개요

- **목적**: 자주 쓰는 프로젝트 폴더를 등록해 두고, 목록에서 선택해 해당 폴더에서 Claude Code(`claude` CLI)를 실행하는 런처 툴.
- **배경**: 기존 `ClaudeHelper.exe`(WinForms / .NET Framework) 프로젝트 소실. 이번엔 git으로 관리되는 새 프로젝트로 재구성한다.
- **성격**: 기존 툴과 **별개의 새 툴**. 기존 `ClaudeHelper.json`은 마이그레이션하지 않는다.
- **프레임워크**: WPF / .NET 8 (`net8.0-windows`).
- **참고**: 원본 동작은 `ClaudeHelper.exe` 메타데이터 분석으로 파악 (클래스 `Form1` / `PathManager` / `ProcessLauncher` / `SimpleJsonParser`, 실행 커맨드 `cmd.exe /k claude`).

## 기능 요구사항

### FR-1 경로 입력 / 선택
- 상단 텍스트박스(`txtPath`)에 경로 직접 입력 가능.
- "찾아보기"(`btnBrowse`) → 폴더 선택 다이얼로그로 폴더 지정 후 텍스트박스에 반영.

### FR-2 Claude 실행
- "실행"(`btnRun`) 클릭 시 선택 경로 유효성 검사 — 존재하지 않으면 안내 메시지.
- `cmd.exe` + 인자 `/k claude` 실행, `WorkingDirectory` = 선택 경로 (`/k`로 창 유지).
- 실행한 경로를 히스토리 최상단에 추가/갱신하고 `LastUsedPath`로 저장.

### FR-3 경로 목록 관리
- 저장된 경로 목록(`lstPaths`, ListBox) 표시.
- 항목 선택 시 텍스트박스에 반영.
- "삭제"(`btnDelete`): 선택 경로 제거 — 선택 없으면 안내, 확인 후 삭제.
- "전체 삭제"(`btnClearAll`): 모든 경로 제거 — 확인 후 삭제.
- "폴더 열기"(`btnOpenFolder`): `explorer.exe`로 선택 경로 열기.

### FR-4 설정 영속화
- config 파일: `ClaudeCodeHelper.json` (실행 파일 옆).
- 구조: `PathHistory`(string 배열) + `LastUsedPath`(string) + `FullPermissionMode`(bool).
- 첫 실행 시 빈 목록(`PathHistory: []`)에서 시작.
- 직렬화/역직렬화: `System.Text.Json` 사용.

### FR-5 모든 권한으로 실행 (전역 토글)
- 목적: 선택한 프로젝트에서 Claude가 모든 권한을 부여받게 한다(권한 프롬프트 생략).
- "모든 권한으로 실행" 체크박스(`chkFullPermission`) — 런처 전체에 적용되는 **전역 토글**.
- 체크 ON 시 실행: `cmd /k claude --dangerously-skip-permissions`, OFF 시: `cmd /k claude`.
- 체크 상태는 `FullPermissionMode`로 config에 저장 → 재시작 시 복원.
- 전역 토글이므로 신규 프로젝트도 현재 토글 상태를 그대로 따름(경로별 설정 없음).

## 비기능 요구사항

- **플랫폼**: Windows 전용 (`net8.0-windows`, WPF).
- **복원 범위**: 원본 동작 1:1 복원 (군더더기 기능 없음).
- **구조**: 원본의 계층 분리 유지 — `PathManager`(경로 관리) / `ProcessLauncher`(실행) 분리. UI는 `MainWindow` code-behind (1:1 복원이므로 MVVM 미적용).
- **안정성**: config 파일 부재/손상 시 안전하게 빈 상태로 시작.

## 결정사항

| 항목 | 결정 | 비고 |
|------|------|------|
| 프레임워크 | WPF / .NET 8 | 기존 스캐폴딩 유지 (원본은 WinForms였음) |
| 복원 범위 | 원본 1:1 + α | 1:1 복원 후 "모든 권한으로 실행"(FR-5) 추가 |
| 실행 방식 | `cmd /k claude` | 원본과 동일, 검증된 방식 |
| config 파일명 | `ClaudeCodeHelper.json` | 빈 목록 시작, 기존 파일 마이그레이션 안 함 |
| JSON 처리 | `System.Text.Json` | 원본의 정규식 자체 파서(`SimpleJsonParser`)는 복원하지 않음 — 동작 동일 + 견고 |
| 권한 부여 방식 | 전역 토글(C안) | 경로별이 아닌 런처 전역 1개. `--dangerously-skip-permissions` 플래그 방식(파일 미생성) |

## 미해결

- 없음. 구현 진입 가능.

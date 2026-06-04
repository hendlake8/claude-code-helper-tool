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
- 구조: `PathHistory`(string 배열) + `LastUsedPath`(string) + `FullPermissionMode`(bool) + `FavoritePaths`(string 배열).
- 첫 실행 시 빈 목록(`PathHistory: []`)에서 시작.
- 직렬화/역직렬화: `System.Text.Json` 사용.

### FR-5 모든 권한으로 실행 (전역 토글)
- 목적: 선택한 프로젝트에서 Claude가 모든 권한을 부여받게 한다(권한 프롬프트 생략).
- "모든 권한으로 실행" 체크박스(`chkFullPermission`) — 런처 전체에 적용되는 **전역 토글**.
- 체크 ON 시 실행: `cmd /k claude --dangerously-skip-permissions`, OFF 시: `cmd /k claude`.
- 체크 상태는 `FullPermissionMode`로 config에 저장 → 재시작 시 복원.
- 전역 토글이므로 신규 프로젝트도 현재 토글 상태를 그대로 따름(경로별 설정 없음).

### FR-6 MCP 관리 (별도 창)
- 목적: 선택한 프로젝트(들)에 로컬/프로젝트 스코프 MCP 서버를 일괄 설치·제거한다.
- 진입: 메인 창에 "MCP 관리" 버튼 → **별도 "MCP 관리" 창** 오픈. 메인 런처는 단순하게 유지.
- 설치 메커니즘: **`claude mcp add` CLI**로만 등록한다(직접 JSON 작성 금지 — 사용자 글로벌 룰). 각 선택 프로젝트를 작업 디렉터리로 하여 실행.
  - 추가: `claude mcp add -s <scope> <name> -- <command> [args...]`
  - 제거: `claude mcp remove -s <scope> <name>` (**항상 스코프 명시** → 글로벌 보호)

- **FR-6.1 스코프 선택**: 추가/제거 시 `-s local` / `-s project` 선택(라디오 등). 기본 `local`.
- **FR-6.2 MCP 정의 출처**: 프리셋 선택 + 직접 입력 둘 다 지원.
- **FR-6.3 프리셋 관리(CRUD)**: 프리셋 추가/편집/삭제. 별도 파일 `mcp_presets.json`에 저장.
  - 프리셋 스키마(잠정): `{ Name, Command, Args[] }`. 환경변수(env)는 현재 미지원(YAGNI) — 별도 파일이라 추후 무손실 추가 가능.
- **FR-6.4 다중 프로젝트 일괄 적용**: 프로젝트 목록(런처의 `PathHistory` 재사용)에서 **다중 선택** → 선택 프로젝트 × 선택 MCP로 추가/제거 반복 실행.
- **FR-6.5 설치된 로컬 MCP 목록 보기**: `claude mcp list`는 스코프 필터가 없어 글로벌까지 섞여 나오므로 그대로 쓰지 않는다. **툴이 자체 설치 기록(프로젝트별)을 보관**하여 로컬 설치분만 표시한다.
- **FR-6.6 결과 표시**: 다중 실행이므로 cmd 창 대신 **창 안 로그**에 성공/실패를 모아 표시.

### FR-7 즐겨찾기 (별도 리스트)
- 목적: 자주 쓰는 프로젝트를 별도 즐겨찾기 리스트로 빠르게 접근한다.
- UI: 메인 창에 **즐겨찾기 리스트(위) + 저장된 경로 목록(아래)** 2개 목록. 창 크기를 늘리면 **두 목록 모두** 비율로 늘어난다(둘 다 `*` 높이 배분).
- **즐겨찾기 토글 버튼**: 선택 항목을 토글. **두 목록 어느 쪽에서 선택하든** 동작.
  - 비즐겨찾기 선택 → 즐겨찾기에 추가(최상단 삽입). 즐겨찾기인 경로 선택 → 즐겨찾기에서 제거.
- **선택 연동**: 두 목록 중 어느 쪽에서 선택하든 `txtPath`에 반영 → 기존 실행/삭제/폴더 열기 버튼이 그 경로에 그대로 동작(즐겨찾기 항목도 전부 동작).
- **저장**: config에 `FavoritePaths`(string 배열) 추가. 기존 파일 호환(필드 없으면 빈 목록).
- **정렬**: 정렬 없음 — 마지막 등록한 항목이 맨 위(추가 시 최상단 삽입). 수동 재정렬(드래그)은 보류(YAGNI).
- **삭제 연동**: 경로를 "삭제/전체 삭제"로 완전 제거하면 `FavoritePaths`에서도 함께 제거.

## 비기능 요구사항

- **플랫폼**: Windows 전용 (`net8.0-windows`, WPF).
- **복원 범위**: 원본 동작 1:1 복원 (군더더기 기능 없음).
- **구조**: 원본의 계층 분리 유지 — `PathManager`(경로 관리) / `ProcessLauncher`(실행) 분리. UI는 `MainWindow` code-behind (1:1 복원이므로 MVVM 미적용).
- **안정성**: config 파일 부재/손상 시 안전하게 빈 상태로 시작.

## 결정사항

| 항목 | 결정 | 비고 |
|------|------|------|
| 프레임워크 | WPF / .NET 8 | 기존 스캐폴딩 유지 (원본은 WinForms였음) |
| 복원 범위 | 원본 1:1 + α | 1:1 복원 후 모든 권한 실행(FR-5)·MCP 관리(FR-6)·즐겨찾기(FR-7) 추가 |
| 실행 방식 | `cmd /k claude` | 원본과 동일, 검증된 방식 |
| config 파일명 | `ClaudeCodeHelper.json` | 빈 목록 시작, 기존 파일 마이그레이션 안 함 |
| JSON 처리 | `System.Text.Json` | 원본의 정규식 자체 파서(`SimpleJsonParser`)는 복원하지 않음 — 동작 동일 + 견고 |
| 권한 부여 방식 | 전역 토글(C안) | 경로별이 아닌 런처 전역 1개. `--dangerously-skip-permissions` 플래그 방식(파일 미생성) |
| MCP 관리 UI | 별도 창 | 메인에 "MCP 관리" 버튼 → 별도 창. 다중선택과 단일선택(실행) 충돌 회피 |
| MCP 등록 방식 | `claude mcp add/remove` CLI | 직접 JSON 작성 금지(사용자 룰). 제거 시 스코프 명시로 글로벌 보호 |
| 프리셋 저장 | 별도 `mcp_presets.json` | config와 분리. env 필드는 YAGNI로 제외(추후 무손실 추가 가능) |
| 설치 목록 | 툴 자체 기록 | `claude mcp list`가 스코프 미구분 → 글로벌 혼입 방지 위해 자체 기록 사용 |
| 즐겨찾기 UI | 별도 리스트(상하) | 즐겨찾기(위)+저장 목록(아래), 둘 다 창 크기 따라 늘어남. 두 목록 선택 모두 txtPath 연동 |
| 즐겨찾기 정렬 | 최신 등록 위 | 정렬/드래그 재정렬 없음(YAGNI). config `FavoritePaths` 배열 |

## 미해결

- MCP 관리 기능(FR-6)은 SPEC 확정. 상세 설계(`/hs:design`)에서 결정할 사항:
  - 프리셋/설치기록 파일 구조 최종(파일 분리 vs 통합, 설치기록 저장 위치)
  - MCP 관리 창 레이아웃(프로젝트 다중선택 ↔ 프리셋 목록 ↔ 스코프 ↔ 로그)
  - 다중 add/remove 실행 방식(순차 실행 + 결과 수집)
- FR-1~FR-5: 구현 완료.

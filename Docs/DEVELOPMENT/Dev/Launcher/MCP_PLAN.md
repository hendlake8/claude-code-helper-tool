# 로컬MCP 관리 구현 계획서

## 개요
- 목적: 선택한 여러 프로젝트에 **통문장 명령**(`claude mcp add ...` 전체)을 실행해 MCP를 추가하고, local 스코프 설치분을 추적·제거하는 별도 창(FR-6)을 구현한다.
- 참조 설계 문서: `Docs/DEVELOPMENT/Dev/Launcher/LAUNCHER_DESIGN.md` (## 로컬MCP 관리 (FR-6) 설계)
- 참조 명세 문서: `Docs/DEVELOPMENT/Dev/Launcher/LAUNCHER_SPEC.md` (FR-6.1~6.6)

> **모델 변경(2026-06)**: 초기 구현은 프리셋 3분할 입력(이름/명령/인자)·스코프 라디오·`McpPresetDialog`(모달) 기반이었으나, **통문장 단일 입력 실행 모델**로 단순화했다. 본 계획서는 갱신 모델 기준으로 재작성됨. (구 Phase 2-1 `McpPresetDialog`는 폐기됨)

## 제약 / 가정
- 제약: MCP 등록은 `claude mcp add/remove` CLI로만. 추가는 통문장을 **원본 그대로** `cmd /c` 실행(스코프·옵션 명령문에 포함). 제거는 `-s <scope>` 명시(글로벌 보호). 추적 대상은 local만. Windows 전용.
- 가정: `claude` CLI가 PATH에 있음(셸 스크립트일 수 있어 `cmd /c`로 실행). 기존 FR-1~5 코드/`ProcessLauncher`는 변경하지 않음.

## 리스크
- `claude`가 .cmd 셸 스크립트 → `Process.Start("claude")` 직접 실행 실패. 완화: `cmd /c`(Phase 1-3).
- 다중 실행 중 UI 프리징. 완화: `Task.Run` + `Dispatcher`(Phase 2-1).
- 통문장 따옴표/공백 깨짐. 완화: `Arguments` raw passthrough(per-arg 재인용 안 함)(Phase 1-3).
- 통문장에서 name/scope 오파싱 → 추적 누락/오류. 완화: `--` 구분자·값동반 플래그 처리 파서(Phase 1-3), 파싱 실패는 로그 안내.

## 구현 순서

### Phase 1-1: 프리셋 데이터 계층 (Windows)
- `McpPreset.cs` — `Name`/`CommandLine`(통문장) POCO, XML summary 주석
- `McpPresetStore.cs` — 생성자(filePath), `Presets` 읽기전용, `Load`/`Save`(System.Text.Json, 부재/손상 시 빈 목록), `AddOrUpdate`(이름 기준)/`Remove`
- 파일 경로 = `AppContext.BaseDirectory` + `mcp_presets.json`

### Phase 1-2: 설치 기록 계층 (Windows)
- `InstalledMcp.cs` — `Name`/`Scope` POCO
- `McpInstallTracker.cs` — 생성자(filePath), `Dictionary<string, List<InstalledMcp>>`, `Load`/`Save`(`mcp_installed.json`), `GetInstalled`/`RecordAdd`(중복 갱신)/`RecordRemove`

### Phase 1-3: MCP 실행 유틸 + 파서 (Windows)
- `McpCommandResult.cs` — `Success`/`Output`
- `McpRunner.cs` static 클래스
- `RunCommandLine(projectPath, commandLine)` — `cmd.exe`, `Arguments="/c " + commandLine`(raw passthrough), `UseShellExecute=false` + stdout/stderr 리다이렉트
- `Remove(projectPath, name, scope)` — `claude mcp remove -s scope name`(ArgumentList)
- `TryParseAddTarget(commandLine, out name, out scope)` — `--` 구분자로 head 분리 → 토큰화 → 값동반 플래그 소비 → name(add 다음/마지막 위치인자) + scope(-s·--scope, 기본 local)
- 공통 캡처 private `CaptureProcess`, 종료코드 0 → 성공

### Phase 2-1: 로컬MCP 관리 창 (Windows)
- `McpManagerWindow.xaml` — 프로젝트 ListBox(`Extended`) / 프리셋 ListBox(`Single`) / 프리셋 이름 TextBox + [프리셋으로 저장][프리셋 삭제] / 명령 입력 TextBox(`AcceptsReturn=False`, `NoWrap`, 가로스크롤) + [선택 프로젝트에서 실행] / 설치목록 + [새로고침][선택 항목 제거] / 로그 TextBox(읽기전용)
- 생성자(`PathManager`) — `McpPresetStore`/`McpInstallTracker` Load, 프로젝트 목록=`PathHistory`, 프리셋 목록 바인딩
- `LstPresets_SelectionChanged` — 선택 프리셋의 이름/통문장을 입력 필드에 채움
- `BtnSavePreset_Click` — 이름+명령 검증 → `AddOrUpdate`/`Save`/갱신(통문장 원본 그대로 저장)
- `BtnDeletePreset_Click` — 선택 프리셋 `Remove`/`Save`/갱신
- `BtnRun_Click` — 선택 프로젝트 루프: `RunCommandLine` → `AppendLog` → 성공 시 `RecordIfLocal`. `Task.Run`+`Dispatcher`
- `RecordIfLocal` — `TryParseAddTarget` → `scope==local`만 `RecordAdd`, 그 외 로그 안내
- `BtnRemove_Click` — 선택 프로젝트 × 선택 설치 MCP 루프: `Remove(-s 명시)` → 성공 시 `RecordRemove`
- 설치목록 표시/`새로고침` — 선택 프로젝트의 `GetInstalled`, 작업 후 `Tracker.Save`

### Phase 3-1: 메인 창 통합 (Windows)
- `MainWindow.xaml`에 "로컬MCP 관리" 버튼
- `BtnMcpManager_Click` — `new McpManagerWindow(_pathManager).Show()` (비모달)

### Phase 4-1: 빌드 & 동작 검증 (Windows)
- `dotnet build` 통과 확인
- 창 열기 + 프리셋 저장/삭제 + 프리셋 클릭→입력필드 채움 동작 확인
- 단일/다중 프로젝트에 통문장 실행 → `claude mcp get`/설정으로 실제 등록 확인
- local 통문장 → 설치목록 표시 / user·project 통문장 → 추적 제외 로그 확인
- `remove -s local` 동작 + 글로벌(user) MCP 미영향 확인
- `mcp_presets.json`/`mcp_installed.json` 영속화 + 재시작 복원 확인

## 미해결 / 추후 결정 사항
- 없음. 모델 변경 결정(통문장 저장 / local만 추적 / 마이그레이션 스킵 / 단일라인 가로스크롤) 모두 확정·구현 완료.

# MCP 관리 구현 계획서

## 개요
- 목적: 선택한 여러 프로젝트에 로컬/프로젝트 스코프 MCP를 프리셋/직접입력으로 일괄 추가·제거하는 별도 창(FR-6)을 구현한다.
- 참조 설계 문서: `Docs/DEVELOPMENT/Dev/Launcher/LAUNCHER_DESIGN.md` (## MCP 관리 (FR-6) 설계)
- 참조 명세 문서: `Docs/DEVELOPMENT/Dev/Launcher/LAUNCHER_SPEC.md` (FR-6.1~6.6)

## 제약 / 가정
- 제약: MCP 등록은 `claude mcp add/remove` CLI로만. 제거는 항상 `-s <scope>` 명시(글로벌 보호). Windows 전용.
- 가정: `claude` CLI가 PATH에 있음(셸 스크립트일 수 있어 `cmd /c`로 실행). 기존 FR-1~5 코드/`ProcessLauncher`는 변경하지 않음.

## 리스크
- `claude`가 .cmd 셸 스크립트 → `Process.Start("claude")` 직접 실행 실패. 완화: `McpRunner`에서 `cmd /c claude ...`(Phase 1-3).
- 다중 add/remove 중 UI 프리징. 완화: `Task.Run` + `Dispatcher`(Phase 2-2).
- 인자 escaping(공백/특수문자). 완화: `ArgumentList` 사용(Phase 1-3), 검증 단계에서 확인.

## 구현 순서

### Phase 1-1: 프리셋 데이터 계층 (Windows)
- `McpPreset.cs` 생성 — `Name`/`Command`/`Args`(List<string>) POCO, XML summary 주석
- `McpPresetStore.cs` 생성 — 생성자(filePath), `Presets` 읽기전용 프로퍼티
- `Load()`/`Save()` 구현 — `System.Text.Json`, 파일 부재/손상 시 빈 목록(try/catch)
- `AddOrUpdate()`(이름 기준) / `Remove()` 구현
- 파일 경로 = `AppContext.BaseDirectory` + `mcp_presets.json`

### Phase 1-2: 설치 기록 계층 (Windows)
- `InstalledMcp.cs` 생성 — `Name`/`Scope` POCO
- `McpInstallTracker.cs` 생성 — 생성자(filePath), 내부 `Dictionary<string, List<InstalledMcp>>`(프로젝트 경로 → 설치 목록)
- `Load()`/`Save()` 구현 — `mcp_installed.json`, 부재/손상 시 빈 상태
- `GetInstalled(projectPath)` 구현
- `RecordAdd()`(중복 시 갱신) / `RecordRemove()` 구현

### Phase 1-3: MCP 실행 유틸 (Windows)
- `McpCommandResult.cs` 생성 — `Success`/`Output`
- `McpRunner.cs` 생성 — static 클래스
- `Add()` 구현 — `cmd.exe /c` + `ArgumentList { "/c","claude","mcp","add","-s",scope,name,"--",command,...args }`, `UseShellExecute=false` + stdout/stderr 리다이렉트
- `Remove()` 구현 — `claude mcp remove -s scope name` (스코프 명시)
- 종료코드 0 → `Success`, stdout+stderr 합쳐 `Output`에 저장

### Phase 2-1: 프리셋 입력 다이얼로그 (Windows)
- `McpPresetDialog.xaml`/`.cs` 생성 — `Name`/`Command`/`Args` 입력 필드(Args는 줄/공백 구분 파싱)
- 확인 시 `McpPreset Result` 채우고 `DialogResult=true`
- 편집 모드 지원 — 기존 프리셋 값으로 필드 초기화(추가/편집/직접입력 공용)

### Phase 2-2: MCP 관리 창 (Windows)
- `McpManagerWindow.xaml` 작성 — 프로젝트 ListBox(`Extended`)/프리셋 ListBox(`Extended`)/스코프 라디오(local·project)/[추가][제거] 버튼/설치목록/로그 TextBox(읽기전용)
- 생성자(`PathManager`) — `McpPresetStore`/`McpInstallTracker` Load, 프로젝트 목록=`PathHistory` 바인딩, 프리셋 목록 바인딩
- 프리셋 CRUD 핸들러 — 추가/편집(`McpPresetDialog`)→`AddOrUpdate`/`Save`→갱신, 삭제→`Remove`/`Save`/갱신
- 직접입력 핸들러 — `McpPresetDialog`로 1회 입력 후 즉시 설치 대상에 사용(저장 안 함)
- `BtnInstall_Click` → `RunBatchAsync` — 선택 프로젝트 × 선택 프리셋 루프: `McpRunner.Add` → `AppendLog` → 성공 시 `RecordAdd`. `Task.Run`+`Dispatcher`
- `BtnRemove_Click` — 선택 프로젝트 × 선택 MCP 루프: `McpRunner.Remove(-s 명시)` → `AppendLog` → 성공 시 `RecordRemove`
- 설치목록 표시/`새로고침` — 선택 프로젝트의 `GetInstalled`, `AppendLog` 헬퍼, 작업 후 `Tracker.Save`

### Phase 3-1: 메인 창 통합 (Windows)
- `MainWindow.xaml`에 "MCP 관리" 버튼 추가
- `BtnMcpManager_Click` — `new McpManagerWindow(_pathManager).Show()` (비모달)

### Phase 4-1: 빌드 & 동작 검증 (Windows)
- `dotnet build` 통과 확인
- MCP 관리 창 열기 + 프리셋 추가/편집/삭제 + 직접입력 동작 확인
- 단일/다중 프로젝트에 add → `claude mcp get`/설정으로 실제 등록 확인
- `remove -s local` 동작 + 글로벌(user) MCP 미영향 확인
- 설치목록 표시 + `mcp_presets.json`/`mcp_installed.json` 영속화 + 재시작 복원 확인

## 미해결 / 추후 결정 사항
- 없음. 설계 단계에서 4건(인자 escaping/claude 미설치/드리프트/비모달) 모두 확정됨. 구현 진입 가능.

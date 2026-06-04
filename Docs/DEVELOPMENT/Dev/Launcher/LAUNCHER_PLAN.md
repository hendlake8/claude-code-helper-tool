# ClaudeCodeHelper 구현 계획서

## 개요
- 목적: 폴더 선택 후 해당 폴더에서 `claude` CLI를 실행하는 WPF 런처를 SPEC/DESIGN대로 구현한다.
- 참조 설계 문서: `Docs/DEVELOPMENT/Dev/Launcher/LAUNCHER_DESIGN.md`
- 참조 명세 문서: `Docs/DEVELOPMENT/Dev/Launcher/LAUNCHER_SPEC.md`

## 제약 / 가정
- 제약: Windows 전용(`net8.0-windows`, WPF), `System.Text.Json` + `Microsoft.Win32.OpenFolderDialog`(.NET 8 내장)만 사용, NuGet 추가 없음.
- 가정: 현재 프로젝트는 빈 WPF 스캐폴딩 상태(`MainWindow`가 비어 있음). `claude`는 PATH에 등록돼 있다.

## 리스크
- `OpenFolderDialog` 미인지 — .NET 8 미만 타깃이면 컴파일 실패. 완화: csproj가 `net8.0-windows`인지 Phase 1-1에서 확인.
- config 파일 손상 시 앱 크래시 — 완화: `PathManager.Load`에서 try/catch로 빈 상태 복구(Phase 1-2 태스크에 포함).

## 구현 순서

### Phase 1-1: 프로젝트 설정 & 설정 모델 (Windows)
- `ClaudeCodeHelper.csproj`가 `net8.0-windows` + `UseWPF`인지 확인 (이미 충족 시 변경 없음)
- `AppConfig.cs` 생성 — `PathHistory`(List<string>), `LastUsedPath`(string) POCO, XML summary 주석 포함
- config 파일 경로 상수/헬퍼 결정 — `AppContext.BaseDirectory` + `ClaudeCodeHelper.json`

### Phase 1-2: PathManager 구현 (Windows)
- `PathManager.cs` 생성 — 생성자(configFilePath), `PathHistory`/`LastUsedPath` 프로퍼티
- `Load()` 구현 — `System.Text.Json` 역직렬화, 파일 부재/손상 시 try/catch로 빈 상태 시작
- `Save()` 구현 — 직렬화 후 파일 쓰기
- `AddPath()` 구현 — 중복 시 제거 후 최상단 삽입 + `LastUsedPath` 갱신
- `RemovePath()` / `ClearAll()` / `SetLastPath()` 구현

### Phase 1-3: ProcessLauncher 구현 (Windows)
- `ProcessLauncher.cs` 생성 — static 클래스, XML summary 주석
- `LaunchClaude()` 구현 — `ProcessStartInfo(cmd.exe, "/k claude", WorkingDirectory, UseShellExecute=true)` → `Process.Start`
- `OpenFolder()` 구현 — `explorer.exe`로 폴더 열기

### Phase 2-1: MainWindow XAML 레이아웃 (Windows)
- `MainWindow.xaml` 작성 — 루트 Grid 3-Row(경로 입력줄 / 목록 라벨 / ListBox+버튼 StackPanel)
- 컨트롤 배치 — `txtPath`/`btnBrowse`/`btnRun`/`lstPaths`/`btnDelete`/`btnClearAll`/`btnOpenFolder`
- Window 제목 `ClaudeCodeHelper` + 적정 크기 설정

### Phase 2-2: MainWindow 이벤트 핸들러 (Windows)
- code-behind에 `PathManager` 필드 + 생성자에서 `Load` + `RefreshPathList`
- `RefreshPathList()` — `lstPaths`를 `PathHistory`로 재바인딩
- `BtnRun_Click` — 경로 검증(빈값/`Directory.Exists`) → `AddPath`/`Save`/`RefreshPathList` → `LaunchClaude`
- `BtnBrowse_Click`(OpenFolderDialog) / `LstPaths_SelectionChanged` — `txtPath` 반영
- `BtnDelete_Click` / `BtnClearAll_Click` — 확인 메시지 → 변경 → `Save`/`RefreshPathList`
- `BtnOpenFolder_Click` — 경로 검증 후 `OpenFolder`

### Phase 3-1: 빌드 & 동작 검증 (Windows)
- `dotnet build`로 컴파일 통과 확인
- 실행 후 경로 추가 → `claude` 실행 → cmd 창 동작 확인
- 삭제/전체삭제/폴더열기 동작 + `ClaudeCodeHelper.json` 영속화 확인
- 앱 재시작 시 목록 복원 확인

## 미해결 / 추후 결정 사항
- Window 제목: `ClaudeCodeHelper`로 진행(권장). 다른 이름 원하면 Phase 2-1에서 반영.
- `claude` 미설치 시 처리: 원본대로 별도 검증 없이 진행(권장).

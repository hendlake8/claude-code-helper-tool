# ClaudeCodeHelper

자주 쓰는 프로젝트 폴더를 등록해 두고, 선택해서 해당 폴더에서 **Claude Code(`claude` CLI)** 를 빠르게 실행하는 Windows 런처 도구입니다.

## 주요 기능

- **프로젝트 폴더 런처** — 경로를 등록/선택해 해당 폴더에서 `claude`를 실행 (`cmd /k claude`)
- **즐겨찾기** — 자주 쓰는 프로젝트를 별도 목록으로 관리 (행별 ★ 토글)
- **모든 권한으로 실행** — 토글 ON 시 `claude --dangerously-skip-permissions`로 실행
- **MCP 관리** — 선택한 여러 프로젝트에 로컬/프로젝트 스코프 MCP 서버를 프리셋/직접입력으로 일괄 추가·제거 (`claude mcp add/remove`)
- **경로 영속화** — 등록 경로·즐겨찾기·설정을 `ClaudeCodeHelper.json`에 저장

## 요구 사항

- Windows
- [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0) — 단, 릴리스의 단일 exe(self-contained)는 별도 설치 불필요
- [Claude Code CLI](https://claude.com/claude-code) (`claude`가 PATH에 등록되어 있어야 실행 기능 사용 가능)

## 설치 / 실행

[Releases](https://github.com/hendlake8/claude-code-helper-tool/releases)에서 `ClaudeCodeHelper.zip`을 받아 압축을 풀고 `ClaudeCodeHelper.exe`를 실행하면 됩니다.

## 소스 빌드

```bash
# 개발 빌드
dotnet build ClaudeCodeHelper/ClaudeCodeHelper.sln

# 실행
dotnet run --project ClaudeCodeHelper/ClaudeCodeHelper.csproj
```

### 배포용 단일 exe 게시

저장소 루트의 `publish.bat`을 실행하면 `Build/` 폴더에 런타임 포함 단일 exe가 생성됩니다.

```bash
dotnet publish ClaudeCodeHelper/ClaudeCodeHelper.csproj -c Release
```

> `csproj`에 self-contained / single-file / 네이티브 라이브러리 임베드 설정이 포함되어 있어, 옵션 없이 publish만 해도 exe 하나로 어디서든 실행됩니다.

## 기술 스택

- WPF / .NET 8 (`net8.0-windows`)
- `System.Text.Json` 기반 설정 저장

## 라이선스

[MIT](LICENSE)

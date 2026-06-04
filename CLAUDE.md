# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 프로젝트 개요

Claude Code 보조 도구 (claude-code-helper-tool). WPF 데스크톱 애플리케이션이며 현재는 Visual Studio 기본 템플릿만 생성된 초기 단계다. 실제 기능 코드는 아직 작성되지 않았다.

## 기술 스택

- **프레임워크**: WPF (.NET 8 / `net8.0-windows`)
- **언어**: C# (`Nullable` enable, `ImplicitUsings` enable)
- **UI 패턴**: XAML + code-behind. `StartupUri`는 `MainWindow.xaml`이다.
- 솔루션 루트는 `ClaudeCodeHelper/ClaudeCodeHelper.sln`이며, 단일 프로젝트 `ClaudeCodeHelper.csproj`를 포함한다.

## 빌드 / 실행 명령

리포지토리 루트가 아닌 `ClaudeCodeHelper/` 하위에서 솔루션을 다룬다.

```powershell
# 빌드
dotnet build ClaudeCodeHelper/ClaudeCodeHelper.sln

# 실행 (WinExe, GUI 앱)
dotnet run --project ClaudeCodeHelper/ClaudeCodeHelper.csproj

# 릴리즈 빌드
dotnet build ClaudeCodeHelper/ClaudeCodeHelper.sln -c Release
```

- WPF는 `net8.0-windows` 타깃이므로 **Windows 환경에서만** 빌드·실행된다.
- 현재 테스트 프로젝트는 없다. 테스트를 추가할 경우 별도 테스트 프로젝트를 솔루션에 등록한다.

## 코드 구조

신규 기능을 추가할 때 진입점은 다음과 같다.

- `App.xaml` / `App.xaml.cs` — 애플리케이션 진입점. 전역 리소스(`Application.Resources`)와 시작 창 설정.
- `MainWindow.xaml` / `MainWindow.xaml.cs` — 메인 창. UI는 XAML, 동작은 code-behind에 작성한다. 현재 `Grid`만 비어 있는 상태.
- `AssemblyInfo.cs` — WPF `ThemeInfo` 어셈블리 속성.

`obj/`, `bin/`, `.vs/` 는 빌드 산출물 / IDE 캐시이므로 편집하지 않는다.

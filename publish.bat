@echo off
chcp 65001 >nul
cd /d "%~dp0"

set "OUTPUT_DIR=Build"

echo === ClaudeCodeHelper Release 게시 시작 ===
echo.

REM 이전 결과물 폴더 완전 삭제
if exist "%OUTPUT_DIR%" (
    echo 기존 %OUTPUT_DIR% 폴더 삭제 중...
    rmdir /s /q "%OUTPUT_DIR%"
)

dotnet publish ClaudeCodeHelper\ClaudeCodeHelper.csproj -c Release -o "%OUTPUT_DIR%"
if errorlevel 1 (
    echo.
    echo 게시 실패 ^(dotnet publish 오류^)
    pause
    exit /b 1
)

echo.
echo === 게시 완료 ===
echo 출력 폴더: %OUTPUT_DIR%
explorer "%OUTPUT_DIR%"

pause

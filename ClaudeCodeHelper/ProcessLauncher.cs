using System.Diagnostics;

namespace ClaudeCodeHelper
{
    /// <summary>
    /// 외부 프로세스 실행 유틸리티.
    /// </summary>
    public static class ProcessLauncher
    {
        /// <summary>
        /// 지정 폴더에서 cmd.exe로 claude CLI를 실행한다(/k로 창 유지).
        /// </summary>
        /// <param name="workingDirectory">작업 디렉터리</param>
        /// <param name="fullPermission">true면 --dangerously-skip-permissions 플래그로 모든 권한 부여</param>
        public static void LaunchClaude(string workingDirectory, bool fullPermission)
        {
            string arguments = fullPermission == true
                ? "/k claude --dangerously-skip-permissions"
                : "/k claude";

            ProcessStartInfo startInfo = new()
            {
                FileName = "cmd.exe",
                Arguments = arguments,
                WorkingDirectory = workingDirectory,
                UseShellExecute = true
            };

            Process.Start(startInfo);
        }

        /// <summary>
        /// 탐색기로 지정 폴더를 연다.
        /// </summary>
        /// <param name="path">열 폴더 경로</param>
        public static void OpenFolder(string path)
        {
            ProcessStartInfo startInfo = new()
            {
                FileName = "explorer.exe",
                Arguments = path,
                UseShellExecute = true
            };

            Process.Start(startInfo);
        }
    }
}

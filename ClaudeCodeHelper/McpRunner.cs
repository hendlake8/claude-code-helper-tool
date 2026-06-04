using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace ClaudeCodeHelper
{
    /// <summary>
    /// claude mcp add/remove 명령을 cmd 경유로 실행하고 출력을 캡처한다.
    /// Windows에서 claude가 셸 스크립트(.cmd)일 수 있어 cmd /c로 호출한다.
    /// </summary>
    public static class McpRunner
    {
        /// <summary>
        /// 지정 프로젝트에 프리셋 MCP를 추가한다.
        /// claude mcp add -s {scope} {name} -- {command} {args...}
        /// </summary>
        /// <param name="projectPath">작업 디렉터리(대상 프로젝트)</param>
        /// <param name="preset">설치할 MCP 프리셋</param>
        /// <param name="scope">스코프(local / project)</param>
        /// <returns>실행 결과</returns>
        public static McpCommandResult Add(string projectPath, McpPreset preset, string scope)
        {
            List<string> args = new()
            {
                "/c", "claude", "mcp", "add", "-s", scope, preset.Name, "--", preset.Command
            };
            args.AddRange(preset.Args);

            return Run(projectPath, args);
        }

        /// <summary>
        /// 지정 프로젝트에서 MCP를 제거한다(스코프 명시 → 글로벌 보호).
        /// claude mcp remove -s {scope} {name}
        /// </summary>
        /// <param name="projectPath">작업 디렉터리(대상 프로젝트)</param>
        /// <param name="name">제거할 MCP 이름</param>
        /// <param name="scope">스코프(local / project)</param>
        /// <returns>실행 결과</returns>
        public static McpCommandResult Remove(string projectPath, string name, string scope)
        {
            List<string> args = new()
            {
                "/c", "claude", "mcp", "remove", "-s", scope, name
            };

            return Run(projectPath, args);
        }

        /// <summary>
        /// cmd.exe로 명령을 실행하고 표준출력/에러를 캡처한다.
        /// </summary>
        /// <param name="workingDirectory">작업 디렉터리</param>
        /// <param name="argumentList">cmd.exe에 전달할 인자 목록(per-arg)</param>
        /// <returns>실행 결과</returns>
        private static McpCommandResult Run(string workingDirectory, List<string> argumentList)
        {
            ProcessStartInfo startInfo = new()
            {
                FileName = "cmd.exe",
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            foreach (string arg in argumentList)
            {
                startInfo.ArgumentList.Add(arg);
            }

            StringBuilder output = new();

            try
            {
                using Process? process = Process.Start(startInfo);
                if (process == null)
                {
                    return new McpCommandResult { Success = false, Output = "프로세스를 시작하지 못했습니다." };
                }

                string stdout = process.StandardOutput.ReadToEnd();
                string stderr = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (string.IsNullOrEmpty(stdout) == false)
                {
                    output.Append(stdout);
                }
                if (string.IsNullOrEmpty(stderr) == false)
                {
                    output.Append(stderr);
                }

                return new McpCommandResult
                {
                    Success = process.ExitCode == 0,
                    Output = output.ToString().Trim()
                };
            }
            catch (System.Exception ex)
            {
                return new McpCommandResult { Success = false, Output = ex.Message };
            }
        }
    }
}

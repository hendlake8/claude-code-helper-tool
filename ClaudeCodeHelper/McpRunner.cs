using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace ClaudeCodeHelper
{
    /// <summary>
    /// claude mcp 명령을 cmd 경유로 실행하고 출력을 캡처한다.
    /// Windows에서 claude가 셸 스크립트(.cmd)일 수 있어 cmd /c로 호출한다.
    /// </summary>
    public static class McpRunner
    {
        /// <summary>값을 동반하는 add 옵션 플래그(파싱 시 다음 토큰 소비 대상).</summary>
        private static readonly HashSet<string> VALUE_FLAGS = new()
        {
            "-s", "--scope", "-t", "--transport", "-e", "--env",
            "-H", "--header", "--client-id", "--callback-port"
        };

        /// <summary>
        /// 사용자가 입력한 전체 명령문을 cmd로 실행한다(claude mcp add 통문장 등).
        /// 명령문에 이미 따옴표가 포함돼 있으므로 원본을 그대로 전달한다.
        /// </summary>
        /// <param name="projectPath">작업 디렉터리(대상 프로젝트)</param>
        /// <param name="commandLine">실행할 전체 명령문(원본 그대로)</param>
        /// <returns>실행 결과</returns>
        public static McpCommandResult RunCommandLine(string projectPath, string commandLine)
        {
            ProcessStartInfo startInfo = new()
            {
                FileName = "cmd.exe",
                Arguments = "/c " + commandLine,
                WorkingDirectory = projectPath,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            return CaptureProcess(startInfo);
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
            ProcessStartInfo startInfo = new()
            {
                FileName = "cmd.exe",
                WorkingDirectory = projectPath,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            foreach (string arg in new[] { "/c", "claude", "mcp", "remove", "-s", scope, name })
            {
                startInfo.ArgumentList.Add(arg);
            }

            return CaptureProcess(startInfo);
        }

        /// <summary>
        /// claude mcp add 명령문에서 MCP 이름과 스코프를 파싱한다.
        /// name = "add" 다음 위치 인자(없으면 마지막 위치 인자), scope = -s/--scope 값(없으면 local).
        /// </summary>
        /// <param name="commandLine">파싱할 명령문</param>
        /// <param name="name">파싱된 MCP 이름(실패 시 빈 문자열)</param>
        /// <param name="scope">파싱된 스코프(기본 local)</param>
        /// <returns>이름 파싱 성공 여부</returns>
        public static bool TryParseAddTarget(string commandLine, out string name, out string scope)
        {
            name = "";
            scope = "local";

            if (string.IsNullOrWhiteSpace(commandLine) == true)
            {
                return false;
            }

            // "--" 앞부분(add 옵션 + name)만 사용. 뒤는 실행 커맨드/인자라 무시.
            string head = commandLine;
            int separatorIndex = FindArgSeparator(commandLine);
            if (separatorIndex >= 0)
            {
                head = commandLine.Substring(0, separatorIndex);
            }

            string[] tokens = head.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

            List<string> positionals = new();
            for (int i = 0; i < tokens.Length; i++)
            {
                string token = tokens[i];
                if (token.StartsWith("-") == true)
                {
                    if ((token == "-s" || token == "--scope") && i + 1 < tokens.Length)
                    {
                        scope = tokens[i + 1];
                    }
                    if (VALUE_FLAGS.Contains(token) == true)
                    {
                        i++;
                    }
                    continue;
                }
                positionals.Add(token);
            }

            // positionals 예상: claude, mcp, add, <name>
            int addIndex = positionals.FindIndex(t => t == "add");
            if (addIndex >= 0 && addIndex + 1 < positionals.Count)
            {
                name = positionals[addIndex + 1];
            }
            else if (positionals.Count > 0)
            {
                name = positionals[positionals.Count - 1];
            }

            return string.IsNullOrEmpty(name) == false;
        }

        /// <summary>
        /// 공백으로 둘러싸인 순수 "--" 구분자의 위치를 찾는다("--scope" 같은 긴 플래그는 제외).
        /// </summary>
        /// <param name="commandLine">검색 대상 명령문</param>
        /// <returns>구분자 시작 인덱스(없으면 -1)</returns>
        private static int FindArgSeparator(string commandLine)
        {
            int searchFrom = 0;
            while (searchFrom < commandLine.Length)
            {
                int found = commandLine.IndexOf("--", searchFrom, StringComparison.Ordinal);
                if (found < 0)
                {
                    return -1;
                }

                bool leftOk = found == 0 || char.IsWhiteSpace(commandLine[found - 1]) == true;
                int after = found + 2;
                bool rightOk = after >= commandLine.Length || char.IsWhiteSpace(commandLine[after]) == true;

                if (leftOk == true && rightOk == true)
                {
                    return found;
                }

                searchFrom = found + 2;
            }

            return -1;
        }

        /// <summary>
        /// 구성된 프로세스를 실행하고 표준출력/에러를 캡처한다.
        /// </summary>
        /// <param name="startInfo">실행할 프로세스 정보</param>
        /// <returns>실행 결과</returns>
        private static McpCommandResult CaptureProcess(ProcessStartInfo startInfo)
        {
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

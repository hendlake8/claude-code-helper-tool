using System.Collections.Generic;

namespace ClaudeCodeHelper
{
    /// <summary>
    /// MCP 프리셋 정의(직렬화 모델). claude mcp add 명령 구성에 사용한다.
    /// </summary>
    public class McpPreset
    {
        /// <summary>MCP 서버 이름(claude mcp add의 name).</summary>
        public string Name { get; set; } = "";

        /// <summary>실행 명령(예: npx, python).</summary>
        public string Command { get; set; } = "";

        /// <summary>명령 인자 목록(-- 뒤에 붙음).</summary>
        public List<string> Args { get; set; } = new();
    }
}

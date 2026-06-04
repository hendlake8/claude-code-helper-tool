namespace ClaudeCodeHelper
{
    /// <summary>
    /// MCP 프리셋 정의(직렬화 모델). 실행할 명령문 전체를 통째로 보관한다.
    /// </summary>
    public class McpPreset
    {
        /// <summary>프리셋 이름(목록 표시용).</summary>
        public string Name { get; set; } = "";

        /// <summary>실행할 전체 명령문(claude mcp add ... 통째).</summary>
        public string CommandLine { get; set; } = "";
    }
}

namespace ClaudeCodeHelper
{
    /// <summary>
    /// 특정 프로젝트에 설치된 MCP 한 건(이름 + 스코프).
    /// </summary>
    public class InstalledMcp
    {
        /// <summary>MCP 서버 이름.</summary>
        public string Name { get; set; } = "";

        /// <summary>설치 스코프(local / project).</summary>
        public string Scope { get; set; } = "local";
    }
}

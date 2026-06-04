namespace ClaudeCodeHelper
{
    /// <summary>
    /// claude mcp 명령 실행 결과(성공 여부 + 출력).
    /// </summary>
    public class McpCommandResult
    {
        /// <summary>명령 성공 여부(종료코드 0이면 true).</summary>
        public bool Success { get; set; }

        /// <summary>표준출력 + 표준에러를 합친 출력 문자열.</summary>
        public string Output { get; set; } = "";
    }
}

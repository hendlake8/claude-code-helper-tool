using System.Collections.Generic;
using System.IO;

namespace ClaudeCodeHelper
{
    /// <summary>
    /// config 파일(ClaudeCodeHelper.json)의 직렬화 모델.
    /// </summary>
    public class AppConfig
    {
        /// <summary>config 파일 이름.</summary>
        public const string FILE_NAME = "ClaudeCodeHelper.json";

        /// <summary>등록된 경로 목록(최신이 앞).</summary>
        public List<string> PathHistory { get; set; } = new();

        /// <summary>마지막으로 실행한 경로.</summary>
        public string LastUsedPath { get; set; } = "";

        /// <summary>
        /// 실행 파일과 같은 폴더의 config 파일 전체 경로를 반환한다.
        /// </summary>
        /// <returns>ClaudeCodeHelper.json의 전체 경로</returns>
        public static string GetDefaultConfigPath()
        {
            return Path.Combine(AppContext.BaseDirectory, FILE_NAME);
        }
    }
}

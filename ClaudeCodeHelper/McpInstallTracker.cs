using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace ClaudeCodeHelper
{
    /// <summary>
    /// 프로젝트별 MCP 설치 기록을 보관한다(mcp_installed.json).
    /// claude mcp list가 스코프를 구분하지 않으므로 툴 자체 기록으로 로컬 설치분을 추적한다.
    /// </summary>
    public class McpInstallTracker
    {
        /// <summary>설치 기록 파일 이름.</summary>
        public const string FILE_NAME = "mcp_installed.json";

        /// <summary>JSON 직렬화 옵션(들여쓰기 + 한글 그대로).</summary>
        private static readonly JsonSerializerOptions JSON_OPTIONS = new()
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        /// <summary>설치 기록 파일 경로.</summary>
        private readonly string _filePath;

        /// <summary>프로젝트 경로 → 설치된 MCP 목록.</summary>
        private Dictionary<string, List<InstalledMcp>> _records = new();

        /// <summary>
        /// 설치 기록 파일 경로를 지정해 생성한다.
        /// </summary>
        /// <param name="filePath">mcp_installed.json의 전체 경로</param>
        public McpInstallTracker(string filePath)
        {
            _filePath = filePath;
        }

        /// <summary>
        /// 실행 파일과 같은 폴더의 설치 기록 파일 전체 경로를 반환한다.
        /// </summary>
        /// <returns>mcp_installed.json의 전체 경로</returns>
        public static string GetDefaultPath()
        {
            return Path.Combine(AppContext.BaseDirectory, FILE_NAME);
        }

        /// <summary>
        /// 설치 기록 파일을 읽는다. 부재/손상 시 빈 상태로 시작.
        /// </summary>
        public void Load()
        {
            if (File.Exists(_filePath) == false)
            {
                _records = new Dictionary<string, List<InstalledMcp>>();
                return;
            }

            try
            {
                string json = File.ReadAllText(_filePath);
                Dictionary<string, List<InstalledMcp>>? loaded =
                    JsonSerializer.Deserialize<Dictionary<string, List<InstalledMcp>>>(json, JSON_OPTIONS);
                _records = loaded ?? new Dictionary<string, List<InstalledMcp>>();
            }
            catch (JsonException)
            {
                _records = new Dictionary<string, List<InstalledMcp>>();
            }
            catch (IOException)
            {
                _records = new Dictionary<string, List<InstalledMcp>>();
            }
        }

        /// <summary>
        /// 현재 설치 기록을 파일에 저장한다.
        /// </summary>
        public void Save()
        {
            string json = JsonSerializer.Serialize(_records, JSON_OPTIONS);
            File.WriteAllText(_filePath, json);
        }

        /// <summary>
        /// 해당 프로젝트의 설치 기록을 반환한다.
        /// </summary>
        /// <param name="projectPath">프로젝트 경로</param>
        /// <returns>설치된 MCP 목록(없으면 빈 목록)</returns>
        public IReadOnlyList<InstalledMcp> GetInstalled(string projectPath)
        {
            if (_records.TryGetValue(projectPath, out List<InstalledMcp>? list) == true)
            {
                return list;
            }

            return new List<InstalledMcp>();
        }

        /// <summary>
        /// 설치 성공 시 기록에 추가한다(같은 이름이 있으면 스코프 갱신).
        /// </summary>
        /// <param name="projectPath">프로젝트 경로</param>
        /// <param name="name">MCP 이름</param>
        /// <param name="scope">스코프</param>
        public void RecordAdd(string projectPath, string name, string scope)
        {
            if (_records.TryGetValue(projectPath, out List<InstalledMcp>? list) == false)
            {
                list = new List<InstalledMcp>();
                _records[projectPath] = list;
            }

            InstalledMcp? existing = list.Find(m => m.Name == name);
            if (existing != null)
            {
                existing.Scope = scope;
            }
            else
            {
                list.Add(new InstalledMcp { Name = name, Scope = scope });
            }
        }

        /// <summary>
        /// 제거 성공 시 기록에서 제외한다.
        /// </summary>
        /// <param name="projectPath">프로젝트 경로</param>
        /// <param name="name">MCP 이름</param>
        /// <param name="scope">스코프</param>
        public void RecordRemove(string projectPath, string name, string scope)
        {
            if (_records.TryGetValue(projectPath, out List<InstalledMcp>? list) == true)
            {
                list.RemoveAll(m => m.Name == name && m.Scope == scope);
                if (list.Count == 0)
                {
                    _records.Remove(projectPath);
                }
            }
        }
    }
}

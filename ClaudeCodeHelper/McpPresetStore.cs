using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace ClaudeCodeHelper
{
    /// <summary>
    /// MCP 프리셋 목록의 직렬화 컨테이너(mcp_presets.json 루트).
    /// </summary>
    public class McpPresetData
    {
        /// <summary>등록된 프리셋 목록.</summary>
        public List<McpPreset> Presets { get; set; } = new();
    }

    /// <summary>
    /// MCP 프리셋을 로드/저장하고 추가·삭제(CRUD)를 관리한다.
    /// </summary>
    public class McpPresetStore
    {
        /// <summary>MCP 프리셋 파일 이름.</summary>
        public const string FILE_NAME = "mcp_presets.json";

        /// <summary>JSON 직렬화 옵션(들여쓰기 + 한글 그대로).</summary>
        private static readonly JsonSerializerOptions JSON_OPTIONS = new()
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        /// <summary>프리셋 파일 경로.</summary>
        private readonly string _filePath;

        /// <summary>현재 프리셋 데이터.</summary>
        private McpPresetData _data = new();

        /// <summary>
        /// 프리셋 파일 경로를 지정해 생성한다.
        /// </summary>
        /// <param name="filePath">mcp_presets.json의 전체 경로</param>
        public McpPresetStore(string filePath)
        {
            _filePath = filePath;
        }

        /// <summary>
        /// 실행 파일과 같은 폴더의 프리셋 파일 전체 경로를 반환한다.
        /// </summary>
        /// <returns>mcp_presets.json의 전체 경로</returns>
        public static string GetDefaultPath()
        {
            return Path.Combine(AppContext.BaseDirectory, FILE_NAME);
        }

        /// <summary>현재 프리셋 목록(읽기 전용).</summary>
        public IReadOnlyList<McpPreset> Presets
        {
            get { return _data.Presets; }
        }

        /// <summary>
        /// 프리셋 파일을 읽어 상태를 채운다. 파일 부재/손상 시 빈 목록으로 시작.
        /// </summary>
        public void Load()
        {
            if (File.Exists(_filePath) == false)
            {
                _data = new McpPresetData();
                return;
            }

            try
            {
                string json = File.ReadAllText(_filePath);
                McpPresetData? loaded = JsonSerializer.Deserialize<McpPresetData>(json, JSON_OPTIONS);
                _data = loaded ?? new McpPresetData();
            }
            catch (JsonException)
            {
                _data = new McpPresetData();
            }
            catch (IOException)
            {
                _data = new McpPresetData();
            }
        }

        /// <summary>
        /// 현재 프리셋 목록을 파일에 저장한다.
        /// </summary>
        public void Save()
        {
            string json = JsonSerializer.Serialize(_data, JSON_OPTIONS);
            File.WriteAllText(_filePath, json);
        }

        /// <summary>
        /// 이름이 같은 프리셋이 있으면 갱신, 없으면 추가한다.
        /// </summary>
        /// <param name="preset">추가/갱신할 프리셋</param>
        public void AddOrUpdate(McpPreset preset)
        {
            if (preset == null || string.IsNullOrWhiteSpace(preset.Name) == true)
            {
                return;
            }

            int index = _data.Presets.FindIndex(p => p.Name == preset.Name);
            if (index >= 0)
            {
                _data.Presets[index] = preset;
            }
            else
            {
                _data.Presets.Add(preset);
            }
        }

        /// <summary>
        /// 이름으로 프리셋을 제거한다.
        /// </summary>
        /// <param name="name">제거할 프리셋 이름</param>
        public void Remove(string name)
        {
            _data.Presets.RemoveAll(p => p.Name == name);
        }
    }
}

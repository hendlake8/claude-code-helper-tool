using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace ClaudeCodeHelper
{
    /// <summary>
    /// 경로 히스토리 로드/저장 및 추가·삭제를 관리한다.
    /// </summary>
    public class PathManager
    {
        /// <summary>JSON 직렬화 옵션(들여쓰기 + 한글 그대로).</summary>
        private static readonly JsonSerializerOptions JSON_OPTIONS = new()
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        /// <summary>config 파일 경로.</summary>
        private readonly string _configFilePath;

        /// <summary>현재 설정 상태.</summary>
        private AppConfig _config = new();

        /// <summary>
        /// config 파일 경로를 지정해 생성한다.
        /// </summary>
        /// <param name="configFilePath">ClaudeCodeHelper.json의 전체 경로</param>
        public PathManager(string configFilePath)
        {
            _configFilePath = configFilePath;
        }

        /// <summary>현재 경로 목록(읽기 전용).</summary>
        public IReadOnlyList<string> PathHistory
        {
            get { return _config.PathHistory; }
        }

        /// <summary>마지막 사용 경로.</summary>
        public string LastUsedPath
        {
            get { return _config.LastUsedPath; }
        }

        /// <summary>모든 권한으로 실행 여부.</summary>
        public bool FullPermissionMode
        {
            get { return _config.FullPermissionMode; }
            set { _config.FullPermissionMode = value; }
        }

        /// <summary>
        /// config 파일을 읽어 상태를 채운다. 파일 부재/손상 시 빈 상태로 시작한다.
        /// </summary>
        public void Load()
        {
            if (File.Exists(_configFilePath) == false)
            {
                _config = new AppConfig();
                return;
            }

            try
            {
                string json = File.ReadAllText(_configFilePath);
                AppConfig? loaded = JsonSerializer.Deserialize<AppConfig>(json, JSON_OPTIONS);
                _config = loaded ?? new AppConfig();
            }
            catch (JsonException)
            {
                // 파일 손상 시 빈 상태로 복구
                _config = new AppConfig();
            }
            catch (IOException)
            {
                _config = new AppConfig();
            }
        }

        /// <summary>
        /// 현재 상태를 config 파일에 저장한다.
        /// </summary>
        public void Save()
        {
            string json = JsonSerializer.Serialize(_config, JSON_OPTIONS);
            File.WriteAllText(_configFilePath, json);
        }

        /// <summary>
        /// 경로를 목록 최상단에 추가(중복이면 위로 이동)하고 LastUsedPath로 설정한다.
        /// </summary>
        /// <param name="path">추가할 경로</param>
        public void AddPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path) == true)
            {
                return;
            }

            _config.PathHistory.Remove(path);
            _config.PathHistory.Insert(0, path);
            _config.LastUsedPath = path;
        }

        /// <summary>
        /// 지정 경로를 목록에서 제거한다.
        /// </summary>
        /// <param name="path">제거할 경로</param>
        public void RemovePath(string path)
        {
            _config.PathHistory.Remove(path);

            if (_config.LastUsedPath == path)
            {
                _config.LastUsedPath = "";
            }
        }

        /// <summary>
        /// 모든 경로를 제거한다.
        /// </summary>
        public void ClearAll()
        {
            _config.PathHistory.Clear();
            _config.LastUsedPath = "";
        }

        /// <summary>
        /// 마지막 사용 경로를 설정한다.
        /// </summary>
        /// <param name="path">설정할 경로</param>
        public void SetLastPath(string path)
        {
            _config.LastUsedPath = path;
        }
    }
}

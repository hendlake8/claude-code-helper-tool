namespace ClaudeCodeHelper
{
    /// <summary>
    /// 리스트 표시 전용 항목(경로 + 즐겨찾기 상태). 직렬화 대상이 아니다.
    /// </summary>
    public class PathItem
    {
        /// <summary>프로젝트 경로.</summary>
        public string Path { get; set; } = "";

        /// <summary>즐겨찾기 등록 여부.</summary>
        public bool IsFavorite { get; set; }

        /// <summary>별 글리프(활성 ★ / 비활성 ☆).</summary>
        public string StarGlyph
        {
            get { return IsFavorite == true ? "★" : "☆"; }
        }
    }
}

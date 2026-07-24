namespace Chronex.UI.Result
{
    /// <summary>
    /// Cầu nối dữ liệu thuần C# giữa LevelScene -> ResultScene, vì Rainbow5s.ISceneManager
    /// không hỗ trợ truyền tham số trực tiếp khi LoadScene. Set ở LevelScene NGAY TRƯỚC KHI
    /// gọi LoadScene, đọc ở ResultScene.Start(). Không networked - mỗi máy tự giữ bản của mình.
    /// </summary>
    public static class MatchResultHolder
    {
        public static MatchResultData PendingResult { get; set; }
    }
}
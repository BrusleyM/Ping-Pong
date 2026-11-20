namespace PingPong.Core
{
    public static class NicknameRules
    {
        public const int MaxLength = 12;

        public static string Clean(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return string.Empty;
            }

            var trimmed = raw.Trim();
            return trimmed.Length <= MaxLength ? trimmed : trimmed.Substring(0, MaxLength).TrimEnd();
        }

        public static bool IsValid(string raw) => Clean(raw).Length > 0;
    }
}

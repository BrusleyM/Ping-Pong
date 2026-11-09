namespace PingPong.Presentation
{
    /// <summary>Every player-facing string on the mode menu, in one place (and testable without a scene).</summary>
    public static class MenuText
    {
        public const string Solo = "Solo vs AI";
        public const string Online = "Online";
        public const string OnlineUnavailable = "Online (coming soon)";
        public const string Restart = "Restart";
        public const string Close = "Close";
        public const string Hint = "Menu button opens and closes this menu";

        /// <summary>A difficulty button; the level being played is marked.</summary>
        public static string Level(string name, bool current) => current ? $"<b>> {name} <</b>" : name;

        public static string Passthrough(bool on) => on ? "Passthrough: On" : "Passthrough: Off";

        public static string OnlineButton(bool available) => available ? Online : OnlineUnavailable;
    }
}

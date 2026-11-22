using PingPong.Gameplay;

namespace PingPong.Presentation
{
    public static class HudNames
    {
        public static string OpponentLevel(UiTheme theme, GameModeController modes)
        {
            var ai = modes != null ? modes.AI : null;
            if (ai == null)
            {
                return null;
            }

            return theme != null ? theme.DifficultyAt(ai.Level, ai.LevelName).Name : ai.LevelName;
        }
    }
}

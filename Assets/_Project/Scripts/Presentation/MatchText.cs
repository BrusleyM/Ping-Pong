using PingPong.Core;
using PingPong.Gameplay;

namespace PingPong.Presentation
{
    /// <summary>
    /// Every player-facing string about the match, in one place (and testable without a scene).
    /// Scores and names are always from the local player's point of view.
    /// </summary>
    public static class MatchText
    {
        public const string PlayerBoardName = "PLAYER";
        public const string AiBoardName = "AI";
        public const string OpponentBoardName = "OPPONENT";
        public const string ServeMarker = "SERVE";

        /// <summary>The label on a side's scoreboard: PLAYER for the local player, AI or OPPONENT for the other side.</summary>
        public static string BoardName(Side side, Side local, GameMode mode) =>
            side == local ? PlayerBoardName : mode == GameMode.Solo ? AiBoardName : OpponentBoardName;

        /// <summary>What to call the other side: the AI and its level in solo play, otherwise "Opponent".</summary>
        public static string OpponentName(GameMode mode, string aiLevelName) =>
            mode == GameMode.Solo && !string.IsNullOrEmpty(aiLevelName) ? $"AI ({aiLevelName})" : "Opponent";

        public static string Point(Side winner, PointReason reason, Side local, string opponentName)
        {
            string why = Describe(reason);
            string scores = winner == local ? "You score" : $"{opponentName} scores";
            return why.Length > 0 ? $"{scores} - {why}" : scores;
        }

        public const string Let = "Let - serve again";

        public static string Game(Side winner, Side local, string opponentName) =>
            winner == local ? "You win!" : $"{opponentName} wins!";

        public static string Describe(PointReason reason) => reason switch
        {
            PointReason.ServeMissedOwnSide => "serve must bounce on your side first",
            PointReason.ServeBouncedTwiceOnOwnSide => "serve bounced twice on the server's side",
            PointReason.ServeFault => "fault",
            PointReason.BouncedOnOwnSide => "bounced on own side",
            PointReason.DoubleBounce => "double bounce",
            PointReason.DoubleHit => "double hit",
            PointReason.Volley => "volley over the table",
            PointReason.Long => "long",
            PointReason.NotReturned => "not returned",
            PointReason.Out => "out",
            _ => "",
        };
    }
}

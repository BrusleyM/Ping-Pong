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
        public const string LocalName = "You";

        public const string PlayAgain = "Menu button to play again";

        public static string Score(int local, int opponent) => $"{local}  -  {opponent}";

        /// <summary>What to call the other side: the AI and its level in solo play, otherwise "Opponent".</summary>
        public static string OpponentName(GameMode mode, string aiLevelName) =>
            mode == GameMode.Solo && !string.IsNullOrEmpty(aiLevelName) ? $"AI ({aiLevelName})" : "Opponent";

        /// <summary>
        /// The second board line: a point message replaces the status for a moment; at game over the
        /// result stays and the status (how to play again) goes under it.
        /// </summary>
        public static string Detail(MatchPhase phase, string message, string status)
        {
            if (phase == MatchPhase.GameOver) return message.Length > 0 ? $"{message}\n{status}" : status;
            return message.Length > 0 ? message : status;
        }

        /// <param name="opponentName">What to call the other side, e.g. "AI (Medium)" or "Opponent".</param>
        public static string Name(Side side, Side local, string opponentName) => side == local ? LocalName : opponentName;

        /// <summary>The standing line under the score for the current phase; empty during a rally.</summary>
        public static string Status(MatchPhase phase, Side server, Side local, string opponentName) => phase switch
        {
            MatchPhase.WaitingForServe => server == local ? "Your serve" : $"{opponentName} serves",
            MatchPhase.GameOver => PlayAgain,
            MatchPhase.FreePlay => "Free play",
            _ => "",
        };

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

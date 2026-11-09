using PingPong.Core;

namespace PingPong.Presentation
{
    /// <summary>
    /// Every player-facing string about the match, in one place (and testable without a scene).
    /// Scores and names are always from the local player's point of view.
    /// </summary>
    public static class MatchText
    {
        public const string LocalName = "You";

        public static string Score(int local, int opponent) => $"{local}  -  {opponent}";

        /// <param name="opponentName">What to call the other side, e.g. "AI (Medium)" or "Opponent".</param>
        public static string Name(Side side, Side local, string opponentName) => side == local ? LocalName : opponentName;

        /// <summary>The standing line under the score for the current phase; empty during a rally.</summary>
        public static string Status(MatchPhase phase, Side server, Side local, string opponentName) => phase switch
        {
            MatchPhase.WaitingForServe => server == local ? "Your serve" : $"{opponentName} serves",
            MatchPhase.GameOver => "Press A to play again",
            MatchPhase.FreePlay => "Free play",
            _ => "",
        };

        public static string Point(Side winner, PointReason reason, Side local, string opponentName)
        {
            string why = Describe(reason);
            string scores = winner == local ? "You score" : $"{opponentName} scores";
            return why.Length > 0 ? $"{scores} - {why}" : scores;
        }

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

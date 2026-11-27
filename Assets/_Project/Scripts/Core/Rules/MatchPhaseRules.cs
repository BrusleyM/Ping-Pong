namespace PingPong.Core
{
    /// <summary>What each match phase needs from the scene: one place for the rules that decide when per-frame work runs.</summary>
    public static class MatchPhaseRules
    {
        /// <summary>Whether a paddle can strike the ball in this phase (the engine's CanHit is false in the others).</summary>
        public static bool AllowsHits(this MatchPhase phase) => phase is not (MatchPhase.PointOver or MatchPhase.GameOver);

        /// <summary>
        /// Whether a physical touch by this player is for the rules to judge: hits are allowed in the phase, and before the serve
        /// only the server's touch counts. In a rally every touch is judged (the engine calls volleys and double hits).
        /// </summary>
        public static bool ContactMatters(this MatchPhase phase, Side server, Side player) =>
            phase.AllowsHits() && (phase != MatchPhase.WaitingForServe || player == server);

        public static bool AllowsRigMoves(this MatchPhase phase) => phase != MatchPhase.Rally;

        public static bool HoldsServe(this MatchPhase phase, bool serveHeld) => serveHeld && phase == MatchPhase.WaitingForServe;

        /// <summary>Whether the match controller must watch the ball and the clock: a rally, a serve, or the pause before the next serve.</summary>
        public static bool NeedsWatching(this MatchPhase phase) => phase is MatchPhase.Rally or MatchPhase.WaitingForServe or MatchPhase.PointOver;
    }
}

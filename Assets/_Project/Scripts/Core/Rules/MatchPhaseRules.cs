namespace PingPong.Core
{
    /// <summary>What each match phase needs from the scene: one place for the rules that decide when per-frame work runs.</summary>
    public static class MatchPhaseRules
    {
        /// <summary>Whether a paddle can strike the ball in this phase (the engine's CanHit is false in the others).</summary>
        public static bool AllowsHits(this MatchPhase phase) => phase is not (MatchPhase.PointOver or MatchPhase.GameOver);

        /// <summary>Whether the match controller must watch the ball and the clock: a rally, a serve, or the pause before the next serve.</summary>
        public static bool NeedsWatching(this MatchPhase phase) => phase is MatchPhase.Rally or MatchPhase.WaitingForServe or MatchPhase.PointOver;
    }
}

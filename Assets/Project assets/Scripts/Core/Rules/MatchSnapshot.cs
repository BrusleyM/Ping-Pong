using System;

namespace PingPong.Core
{
    /// <summary>
    /// The complete rules state of a match: plain, blittable data, so the network can send it and a
    /// client can rebuild the exact same state with <see cref="MatchRulesEngine.Restore"/>.
    /// </summary>
    [Serializable]
    public struct MatchSnapshot : IEquatable<MatchSnapshot>
    {
        public MatchPhase Phase;
        public Side Server;
        public Side LastHitter;
        public Side Winner;
        public int ScoreNear;
        public int ScoreFar;
        /// <summary>Between the serve hit and the serve's second bounce.</summary>
        public bool IsServing;
        public int ServeBounces;
        public int BouncesNear;
        public int BouncesFar;
        /// <summary>Serves already made by the current server in this turn.</summary>
        public int ServesThisTurn;

        public static MatchSnapshot Idle => new MatchSnapshot
        {
            Phase = MatchPhase.FreePlay,
            Server = Side.Near,
            LastHitter = Side.None,
            Winner = Side.None,
        };

        public int Score(Side side) => side == Side.Near ? ScoreNear : side == Side.Far ? ScoreFar : 0;
        public int Bounces(Side side) => side == Side.Near ? BouncesNear : side == Side.Far ? BouncesFar : 0;

        public bool Equals(MatchSnapshot o) =>
            Phase == o.Phase && Server == o.Server && LastHitter == o.LastHitter && Winner == o.Winner
            && ScoreNear == o.ScoreNear && ScoreFar == o.ScoreFar && IsServing == o.IsServing
            && ServeBounces == o.ServeBounces && BouncesNear == o.BouncesNear && BouncesFar == o.BouncesFar
            && ServesThisTurn == o.ServesThisTurn;

        public override bool Equals(object obj) => obj is MatchSnapshot o && Equals(o);

        public override int GetHashCode() =>
            HashCode.Combine(HashCode.Combine(Phase, Server, LastHitter, Winner, ScoreNear, ScoreFar),
                HashCode.Combine(IsServing, ServeBounces, BouncesNear, BouncesFar, ServesThisTurn));

        public override string ToString() => $"{Phase} {ScoreNear}-{ScoreFar} server={Server} last={LastHitter}";
    }
}

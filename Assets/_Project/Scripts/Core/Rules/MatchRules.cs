using System;

namespace PingPong.Core
{
    /// <summary>Scoring and serve-rotation rules. Defaults are ITTF: 11 points, win by 2, 2 serves each, 1 each at deuce.</summary>
    [Serializable]
    public struct MatchRules
    {
        public int PointsToWin;
        public int WinBy;
        public int ServesPerTurn;
        public int DeuceServesPerTurn;

        public static MatchRules Standard => new MatchRules
        {
            PointsToWin = 11,
            WinBy = 2,
            ServesPerTurn = 2,
            DeuceServesPerTurn = 1,
        };

        public bool IsValid => PointsToWin > 0 && WinBy > 0 && ServesPerTurn > 0 && DeuceServesPerTurn > 0;
    }
}

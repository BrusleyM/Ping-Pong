namespace PingPong.Core
{
    /// <summary>A player's end of the table. Near is the local player in solo play.</summary>
    public enum Side
    {
        None = -1,
        Near = 0,
        Far = 1,
    }

    public static class SideExtensions
    {
        public static Side Opponent(this Side side) => side switch
        {
            Side.Near => Side.Far,
            Side.Far => Side.Near,
            _ => Side.None,
        };

        /// <summary>Sign of the side along the table's length axis: Near is negative, Far positive.</summary>
        public static float Sign(this Side side) => side == Side.Far ? 1f : -1f;

        public static bool IsPlayer(this Side side) => side == Side.Near || side == Side.Far;
    }
}

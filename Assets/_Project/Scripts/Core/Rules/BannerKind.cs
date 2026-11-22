namespace PingPong.Core
{
    public enum BannerKind
    {
        PointWon,
        PointLost,
        Let,
        GameWon,
        GameLost,
    }

    public static class BannerKinds
    {
        public static BannerKind ForPoint(Side winner, Side local) => winner == local ? BannerKind.PointWon : BannerKind.PointLost;

        public static BannerKind ForGame(Side winner, Side local) => winner == local ? BannerKind.GameWon : BannerKind.GameLost;

        public static bool StaysOnScreen(BannerKind kind) => kind == BannerKind.GameWon || kind == BannerKind.GameLost;
    }
}

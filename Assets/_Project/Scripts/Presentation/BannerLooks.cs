using PingPong.Core;
using UnityEngine;

namespace PingPong.Presentation
{
    public readonly struct BannerLook
    {
        public BannerLook(Color fill, Color border, Color text)
        {
            Fill = fill;
            Border = border;
            Text = text;
        }

        public Color Fill { get; }

        public Color Border { get; }

        public Color Text { get; }
    }

    public static class BannerLooks
    {
        public static BannerLook For(UiTheme theme, BannerKind kind)
        {
            switch (kind)
            {
                case BannerKind.PointWon: return new BannerLook(theme.Success, theme.SuccessDark, theme.Ink);
                case BannerKind.PointLost: return new BannerLook(theme.Danger, theme.DangerDark, Color.white);
                case BannerKind.Let: return new BannerLook(theme.Secondary, theme.SecondaryDark, theme.Ink);
                case BannerKind.GameWon: return new BannerLook(theme.Accent, theme.AccentDark, theme.Ink);
                default: return new BannerLook(theme.Surface, theme.Accent, Color.white);
            }
        }
    }

    public static class PlateLooks
    {
        const float BorderDarkening = 0.4f;

        public static Color BorderOf(Color fill) => Color.Lerp(fill, Color.black, BorderDarkening);
    }
}

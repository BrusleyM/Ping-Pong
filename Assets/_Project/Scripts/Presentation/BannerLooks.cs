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
        static readonly Color WonBorder = new Color32(0x16, 0x89, 0x6C, 0xFF);
        static readonly Color LostBorder = new Color32(0x9D, 0x20, 0x3B, 0xFF);
        static readonly Color GameWonBorder = new Color32(0xA8, 0x6E, 0x07, 0xFF);

        public static BannerLook For(UiTheme theme, BannerKind kind)
        {
            switch (kind)
            {
                case BannerKind.PointWon: return new BannerLook(theme.Success, WonBorder, theme.Ink);
                case BannerKind.PointLost: return new BannerLook(theme.Danger, LostBorder, Color.white);
                case BannerKind.Let: return new BannerLook(theme.Secondary, theme.SecondaryDark, theme.Ink);
                case BannerKind.GameWon: return new BannerLook(theme.Accent, GameWonBorder, theme.Ink);
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

using PingPong.Presentation;
using UnityEngine;

namespace PingPong.EditorTools
{
    internal static class HudSprites
    {
        internal const string PlateFill = "ui_hud_plate_fill";
        internal const string PlateOutline = "ui_hud_plate_outline";
        internal const string BannerFill = "ui_hud_banner_fill";
        internal const string BannerOutline = "ui_hud_banner_outline";
        internal const string ServePill = "ui_hud_serve_pill";
        internal const string ScoreNet = "ui_hud_net";
        internal static readonly string[] BannerIcons =
        {
            "ui_banner_icon_win",
            "ui_banner_icon_lose",
            "ui_banner_icon_let",
            "ui_banner_icon_crown",
            "ui_banner_icon_dot",
        };

        private const int IconSize = HudLayout.IconSize;
        private const int Samples = 4;
        private static readonly Color IconRing = new Color32(0xBB, 0xBB, 0xBB, 0xFF);

        internal static void Build(UiTheme theme)
        {
            var root = MenuAssetsBuilder.Root;
            const int sliced = HudLayout.SlicedSourceSize;
            UiSpriteFactory.Create($"{root}/{PlateFill}.png", new UiSpriteFactory.Style(sliced, sliced, HudLayout.PlateRadius, Color.white, Color.white, 0, 0, Color.clear, 0f));
            UiSpriteFactory.Create($"{root}/{PlateOutline}.png", new UiSpriteFactory.Style(sliced, sliced, HudLayout.PlateRadius, Color.clear, Color.white, HudLayout.PlateEdge, 0, Color.clear, 0f));
            UiSpriteFactory.Create($"{root}/{BannerFill}.png", new UiSpriteFactory.Style(sliced, sliced, HudLayout.BannerRadius, Color.white, Color.white, 0, 0, Color.clear, 0f));
            UiSpriteFactory.Create($"{root}/{BannerOutline}.png", new UiSpriteFactory.Style(sliced, sliced, HudLayout.BannerRadius, Color.clear, Color.white, HudLayout.BannerEdge, 0, Color.clear, 0f));
            UiSpriteFactory.Create($"{root}/{ServePill}.png", new UiSpriteFactory.Style((int)HudLayout.ServePillSize.x, (int)HudLayout.ServePillSize.y, 22, Color.white, theme.Ink, 4, 5, theme.Ink, 0f));
            UiSpriteFactory.Import($"{root}/{ScoreNet}.png", RenderNet(theme.Ink), Vector4.zero);
            var glyphs = new[] { Star(), Cross(), Zigzag(), Crown(), Dot() };
            for (var i = 0; i < glyphs.Length; i++)
            {
                UiSpriteFactory.Import($"{root}/{BannerIcons[i]}.png", RenderIcon(glyphs[i], theme.Ink), Vector4.zero);
            }
        }

        private static Texture2D RenderNet(Color ink)
        {
            var width = (int)HudLayout.NetSize.x;
            var height = (int)HudLayout.NetSize.y;
            const int edge = 7;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color[width * height];
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var onEdge = x < edge || x >= width - edge || y < edge || y >= height - edge;
                    var stripe = (y - edge) % 18 >= 14;
                    pixels[y * width + x] = onEdge || stripe ? ink : Color.white;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        private static Texture2D RenderIcon(Vector2[][] glyph, Color ink)
        {
            var texture = new Texture2D(IconSize, IconSize, TextureFormat.RGBA32, false);
            var pixels = new Color[IconSize * IconSize];
            var innerRadius = 1f - 8f / (IconSize * 0.5f);
            for (var y = 0; y < IconSize; y++)
            {
                for (var x = 0; x < IconSize; x++)
                {
                    var sum = Vector4.zero;
                    for (var sy = 0; sy < Samples; sy++)
                    {
                        for (var sx = 0; sx < Samples; sx++)
                        {
                            var point = new Vector2((x + (sx + 0.5f) / Samples) / IconSize * 2f - 1f, (y + (sy + 0.5f) / Samples) / IconSize * 2f - 1f);
                            var radius = point.magnitude;
                            if (radius > 1f)
                            {
                                continue;
                            }

                            var colour = radius > innerRadius ? IconRing : Inside(glyph, point) ? ink : Color.white;
                            sum += new Vector4(colour.r, colour.g, colour.b, 1f);
                        }
                    }

                    var alpha = sum.w / (Samples * Samples);
                    pixels[y * IconSize + x] = sum.w > 0f ? new Color(sum.x / sum.w, sum.y / sum.w, sum.z / sum.w, alpha) : Color.clear;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        private static bool Inside(Vector2[][] polygons, Vector2 point)
        {
            foreach (var polygon in polygons)
            {
                if (Contains(polygon, point))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool Contains(Vector2[] polygon, Vector2 point)
        {
            var inside = false;
            for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
            {
                var a = polygon[i];
                var b = polygon[j];
                if ((a.y > point.y) != (b.y > point.y) && point.x < (b.x - a.x) * (point.y - a.y) / (b.y - a.y) + a.x)
                {
                    inside = !inside;
                }
            }

            return inside;
        }

        private static Vector2[][] Star()
        {
            var points = new Vector2[10];
            for (var i = 0; i < points.Length; i++)
            {
                var angle = Mathf.PI * 0.5f + i * Mathf.PI / 5f;
                var radius = i % 2 == 0 ? 0.58f : 0.24f;
                points[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            }

            return new[] { points };
        }

        private static Vector2[][] Cross() => new[] { Bar(new Vector2(-0.4f, -0.4f), new Vector2(0.4f, 0.4f), 0.1f), Bar(new Vector2(-0.4f, 0.4f), new Vector2(0.4f, -0.4f), 0.1f) };

        private static Vector2[][] Zigzag() => new[]
        {
            Bar(new Vector2(-0.5f, 0.12f), new Vector2(-0.18f, -0.22f), 0.09f),
            Bar(new Vector2(-0.18f, -0.22f), new Vector2(0.12f, 0.22f), 0.09f),
            Bar(new Vector2(0.12f, 0.22f), new Vector2(0.5f, -0.12f), 0.09f),
        };

        private static Vector2[][] Crown() => new[]
        {
            new[] { new Vector2(-0.5f, -0.28f), new Vector2(-0.5f, 0.28f), new Vector2(-0.25f, 0.02f), new Vector2(0f, 0.38f), new Vector2(0.25f, 0.02f), new Vector2(0.5f, 0.28f), new Vector2(0.5f, -0.28f) },
            new[] { new Vector2(-0.5f, -0.46f), new Vector2(-0.5f, -0.34f), new Vector2(0.5f, -0.34f), new Vector2(0.5f, -0.46f) },
        };

        private static Vector2[][] Dot()
        {
            var points = new Vector2[32];
            for (var i = 0; i < points.Length; i++)
            {
                var angle = i * Mathf.PI * 2f / points.Length;
                points[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 0.3f;
            }

            return new[] { points };
        }

        private static Vector2[] Bar(Vector2 from, Vector2 to, float halfThickness)
        {
            var side = Vector2.Perpendicular((to - from).normalized) * halfThickness;
            return new[] { from + side, to + side, to - side, from - side };
        }
    }
}

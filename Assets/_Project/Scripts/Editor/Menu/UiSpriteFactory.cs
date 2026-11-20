using System.IO;
using UnityEditor;
using UnityEngine;

namespace PingPong.EditorTools
{
    internal static class UiSpriteFactory
    {
        internal readonly struct Style
        {
            internal Style(int width, int height, float radius, Color fill, Color outline, float outlineWidth, int lip, Color lipColor, float gloss)
            {
                Width = width;
                Height = height;
                Radius = radius;
                Fill = fill;
                Outline = outline;
                OutlineWidth = outlineWidth;
                Lip = lip;
                LipColor = lipColor;
                Gloss = gloss;
            }

            internal int Width { get; }
            internal int Height { get; }
            internal float Radius { get; }
            internal Color Fill { get; }
            internal Color Outline { get; }
            internal float OutlineWidth { get; }
            internal int Lip { get; }
            internal Color LipColor { get; }
            internal float Gloss { get; }
        }

        internal static Sprite Create(string path, Style style)
        {
            var texture = Render(style);
            var directory = Path.GetDirectoryName(path);
            Directory.CreateDirectory(directory);
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 1024;
            var border = Mathf.Ceil(style.Radius + style.OutlineWidth);
            importer.spriteBorder = new Vector4(border, border + style.Lip, border, border);
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static Texture2D Render(Style s)
        {
            var texture = new Texture2D(s.Width, s.Height, TextureFormat.RGBA32, false);
            var pixels = new Color[s.Width * s.Height];
            var bodyBottom = s.Lip;
            var bodyTop = s.Height;
            var lipTop = s.Height - s.Lip;
            for (var y = 0; y < s.Height; y++)
            {
                for (var x = 0; x < s.Width; x++)
                {
                    var colour = Color.clear;
                    if (s.Lip > 0)
                    {
                        colour = Over(colour, s.LipColor, Coverage(x + 0.5f, y + 0.5f, 0, 0, s.Width, lipTop, s.Radius));
                    }

                    var body = Coverage(x + 0.5f, y + 0.5f, 0, bodyBottom, s.Width, bodyTop, s.Radius);
                    var inner = Coverage(x + 0.5f, y + 0.5f, s.OutlineWidth, bodyBottom + s.OutlineWidth, s.Width - s.OutlineWidth, bodyTop - s.OutlineWidth, Mathf.Max(0f, s.Radius - s.OutlineWidth));
                    if (s.Fill.a <= 0f)
                    {
                        pixels[y * s.Width + x] = Over(colour, s.Outline, body * (1f - inner));
                        continue;
                    }

                    colour = Over(colour, s.Outline, body);
                    var fill = s.Fill;
                    if (s.Gloss > 0f)
                    {
                        var fromTop = (bodyTop - s.OutlineWidth - y) / Mathf.Max(1f, (bodyTop - bodyBottom) * 0.45f);
                        fill = Color.Lerp(s.Fill, Color.white, s.Gloss * Mathf.Clamp01(1f - fromTop));
                        fill.a = s.Fill.a;
                    }

                    colour = Over(colour, fill, inner);
                    pixels[y * s.Width + x] = colour;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        private static float Coverage(float px, float py, float left, float bottom, float right, float top, float radius)
        {
            var centreX = (left + right) * 0.5f;
            var centreY = (bottom + top) * 0.5f;
            var halfX = (right - left) * 0.5f - radius;
            var halfY = (top - bottom) * 0.5f - radius;
            var qx = Mathf.Abs(px - centreX) - halfX;
            var qy = Mathf.Abs(py - centreY) - halfY;
            var outside = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude;
            var distance = outside + Mathf.Min(Mathf.Max(qx, qy), 0f) - radius;
            return Mathf.Clamp01(0.5f - distance);
        }

        private static Color Over(Color under, Color over, float coverage)
        {
            var alpha = over.a * coverage;
            var outAlpha = alpha + under.a * (1f - alpha);
            if (outAlpha <= 0f)
            {
                return Color.clear;
            }

            var rgb = (new Vector3(over.r, over.g, over.b) * alpha + new Vector3(under.r, under.g, under.b) * under.a * (1f - alpha)) / outAlpha;
            return new Color(rgb.x, rgb.y, rgb.z, outAlpha);
        }
    }
}

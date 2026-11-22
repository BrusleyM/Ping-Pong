using PingPong.Presentation;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static PingPong.EditorTools.MenuUi;
using static PingPong.EditorTools.SetupHelpers;

namespace PingPong.EditorTools
{
    internal static class HudBuilder
    {
        internal const string ScoreboardPrefabPath = "Assets/_Project/Prefabs/UI/Scoreboard.prefab";
        internal const string MatchPrefabPath = "Assets/_Project/Prefabs/Match.prefab";

        private const float BoardMetres = 1.8f;
        private const float BoardPixels = 1200f;
        private const float BannerMetres = 2.4f;

        [MenuItem("Ping Pong/Build HUD")]
        internal static void Build()
        {
            Theme = AssetDatabase.LoadAssetAtPath<UiTheme>(MenuAssetsBuilder.ThemePath);
            HudSprites.Build(Theme);
            EditPrefab(ScoreboardPrefabPath, BuildScoreboard);
            EditPrefab(MatchPrefabPath, root => BuildBanner(root.GetComponentInChildren<MatchBanner>(true)));
            AssetDatabase.SaveAssets();
            Debug.Log("Ping Pong: HUD built.");
        }

        private static void BuildScoreboard(GameObject root)
        {
            var board = root.transform.Find("Board");
            Clear(board);
            board.localScale = Vector3.one;
            var canvas = WorldCanvas(board, "Canvas", new Vector2(BoardPixels, 400f), BoardMetres / BoardPixels);

            var local = BuildPlate(canvas, "LocalPlate", new Vector2(-310f, 0f), false);
            var opponent = BuildPlate(canvas, "OpponentPlate", new Vector2(310f, 0f), true);
            var net = Picture(canvas, "Net", null, Vector2.zero, HudLayout.NetSize);
            net.sprite = MenuAssetsBuilder.SpriteAt(HudSprites.ScoreNet);
            net.type = Image.Type.Simple;
            Round(canvas, "NetBall", new Vector2(-6f, 138f), 26f, Theme.Accent);

            var scoreboard = root.GetComponent<MatchScoreboard>();
            Set(scoreboard, "_theme", Theme);
            Set(scoreboard, "_localFill", local.fill);
            Set(scoreboard, "_localBorder", local.border);
            Set(scoreboard, "_localName", local.name);
            Set(scoreboard, "_localScore", local.score);
            Set(scoreboard, "_localServe", local.serve);
            Set(scoreboard, "_opponentFill", opponent.fill);
            Set(scoreboard, "_opponentBorder", opponent.border);
            Set(scoreboard, "_opponentName", opponent.name);
            Set(scoreboard, "_opponentLevel", opponent.level);
            Set(scoreboard, "_opponentScore", opponent.score);
            Set(scoreboard, "_opponentServe", opponent.serve);
        }

        private static (Image fill, Image border, TMP_Text name, TMP_Text level, TMP_Text score, GameObject serve) BuildPlate(Transform canvas, string name, Vector2 position, bool withLevel)
        {
            var plate = Rect(canvas, name, position, HudLayout.PlateSize);
            var fill = Picture(plate, "Fill", HudSprites.PlateFill, Vector2.zero, HudLayout.PlateSize);
            var border = Picture(plate, "Border", HudSprites.PlateOutline, Vector2.zero, HudLayout.PlateSize);

            var row = Rect(plate, "NameRow", new Vector2(-283f + 34f + 150f, 121f), new Vector2(300f, 50f));
            var layout = GetOrAdd<HorizontalLayoutGroup>(row.gameObject);
            layout.childAlignment = TextAnchor.LowerLeft;
            layout.spacing = 11f;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            var title = Label(row, "Name", "PLAYER", 38f, false, Color.white, Vector2.zero, new Vector2(100f, 46f), TextAlignmentOptions.BottomLeft);
            title.textWrappingMode = TextWrappingModes.NoWrap;
            TextMeshProUGUI level = null;
            if (withLevel)
            {
                level = Label(row, "Level", string.Empty, 22f, false, new Color(1f, 1f, 1f, 0.85f), Vector2.zero, new Vector2(100f, 30f), TextAlignmentOptions.BottomLeft);
                level.textWrappingMode = TextWrappingModes.NoWrap;
            }

            var score = Label(plate, "Score", "0", 190f, false, Color.white, new Vector2(0f, -45f), new Vector2(500f, 230f));
            score.characterSpacing = -6f;
            score.textWrappingMode = TextWrappingModes.NoWrap;

            var serve = Picture(plate, "Serve", HudSprites.ServePill, new Vector2(283f - 34f - 85f, 121f), HudLayout.ServePillSize);
            Round(serve.transform, "Ball", new Vector2(-52f, 3f), 24f, Color.white);
            Label(serve.transform, "Label", "SERVE", 22f, false, Theme.Ink, new Vector2(14f, 3f), new Vector2(100f, 34f));
            return (fill, border, title, level, score, serve.gameObject);
        }

        private static void BuildBanner(MatchBanner banner)
        {
            Clear(banner.transform);
            var canvas = WorldCanvas(banner.transform, "Canvas", new Vector2(1600f, 300f), BannerMetres / HudLayout.BannerSize.x);
            var visual = Rect(canvas, "Visual", Vector2.zero, HudLayout.BannerSize);
            var fill = Picture(visual, "Fill", HudSprites.BannerFill, Vector2.zero, HudLayout.BannerSize);
            var border = Picture(visual, "Border", HudSprites.BannerOutline, Vector2.zero, HudLayout.BannerSize);

            var group = Rect(visual, "Group", Vector2.zero, HudLayout.BannerSize);
            var horizontal = GetOrAdd<HorizontalLayoutGroup>(group.gameObject);
            horizontal.childAlignment = TextAnchor.MiddleCenter;
            horizontal.spacing = 40f;
            horizontal.padding = new RectOffset(64, 64, 24, 24);
            horizontal.childControlWidth = horizontal.childControlHeight = true;
            horizontal.childForceExpandWidth = horizontal.childForceExpandHeight = false;

            var icon = Picture(group, "Icon", null, Vector2.zero, new Vector2(HudLayout.IconSize, HudLayout.IconSize));
            icon.type = Image.Type.Simple;
            icon.preserveAspect = true;
            var iconSize = GetOrAdd<LayoutElement>(icon.gameObject);
            iconSize.preferredWidth = iconSize.preferredHeight = HudLayout.IconSize;

            var copy = Rect(group, "Copy", Vector2.zero, new Vector2(400f, 160f));
            var vertical = GetOrAdd<VerticalLayoutGroup>(copy.gameObject);
            vertical.childAlignment = TextAnchor.MiddleLeft;
            vertical.spacing = 6f;
            vertical.childControlWidth = vertical.childControlHeight = true;
            vertical.childForceExpandWidth = vertical.childForceExpandHeight = false;
            var title = Label(copy, "Title", "You score", 96f, true, Theme.Ink, Vector2.zero, new Vector2(400f, 100f), TextAlignmentOptions.Left);
            title.textWrappingMode = TextWrappingModes.NoWrap;
            var reason = Label(copy, "Reason", "not returned", 48f, false, Theme.Ink, Vector2.zero, new Vector2(400f, 52f), TextAlignmentOptions.Left);
            reason.textWrappingMode = TextWrappingModes.NoWrap;

            var confetti = Rect(visual, "Confetti", Vector2.zero, HudLayout.BannerSize);
            Dot(confetti, "Dot1", new Vector2(-690f, 90f), 28f, Theme.Primary);
            Dot(confetti, "Dot2", new Vector2(-560f, -95f), 22f, Theme.Secondary);
            Dot(confetti, "Dot3", new Vector2(660f, 95f), 30f, new Color32(0xFF, 0x75, 0x3D, 0xFF));
            Dot(confetti, "Dot4", new Vector2(540f, -100f), 24f, Theme.Primary);
            Dot(confetti, "Dot5", new Vector2(-220f, 112f), 22f, new Color32(0x8F, 0x62, 0xD7, 0xFF));
            Dot(confetti, "Dot6", new Vector2(230f, -112f), 26f, Theme.SuccessDark);

            if (HudSprites.BannerIcons.Length != System.Enum.GetValues(typeof(PingPong.Core.BannerKind)).Length)
            {
                throw new System.InvalidOperationException("There must be one banner icon per banner kind, in the same order.");
            }

            var icons = new Sprite[HudSprites.BannerIcons.Length];
            for (var i = 0; i < icons.Length; i++)
            {
                icons[i] = MenuAssetsBuilder.SpriteAt(HudSprites.BannerIcons[i]);
            }

            Set(banner, "_theme", Theme);
            Set(banner, "_visual", visual);
            Set(banner, "_fill", fill);
            Set(banner, "_border", border);
            Set(banner, "_icon", icon);
            Set(banner, "_title", title);
            Set(banner, "_reason", reason);
            Set(banner, "_confetti", confetti.gameObject);
            SetArray(banner, "_icons", icons);
            visual.gameObject.SetActive(false);
        }

        private static void Dot(Transform parent, string name, Vector2 position, float size, Color colour) => Round(parent, name, position, size, colour);

        private static Image Round(Transform parent, string name, Vector2 position, float size, Color colour)
        {
            var image = Picture(parent, name, "ui_round", position, new Vector2(size, size), colour);
            image.type = Image.Type.Simple;
            return image;
        }

        private static RectTransform WorldCanvas(Transform parent, string name, Vector2 pixels, float metresPerPixel)
        {
            var go = GetOrCreate(name, parent);
            var canvas = GetOrAdd<Canvas>(go);
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.TexCoord1 | AdditionalCanvasShaderChannels.Normal | AdditionalCanvasShaderChannels.Tangent;
            var rect = (RectTransform)go.transform;
            rect.localPosition = Vector3.zero;
            rect.localRotation = Quaternion.identity;
            rect.sizeDelta = pixels;
            rect.localScale = Vector3.one * metresPerPixel;
            return rect;
        }

        private static void Clear(Transform parent)
        {
            for (var i = parent.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(parent.GetChild(i).gameObject);
            }
        }
    }
}

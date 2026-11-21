using PingPong.Presentation;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using static PingPong.EditorTools.SetupHelpers;

namespace PingPong.EditorTools
{
    internal static class MenuAssetsBuilder
    {
        internal const string Root = "Assets/_Project/Art/Ui";
        internal const string ThemePath = "Assets/_Project/Settings/UiTheme.asset";
        internal const string TitleFontPath = Root + "/Fredoka-Bold SDF.asset";
        internal const string BodyFontPath = Root + "/Nunito-ExtraBold SDF.asset";
        internal const string PreviewTexturePath = Root + "/CharacterPreview.renderTexture";
        internal const string CharacterRoot = "Assets/Models/Character";
        internal const string CharacterPrefabPath = CharacterRoot + "/Prefabs/Character.prefab";
        internal const string RedMaterialPath = CharacterRoot + "/Materials/PlayerRed.mat";
        internal const string BlueMaterialPath = CharacterRoot + "/Materials/PlayerBlue.mat";

        internal static UiTheme Theme { get; private set; }

        internal static void Build()
        {
            EnsureFolder(Root);
            Theme = BuildTheme();
            BuildFont("Assets/_Project/Fonts/Fredoka-Bold.ttf", TitleFontPath);
            BuildFont("Assets/_Project/Fonts/Nunito-ExtraBold.ttf", BodyFontPath);
            KeepGlyphsInBuild(TitleFontPath);
            KeepGlyphsInBuild(BodyFontPath);
            BuildSprites(Theme);
            BuildPreviewTexture();
            BuildTeamMaterial(RedMaterialPath, Theme.TeamRed);
            BuildTeamMaterial(BlueMaterialPath, Theme.TeamBlue);
            AssetDatabase.SaveAssets();
        }

        internal static Sprite SpriteAt(string name) => AssetDatabase.LoadAssetAtPath<Sprite>($"{Root}/{name}.png");

        internal static TMP_FontAsset Title => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TitleFontPath);

        internal static TMP_FontAsset Body => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BodyFontPath);

        private static UiTheme BuildTheme()
        {
            var theme = AssetDatabase.LoadAssetAtPath<UiTheme>(ThemePath);
            if (theme == null)
            {
                EnsureFolder("Assets/_Project/Settings");
                theme = ScriptableObject.CreateInstance<UiTheme>();
                AssetDatabase.CreateAsset(theme, ThemePath);
            }

            return theme;
        }

        private static void BuildFont(string fontPath, string assetPath)
        {
            if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath) != null)
            {
                return;
            }

            var font = AssetDatabase.LoadAssetAtPath<Font>(fontPath);
            var asset = TMP_FontAsset.CreateFontAsset(font, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
            AssetDatabase.CreateAsset(asset, assetPath);
            asset.atlasTexture.name = asset.name + " Atlas";
            asset.material.name = asset.name + " Material";
            AssetDatabase.AddObjectToAsset(asset.atlasTexture, asset);
            AssetDatabase.AddObjectToAsset(asset.material, asset);
            var characters = new System.Text.StringBuilder();
            for (var c = 32; c < 127; c++)
            {
                characters.Append((char)c);
            }

            characters.Append("…’✓");
            asset.TryAddCharacters(characters.ToString(), out _);
            EditorUtility.SetDirty(asset);
        }

        private static void KeepGlyphsInBuild(string assetPath)
        {
            var asset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
            var serialized = new SerializedObject(asset);
            var property = serialized.FindProperty("m_ClearDynamicDataOnBuild");
            if (property != null)
            {
                property.boolValue = false;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void BuildSprites(UiTheme t)
        {
            var white = Color.white;
            Button("ui_button_primary", t.Primary, t.PrimaryDark);
            Button("ui_button_secondary", t.Secondary, t.SecondaryDark);
            Button("ui_button_accent", t.Accent, new Color32(0xB8, 0x8F, 0x00, 0xFF));
            Button("ui_button_success", t.Success, new Color32(0x1F, 0x8F, 0x6A, 0xFF));
            Button("ui_button_danger", t.Danger, new Color32(0x8F, 0x1F, 0x2E, 0xFF));
            Button("ui_button_tertiary", new Color32(0x6F, 0x50, 0xA1, 0xFF), new Color32(0x3C, 0x24, 0x69, 0xFF));
            Button("ui_button_white", white, new Color32(0xBD, 0xA8, 0xDC, 0xFF));
            Button("ui_button_dark", t.SurfaceRaised, new Color32(0x1A, 0x0C, 0x39, 0xFF));
            UiSpriteFactory.Create($"{Root}/ui_panel_large.png", new UiSpriteFactory.Style(1472, 772, 48, t.Surface, new Color32(0x5D, 0x37, 0x97, 0xFF), 6, 0, Color.clear, 0f));
            UiSpriteFactory.Create($"{Root}/ui_panel_inner.png", new UiSpriteFactory.Style(256, 256, 36, new Color32(0x24, 0x10, 0x4F, 0xFF), new Color32(0x5D, 0x37, 0x97, 0xFF), 4, 0, Color.clear, 0f));
            UiSpriteFactory.Create($"{Root}/ui_card_mode.png", new UiSpriteFactory.Style(360, 480, 40, t.Secondary, t.SecondaryDark, 6, 10, t.SecondaryDark, 0.2f));
            UiSpriteFactory.Create($"{Root}/ui_text_field.png", new UiSpriteFactory.Style(760, 104, 36, white, new Color32(0xBD, 0xA8, 0xDC, 0xFF), 5, 0, Color.clear, 0f));
            UiSpriteFactory.Create($"{Root}/ui_toast.png", new UiSpriteFactory.Style(480, 96, 32, t.Success, new Color32(0x1F, 0x8F, 0x6A, 0xFF), 5, 8, new Color32(0x1F, 0x8F, 0x6A, 0xFF), 0.2f));
            UiSpriteFactory.Create($"{Root}/ui_round.png", new UiSpriteFactory.Style(128, 128, 64, white, new Color32(0xBD, 0xA8, 0xDC, 0xFF), 5, 0, Color.clear, 0f));
            UiSpriteFactory.Create($"{Root}/ui_ring.png", new UiSpriteFactory.Style(256, 256, 40, Color.clear, t.Accent, 10, 0, Color.clear, 0f));
            UiSpriteFactory.Create($"{Root}/ui_tag.png", new UiSpriteFactory.Style(160, 56, 28, t.Accent, new Color32(0xB8, 0x8F, 0x00, 0xFF), 4, 0, Color.clear, 0f));
            UiSpriteFactory.Create($"{Root}/ui_track.png", new UiSpriteFactory.Style(64, 40, 20, new Color32(0x24, 0x10, 0x4F, 0xFF), new Color32(0x5D, 0x37, 0x97, 0xFF), 4, 0, Color.clear, 0f));
        }

        private static void Button(string name, Color fill, Color dark)
        {
            UiSpriteFactory.Create($"{Root}/{name}.png", new UiSpriteFactory.Style(400, 112, 34, fill, dark, 6, 10, dark, 0.25f));
        }

        private static void BuildPreviewTexture()
        {
            if (AssetDatabase.LoadAssetAtPath<RenderTexture>(PreviewTexturePath) != null)
            {
                return;
            }

            var texture = new RenderTexture(512, 768, 24, RenderTextureFormat.ARGB32) { name = "CharacterPreview" };
            AssetDatabase.CreateAsset(texture, PreviewTexturePath);
        }

        private static void BuildTeamMaterial(string path, Color colour)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                AssetDatabase.CopyAsset(CharacterRoot + "/Materials/Home.mat", path);
                material = AssetDatabase.LoadAssetAtPath<Material>(path);
            }

            colour.a = 1f;
            material.SetColor("_BaseColor", colour);
            material.SetColor("_Color", colour);
            EditorUtility.SetDirty(material);
        }
    }
}

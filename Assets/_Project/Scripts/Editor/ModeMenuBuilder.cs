using Oculus.Interaction;
using Oculus.Interaction.Surfaces;
using PingPong.Presentation;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static PingPong.EditorTools.SetupHelpers;

namespace PingPong.EditorTools
{
    /// <summary>
    /// Builds the mode menu as a base prefab and a platform variant, like PaddleModel and Paddle:
    /// ModeMenu is the Meta-free UGUI panel (canvas, buttons, presenter); ModeMenuMeta adds pointing
    /// with the rig's Meta rays (PointableCanvas + RayInteractable on a clipped plane). Another input
    /// (hands, poke, desktop) is another variant. Idempotent: existing children are reused and every
    /// value is written the same way each run.
    /// </summary>
    internal static class ModeMenuBuilder
    {
        const string BasePath = "Assets/_Project/Prefabs/UI/ModeMenu.prefab";
        /// <summary>The variant the scene uses.</summary>
        internal const string PrefabPath = "Assets/_Project/Prefabs/UI/ModeMenuMeta.prefab";

        const float PixelsPerMetre = 1000f;            // canvas units are millimetres
        static readonly Vector2 Size = new Vector2(560f, 600f);
        static readonly Color PanelColor = new Color(0.07f, 0.09f, 0.13f, 0.92f);
        static readonly Color ButtonColor = new Color(0.18f, 0.22f, 0.3f, 1f);
        static readonly Color TextColor = new Color(0.95f, 0.96f, 1f, 1f);

        internal static void Build()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(BasePath) == null)
            {
                EnsureFolder(System.IO.Path.GetDirectoryName(BasePath).Replace('\\', '/'));
                var empty = new GameObject("ModeMenu");
                PrefabUtility.SaveAsPrefabAsset(empty, BasePath);
                Object.DestroyImmediate(empty);
            }
            EditPrefab(BasePath, BuildContents);

            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(BasePath));
                PrefabUtility.SaveAsPrefabAsset(instance, PrefabPath); // saving an instance makes a variant
                Object.DestroyImmediate(instance);
            }
            EditPrefab(PrefabPath, AddMetaRayPointing);
        }

        static void BuildContents(GameObject root)
        {
            var panel = GetOrCreate("Panel", root.transform);

            // Canvas: millimetre units, scaled to metres.
            var canvasGo = GetOrCreate("Canvas", panel.transform);
            var canvas = GetOrAdd<Canvas>(canvasGo);
            canvas.renderMode = RenderMode.WorldSpace;
            // The channels TextMeshPro asks for at runtime; stored up front so scene instances don't override it.
            canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.TexCoord1 | AdditionalCanvasShaderChannels.Normal | AdditionalCanvasShaderChannels.Tangent;
            GetOrAdd<GraphicRaycaster>(canvasGo);
            var rect = (RectTransform)canvasGo.transform;
            rect.sizeDelta = Size;
            rect.localPosition = Vector3.zero;
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one / PixelsPerMetre;
            var background = GetOrAdd<Image>(canvasGo);
            background.sprite = Sprite("UI/Skin/Background.psd");
            background.type = Image.Type.Sliced;
            background.color = PanelColor;
            var layout = GetOrAdd<VerticalLayoutGroup>(canvasGo);
            layout.padding = new RectOffset(32, 32, 28, 28);
            layout.spacing = 14f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var title = Text(canvasGo.transform, "Title", PlayerSettings.productName, 44f, FontStyles.Bold, 60f);
            Text(canvasGo.transform, "SoloLabel", MenuText.Solo, 30f, FontStyles.Normal, 40f);
            var levels = GetOrCreate("Levels", canvasGo.transform);
            var row = GetOrAdd<HorizontalLayoutGroup>(levels);
            row.spacing = 12f;
            row.childControlWidth = row.childControlHeight = true;
            row.childForceExpandWidth = true;
            row.childForceExpandHeight = true;
            GetOrAdd<LayoutElement>(levels).preferredHeight = 72f;
            var levelTemplate = ButtonAt(levels.transform, "LevelTemplate", "Level");
            levelTemplate.gameObject.SetActive(false);
            var online = ButtonAt(canvasGo.transform, "Online", MenuText.OnlineUnavailable);
            var passthrough = ButtonAt(canvasGo.transform, "Passthrough", MenuText.Passthrough(false));
            var restart = ButtonAt(canvasGo.transform, "Restart", MenuText.Restart);
            var close = ButtonAt(canvasGo.transform, "Close", MenuText.Close);
            Text(canvasGo.transform, "Hint", MenuText.Hint, 22f, FontStyles.Italic, 34f);

            // Meta pointing belongs to the variant (the base predates the split and may still carry it).
            foreach (var c in panel.GetComponents<RayInteractable>()) Object.DestroyImmediate(c);
            foreach (var c in panel.GetComponents<PointableCanvas>()) Object.DestroyImmediate(c);
            var oldSurface = panel.transform.Find("Surface");
            if (oldSurface != null) Object.DestroyImmediate(oldSurface.gameObject);

            var presenter = GetOrAdd<ModeMenuPresenter>(root);
            Set(presenter, "_panel", panel);
            Set(presenter, "_title", title);
            Set(presenter, "_levelTemplate", levelTemplate);
            Set(presenter, "_online", online);
            Set(presenter, "_passthrough", passthrough);
            Set(presenter, "_restart", restart);
            Set(presenter, "_close", close);

            StoreAsUnityWould(canvasGo.transform);
        }

        /// <summary>
        /// Stores each text's packed colour and style hash to match its colour and style (TextMeshPro only fills
        /// them at runtime), so scene instances don't record them as overrides.
        /// </summary>
        static void StoreAsUnityWould(Transform canvas)
        {
            int normalStyle = TMP_Style.NormalStyle.hashCode;
            foreach (var text in canvas.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                var so = new SerializedObject(text);
                Color32 c = text.color;
                so.FindProperty("m_fontColor32.rgba").longValue = c.r | (c.g << 8) | (c.b << 16) | ((long)c.a << 24);
                so.FindProperty("m_TextStyleHashCode").intValue = normalStyle;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        /// <summary>Meta rays: the panel becomes a pointable canvas on a plane clipped to the canvas rectangle.</summary>
        static void AddMetaRayPointing(GameObject root)
        {
            var panel = root.transform.Find("Panel").gameObject;
            var canvas = panel.GetComponentInChildren<Canvas>(true);
            var surfaceGo = GetOrCreate("Surface", panel.transform);
            surfaceGo.transform.localPosition = Vector3.zero;
            surfaceGo.transform.localRotation = Quaternion.identity;
            surfaceGo.transform.localScale = new Vector3(Size.x / PixelsPerMetre, Size.y / PixelsPerMetre, 0.01f);
            var plane = GetOrAdd<PlaneSurface>(surfaceGo);
            var clipper = GetOrAdd<BoundsClipper>(surfaceGo); // unit box, scaled with the surface
            var clipped = GetOrAdd<ClippedPlaneSurface>(surfaceGo);
            Set(clipped, "_planeSurface", plane);
            SetArray(clipped, "_clippers", new Object[] { clipper });
            var pointable = GetOrAdd<PointableCanvas>(panel);
            Set(pointable, "_canvas", canvas);
            var ray = GetOrAdd<RayInteractable>(panel);
            Set(ray, "_surface", clipped);
            Set(ray, "_pointableElement", pointable);
        }

        static TextMeshProUGUI Text(Transform parent, string name, string text, float size, FontStyles style, float height)
        {
            var go = GetOrCreate(name, parent);
            var tmp = GetOrAdd<TextMeshProUGUI>(go);
            tmp.text = text;
            tmp.fontSize = size;
            tmp.fontStyle = style;
            tmp.color = TextColor;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            GetOrAdd<LayoutElement>(go).preferredHeight = height;
            return tmp;
        }

        static Button ButtonAt(Transform parent, string name, string label)
        {
            var go = GetOrCreate(name, parent);
            var image = GetOrAdd<Image>(go);
            image.sprite = Sprite("UI/Skin/UISprite.psd");
            image.type = Image.Type.Sliced;
            image.color = ButtonColor;
            var button = GetOrAdd<Button>(go);
            button.targetGraphic = image;
            var colors = ColorBlock.defaultColorBlock;
            colors.highlightedColor = new Color(0.75f, 0.85f, 1f);
            colors.pressedColor = new Color(0.55f, 0.65f, 0.85f);
            colors.disabledColor = new Color(0.6f, 0.6f, 0.6f, 0.5f);
            button.colors = colors;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            GetOrAdd<LayoutElement>(go).preferredHeight = 72f;

            var text = Text(go.transform, "Label", label, 30f, FontStyles.Normal, 72f);
            text.richText = true;
            var r = text.rectTransform; // fill the button
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = r.offsetMax = Vector2.zero;
            text.raycastTarget = false;
            return button;
        }

        static Sprite Sprite(string builtinPath) => AssetDatabase.GetBuiltinExtraResource<Sprite>(builtinPath);
    }
}

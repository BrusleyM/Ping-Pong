using System.Collections.Generic;
using Framework;
using PingPong.Core;
using PingPong.Gameplay;
using PingPong.Presentation;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static PingPong.EditorTools.MenuUi;
using static PingPong.EditorTools.SetupHelpers;

namespace PingPong.EditorTools
{
    internal static class MenuSceneBuilder
    {
        internal const string GamePath = "Assets/_Project/Scenes/Game.unity";
        internal const string MenuPath = "Assets/_Project/Scenes/Menu.unity";
        private const string OldMainPath = "Assets/_Project/Scenes/Main.unity";
        private const float CanvasMetres = 1.5f;

        [MenuItem("Ping Pong/Build Menu Scene")]
        internal static void Build()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            MenuAssetsBuilder.Build();
            Theme = MenuAssetsBuilder.Theme;
            SplitScenes();
            var scene = EditorSceneManager.OpenScene(MenuPath, OpenSceneMode.Single);
            StripGameplay(scene);
            BuildMenu(scene);
            EditorSceneManager.SaveScene(scene, MenuPath);
            SetBuildScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("Ping Pong: Menu scene built.");
        }

        private static void SplitScenes()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(GamePath) == null && AssetDatabase.LoadAssetAtPath<SceneAsset>(OldMainPath) != null)
            {
                AssetDatabase.CopyAsset(OldMainPath, MenuPath);
                AssetDatabase.MoveAsset(OldMainPath, GamePath);
            }
            else if (AssetDatabase.LoadAssetAtPath<SceneAsset>(MenuPath) == null)
            {
                AssetDatabase.CopyAsset(GamePath, MenuPath);
            }
            else
            {
                AssetDatabase.DeleteAsset(MenuPath);
                AssetDatabase.CopyAsset(GamePath, MenuPath);
            }
        }

        private static void StripGameplay(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name != "World")
                {
                    continue;
                }

                if (PrefabUtility.IsPartOfPrefabInstance(root))
                {
                    PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.OutermostRoot, InteractionMode.AutomatedAction);
                }

                foreach (var path in new[] { "Match", "Platform", "Tutorial", "Player/Paddle", "Player/WristGlanceMenu", "Environment/Table_Tennis", "Environment/Table", "UI/ModeMenuMeta" })
                {
                    var child = root.transform.Find(path);
                    if (child != null)
                    {
                        Object.DestroyImmediate(child.gameObject);
                    }
                }
            }
        }

        private static void BuildMenu(Scene scene)
        {
            var head = FindInScene(scene, "CenterEyeAnchor");
            var root = GetOrCreate("MenuRoot");
            root.transform.position = new Vector3(0f, 1.5f, 1.2f);

            var stage = BuildPreviewStage(out var camera, out var preview);
            var canvas = BuildCanvas(root.transform);
            var flow = BuildScreens(canvas, camera, preview);

            GetOrAdd<SettingsApplier>(GetOrCreate("SettingsApplier"));

            var placer = GetOrAdd<EyeHeightPlacer>(root);
            Set(placer, "_eyes", head);
            Set(placer, "_target", root.transform);
            SetF(placer, "_distanceInFront", 1.2f);
            SetF(placer, "_heightOffset", -0.05f);
            SetEnum(placer, "_facing", (int)PlacementFacing.SameAsViewDirection);
            SetBool(placer, "_placeOnEnable", true);
            SetF(placer, "_placeOnEnableDelaySeconds", 1.5f);

            var loader = GetOrAdd<UnitySceneLoader>(GetOrCreate("SceneLoader"));
            var difficulty = canvas.GetComponentInChildren<DifficultyScreenPresenter>(true);
            SetSeam(difficulty, "_loader", loader);

            Selection.activeObject = flow;
            stage.name = "PreviewStage";
        }

        internal static Transform FindInScene(Scene scene, string name)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name == name)
                    {
                        return t;
                    }
                }
            }

            return null;
        }

        private static GameObject BuildPreviewStage(out Camera camera, out CharacterPreview preview)
        {
            var stage = GetOrCreate("PreviewStage");
            stage.transform.position = new Vector3(0f, -300f, 0f);
            var character = stage.transform.Find("Character");
            if (character == null)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Art/Character/Prefabs/Character.prefab");
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, stage.transform);
                instance.name = "Character";
                character = instance.transform;
            }

            character.localPosition = Vector3.zero;
            character.localRotation = Quaternion.Euler(0f, 160f, 0f);
            foreach (var controller in character.GetComponentsInChildren<CharacterController>(true))
            {
                Object.DestroyImmediate(controller);
            }

            var cameraObject = GetOrCreate("Camera", stage.transform);
            cameraObject.transform.localPosition = new Vector3(0f, 1f, 3.2f);
            cameraObject.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            camera = GetOrAdd<Camera>(cameraObject);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
            camera.fieldOfView = 28f;
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 12f;
            camera.targetTexture = AssetDatabase.LoadAssetAtPath<RenderTexture>(MenuAssetsBuilder.PreviewTexturePath);
            camera.enabled = false;
            var listener = cameraObject.GetComponent<AudioListener>();
            if (listener != null)
            {
                Object.DestroyImmediate(listener);
            }

            var light = GetOrCreate("Light", stage.transform);
            light.transform.localPosition = new Vector3(1.2f, 2.6f, 2.2f);
            var lightComponent = GetOrAdd<Light>(light);
            lightComponent.type = LightType.Point;
            lightComponent.range = 9f;
            lightComponent.intensity = 6f;

            preview = GetOrAdd<CharacterPreview>(stage);
            var renderers = character.GetComponentsInChildren<Renderer>(true);
            SetArray(preview, "_renderers", renderers);
            Set(preview, "_red", AssetDatabase.LoadAssetAtPath<Material>(MenuAssetsBuilder.RedMaterialPath));
            Set(preview, "_blue", AssetDatabase.LoadAssetAtPath<Material>(MenuAssetsBuilder.BlueMaterialPath));
            Set(preview, "_turntable", character);
            return stage;
        }

        private static RectTransform BuildCanvas(Transform root)
        {
            var canvasObject = GetOrCreate("MenuCanvas", root);
            var canvas = GetOrAdd<Canvas>(canvasObject);
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.TexCoord1 | AdditionalCanvasShaderChannels.Normal | AdditionalCanvasShaderChannels.Tangent;
            GetOrAdd<GraphicRaycaster>(canvasObject);
            var rect = (RectTransform)canvasObject.transform;
            rect.sizeDelta = new Vector2(1600f, 900f);
            rect.localPosition = Vector3.zero;
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one * (CanvasMetres / 1600f);

            var backdrop = Picture(rect, "Backdrop", "ui_panel_large", Vector2.zero, new Vector2(1472f, 772f));
            backdrop.raycastTarget = true;

            ModeMenuBuilder.AddCanvasTemplate(canvas, "ISDK_RayCanvasInteraction", ModeMenuBuilder.RayCanvasTemplateGuid);
            ModeMenuBuilder.AddCanvasTemplate(canvas, "ISDK_PokeCanvasInteraction", ModeMenuBuilder.PokeCanvasTemplateGuid);
            return rect;
        }

        private static MenuFlowPresenter BuildScreens(RectTransform canvas, Camera previewCamera, CharacterPreview preview)
        {
            var links = new List<(PopButton button, MenuScreen target, bool back)>();
            var panels = new List<(MenuScreen screen, PanelPop panel)>();

            var main = BuildMain(canvas, out var play, out var tutorial, out var quit, out var chip, out var chipName, out var chipDot, out var settingsButton);
            panels.Add((MenuScreen.Main, main));
            links.Add((play, MenuScreen.ChooseWhere, false));
            links.Add((settingsButton, MenuScreen.Settings, false));

            var where = BuildChoice(canvas, "ChooseWhere", "STEP 1 OF 3", "Choose where", "Where do you want to play?", ("Local", "Play on this headset", "L", false), ("Online", "Play the world", "O", true), out var whereBack, out var local, out var online);
            panels.Add((MenuScreen.ChooseWhere, where));
            links.Add((whereBack, MenuScreen.Main, true));
            links.Add((local, MenuScreen.ChooseWho, false));

            var who = BuildChoice(canvas, "ChooseWho", "STEP 2 OF 3", "Choose who", "Pick your opponent.", ("Solo vs AI", "Challenge the machine", "AI", false), ("With a friend", "Play together", "2", true), out var whoBack, out var solo, out var friend);
            panels.Add((MenuScreen.ChooseWho, who));
            links.Add((whoBack, MenuScreen.ChooseWhere, true));
            links.Add((solo, MenuScreen.ChooseDifficulty, false));

            var difficulty = BuildDifficulty(canvas, out var difficultyBack);
            panels.Add((MenuScreen.ChooseDifficulty, difficulty));
            links.Add((difficultyBack, MenuScreen.ChooseWho, true));

            var settings = BuildSettings(canvas, out var settingsBack);
            panels.Add((MenuScreen.Settings, settings));
            links.Add((settingsBack, MenuScreen.Main, true));

            var profile = BuildProfile(canvas, previewCamera, preview, out var profileBack, out var profilePresenter);
            panels.Add((MenuScreen.Profile, profile));
            links.Add((profileBack, MenuScreen.Main, true));

            var confirm = BuildConfirm(canvas);
            var toast = BuildToast(canvas);

            var flowObject = GetOrCreate("MenuFlow", canvas);
            RectOf(flowObject);
            var flow = GetOrAdd<MenuFlowPresenter>(flowObject);
            var switcher = GetOrAdd<MenuScreenSwitcher>(flowObject);
            WriteScreenPanels(switcher, panels);
            WriteLinks(switcher, links);
            Set(flow, "_switcher", switcher);
            Set(flow, "_profile", profilePresenter);
            Set(flow, "_tutorial", tutorial);
            Set(flow, "_online", online);
            Set(flow, "_friend", friend);
            Set(flow, "_editProfile", chip);
            Set(flow, "_quit", quit);
            Set(flow, "_chipName", chipName);
            Set(flow, "_theme", Theme);
            Set(flow, "_confirm", confirm);
            Set(flow, "_toast", toast);
            Set(flow, "_chipDot", chipDot);
            return flow;
        }

        private static PanelPop BuildMain(Transform canvas, out PopButton play, out PopButton tutorial, out PopButton quit, out PopButton chip, out TMP_Text chipName, out Image chipDot, out PopButton settings)
        {
            var panel = Panel(canvas, "MainMenu");
            var t = panel.transform;
            Picture(t, "Dot1", "ui_round", new Vector2(-470f, 130f), new Vector2(46f, 46f), Theme.Accent);
            Picture(t, "Dot2", "ui_round", new Vector2(480f, 270f), new Vector2(34f, 34f), Theme.Success);
            Picture(t, "Dot3", "ui_round", new Vector2(-520f, 250f), new Vector2(28f, 28f), Theme.Secondary);
            Picture(t, "Dot4", "ui_round", new Vector2(520f, 120f), new Vector2(52f, 52f), Theme.Primary);
            Picture(t, "Ball", "ui_round", new Vector2(360f, 215f), new Vector2(84f, 84f));
            Picture(t, "Paddle", "ui_round", new Vector2(-380f, 215f), new Vector2(130f, 130f), Theme.Primary);
            Label(t, "TitleTop", "Ping Pong", 130f, true, Color.white, new Vector2(0f, 230f), new Vector2(900f, 150f));
            var mania = Label(t, "TitleMania", "MANIA!", 150f, true, Theme.Accent, new Vector2(0f, 95f), new Vector2(900f, 170f));
            mania.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -3f);
            play = Button(t, "Play", "Play", "ui_button_primary", new Vector2(0f, -70f), new Vector2(560f, 150f), 70f, Color.white);
            tutorial = Button(t, "Tutorial", "Tutorial", "ui_button_secondary", new Vector2(0f, -235f), new Vector2(460f, 110f), 48f, Color.white, true, true);
            quit = Button(t, "Quit", "Quit game", "ui_button_tertiary", new Vector2(0f, -345f), new Vector2(260f, 66f), 30f, Color.white);
            chip = Button(t, "ProfileChip", string.Empty, "ui_button_white", new Vector2(-520f, 330f), new Vector2(340f, 96f), 10f, Theme.Ink);
            chip.transform.Find("Label").gameObject.SetActive(false);
            chipDot = Picture(chip.transform, "Dot", "ui_round", new Vector2(-120f, 4f), new Vector2(60f, 60f), Theme.TeamRed);
            Label(chip.transform, "Caption", "PLAYER", 22f, false, new Color(0.45f, 0.35f, 0.65f, 1f), new Vector2(30f, 24f), new Vector2(200f, 28f), TextAlignmentOptions.Left);
            chipName = Label(chip.transform, "Name", "Loading…", 34f, true, Theme.Ink, new Vector2(30f, -10f), new Vector2(200f, 44f), TextAlignmentOptions.Left);
            settings = Button(t, "Settings", "Settings", "ui_button_secondary", new Vector2(560f, 330f), new Vector2(250f, 84f), 36f, Color.white);
            return panel;
        }

        private static PanelPop BuildChoice(Transform canvas, string name, string eyebrow, string title, string sub, (string title, string note, string glyph, bool soon) left, (string title, string note, string glyph, bool soon) right, out PopButton back, out PopButton first, out PopButton second)
        {
            var panel = Panel(canvas, name);
            var t = panel.transform;
            Heading(t, eyebrow, title, sub);
            back = BackButton(t);
            first = ModeCard(t, left.title.Replace(" ", string.Empty), left.title, left.note, left.glyph, new Vector2(-230f, -95f), Theme.Secondary, left.soon);
            second = ModeCard(t, right.title.Replace(" ", string.Empty), right.title, right.note, right.glyph, new Vector2(230f, -95f), Theme.Primary, right.soon);
            return panel;
        }

        private static PanelPop BuildDifficulty(Transform canvas, out PopButton back)
        {
            var panel = Panel(canvas, "ChooseDifficulty");
            var t = panel.transform;
            Heading(t, "STEP 3 OF 3", "Choose difficulty", "How spicy should this match be?");
            back = BackButton(t);
            var list = Rect(t, "Levels", new Vector2(0f, -120f), new Vector2(780f, 440f));
            var layout = GetOrAdd<VerticalLayoutGroup>(list.gameObject);
            layout.spacing = 22f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = layout.childControlHeight = false;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            var template = Button(list, "LevelTemplate", string.Empty, "ui_button_white", Vector2.zero, new Vector2(780f, 118f), 10f, Theme.Ink);
            template.transform.Find("Label").gameObject.SetActive(false);
            Label(template.transform, "Name", "Level", 56f, true, Theme.Ink, new Vector2(0f, 14f), new Vector2(740f, 64f));
            Label(template.transform, "Note", "Note", 30f, false, new Color(0.14f, 0.07f, 0.25f, 0.75f), new Vector2(0f, -30f), new Vector2(740f, 40f));
            template.gameObject.SetActive(false);

            var loading = Picture(t, "Loading", "ui_panel_large", Vector2.zero, new Vector2(1472f, 772f), new Color(0.1f, 0.04f, 0.25f, 0.92f));
            loading.raycastTarget = true;
            Label(loading.transform, "Text", "Getting the table ready…", 70f, true, Color.white, Vector2.zero, new Vector2(1200f, 110f));
            loading.gameObject.SetActive(false);

            var presenter = GetOrAdd<DifficultyScreenPresenter>(panel.gameObject);
            Set(presenter, "_theme", Theme);
            Set(presenter, "_ai", FindAiProfile());
            Set(presenter, "_template", template);
            Set(presenter, "_loading", loading.gameObject);
            return panel;
        }

        private static Object FindAiProfile()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:AIProfile"))
            {
                return AssetDatabase.LoadAssetAtPath<AIProfile>(AssetDatabase.GUIDToAssetPath(guid));
            }

            return null;
        }

        internal static PanelPop BuildSettings(Transform canvas, out PopButton back)
        {
            var panel = Panel(canvas, "Settings");
            var t = panel.transform;
            Heading(t, "MAKE IT YOURS", "Settings", "Changes apply right away.");
            back = BackButton(t);
            var card = Picture(t, "Card", "ui_panel_inner", new Vector2(0f, -120f), new Vector2(1180f, 330f));

            Label(card.transform, "EffectsLabel", "Sound effects", 44f, true, Color.white, new Vector2(-380f, 90f), new Vector2(420f, 60f), TextAlignmentOptions.Left);
            var effects = SliderRow(card.transform, "Effects", new Vector2(150f, 90f), 480f);
            var effectsValue = Label(card.transform, "EffectsValue", "100%", 40f, true, Theme.Accent, new Vector2(450f, 90f), new Vector2(160f, 60f));

            Label(card.transform, "HandLabel", "Paddle hand", 44f, true, Color.white, new Vector2(-380f, -60f), new Vector2(420f, 60f), TextAlignmentOptions.Left);
            var left = Button(card.transform, "HandLeft", "Left", "ui_button_secondary", new Vector2(200f, -60f), new Vector2(220f, 84f), 40f, Color.white);
            var right = Button(card.transform, "HandRight", "Right", "ui_button_secondary", new Vector2(450f, -60f), new Vector2(220f, 84f), 40f, Color.white);
            var leftRing = Picture(left.transform, "Selected", "ui_ring", Vector2.zero, new Vector2(240f, 104f));
            var rightRing = Picture(right.transform, "Selected", "ui_ring", Vector2.zero, new Vector2(240f, 104f));

            var presenter = GetOrAdd<SettingsScreenPresenter>(panel.gameObject);
            Set(presenter, "_effects", effects);
            Set(presenter, "_effectsValue", effectsValue);
            Set(presenter, "_left", left);
            Set(presenter, "_right", right);
            Set(presenter, "_leftSelected", leftRing.gameObject);
            Set(presenter, "_rightSelected", rightRing.gameObject);
            return panel;
        }

        private static PanelPop BuildProfile(Transform canvas, Camera previewCamera, CharacterPreview preview, out PopButton back, out ProfileScreenPresenter presenter)
        {
            var panel = Panel(canvas, "Profile");
            var t = panel.transform;
            Label(t, "Eyebrow", "YOUR PLAYER", 30f, false, Theme.Accent, new Vector2(0f, 350f), new Vector2(900f, 44f));
            var heading = Label(t, "Title", "Create your player", 76f, true, Color.white, new Vector2(0f, 290f), new Vector2(1100f, 96f));
            back = BackButton(t, "Cancel");

            var field = Picture(t, "NameField", "ui_text_field", new Vector2(-230f, 165f), new Vector2(760f, 104f));
            var nickname = Label(field.transform, "Text", "Type your nickname", 52f, true, Theme.Ink, Vector2.zero, new Vector2(720f, 90f));

            var grid = Rect(t, "Keyboard", new Vector2(-230f, -62f), new Vector2(860f, 250f));
            var layout = GetOrAdd<GridLayoutGroup>(grid.gameObject);
            layout.cellSize = new Vector2(78f, 78f);
            layout.spacing = new Vector2(8f, 8f);
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = 10;
            layout.childAlignment = TextAnchor.UpperCenter;
            var letter = Button(grid, "LetterTemplate", "A", "ui_button_dark", Vector2.zero, new Vector2(78f, 78f), 40f, Color.white);
            var space = Button(grid, "Space", "SPACE", "ui_button_tertiary", Vector2.zero, new Vector2(78f, 78f), 18f, Color.white);
            var backspace = Button(grid, "Backspace", "DEL", "ui_button_danger", Vector2.zero, new Vector2(78f, 78f), 28f, Color.white);
            var keyboard = GetOrAdd<NicknameKeyboard>(grid.gameObject);
            Set(keyboard, "_letterTemplate", letter);
            Set(keyboard, "_space", space);
            Set(keyboard, "_backspace", backspace);

            var preview3d = GetOrCreate("CharacterImage", t);
            var rawRect = RectOf(preview3d);
            rawRect.anchorMin = rawRect.anchorMax = new Vector2(0.5f, 0.5f);
            rawRect.anchoredPosition = new Vector2(540f, 30f);
            rawRect.sizeDelta = new Vector2(280f, 420f);
            rawRect.localScale = Vector3.one;
            var raw = GetOrAdd<RawImage>(preview3d);
            raw.texture = AssetDatabase.LoadAssetAtPath<RenderTexture>(MenuAssetsBuilder.PreviewTexturePath);
            raw.raycastTarget = false;

            var red = Button(t, "TeamRed", "Red", "ui_button_danger", new Vector2(440f, -230f), new Vector2(180f, 76f), 38f, Color.white);
            var blue = Button(t, "TeamBlue", "Blue", "ui_button_secondary", new Vector2(640f, -230f), new Vector2(180f, 76f), 38f, Color.white);
            var redRing = Picture(red.transform, "Selected", "ui_ring", Vector2.zero, new Vector2(200f, 96f));
            var blueRing = Picture(blue.transform, "Selected", "ui_ring", Vector2.zero, new Vector2(200f, 96f));

            var save = Button(t, "Save", "Let's go!", "ui_button_primary", new Vector2(-230f, -285f), new Vector2(520f, 104f), 56f, Color.white);
            var hint = Label(t, "Hint", string.Empty, 30f, false, Theme.Accent, new Vector2(-230f, 84f), new Vector2(700f, 40f));

            presenter = GetOrAdd<ProfileScreenPresenter>(panel.gameObject);
            Set(presenter, "_keyboard", keyboard);
            Set(presenter, "_nickname", nickname);
            Set(presenter, "_hint", hint);
            Set(presenter, "_red", red);
            Set(presenter, "_blue", blue);
            Set(presenter, "_redSelected", redRing.gameObject);
            Set(presenter, "_blueSelected", blueRing.gameObject);
            Set(presenter, "_save", save);
            Set(presenter, "_saveLabel", save.GetComponentInChildren<TMP_Text>());
            Set(presenter, "_heading", heading);
            Set(presenter, "_preview", preview);
            Set(presenter, "_cancelButton", back.gameObject);
            Set(presenter, "_previewCamera", previewCamera);
            return panel;
        }

        internal static ConfirmDialog BuildConfirm(Transform canvas)
        {
            var root = GetOrCreate("Confirm", canvas);
            var rect = RectOf(root);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
            var panel = Panel(rect, "Dim");
            var dim = Picture(panel.transform, "Shade", "ui_panel_large", Vector2.zero, new Vector2(1472f, 772f), new Color(0.05f, 0.02f, 0.15f, 0.85f));
            dim.raycastTarget = true;
            var box = Picture(panel.transform, "Box", "ui_panel_inner", Vector2.zero, new Vector2(820f, 430f));
            var title = Label(box.transform, "Title", "Leave?", 62f, true, Color.white, new Vector2(0f, 135f), new Vector2(760f, 80f));
            var body = Label(box.transform, "Body", string.Empty, 38f, false, new Color(1f, 1f, 1f, 0.85f), new Vector2(0f, 30f), new Vector2(720f, 130f));
            var stay = Button(box.transform, "Stay", "Stay", "ui_button_tertiary", new Vector2(-190f, -130f), new Vector2(330f, 100f), 46f, Color.white);
            var confirm = Button(box.transform, "Confirm", "Leave", "ui_button_danger", new Vector2(190f, -130f), new Vector2(330f, 100f), 46f, Color.white);
            panel.gameObject.SetActive(false);

            var dialog = GetOrAdd<ConfirmDialog>(root);
            Set(dialog, "_panel", panel);
            Set(dialog, "_title", title);
            Set(dialog, "_body", body);
            Set(dialog, "_confirmLabel", confirm.GetComponentInChildren<TMP_Text>());
            Set(dialog, "_confirm", confirm);
            Set(dialog, "_cancel", stay);
            return dialog;
        }

        internal static ToastPresenter BuildToast(Transform canvas)
        {
            var root = GetOrCreate("Toast", canvas);
            var rect = RectOf(root);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, -395f);
            rect.sizeDelta = new Vector2(620f, 96f);
            rect.localScale = Vector3.one;
            var panel = GetOrCreate("Panel", rect);
            var panelRect = RectOf(panel);
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = panelRect.offsetMax = Vector2.zero;
            panelRect.localScale = Vector3.one;
            GetOrAdd<CanvasGroup>(panel);
            var pop = GetOrAdd<PanelPop>(panel);
            Set(pop, "_theme", Theme);
            var image = GetOrAdd<Image>(panel);
            image.sprite = MenuAssetsBuilder.SpriteAt("ui_toast");
            image.type = Image.Type.Sliced;
            image.raycastTarget = false;
            var label = Label(panel.transform, "Label", "Saved!", 40f, true, Theme.Ink, Vector2.zero, new Vector2(580f, 70f));
            panel.SetActive(false);

            var toast = GetOrAdd<ToastPresenter>(root);
            Set(toast, "_panel", pop);
            Set(toast, "_label", label);
            return toast;
        }

        internal static void WriteScreenPanels(Component flow, List<(MenuScreen screen, PanelPop panel)> panels)
        {
            var so = new SerializedObject(flow);
            var array = so.FindProperty("_panels");
            array.arraySize = panels.Count;
            for (var i = 0; i < panels.Count; i++)
            {
                var element = array.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("Screen").enumValueIndex = (int)panels[i].screen;
                element.FindPropertyRelative("Panel").objectReferenceValue = panels[i].panel;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        internal static void WriteLinks(Component flow, List<(PopButton button, MenuScreen target, bool back)> links)
        {
            var so = new SerializedObject(flow);
            var array = so.FindProperty("_links");
            array.arraySize = links.Count;
            for (var i = 0; i < links.Count; i++)
            {
                var element = array.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("Button").objectReferenceValue = links[i].button;
                element.FindPropertyRelative("Target").enumValueIndex = (int)links[i].target;
                element.FindPropertyRelative("GoesBack").boolValue = links[i].back;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetBuildScenes()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(MenuPath, true),
                new EditorBuildSettingsScene(GamePath, true)
            };
        }
    }
}

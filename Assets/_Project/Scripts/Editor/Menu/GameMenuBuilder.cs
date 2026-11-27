using System.Collections.Generic;
using Framework;
using PingPong.Core;
using PingPong.Gameplay;
using PingPong.Presentation;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using static PingPong.EditorTools.MenuUi;
using static PingPong.EditorTools.SetupHelpers;

namespace PingPong.EditorTools
{
    internal static class GameMenuBuilder
    {
        private const float CanvasMetres = 0.9f;

        internal const string PauseMenuRoot = "PauseMenu";
        internal const string PauseMenuCanvasRoot = "PauseMenuCanvas";

        [MenuItem("Ping Pong/Build Game Menus")]
        internal static void Build()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            MenuAssetsBuilder.Build();
            Theme = MenuAssetsBuilder.Theme;
            var scene = EditorSceneManager.OpenScene(MenuSceneBuilder.GamePath, OpenSceneMode.Single);
            var world = FindRoot(scene, "World");
            var modes = world.transform.Find("Match").GetComponent<GameModeController>();
            var grip = world.transform.Find("Player/Paddle").GetComponent<PaddleGrip>();
            var head = MenuSceneBuilder.FindInScene(scene, "CenterEyeAnchor");

            Shelve(world, "UI/ModeMenuMeta");
            Shelve(world, "Tutorial");

            var loader = GetOrAdd<UnitySceneLoader>(GetOrCreate("SceneLoader"));
            var starter = GetOrAdd<GameStarter>(GetOrCreate("GameStarter"));
            Set(starter, "_modes", modes);
            var applier = GetOrAdd<SettingsApplier>(GetOrCreate("SettingsApplier"));
            Set(applier, "_paddleGrip", grip);

            var colouring = GetOrAdd<TeamColouring>(GetOrCreate("TeamColouring"));
            Set(colouring, "_theme", Theme);
            Set(colouring, "_playerHandle", HandleOf(world, "Player/Paddle"));
            Set(colouring, "_opponentHandle", HandleOf(world, "Match/AIPaddle"));

            foreach (var name in new[] { PauseMenuRoot, PauseMenuCanvasRoot })
            {
                var existing = FindRoot(scene, name);
                if (existing != null)
                {
                    Object.DestroyImmediate(existing);
                }
            }

            BuildPauseMenu(modes, head, loader);
            EditorSceneManager.SaveScene(scene, MenuSceneBuilder.GamePath);
            Debug.Log("Ping Pong: Game menus built.");
        }

        private static Renderer HandleOf(GameObject world, string paddlePath)
        {
            foreach (var renderer in world.transform.Find(paddlePath).GetComponentsInChildren<Renderer>(true))
            {
                if (renderer.name == "HandleMesh")
                {
                    return renderer;
                }
            }

            return null;
        }

        private static GameObject FindRoot(Scene scene, string name)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name == name)
                {
                    return root;
                }
            }

            return null;
        }

        private static void Shelve(GameObject world, string path)
        {
            var target = world.transform.Find(path);
            if (target != null)
            {
                target.gameObject.SetActive(false);
            }
        }

        private static void BuildPauseMenu(GameModeController modes, Transform head, UnitySceneLoader loader)
        {
            var controller = new GameObject(PauseMenuRoot);
            var canvasObject = new GameObject(PauseMenuCanvasRoot);
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.TexCoord1 | AdditionalCanvasShaderChannels.Normal | AdditionalCanvasShaderChannels.Tangent;
            canvasObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            var rect = (RectTransform)canvasObject.transform;
            rect.sizeDelta = new Vector2(1600f, 900f);
            rect.localScale = Vector3.one * (CanvasMetres / 1600f);

            var backdrop = Picture(rect, "Backdrop", "ui_panel_large", Vector2.zero, new Vector2(1472f, 772f));
            backdrop.raycastTarget = true;
            ModeMenuBuilder.AddCanvasTemplate(canvas, "ISDK_RayCanvasInteraction", ModeMenuBuilder.RayCanvasTemplateGuid);
            ModeMenuBuilder.AddCanvasTemplate(canvas, "ISDK_PokeCanvasInteraction", ModeMenuBuilder.PokeCanvasTemplateGuid);

            var panels = new List<(MenuScreen screen, PanelPop panel)>();
            var links = new List<(PopButton button, MenuScreen target, bool back)>();

            var pause = Panel(rect, "Pause");
            var t = pause.transform;
            Label(t, "Eyebrow", "TAKE A BREAK", 30f, false, Theme.Accent, new Vector2(0f, 330f), new Vector2(900f, 44f));
            Label(t, "Title", "Paused", 90f, true, Color.white, new Vector2(0f, 255f), new Vector2(1100f, 110f));
            var resume = Button(t, "Resume", "Resume", "ui_button_primary", new Vector2(0f, 110f), new Vector2(560f, 120f), 60f, Color.white);
            var restart = Button(t, "Restart", "Restart", "ui_button_secondary", new Vector2(0f, -25f), new Vector2(460f, 100f), 48f, Color.white);
            var passthrough = Button(t, "Passthrough", "Passthrough: OFF", "ui_button_secondary", new Vector2(0f, -140f), new Vector2(560f, 100f), 44f, Color.white);
            var settings = Button(t, "Settings", "Settings", "ui_button_secondary", new Vector2(0f, -255f), new Vector2(460f, 100f), 48f, Color.white);
            var quit = Button(t, "QuitToMenu", "Quit to main menu", "ui_button_tertiary", new Vector2(0f, -350f), new Vector2(380f, 64f), 30f, Color.white);
            panels.Add((MenuScreen.Pause, pause));
            links.Add((settings, MenuScreen.Settings, false));

            var settingsPanel = MenuSceneBuilder.BuildSettings(rect, out var settingsBack);
            panels.Add((MenuScreen.Settings, settingsPanel));
            links.Add((settingsBack, MenuScreen.Pause, true));

            var over = Panel(rect, "GameOver");
            var o = over.transform;
            Label(o, "Eyebrow", "MATCH OVER", 30f, false, Theme.Accent, new Vector2(0f, 330f), new Vector2(900f, 44f));
            var resultTitle = Label(o, "Result", "You win!", 120f, true, Theme.Accent, new Vector2(0f, 230f), new Vector2(1200f, 150f));
            var resultScore = Label(o, "Score", "11 : 7", 90f, true, Color.white, new Vector2(0f, 100f), new Vector2(900f, 110f));
            var again = Button(o, "PlayAgain", "Play again", "ui_button_primary", new Vector2(0f, -45f), new Vector2(560f, 120f), 60f, Color.white);
            var change = Button(o, "ChangeDifficulty", "Change difficulty", "ui_button_secondary", new Vector2(0f, -185f), new Vector2(520f, 100f), 46f, Color.white);
            var main = Button(o, "MainMenu", "Main menu", "ui_button_tertiary", new Vector2(0f, -310f), new Vector2(320f, 72f), 34f, Color.white);
            panels.Add((MenuScreen.GameOver, over));

            var confirm = MenuSceneBuilder.BuildConfirm(rect);

            var switcher = controller.AddComponent<MenuScreenSwitcher>();
            MenuSceneBuilder.WriteScreenPanels(switcher, panels);
            MenuSceneBuilder.WriteLinks(switcher, links);

            var presenter = controller.AddComponent<PauseMenuPresenter>();
            Set(presenter, "_modes", modes);
            Set(presenter, "_switcher", switcher);
            Set(presenter, "_root", canvasObject);
            var placer = canvasObject.AddComponent<EyeHeightPlacer>();
            Set(placer, "_eyes", head);
            Set(placer, "_target", canvasObject.transform);
            SetF(placer, "_distanceInFront", 0.75f);
            SetF(placer, "_heightOffset", -0.05f);
            SetEnum(placer, "_facing", (int)PlacementFacing.SameAsViewDirection);
            Set(presenter, "_placer", placer);
            ViewHeightSetup.Wire(canvasObject.scene, modes, canvasObject, placer);
            Set(presenter, "_resume", resume);
            Set(presenter, "_restart", restart);
            Set(presenter, "_passthrough", passthrough);
            Set(presenter, "_passthroughState", passthrough.GetComponentInChildren<TMP_Text>(true));
            Set(presenter, "_quitToMenu", quit);
            Set(presenter, "_playAgain", again);
            Set(presenter, "_changeDifficulty", change);
            Set(presenter, "_mainMenu", main);
            Set(presenter, "_resultTitle", resultTitle);
            Set(presenter, "_resultScore", resultScore);
            Set(presenter, "_confirm", confirm);
            SetSeam(presenter, "_loader", loader);
            canvasObject.SetActive(false);
        }
    }
}

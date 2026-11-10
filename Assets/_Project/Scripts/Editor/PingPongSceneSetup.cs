using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using PingPong.Core;
using PingPong.Gameplay;
using PingPong.Platform.Meta;
using PingPong.Presentation;
using TMPro;
using static PingPong.EditorTools.SetupHelpers;

namespace PingPong.EditorTools
{
    /// <summary>
    /// Ping Pong > Set Up Game Scene. Safe to run again; it reuses what already exists.
    /// Prefabs first: everything a prefab owns is wired inside that prefab (so every copy gets it).
    /// Then the scene, which is the composition root: it places the prefabs and links them to each other
    /// and to the platform (rig, passthrough, input). Only those cross-links are scene overrides.
    /// </summary>
    public static class PingPongSceneSetup
    {
        const string ScenePath = "Assets/_Project/Scenes/GameScene.unity";
        const string ProfileFolder = "Assets/_Project/Settings/Gameplay";
        const string EnvironmentPrefabPath = "Assets/_Project/Prefabs/Scenes/GameScene/Environment.prefab";
        const string PaddleModelPrefabPath = "Assets/_Project/Prefabs/Equipment/PaddleModel.prefab";
        const string PaddlePrefabPath = "Assets/_Project/Prefabs/Equipment/Paddle.prefab";
        const string MatchPrefabPath = "Assets/_Project/Prefabs/Match.prefab";
        const string BallHitClipGuid = "c68f605b3a40145fb873f4210b4ecc23"; // the ball's existing hit sound

        [MenuItem("Ping Pong/Set Up Game Scene")]
        public static void Run()
        {
            // Opening GameScene replaces the open scene: let the user save (or cancel) first.
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var log = new System.Text.StringBuilder("Ping Pong setup:\n");

            // Tuning profiles (created once, then edited as assets).
            var ballPhysics = GetOrCreateProfile<BallPhysicsProfile>("BallPhysics");
            var matchRules = GetOrCreateProfile<MatchRulesProfile>("MatchRules");
            var paddleFeel = GetOrCreateProfile<PaddleFeelProfile>("PaddleFeel");
            var aiProfile = GetOrCreateProfile<AIProfile>("AIOpponent");
            log.AppendLine($"Profiles: {ProfileFolder}");

            // 1. Prefabs.
            EditPrefab(EnvironmentPrefabPath, root => SetUpTable(root, log));
            EditPrefab(PaddleModelPrefabPath, root => SetUpPaddleModel(root, log));
            EditPrefab(PaddlePrefabPath, root => SetUpPlayerPaddle(root, paddleFeel));
            EditPrefab(MatchPrefabPath, root => SetUpMatch(root, matchRules, ballPhysics, aiProfile));
            ModeMenuBuilder.Build();

            // 2. Scene.
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var table = Object.FindAnyObjectByType<PingPongTable>(FindObjectsInactive.Include);
            if (table == null) { Debug.LogError("Ping Pong setup: no table in the scene (place the Environment prefab)."); return; }
            var match = Instance(MatchPrefabPath);
            var paddle = Instance(PaddlePrefabPath);
            var rig = Object.FindAnyObjectByType<OVRCameraRig>(FindObjectsInactive.Include);
            if (rig == null) { Debug.LogError("Ping Pong setup: OVRCameraRig not found."); return; }

            // The local player is the Near side: flip the table frame if the rig stands at the other end.
            table.Rebuild();
            if (table.Frame.SideOf(rig.transform.position) != Side.Near)
            {
                SetBool(table, "_flipSides", !new SerializedObject(table).FindProperty("_flipSides").boolValue);
                table.Rebuild();
            }
            TableFrame frame = table.Frame;
            log.AppendLine($"Table: {Path(table.transform)}  {frame.HalfLength * 2:F2} x {frame.HalfWidth * 2:F2} m");

            // The ball plays on this table, and the rig (hands, body) never ends a rally.
            var ball = match.GetComponentInChildren<PingPongBall>(true);
            Set(ball, "_table", table);
            SetArray(ball, "_ignoredRoots", new Object[] { rig.transform });

            // The player's paddle strikes this match's ball.
            var hitter = paddle.GetComponent<PaddleHitter>();
            Set(hitter, "_match", match.GetComponent<MatchController>());
            Set(hitter, "_ball", ball);
            log.AppendLine($"Player paddle: {Path(paddle.transform)} (haptics: right controller)");

            // Platform services live outside the Match prefab, which stays free of Meta code.
            var platform = GetOrCreate("Platform");
            var passthrough = SetUpPassthrough(platform, rig, table, log);
            var commands = GetOrAdd<InputSystemPlayerCommands>(platform);
            var modes = match.GetComponent<GameModeController>();
            SetSeam(modes, "_commands", commands);

            // The paddle is grabbed with either controller.
            var grip = paddle.GetComponent<PaddleGrip>();
            SetSeam(grip, "_commands", commands);
            Set(grip, "_leftHand", rig.leftControllerAnchor);
            Set(grip, "_rightHand", rig.rightControllerAnchor);
            SetSeam(modes, "_passthrough", passthrough);

            // The mode menu opens in front of the head; Meta rays point at it through the event system.
            var menu = Instance(ModeMenuBuilder.PrefabPath).GetComponent<ModeMenuPresenter>();
            Set(menu, "_modes", modes);
            Set(menu, "_head", rig.centerEyeAnchor);
            var events = GetOrCreate("EventSystem");
            GetOrAdd<UnityEngine.EventSystems.EventSystem>(events);
            GetOrAdd<Oculus.Interaction.PointableCanvasModule>(events);
            log.AppendLine($"Mode menu: {Path(menu.transform)} (left menu button / Escape)");

            // Physics at headset rate (a project setting: logged when changed).
            if (!Mathf.Approximately(Time.fixedDeltaTime, 1f / 90f))
            {
                SetFixedTimestep(1f / 90f);
                log.AppendLine("Project setting: fixed timestep set to 1/90 s.");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log(log.ToString());
        }

        /// <summary>The table and its net (the model has no net collider), inside the Environment prefab.</summary>
        static void SetUpTable(GameObject root, System.Text.StringBuilder log)
        {
            BoxCollider top = root.GetComponentsInChildren<BoxCollider>(true)
                .Where(IsTableTop).OrderByDescending(b => b.bounds.size.x * b.bounds.size.z).FirstOrDefault();
            if (top == null) { Debug.LogError("Ping Pong setup: table top collider not found in the Environment prefab."); return; }
            var table = GetOrAdd<PingPongTable>(top.gameObject);
            Set(table, "_top", top);
            table.Rebuild();
            TableFrame frame = table.Frame;

            // The net sits with the table, as a sibling of the top collider.
            Transform parent = top.transform.parent != null ? top.transform.parent : top.transform;
            var net = GetOrCreate("Net", parent).transform;
            net.SetPositionAndRotation(frame.SurfaceCenter + Vector3.up * (frame.NetHeight * 0.5f), Quaternion.LookRotation(frame.Forward, Vector3.up));
            Vector3 worldSize = new Vector3(frame.HalfWidth * 2f + 0.3f, frame.NetHeight, 0.015f);
            Vector3 s = net.lossyScale;
            var netCol = GetOrAdd<BoxCollider>(net.gameObject);
            netCol.size = new Vector3(worldSize.x / s.x, worldSize.y / s.y, worldSize.z / s.z);
            Set(table, "_net", netCol);
            log.AppendLine($"Net: {Path(net)}");
        }

        /// <summary>
        /// Measures the blade once, from the rubber (or largest) mesh: Blade forward = face normal. After that the
        /// serialized values are kept, so renaming or swapping meshes can't silently change them; clear the
        /// model's Blade to measure again.
        /// </summary>
        static void SetUpPaddleModel(GameObject root, System.Text.StringBuilder log)
        {
            var model = GetOrAdd<PaddleModel>(root);
            if (GetRef(model, "_blade") != null)
            {
                MeasureHandle(model, root);
                log.AppendLine($"Paddle model: blade kept (radius {model.BladeRadius:F3} m)");
                return;
            }
            var filters = root.GetComponentsInChildren<MeshFilter>(true).Where(f => f.sharedMesh != null).ToArray();
            if (filters.Length == 0) { Debug.LogError("Ping Pong setup: the PaddleModel prefab has no mesh to measure."); return; }
            MeshFilter mf = filters.FirstOrDefault(f => f.name.ToLower().Contains("rubber"))
                            ?? filters.OrderByDescending(f => f.sharedMesh.bounds.size.sqrMagnitude).First();
            Bounds b = mf.sharedMesh.bounds;
            Vector3 ls = mf.transform.lossyScale;
            Vector3 world = new Vector3(b.size.x * Mathf.Abs(ls.x), b.size.y * Mathf.Abs(ls.y), b.size.z * Mathf.Abs(ls.z));
            int thin = world.x <= world.y && world.x <= world.z ? 0 : world.y <= world.z ? 1 : 2;
            Vector3 normal = thin == 0 ? Vector3.right : thin == 1 ? Vector3.up : Vector3.forward;
            Vector3 upAxis = thin == 1 ? Vector3.forward : Vector3.up;
            float a = world[(thin + 1) % 3], c = world[(thin + 2) % 3];
            float radius = Mathf.Clamp(Mathf.Min(a, c) * 0.5f, 0.05f, 0.11f);
            float halfThick = Mathf.Clamp(world[thin] * 0.5f, 0.004f, 0.02f);

            var blade = new GameObject("Blade").transform;
            blade.SetParent(mf.transform, false);
            blade.localPosition = b.center;
            blade.localRotation = Quaternion.LookRotation(normal, upAxis);
            blade.localScale = new Vector3(1f / Mathf.Abs(ls.x), 1f / Mathf.Abs(ls.y), 1f / Mathf.Abs(ls.z));

            Set(model, "_blade", blade);
            SetV(model, "_faceNormalLocal", Vector3.forward);
            SetF(model, "_bladeRadius", radius);
            SetF(model, "_bladeHalfThickness", halfThick);
            MeasureHandle(model, root);
            log.AppendLine($"Paddle model: blade radius {radius:F3} m, half-thickness {halfThick:F3} m");
        }

        /// <summary>
        /// Handle-to-blade direction, once: the mesh point farthest from the blade centre within the blade's
        /// plane is the handle's end, so the blade lies the opposite way. Lets code hold the paddle upright.
        /// </summary>
        static void MeasureHandle(PaddleModel model, GameObject root)
        {
            if (new SerializedObject(model).FindProperty("_handleToBladeLocal").vector3Value.sqrMagnitude > 1e-6f) return;
            Transform blade = model.Blade;
            Vector3 centre = blade.position, normal = blade.TransformDirection(model.FaceNormalLocal);
            Vector3 farthest = Vector3.zero;
            foreach (var f in root.GetComponentsInChildren<MeshFilter>(true))
            {
                if (f.sharedMesh == null) continue; // the editor can read any mesh
                foreach (Vector3 v in f.sharedMesh.vertices)
                {
                    Vector3 d = f.transform.TransformPoint(v) - centre;
                    d -= Vector3.Dot(d, normal) * normal;
                    if (d.sqrMagnitude > farthest.sqrMagnitude) farthest = d;
                }
            }
            if (farthest.sqrMagnitude < 1e-6f) { Debug.LogWarning("Ping Pong setup: couldn't find the paddle's handle; assuming blade-up is +Y."); return; }
            SetV(model, "_handleToBladeLocal", blade.InverseTransformDirection(-farthest).normalized);
        }

        /// <summary>The player's paddle (PaddleModel variant with grabbing): hit detection and controller haptics.</summary>
        static void SetUpPlayerPaddle(GameObject root, PaddleFeelProfile feel)
        {
            var hitter = GetOrAdd<PaddleHitter>(root);
            SetEnum(hitter, "_player", (int)Side.Near);
            Set(hitter, "_feel", feel);
            Set(hitter, "_model", root.GetComponent<PaddleModel>());
            // Controllers: toggle grab with a snapped grip (PaddleGrip). Hand tracking keeps Meta's free grab
            // (HandGrabInteractable), so only the controller part of Meta's grab (GrabInteractable) goes.
            foreach (var controllerGrab in root.GetComponentsInChildren<MonoBehaviour>(true)
                         .Where(c => c != null && c.GetType().Name == "GrabInteractable").ToArray())
                Object.DestroyImmediate(controllerGrab);
            var grip = GetOrAdd<PaddleGrip>(root);
            Set(grip, "_model", root.GetComponent<PaddleModel>());

            var controller = GetOrAdd<OvrControllerHaptics>(root);
            Set(controller, "_grip", grip);
            var haptics = GetOrAdd<PaddleHaptics>(root);
            Set(haptics, "_hitter", hitter);
            SetSeam(haptics, "_haptics", controller);
        }

        /// <summary>Rules, ball, AI opponent and its paddle, modes and scoreboard: everything inside Match.prefab.</summary>
        static void SetUpMatch(GameObject root, MatchRulesProfile rules, BallPhysicsProfile physics, AIProfile aiProfile)
        {
            var legacyNet = root.transform.Find("Net"); // the net now lives with the table
            if (legacyNet != null) Object.DestroyImmediate(legacyNet.gameObject);

            var ball = root.GetComponentInChildren<PingPongBall>(true);
            Set(ball, "_physics", physics);
            var src = GetOrAdd<AudioSource>(ball.gameObject);
            src.playOnAwake = false;
            src.spatialBlend = 1f;
            var impactAudio = GetOrAdd<BallImpactAudio>(ball.gameObject);
            Set(impactAudio, "_ball", ball);
            var hitClip = AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(BallHitClipGuid));
            if (hitClip != null && GetRef(impactAudio, "_table") == null) Set(impactAudio, "_table", hitClip);

            var matchCtl = GetOrAdd<MatchController>(root);
            Set(matchCtl, "_ball", ball);
            Set(matchCtl, "_profile", rules);

            // The AI's paddle: the shared PaddleModel, without grabbing or physics.
            var aiPaddle = root.GetComponentsInChildren<PaddleModel>(true).FirstOrDefault();
            if (aiPaddle == null)
            {
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(PaddleModelPrefabPath);
                aiPaddle = ((GameObject)PrefabUtility.InstantiatePrefab(model, root.transform)).GetComponent<PaddleModel>();
                aiPaddle.name = "AIPaddle";
            }
            var ai = GetOrAdd<AIOpponent>(root);
            Set(ai, "_match", matchCtl);
            Set(ai, "_model", aiPaddle);
            SetEnum(ai, "_player", (int)Side.Far);
            Set(ai, "_profile", aiProfile);

            var modes = GetOrAdd<GameModeController>(root);
            Set(modes, "_match", matchCtl);
            Set(modes, "_ai", ai);

            var board = GetOrCreate("Scoreboard", root.transform);
            var presenter = GetOrAdd<ScoreboardPresenter>(board);
            var tmp = GetOrAdd<TextMeshPro>(GetOrCreate("ScoreText", board.transform));
            tmp.fontSize = 0.8f; // ~8 cm digits
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.rectTransform.sizeDelta = new Vector2(1.6f, 0.5f);
            Set(presenter, "_text", tmp);
            Set(presenter, "_match", matchCtl);
            Set(presenter, "_modes", modes);
        }

        /// <summary>Meta passthrough on the Platform object: the layer on the rig, the room hidden while it's on.</summary>
        static MetaPassthrough SetUpPassthrough(GameObject platform, OVRCameraRig rig, PingPongTable table, System.Text.StringBuilder log)
        {
            OVRProjectConfig cfg = OVRProjectConfig.CachedProjectConfig;
            if (cfg.insightPassthroughSupport != OVRProjectConfig.FeatureSupport.Supported)
            {
                cfg.insightPassthroughSupport = OVRProjectConfig.FeatureSupport.Supported;
                OVRProjectConfig.CommitProjectConfig(cfg);
                log.AppendLine("Project config: Insight Passthrough set to Supported.");
            }
            var mgr = rig.GetComponent<OVRManager>();
            if (mgr == null) mgr = Object.FindAnyObjectByType<OVRManager>(FindObjectsInactive.Include);
            if (mgr != null && !mgr.isInsightPassthroughEnabled)
            {
                mgr.isInsightPassthroughEnabled = true;
                EditorUtility.SetDirty(mgr);
                log.AppendLine("OVRManager: Insight Passthrough enabled.");
            }
            var layer = GetOrAdd<OVRPassthroughLayer>(rig.gameObject);
#pragma warning disable CS0618 // overlayType is deprecated with flexible layering; Underlay is still the setting we need
            layer.overlayType = OVROverlay.OverlayType.Underlay;
#pragma warning restore CS0618
            layer.hidden = true;
            EditorUtility.SetDirty(layer);

            var pt = GetOrAdd<MetaPassthrough>(platform);
            Set(pt, "_layer", layer);
            var cam = rig.centerEyeAnchor != null ? rig.centerEyeAnchor.GetComponent<Camera>() : null;
            SetArray(pt, "_cameras", cam != null ? new Object[] { cam } : new Object[0]);
            var hide = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(t => t.name.StartsWith("experimental_neon_room") || t.name == "Floor" || t.name == "Visuals")
                .Where(t => !table.transform.IsChildOf(t))
                .OrderBy(HierarchyOrder, System.StringComparer.Ordinal) // stable: re-runs don't reorder the list
                .Select(t => (Object)t.gameObject).ToArray();
            SetArray(pt, "_hideInPassthrough", hide);
            log.AppendLine($"Passthrough: layer on {rig.name}, hides {hide.Length} object(s)" + (mgr == null ? " (no OVRManager found!)" : ""));
            return pt;
        }

        /// <summary>The scene's instance of a prefab (found by asset, never by name), placed if missing.</summary>
        static GameObject Instance(string prefabPath)
        {
            var existing = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Select(t => t.gameObject)
                .FirstOrDefault(g => PrefabUtility.IsOutermostPrefabInstanceRoot(g)
                    && PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(g) == prefabPath);
            if (existing != null) return existing;
            return (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath));
        }

        static T GetOrCreateProfile<T>(string fileName) where T : ScriptableObject
        {
            string path = $"{ProfileFolder}/{fileName}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) return existing;
            EnsureFolder(ProfileFolder);
            var asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            return asset;
        }

        static bool IsTableTop(BoxCollider b)
        {
            Vector3 s = b.bounds.size;
            float l = Mathf.Max(s.x, s.z), w = Mathf.Min(s.x, s.z);
            // Size only: the floor height differs per scene, so absolute height isn't reliable.
            return l > 2f && l < 3.2f && w > 1.2f && w < 1.9f && s.y < 0.15f;
        }

        /// <summary>Time.fixedDeltaTime alone isn't saved; write it to ProjectSettings/TimeManager.asset.</summary>
        static void SetFixedTimestep(float dt)
        {
            Time.fixedDeltaTime = dt;
            EditorUtility.SetDirty(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TimeManager.asset")[0]);
            AssetDatabase.SaveAssets();
        }

        /// <summary>Sort key in hierarchy order (sibling indices from the scene root down).</summary>
        static string HierarchyOrder(Transform t) =>
            (t.parent == null ? "" : HierarchyOrder(t.parent) + "/") + t.GetSiblingIndex().ToString("D5");

        static string Path(Transform t) => t.parent == null ? t.name : Path(t.parent) + "/" + t.name;
    }
}

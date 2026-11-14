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
            SetUp(() => EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single));
        }

        /// <summary>
        /// Adds the game to the open scene: it needs an OVRCameraRig (e.g. Meta's Camera Rig building block with
        /// the interaction rig); the table, match, paddle and menu are placed if missing.
        /// </summary>
        [MenuItem("Ping Pong/Set Up Open Scene")]
        public static void RunOnOpenScene() => SetUp(() => UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        static void SetUp(System.Func<UnityEngine.SceneManagement.Scene> openScene)
        {
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
            var scene = openScene();
            var rig = Object.FindAnyObjectByType<OVRCameraRig>(FindObjectsInactive.Include);
            if (rig == null) { Debug.LogError("Ping Pong setup: OVRCameraRig not found (add Meta's Camera Rig building block)."); return; }
            var table = Object.FindAnyObjectByType<PingPongTable>(FindObjectsInactive.Include);
            if (table == null)
            {
                var environment = Instance(EnvironmentPrefabPath);
                table = environment.GetComponentInChildren<PingPongTable>(true);
                PlaceInFrontOf(rig, environment, table);
                log.AppendLine("Environment placed in front of the player");
            }
            var match = Instance(MatchPrefabPath);
            var paddle = Instance(PaddlePrefabPath);

            // The local player is the Near side: flip the table frame if the rig stands at the other end.
            table.Rebuild();
            if (table.Frame.SideOf(rig.transform.position) != Side.Near)
            {
                SetBool(table, "_flipSides", !new SerializedObject(table).FindProperty("_flipSides").boolValue);
                table.Rebuild();
            }
            TableFrame frame = table.Frame;
            log.AppendLine("Scoreboards and banner are placed once from the table: re-run this after moving the Environment.");
            log.AppendLine($"Table: {Path(table.transform)}  {frame.HalfLength * 2:F2} x {frame.HalfWidth * 2:F2} m");

            // The ball plays on this table, and the rig (hands, body) never ends a rally.
            var ball = match.GetComponentInChildren<PingPongBall>(true);
            Set(ball, "_table", table);
            SetArray(ball, "_ignoredRoots", new Object[] { rig.transform });

            // Static structures: placed once here and saved in the scene, with no runtime positioning.
            PlaceBoards(match, frame);

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
            Set(grip, "_modes", modes); // a starting match puts the paddle in the right hand
            SetSeam(modes, "_passthrough", passthrough);

            // The mode menu opens in front of the head; Meta rays point at it through the event system.
            var menu = Instance(ModeMenuBuilder.PrefabPath).GetComponent<ModeMenuPresenter>();
            Set(menu, "_modes", modes);
            Set(menu, "_head", rig.centerEyeAnchor);
            // Reuse whichever event system the scene has (Meta's wizards add their own), else make one.
            var existing = Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>(FindObjectsInactive.Include);
            var events = existing != null ? existing.gameObject : GetOrCreate("EventSystem");
            GetOrAdd<UnityEngine.EventSystems.EventSystem>(events);
            GetOrAdd<Oculus.Interaction.PointableCanvasModule>(events);
            log.AppendLine($"Mode menu: {Path(menu.transform)} (left menu button / Escape)");

            // Physics at headset rate (a project setting: logged when changed).
            if (!Mathf.Approximately(Time.fixedDeltaTime, 1f / 90f))
            {
                SetFixedTimestep(1f / 90f);
                log.AppendLine("Project setting: fixed timestep set to 1/90 s.");
            }

            PackIntoWorld(rig, platform, menu, log);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log(log.ToString());
        }

        /// <summary>
        /// Keeps the scene thin: everything the game needs lives in one World prefab, in three levels (World, its groups,
        /// then the prefabs and objects inside). First run groups the scene's objects into it; every run then applies the
        /// scene's changes to the prefab, so the scene file itself only changes when the scene does.
        /// </summary>
        static void PackIntoWorld(OVRCameraRig rig, GameObject platform, ModeMenuPresenter menu, System.Text.StringBuilder log)
        {
            GameObject world = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Select(t => t.gameObject)
                .FirstOrDefault(g => PrefabUtility.IsOutermostPrefabInstanceRoot(g) && PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(g) == WorldPrefabPath);
            if (world == null)
            {
                world = new GameObject("World");
                Transform Group(string name, params Transform[] members)
                {
                    var group = new GameObject(name).transform;
                    group.SetParent(world.transform, false);
                    foreach (Transform m in members.Where(m => m != null).Distinct()) m.SetParent(group, true);
                    return group;
                }
                var eventSystem = Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>(FindObjectsInactive.Include);
                var layer = Object.FindAnyObjectByType<OVRPassthroughLayer>(FindObjectsInactive.Include);
                Transform RootOf(Component c) => c != null ? c.transform.root : null;
                Transform paddleRoot = RootOf(Object.FindAnyObjectByType<PaddleGrip>(FindObjectsInactive.Include));
                Transform matchRoot = RootOf(Object.FindAnyObjectByType<MatchController>(FindObjectsInactive.Include));
                Transform environmentRoot = RootOf(Object.FindAnyObjectByType<PingPongTable>(FindObjectsInactive.Include));
                // Prefab roots and the Platform object sit directly under World; the loose Meta blocks are grouped.
                environmentRoot.SetParent(world.transform, true);
                matchRoot.SetParent(world.transform, true);
                Group("Player", rig.transform.root, RootOf(layer), paddleRoot);
                Group("UI", menu.transform.root, RootOf(eventSystem));
                platform.transform.SetParent(world.transform, true);
                PrefabUtility.SaveAsPrefabAssetAndConnect(world, WorldPrefabPath, InteractionMode.AutomatedAction);
                log.AppendLine($"World prefab created: {WorldPrefabPath}");
            }
            else PrefabUtility.ApplyPrefabInstance(world, InteractionMode.AutomatedAction);
        }

        /// <summary>
        /// Puts a newly placed environment where the player stands: the table top at regulation height above
        /// the rig's floor (floor-level tracking puts the floor at the rig's origin) and its near end just in
        /// front of the player, running away from them.
        /// </summary>
        static void PlaceInFrontOf(OVRCameraRig rig, GameObject environment, PingPongTable table)
        {
            const float GapToTable = 0.45f;     // from the player to the near end line, metres
            const float SurfaceHeight = 0.76f;  // regulation table height
            table.Rebuild();
            Vector3 rigForward = Vector3.ProjectOnPlane(rig.transform.forward, Vector3.up).normalized;
            Vector3 tableForward = Vector3.ProjectOnPlane(table.Frame.Forward, Vector3.up).normalized;
            Transform env = environment.transform;
            // Yaw only: FromToRotation picks an arbitrary axis for opposite vectors and could tip the table.
            env.rotation = Quaternion.AngleAxis(Vector3.SignedAngle(tableForward, rigForward, Vector3.up), Vector3.up) * env.rotation;
            table.Rebuild();

            Vector3 wantedCentre = rig.transform.position + rigForward * (table.Frame.HalfLength + GapToTable)
                                   + Vector3.up * SurfaceHeight;
            Vector3 move = wantedCentre - table.Frame.SurfaceCenter;
            env.position += move;
            table.Rebuild();
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

            // The old board above the net is gone: the two scoreboards show the score.
            Transform netBoard = root.transform.Find("Scoreboard");
            if (netBoard != null) Object.DestroyImmediate(netBoard.gameObject);

            // One floating board behind each end of the table, both showing both scores.
            foreach (string old in new[] { "PlayerScoreboard", "AIScoreboard" }) // earlier names
            {
                Transform legacy = root.transform.Find(old);
                if (legacy != null) Object.DestroyImmediate(legacy.gameObject);
            }
            SetUpBoard(root, BoardBehindPlayer, matchCtl, modes);
            SetUpBoard(root, BoardBehindAI, matchCtl, modes);
            SetUpBanner(root, matchCtl, modes);
        }

        const string ScoreboardPrefabPath = "Assets/_Project/Prefabs/UI/Scoreboard.prefab";
        const string WorldPrefabPath = "Assets/_Project/Prefabs/World.prefab";
        const string BannerName = "MatchBanner";
        const string BoardBehindPlayer = "ScoreboardBehindPlayer", BoardBehindAI = "ScoreboardBehindAI";

        /// <summary>
        /// Puts each scoreboard on the floor behind its end of the table, facing the table with its board tilted
        /// down, and the banner above the far end. They are placed once and saved in the scene, so after moving the
        /// Environment run Set Up Open Scene again.
        /// </summary>
        static void PlaceBoards(GameObject match, TableFrame t)
        {
            const float TableHeight = 0.76f, BehindTable = 2f;
            Transform banner = match.transform.Find(BannerName);
            if (banner != null) // above the far end line, facing the player
            {
                banner.SetPositionAndRotation(t.SurfaceCenter + t.Forward * Side.Far.Sign() * t.HalfLength + Vector3.up * 1.7f, Quaternion.LookRotation(t.Forward * Side.Far.Sign(), Vector3.up));
                EditorUtility.SetDirty(banner);
            }
            foreach (var (name, side) in new[] { (BoardBehindPlayer, Side.Near), (BoardBehindAI, Side.Far) })
            {
                Transform board = match.transform.Find(name);
                if (board == null) continue;
                Vector3 outward = t.Forward * side.Sign();
                board.SetPositionAndRotation(t.SurfaceCenter + outward * (t.HalfLength + BehindTable) - Vector3.up * TableHeight, Quaternion.LookRotation(outward, Vector3.up));
                EditorUtility.SetDirty(board);
            }
        }

        static void SetUpBoard(GameObject root, string name, MatchController match, GameModeController modes)
        {
            var prefab = EnsureScoreboardPrefab();
            Transform existing = root.transform.Find(name);
            if (existing != null && !PrefabUtility.IsAnyPrefabInstanceRoot(existing.gameObject))
            {
                Object.DestroyImmediate(existing.gameObject); // earlier layouts built the board inline
                existing = null;
            }
            var board = existing != null ? existing.gameObject : (GameObject)PrefabUtility.InstantiatePrefab(prefab, root.transform);
            board.name = name;
            var sb = board.GetComponent<MatchScoreboard>();
            Set(sb, "_match", match);
            Set(sb, "_modes", modes);
        }

        /// <summary>The one scoreboard prefab, used for both ends: built or refreshed here, so a change is made in one place.</summary>
        static GameObject EnsureScoreboardPrefab()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(ScoreboardPrefabPath) == null)
            {
                var temp = new GameObject("Scoreboard");
                try { BuildScoreboard(temp); PrefabUtility.SaveAsPrefabAsset(temp, ScoreboardPrefabPath); }
                finally { Object.DestroyImmediate(temp); }
            }
            else EditPrefab(ScoreboardPrefabPath, BuildScoreboard);
            return AssetDatabase.LoadAssetAtPath<GameObject>(ScoreboardPrefabPath);
        }

        /// <summary>
        /// A scoreboard like a tournament venue's, floating near the ceiling: a dark board with the local player on the
        /// left (blue) and the opponent on the right (red), each with a name, a big score and a SERVE marker.
        /// The root sits on the floor, under the board; the board is tilted down towards the players.
        /// </summary>
        static void BuildScoreboard(GameObject rootGo)
        {
            const float W = 2.4f, H = 1.2f, Centre = 3.8f, HeaderH = 0.34f, Tilt = 8f;
            Material panelMat = BoardMaterials.Panel, playerMat = BoardMaterials.Player, aiMat = BoardMaterials.AI, lineMat = BoardMaterials.Line;

            var sb = GetOrAdd<MatchScoreboard>(rootGo);
            var board = GetOrCreate("Board", rootGo.transform).transform;
            board.localPosition = new Vector3(0f, Centre, 0f);
            board.localRotation = Quaternion.Euler(-Tilt, 0f, 0f);

            float half = W * 0.25f; // centre of each half
            Shape("Panel", board, PrimitiveType.Quad, new Vector3(0f, 0f, 0.002f), new Vector3(W, H, 1f), panelMat);
            Shape("HeaderPlayer", board, PrimitiveType.Quad, new Vector3(-half, H * 0.5f - HeaderH * 0.5f, 0f), new Vector3(W * 0.5f - 0.02f, HeaderH, 1f), playerMat);
            Shape("HeaderAI", board, PrimitiveType.Quad, new Vector3(half, H * 0.5f - HeaderH * 0.5f, 0f), new Vector3(W * 0.5f - 0.02f, HeaderH, 1f), aiMat);
            Shape("Divider", board, PrimitiveType.Quad, new Vector3(0f, 0f, 0f), new Vector3(0.02f, H, 1f), lineMat);
            Shape("Frame", board, PrimitiveType.Quad, new Vector3(0f, 0f, 0.004f), new Vector3(W + 0.08f, H + 0.08f, 1f), lineMat);

            float nameY = H * 0.5f - HeaderH * 0.5f, scoreY = -0.08f, serveY = -H * 0.5f + 0.14f;
            Vector2 nameBox = new Vector2(W * 0.5f - 0.1f, 0.3f), scoreBox = new Vector2(W * 0.5f - 0.1f, 0.8f), serveBox = new Vector2(W * 0.5f - 0.1f, 0.22f);
            TextMeshPro localName = BoardText("PlayerName", board, new Vector3(-half, nameY, -0.01f), nameBox, 2.2f, FontStyles.Bold);
            TextMeshPro localScore = BoardText("PlayerScore", board, new Vector3(-half, scoreY, -0.01f), scoreBox, 9f, FontStyles.Bold);
            TextMeshPro localServe = BoardText("PlayerServe", board, new Vector3(-half, serveY, -0.01f), serveBox, 1.5f, FontStyles.Bold);
            TextMeshPro oppName = BoardText("AIName", board, new Vector3(half, nameY, -0.01f), nameBox, 2.2f, FontStyles.Bold);
            TextMeshPro oppScore = BoardText("AIScore", board, new Vector3(half, scoreY, -0.01f), scoreBox, 9f, FontStyles.Bold);
            TextMeshPro oppServe = BoardText("AIServe", board, new Vector3(half, serveY, -0.01f), serveBox, 1.5f, FontStyles.Bold);
            localServe.text = oppServe.text = MatchText.ServeMarker;
            localServe.color = new Color(0.4f, 0.75f, 1f);
            oppServe.color = new Color(1f, 0.5f, 0.45f);
            // Placeholders so the editor shows the board as it will look; the scoreboard overwrites them at runtime.
            localName.text = MatchText.BoardName(Side.Near, Side.Near, GameMode.Solo);
            oppName.text = MatchText.BoardName(Side.Far, Side.Near, GameMode.Solo);
            localScore.text = oppScore.text = "0";

            Set(sb, "_localName", localName);
            Set(sb, "_localScore", localScore);
            Set(sb, "_localServe", localServe);
            Set(sb, "_opponentName", oppName);
            Set(sb, "_opponentScore", oppScore);
            Set(sb, "_opponentServe", oppServe);
        }

        /// <summary>The banner that pops in for points, lets and the game result; a static object above the far end of the table.</summary>
        static void SetUpBanner(GameObject root, MatchController match, GameModeController modes)
        {
            Material panelMat = BoardMaterials.Panel, frameMat = BoardMaterials.Line;
            var rootGo = GetOrCreate(BannerName, root.transform);
            var banner = GetOrAdd<MatchBanner>(rootGo);
            var visual = GetOrCreate("Visual", rootGo.transform).transform;
            Shape("Panel", visual, PrimitiveType.Quad, new Vector3(0f, 0f, 0.002f), new Vector3(2.4f, 0.55f, 1f), panelMat);
            Shape("Frame", visual, PrimitiveType.Quad, new Vector3(0f, 0f, 0.004f), new Vector3(2.46f, 0.61f, 1f), frameMat);
            TextMeshPro text = BoardText("Text", visual, new Vector3(0f, 0f, -0.01f), new Vector2(2.3f, 0.45f), 2.4f, FontStyles.Bold);
            text.text = "You score - double bounce"; // placeholder for the editor
            Set(banner, "_match", match);
            Set(banner, "_modes", modes);
            Set(banner, "_visual", visual);
            Set(banner, "_text", text);
        }

        /// <summary>The scoreboard and banner materials: one place for the paths and colours.</summary>
        static class BoardMaterials
        {
            const string Folder = "Assets/_Project/Materials/Scoreboard";
            public static Material Panel => Get("ScoreboardPanel", new Color(0.04f, 0.05f, 0.08f));
            public static Material Player => Get("ScoreboardPlayer", new Color(0.15f, 0.55f, 1f));
            public static Material AI => Get("ScoreboardAI", new Color(1f, 0.3f, 0.25f));
            public static Material Line => Get("ScoreboardDivider", new Color(0.8f, 0.82f, 0.9f));

            static Material Get(string name, Color color)
            {
                if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/_Project/Materials", "Scoreboard");
                return UnlitMaterial($"{Folder}/{name}.mat", color);
            }
        }

        static Material UnlitMaterial(string path, Color color)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetColor("_BaseColor", color);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static void Shape(string name, Transform parent, PrimitiveType shape, Vector3 localPos, Vector3 scale, Material mat)
        {
            var go = GetOrCreate(name, parent);
            var mf = GetOrAdd<MeshFilter>(go);
            mf.sharedMesh = Resources.GetBuiltinResource<Mesh>(shape == PrimitiveType.Quad ? "Quad.fbx" : "Cube.fbx");
            var mr = GetOrAdd<MeshRenderer>(go);
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.identity; // a quad and TextMeshPro both face -Z, the board's readable side
            go.transform.localScale = scale;
        }

        static TextMeshPro BoardText(string name, Transform parent, Vector3 localPos, Vector2 size, float fontSize, FontStyles style)
        {
            var tmp = GetOrAdd<TextMeshPro>(GetOrCreate(name, parent));
            tmp.transform.localPosition = localPos;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.color = Color.white;
            tmp.rectTransform.sizeDelta = size;
            return tmp;
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
            // Reuse a passthrough layer the scene already has (e.g. Meta's Passthrough building block).
            var layer = Object.FindAnyObjectByType<OVRPassthroughLayer>(FindObjectsInactive.Include)
                        ?? GetOrAdd<OVRPassthroughLayer>(rig.gameObject);
            // Passthrough must be an underlay. Newer Meta SDKs force it and no longer have the field; older ones need it set.
            var layerObject = new SerializedObject(layer);
            SerializedProperty overlayType = layerObject.FindProperty("overlayType");
            if (overlayType != null && overlayType.propertyType == SerializedPropertyType.Enum)
            {
                overlayType.enumValueIndex = (int)OVROverlay.OverlayType.Underlay;
                layerObject.ApplyModifiedPropertiesWithoutUndo();
            }
            layer.hidden = true;
            EditorUtility.SetDirty(layer);

            var pt = GetOrAdd<MetaPassthrough>(platform);
            Set(pt, "_layer", layer);
            var cam = rig.centerEyeAnchor != null ? rig.centerEyeAnchor.GetComponent<Camera>() : null;
            SetArray(pt, "_cameras", cam != null ? new Object[] { cam } : new Object[0]);
            var hide = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(t => t.name.StartsWith("experimental_neon_room") || t.name == "Floor" || t.name == "Visuals")
                .Where(t => !table.transform.IsChildOf(t))
                .Where(t => !t.IsChildOf(rig.transform)) // the rig's own visuals (controllers, rays) stay visible
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
                .FirstOrDefault(g => PrefabUtility.IsAnyPrefabInstanceRoot(g)
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

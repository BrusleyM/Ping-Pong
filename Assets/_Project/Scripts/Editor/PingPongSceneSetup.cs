using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using PingPong.Core;
using PingPong.Gameplay;
using PingPong.Platform.Meta;

namespace PingPong.EditorTools
{
    /// <summary>
    /// Ping Pong > Set Up Game Scene: wires the gameplay into GameScene. Safe to run again;
    /// it reuses what already exists.
    /// </summary>
    public static class PingPongSceneSetup
    {
        const string ScenePath = "Assets/_Project/Scenes/GameScene.unity";

        const string ProfileFolder = "Assets/_Project/Settings/Gameplay";
        const string PaddlePrefabPath = "Assets/_Project/Prefabs/Equipment/Paddle.prefab";
        const string BallHitClipGuid = "c68f605b3a40145fb873f4210b4ecc23"; // the ball's existing hit sound

        [MenuItem("Ping Pong/Set Up Game Scene")]
        public static void Run()
        {
            // Opening GameScene replaces the open scene: let the user save (or cancel) first.
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var log = new System.Text.StringBuilder("Ping Pong setup:\n");

            // Tuning profiles (created once, then edited as assets).
            var ballPhysics = GetOrCreateProfile<BallPhysicsProfile>("BallPhysics");
            var matchRules = GetOrCreateProfile<MatchRulesProfile>("MatchRules");
            var paddleFeel = GetOrCreateProfile<PaddleFeelProfile>("PaddleFeel");
            var aiProfile = GetOrCreateProfile<AIProfile>("AIOpponent");
            log.AppendLine($"Profiles: {ProfileFolder}");

            // Table: the flat box collider of table size.
            BoxCollider top = Object.FindObjectsByType<BoxCollider>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(IsTableTop).OrderByDescending(b => b.bounds.size.x * b.bounds.size.z).FirstOrDefault();
            if (top == null) { Debug.LogError("Ping Pong setup: table top collider not found."); return; }
            var table = GetOrAdd<PingPongTable>(top.gameObject);
            Set(table, "_top", top);
            table.Rebuild();
            TableFrame frame = table.Frame;
            log.AppendLine($"Table: {Path(top.transform)}  {frame.HalfLength * 2:F2} x {frame.HalfWidth * 2:F2} m");

            var match = GetOrCreate("Match").transform;

            // Net collider (the model has none).
            var net = GetOrCreate("Net", match);
            net.transform.SetPositionAndRotation(frame.SurfaceCenter + Vector3.up * (frame.NetHeight * 0.5f),
                Quaternion.LookRotation(frame.Forward, Vector3.up));
            var netCol = GetOrAdd<BoxCollider>(net);
            netCol.size = new Vector3(frame.HalfWidth * 2f + 0.3f, frame.NetHeight, 0.015f);
            Set(table, "_net", netCol);

            // The local player is the Near side: flip the table frame if the rig stands at the other end.
            var rig = Object.FindAnyObjectByType<OVRCameraRig>(FindObjectsInactive.Include);
            if (rig == null) { Debug.LogError("Ping Pong setup: OVRCameraRig not found."); return; }
            if (frame.SideOf(rig.transform.position) != Side.Near)
            {
                var so = new SerializedObject(table);
                so.FindProperty("_flipSides").boolValue = !so.FindProperty("_flipSides").boolValue;
                so.ApplyModifiedPropertiesWithoutUndo();
                table.Rebuild();
                frame = table.Frame;
            }

            // Ball: physics profile, impact audio, and the rig never ends a rally.
            var ball = Object.FindAnyObjectByType<PingPongBall>(FindObjectsInactive.Include);
            if (ball == null) { Debug.LogError("Ping Pong setup: ball not found."); return; }
            var src = GetOrAdd<AudioSource>(ball.gameObject);
            src.playOnAwake = false; src.spatialBlend = 1f;
            var impactAudio = GetOrAdd<AudioSourceImpactAudio>(ball.gameObject);
            var hitClip = AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(BallHitClipGuid));
            if (hitClip != null && GetRef(impactAudio, "_table") == null) Set(impactAudio, "_table", hitClip);
            Set(ball, "_physics", ballPhysics);
            // Also on the ball prefab, so every copy of it has physics (not just this scene's override).
            var ballPrefab = PrefabUtility.GetCorrespondingObjectFromSource(ball);
            if (ballPrefab != null && GetRef(ballPrefab, "_physics") == null)
            {
                Set(ballPrefab, "_physics", ballPhysics);
                PrefabUtility.SavePrefabAsset(ballPrefab.transform.root.gameObject);
            }
            SetSeam(ball, "_audio", impactAudio);
            SetArray(ball, "_ignoredRoots", new Object[] { rig.transform });

            // Match rules.
            var matchCtl = GetOrAdd<MatchController>(match.gameObject);
            Set(matchCtl, "_table", table);
            Set(matchCtl, "_ball", ball);
            Set(matchCtl, "_profile", matchRules);

            // Player paddle, with haptics on the controller that holds it.
            // Found by component or prefab, never by name, so renames can't break the setup.
            var paddlePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PaddlePrefabPath);
            var paddle = Object.FindObjectsByType<PaddleHitter>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .Select(h => h.transform).FirstOrDefault(t => !t.IsChildOf(match))
                ?? Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .FirstOrDefault(t => !t.IsChildOf(match) && PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject)
                        && PrefabUtility.GetCorrespondingObjectFromOriginalSource(t.gameObject) == paddlePrefab);
            if (paddle == null) { Debug.LogError("Ping Pong setup: player paddle not found."); return; }
            var blade = MakeBlade(paddle, out float radius, out float halfThick);
            var hitter = GetOrAdd<PaddleHitter>(paddle.gameObject);
            var haptics = GetOrAdd<OvrControllerHaptics>(paddle.gameObject);
            SetEnum(hitter, "_player", (int)Side.Near);
            Set(hitter, "_match", matchCtl);
            Set(hitter, "_ball", ball);
            Set(hitter, "_feel", paddleFeel);
            SetSeam(hitter, "_haptics", haptics);
            Set(hitter, "_blade", blade);
            SetV(hitter, "_faceNormalLocal", Vector3.forward);
            SetF(hitter, "_bladeRadius", radius);
            SetF(hitter, "_bladeHalfThickness", halfThick);
            log.AppendLine($"Player paddle: {Path(paddle)}  blade radius {radius:F3} m (haptics: right controller)");

            // AI paddle: a visual copy with no grab or physics.
            var aiPaddle = match.Find("AI Paddle");
            if (aiPaddle == null)
            {
                aiPaddle = Object.Instantiate(paddle.gameObject, match).transform;
                aiPaddle.name = "AI Paddle";
                StripToVisual(aiPaddle.gameObject);
            }
            var ai = GetOrAdd<AIOpponent>(match.gameObject);
            Set(ai, "_match", matchCtl);
            Set(ai, "_paddle", aiPaddle);
            SetEnum(ai, "_player", (int)Side.Far);
            Set(ai, "_profile", aiProfile);

            // Passthrough (Meta).
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
            layer.overlayType = OVROverlay.OverlayType.Underlay;
            layer.hidden = true;
            EditorUtility.SetDirty(layer);
            var pt = GetOrAdd<MetaPassthrough>(match.gameObject);
            Set(pt, "_layer", layer);
            var cam = rig.centerEyeAnchor != null ? rig.centerEyeAnchor.GetComponent<Camera>() : null;
            SetArray(pt, "_cameras", cam != null ? new Object[] { cam } : new Object[0]);
            var hide = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(t => t.name.StartsWith("experimental_neon_room") || t.name == "Floor" || t.name == "Visuals")
                .Where(t => !top.transform.IsChildOf(t)).Select(t => (Object)t.gameObject).ToArray();
            SetArray(pt, "_hideInPassthrough", hide);
            log.AppendLine($"Passthrough: layer on {rig.name}, hides {hide.Length} object(s)" + (mgr == null ? " (no OVRManager found!)" : ""));

            // Commands, modes and scoreboard.
            var commands = GetOrAdd<InputSystemPlayerCommands>(match.gameObject);
            var modes = GetOrAdd<GameModeController>(match.gameObject);
            Set(modes, "_match", matchCtl);
            Set(modes, "_ai", ai);
            SetSeam(modes, "_commands", commands);
            SetSeam(modes, "_passthrough", pt);
            var board = GetOrCreate("Scoreboard", match);
            var sb = GetOrAdd<Scoreboard>(board);
            Set(sb, "_match", matchCtl);
            Set(sb, "_modes", modes);

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

        static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;
            string parent = System.IO.Path.GetDirectoryName(folder).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(folder));
        }

        static bool IsTableTop(BoxCollider b)
        {
            Vector3 s = b.bounds.size;
            float l = Mathf.Max(s.x, s.z), w = Mathf.Min(s.x, s.z);
            // Size only: the floor height differs per scene, so absolute height isn't reliable.
            return l > 2f && l < 3.2f && w > 1.2f && w < 1.9f && s.y < 0.15f;
        }

        /// <summary>Blade frame from the rubber (or largest) mesh: forward = face normal.</summary>
        static Transform MakeBlade(Transform paddle, out float radius, out float halfThick)
        {
            var existing = paddle.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "Blade");
            var filters = paddle.GetComponentsInChildren<MeshFilter>(true).Where(f => f.sharedMesh != null).ToArray();
            MeshFilter mf = filters.FirstOrDefault(f => f.name.ToLower().Contains("rubber"))
                            ?? filters.OrderByDescending(f => f.sharedMesh.bounds.size.sqrMagnitude).First();
            Bounds b = mf.sharedMesh.bounds;
            Vector3 ls = mf.transform.lossyScale;
            Vector3 world = new Vector3(b.size.x * Mathf.Abs(ls.x), b.size.y * Mathf.Abs(ls.y), b.size.z * Mathf.Abs(ls.z));
            int thin = world.x <= world.y && world.x <= world.z ? 0 : world.y <= world.z ? 1 : 2;
            Vector3 normal = thin == 0 ? Vector3.right : thin == 1 ? Vector3.up : Vector3.forward;
            Vector3 upAxis = thin == 1 ? Vector3.forward : Vector3.up;
            float a = world[(thin + 1) % 3], c = world[(thin + 2) % 3];
            radius = Mathf.Clamp(Mathf.Min(a, c) * 0.5f, 0.05f, 0.11f);
            halfThick = Mathf.Clamp(world[thin] * 0.5f, 0.004f, 0.02f);

            Transform blade = existing != null ? existing : new GameObject("Blade").transform;
            blade.SetParent(mf.transform, false);
            blade.localPosition = b.center;
            blade.localRotation = Quaternion.LookRotation(normal, upAxis);
            blade.localScale = new Vector3(1f / Mathf.Abs(ls.x), 1f / Mathf.Abs(ls.y), 1f / Mathf.Abs(ls.z));
            return blade;
        }

        static void StripToVisual(GameObject go)
        {
            foreach (Transform child in go.GetComponentsInChildren<Transform>(true).ToArray())
                if (child != null && child != go.transform && child.name.StartsWith("ISDK_")) Object.DestroyImmediate(child.gameObject);
            for (int pass = 0; pass < 5; pass++)
                foreach (var mb in go.GetComponentsInChildren<MonoBehaviour>(true)) Object.DestroyImmediate(mb);
            foreach (var j in go.GetComponentsInChildren<Joint>(true)) Object.DestroyImmediate(j);
            foreach (var col in go.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(col);
            foreach (var rb in go.GetComponentsInChildren<Rigidbody>(true)) Object.DestroyImmediate(rb);
        }

        static GameObject GetOrCreate(string name, Transform parent = null)
        {
            Transform found = parent != null ? parent.Find(name) : null;
            if (found == null && parent == null)
                found = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .FirstOrDefault(t => t.parent == null && t.name == name);
            if (found != null) return found.gameObject;
            var go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            return go;
        }

        static T GetOrAdd<T>(GameObject go) where T : Component =>
            go.TryGetComponent(out T c) ? c : go.AddComponent<T>();

        static void Set(Object target, string prop, Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(prop).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetEnum(Object target, string prop, int value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(prop).intValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Assigns a Seam&lt;T&gt; field (its serialized component reference).</summary>
        static void SetSeam(Object target, string prop, Component implementation) => Set(target, prop + "._component", implementation);

        static Object GetRef(Object target, string prop) => new SerializedObject(target).FindProperty(prop).objectReferenceValue;

        static void SetF(Object target, string prop, float value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(prop).floatValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetV(Object target, string prop, Vector3 value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(prop).vector3Value = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetArray(Object target, string prop, Object[] values)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(prop);
            p.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Time.fixedDeltaTime alone isn't saved; write it to ProjectSettings/TimeManager.asset.</summary>
        static void SetFixedTimestep(float dt)
        {
            Time.fixedDeltaTime = dt;
            EditorUtility.SetDirty(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TimeManager.asset")[0]);
            AssetDatabase.SaveAssets();
        }

        static string Path(Transform t) => t.parent == null ? t.name : Path(t.parent) + "/" + t.name;
    }
}

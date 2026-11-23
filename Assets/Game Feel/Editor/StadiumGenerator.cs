using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PingPong.GameFeel.EditorTools
{
    /// <summary>Inspector for StadiumBuilder: Build / Clear buttons.</summary>
    [CustomEditor(typeof(StadiumBuilder))]
    public class StadiumBuilderEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var b = (StadiumBuilder)target;
            EditorGUILayout.Space();
            if (GUILayout.Button("Build / Rebuild Stadium", GUILayout.Height(34))) StadiumGenerator.Build(b);
            using (new EditorGUI.DisabledScope(Application.isPlaying))
            {
                if (GUILayout.Button(CrowdPreview.Running ? "Stop Cheer Preview" : "Preview Cheer (no Play mode needed)", GUILayout.Height(26)))
                {
                    if (CrowdPreview.Running) CrowdPreview.Stop(); else CrowdPreview.Start(b, 4f);
                }
            }
            if (GUILayout.Button("Clear Stadium")) StadiumGenerator.Clear(b);
            EditorGUILayout.Space();
            DrawDefaultInspector();
        }
    }

    /// <summary>Animates the crowd in Edit mode using FanJump's own jump maths, then puts everyone back.</summary>
    public static class CrowdPreview
    {
        static FanJump[] _fans;
        static Vector3[] _bases;
        static double _start;
        static float _duration;

        public static bool Running => _fans != null;

        public static void Start(StadiumBuilder b, float duration)
        {
            Stop();
            _fans = b.GetComponentsInChildren<FanJump>(true);
            _bases = new Vector3[_fans.Length];
            for (int i = 0; i < _fans.Length; i++) _bases[i] = _fans[i].transform.localPosition;
            _start = EditorApplication.timeSinceStartup;
            _duration = duration;
            EditorApplication.update += Tick;
            // Never leave fans mid-jump in a saved scene, after a script reload or when entering Play mode.
            EditorSceneManager.sceneSaving += OnSceneSaving;
            AssemblyReloadEvents.beforeAssemblyReload += Stop;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            Debug.Log("[Game Feel] Previewing crowd cheer: " + _fans.Length + " fans");
        }

        public static void Stop()
        {
            if (_fans == null) return;
            EditorApplication.update -= Tick;
            EditorSceneManager.sceneSaving -= OnSceneSaving;
            AssemblyReloadEvents.beforeAssemblyReload -= Stop;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            for (int i = 0; i < _fans.Length; i++)
            {
                if (_fans[i] == null) continue;
                _fans[i].PreviewPose(_bases[i], 0f, 0f);
            }
            _fans = null;
            _bases = null;
            SceneView.RepaintAll();
        }

        static void OnSceneSaving(UnityEngine.SceneManagement.Scene scene, string path) => Stop();
        static void OnPlayModeChanged(PlayModeStateChange change) => Stop();

        static void Tick()
        {
            float t = (float)(EditorApplication.timeSinceStartup - _start);
            if (t >= _duration) { Stop(); return; }
            float env = Mathf.Min(Mathf.Clamp01(t / 0.25f), Mathf.Clamp01((_duration - t) / 0.7f));
            for (int i = 0; i < _fans.Length; i++)
            {
                if (_fans[i] != null) _fans[i].PreviewPose(_bases[i], t, env);
            }
            SceneView.RepaintAll();
        }
    }

    /// <summary>Edit-mode previews of the particle effects (a temporary, unsaved object; nothing is added to the scene).</summary>
    public static class EffectsPreview
    {
        static GameObject _go;

        static GameFeelEffects Make(GameFeelBootstrap boot)
        {
            Clear();
            _go = new GameObject("GameFeel Preview (not saved)") { hideFlags = HideFlags.DontSave };
            var fx = _go.AddComponent<GameFeelEffects>();
            fx.Init(boot);
            return fx;
        }

        static Vector3 TableCenter()
        {
            var t = Object.FindFirstObjectByType<PingPong.Gameplay.PingPongTable>();
            return t != null ? GameFeelTable.Center(t) : Vector3.zero;
        }

        public static void Confetti(GameFeelBootstrap boot)
        {
            var fx = Make(boot);
            fx.SimulateConfetti(TableCenter(), 1.6f);
            SceneView.RepaintAll();
        }

        public static void Impact(GameFeelBootstrap boot)
        {
            var fx = Make(boot);
            // A hard smash just above the south end of the table, facing the player.
            var p = TableCenter() + new Vector3(0f, 0.95f, -1.0f);
            fx.Impact(p, Vector3.back, 1f, boot.impactColor);
            fx.SimulateParticles(0.04f);
            fx.PreviewImpactPose(0.45f);
            SceneView.RepaintAll();
        }

        public static void Clear()
        {
            if (_go != null) Object.DestroyImmediate(_go);
            foreach (var g in Object.FindObjectsByType<GameFeelEffects>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (g.gameObject.hideFlags == HideFlags.DontSave) Object.DestroyImmediate(g.gameObject);
            _go = null;
            SceneView.RepaintAll();
        }
    }

    /// <summary>Adds a "Build Stadium" button to the Game Feel object, which creates the Stadium object.</summary>
    [CustomEditor(typeof(GameFeelBootstrap))]
    public class GameFeelBootstrapEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var boot = (GameFeelBootstrap)target;
            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(!Application.isPlaying))
            {
                if (GUILayout.Button(Application.isPlaying ? "Test Crowd Cheer" : "Test Crowd Cheer (Play mode only)", GUILayout.Height(28)))
                    FanJump.CheerAll(3f, 1f);
            }
            using (new EditorGUI.DisabledScope(Application.isPlaying))
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Preview Confetti")) EffectsPreview.Confetti(boot);
                if (GUILayout.Button("Preview Hit Impact")) EffectsPreview.Impact(boot);
                if (GUILayout.Button("Clear Preview")) EffectsPreview.Clear();
                EditorGUILayout.EndHorizontal();
            }
            using (new EditorGUI.DisabledScope(Application.isPlaying))
            {
                if (GUILayout.Button("Create Test Stations Scene", GUILayout.Height(26))) TestSceneBuilder.Create();
            }
            if (GUILayout.Button("Build Stadium", GUILayout.Height(34)))
            {
                var builder = Object.FindFirstObjectByType<StadiumBuilder>(FindObjectsInactive.Include);
                if (builder == null || builder.gameObject.scene != boot.gameObject.scene)
                {
                    var go = new GameObject("Stadium");
                    SceneManager_MoveToScene(go, boot.gameObject);
                    Undo.RegisterCreatedObjectUndo(go, "Create Stadium");
                    builder = Undo.AddComponent<StadiumBuilder>(go);
                }
                StadiumGenerator.Build(builder);
                Selection.activeGameObject = builder.gameObject;
            }
            EditorGUILayout.Space();
            DrawDefaultInspector();
        }

        void OnEnable()
        {
            var boot = (GameFeelBootstrap)target;
            if (boot != null && boot.confettiMaterial == null)
            {
                boot.confettiMaterial = AssetDatabase.LoadAssetAtPath<Material>(StadiumGenerator.ConfettiMaterialPath);
                if (boot.confettiMaterial != null) EditorUtility.SetDirty(boot);
            }
        }

        static void SceneManager_MoveToScene(GameObject go, GameObject sceneOf)
        {
            if (go.scene != sceneOf.scene) UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, sceneOf.scene);
        }
    }

    /// <summary>Generates a stadium out of cubes around the ping pong table.</summary>
    public static class StadiumGenerator
    {
        const string GeneratedName = "Generated";
        const string MaterialFolderParent = "Assets/Game Feel/Materials";
        const string MaterialFolder = "Assets/Game Feel/Materials/Stadium";
        public const string ConfettiMaterialPath = "Assets/Game Feel/Materials/GameFeel_Confetti.mat";

        static readonly Dictionary<string, Material> _mats = new Dictionary<string, Material>();

        public static void Clear(StadiumBuilder b)
        {
            var old = b.transform.Find(GeneratedName);
            if (old != null) Undo.DestroyObjectImmediate(old.gameObject);
            EditorSceneManager.MarkSceneDirty(b.gameObject.scene);
        }

        public static void Build(StadiumBuilder b)
        {
            _mats.Clear();
            var old = b.transform.Find(GeneratedName);
            if (old != null) Undo.DestroyObjectImmediate(old.gameObject);

            // ---- Anchor on the project's table (PingPongTable) and the floor ----
            Vector3 center = b.transform.position;
            Quaternion rotation = b.transform.rotation;
            float groundY = b.transform.position.y;
            var ppTable = b.centerOnTable ? Object.FindFirstObjectByType<PingPong.Gameplay.PingPongTable>() : null;
            if (ppTable != null && ppTable.gameObject.scene == b.gameObject.scene)
            {
                ppTable.Rebuild();
                var f = ppTable.Frame;
                center = f.SurfaceCenter;
                rotation = GameFeelTable.Rotation(f);
                groundY = f.SurfaceCenter.y - 0.76f; // regulation table height
            }
            var floor = FindInScene(b, b.floorObjectNamed);
            if (floor != null)
            {
                var fb = BoundsOf(floor);
                groundY = fb.HasValue ? fb.Value.max.y : floor.position.y;
            }

            var root = new GameObject(GeneratedName).transform;
            root.SetParent(b.transform, false);
            root.position = new Vector3(center.x, groundY, center.z);
            root.rotation = rotation;
            Undo.RegisterCreatedObjectUndo(root.gameObject, "Build Stadium");

            var rng = new System.Random(b.randomSeed);
            float hx = b.courtSize.x * 0.5f;
            float hz = b.courtSize.y * 0.5f;
            float standInner = b.walkwayWidth;               // distance from court edge to first row
            float standDepth = b.rows * b.rowDepth;
            float topHeight = RowTop(b, b.rows - 1);
            float outerX = hx + standInner + standDepth;
            float outerZ = hz + standInner + standDepth;

            var mGround = Mat("Ground", b.groundColor);
            var mCourt = Mat("Court", b.courtColor, smooth: 0.35f);
            var mLine = Mat("Lines", b.lineColor);
            var mStand = Mat("Stand", b.standColor);
            var mSeatA = Mat("Seat A", b.seatColorA);
            var mSeatB = Mat("Seat B", b.seatColorB);
            var mSkin = Mat("Crowd Skin", b.skinColor);
            var mLight = Mat("Light", b.lightColor, emission: 2f);
            var mFrame = Mat("Frame", new Color(0.1f, 0.1f, 0.12f), smooth: 0.5f);
            var crowdMats = new List<Material>();
            for (int i = 0; i < b.crowdColors.Length; i++) crowdMats.Add(Mat("Crowd " + i, b.crowdColors[i]));
            var adMats = new List<Material>();
            for (int i = 0; i < b.adBoardColors.Length; i++) adMats.Add(Mat("Ad Board " + i, b.adBoardColors[i], emission: 0.6f));
            if (adMats.Count == 0) adMats.Add(mSeatA);
            if (crowdMats.Count == 0) crowdMats.Add(mSeatB);

            var floorGroup = Group(root, "Ground");
            var ground = Cube("Ground", floorGroup, new Vector3(0, -0.06f, 0), new Vector3(outerX * 2 + 10, 0.1f, outerZ * 2 + 10), mGround, true, false);
            var groundCollider = ground.GetComponent<BoxCollider>();
            groundCollider.center = new Vector3(0f, -4.5f, 0f);
            groundCollider.size = new Vector3(1f, 10f, 1f);
            Cube("Court", floorGroup, new Vector3(0, -0.025f, 0), new Vector3(b.courtSize.x, 0.04f, b.courtSize.y), mCourt, false, false);
            float lw = 0.07f, inset = 0.35f, ly = -0.003f;
            Cube("Line N", floorGroup, new Vector3(0, ly, hz - inset), new Vector3(b.courtSize.x - inset * 2, 0.01f, lw), mLine, false, false);
            Cube("Line S", floorGroup, new Vector3(0, ly, -hz + inset), new Vector3(b.courtSize.x - inset * 2, 0.01f, lw), mLine, false, false);
            Cube("Line E", floorGroup, new Vector3(hx - inset, ly, 0), new Vector3(lw, 0.01f, b.courtSize.y - inset * 2), mLine, false, false);
            Cube("Line W", floorGroup, new Vector3(-hx + inset, ly, 0), new Vector3(lw, 0.01f, b.courtSize.y - inset * 2), mLine, false, false);
            Cube("Line Centre", floorGroup, new Vector3(0, ly, 0), new Vector3(b.courtSize.x - inset * 2, 0.01f, lw), mLine, false, false);
            // Centre "circle" as an octagon of short segments.
            for (int i = 0; i < 16; i++)
            {
                float a = i / 16f * Mathf.PI * 2f;
                float r = 1.6f;
                var p = new Vector3(Mathf.Cos(a) * r, ly, Mathf.Sin(a) * r);
                Cube("Circle " + i, floorGroup, p, new Vector3(0.65f, 0.01f, lw), mLine, false, false,
                    Quaternion.Euler(0, -a * Mathf.Rad2Deg + 90f, 0));
            }

            // ---- Ad boards around the court ----
            var boards = Group(root, "Ad Boards");
            BuildBoards(boards, b, -hx, hx, hz, true, adMats);
            BuildBoards(boards, b, -hx, hx, -hz, true, adMats);
            BuildBoards(boards, b, -hz, hz, hx, false, adMats);
            BuildBoards(boards, b, -hz, hz, -hx, false, adMats);

            // ---- Stands, seats and crowd ----
            var stands = Group(root, "Stands");
            var seats = Group(root, "Seats");
            var crowd = Group(root, "Crowd");
            for (int r = 0; r < b.rows; r++)
            {
                float top = RowTop(b, r);
                float inner = standInner + r * b.rowDepth;
                float lenX = b.courtSize.x + 2f * (standInner + (r + 1) * b.rowDepth); // long sides include corners
                float lenZ = b.courtSize.y + 2f * inner;                               // short sides fit between

                // North / South (along X)
                foreach (float sign in new[] { 1f, -1f })
                {
                    float z = sign * (hz + inner + b.rowDepth * 0.5f);
                    Cube("Row " + r + (sign > 0 ? " N" : " S"), stands, new Vector3(0, top * 0.5f, z),
                        new Vector3(lenX, top, b.rowDepth), mStand, true, true);
                    PlaceSeats(b, rng, seats, crowd, true, lenX, top, z + sign * b.rowDepth * 0.15f, sign,
                        mSeatA, mSeatB, crowdMats, mSkin);
                }
                // East / West (along Z)
                foreach (float sign in new[] { 1f, -1f })
                {
                    float x = sign * (hx + inner + b.rowDepth * 0.5f);
                    Cube("Row " + r + (sign > 0 ? " E" : " W"), stands, new Vector3(x, top * 0.5f, 0),
                        new Vector3(b.rowDepth, top, lenZ), mStand, true, true);
                    PlaceSeats(b, rng, seats, crowd, false, lenZ, top, x + sign * b.rowDepth * 0.15f, sign,
                        mSeatA, mSeatB, crowdMats, mSkin);
                }
            }

            // Back wall with light strip on top
            var wall = Group(root, "Back Wall");
            float wallH = topHeight + 1.2f;
            float wt = 0.3f;
            Cube("Wall N", wall, new Vector3(0, wallH * 0.5f, outerZ + wt * 0.5f), new Vector3(outerX * 2 + wt * 2, wallH, wt), mStand, true, true);
            Cube("Wall S", wall, new Vector3(0, wallH * 0.5f, -outerZ - wt * 0.5f), new Vector3(outerX * 2 + wt * 2, wallH, wt), mStand, true, true);
            Cube("Wall E", wall, new Vector3(outerX + wt * 0.5f, wallH * 0.5f, 0), new Vector3(wt, wallH, outerZ * 2), mStand, true, true);
            Cube("Wall W", wall, new Vector3(-outerX - wt * 0.5f, wallH * 0.5f, 0), new Vector3(wt, wallH, outerZ * 2), mStand, true, true);
            Cube("Wall Strip N", wall, new Vector3(0, wallH - 0.15f, outerZ - 0.02f), new Vector3(outerX * 2, 0.12f, 0.06f), mLight, false, false);
            Cube("Wall Strip S", wall, new Vector3(0, wallH - 0.15f, -outerZ + 0.02f), new Vector3(outerX * 2, 0.12f, 0.06f), mLight, false, false);
            Cube("Wall Strip E", wall, new Vector3(outerX - 0.02f, wallH - 0.15f, 0), new Vector3(0.06f, 0.12f, outerZ * 2), mLight, false, false);
            Cube("Wall Strip W", wall, new Vector3(-outerX + 0.02f, wallH - 0.15f, 0), new Vector3(0.06f, 0.12f, outerZ * 2), mLight, false, false);

            // Banners hanging on the back wall above the top row
            var banners = Group(root, "Banners");
            int bi = 0;
            for (float x = -outerX + 2f; x <= outerX - 2f; x += 4f, bi++)
            {
                var m = bi % 2 == 0 ? mSeatA : mSeatB;
                Cube("Banner N " + bi, banners, new Vector3(x, topHeight + 0.75f, outerZ - 0.05f), new Vector3(1.2f, 0.9f, 0.04f), m, false, false);
                Cube("Banner S " + bi, banners, new Vector3(x, topHeight + 0.75f, -outerZ + 0.05f), new Vector3(1.2f, 0.9f, 0.04f), m, false, false);
            }

            // ---- Canopy with floodlight strip ----
            if (b.canopy)
            {
                var can = Group(root, "Canopy");
                float ch = wallH + 1.6f;
                float cd = 3.2f;
                Cube("Canopy N", can, new Vector3(0, ch, outerZ - cd * 0.5f + wt), new Vector3(outerX * 2 + wt * 2, 0.15f, cd), mFrame, false, true);
                Cube("Canopy S", can, new Vector3(0, ch, -outerZ + cd * 0.5f - wt), new Vector3(outerX * 2 + wt * 2, 0.15f, cd), mFrame, false, true);
                Cube("Canopy E", can, new Vector3(outerX - cd * 0.5f + wt, ch, 0), new Vector3(cd, 0.15f, outerZ * 2 - cd * 2 + wt * 4), mFrame, false, true);
                Cube("Canopy W", can, new Vector3(-outerX + cd * 0.5f - wt, ch, 0), new Vector3(cd, 0.15f, outerZ * 2 - cd * 2 + wt * 4), mFrame, false, true);
                float fz = outerZ - cd + wt + 0.1f, fx = outerX - cd + wt + 0.1f;
                Cube("Floodlight N", can, new Vector3(0, ch - 0.12f, fz), new Vector3(outerX * 2 - 1f, 0.08f, 0.2f), mLight, false, false);
                Cube("Floodlight S", can, new Vector3(0, ch - 0.12f, -fz), new Vector3(outerX * 2 - 1f, 0.08f, 0.2f), mLight, false, false);
                Cube("Floodlight E", can, new Vector3(fx, ch - 0.12f, 0), new Vector3(0.2f, 0.08f, outerZ * 2 - cd * 2), mLight, false, false);
                Cube("Floodlight W", can, new Vector3(-fx, ch - 0.12f, 0), new Vector3(0.2f, 0.08f, outerZ * 2 - cd * 2), mLight, false, false);
                // Support pillars at the back
                for (float x = -outerX; x <= outerX + 0.01f; x += 6f)
                {
                    Cube("Pillar N", can, new Vector3(x, ch * 0.5f, outerZ + wt * 0.5f), new Vector3(0.25f, ch, 0.25f), mFrame, false, true);
                    Cube("Pillar S", can, new Vector3(x, ch * 0.5f, -outerZ - wt * 0.5f), new Vector3(0.25f, ch, 0.25f), mFrame, false, true);
                }
            }

            // ---- Light towers at the corners ----
            if (b.lightTowers)
            {
                var towers = Group(root, "Light Towers");
                float th = wallH + 9f;
                foreach (var c in new[] { new Vector2(1, 1), new Vector2(-1, 1), new Vector2(1, -1), new Vector2(-1, -1) })
                {
                    var p = new Vector3(c.x * (outerX + 1.5f), 0, c.y * (outerZ + 1.5f));
                    Cube("Tower", towers, p + Vector3.up * th * 0.5f, new Vector3(0.5f, th, 0.5f), mFrame, false, true);
                    var look = Quaternion.LookRotation(new Vector3(-p.x, 0, -p.z).normalized) * Quaternion.Euler(30f, 0, 0);
                    var head = Cube("Tower Head", towers, p + Vector3.up * (th + 0.6f), new Vector3(3f, 1.8f, 0.35f), mFrame, false, true, look);
                    // grid of lamps on the face of the head
                    for (int ix = 0; ix < 4; ix++)
                    for (int iy = 0; iy < 2; iy++)
                    {
                        var local = new Vector3(-1.05f + ix * 0.7f, -0.42f + iy * 0.84f, 0.2f);
                        var lampPos = head.transform.localPosition + look * local;
                        Cube("Lamp", towers, lampPos, new Vector3(0.5f, 0.6f, 0.08f), mLight, false, false, look);
                    }
                }
            }

            // ---- Scoreboard ----
            if (b.scoreboard)
            {
                var board = Group(root, "Scoreboard");
                var mScreen = Mat("Screen", b.screenColor, smooth: 0.8f);
                var mText = Mat("Screen Text", b.screenTextColor, emission: 2.5f);
                float px = 0.17f;
                string text = string.IsNullOrEmpty(b.scoreboardText) ? " " : b.scoreboardText.ToUpperInvariant();
                int chars = Mathf.Max(11, text.Length); // room for "BLUE WINS", "11 - 10" etc.
                int cols = chars * 6 - 1;
                float screenW = cols * px + 1.2f;
                float screenH = 7 * px + 1.0f;
                float by = wallH + 1.6f + 2.4f + screenH * 0.5f; // above the canopy so it can be seen from the court
                float bz = outerZ + 0.6f; // far (north) end, facing the player who starts at the south end
                Cube("Board Frame", board, new Vector3(0, by, bz), new Vector3(screenW + 0.4f, screenH + 0.4f, 0.4f), mFrame, false, true);
                var screen = Cube("Board Screen", board, new Vector3(0, by, bz - 0.21f), new Vector3(screenW, screenH, 0.02f), mScreen, false, false);
                Cube("Board Leg L", board, new Vector3(-screenW * 0.35f, (by - screenH * 0.5f) * 0.5f, bz), new Vector3(0.35f, by - screenH * 0.5f, 0.35f), mFrame, false, true);
                Cube("Board Leg R", board, new Vector3(screenW * 0.35f, (by - screenH * 0.5f) * 0.5f, bz), new Vector3(0.35f, by - screenH * 0.5f, 0.35f), mFrame, false, true);

                // Full pixel grid (every pixel of every character slot) so the text can change at runtime.
                var display = board.gameObject.AddComponent<ScoreboardDisplay>();
                display.characters = chars;
                display.idleText = text;
                display.screen = screen.GetComponent<Renderer>();
                var pixelRoot = Group(board, "Pixels");
                // Viewer at the court looks north (+Z), so their right is +X.
                float startX = -cols * px * 0.5f + px * 0.5f;
                for (int slot = 0; slot < chars; slot++)
                for (int row = 0; row < PixelFont.Height; row++)
                for (int col = 0; col < PixelFont.Width; col++)
                {
                    float x = startX + (slot * 6 + col) * px;
                    float y = by + (3 - row) * px;
                    var p = Cube("Px", pixelRoot, new Vector3(x, y, bz - 0.23f), new Vector3(px * 0.85f, px * 0.85f, 0.03f), mText, false, false);
                    display.pixels.Add(p.GetComponent<Renderer>());
                }
                display.SetText(text);
            }

            var boot = Object.FindFirstObjectByType<GameFeelBootstrap>();
            if (boot != null && boot.confettiMaterial == null)
            {
                Undo.RecordObject(boot, "Assign confetti material");
                boot.confettiMaterial = AssetDatabase.LoadAssetAtPath<Material>(ConfettiMaterialPath);
            }
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(b.gameObject.scene);
            Debug.Log("[Game Feel] Stadium built: " + root.GetComponentsInChildren<MeshRenderer>().Length + " cubes.");
        }

        // ------------------------------------------------------------------

        static float RowTop(StadiumBuilder b, int r) => 0.35f + (r + 1) * b.rowHeight;

        static void BuildBoards(Transform parent, StadiumBuilder b, float from, float to, float fixedCoord, bool alongX, List<Material> mats)
        {
            const float panel = 2f;
            int count = Mathf.Max(1, Mathf.RoundToInt((to - from) / panel));
            float len = (to - from) / count;
            for (int i = 0; i < count; i++)
            {
                float c = from + len * (i + 0.5f);
                var m = mats[i % mats.Count];
                var pos = alongX ? new Vector3(c, b.barrierHeight * 0.5f, fixedCoord) : new Vector3(fixedCoord, b.barrierHeight * 0.5f, c);
                var size = alongX ? new Vector3(len - 0.04f, b.barrierHeight, 0.12f) : new Vector3(0.12f, b.barrierHeight, len - 0.04f);
                Cube("Ad Board", parent, pos, size, m, true, true);
            }
        }

        static void PlaceSeats(StadiumBuilder b, System.Random rng, Transform seats, Transform crowd, bool alongX,
            float length, float top, float fixedCoord, float sign, Material seatA, Material seatB,
            List<Material> crowdMats, Material skin)
        {
            int count = Mathf.FloorToInt((length - 0.6f) / b.seatSpacing);
            float start = -(count - 1) * b.seatSpacing * 0.5f;
            const int seatsPerSection = 8;
            for (int i = 0; i < count; i++)
            {
                float a = start + i * b.seatSpacing;
                Vector3 Pos(float y) => alongX ? new Vector3(a, y, fixedCoord) : new Vector3(fixedCoord, y, a);
                Vector3 Size(float w, float h, float d) => alongX ? new Vector3(w, h, d) : new Vector3(d, h, w);

                bool person = rng.NextDouble() < b.crowdDensity;
                if (person)
                {
                    // One "Fan" object per person (body, head, arms as children) with FanJump on it,
                    // so the whole fan moves when the crowd cheers. Not static, because it moves.
                    var m = crowdMats[rng.Next(crowdMats.Count)];
                    float bodyH = 0.45f + (float)rng.NextDouble() * 0.15f;
                    float jitter = ((float)rng.NextDouble() - 0.5f) * 0.1f;
                    var fan = new GameObject("Fan").transform;
                    fan.SetParent(crowd, false);
                    fan.localPosition = Pos(top) + (alongX ? new Vector3(jitter, 0, 0) : new Vector3(0, 0, jitter));

                    MovingCube("Body", fan, new Vector3(0, bodyH * 0.5f, 0), Size(0.42f, bodyH, 0.3f), m);
                    MovingCube("Head", fan, new Vector3(0, bodyH + 0.13f, 0), Size(0.24f, 0.24f, 0.24f), skin);
                    Vector3 side = alongX ? new Vector3(0.16f, 0, 0) : new Vector3(0, 0, 0.16f);
                    var armL = MovingCube("Arm L", fan, new Vector3(0, bodyH + 0.3f, 0) - side, Size(0.09f, 0.45f, 0.09f), m);
                    var armR = MovingCube("Arm R", fan, new Vector3(0, bodyH + 0.3f, 0) + side, Size(0.09f, 0.45f, 0.09f), m);

                    bool armsUp = rng.NextDouble() < 0.18;
                    armL.SetActive(armsUp);
                    armR.SetActive(armsUp);

                    var jump = fan.gameObject.AddComponent<FanJump>();
                    jump.leftArm = armL.transform;
                    jump.rightArm = armR.transform;
                    jump.armsAlwaysUp = armsUp;
                    jump.eagerness = (float)rng.NextDouble();
                }
                else
                {
                    var m = ((i / seatsPerSection) % 2 == 0) ? seatA : seatB;
                    Cube("Seat", seats, Pos(top + 0.11f), Size(0.48f, 0.22f, 0.42f), m, false, false);
                    Cube("Seat Back", seats, Pos(top + 0.32f) + (alongX ? new Vector3(0, 0, sign * 0.19f) : new Vector3(sign * 0.19f, 0, 0)),
                        Size(0.48f, 0.4f, 0.06f), m, false, false);
                }
            }
        }

        /// <summary>A cube for something that moves at runtime: no collider, no shadows, not static.</summary>
        static GameObject MovingCube(string name, Transform parent, Vector3 localPos, Vector3 size, Material m)
        {
            var go = Cube(name, parent, localPos, size, m, false, false);
            GameObjectUtility.SetStaticEditorFlags(go, 0);
            return go;
        }

        internal static Transform Group(Transform parent, string name)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            return t;
        }

        internal static GameObject Cube(string name, Transform parent, Vector3 localPos, Vector3 size, Material m,
            bool collider, bool shadows, Quaternion? rot = null)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = rot ?? Quaternion.identity;
            go.transform.localScale = size;
            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = m;
            mr.shadowCastingMode = shadows ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = shadows;
            if (!collider) Object.DestroyImmediate(go.GetComponent<BoxCollider>());
            GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic);
            return go;
        }

        internal static Material Mat(string name, Color c, float emission = 0f, float smooth = 0.15f)
        {
            if (_mats.TryGetValue(name, out var cached)) return cached;
            if (!AssetDatabase.IsValidFolder(MaterialFolder)) AssetDatabase.CreateFolder(MaterialFolderParent, "Stadium");
            string path = MaterialFolder + "/" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Standard");
                m = new Material(shader);
                AssetDatabase.CreateAsset(m, path);
            }
            m.color = c;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
            m.enableInstancing = true;
            if (emission > 0f)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", c * emission);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            else
            {
                m.DisableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", Color.black);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
            }
            EditorUtility.SetDirty(m);
            _mats[name] = m;
            return m;
        }

        static Transform FindInScene(StadiumBuilder b, string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            foreach (var root in b.gameObject.scene.GetRootGameObjects())
            {
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                {
                    if (t.IsChildOf(b.transform)) continue;
                    if (string.Equals(t.name, name, System.StringComparison.OrdinalIgnoreCase)) return t;
                }
            }
            return null;
        }

        static Bounds? BoundsOf(Transform t)
        {
            var rs = t.GetComponentsInChildren<Renderer>(true);
            if (rs.Length == 0) return null;
            var bb = rs[0].bounds;
            for (int i = 1; i < rs.Length; i++) bb.Encapsulate(rs[i].bounds);
            return bb;
        }
    }
}

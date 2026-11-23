using System.Collections.Generic;
using PingPong.Core;
using PingPong.Gameplay;
using PingPong.Presentation;
using PingPong.GameFeel.Testing;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PingPong.GameFeel.EditorTools
{
    /// <summary>
    /// Builds Assets/Game Feel/Scenes/GameFeel_TestStations.unity: a line of stations, each showing one Game Feel
    /// feature on its own, inside a long cube stadium (crowd, lights, screen). Stations that need a ball use the
    /// project's own table prefab and a real PingPongBall, so they behave exactly as in the game.
    /// Menu: Tools > Game Feel > Create Test Stations Scene. Running it again rebuilds the scene from scratch.
    /// </summary>
    public static class TestSceneBuilder
    {
        public const string ScenePath = "Assets/Game Feel/Scenes/GameFeel_TestStations.unity";
        const string EffectMatPath = "Assets/Game Feel/Materials/GameFeel_Additive.mat";
        const string BallPhysicsMatPath = "Assets/Game Feel/Materials/GameFeel_TestBall.physicMaterial";
        const string TablePrefabPath = "Assets/_Project/Prefabs/General/Table_Tennis.prefab";
        const string BallPrefabPath = "Assets/_Project/Prefabs/Equipment/Ball.prefab";
        const string BallHitClipPath = "Assets/_Project/Audio/BallHit.wav";

        const float Spacing = 7f;   // metres between stations along X
        const float PadZ = -3.2f;   // where you stand, in front of each station

        [MenuItem("Tools/Game Feel/Create Test Stations Scene")]
        public static void Create()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Selection.activeObject = null;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // ---- Light & floor (the floor collider ends rallies, like the game's floor) ----
            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            light.shadows = LightShadows.Soft;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            float lineLength = Spacing * 7f;
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor";
            floor.transform.position = new Vector3(lineLength * 0.5f, 0f, 0f);
            floor.transform.localScale = new Vector3(lineLength / 10f + 4f, 1f, 4f);
            floor.GetComponent<MeshRenderer>().enabled = false; // the stadium court is the visible floor

            // ---- Game Feel ----
            var gf = new GameObject("Game Feel");
            var boot = gf.AddComponent<GameFeelBootstrap>();
            boot.effectMaterial = AssetDatabase.LoadAssetAtPath<Material>(EffectMatPath);
            boot.confettiMaterial = AssetDatabase.LoadAssetAtPath<Material>(StadiumGenerator.ConfettiMaterialPath);
            boot.popupFont = AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>(GameSceneBuilder.PopupFontPath);

            // ---- A long stadium around the whole line ----
            var stadiumGo = new GameObject("Stadium");
            stadiumGo.transform.position = new Vector3(lineLength * 0.5f, 0f, 0f);
            var stadium = stadiumGo.AddComponent<StadiumBuilder>();
            stadium.centerOnTable = false;
            stadium.floorObjectNamed = "";
            stadium.hideWhileShown = new string[0];
            stadium.courtSize = new Vector2(lineLength + 12f, 15f);
            stadium.rows = 5;
            stadium.lightTowers = false;
            stadium.scoreboardText = "TEST LAB";
            StadiumGenerator.Build(stadium);

            var mPost = StadiumGenerator.Mat("Test Sign Post", new Color(0.12f, 0.12f, 0.14f));
            var mBoard = StadiumGenerator.Mat("Test Sign Board", new Color(0.03f, 0.04f, 0.09f), smooth: 0.7f);
            var mText = StadiumGenerator.Mat("Test Sign Text", new Color(1f, 0.8f, 0.2f), emission: 2f);
            var mRubber = StadiumGenerator.Mat("Test Paddle Rubber", new Color(0.85f, 0.1f, 0.12f), smooth: 0.3f);
            var mWood = StadiumGenerator.Mat("Test Paddle Wood", new Color(0.62f, 0.42f, 0.24f));
            var mLauncher = StadiumGenerator.Mat("Test Launcher", new Color(0.25f, 0.27f, 0.3f), smooth: 0.6f);
            var mPad = StadiumGenerator.Mat("Test Station Pad", new Color(1f, 0.55f, 0.1f), emission: 0.8f);
            var ballPhysics = BallPhysicsMaterial();

            var stations = new GameObject("Test Stations").transform;
            var director = new GameObject("Test Stations Director").AddComponent<TestStationsDirector>();
            var views = new List<Transform>();

            // ---- 2 HIT: launcher at the far end fires at a paddle over the near end ----
            {
                var g = Station(stations, 2, out float x);
                var table = Table(g, x);
                var f = table.Frame;
                var ball = Ball(g, table, ballPhysics);
                // Only the hit itself: no streak, landing ring, table effects or hit points here.
                var opts = ball.gameObject.AddComponent<GameFeelBallOptions>();
                opts.streak = false;
                opts.landingRing = false;
                opts.hitPoints = false;
                opts.onlyPaddleEffects = true;
                Vector3 paddlePos = f.PointOnSide(Side.Near, 0f, f.HalfLength - 0.15f, 0.3f);
                var paddle = MakePaddle(g, paddlePos, mRubber, mWood);
                var launcher = MakeLauncher(g, "Hit Launcher", f.PointOnSide(Side.Far, 0f, f.HalfLength + 0.4f, 0.35f), paddle.transform, mLauncher);
                launcher.ball = ball;
                launcher.mode = TestBallLauncher.FireMode.Straight;
                launcher.speeds = new[] { 3.5f, 7f, 12f };
                director.hitLauncher = launcher;
                views.Add(Spot(g, x, TestStation.Demo.Hit, 2, "2  HIT",
                    "Fires a ball at the paddle. Each press: soft, medium, then a SMASH. Just the hit: the flash, shockwave ring, sparks, ball squash and paddle flash, bigger the harder it is hit.",
                    paddlePos, mPad));
                Sign(g, "2 HIT", x, mPost, mBoard, mText);
            }

            // ---- 1 STREAK: lob from the near end onto the far half ----
            {
                var g = Station(stations, 1, out float x);
                var table = Table(g, x);
                var f = table.Frame;
                var ball = Ball(g, table, ballPhysics);
                var target = Empty(g, "Streak Target", f.PointOnSide(Side.Far, 0f, 0.8f));
                var launcher = MakeLauncher(g, "Streak Launcher", f.PointOnSide(Side.Near, -0.55f, f.HalfLength + 0.3f, 0.35f), target, mLauncher);
                launcher.ball = ball;
                launcher.mode = TestBallLauncher.FireMode.Lob;
                launcher.speeds = new[] { 3.5f, 5f, 6.5f };
                launcher.targetJitter = new Vector3(0.45f, 0f, 0.3f);
                director.streakLauncher = launcher;
                views.Add(Spot(g, x, TestStation.Demo.Streak, 1, "1  STREAK",
                    "Lobs a ball over the table. Watch the streak behind the ball, the landing ring that predicts the bounce, and the ring where it lands.",
                    f.SurfaceCenter, mPad));
                Sign(g, "1 STREAK", x, mPost, mBoard, mText);
            }

            // ---- 3 HIT POINTS: scripted rally popups ----
            {
                var g = Station(stations, 3, out float x);
                var focus = Empty(g, "Popup Point", new Vector3(x, 1.15f, -1.3f));
                Plinth(g, x, mPost);
                var view = Spot(g, x, TestStation.Demo.HitPoints, 3, "3  HIT POINTS",
                    "A scripted rally: +points popups with a growing combo (X2...X5), landing bonuses, RALLY X5! on the big screen.",
                    focus.position, mPad);
                view.parent.GetComponent<TestStation>().focus = focus;
                views.Add(view);
                Sign(g, "3 HIT POINTS", x, mPost, mBoard, mText);
            }

            // ---- 4 CROWD, 5 POINT, 6 WIN: stadium-wide reactions ----
            {
                var g = Station(stations, 5, out float x);
                Plinth(g, x, mPost);
                views.Add(Spot(g, x, TestStation.Demo.Point, 5, "5  POINT",
                    "A point is won (alternating sides): the lights and the big screen flash in the winner's colour, the crowd cheers and the score on the screen changes.",
                    new Vector3(x, 4f, 6f), mPad));
                Sign(g, "5 POINT", x, mPost, mBoard, mText);
            }
            {
                var g = Station(stations, 6, out float x);
                var focus = Empty(g, "Confetti Point", new Vector3(x, 0.8f, 0.5f));
                Plinth(g, x, mPost);
                var view = Spot(g, x, TestStation.Demo.Win, 6, "6  WIN",
                    "The game is won: confetti, YOU WIN / CPU WINS on the big screen, the big crowd cheer and flashing lights, then your hit-point high scores.",
                    new Vector3(x, 1.5f, 1f), mPad);
                view.parent.GetComponent<TestStation>().focus = focus;
                views.Add(view);
                Sign(g, "6 WIN", x, mPost, mBoard, mText);
            }
            {
                var g = Station(stations, 4, out float x);
                Plinth(g, x, mPost);
                views.Add(Spot(g, x, TestStation.Demo.Crowd, 4, "4  CROWD",
                    "The crowd jumps up and down with their arms in the air. (Crowd sounds are paused for now.)",
                    new Vector3(x, 2.5f, 9f), mPad));
                Sign(g, "4 CROWD", x, mPost, mBoard, mText);
            }

            // ---- 7 TARGETS ----
            {
                var g = Station(stations, 7, out float x);
                var table = Table(g, x);
                var f = table.Frame;
                var ball = Ball(g, table, ballPhysics);
                var target = Empty(g, "Targets Aim", f.PointOnSide(Side.Far, 0f, 0.8f));
                var launcher = MakeLauncher(g, "Targets Launcher", f.PointOnSide(Side.Near, 0.55f, f.HalfLength + 0.3f, 0.35f), target, mLauncher);
                launcher.ball = ball;
                launcher.mode = TestBallLauncher.FireMode.Lob;
                launcher.targetJitter = new Vector3(0.08f, 0f, 0.08f);
                director.targetsLauncher = launcher;
                director.targetsTable = table;
                views.Add(Spot(g, x, TestStation.Demo.Targets, 7, "7  TARGETS",
                    "First press: glowing targets appear on the table. Then each press lobs a ball at the far target: inside = +100, bullseye = +200.",
                    f.SurfaceCenter, mPad));
                Sign(g, "7 TARGETS", x, mPost, mBoard, mText);
            }

            // ---- 8 PRACTICE: the ball machine (built at runtime behind the far end) ----
            {
                var g = Station(stations, 8, out float x);
                var table = Table(g, x);
                var f = table.Frame;
                var ball = Ball(g, table, ballPhysics);
                ball.transform.position = f.PointOnSide(Side.Far, 0f, f.HalfLength + 0.4f, 0.3f);
                director.practiceBall = ball;
                views.Add(Spot(g, x, TestStation.Demo.Practice, 8, "8  PRACTICE",
                    "Turns the ball machine on/off. It feeds the ball onto your half (watch the landing rings). In the game it pauses the match and the AI.",
                    f.SurfaceCenter, mPad));
                Sign(g, "8 PRACTICE", x, mPost, mBoard, mText);
            }

            // ---- Camera ----
            var overview = new GameObject("F1 Overview").transform;
            overview.SetPositionAndRotation(new Vector3(lineLength * 0.5f, 9f, -16f), Quaternion.LookRotation(new Vector3(0f, -0.35f, 1f)));
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.nearClipPlane = 0.03f;
            cam.farClipPlane = 300f;
            camGo.AddComponent<AudioListener>();
            var fly = camGo.AddComponent<TestFlyCamera>();
            fly.viewpoints = new[] { overview };
            director.player = fly;
            var first = views.Find(v => v.parent.GetComponent<TestStation>().number == 1);
            camGo.transform.SetPositionAndRotation(first.position, first.rotation);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("[Game Feel] Test stations scene created: " + ScenePath + ". Press Play, then 1-8 to visit a station and E to run it.");
        }

        // ------------------------------------------------------------------

        static Transform Station(Transform parent, int number, out float x)
        {
            x = (number - 1) * Spacing;
            var g = StadiumGenerator.Group(parent, "Station " + number);
            g.position = new Vector3(x, 0f, 0f);
            return g;
        }

        static Transform Empty(Transform parent, string name, Vector3 pos)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.position = pos;
            return t;
        }

        /// <summary>The project's table prefab, turned so its Near end faces the camera (-Z).</summary>
        static PingPongTable Table(Transform parent, float x)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TablePrefabPath);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.transform.position = new Vector3(x, 0f, 0.6f);
            var table = go.GetComponentInChildren<PingPongTable>();
            table.Rebuild();
            Vector3 fwd = table.Frame.Forward;
            float turn = Vector3.SignedAngle(fwd, Vector3.forward, Vector3.up);
            go.transform.rotation = Quaternion.AngleAxis(turn, Vector3.up) * go.transform.rotation;
            // keep the surface centre on the station's line
            table.Rebuild();
            Vector3 c = table.Frame.SurfaceCenter;
            go.transform.position += new Vector3(x - c.x, 0f, 0.6f - c.z);
            table.Rebuild();
            return table;
        }

        /// <summary>A real PingPongBall (the project's ball script, physics profile and impact sound), held at rest.</summary>
        static PingPongBall Ball(Transform parent, PingPongTable table, PhysicsMaterial physics)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Ball";
            go.transform.SetParent(parent, false);
            go.transform.localScale = Vector3.one * 0.04f;
            go.transform.position = table.Frame.SurfaceCenter + Vector3.up * 0.4f;
            go.GetComponent<SphereCollider>().sharedMaterial = physics;

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BallPrefabPath);
            var prefabRenderer = prefab != null ? prefab.GetComponentInChildren<MeshRenderer>() : null;
            if (prefabRenderer != null) go.GetComponent<MeshRenderer>().sharedMaterial = prefabRenderer.sharedMaterial;

            var rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            var ball = go.AddComponent<PingPongBall>();
            var so = new SerializedObject(ball);
            so.FindProperty("_physics").objectReferenceValue = FindAsset<BallPhysicsProfile>();
            so.FindProperty("_table").objectReferenceValue = table;
            so.ApplyModifiedPropertiesWithoutUndo();

            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(BallHitClipPath);
            if (clip != null)
            {
                var src = go.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.spatialBlend = 1f;
                var audio = go.AddComponent<BallImpactAudio>();
                var ao = new SerializedObject(audio);
                ao.FindProperty("_ball").objectReferenceValue = ball;
                foreach (var p in new[] { "_table", "_paddle", "_net", "_other" })
                {
                    var prop = ao.FindProperty(p);
                    if (prop != null) prop.objectReferenceValue = clip;
                }
                ao.ApplyModifiedPropertiesWithoutUndo();
            }
            return ball;
        }

        static T FindAsset<T>() where T : Object
        {
            foreach (var guid in AssetDatabase.FindAssets("t:" + typeof(T).Name))
            {
                var a = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
                if (a != null) return a;
            }
            return null;
        }

        static PhysicsMaterial BallPhysicsMaterial()
        {
            var m = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(BallPhysicsMatPath);
            if (m != null) return m;
            // Only for the test paddle: table bounces come from the ball's own flight model.
            m = new PhysicsMaterial("GameFeel_TestBall")
            {
                bounciness = 0.85f,
                bounceCombine = PhysicsMaterialCombine.Maximum,
                dynamicFriction = 0.2f,
                staticFriction = 0.2f,
            };
            AssetDatabase.CreateAsset(m, BallPhysicsMatPath);
            return m;
        }

        static GameObject MakePaddle(Transform parent, Vector3 pos, Material rubber, Material wood)
        {
            // Root name contains "paddle" so GameFeelBootstrap gives it PaddleGameFeel; the kinematic Rigidbody makes
            // PingPongBall report the contact as a paddle hit.
            var root = new GameObject("Test Paddle");
            root.transform.SetParent(parent, false);
            root.transform.position = pos;
            root.transform.rotation = Quaternion.Euler(-25f, 0f, 0f); // tilted so the ball pops up and back
            var rb = root.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            var col = root.AddComponent<BoxCollider>();
            col.size = new Vector3(0.17f, 0.17f, 0.02f);
            var face = StadiumGenerator.Cube("Test Paddle rubber", root.transform, Vector3.zero, new Vector3(0.17f, 0.17f, 0.014f), rubber, false, false);
            var handle = StadiumGenerator.Cube("Test Paddle Handle", root.transform, new Vector3(0f, -0.13f, 0f), new Vector3(0.03f, 0.1f, 0.025f), wood, false, false);
            GameObjectUtility.SetStaticEditorFlags(face, 0); // it flashes, so keep it out of static batching
            GameObjectUtility.SetStaticEditorFlags(handle, 0);
            return root;
        }

        static TestBallLauncher MakeLauncher(Transform parent, string name, Vector3 muzzle, Transform target, Material m)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = muzzle;
            if (target != null)
            {
                Vector3 d = target.position - muzzle;
                d.y = 0f;
                go.transform.rotation = Quaternion.LookRotation(d.sqrMagnitude > 0.001f ? d : Vector3.forward);
            }
            // visuals only (no colliders, so the ball doesn't hit the launcher)
            StadiumGenerator.Cube("Barrel", go.transform, new Vector3(0f, 0f, -0.2f), new Vector3(0.09f, 0.09f, 0.3f), m, false, false);
            StadiumGenerator.Cube("Launcher Stand", parent, new Vector3(muzzle.x, (muzzle.y - 0.06f) * 0.5f, muzzle.z) - parent.position,
                new Vector3(0.06f, muzzle.y - 0.06f, 0.06f), m, false, false);
            var l = go.AddComponent<TestBallLauncher>();
            l.target = target;
            return l;
        }

        /// <summary>A small stand for stations without a table, so they don't look empty.</summary>
        static void Plinth(Transform parent, float x, Material m)
        {
            StadiumGenerator.Cube("Plinth", parent, new Vector3(0f, 0.4f, 0.5f), new Vector3(1.2f, 0.8f, 0.8f), m, false, false);
        }

        /// <summary>The pad you stand on (with its TestStation) and the camera view for this station.</summary>
        static Transform Spot(Transform parent, float x, TestStation.Demo demo, int number, string title, string description, Vector3 lookAt, Material pad)
        {
            var go = new GameObject("Spot " + title.Trim());
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(x, 0f, PadZ);
            var st = go.AddComponent<TestStation>();
            st.demo = demo;
            st.number = number;
            st.title = title;
            st.description = description;
            st.radius = 1.6f;
            StadiumGenerator.Cube("Stand Here", go.transform, new Vector3(0f, 0.004f, 0f), new Vector3(1.0f, 0.006f, 1.0f), pad, false, false);

            var view = new GameObject("View").transform;
            view.SetParent(go.transform, false);
            Vector3 eye = new Vector3(x, 1.6f, PadZ - 0.4f);
            view.SetPositionAndRotation(eye, Quaternion.LookRotation(lookAt - eye));
            st.view = view;
            return view;
        }

        /// <summary>A post with a pixel-text sign behind the station, facing the camera.</summary>
        static void Sign(Transform parent, string text, float x, Material post, Material board, Material lit, float y = 2.9f)
        {
            var root = StadiumGenerator.Group(parent, "Sign " + text);
            root.position = new Vector3(x, 0f, 3.2f);
            const float px = 0.05f;
            text = text.ToUpperInvariant();
            int cols = text.Length * 6 - 1;
            float w = cols * px + 0.3f, h = PixelFont.Height * px + 0.25f;
            StadiumGenerator.Cube("Post", root, new Vector3(0f, y * 0.5f, 0.05f), new Vector3(0.06f, y, 0.06f), post, false, true);
            StadiumGenerator.Cube("Board", root, new Vector3(0f, y, 0f), new Vector3(w, h, 0.04f), board, false, true);
            float x0 = -cols * px * 0.5f + px * 0.5f;
            for (int ci = 0; ci < text.Length; ci++)
            {
                var g = PixelFont.Glyph(text[ci]);
                for (int r = 0; r < PixelFont.Height; r++)
                for (int c = 0; c < PixelFont.Width; c++)
                {
                    if (g[r][c] != '#') continue;
                    StadiumGenerator.Cube("Px", root, new Vector3(x0 + (ci * 6 + c) * px, y + (3 - r) * px, -0.03f),
                        new Vector3(px * 0.85f, px * 0.85f, 0.02f), lit, false, false);
                }
            }
        }
    }
}

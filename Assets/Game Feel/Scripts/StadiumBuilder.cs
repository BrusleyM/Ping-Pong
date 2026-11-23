using UnityEngine;

namespace PingPong.GameFeel
{
    /// <summary>
    /// Settings for the cube stadium. The stadium itself is generated in the Editor with the "Build / Rebuild Stadium"
    /// button in this component's Inspector (or Tools > Game Feel > Show Table, Hide Room, Rebuild Stadium) (see Assets/Game Feel/Editor/StadiumGenerator.cs), centred on the project's
    /// table. At play time it only switches visibility: the stadium hides while passthrough is on (you see your real
    /// room), and the scene's own environment (neon room, floor) hides while the stadium is showing.
    /// </summary>
    [DisallowMultipleComponent]
    public class StadiumBuilder : MonoBehaviour
    {
        [Header("Placement")]
        [Tooltip("Centre on the scene's PingPongTable. Off: centred on this object (the test scene's line of stations).")]
        public bool centerOnTable = true;
        [Tooltip("Without a VR rig in the scene, the court goes at the table's feet, then at the top of this object. With a rig, it goes at the player's floor.")]
        public string floorObjectNamed = "Floor";

        [Header("Play time")]
        [Tooltip("Hide the stadium while passthrough is on.")]
        public bool hideInPassthrough = true;
        [Tooltip("Objects of the scene's own environment to hide while the stadium shows (renderers only; colliders stay).")]
        public string[] hideWhileShown = { "experimental_neon_room", "Floor" };

        [Header("Court")]
        [Tooltip("X = width across the table, Y = length along the table (players stand at the Y ends).")]
        public Vector2 courtSize = new Vector2(14f, 18f);
        public float barrierHeight = 0.9f;
        public float walkwayWidth = 1f;

        [Header("Stands")]
        [Range(2, 12)] public int rows = 7;
        public float rowDepth = 0.9f;
        public float rowHeight = 0.45f;
        public float seatSpacing = 0.8f;
        [Range(0f, 1f)] public float crowdDensity = 0.65f;
        public int randomSeed = 7;

        [Header("Extras")]
        public bool canopy = true;
        public bool lightTowers = true;
        public bool scoreboard = true;
        [Tooltip("A second scoreboard at the near end, facing the other player.")]
        public bool scoreboardBothEnds = true;
        public string scoreboardText = "PING PONG";

        [Header("Colours")]
        public Color groundColor = new Color(0.06f, 0.07f, 0.09f);
        public Color courtColor = new Color(0.07f, 0.18f, 0.42f);
        public Color lineColor = new Color(0.95f, 0.95f, 0.95f);
        public Color standColor = new Color(0.2f, 0.21f, 0.24f);
        public Color seatColorA = new Color(0.85f, 0.14f, 0.16f);
        public Color seatColorB = new Color(0.12f, 0.38f, 0.92f);
        public Color[] adBoardColors =
        {
            new Color(1f, 0.55f, 0.05f),
            new Color(0.1f, 0.85f, 0.6f),
            new Color(0.95f, 0.2f, 0.55f),
            new Color(0.25f, 0.6f, 1f)
        };
        public Color[] crowdColors =
        {
            new Color(0.85f, 0.14f, 0.16f),
            new Color(0.12f, 0.38f, 0.92f),
            new Color(0.95f, 0.95f, 0.95f),
            new Color(0.98f, 0.8f, 0.15f),
            new Color(0.15f, 0.15f, 0.17f),
            new Color(0.2f, 0.75f, 0.35f)
        };
        public Color skinColor = new Color(0.86f, 0.68f, 0.52f);
        public Color lightColor = new Color(1f, 0.96f, 0.82f);
        public Color screenColor = new Color(0.02f, 0.03f, 0.08f);
        public Color screenTextColor = new Color(1f, 0.75f, 0.1f);

        // ---------------- Play time visibility ----------------

        const string GeneratedName = "Generated";

        PingPong.Gameplay.IPassthrough _passthrough;
        Transform _generated;
        readonly System.Collections.Generic.List<Renderer> _hidden = new System.Collections.Generic.List<Renderer>();
        bool? _shown;
        float _nextCheck;

        void Start()
        {
            _generated = transform.Find(GeneratedName);
            foreach (var mb in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            {
                if (mb is PingPong.Gameplay.IPassthrough p) { _passthrough = p; break; }
            }
            foreach (var root in gameObject.scene.GetRootGameObjects())
            {
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                {
                    if (t.IsChildOf(transform)) continue;
                    foreach (var n in hideWhileShown)
                    {
                        if (string.IsNullOrEmpty(n) || !string.Equals(t.name, n, System.StringComparison.OrdinalIgnoreCase)) continue;
                        foreach (var r in t.GetComponentsInChildren<Renderer>(true)) if (r.enabled) _hidden.Add(r);
                    }
                }
            }
            Apply();
        }

        void Update()
        {
            if (_shown == true && _hidden.Count > 0 && _hidden[0] != null && _hidden[0].enabled) _shown = null; // re-shown by someone else
            if (Time.unscaledTime < _nextCheck) return;
            _nextCheck = Time.unscaledTime + 0.25f;
            Apply();
        }

        void Apply()
        {
            bool show = !(hideInPassthrough && _passthrough != null && _passthrough.IsOn);
            if (_shown == show) return;
            _shown = show;
            if (_generated != null) _generated.gameObject.SetActive(show);
            // Stadium showing: hide the scene's own room. In passthrough the project hides that room itself
            // (MetaPassthrough), and turning passthrough off shows it again; the next check hides it once more.
            if (show) foreach (var r in _hidden) if (r != null) r.enabled = false;
        }

        void OnDestroy()
        {
            if (_shown == true) foreach (var r in _hidden) if (r != null) r.enabled = true;
        }
    }
}

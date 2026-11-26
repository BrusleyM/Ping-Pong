using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace PingPong.GameFeel
{
    /// <summary>
    /// Floating "+20 X2" labels above the ball whenever hit points are earned (HitPoints.Scored).
    /// Each label is bold white text (the game's own Fredoka font) on a dark plate in the scoring player's colour,
    /// so it reads against any background. Labels always face the player.
    /// Added automatically by GameFeelBootstrap.
    /// </summary>
    public class HitScorePopups : MonoBehaviour
    {
        [Tooltip("Height of the text in metres.")]
        public float textHeight = 0.16f;
        [Tooltip("Height above the ball where the label appears.")]
        public float spawnHeight = 0.15f;
        [Tooltip("How far the label floats up.")]
        public float riseDistance = 0.3f;
        public float lifetime = 1.2f;
        [Tooltip("Landing bonuses are shown this much bigger.")]
        public float bonusScale = 1.2f;
        [Tooltip("Space around the text inside the plate, in text heights.")]
        public Vector2 platePadding = new Vector2(0.35f, 0.2f);
        [Tooltip("How dark the plate is (0 = the player's colour, 1 = black).")]
        [Range(0f, 1f)] public float plateDarkness = 0.5f;
        [Range(0f, 1f)] public float plateOpacity = 0.96f;
        public Color textColor = Color.white;
        public Color bonusTextColor = new Color(1f, 0.85f, 0.3f);

        const int PoolSize = 8;
        const float MeasureFontSize = 10f;

        class Popup
        {
            public Transform root;
            public TextMeshPro text;
            public Transform plate;
            public Material plateMat;
            public Vector3 start;
            public float age;
            public float scale;
            public Color plateColor;
            public Color textColor;
            public bool active;
        }

        GameFeelBootstrap _cfg;
        HitPoints _points;
        readonly List<Popup> _pool = new List<Popup>();
        Camera _cam;
        int _next;

        void Start()
        {
            _cfg = GetComponent<GameFeelBootstrap>();
            _points = GetComponent<HitPoints>();
            var font = _cfg != null && _cfg.popupFont != null ? _cfg.popupFont : TMP_Settings.defaultFontAsset;
            var plateShader = Shader.Find("Universal Render Pipeline/Unlit");

            for (int i = 0; i < PoolSize; i++)
            {
                var root = new GameObject("Hit Score Popup " + i).transform;
                root.SetParent(transform, false);

                var plate = GameObject.CreatePrimitive(PrimitiveType.Quad);
                plate.name = "Plate";
                Destroy(plate.GetComponent<Collider>());
                plate.transform.SetParent(root, false);
                plate.transform.localPosition = new Vector3(0f, 0f, 0.002f); // just behind the text
                var mat = new Material(plateShader) { name = "Popup Plate (runtime)" };
                MakeTransparent(mat);
                var pr = plate.GetComponent<MeshRenderer>();
                pr.sharedMaterial = mat;
                pr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                pr.receiveShadows = false;

                var textGo = new GameObject("Text");
                textGo.transform.SetParent(root, false);
                var tmp = textGo.AddComponent<TextMeshPro>();
                if (font != null) tmp.font = font;
                tmp.fontSize = MeasureFontSize;
                tmp.fontStyle = FontStyles.Bold;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.textWrappingMode = TextWrappingModes.NoWrap;
                tmp.rectTransform.sizeDelta = new Vector2(40f, 10f);
                tmp.renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

                root.gameObject.SetActive(false);
                _pool.Add(new Popup { root = root, text = tmp, plate = plate.transform, plateMat = mat });
            }

            if (_points != null) _points.Scored += OnHitScored;
        }

        static void MakeTransparent(Material m)
        {
            // URP Unlit, transparent, alpha blended, drawn on top of the scene's opaque objects.
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetFloat("_ZWrite", 0f);
            m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }

        void OnDestroy()
        {
            if (_points != null) _points.Scored -= OnHitScored;
            foreach (var p in _pool) if (p.plateMat != null) Destroy(p.plateMat);
        }

        void OnHitScored(int side, int points, int multiplier, bool bonus, Vector3 where)
        {
            string text = "+" + points;
            if (multiplier > 1) text += "  X" + multiplier;
            Color team = _cfg == null ? new Color(0.2f, 0.3f, 0.5f) : _cfg.TeamColor(side);
            Show(text, team, where + Vector3.up * spawnHeight, bonus ? bonusScale : 1f, bonus ? bonusTextColor : textColor);
        }

        /// <summary>Show any short label above a point in the world, on a plate in 'color'.</summary>
        public void Show(string text, Color color, Vector3 position, float scale = 1f) => Show(text, color, position, scale, textColor);

        public void Show(string text, Color color, Vector3 position, float scale, Color labelColor)
        {
            if (_pool.Count == 0) return;
            var p = _pool[_next];
            _next = (_next + 1) % _pool.Count;

            // The label must be active for TextMeshPro to build its mesh and measure it.
            p.root.gameObject.SetActive(true);
            p.text.text = text;
            p.text.ForceMeshUpdate(true, true);
            Bounds b = p.text.textBounds;
            float h = b.size.y;
            if (!(h > MeasureFontSize * 0.02f) || h > MeasureFontSize * 10f)
            {
                // Not measured (font still loading): use a typical size so the label never covers the view.
                b = new Bounds(Vector3.zero, new Vector3(text.Length * MeasureFontSize * 0.06f, MeasureFontSize * 0.1f, 0f));
                h = b.size.y;
            }
            // Scale the text so its height is textHeight; size the plate around it.
            float k = textHeight / h;
            p.text.transform.localScale = Vector3.one * k;
            p.text.transform.localPosition = new Vector3(-b.center.x * k, -b.center.y * k, 0f);
            float w = b.size.x * k + textHeight * platePadding.x * 2f;
            float ph = textHeight * (1f + platePadding.y * 2f);
            p.plate.localScale = new Vector3(w, ph, 1f);

            p.plateColor = Color.Lerp(color, Color.black, plateDarkness);
            p.textColor = labelColor;
            p.start = position;
            p.age = 0f;
            p.scale = scale;
            p.active = true;
            Place(p);
        }

        void Update()
        {
            for (int i = 0; i < _pool.Count; i++)
            {
                var p = _pool[i];
                if (!p.active) continue;
                p.age += Time.deltaTime;
                if (p.age >= lifetime)
                {
                    p.active = false;
                    p.root.gameObject.SetActive(false);
                    continue;
                }
                Place(p);
            }
        }

        void Place(Popup p)
        {
            float u = Mathf.Clamp01(p.age / lifetime);
            // Rise quickly then slow down; pop in big, settle, fade at the end.
            float rise = 1f - (1f - u) * (1f - u);
            float pop = u < 0.12f ? Mathf.Lerp(1.35f, 1f, u / 0.12f) : 1f;
            float fade = u > 0.75f ? 1f - (u - 0.75f) / 0.25f : 1f;

            p.root.position = p.start + Vector3.up * (riseDistance * rise);
            p.root.localScale = Vector3.one * (p.scale * pop);

            if (_cam == null || !_cam.isActiveAndEnabled) _cam = Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>();
            if (_cam != null)
            {
                Vector3 away = p.root.position - _cam.transform.position;
                if (away.sqrMagnitude > 0.0001f) p.root.rotation = Quaternion.LookRotation(away, Vector3.up);
            }

            Color pc = p.plateColor;
            pc.a = plateOpacity * fade;
            p.plateMat.SetColor("_BaseColor", pc);
            Color tc = p.textColor;
            tc.a = fade;
            p.text.color = tc;
        }
    }
}

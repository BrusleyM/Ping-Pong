using System.Collections.Generic;
using UnityEngine;

namespace PingPong.GameFeel
{
    /// <summary>
    /// Added to each paddle at runtime by GameFeelBootstrap (PaddleModel objects, or Rigidbodies named "paddle").
    /// Flashes the paddle when it hits the ball.
    /// </summary>
    [DisallowMultipleComponent]
    public class PaddleGameFeel : MonoBehaviour
    {
        const float FlashDuration = 0.15f;

        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int ColorId = Shader.PropertyToID("_Color");

        GameFeelBootstrap _cfg;

        readonly List<Renderer> _renderers = new List<Renderer>();
        readonly List<Color> _baseColors = new List<Color>();
        readonly List<int> _colorProps = new List<int>();
        MaterialPropertyBlock _mpb;

        float _flashT = 1f;
        float _flashAmount;

        static readonly List<PaddleGameFeel> All = new List<PaddleGameFeel>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => All.Clear();

        void OnEnable() => All.Add(this);

        /// <summary>Flashes the paddle closest to a hit (within 'range' metres).</summary>
        public static void FlashNearest(Vector3 point, float intensity, float range = 0.6f)
        {
            PaddleGameFeel best = null;
            float bestD = range * range;
            foreach (var p in All)
            {
                if (p == null || !p.isActiveAndEnabled) continue;
                float d = (p.Center - point).sqrMagnitude;
                if (d < bestD) { bestD = d; best = p; }
            }
            if (best != null) best.Flash(intensity);
        }

        Vector3 Center
        {
            get
            {
                if (_renderers.Count > 0 && _renderers[0] != null) return _renderers[0].bounds.center;
                return transform.position;
            }
        }

        public void Init(GameFeelBootstrap cfg)
        {
            _cfg = cfg;
            _mpb = new MaterialPropertyBlock();

            // Flash the rubber faces only; fall back to every renderer if none are named "rubber".
            var all = GetComponentsInChildren<MeshRenderer>(true);
            foreach (var r in all)
            {
                if (GameFeelBootstrap.NameContains(r.name, "rubber")) TryAdd(r);
            }
            if (_renderers.Count == 0)
            {
                foreach (var r in all) TryAdd(r);
            }
        }

        void TryAdd(Renderer r)
        {
            var mat = r.sharedMaterial;
            if (mat == null) return;
            int prop;
            if (mat.HasProperty(BaseColorId)) prop = BaseColorId;
            else if (mat.HasProperty(ColorId)) prop = ColorId;
            else return;
            _renderers.Add(r);
            _baseColors.Add(mat.GetColor(prop));
            _colorProps.Add(prop);
        }

        public void Flash(float intensity)
        {
            if (_renderers.Count == 0) return;
            _flashAmount = Mathf.Lerp(0.25f, 0.85f, intensity);
            _flashT = 0f;
        }

        void Update()
        {
            if (_flashT >= 1f) return;

            _flashT = Mathf.Min(1f, _flashT + Time.deltaTime / FlashDuration);
            float k = _flashAmount * (1f - _flashT) * (1f - _flashT);
            Color target = _cfg != null ? _cfg.flashColor : Color.white;

            for (int i = 0; i < _renderers.Count; i++)
            {
                var r = _renderers[i];
                if (r == null) continue;
                if (_flashT >= 1f)
                {
                    r.SetPropertyBlock(null);
                    continue;
                }
                _mpb.Clear();
                _mpb.SetColor(_colorProps[i], Color.Lerp(_baseColors[i], target, k));
                r.SetPropertyBlock(_mpb);
            }
        }

        void OnDisable()
        {
            All.Remove(this);
            foreach (var r in _renderers)
            {
                if (r != null) r.SetPropertyBlock(null);
            }
            _flashT = 1f;
        }
    }
}

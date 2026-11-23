using System.Collections;
using UnityEngine;

namespace PingPong.GameFeel
{
    /// <summary>
    /// Shared effects used by BallGameFeel: one pooled spark particle system,
    /// a small pool of table-marker rings, hit impacts and confetti.
    /// Created automatically by GameFeelBootstrap.
    /// </summary>
    public class GameFeelEffects : MonoBehaviour
    {
        const int MarkerPoolSize = 6;
        const int MarkerSegments = 32;
        const float MarkerDuration = 0.4f;

        GameFeelBootstrap _cfg;
        ParticleSystem _sparks;

        LineRenderer[] _markers;
        float[] _markerAge;
        float[] _markerSize;
        int _nextMarker;


        // Hit impact
        const int ShockPoolSize = 6;
        const float ShockDuration = 0.18f;
        ParticleSystem _flash;
        LineRenderer[] _shocks;
        float[] _shockAge;
        float[] _shockSize;
        int _nextShock;

        // Confetti
        Transform _confettiRoot;

        public void Init(GameFeelBootstrap cfg)
        {
            _cfg = cfg;
            if (cfg.effectMaterial != null)
            {
                CreateSparks();
                CreateMarkers();
                CreateImpact();
            }
            var confettiMat = cfg.confettiMaterial != null ? cfg.confettiMaterial : cfg.effectMaterial;
            if (confettiMat != null) CreateConfetti(confettiMat);
        }

        // ---------------- Sparks ----------------

        void CreateSparks()
        {
            var go = new GameObject("GameFeel Sparks");
            go.transform.SetParent(transform, false);
            _sparks = go.AddComponent<ParticleSystem>();
            _sparks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = _sparks.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 1f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.12f, 0.3f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.6f, 2.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.004f, 0.009f);
            main.gravityModifier = 0.6f;
            main.maxParticles = 300;

            var emission = _sparks.emission;
            emission.enabled = false;

            var shape = _sparks.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 55f;
            shape.radius = 0.002f;

            var size = _sparks.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

            var col = _sparks.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            col.color = grad;

            var psr = go.GetComponent<ParticleSystemRenderer>();
            psr.sharedMaterial = _cfg.effectMaterial;
            psr.renderMode = ParticleSystemRenderMode.Billboard;
            psr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            psr.receiveShadows = false;
        }

        public void Sparks(Vector3 point, Vector3 normal, float intensity, Color color)
        {
            if (_sparks == null) return;
            _sparks.transform.SetPositionAndRotation(point, Quaternion.LookRotation(normal));
            var ep = new ParticleSystem.EmitParams
            {
                startColor = color,
                applyShapeToPosition = true
            };
            _sparks.Emit(ep, Mathf.RoundToInt(Mathf.Lerp(4f, 18f, intensity)));
        }

        // ---------------- Hit impact (flash + shockwave) ----------------

        void CreateImpact()
        {
            var go = new GameObject("GameFeel Impact Flash");
            go.transform.SetParent(transform, false);
            _flash = go.AddComponent<ParticleSystem>();
            _flash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = _flash.main;
            main.playOnAwake = false;
            main.loop = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = 0.1f;
            main.startSpeed = 0f;
            main.maxParticles = 20;
            var emission = _flash.emission;
            emission.enabled = false;
            var shape = _flash.shape;
            shape.enabled = false;
            var size = _flash.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.6f), new Keyframe(0.25f, 1f), new Keyframe(1f, 0f)));
            var psr = go.GetComponent<ParticleSystemRenderer>();
            psr.sharedMaterial = _cfg.effectMaterial;
            psr.renderMode = ParticleSystemRenderMode.Billboard;
            psr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            psr.receiveShadows = false;

            _shocks = new LineRenderer[ShockPoolSize];
            _shockAge = new float[ShockPoolSize];
            _shockSize = new float[ShockPoolSize];
            var points = new Vector3[MarkerSegments];
            for (int i = 0; i < MarkerSegments; i++)
            {
                float a = i / (float)MarkerSegments * Mathf.PI * 2f;
                points[i] = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
            }
            for (int i = 0; i < ShockPoolSize; i++)
            {
                var r = new GameObject("GameFeel Shockwave " + i);
                r.transform.SetParent(transform, false);
                var lr = r.AddComponent<LineRenderer>();
                lr.sharedMaterial = _cfg.effectMaterial;
                lr.useWorldSpace = false;
                lr.loop = true;
                lr.positionCount = MarkerSegments;
                lr.SetPositions(points);
                lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                lr.receiveShadows = false;
                lr.enabled = false;
                _shocks[i] = lr;
                _shockAge[i] = ShockDuration;
            }
        }

        /// <summary>Paddle hit: bright flash + shockwave ring + extra sparks, all scaled by hit strength.</summary>
        public void Impact(Vector3 point, Vector3 normal, float intensity, Color color)
        {
            bool smash = intensity >= _cfg.smashThreshold;
            if (_flash != null)
            {
                var ep = new ParticleSystem.EmitParams
                {
                    position = point,
                    startColor = Color.Lerp(color, Color.white, smash ? 0.7f : 0.4f),
                    startSize = Mathf.Lerp(0.06f, 0.3f, intensity) * (smash ? 1.6f : 1f),
                    startLifetime = smash ? 0.14f : 0.09f
                };
                _flash.Emit(ep, 1);
            }

            if (_shocks != null)
            {
                int i = _nextShock;
                _nextShock = (_nextShock + 1) % ShockPoolSize;
                var lr = _shocks[i];
                lr.transform.SetPositionAndRotation(point, Quaternion.LookRotation(normal));
                _shockAge[i] = 0f;
                _shockSize[i] = Mathf.Lerp(0.1f, 0.35f, intensity) * (smash ? 1.5f : 1f);
                lr.widthMultiplier = smash ? 0.012f : 0.007f;
                lr.enabled = true;
                UpdateShock(i, color);
            }

            // Extra sparks on top of the normal contact sparks.
            if (_sparks != null) Sparks(point, normal, Mathf.Clamp01(intensity * (smash ? 1.6f : 1.1f)), color);
        }

        void UpdateShock(int i, Color color)
        {
            float t = Mathf.Clamp01(_shockAge[i] / ShockDuration);
            float ease = 1f - (1f - t) * (1f - t) * (1f - t);
            float r = Mathf.Lerp(0.02f, _shockSize[i], ease);
            _shocks[i].transform.localScale = new Vector3(r, r, 1f);
            color.a = 1f - t;
            _shocks[i].startColor = color;
            _shocks[i].endColor = color;
        }

        // ---------------- Confetti ----------------

        void CreateConfetti(Material mat)
        {
            var root = new GameObject("GameFeel Confetti");
            root.transform.SetParent(transform, false);
            _confettiRoot = root.transform;

            // Discrete random colours (Fixed gradient + RandomColor picks one of the keys).
            var colors = _cfg.confettiColors != null && _cfg.confettiColors.Length > 0 ? _cfg.confettiColors : new[] { Color.white };
            var grad = new Gradient { mode = GradientMode.Fixed };
            int n = Mathf.Min(colors.Length, 8);
            var keys = new GradientColorKey[n];
            for (int i = 0; i < n; i++) keys[i] = new GradientColorKey(colors[i], (i + 1f) / n);
            grad.SetKeys(keys, new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });

            // Rain from above the court.
            var rain = MakeConfettiSystem("Rain", root.transform, mat, grad, 700, new Vector2(0.3f, 1.2f));
            rain.transform.localPosition = new Vector3(0f, 7.2f, 0f);
            rain.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // emit downwards, box Y runs along world Z
            var rs = rain.shape;
            rs.shapeType = ParticleSystemShapeType.Box;
            rs.scale = new Vector3(12f, 16f, 3f); // box in local space (Z points down)

            // Four cannons around the table shooting up and inwards.
            foreach (var c in new[] { new Vector3(-3f, 0, -4.5f), new Vector3(3f, 0, -4.5f), new Vector3(-3f, 0, 4.5f), new Vector3(3f, 0, 4.5f) })
            {
                var cannon = MakeConfettiSystem("Cannon", root.transform, mat, grad, 180, new Vector2(8f, 13f));
                cannon.transform.localPosition = new Vector3(c.x, 0.3f, c.z);
                Vector3 inward = new Vector3(-c.x, 0, -c.z).normalized;
                cannon.transform.localRotation = Quaternion.LookRotation((Vector3.up * 2.2f + inward).normalized);
                var cs = cannon.shape;
                cs.shapeType = ParticleSystemShapeType.Cone;
                cs.angle = 18f;
                cs.radius = 0.08f;
            }
        }

        ParticleSystem MakeConfettiSystem(string name, Transform parent, Material mat, Gradient colors, int count, Vector2 speed)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 1f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(6f, 9f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed.x, speed.y);
            main.gravityModifier = 0.12f;
            main.maxParticles = count + 50;
            main.startSize3D = true;
            main.startSizeX = new ParticleSystem.MinMaxCurve(0.07f, 0.11f);
            main.startSizeY = new ParticleSystem.MinMaxCurve(0.04f, 0.06f);
            main.startSizeZ = 1f;
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = new ParticleSystem.MinMaxGradient(colors) { mode = ParticleSystemGradientMode.RandomColor };

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });

            // Slow the cannon shots down so the pieces flutter instead of flying off.
            var limit = ps.limitVelocityOverLifetime;
            limit.enabled = true;
            limit.limit = 1.6f;
            limit.dampen = 0.08f;

            // Flutter: spin and drift.
            var rot = ps.rotationOverLifetime;
            rot.enabled = true;
            rot.z = new ParticleSystem.MinMaxCurve(-6f, 6f);
            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.5f;
            noise.frequency = 0.4f;
            noise.scrollSpeed = 0.3f;
            noise.quality = ParticleSystemNoiseQuality.Low;

            var col = ps.colorOverLifetime;
            col.enabled = true;
            var fade = new Gradient();
            fade.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.85f), new GradientAlphaKey(0f, 1f) });
            col.color = fade;

            var psr = go.GetComponent<ParticleSystemRenderer>();
            psr.sharedMaterial = mat;
            psr.renderMode = ParticleSystemRenderMode.Billboard;
            psr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            psr.receiveShadows = false;
            return ps;
        }

        /// <summary>Confetti rain over the court plus cannon bursts near the table.</summary>
        public void Confetti(Vector3 center, float intensity = 1f)
        {
            if (_confettiRoot == null) return;
            _confettiRoot.position = center;
            foreach (var ps in _confettiRoot.GetComponentsInChildren<ParticleSystem>())
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                ps.Play(true);
            }
        }

        /// <summary>Editor preview helper: show the confetti as it looks a moment after firing.</summary>
        public void SimulateConfetti(Vector3 center, float seconds)
        {
            if (_confettiRoot == null) return;
            _confettiRoot.position = center;
            foreach (var ps in _confettiRoot.GetComponentsInChildren<ParticleSystem>()) ps.Simulate(seconds, true, true, false);
        }

        /// <summary>Editor preview helper: freeze active shockwaves partway through so they can be seen.</summary>
        public void PreviewImpactPose(float fraction)
        {
            if (_shocks == null) return;
            for (int i = 0; i < ShockPoolSize; i++)
            {
                if (!_shocks[i].enabled) continue;
                _shockAge[i] = ShockDuration * fraction;
                UpdateShock(i, _cfg.impactColor);
            }
        }

        /// <summary>Editor preview helper: fast-forward all effect particles.</summary>
        public void SimulateParticles(float seconds)
        {
            foreach (var ps in GetComponentsInChildren<ParticleSystem>()) ps.Simulate(seconds, true, false, false);
        }

        // ---------------- Table markers ----------------

        void CreateMarkers()
        {
            _markers = new LineRenderer[MarkerPoolSize];
            _markerAge = new float[MarkerPoolSize];
            _markerSize = new float[MarkerPoolSize];

            var points = new Vector3[MarkerSegments];
            for (int i = 0; i < MarkerSegments; i++)
            {
                float a = i / (float)MarkerSegments * Mathf.PI * 2f;
                points[i] = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
            }

            for (int i = 0; i < MarkerPoolSize; i++)
            {
                var go = new GameObject("GameFeel Marker " + i);
                go.transform.SetParent(transform, false);
                var lr = go.AddComponent<LineRenderer>();
                lr.sharedMaterial = _cfg.effectMaterial;
                lr.useWorldSpace = false;
                lr.loop = true;
                lr.positionCount = MarkerSegments;
                lr.SetPositions(points);
                lr.widthMultiplier = 0.004f;
                lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                lr.receiveShadows = false;
                lr.enabled = false;
                _markers[i] = lr;
                _markerAge[i] = MarkerDuration;
            }
        }

        public void Marker(Vector3 point, Vector3 normal, float intensity)
        {
            if (_markers == null) return;
            int i = _nextMarker;
            _nextMarker = (_nextMarker + 1) % MarkerPoolSize;

            var lr = _markers[i];
            lr.transform.SetPositionAndRotation(point + normal * 0.002f, Quaternion.LookRotation(normal));
            _markerAge[i] = 0f;
            _markerSize[i] = Mathf.Lerp(0.05f, 0.11f, intensity);
            lr.enabled = true;
            UpdateMarker(i);
        }

        void Update()
        {
            if (_shocks != null)
            {
                for (int i = 0; i < ShockPoolSize; i++)
                {
                    if (_shockAge[i] >= ShockDuration) continue;
                    _shockAge[i] += Time.deltaTime;
                    if (_shockAge[i] >= ShockDuration) { _shocks[i].enabled = false; continue; }
                    UpdateShock(i, _shocks[i].startColor);
                }
            }
            if (_markers == null) return;
            for (int i = 0; i < MarkerPoolSize; i++)
            {
                if (_markerAge[i] >= MarkerDuration) continue;
                _markerAge[i] += Time.deltaTime;
                if (_markerAge[i] >= MarkerDuration)
                {
                    _markers[i].enabled = false;
                    continue;
                }
                UpdateMarker(i);
            }
        }

        void UpdateMarker(int i)
        {
            float t = Mathf.Clamp01(_markerAge[i] / MarkerDuration);
            float ease = 1f - (1f - t) * (1f - t);
            float radius = Mathf.Lerp(0.012f, _markerSize[i], ease);
            _markers[i].transform.localScale = new Vector3(radius, radius, 1f);
            Color c = _cfg.markerColor;
            c.a *= 1f - t;
            _markers[i].startColor = c;
            _markers[i].endColor = c;
        }
    }
}

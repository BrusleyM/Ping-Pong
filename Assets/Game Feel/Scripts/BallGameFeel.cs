using UnityEngine;
using PingPong.Gameplay;

namespace PingPong.GameFeel
{
    /// <summary>
    /// Added to the ball at runtime by GameFeelBootstrap. Listens to the ball's own Impact event (paddle, table, net,
    /// anything else) and adds sparks, squash, paddle flash, hit impact and table markers. Also drives the streak.
    /// Sound and controller vibration are the project's own (BallImpactAudio, PaddleHaptics).
    /// </summary>
    [DisallowMultipleComponent]
    public class BallGameFeel : MonoBehaviour
    {
        public enum ContactKind { Paddle, Table, Net, Other }

        /// <summary>Raised on every bounce: (ball, what it hit, contact point, paddle position if a paddle).</summary>
        public static event System.Action<BallGameFeel, ContactKind, Vector3, Vector3> Contact;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Contact = null;

        /// <summary>Impact speed (m/s) and strength (0..1) of the most recent bounce.</summary>
        public float LastImpactSpeed { get; private set; }
        public float LastImpactStrength { get; private set; }

        const float SquashDuration = 0.16f;

        GameFeelBootstrap _cfg;
        Rigidbody _rb;

        TrailRenderer _trail;
        TrailRenderer _core;
        Color _streakTint;
        Vector3 _lastPos;
        float _speed;
        float _diameter = 0.04f;
        Transform _visual;

        float _squashT = 1f;
        float _squashAmount;
        Vector3 _squashNormal = Vector3.up;


        /// <summary>Per-ball switches (null = everything on).</summary>
        public GameFeelBallOptions Options { get; private set; }

        /// <summary>The project's ball this feel belongs to.</summary>
        public PingPongBall Ball { get; private set; }

        public void Init(GameFeelBootstrap cfg, PingPongBall ball)
        {
            _cfg = cfg;
            Ball = ball;
            _rb = GetComponent<Rigidbody>();
            _diameter = BallDiameter();
            if (ball != null) ball.Impact += OnImpact;

            Options = GetComponent<GameFeelBallOptions>();
            bool streak = Options == null || Options.streak;
            if (streak && (cfg.ballStreak || cfg.ballVisuals) && cfg.effectMaterial != null) SetupTrail();
            if (cfg.squashAndFlash) SetupVisual();
        }

        void OnDestroy()
        {
            if (Ball != null) Ball.Impact -= OnImpact;
        }

        float BallRadius => _diameter * 0.5f;

        /// <summary>Which end of this ball's table a point is on (South = Near, North = Far).</summary>
        public int SideOf(Vector3 world) => GameFeelTable.SideOf(Ball != null ? Ball.Table : GameFeelTable.Main, world);

        void SetupTrail()
        {
            _diameter = BallDiameter();
            _trail = MakeTrail(gameObject);
            if (_cfg.streakCore)
            {
                var core = new GameObject("Streak Core");
                core.transform.SetParent(transform, false);
                _core = MakeTrail(core);
            }
            _streakTint = _cfg.streakColor;
            _lastPos = transform.position;
        }

        TrailRenderer MakeTrail(GameObject go)
        {
            var t = go.AddComponent<TrailRenderer>();
            t.sharedMaterial = _cfg.effectMaterial;
            t.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            t.receiveShadows = false;
            t.minVertexDistance = 0.004f;
            t.time = _cfg.streakTime.x;
            t.widthCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(0.35f, 0.75f), new Keyframe(1f, 0f));
            t.numCapVertices = 3;
            t.emitting = false;
            return t;
        }

        void SetupVisual()
        {
            // Squash a child copy of the mesh instead of the ball itself,
            // so the collider and any networked transform keep their size.
            var mf = GetComponent<MeshFilter>();
            var mr = GetComponent<MeshRenderer>();
            if (mf == null || mr == null || mf.sharedMesh == null) return;

            var go = new GameObject("GameFeel Visual");
            go.layer = gameObject.layer;
            _visual = go.transform;
            _visual.SetParent(transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = mf.sharedMesh;
            var vr = go.AddComponent<MeshRenderer>();
            vr.sharedMaterials = mr.sharedMaterials;
            vr.shadowCastingMode = mr.shadowCastingMode;
            vr.receiveShadows = mr.receiveShadows;
            mr.enabled = false;
        }

        float BallDiameter()
        {
            var sc = GetComponent<SphereCollider>();
            Vector3 s = transform.lossyScale;
            float scale = Mathf.Max(Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z));
            return sc != null ? sc.radius * 2f * scale : 0.04f;
        }

        void Update()
        {
            if (_trail == null) return;

            // Speed from actual movement (works on remote clients where the Rigidbody may be kinematic).
            Vector3 pos = transform.position;
            float dt = Time.deltaTime;
            if (dt > 0f)
            {
                float jump = (pos - _lastPos).magnitude;
                if (jump > 1.0f)
                {
                    // Teleport / respawn: don't draw a streak across the court.
                    _trail.Clear();
                    if (_core != null) _core.Clear();
                }
                else _speed = Mathf.Lerp(_speed, jump / dt, 0.5f);
            }
            _lastPos = pos;

            float k = _cfg.Intensity(_speed);
            bool on = (_cfg.ballStreak || _cfg.ballVisuals) && _speed > _cfg.streakMinSpeed;
            float time = Mathf.Lerp(_cfg.streakTime.x, _cfg.streakTime.y, k);

            _trail.emitting = on;
            _trail.time = time;
            _trail.widthMultiplier = _diameter * _cfg.streakWidth;
            Color c = _streakTint;
            c.a = Mathf.Lerp(0.55f, 1f, k);
            _trail.startColor = c * 1.3f;
            c.a = 0f;
            _trail.endColor = c;

            if (_core != null)
            {
                _core.emitting = on;
                _core.time = time * 0.6f;
                _core.widthMultiplier = _diameter * _cfg.streakWidth * 0.35f;
                _core.startColor = new Color(1f, 1f, 1f, Mathf.Lerp(0.5f, 0.9f, k));
                _core.endColor = new Color(1f, 1f, 1f, 0f);
            }
        }

        void LateUpdate()
        {
            if (_visual == null || _squashT >= 1f) return;

            _squashT = Mathf.Min(1f, _squashT + Time.deltaTime / SquashDuration);
            float t = _squashT;
            // Quick squash, small overshoot, settle.
            float s = _squashAmount * Mathf.Cos(t * Mathf.PI * 2.5f) * (1f - t) * (1f - t);

            if (_squashT >= 1f)
            {
                _visual.localRotation = Quaternion.identity;
                _visual.localScale = Vector3.one;
                return;
            }

            _visual.rotation = Quaternion.LookRotation(_squashNormal);
            _visual.localScale = new Vector3(1f + s * 0.5f, 1f + s * 0.5f, 1f - s);
        }

        void OnImpact(BallImpact impact)
        {
            if (_cfg == null) return;

            bool isPaddle = impact.Kind == ImpactKind.Paddle;
            bool isTable = impact.Kind == ImpactKind.Table;
            float speed = impact.Speed;
            float k = _cfg.Intensity(speed);
            LastImpactSpeed = speed;
            LastImpactStrength = k;

            Vector3 point = impact.Position;
            Vector3 normal = Vector3.up;
            Vector3 v = _rb != null && !_rb.isKinematic ? _rb.linearVelocity : Vector3.zero;
            if (isPaddle && v.sqrMagnitude > 0.01f) normal = v.normalized;          // the ball leaves along the face normal
            else if (impact.Kind == ImpactKind.Net && v.sqrMagnitude > 0.01f) normal = -v.normalized;
            if (isTable) point -= Vector3.up * BallRadius;                          // the contact is under the ball

            ContactKind kind = impact.Kind switch
            {
                ImpactKind.Paddle => ContactKind.Paddle,
                ImpactKind.Table => ContactKind.Table,
                ImpactKind.Net => ContactKind.Net,
                _ => ContactKind.Other,
            };
            Contact?.Invoke(this, kind, point, impact.Position);

            if (isPaddle && _cfg.streakTeamColors) _streakTint = _cfg.TeamColor(SideOf(impact.Position));
            if (!isPaddle && Options != null && Options.onlyPaddleEffects) return;

            if (_cfg.ballVisuals && k > 0.1f)
                _cfg.Effects.Sparks(point, normal, k, isPaddle ? _cfg.paddleSparkColor : _cfg.sparkColor);

            if (_cfg.bigHitImpact && isPaddle)
                _cfg.Effects.Impact(point, normal, k, _cfg.impactColor);

            if (_cfg.squashAndFlash)
            {
                StartSquash(normal, Mathf.Lerp(0.06f, _cfg.maxSquash, k));
                if (isPaddle) PaddleGameFeel.FlashNearest(impact.Position, k);
            }

            if (_cfg.tableMarkers && isTable)
                _cfg.Effects.Marker(point, Vector3.up, k);
        }

        void StartSquash(Vector3 normal, float amount)
        {
            if (_visual == null) return;
            _squashNormal = normal;
            _squashAmount = amount;
            _squashT = 0f;
        }
    }
}

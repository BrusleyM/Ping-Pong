using PingPong.Core;
using PingPong.Gameplay;
using UnityEngine;

namespace PingPong.GameFeel
{
    /// <summary>
    /// Shows a soft ring on the table where the ball is about to bounce (helps with depth in VR).
    /// Uses the ball's own flight model (BallFlightModel.Simulate: gravity, drag and the table), so the ring matches
    /// what the ball really does. The ring grows brighter and tighter as the bounce gets closer and is coloured by
    /// whoever hit the ball last. Added to each ball at runtime by GameFeelBootstrap.
    /// </summary>
    [DisallowMultipleComponent]
    public class LandingPredictor : MonoBehaviour
    {
        [Tooltip("Only show the ring when the bounce is at most this many seconds away.")]
        public float lookAhead = 0.9f;
        [Tooltip("Ball slower than this (m/s) shows no prediction.")]
        public float minSpeed = 1.0f;
        public float ringRadiusFar = 0.11f;
        public float ringRadiusNear = 0.045f;

        const int Segments = 28;

        GameFeelBootstrap _cfg;
        PingPongBall _ball;
        Rigidbody _rb;
        LineRenderer _ring, _dot;
        Color _color = Color.white;
        float _alpha;
        readonly BallBounce[] _bounces = new BallBounce[1];

        public void Init(GameFeelBootstrap cfg, PingPongBall ball)
        {
            _cfg = cfg;
            _ball = ball;
            _rb = GetComponent<Rigidbody>();
            _ring = MakeRing("Landing Ring", 0.006f);
            _dot = MakeRing("Landing Dot", 0.012f);
            BallGameFeel.Contact += OnContact;
        }

        void OnDestroy()
        {
            BallGameFeel.Contact -= OnContact;
            if (_ring != null) Destroy(_ring.gameObject);
            if (_dot != null) Destroy(_dot.gameObject);
        }

        LineRenderer MakeRing(string name, float width)
        {
            var go = new GameObject(name);
            // Not parented to the ball: it lives on the table.
            var lr = go.AddComponent<LineRenderer>();
            lr.sharedMaterial = _cfg != null ? _cfg.effectMaterial : null;
            lr.useWorldSpace = false;
            lr.loop = true;
            lr.positionCount = Segments;
            var pts = new Vector3[Segments];
            for (int i = 0; i < Segments; i++)
            {
                float a = i / (float)Segments * Mathf.PI * 2f;
                pts[i] = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
            }
            lr.SetPositions(pts);
            lr.widthMultiplier = width;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
            lr.alignment = LineAlignment.View;
            lr.enabled = false;
            return lr;
        }

        void OnContact(BallGameFeel ball, BallGameFeel.ContactKind kind, Vector3 point, Vector3 paddlePos)
        {
            if (ball == null || ball.gameObject != gameObject || kind != BallGameFeel.ContactKind.Paddle || _cfg == null) return;
            _color = _cfg.TeamColor(ball.SideOf(paddlePos));
        }

        void Update()
        {
            if (_ring == null || _ball == null || _ball.Table == null || _rb == null) return;

            float dt = Time.deltaTime;
            Vector3 vel = _rb.isKinematic ? Vector3.zero : _rb.linearVelocity;
            bool show = false;
            float timeToLand = 0f;
            Vector3 hit = Vector3.zero;
            TableFrame frame = _ball.Table.Frame;
            if (vel.magnitude > minSpeed)
            {
                int n = _ball.Model.Simulate(frame, transform.position, vel, lookAhead, _bounces, out _);
                if (n > 0)
                {
                    show = true;
                    hit = _bounces[0].Position;
                    timeToLand = _bounces[0].Time;
                }
            }

            float target = show ? 1f : 0f;
            _alpha = Mathf.MoveTowards(_alpha, target, dt * (show ? 8f : 5f));
            if (_alpha <= 0.001f) { _ring.enabled = _dot.enabled = false; return; }

            _ring.enabled = _dot.enabled = true;
            if (show)
            {
                // The bounce position is the ball's centre at contact: drop it onto the surface.
                Vector3 local = frame.ToLocal(hit);
                local.y = 0.002f;
                Vector3 p = frame.ToWorld(local);
                Quaternion rot = GameFeelTable.Rotation(frame);
                float k = 1f - Mathf.Clamp01(timeToLand / lookAhead); // 0 far .. 1 about to land
                float r = Mathf.Lerp(ringRadiusFar, ringRadiusNear, k);
                _ring.transform.SetPositionAndRotation(p, rot);
                _ring.transform.localScale = new Vector3(r, 1f, r);
                _dot.transform.SetPositionAndRotation(p, rot);
                _dot.transform.localScale = new Vector3(0.006f, 1f, 0.006f);
                float a = _alpha * Mathf.Lerp(0.35f, 1f, k);
                SetColor(_ring, a);
                SetColor(_dot, a * 0.9f);
            }
            else
            {
                SetColor(_ring, _alpha * 0.5f);
                SetColor(_dot, _alpha * 0.4f);
            }
        }

        void SetColor(LineRenderer lr, float a)
        {
            Color c = _color * 1.4f;
            c.a = a;
            lr.startColor = lr.endColor = c;
        }
    }
}

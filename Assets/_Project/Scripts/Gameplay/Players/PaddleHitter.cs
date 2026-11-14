using System;
using PingPong.Core;
using UnityEngine;
using UnityEngine.Serialization;

namespace PingPong.Gameplay
{
    /// <summary>A paddle hit, for feedback. Velocity is the ball's outgoing velocity, m/s.</summary>
    public readonly struct PaddleHit
    {
        public readonly Side Player;
        public readonly Vector3 Position;
        public readonly Vector3 Velocity;

        public PaddleHit(Side player, Vector3 position, Vector3 velocity)
        {
            Player = player;
            Position = position;
            Velocity = velocity;
        }
    }

    /// <summary>
    /// Detects paddle-ball contact with a swept test between physics steps (fast swings can't tunnel)
    /// and computes the return from the paddle's real motion. Unity's own paddle-ball collisions are
    /// disabled so they don't fight this.
    /// </summary>
    public sealed class PaddleHitter : MonoBehaviour
    {
        [FormerlySerializedAs("_playerIndex")]
        [SerializeField] Side _player = Side.Near;
        [SerializeField] MatchController _match;
        [SerializeField] PingPongBall _ball;
        [SerializeField] PaddleFeelProfile _feel;
        [Tooltip("The paddle's geometry (blade position and size).")]
        [SerializeField] PaddleModel _model;

        public Side Player { get => _player; set => _player = value; }
        /// <summary>Set false for a remote player's paddle: only the owner detects its hits.</summary>
        public bool DetectHits { get; set; } = true;

        /// <summary>Raised after this paddle hits the ball.</summary>
        public event Action<PaddleHit> Hit;

        const float ContactClearance = 0.004f; // the ball leaves just off the rubber
        const float MinFrameTime = 1e-4f;
        const float AssistPredictionTime = 2.5f;

        Vector3 _prevPos, _prevBall;
        Quaternion _prevRot;
        bool _hasPrev;
        float _lastHit = float.NegativeInfinity;
        readonly BallBounce[] _bounce = new BallBounce[1];

        // Blade pose sampled once per rendered frame, after the grab has moved it. The paddle moves at the
        // headset's frame rate, not at the physics rate, so velocity is taken over real frame time
        // (otherwise a frame with two physics steps reads one too-fast and one zero-speed step).
        Vector3 _vPos0, _vPos1;
        Quaternion _vRot0, _vRot1;
        float _vT0, _vT1;
        int _vSamples;

        Transform Blade => _model.Blade;
        PaddleFeel Feel => _feel.Feel;

        void OnValidate()
        {
            Validation.Require(_feel, "a Paddle Feel Profile", this);
            Validation.Require(_model, "the Paddle Model", this);
        }

        void Start()
        {
            var ballCollider = _ball.GetComponent<Collider>();
            if (ballCollider == null) return;
            foreach (var c in GetComponentsInChildren<Collider>(true)) Physics.IgnoreCollision(c, ballCollider, true);
        }

        void OnEnable() { _hasPrev = false; _vSamples = 0; }

        // Nobody can hit between a point and the next serve or after the game, so the hitter sleeps then (no 90 Hz
        // physics checks, no pose sampling). It listens from Awake to OnDestroy because it is switched off itself.
        void Awake()
        {
            _match.Changed += SyncActive;
            SyncActive();
        }

        void OnDestroy() => _match.Changed -= SyncActive;

        void SyncActive() => enabled = _match.Phase is not (MatchPhase.PointOver or MatchPhase.GameOver);

        void LateUpdate()
        {
            Transform b = Blade;
            float t = Time.time;
            if (_vSamples > 0 && t <= _vT1) return;
            _vPos0 = _vPos1; _vRot0 = _vRot1; _vT0 = _vT1;
            _vPos1 = b.position; _vRot1 = b.rotation; _vT1 = t;
            if (_vSamples < 2) _vSamples++;
        }

        void FixedUpdate()
        {
            Transform b = Blade;
            Vector3 pos = b.position;
            Quaternion rot = b.rotation;
            Vector3 ballPos = _ball.Body.position;

            if (_hasPrev && DetectHits && Time.time - _lastHit > Feel.Cooldown && _match.CanHit(_player))
                TryHit(pos, rot, ballPos);

            _prevPos = pos; _prevRot = rot; _prevBall = _ball.Body.position;
            _hasPrev = true;
        }

        void TryHit(Vector3 pos, Quaternion rot, Vector3 ballPos)
        {
            float ballRadius = _ball.Model.Radius;
            Vector3 n = _model.FaceNormalLocal;
            Vector3 l0 = Quaternion.Inverse(_prevRot) * (_prevBall - _prevPos);
            Vector3 l1 = Quaternion.Inverse(rot) * (ballPos - pos);
            float d0 = Vector3.Dot(l0, n), d1 = Vector3.Dot(l1, n);
            float reach = ballRadius + _model.BladeHalfThickness;

            float t;
            if (Mathf.Abs(d1) <= reach) t = 1f;                            // overlapping now
            else if (Mathf.Sign(d0) != Mathf.Sign(d1)) t = d0 / (d0 - d1); // passed through the blade
            else return;

            Vector3 lc = Vector3.Lerp(l0, l1, t);
            Vector3 inPlane = lc - Vector3.Dot(lc, n) * n;
            if (inPlane.magnitude > _model.BladeRadius + ballRadius) return;

            float side = Mathf.Abs(d0) > 1e-5f ? Mathf.Sign(d0) : Mathf.Sign(d1);
            Vector3 normal = (rot * n) * side;
            Vector3 contact = rot * inPlane + pos;
            if (!FrameVelocity(contact, out Vector3 paddleVel))
                paddleVel = (contact - (_prevRot * inPlane + _prevPos)) / Time.fixedDeltaTime;

            Vector3 ballVel = _ball.Body.isKinematic ? Vector3.zero : _ball.Body.linearVelocity;
            Vector3 rel = ballVel - paddleVel;
            float vn = Vector3.Dot(rel, normal);
            if (vn >= 0f) return; // separating

            PaddleFeel feel = Feel;
            Vector3 relOut = (rel - vn * normal) * feel.TangentialRetain - feel.Restitution * vn * normal;
            Vector3 outVel = paddleVel + relOut;

            Vector3 hitPos = contact + normal * (reach + ContactClearance);
            outVel = ApplyAssist(hitPos, outVel, feel.AimAssist);
            if (outVel.magnitude > feel.MaxSpeed) outVel = outVel.normalized * feel.MaxSpeed;

            _lastHit = Time.time;
            _match.Strike(_player, hitPos, outVel);
            Hit?.Invoke(new PaddleHit(_player, hitPos, outVel));
        }

        /// <summary>Velocity of a world point on the blade, from the last two rendered frames.</summary>
        bool FrameVelocity(Vector3 worldPoint, out Vector3 velocity)
        {
            velocity = Vector3.zero;
            float dt = _vT1 - _vT0;
            if (_vSamples < 2 || dt <= MinFrameTime) return false;
            Vector3 linear = (_vPos1 - _vPos0) / dt;
            (_vRot1 * Quaternion.Inverse(_vRot0)).ToAngleAxis(out float angle, out Vector3 axis);
            if (angle > 180f) angle -= 360f;
            Vector3 angular = float.IsFinite(axis.x) ? axis * (angle * Mathf.Deg2Rad / dt) : Vector3.zero;
            velocity = linear + Vector3.Cross(angular, worldPoint - _vPos1);
            return true;
        }

        /// <summary>Bends a rally shot towards a legal landing spot on the opponent's half.</summary>
        Vector3 ApplyAssist(Vector3 from, Vector3 vel, float assist)
        {
            PaddleFeel feel = Feel;
            if (assist <= 0f) return vel;
            MatchPhase phase = _match.Phase;
            if (phase == MatchPhase.WaitingForServe || phase == MatchPhase.FreePlay) return vel; // serves must bounce own side first

            TableFrame table = _match.Table.Frame;
            BallFlightModel model = _ball.Model;
            Side opp = _player.Opponent();
            Vector3 lv = table.DirToLocal(vel);
            if (lv.z * opp.Sign() < 1f) return vel; // not really going towards the opponent

            int n = model.Simulate(table, from, vel, AssistPredictionTime, _bounce, out bool net);
            if (n > 0 && _bounce[0].Side == opp && !net) return vel; // already good

            Vector3 landing;
            if (n > 0) landing = table.ToLocal(_bounce[0].Position);
            else
            {
                // Estimate where it would cross table height.
                Vector3 l0 = table.ToLocal(from);
                float flat = new Vector2(lv.x, lv.z).magnitude;
                landing = l0 + new Vector3(lv.x, 0, lv.z) * (Mathf.Abs(l0.z) + table.HalfLength) / Mathf.Max(flat, 0.1f);
            }
            Vector3 target = table.ClampToSide(opp, landing.x, Mathf.Abs(landing.z), feel.AssistMargins);
            float speed = Mathf.Clamp(new Vector2(lv.x, lv.z).magnitude, feel.AssistSpeed.x, feel.AssistSpeed.y);
            model.SolveLaunchClearingNet(table, from, target, speed, feel.AssistNetRetry, out Vector3 solved);
            return Vector3.Lerp(vel, solved, assist);
        }
    }
}

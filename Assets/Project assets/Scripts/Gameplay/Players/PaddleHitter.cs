using System;
using PingPong.Core;
using UnityEngine;
using UnityEngine.Serialization;

namespace PingPong.Gameplay
{
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
        [Tooltip("Vibrates the controller holding this paddle on each hit (optional).")]
        [SerializeField] Seam<IHaptics> _haptics;
        [Tooltip("Centre of the blade. Defaults to this transform.")]
        [SerializeField] Transform _blade;
        [Tooltip("Blade face normal in the blade transform's local space.")]
        [SerializeField] Vector3 _faceNormalLocal = Vector3.forward;
        [Tooltip("Blade radius and half-thickness, metres. Measured from the mesh by the scene setup.")]
        [SerializeField] float _bladeRadius = 0.08f;
        [SerializeField] float _bladeHalfThickness = 0.006f;

        public Side Player { get => _player; set => _player = value; }
        /// <summary>Set false for a remote player's paddle: only the owner detects its hits.</summary>
        public bool DetectHits { get; set; } = true;

        /// <summary>Raised after a hit. Arguments: hit position, outgoing velocity.</summary>
        public event Action<Vector3, Vector3> Hit;

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

        Transform Blade => _blade != null ? _blade : transform;
        PaddleFeel Feel => _feel.Feel;

        void OnValidate()
        {
            if (_feel == null) Debug.LogWarning($"{name}: assign a Paddle Feel Profile.", this);
            string error = _haptics.Validate(nameof(_haptics));
            if (error != null) Debug.LogError(error, this);
        }

        void Start()
        {
            var ballCollider = _ball.GetComponent<Collider>();
            if (ballCollider == null) return;
            foreach (var c in GetComponentsInChildren<Collider>(true)) Physics.IgnoreCollision(c, ballCollider, true);
        }

        void OnEnable() { _hasPrev = false; _vSamples = 0; }

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
            Vector3 n = _faceNormalLocal.normalized;
            Vector3 l0 = Quaternion.Inverse(_prevRot) * (_prevBall - _prevPos);
            Vector3 l1 = Quaternion.Inverse(rot) * (ballPos - pos);
            float d0 = Vector3.Dot(l0, n), d1 = Vector3.Dot(l1, n);
            float reach = ballRadius + _bladeHalfThickness;

            float t;
            if (Mathf.Abs(d1) <= reach) t = 1f;                            // overlapping now
            else if (Mathf.Sign(d0) != Mathf.Sign(d1)) t = d0 / (d0 - d1); // passed through the blade
            else return;

            Vector3 lc = Vector3.Lerp(l0, l1, t);
            Vector3 inPlane = lc - Vector3.Dot(lc, n) * n;
            if (inPlane.magnitude > _bladeRadius + ballRadius) return;

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
            _ball.Launch(hitPos, outVel);
            _haptics.Value?.Pulse(Mathf.Clamp01(outVel.magnitude / feel.HapticFullSpeed), feel.HapticDuration);
            Hit?.Invoke(hitPos, outVel);
            _match.ReportPaddleHit(_player, hitPos);
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
            const float edgeMargin = 0.1f, minDepth = 0.3f, endMargin = 0.12f, minSpeed = 3f, maxSpeed = 12f;
            var target = new Vector3(
                Mathf.Clamp(landing.x, -table.HalfWidth + edgeMargin, table.HalfWidth - edgeMargin), 0f,
                opp.Sign() * Mathf.Clamp(Mathf.Abs(landing.z), minDepth, table.HalfLength - endMargin));
            float speed = Mathf.Clamp(new Vector2(lv.x, lv.z).magnitude, minSpeed, maxSpeed);
            if (!model.SolveLaunch(table, from, table.ToWorld(target), speed, out Vector3 solved))
                model.SolveLaunch(table, from, table.ToWorld(target), speed * 0.8f, out solved);
            return Vector3.Lerp(vel, solved, assist);
        }
    }
}

using System;
using UnityEngine;
using PingPong.GameObjects;

namespace PingPong.Gameplay
{
    /// <summary>
    /// Detects paddle-ball contact with a swept test between physics steps (fast swings can't tunnel)
    /// and computes the return velocity from the paddle's real motion.
    /// Unity's own paddle-ball collisions are disabled so they don't fight this.
    /// </summary>
    public class PaddleHitter : MonoBehaviour
    {
        [SerializeField] int _playerIndex;
        [SerializeField] MatchController _match;
        [SerializeField] PingPongBall _ball;
        [Tooltip("Centre of the blade. Defaults to this transform.")]
        [SerializeField] Transform _blade;
        [Tooltip("Blade face normal in the blade transform's local space.")]
        [SerializeField] Vector3 _faceNormalLocal = Vector3.forward;
        [SerializeField] float _bladeRadius = 0.08f;
        [SerializeField] float _bladeHalfThickness = 0.006f;
        [SerializeField] float _restitution = 0.85f;
        [SerializeField, Range(0f, 1f)] float _tangentialRetain = 0.6f;
        [Tooltip("0 = pure physics, 1 = always lands on the opponent's half.")]
        [SerializeField, Range(0f, 1f)] float _aimAssist = 0.35f;
        [SerializeField] float _maxSpeed = 20f;
        [SerializeField] float _cooldown = 0.12f;

        public int PlayerIndex { get => _playerIndex; set => _playerIndex = value; }
        public float AimAssist { get => _aimAssist; set => _aimAssist = Mathf.Clamp01(value); }
        /// <summary>Set false for a remote player's paddle: only the owner detects its hits.</summary>
        public bool DetectHits { get; set; } = true;

        /// <summary>Raised after a hit. Arguments: hit position, outgoing velocity.</summary>
        public event Action<Vector3, Vector3> Hit;

        Vector3 _prevPos, _prevBall;
        Quaternion _prevRot;
        bool _hasPrev;
        float _lastHit = -10f;
        readonly BallFlight.Bounce[] _bounce = new BallFlight.Bounce[1];

        Transform Blade => _blade != null ? _blade : transform;

        void Start()
        {
            if (_ball == null) _ball = FindAnyObjectByType<PingPongBall>();
            if (_match == null) _match = FindAnyObjectByType<MatchController>();
            if (_ball == null) return;
            var ballCol = _ball.GetComponent<Collider>();
            foreach (var c in GetComponentsInChildren<Collider>(true))
                if (ballCol != null) Physics.IgnoreCollision(c, ballCol, true);
        }

        void OnEnable() => _hasPrev = false;

        void FixedUpdate()
        {
            if (_ball == null) return;
            Transform b = Blade;
            Vector3 pos = b.position;
            Quaternion rot = b.rotation;
            Vector3 ballPos = _ball.Body.position;

            if (_hasPrev && DetectHits && Time.time - _lastHit > _cooldown
                && (_match == null || _match.CanHit(_playerIndex)))
                TryHit(pos, rot, ballPos);

            _prevPos = pos; _prevRot = rot; _prevBall = _ball.Body.position;
            _hasPrev = true;
        }

        void TryHit(Vector3 pos, Quaternion rot, Vector3 ballPos)
        {
            Vector3 n = _faceNormalLocal.normalized;
            Vector3 l0 = Quaternion.Inverse(_prevRot) * (_prevBall - _prevPos);
            Vector3 l1 = Quaternion.Inverse(rot) * (ballPos - pos);
            float d0 = Vector3.Dot(l0, n), d1 = Vector3.Dot(l1, n);
            float reach = BallFlight.Radius + _bladeHalfThickness;

            float t;
            if (Mathf.Abs(d1) <= reach) t = 1f;                          // overlapping now
            else if (Mathf.Sign(d0) != Mathf.Sign(d1)) t = d0 / (d0 - d1); // passed through the blade
            else return;

            Vector3 lc = Vector3.Lerp(l0, l1, t);
            Vector3 inPlane = lc - Vector3.Dot(lc, n) * n;
            if (inPlane.magnitude > _bladeRadius + BallFlight.Radius) return;

            float side = Mathf.Abs(d0) > 1e-5f ? Mathf.Sign(d0) : Mathf.Sign(d1);
            Vector3 normal = (rot * n) * side;
            Vector3 contactLocal = inPlane;
            float dt = Time.fixedDeltaTime;
            Vector3 paddleVel = ((rot * contactLocal + pos) - (_prevRot * contactLocal + _prevPos)) / dt;

            Vector3 ballVel = _ball.Body.isKinematic ? Vector3.zero : _ball.Body.linearVelocity;
            Vector3 rel = ballVel - paddleVel;
            float vn = Vector3.Dot(rel, normal);
            if (vn >= 0f) return; // separating

            Vector3 relOut = (rel - vn * normal) * _tangentialRetain - _restitution * vn * normal;
            Vector3 outVel = paddleVel + relOut;

            Vector3 hitPos = rot * contactLocal + pos + normal * (reach + 0.004f);
            outVel = ApplyAssist(hitPos, outVel);
            if (outVel.magnitude > _maxSpeed) outVel = outVel.normalized * _maxSpeed;

            _lastHit = Time.time;
            _ball.Launch(hitPos, outVel);
            _ball.PlayPaddleHit();
            Hit?.Invoke(hitPos, outVel);
            if (_match != null) _match.ReportPaddleHit(_playerIndex, hitPos);
        }

        Vector3 ApplyAssist(Vector3 from, Vector3 vel)
        {
            if (_aimAssist <= 0f || _match == null || _match.Table == null) return vel;
            var state = _match.State;
            if (state == MatchState.WaitingForServe || state == MatchState.FreePlay) return vel; // serves must bounce own side first
            PingPongTable table = _match.Table;
            int opp = 1 - _playerIndex;
            Vector3 lv = table.DirToLocal(vel);
            if (lv.z * PingPongTable.SideSign(opp) < 1f) return vel; // not really going towards the opponent

            int n = BallFlight.Simulate(table, from, vel, 2.5f, _bounce, out bool net);
            Vector3 landing;
            if (n > 0 && _bounce[0].Side == opp && !net) return vel; // already good
            if (n > 0) landing = table.ToLocal(_bounce[0].Position);
            else
            {
                // Estimate where it would cross table height.
                Vector3 l0 = table.ToLocal(from);
                float flat = new Vector2(lv.x, lv.z).magnitude;
                landing = l0 + new Vector3(lv.x, 0, lv.z) * (Mathf.Abs(l0.z) + table.HalfLength) / Mathf.Max(flat, 0.1f);
            }
            float sign = PingPongTable.SideSign(opp);
            Vector3 target = new Vector3(
                Mathf.Clamp(landing.x, -table.HalfWidth + 0.1f, table.HalfWidth - 0.1f), 0f,
                sign * Mathf.Clamp(Mathf.Abs(landing.z), 0.3f, table.HalfLength - 0.12f));
            float speed = Mathf.Clamp(new Vector2(lv.x, lv.z).magnitude, 3f, 12f);
            if (!BallFlight.SolveLaunch(table, from, table.ToWorld(target), speed, out Vector3 solved))
                BallFlight.SolveLaunch(table, from, table.ToWorld(target), speed * 0.8f, out solved);
            return Vector3.Lerp(vel, solved, _aimAssist);
        }
    }
}

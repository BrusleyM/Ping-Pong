using System;
using UnityEngine;

namespace PingPong.Gameplay
{
    /// <summary>
    /// Computer opponent. Predicts where the ball will be after it bounces on its side,
    /// moves its paddle there at a limited speed and returns the ball to a chosen spot.
    /// </summary>
    public class AIOpponent : MonoBehaviour
    {
        [Serializable]
        public struct Difficulty
        {
            public string Name;
            [Tooltip("Seconds before the AI starts moving after the player's hit.")]
            public float ReactionTime;
            [Tooltip("Paddle movement speed, m/s.")]
            public float MoveSpeed;
            [Tooltip("Horizontal speed of returns, m/s.")]
            public Vector2 ShotSpeed;
            [Tooltip("Random aim error on the landing point, metres.")]
            public float AimError;
            [Tooltip("How far into the opponent's half it aims, 0-1 (1 = deep).")]
            public Vector2 Depth;
            [Range(0f, 1f)] public float MissChance;
        }

        [SerializeField] MatchController _match;
        [SerializeField] Transform _paddle;
        [SerializeField] int _playerIndex = 1;
        [SerializeField] float _reach = 0.3f;
        [SerializeField] float _serveDelay = 1.2f;
        [SerializeField] int _level = 1;
        [SerializeField] Difficulty[] _levels =
        {
            new Difficulty { Name = "Easy", ReactionTime = 0.35f, MoveSpeed = 1.6f, ShotSpeed = new Vector2(3.5f, 4.5f), AimError = 0.25f, Depth = new Vector2(0.35f, 0.7f), MissChance = 0.15f },
            new Difficulty { Name = "Normal", ReactionTime = 0.22f, MoveSpeed = 2.6f, ShotSpeed = new Vector2(4.5f, 6.5f), AimError = 0.15f, Depth = new Vector2(0.45f, 0.85f), MissChance = 0.06f },
            new Difficulty { Name = "Hard", ReactionTime = 0.12f, MoveSpeed = 4f, ShotSpeed = new Vector2(6f, 9f), AimError = 0.08f, Depth = new Vector2(0.6f, 0.92f), MissChance = 0.02f },
        };

        public int PlayerIndex => _playerIndex;
        public int Level { get => _level; set => _level = Mathf.Clamp(value, 0, _levels.Length - 1); }
        public string LevelName => _levels[_level].Name;
        public int LevelCount => _levels.Length;

        readonly BallFlight.Bounce[] _bounces = new BallFlight.Bounce[2];
        Vector3 _target;
        float _strikeTime = -1f;
        bool _willMiss;
        int _plannedForHit = -1;
        float _serveTimer;
        float _swing;
        Quaternion _restRotation;

        PingPongTable Table => _match.Table;
        Difficulty D => _levels[_level];

        void Start()
        {
            _restRotation = Quaternion.LookRotation(-Table.Forward * PingPongTable.SideSign(_playerIndex), Vector3.up);
            _paddle.SetPositionAndRotation(RestPosition(), _restRotation);
            _target = RestPosition();
        }

        Vector3 RestPosition() =>
            Table.ToWorld(new Vector3(0f, 0.25f, PingPongTable.SideSign(_playerIndex) * (Table.HalfLength + 0.35f)));

        void Update()
        {
            var ball = _match.Ball;
            switch (_match.State)
            {
                case MatchState.WaitingForServe when _match.Server == _playerIndex:
                    _target = ball.transform.position + Table.Forward * (PingPongTable.SideSign(_playerIndex) * 0.06f);
                    _serveTimer += Time.deltaTime;
                    if (_serveTimer > _serveDelay) { _serveTimer = 0f; Serve(); }
                    break;
                case MatchState.Rally when _match.LastHitter != _playerIndex && _match.LastHitter >= 0:
                    _serveTimer = 0f;
                    if (Time.time - _match.LastHitTime > D.ReactionTime && _plannedForHit != Mathf.RoundToInt(_match.LastHitTime * 1000f))
                        Plan();
                    break;
                default:
                    _serveTimer = 0f;
                    if (_strikeTime < 0f) _target = RestPosition();
                    break;
            }

            _paddle.position = Vector3.MoveTowards(_paddle.position, _target, D.MoveSpeed * Time.deltaTime);
            _swing = Mathf.MoveTowards(_swing, 0f, Time.deltaTime * 4f);
            _paddle.rotation = _restRotation * Quaternion.Euler(-40f * _swing, 0f, 0f);
        }

        void FixedUpdate()
        {
            if (_strikeTime < 0f || Time.time < _strikeTime) return;
            _strikeTime = -1f;
            if (_match.State != MatchState.Rally || !_match.CanHit(_playerIndex)) return;
            Vector3 ballPos = _match.Ball.Body.position;
            if (_willMiss || Vector3.Distance(_paddle.position, ballPos) > _reach) return; // too slow: point lost
            Strike(ballPos);
        }

        void Plan()
        {
            _plannedForHit = Mathf.RoundToInt(_match.LastHitTime * 1000f);
            var body = _match.Ball.Body;
            int n = BallFlight.Simulate(Table, body.position, body.linearVelocity, 3f, _bounces, out bool net);
            if (net || n == 0) return;
            // Find the first bounce on our side; a ball that never lands on our side is out, let it go.
            int idx = -1;
            for (int i = 0; i < n; i++) if (_bounces[i].Side == _playerIndex) { idx = i; break; }
            if (idx < 0) return;

            // Continue from the bounce until the ball is at a comfortable hitting point.
            Vector3 p = _bounces[idx].Position;
            Vector3 v = SimulateVelocityAt(body.position, body.linearVelocity, _bounces[idx].Time);
            v = BallFlight.BounceOffTable(v);
            float t = _bounces[idx].Time;
            float endZ = Table.HalfLength + 0.15f;
            for (int i = 0; i < 240; i++)
            {
                BallFlight.Step(ref p, ref v, BallFlight.SimStep);
                t += BallFlight.SimStep;
                Vector3 l = Table.ToLocal(p);
                bool pastEnd = Mathf.Abs(l.z) > endZ;
                bool dropping = v.y < 0f && l.y < 0.3f;
                if (pastEnd || dropping || l.y < BallFlight.Radius) break;
            }

            // Stand just behind the ball (away from the net). Prediction starts now, so t is time from now.
            _target = p + Table.Forward * (PingPongTable.SideSign(_playerIndex) * 0.04f);
            _strikeTime = Time.time + t;
            _willMiss = UnityEngine.Random.value < D.MissChance;
        }

        Vector3 SimulateVelocityAt(Vector3 p, Vector3 v, float time)
        {
            for (float t = 0f; t < time - 1e-4f; t += BallFlight.SimStep) BallFlight.Step(ref p, ref v, BallFlight.SimStep);
            return v;
        }

        void Strike(Vector3 from)
        {
            int opp = 1 - _playerIndex;
            var d = D;
            float sign = PingPongTable.SideSign(opp);
            var target = new Vector3(
                Mathf.Clamp(UnityEngine.Random.Range(-0.6f, 0.6f) * Table.HalfWidth + UnityEngine.Random.Range(-d.AimError, d.AimError),
                    -Table.HalfWidth + 0.05f, Table.HalfWidth - 0.05f),
                0f,
                sign * Mathf.Clamp(UnityEngine.Random.Range(d.Depth.x, d.Depth.y) * Table.HalfLength
                                   + UnityEngine.Random.Range(-d.AimError, d.AimError), 0.2f, Table.HalfLength - 0.05f));
            float speed = UnityEngine.Random.Range(d.ShotSpeed.x, d.ShotSpeed.y);
            Vector3 vel = Vector3.zero;
            for (int i = 0; i < 5; i++)
            {
                if (BallFlight.SolveLaunch(Table, from, Table.ToWorld(target), speed, out vel)) break;
                speed *= 0.85f; // more arc to clear the net
            }
            HitBall(from, vel);
        }

        void Serve()
        {
            Vector3 from = _match.Ball.transform.position;
            float mySign = PingPongTable.SideSign(_playerIndex);
            float oppSign = -mySign;
            // Try a few first-bounce spots/speeds and keep one whose second bounce lands on the opponent's half.
            for (int attempt = 0; attempt < 12; attempt++)
            {
                var first = new Vector3(UnityEngine.Random.Range(-0.3f, 0.3f) * Table.HalfWidth, 0f,
                    mySign * Table.HalfLength * UnityEngine.Random.Range(0.4f, 0.7f));
                float speed = UnityEngine.Random.Range(2.8f, 4.2f);
                BallFlight.SolveLaunch(Table, from, Table.ToWorld(first), speed, out Vector3 vel);
                int n = BallFlight.Simulate(Table, from, vel, 3f, _bounces, out bool net);
                if (net || n < 2 || _bounces[0].Side != _playerIndex || _bounces[1].Side == _playerIndex) continue;
                if (Table.ToLocal(_bounces[1].Position).z * oppSign < 0.15f) continue;
                HitBall(from, vel);
                return;
            }
            // Fallback: simple short serve.
            BallFlight.SolveLaunch(Table, from, Table.ToWorld(new Vector3(0, 0, mySign * Table.HalfLength * 0.5f)), 3.5f, out Vector3 fb);
            HitBall(from, fb);
        }

        void HitBall(Vector3 from, Vector3 vel)
        {
            var ball = _match.Ball;
            ball.Launch(from, vel);
            ball.PlayPaddleHit();
            _swing = 1f;
            _strikeTime = -1f;
            _target = RestPosition();
            _match.ReportPaddleHit(_playerIndex, from);
        }
    }
}

using System;
using PingPong.Core;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace PingPong.Gameplay
{
    /// <summary>
    /// Computer opponent. Predicts where the ball will be after it bounces on its side, moves its
    /// paddle there at a limited speed and returns the ball to a chosen spot. Toggle the component
    /// (not the GameObject) to switch the AI on or off.
    /// </summary>
    public sealed class AIOpponent : MonoBehaviour
    {
        [SerializeField] MatchController _match;
        [SerializeField] Transform _paddle;
        [FormerlySerializedAs("_playerIndex")]
        [SerializeField] Side _player = Side.Far;
        [SerializeField] AIProfile _profile;

        public Side Player => _player;
        public int Level { get => _level; set => SetLevel(value); }
        public string LevelName => Difficulty.Name;
        public int LevelCount => _profile.LevelCount;

        /// <summary>Raised when the difficulty level changes.</summary>
        public event Action<int> LevelChanged;

        // Search limits (not design tuning; design values live in the AI profile).
        const float PredictionTime = 3f;
        const int MaxHitSearchSteps = 240;
        const int ServeAttempts = 12;
        const float PastEndMargin = 0.15f; // stop the hit search once the ball is this far past the end line

        readonly BallBounce[] _bounces = new BallBounce[2];
        int _level = -1;
        Vector3 _target;
        float _strikeTime = -1f;
        bool _willMiss;
        int _plannedForHit = -1;
        float _serveTimer;
        float _swing;
        Quaternion _restRotation;
        Vector3 _swingAxis;

        AIDifficulty Difficulty => _profile.Level(Level);
        AIBehaviour Behaviour => _profile.Behaviour;
        TableFrame Table => _match.Table.Frame;
        BallFlightModel Model => _match.Ball.Model;

        void Awake()
        {
            if (_level < 0) _level = _profile.DefaultLevel;
        }

        void OnValidate() => Validation.Require(_profile, "an AI Profile", this);

        void SetLevel(int value)
        {
            int clamped = Mathf.Clamp(value, 0, _profile.LevelCount - 1);
            if (clamped == _level) return;
            _level = clamped;
            LevelChanged?.Invoke(_level);
        }

        void Start()
        {
            // Blade face towards the opponent, blade above the handle, whatever the model's own axes.
            Vector3 towardOpponent = -Table.Forward * _player.Sign();
            _restRotation = _paddle.TryGetComponent(out PaddleModel model)
                ? model.RootRotationFor(towardOpponent, Vector3.up)
                : Quaternion.LookRotation(towardOpponent, Vector3.up);
            _swingAxis = Vector3.Cross(Vector3.up, towardOpponent);
            _paddle.SetPositionAndRotation(RestPosition(), _restRotation);
            _target = RestPosition();
        }

        Vector3 RestPosition()
        {
            Vector2 rest = Behaviour.RestOffset;
            return Table.PointOnSide(_player, 0f, Table.HalfLength + rest.y, rest.x);
        }

        void Update()
        {
            var ball = _match.Ball;
            switch (_match.Phase)
            {
                case MatchPhase.WaitingForServe when _match.Server == _player:
                    _target = ball.transform.position + Table.Forward * (_player.Sign() * Behaviour.BehindBall * 1.5f);
                    _serveTimer += Time.deltaTime;
                    if (_serveTimer > Behaviour.ServeDelay) { _serveTimer = 0f; Serve(); }
                    break;
                case MatchPhase.Rally when _match.LastHitter == _player.Opponent():
                    _serveTimer = 0f;
                    if (Time.time - _match.LastHitTime > Difficulty.ReactionTime && _plannedForHit != _match.HitCount)
                        Plan();
                    break;
                default:
                    _serveTimer = 0f;
                    if (_strikeTime < 0f) _target = RestPosition();
                    break;
            }

            _paddle.position = Vector3.MoveTowards(_paddle.position, _target, Difficulty.MoveSpeed * Time.deltaTime);
            _swing = Mathf.MoveTowards(_swing, 0f, Time.deltaTime * Behaviour.Swing.y);
            _paddle.rotation = Quaternion.AngleAxis(-Behaviour.Swing.x * _swing, _swingAxis) * _restRotation;
        }

        void FixedUpdate()
        {
            if (_strikeTime < 0f || Time.time < _strikeTime) return;
            _strikeTime = -1f;
            if (_match.Phase != MatchPhase.Rally || !_match.CanHit(_player)) return;
            Vector3 ballPos = _match.Ball.Body.position;
            if (_willMiss || Vector3.Distance(_paddle.position, ballPos) > Behaviour.Reach) return; // too slow: point lost
            Strike(ballPos);
        }

        void Plan()
        {
            _plannedForHit = _match.HitCount;
            var body = _match.Ball.Body;
            TableFrame table = Table;
            int n = Model.Simulate(table, body.position, body.linearVelocity, PredictionTime, _bounces, out bool net);
            // Predicted to touch the net: retry next frame with the real post-net velocity, so a ball that
            // clips the net cord and still drops on our side gets played.
            if (net) { _plannedForHit = -1; return; }
            if (n == 0) return;
            // First bounce on our side; a ball that never lands on our side is out, let it go.
            int idx = -1;
            for (int i = 0; i < n; i++) if (_bounces[i].Side == _player) { idx = i; break; }
            if (idx < 0) return;

            // Continue from the bounce until the ball is at a comfortable hitting point.
            Vector3 p = _bounces[idx].Position;
            Vector3 v = Model.BounceOffTable(Model.VelocityAfter(body.position, body.linearVelocity, _bounces[idx].Time));
            float t = _bounces[idx].Time;
            float endZ = table.HalfLength + PastEndMargin;
            for (int i = 0; i < MaxHitSearchSteps; i++)
            {
                Model.Step(ref p, ref v, Model.SimStep);
                t += Model.SimStep;
                Vector3 l = table.ToLocal(p);
                bool pastEnd = Mathf.Abs(l.z) > endZ;
                bool dropping = v.y < 0f && l.y < Behaviour.ComfortHeight;
                if (pastEnd || dropping || l.y < Model.Radius) break;
            }

            _target = p + table.Forward * (_player.Sign() * Behaviour.BehindBall);
            _strikeTime = Time.time + t;
            _willMiss = Random.value < Difficulty.MissChance;
        }

        void Strike(Vector3 from)
        {
            TableFrame table = Table;
            AIDifficulty d = Difficulty;
            AIBehaviour b = Behaviour;
            float across = Random.Range(-b.AcrossSpread, b.AcrossSpread) * table.HalfWidth + Random.Range(-d.AimError, d.AimError);
            float depth = Random.Range(d.Depth.x, d.Depth.y) * table.HalfLength + Random.Range(-d.AimError, d.AimError);
            Vector3 target = table.ClampToSide(_player.Opponent(), across, depth, b.TargetMargins);

            float speed = Random.Range(d.ShotSpeed.x, d.ShotSpeed.y);
            Model.SolveLaunchClearingNet(table, from, target, speed, b.NetRetry, out Vector3 vel);
            HitBall(from, vel);
        }

        void Serve()
        {
            TableFrame table = Table;
            Vector3 from = _match.Ball.transform.position;
            Side opp = _player.Opponent();
            AIBehaviour b = Behaviour;
            // Try a few first-bounce spots and speeds; keep one whose second bounce lands on the opponent's half.
            for (int attempt = 0; attempt < ServeAttempts; attempt++)
            {
                Vector3 first = table.PointOnSide(_player, Random.Range(-b.ServeAcrossSpread, b.ServeAcrossSpread) * table.HalfWidth,
                    Random.Range(b.ServeFirstBounce.x, b.ServeFirstBounce.y) * table.HalfLength);
                float speed = Random.Range(Behaviour.ServeSpeed.x, Behaviour.ServeSpeed.y);
                Model.SolveLaunch(table, from, first, speed, out Vector3 vel);
                int n = Model.Simulate(table, from, vel, PredictionTime, _bounces, out bool net);
                if (net || n < 2 || _bounces[0].Side != _player || _bounces[1].Side != opp) continue;
                if (table.ToLocal(_bounces[1].Position).z * opp.Sign() < b.ServeMinReceiverDepth) continue;
                HitBall(from, vel);
                return;
            }
            // Fallback: a simple short serve.
            Model.SolveLaunch(table, from, table.PointOnSide(_player, 0f, 0.5f * table.HalfLength), b.ServeSpeed.x, out Vector3 fallback);
            HitBall(from, fallback);
        }

        void HitBall(Vector3 from, Vector3 vel)
        {
            _match.Strike(_player, from, vel);
            _swing = 1f;
            _strikeTime = -1f;
            _target = RestPosition();
        }
    }
}

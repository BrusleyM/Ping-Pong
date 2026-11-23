using PingPong.Core;
using PingPong.Gameplay;
using UnityEngine;

namespace PingPong.GameFeel
{
    /// <summary>
    /// Connects the project's match to the game feel. Listens to MatchController (points, lets, game won, counted hits) and
    /// GameModeController (match started) and passes them to GameFeelScore, which drives the crowd, the stadium
    /// screen, flashes and confetti. Read-only: it never changes the match.
    /// Added by GameFeelBootstrap when the scene has a MatchController; references are found if left empty.
    /// </summary>
    [DisallowMultipleComponent]
    public class MatchLink : MonoBehaviour
    {
        [SerializeField] MatchController _match;
        [SerializeField] GameModeController _modes;

        GameFeelScore _score;
        bool _started;

        public MatchController Match => _match;
        public GameModeController Modes => _modes;

        void Awake()
        {
            if (_match == null) _match = FindFirstObjectByType<MatchController>();
            if (_modes == null) _modes = FindFirstObjectByType<GameModeController>();
            _score = GetComponent<GameFeelScore>();
            if (_score != null) _score.Linked = _match != null;
        }

        void OnEnable()
        {
            if (_match != null)
            {
                _match.PointWon += OnPointWon;
                _match.GameWon += OnGameWon;
                _match.Let += OnLet;
                _match.PaddleHit += OnPaddleHit;
            }
            if (_modes != null) _modes.MatchStarted += OnMatchStarted;
        }

        void OnDisable()
        {
            if (_match != null)
            {
                _match.PointWon -= OnPointWon;
                _match.GameWon -= OnGameWon;
                _match.Let -= OnLet;
                _match.PaddleHit -= OnPaddleHit;
            }
            if (_modes != null) _modes.MatchStarted -= OnMatchStarted;
        }

        void Start()
        {
            // The game scene can start its match before we subscribed (GameStarter runs in Start too).
            if (!_started && _match != null && _match.Phase != MatchPhase.FreePlay && _score != null)
            {
                _started = true;
                _score.StartGame(_match.Score(Side.Near), _match.Score(Side.Far));
            }
        }

        void OnMatchStarted()
        {
            _started = true;
            if (_score != null) _score.StartGame(_match.Score(Side.Near), _match.Score(Side.Far));
        }

        void OnPointWon(Side winner, PointReason reason)
        {
            if (_score == null) return;
            _score.ApplyPoint(GameFeelTable.ToInt(winner), _match.Score(Side.Near), _match.Score(Side.Far));
        }

        /// <summary>A hit the project's rules counted: hit points come from these, not from every paddle touch.</summary>
        void OnPaddleHit(Side player)
        {
            // Practice stops the match: there the ball's own paddle contacts count instead.
            if (_score == null || _score.PracticeMode || _match.Ball == null) return;
            var ball = _match.Ball;
            // The ball has already been launched/bounced, so its speed is the shot's speed.
            float speed = ball.Body != null ? ball.Body.linearVelocity.magnitude : 0f;
            var cfg = GameFeelBootstrap.Instance;
            float strength = cfg != null ? cfg.Intensity(speed) : 0.5f;
            _score.RegisterPaddleHit(GameFeelTable.ToInt(player), ball.transform.position, speed, strength);
        }

        void OnLet()
        {
            if (_score != null) _score.ApplyLet();
        }

        void OnGameWon(Side winner)
        {
            if (_score != null) _score.EndGame(GameFeelTable.ToInt(winner));
        }
    }
}

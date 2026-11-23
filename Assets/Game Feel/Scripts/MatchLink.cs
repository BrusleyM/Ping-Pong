using PingPong.Core;
using PingPong.Gameplay;
using UnityEngine;

namespace PingPong.GameFeel
{
    /// <summary>
    /// Connects the project's match to the game feel. Listens to MatchController (points, lets, game won) and
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
            }
            if (_modes != null) _modes.MatchStarted -= OnMatchStarted;
        }

        void Start()
        {
            // The game scene can start its match before we subscribed (GameStarter runs in Start too).
            if (_match != null && _match.Phase != MatchPhase.FreePlay && _score != null)
                _score.StartGame(_match.Score(Side.Near), _match.Score(Side.Far));
        }

        void OnMatchStarted()
        {
            if (_score != null) _score.StartGame(_match.Score(Side.Near), _match.Score(Side.Far));
        }

        void OnPointWon(Side winner, PointReason reason)
        {
            if (_score == null) return;
            _score.ApplyPoint(GameFeelTable.ToInt(winner), _match.Score(Side.Near), _match.Score(Side.Far));
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

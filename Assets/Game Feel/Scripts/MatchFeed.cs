using System;
using PingPong.Core;
using PingPong.Gameplay;
using UnityEngine;

namespace PingPong.GameFeel
{
    public class MatchFeed : MonoBehaviour
    {
        [SerializeField] MatchController _match;
        [SerializeField] GameModeController _modes;

        public int ScoreSouth { get; private set; }
        public int ScoreNorth { get; private set; }
        public bool GameBegun { get; private set; }
        public bool GameOver { get; private set; }

        public event Action GameStarted;
        public event Action<int, int, int> PointScored;
        public event Action<int, int> ScoreChanged;
        public event Action<int> GameWon;
        public event Action LetCalled;

        void Awake()
        {
            if (_match == null) _match = FindFirstObjectByType<MatchController>();
            if (_modes == null) _modes = FindFirstObjectByType<GameModeController>();
        }

        void OnEnable()
        {
            if (_match != null)
            {
                _match.PointWon += OnPointWon;
                _match.GameWon += OnGameWon;
                _match.Let += ApplyLet;
            }
            if (_modes != null) _modes.MatchStarted += StartFromMatch;
        }

        void OnDisable()
        {
            if (_match != null)
            {
                _match.PointWon -= OnPointWon;
                _match.GameWon -= OnGameWon;
                _match.Let -= ApplyLet;
            }
            if (_modes != null) _modes.MatchStarted -= StartFromMatch;
        }

        void Start()
        {
            bool startedBeforeSubscribing = _match != null && _match.Phase != MatchPhase.FreePlay;
            if (startedBeforeSubscribing) StartFromMatch();
        }

        public void StartGame(int scoreSouth = 0, int scoreNorth = 0)
        {
            ScoreSouth = scoreSouth;
            ScoreNorth = scoreNorth;
            GameOver = false;
            GameBegun = true;
            ScoreChanged?.Invoke(ScoreSouth, ScoreNorth);
            GameStarted?.Invoke();
        }

        public void ApplyPoint(int winnerSide, int scoreSouth, int scoreNorth)
        {
            if (GameOver) return;
            ScoreSouth = scoreSouth;
            ScoreNorth = scoreNorth;
            PointScored?.Invoke(winnerSide, ScoreSouth, ScoreNorth);
            ScoreChanged?.Invoke(ScoreSouth, ScoreNorth);
        }

        public void ApplyLet() => LetCalled?.Invoke();

        public void EndGame(int winnerSide)
        {
            if (GameOver) return;
            GameOver = true;
            GameWon?.Invoke(winnerSide);
        }

        void StartFromMatch() => StartGame(_match.Score(Side.Near), _match.Score(Side.Far));

        void OnPointWon(Side winner, PointReason reason) =>
            ApplyPoint(GameFeelTable.ToInt(winner), _match.Score(Side.Near), _match.Score(Side.Far));

        void OnGameWon(Side winner) => EndGame(GameFeelTable.ToInt(winner));
    }
}

using System.Collections;
using UnityEngine;

namespace PingPong.GameFeel.Testing
{
    [RequireComponent(typeof(MatchFeed))]
    public class DemoMatch : MonoBehaviour
    {
        public float firstGameDelay = 0.75f;
        public int pointsToWin = 11;
        [Tooltip("Seconds after a demo game ends before the score resets.")]
        public float newGameDelay = 7f;

        MatchFeed _match;

        void OnEnable()
        {
            _match = GetComponent<MatchFeed>();
            _match.GameWon += OnGameWon;
        }

        void OnDisable() => _match.GameWon -= OnGameWon;

        IEnumerator Start()
        {
            yield return new WaitForSeconds(firstGameDelay);
            _match.StartGame();
        }

        public void Point(int winnerSide)
        {
            if (_match.GameOver) return;
            int south = _match.ScoreSouth + (winnerSide == GameFeelTable.South ? 1 : 0);
            int north = _match.ScoreNorth + (winnerSide == GameFeelTable.North ? 1 : 0);
            _match.ApplyPoint(winnerSide, south, north);
            bool won = Mathf.Max(south, north) >= pointsToWin && Mathf.Abs(south - north) >= 2;
            if (won) _match.EndGame(south > north ? GameFeelTable.South : GameFeelTable.North);
        }

        void OnGameWon(int winner) => StartCoroutine(StartNextGame());

        IEnumerator StartNextGame()
        {
            yield return new WaitForSeconds(newGameDelay);
            _match.StartGame();
        }
    }
}

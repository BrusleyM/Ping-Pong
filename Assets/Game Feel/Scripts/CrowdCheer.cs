using UnityEngine;

namespace PingPong.GameFeel
{
    [RequireComponent(typeof(MatchFeed))]
    public class CrowdCheer : MonoBehaviour
    {
        public float startCheerDuration = 3.5f;
        [Range(0f, 1f)] public float startCheerIntensity = 0.8f;
        public float pointCheerDuration = 2.2f;
        [Range(0f, 1f)] public float pointCheerIntensity = 0.65f;
        public float endCheerDuration = 7f;
        [Range(0f, 1f)] public float endCheerIntensity = 1f;

        MatchFeed _match;

        void OnEnable()
        {
            _match = GetComponent<MatchFeed>();
            _match.GameStarted += OnGameStarted;
            _match.PointScored += OnPointScored;
            _match.GameWon += OnGameWon;
        }

        void OnDisable()
        {
            _match.GameStarted -= OnGameStarted;
            _match.PointScored -= OnPointScored;
            _match.GameWon -= OnGameWon;
        }

        void OnGameStarted() => FanJump.CheerAll(startCheerDuration, startCheerIntensity);

        void OnPointScored(int winner, int south, int north) => FanJump.CheerAll(pointCheerDuration, pointCheerIntensity);

        void OnGameWon(int winner) => FanJump.CheerAll(endCheerDuration, endCheerIntensity);
    }
}

using System;
using UnityEngine;

namespace PingPong.GameFeel
{
    [RequireComponent(typeof(MatchFeed))]
    public class RallyFeed : MonoBehaviour
    {
        [Tooltip("Highest combo multiplier a rally can reach.")]
        public int maxCombo = 8;

        readonly RallyTracker _tracker = new RallyTracker();
        MatchFeed _match;

        public event Action<int, float, float> PaddleHit;
        public event Action<int, int, Vector3> ComboHit;
        public event Action<int, int, Vector3> Landed;
        public event Action NetTouched;

        public int LongestRally => _tracker.Longest;
        public int PracticeStreak => _tracker.PracticeStreak;
        public bool PracticeMode { get => _tracker.PracticeMode; set => _tracker.PracticeMode = value; }

        void OnEnable()
        {
            _match = GetComponent<MatchFeed>();
            _match.GameStarted += _tracker.Reset;
            _match.PointScored += OnPointScored;
            _match.LetCalled += _tracker.End;
            _match.GameWon += OnGameWon;
            BallGameFeel.Contact += OnContact;
        }

        void OnDisable()
        {
            _match.GameStarted -= _tracker.Reset;
            _match.PointScored -= OnPointScored;
            _match.LetCalled -= _tracker.End;
            _match.GameWon -= OnGameWon;
            BallGameFeel.Contact -= OnContact;
        }

        public void RegisterFeed()
        {
            if (!_match.GameOver) _tracker.Feed();
        }

        public void ShowDemoHit(int side, int combo, Vector3 where)
        {
            if (_match.GameOver) return;
            PaddleHit?.Invoke(side, 30f, 0.4f);
            ComboHit?.Invoke(side, combo, where);
        }

        public void ShowDemoLanding(int side, int combo, Vector3 where)
        {
            if (!_match.GameOver) Landed?.Invoke(side, combo, where);
        }

        void OnPointScored(int winner, int south, int north) => _tracker.End();

        void OnGameWon(int winner) => _tracker.End();

        void OnContact(BallGameFeel ball, BallGameFeel.ContactKind kind, Vector3 point, Vector3 paddlePosition)
        {
            if (_match.GameOver || ball == null) return;
            if (ball.Options != null && !ball.Options.countsForRally) return;
            Vector3 where = ball.transform.position;
            switch (kind)
            {
                case BallGameFeel.ContactKind.Paddle:
                    int side = ball.SideOf(paddlePosition);
                    if (!_tracker.TryPaddleHit(side, Time.time)) return;
                    PaddleHit?.Invoke(side, ball.LastImpactSpeed * 3.6f, ball.LastImpactStrength);
                    ComboHit?.Invoke(side, _tracker.Combo(maxCombo), where);
                    break;
                case BallGameFeel.ContactKind.Table:
                    if (_tracker.TableBounce(ball.SideOf(point)) == BounceOutcome.Landed)
                        Landed?.Invoke(_tracker.Hitter, _tracker.Combo(maxCombo), where);
                    break;
                case BallGameFeel.ContactKind.Net:
                    if (_tracker.InPlay) NetTouched?.Invoke();
                    break;
                case BallGameFeel.ContactKind.Other:
                    _tracker.End();
                    break;
            }
        }
    }
}

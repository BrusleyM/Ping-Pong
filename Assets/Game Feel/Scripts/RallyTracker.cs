using System;

namespace PingPong.GameFeel
{
    public enum BounceOutcome { None, Landed, RallyOver }

    public sealed class RallyTracker
    {
        const float RepeatedContactWindow = 0.25f;
        const int BallMachineSide = GameFeelTable.North;

        bool _awaitingLanding;
        bool _landedThisShot;
        int _lastBounceSide;
        int _lastPaddleSide;
        float _lastPaddleTime = float.NegativeInfinity;

        public bool PracticeMode { get; set; }
        public bool InPlay { get; private set; }
        public int Hitter { get; private set; }
        public int Hits { get; private set; }
        public int Longest { get; private set; }
        public int PracticeStreak { get; private set; }

        public void Reset()
        {
            InPlay = false;
            _awaitingLanding = false;
            Hits = 0;
            Longest = 0;
            PracticeStreak = 0;
        }

        public bool TryPaddleHit(int side, float time)
        {
            if (side == _lastPaddleSide && time - _lastPaddleTime < RepeatedContactWindow) return false;
            _lastPaddleSide = side;
            _lastPaddleTime = time;

            if (PracticeMode) Hits = PracticeStreak + 1;
            else if (InPlay && side != Hitter) Hits++;
            else Hits = 1;
            if (Hits > Longest) Longest = Hits;

            Hitter = side;
            InPlay = true;
            _awaitingLanding = true;
            _landedThisShot = false;
            _lastBounceSide = 0;
            return true;
        }

        public int Combo(int highest)
        {
            if (Hits < 1) return 1;
            return Math.Min(Hits, Math.Max(1, highest));
        }

        public BounceOutcome TableBounce(int side)
        {
            if (!InPlay) return BounceOutcome.None;
            var outcome = BounceOutcome.None;
            if (_awaitingLanding && side == -Hitter)
            {
                _awaitingLanding = false;
                _landedThisShot = true;
                if (PracticeMode) PracticeStreak++;
                outcome = BounceOutcome.Landed;
            }
            else if (side == _lastBounceSide)
            {
                End();
                return BounceOutcome.RallyOver;
            }
            _lastBounceSide = side;
            return outcome;
        }

        public void Feed()
        {
            if (InPlay && Hitter == BallMachineSide)
            {
                PracticeStreak = 0;
                End();
            }
            Hitter = BallMachineSide;
            InPlay = true;
            _awaitingLanding = false;
            _lastBounceSide = 0;
        }

        public void End()
        {
            if (!InPlay) return;
            InPlay = false;
            _awaitingLanding = false;
            Hits = 0;
            bool streakBroken = Hitter == BallMachineSide || !_landedThisShot;
            if (PracticeMode && streakBroken) PracticeStreak = 0;
        }
    }
}

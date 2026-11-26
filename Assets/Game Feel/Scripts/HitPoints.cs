using System;
using UnityEngine;

namespace PingPong.GameFeel
{
    [RequireComponent(typeof(RallyFeed))]
    public class HitPoints : MonoBehaviour
    {
        [Tooltip("Points for every hit, before the combo multiplier.")]
        public int pointsPerHit = 10;
        [Tooltip("Extra points when the shot lands on the opponent's half, before the multiplier.")]
        public int landingBonus = 20;

        MatchFeed _match;
        RallyFeed _rally;

        public int South { get; private set; }
        public int North { get; private set; }

        public event Action<int, int, int, bool, Vector3> Scored;

        void OnEnable()
        {
            _match = GetComponent<MatchFeed>();
            _rally = GetComponent<RallyFeed>();
            _match.GameStarted += ResetPoints;
            _rally.ComboHit += OnComboHit;
            _rally.Landed += OnLanded;
        }

        void OnDisable()
        {
            _match.GameStarted -= ResetPoints;
            _rally.ComboHit -= OnComboHit;
            _rally.Landed -= OnLanded;
        }

        public void AddBonus(int side, int points, Vector3 where)
        {
            if (!_match.GameOver) Add(side, points, 1, true, where);
        }

        void ResetPoints() => South = North = 0;

        void OnComboHit(int side, int combo, Vector3 where) => Add(side, pointsPerHit * combo, combo, false, where);

        void OnLanded(int side, int combo, Vector3 where)
        {
            if (landingBonus > 0) Add(side, landingBonus * combo, combo, true, where);
        }

        void Add(int side, int points, int combo, bool bonus, Vector3 where)
        {
            if (side == GameFeelTable.South) South += points; else North += points;
            Scored?.Invoke(side, points, combo, bonus, where);
        }
    }
}

using PingPong.Core;
using UnityEngine;

namespace PingPong.Gameplay
{
    /// <summary>Scoring rules and match pacing.</summary>
    [CreateAssetMenu(menuName = "Ping Pong/Match Rules Profile", fileName = "MatchRules")]
    public sealed class MatchRulesProfile : ScriptableObject
    {
        [SerializeField] MatchRules _rules = MatchRules.Standard;
        [SerializeField] MatchPacing _pacing = MatchPacing.Default;

        public MatchRules Rules => _rules;
        public MatchPacing Pacing => _pacing;
    }
}

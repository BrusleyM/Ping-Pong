using PingPong.Core;
using UnityEngine;

namespace PingPong.Gameplay
{
    /// <summary>AI difficulty levels and shared AI behaviour.</summary>
    [CreateAssetMenu(menuName = "Ping Pong/AI Profile", fileName = "AIOpponent")]
    public sealed class AIProfile : ScriptableObject
    {
        [SerializeField] AIDifficulty[] _levels = AIDifficulty.Defaults;
        [SerializeField] int _defaultLevel = 1;
        [SerializeField] AIBehaviour _behaviour = AIBehaviour.Default;

        public int LevelCount => _levels.Length;
        public int DefaultLevel => Mathf.Clamp(_defaultLevel, 0, Mathf.Max(0, _levels.Length - 1));
        public AIBehaviour Behaviour => _behaviour;
        public AIDifficulty Level(int index) => _levels[Mathf.Clamp(index, 0, _levels.Length - 1)];

        void OnValidate()
        {
            if (_levels == null || _levels.Length == 0) _levels = AIDifficulty.Defaults;
        }
    }
}

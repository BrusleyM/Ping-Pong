using PingPong.Core;
using UnityEngine;

namespace PingPong.Gameplay
{
    [CreateAssetMenu(menuName = "Ping Pong/Paddle Recovery Profile", fileName = "PaddleRecovery")]
    public sealed class PaddleRecoveryProfile : ScriptableObject
    {
        [SerializeField] PaddleRecoveryFeel _feel = PaddleRecoveryFeel.Default;

        public PaddleRecoveryFeel Feel => _feel;
    }
}

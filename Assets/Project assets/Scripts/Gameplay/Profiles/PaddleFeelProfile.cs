using PingPong.Core;
using UnityEngine;

namespace PingPong.Gameplay
{
    /// <summary>How the player's paddle feels.</summary>
    [CreateAssetMenu(menuName = "Ping Pong/Paddle Feel Profile", fileName = "PaddleFeel")]
    public sealed class PaddleFeelProfile : ScriptableObject
    {
        [SerializeField] PaddleFeel _feel = PaddleFeel.Default;

        public PaddleFeel Feel => _feel;
    }
}

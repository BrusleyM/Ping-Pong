using System;

namespace PingPong.Gameplay
{
    [Serializable]
    public sealed class GameSettings
    {
        public float EffectsVolume = 1f;
        public Hand DominantHand = Hand.Right;
    }
}

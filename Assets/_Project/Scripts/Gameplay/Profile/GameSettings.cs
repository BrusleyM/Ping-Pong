using System;

namespace PingPong.Gameplay
{
    [Serializable]
    public sealed class GameSettings
    {
        public float MusicVolume = 0.8f;
        public float EffectsVolume = 1f;
        public Hand DominantHand = Hand.Right;
        public bool ComfortMode;
        public bool PassthroughOn;
    }
}

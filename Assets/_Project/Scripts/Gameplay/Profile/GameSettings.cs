using System;

namespace PingPong.Gameplay
{
    public enum PlayPosture { Standing, Seated }

    [Serializable]
    public sealed class GameSettings
    {
        public float EffectsVolume = 1f;
        public Hand DominantHand = Hand.Right;
        public PlayPosture Posture = PlayPosture.Standing;
    }
}

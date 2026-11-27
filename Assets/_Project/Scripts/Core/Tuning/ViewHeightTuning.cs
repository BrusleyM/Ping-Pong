using System;

namespace PingPong.Core
{
    [Serializable]
    public struct ViewHeightTuning
    {
        public float StandingEyeHeight;
        public float LowestSeatedEyeHeight;
        public float HighestLift;

        public static ViewHeightTuning Default => new ViewHeightTuning
        {
            StandingEyeHeight = 1.6f,
            LowestSeatedEyeHeight = 0.7f,
            HighestLift = 0.8f
        };

        public bool TryGetSeatedLift(float eyeHeight, out float lift)
        {
            lift = 0f;
            if (eyeHeight < LowestSeatedEyeHeight) return false;
            lift = Math.Max(0f, Math.Min(HighestLift, StandingEyeHeight - eyeHeight));
            return true;
        }
    }
}

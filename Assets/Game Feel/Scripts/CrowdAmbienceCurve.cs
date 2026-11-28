using UnityEngine;

namespace PingPong.GameFeel
{
    public static class CrowdAmbienceCurve
    {
        const float SwellStrength = 0.8f;

        public static float Volume(float progress, float from, float resting, float lively)
        {
            float t = Mathf.Clamp01(progress);
            return Mathf.Lerp(from, resting, t) + lively * SwellStrength * Mathf.Sin(t * Mathf.PI);
        }
    }
}

using System;
using System.Collections;
using UnityEngine;

namespace PingPong.Presentation
{
    /// <summary>
    /// Small eased animations for UI feedback. Each one is a coroutine that exists only while it animates,
    /// so an idle UI costs nothing per frame. Run with <c>StartCoroutine(UiTween.Run(...))</c>.
    /// </summary>
    public static class UiTween
    {
        /// <summary>Calls 'apply' with 0..1 over 'seconds' (unscaled), ending exactly on 1.</summary>
        public static IEnumerator Run(float seconds, Action<float> apply)
        {
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                apply(t / seconds);
                yield return null;
            }
            apply(1f);
        }

        /// <summary>Overshoots past 1 then settles: a springy pop-in.</summary>
        public static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f, c3 = c1 + 1f;
            float u = t - 1f;
            return 1f + c3 * u * u * u + c1 * u * u;
        }

        public static float EaseInCubic(float t) => t * t * t;

        /// <summary>0 -> 1 -> 0, peaking mid-way: a quick punch.</summary>
        public static float Pulse(float t) => Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI);
    }
}

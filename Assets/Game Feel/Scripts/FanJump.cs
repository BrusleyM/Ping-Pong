using System.Collections.Generic;
using UnityEngine;

namespace PingPong.GameFeel
{
    /// <summary>
    /// Attached to every fan in the stadium. When the crowd cheers (FanJump.CheerAll),
    /// the fan jumps up and down and raises its arms. Each fan has its own timing so the
    /// crowd looks lively instead of moving in lockstep.
    /// The component switches itself off while the crowd is quiet, so idle fans cost nothing.
    /// </summary>
    public class FanJump : MonoBehaviour
    {
        [Tooltip("Jump height in metres at full intensity.")]
        public float jumpHeight = 0.3f;
        [Tooltip("Average jumps per second.")]
        public float jumpsPerSecond = 2.2f;
        [Tooltip("0 = always joins in, 1 = only joins the biggest cheers.")]
        [Range(0f, 1f)] public float eagerness = 0.5f;
        public Transform leftArm;
        public Transform rightArm;
        [Tooltip("This fan keeps its arms up even when not cheering.")]
        public bool armsAlwaysUp;

        const float RampIn = 0.25f;
        const float RampOut = 0.7f;

        static readonly List<FanJump> All = new List<FanJump>();
        static float s_start = -100f, s_end = -100f, s_intensity;

        Vector3 _base;
        float _phase, _rate;

        /// <summary>Make the whole crowd cheer for duration seconds (intensity 0..1).</summary>
        public static void CheerAll(float duration, float intensity)
        {
            float now = Time.time;
            if (now < s_end)
            {
                // Already cheering: extend and strengthen rather than restart.
                s_end = Mathf.Max(s_end, now + duration);
                s_intensity = Mathf.Max(s_intensity, Mathf.Clamp01(intensity));
            }
            else
            {
                s_start = now;
                s_end = now + duration;
                s_intensity = Mathf.Clamp01(intensity);
            }
            int woken = 0;
            for (int i = 0; i < All.Count; i++)
            {
                if (All[i] == null) continue;
                All[i].enabled = true;
                woken++;
            }
            Debug.Log($"[Game Feel] Crowd cheer: {woken} fans, {duration:0.0}s, intensity {intensity:0.00}");
        }

        public static bool IsCheering => Time.time < s_end;

        static float Envelope(float now)
        {
            if (now < s_start || now >= s_end) return 0f;
            float a = Mathf.Clamp01((now - s_start) / RampIn);
            float b = Mathf.Clamp01((s_end - now) / RampOut);
            return Mathf.Min(a, b) * s_intensity;
        }

        void Awake()
        {
            All.Add(this);
            _base = transform.localPosition;
            _phase = Random.value;
            _rate = jumpsPerSecond * Random.Range(0.85f, 1.15f);
            SetArms(armsAlwaysUp);
            enabled = IsCheering;
        }

        void OnDestroy()
        {
            All.Remove(this);
        }

        void Update()
        {
            float now = Time.time;
            if (now >= s_end)
            {
                // Cheer over: settle back down and go to sleep.
                transform.localPosition = _base;
                SetArms(armsAlwaysUp);
                enabled = false;
                return;
            }

            // Never zero while cheering (the first frame of a ramp would otherwise put fans to sleep).
            float env = Mathf.Max(Envelope(now), 0.05f * s_intensity);

            // The stronger the cheer, the more of the crowd joins in.
            bool joins = eagerness < 0.55f + 0.45f * env;
            if (!joins)
            {
                transform.localPosition = _base;
                SetArms(armsAlwaysUp);
                return;
            }

            transform.localPosition = _base + Vector3.up * JumpOffset(now, env, _phase, _rate);
            SetArms(true);
        }

        /// <summary>How high this fan is off its seat at a given time and cheer strength.</summary>
        public float JumpOffset(float time, float env, float phase, float rate)
        {
            float bounce = Mathf.Abs(Mathf.Sin(Mathf.PI * (time * rate + phase)));
            return bounce * jumpHeight * (0.35f + 0.65f * env);
        }

        /// <summary>Editor preview: pose the fan without Play mode. Returns false if it sits this one out.</summary>
        public bool PreviewPose(Vector3 basePos, float time, float env)
        {
            bool joins = env > 0f && eagerness < 0.55f + 0.45f * env;
            // Deterministic per-fan timing for the preview (Awake doesn't run in the Editor).
            float phase = Mathf.Repeat(eagerness * 7.31f, 1f);
            float rate = jumpsPerSecond * (0.85f + 0.3f * Mathf.Repeat(eagerness * 3.17f, 1f));
            transform.localPosition = joins ? basePos + Vector3.up * JumpOffset(time, env, phase, rate) : basePos;
            SetArms(joins || armsAlwaysUp);
            return joins;
        }

        void SetArms(bool up)
        {
            if (leftArm != null && leftArm.gameObject.activeSelf != up) leftArm.gameObject.SetActive(up);
            if (rightArm != null && rightArm.gameObject.activeSelf != up) rightArm.gameObject.SetActive(up);
        }
    }
}

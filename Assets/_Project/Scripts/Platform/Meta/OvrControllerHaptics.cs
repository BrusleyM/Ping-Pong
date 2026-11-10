using PingPong.Gameplay;
using UnityEngine;

namespace PingPong.Platform.Meta
{
    /// <summary>
    /// Haptic pulses on one Touch controller through OVRInput. With a PaddleGrip assigned, it vibrates
    /// whichever hand holds that paddle.
    /// </summary>
    public sealed class OvrControllerHaptics : MonoBehaviour, IHaptics
    {
        [SerializeField] OVRInput.Controller _controller = OVRInput.Controller.RTouch;
        [Tooltip("Optional: follow the hand holding this paddle.")]
        [SerializeField] PaddleGrip _grip;
        [Tooltip("Vibration frequency, 0-1 (OVRInput scale).")]
        [SerializeField, Range(0f, 1f)] float _frequency = 0.5f;

        float _stopAt = -1f;

        public OVRInput.Controller Controller { get => _controller; set => _controller = value; }

        public void Pulse(float amplitude, float seconds)
        {
            if (amplitude <= 0f || seconds <= 0f) return;
            OVRInput.SetControllerVibration(_frequency, Mathf.Clamp01(amplitude), _controller);
            _stopAt = Time.unscaledTime + seconds;
        }

        void Update()
        {
            if (_stopAt < 0f || Time.unscaledTime < _stopAt) return;
            Stop();
        }

        void OnEnable() { if (_grip != null) _grip.HeldByChanged += OnHeldByChanged; }

        void OnDisable()
        {
            if (_grip != null) _grip.HeldByChanged -= OnHeldByChanged;
            Stop();
        }

        void OnHeldByChanged(Hand hand)
        {
            if (hand == Hand.None) return;
            Stop();
            _controller = hand == Hand.Left ? OVRInput.Controller.LTouch : OVRInput.Controller.RTouch;
        }

        void Stop()
        {
            _stopAt = -1f;
            OVRInput.SetControllerVibration(0f, 0f, _controller);
        }
    }
}

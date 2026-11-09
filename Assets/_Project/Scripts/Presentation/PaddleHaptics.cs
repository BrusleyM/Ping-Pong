using PingPong.Gameplay;
using UnityEngine;

namespace PingPong.Presentation
{
    /// <summary>Vibrates the controller holding a paddle on each hit, harder for faster returns.</summary>
    public sealed class PaddleHaptics : MonoBehaviour
    {
        [SerializeField] PaddleHitter _hitter;
        [Tooltip("The controller to vibrate.")]
        [SerializeField] Seam<IHaptics> _haptics;
        [Tooltip("Return speed that gives a full-strength pulse, m/s.")]
        [SerializeField, Min(0.1f)] float _fullStrengthSpeed = 12f;
        [Tooltip("Pulse length, seconds.")]
        [SerializeField, Min(0f)] float _pulseSeconds = 0.04f;

        void OnValidate()
        {
            Validation.Require(_hitter, "the Paddle Hitter", this);
            Validation.Seam(_haptics, nameof(_haptics), this);
        }

        void OnEnable() => _hitter.Hit += OnHit;
        void OnDisable() => _hitter.Hit -= OnHit;

        void OnHit(PaddleHit hit) =>
            _haptics.Value?.Pulse(Mathf.Clamp01(hit.Velocity.magnitude / _fullStrengthSpeed), _pulseSeconds);
    }
}

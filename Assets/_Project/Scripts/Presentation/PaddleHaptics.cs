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

        void OnValidate()
        {
            Validation.Require(_hitter, "the Paddle Hitter", this);
            Validation.Seam(_haptics, nameof(_haptics), this);
        }

        void OnEnable() => _hitter.Hit += OnHit;
        void OnDisable() => _hitter.Hit -= OnHit;

        void OnHit(Vector3 position, Vector3 outVelocity)
        {
            var feel = _hitter.Feel;
            _haptics.Value?.Pulse(Mathf.Clamp01(outVelocity.magnitude / feel.HapticFullSpeed), feel.HapticDuration);
        }
    }
}

using UnityEngine;

namespace PingPong.Gameplay
{
    public sealed class SettingsApplier : MonoBehaviour
    {
        [SerializeField] private PaddleGrip _paddleGrip;

        private void OnEnable()
        {
            AppServices.Settings.Changed += Apply;
            Apply(AppServices.Settings.Current);
        }

        private void OnDisable()
        {
            AppServices.Settings.Changed -= Apply;
        }

        private void Apply(GameSettings settings)
        {
            AudioListener.volume = settings.EffectsVolume;
            if (_paddleGrip != null)
            {
                _paddleGrip.HandOnMatchStart = settings.DominantHand;
            }
        }
    }
}

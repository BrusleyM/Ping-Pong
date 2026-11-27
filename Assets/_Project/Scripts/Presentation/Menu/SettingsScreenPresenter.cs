using PingPong.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PingPong.Presentation
{
    public sealed class SettingsScreenPresenter : MonoBehaviour
    {
        [SerializeField] private Slider _effects;
        [SerializeField] private TMP_Text _effectsValue;
        [SerializeField] private PopButton _left;
        [SerializeField] private PopButton _right;
        [SerializeField] private GameObject _leftSelected;
        [SerializeField] private GameObject _rightSelected;
        [SerializeField] private PopButton _standing;
        [SerializeField] private PopButton _seated;
        [SerializeField] private GameObject _standingSelected;
        [SerializeField] private GameObject _seatedSelected;

        private bool _refreshing;

        private void Awake()
        {
            _effects.onValueChanged.AddListener(value => Apply(settings => settings.EffectsVolume = value));
            _left.Activated += () => Apply(settings => settings.DominantHand = Hand.Left);
            _right.Activated += () => Apply(settings => settings.DominantHand = Hand.Right);
            _standing.Activated += () => Apply(settings => settings.Posture = PlayPosture.Standing);
            _seated.Activated += () => Apply(settings => settings.Posture = PlayPosture.Seated);
        }

        private void OnEnable()
        {
            Refresh(AppServices.Settings.Current);
        }

        private void Apply(System.Action<GameSettings> change)
        {
            if (_refreshing)
            {
                return;
            }

            AppServices.Settings.Update(change);
            Refresh(AppServices.Settings.Current);
        }

        private void Refresh(GameSettings settings)
        {
            _refreshing = true;
            _effects.value = settings.EffectsVolume;
            _effectsValue.text = Mathf.RoundToInt(settings.EffectsVolume * 100f) + "%";
            _leftSelected.SetActive(settings.DominantHand == Hand.Left);
            _rightSelected.SetActive(settings.DominantHand == Hand.Right);
            _standingSelected.SetActive(settings.Posture == PlayPosture.Standing);
            _seatedSelected.SetActive(settings.Posture == PlayPosture.Seated);
            _refreshing = false;
        }
    }
}

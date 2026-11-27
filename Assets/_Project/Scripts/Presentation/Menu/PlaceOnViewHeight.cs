using Framework;
using PingPong.Gameplay;
using UnityEngine;

namespace PingPong.Presentation
{
    public sealed class PlaceOnViewHeight : MonoBehaviour
    {
        [SerializeField] private EyeHeightPlacer _placer;
        [SerializeField] private ViewHeightAdjuster _viewHeight;

        private void OnValidate()
        {
            Validation.Require(_placer, "the eye height placer", this);
            Validation.Require(_viewHeight, "the View Height Adjuster", this);
        }

        private void OnEnable()
        {
            if (_viewHeight == null || _placer == null)
            {
                return;
            }

            _viewHeight.Adjusted += _placer.Place;
        }

        private void OnDisable()
        {
            if (_viewHeight != null && _placer != null)
            {
                _viewHeight.Adjusted -= _placer.Place;
            }
        }
    }
}

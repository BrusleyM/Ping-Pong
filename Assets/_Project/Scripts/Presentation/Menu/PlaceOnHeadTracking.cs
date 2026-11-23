using Framework;
using PingPong.Gameplay;
using UnityEngine;

namespace PingPong.Presentation
{
    public sealed class PlaceOnHeadTracking : MonoBehaviour
    {
        [SerializeField] private EyeHeightPlacer _placer;
        [SerializeField] private Seam<IHeadTracking> _tracking;

        private void OnValidate()
        {
            Validation.Require(_placer, "the eye height placer", this);
            Validation.Seam(_tracking, nameof(_tracking), this);
        }

        private void OnEnable()
        {
            var tracking = _tracking.Value;
            if (tracking == null)
            {
                return;
            }

            tracking.Acquired += _placer.Place;
            tracking.Recentered += _placer.Place;
            tracking.PoseUpdated += PlaceOnFirstPose;
        }

        private void Start()
        {
            var tracking = _tracking.Value;
            if (tracking != null && tracking.IsTracked)
            {
                _placer.Place();
            }
        }

        private void OnDisable()
        {
            var tracking = _tracking.Value;
            if (tracking == null)
            {
                return;
            }

            tracking.Acquired -= _placer.Place;
            tracking.Recentered -= _placer.Place;
            tracking.PoseUpdated -= PlaceOnFirstPose;
        }

        private void PlaceOnFirstPose()
        {
            _tracking.Value.PoseUpdated -= PlaceOnFirstPose;
            if (_tracking.Value.IsTracked)
            {
                _placer.Place();
            }
        }
    }
}

using System;
using UnityEngine;

namespace PingPong.Gameplay
{
    public sealed class ViewHeightAdjuster : MonoBehaviour
    {
        [SerializeField] Seam<IHeadTracking> _tracking;
        [SerializeField] Transform _rig;
        [SerializeField] ViewHeightProfile _profile;
        [SerializeField] GameModeController _modes;

        PlayPosture _posture;
        float _floorHeight;
        float _lift;
        bool _waitingForPose;

        public event Action Adjusted;

        void Awake()
        {
            if (_rig != null) _floorHeight = _rig.localPosition.y;
        }

        void OnValidate()
        {
            Validation.Seam(_tracking, nameof(_tracking), this);
            Validation.Require(_rig, "the camera rig", this);
            Validation.Require(_profile, "a View Height Profile", this);
        }

        void OnEnable()
        {
            if (_tracking.Value == null || _rig == null || _profile == null) return;
            _posture = AppServices.Settings.Current.Posture;
            AppServices.Settings.Changed += OnSettingsChanged;
            _tracking.Value.Recentered += Calibrate;
            if (_modes != null) _modes.MatchStarted += Calibrate;
            Calibrate();
        }

        void OnDisable()
        {
            StopWaiting();
            AppServices.Settings.Changed -= OnSettingsChanged;
            if (_tracking.Value != null) _tracking.Value.Recentered -= Calibrate;
            if (_modes != null) _modes.MatchStarted -= Calibrate;
        }

        void OnSettingsChanged(GameSettings settings)
        {
            if (settings.Posture == _posture) return;
            _posture = settings.Posture;
            Calibrate();
        }

        void Calibrate()
        {
            if (_posture == PlayPosture.Standing)
            {
                StopWaiting();
                SetLift(0f);
                return;
            }

            if (_waitingForPose) return;
            _waitingForPose = true;
            _tracking.Value.PoseUpdated += LiftFromPose;
        }

        void LiftFromPose()
        {
            var tracking = _tracking.Value;
            if (!tracking.IsTracked || !_profile.Tuning.TryGetSeatedLift(tracking.EyeHeight, out float lift)) return;
            StopWaiting();
            SetLift(lift);
        }

        void StopWaiting()
        {
            if (!_waitingForPose) return;
            _waitingForPose = false;
            if (_tracking.Value != null) _tracking.Value.PoseUpdated -= LiftFromPose;
        }

        void SetLift(float lift)
        {
            if (Mathf.Approximately(lift, _lift)) return;
            _lift = lift;
            Vector3 position = _rig.localPosition;
            position.y = _floorHeight + lift;
            _rig.localPosition = position;
            Adjusted?.Invoke();
        }
    }
}

using System;
using PingPong.Gameplay;
using UnityEngine;

namespace PingPong.Platform.Meta
{
    public sealed class MetaHeadTracking : MonoBehaviour, IHeadTracking
    {
        private bool _recenterSubscribed;
        private OVRCameraRig _rig;

        public bool IsTracked => OVRManager.tracker != null && OVRManager.tracker.isPositionTracked;

        public float EyeHeight => _rig != null ? _rig.transform.InverseTransformPoint(_rig.centerEyeAnchor.position).y : 0f;

        public event Action Acquired;

        public event Action Recentered;

        public event Action PoseUpdated;

        private void OnEnable()
        {
            OVRManager.TrackingAcquired += OnAcquired;
            SubscribeToRecenter();
            SubscribeToRig();
        }

        private void Start()
        {
            SubscribeToRecenter();
            SubscribeToRig();
        }

        private void OnDisable()
        {
            OVRManager.TrackingAcquired -= OnAcquired;
            if (_rig != null)
            {
                _rig.UpdatedAnchors -= OnAnchorsUpdated;
                _rig = null;
            }

            if (_recenterSubscribed && OVRManager.display != null)
            {
                OVRManager.display.RecenteredPose -= OnRecentered;
            }

            _recenterSubscribed = false;
        }

        private void SubscribeToRecenter()
        {
            if (_recenterSubscribed || OVRManager.display == null)
            {
                return;
            }

            OVRManager.display.RecenteredPose += OnRecentered;
            _recenterSubscribed = true;
        }

        private void SubscribeToRig()
        {
            if (_rig != null)
            {
                return;
            }

            _rig = FindFirstObjectByType<OVRCameraRig>();
            if (_rig != null)
            {
                _rig.UpdatedAnchors += OnAnchorsUpdated;
            }
        }

        private void OnAnchorsUpdated(OVRCameraRig rig) => PoseUpdated?.Invoke();

        private void OnAcquired() => Acquired?.Invoke();

        private void OnRecentered() => Recentered?.Invoke();
    }
}

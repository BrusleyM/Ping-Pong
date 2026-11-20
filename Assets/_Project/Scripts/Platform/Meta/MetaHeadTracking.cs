using System;
using PingPong.Gameplay;
using UnityEngine;

namespace PingPong.Platform.Meta
{
    public sealed class MetaHeadTracking : MonoBehaviour, IHeadTracking
    {
        private bool _recenterSubscribed;

        public bool IsTracked => OVRManager.tracker != null && OVRManager.tracker.isPositionTracked;

        public event Action Acquired;

        public event Action Recentered;

        private void OnEnable()
        {
            OVRManager.TrackingAcquired += OnAcquired;
            SubscribeToRecenter();
        }

        private void Start()
        {
            SubscribeToRecenter();
        }

        private void OnDisable()
        {
            OVRManager.TrackingAcquired -= OnAcquired;
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

        private void OnAcquired() => Acquired?.Invoke();

        private void OnRecentered() => Recentered?.Invoke();
    }
}

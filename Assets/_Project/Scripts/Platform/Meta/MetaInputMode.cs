using System;
using PingPong.Core;
using PingPong.Gameplay;
using UnityEngine;

namespace PingPong.Platform.Meta
{
    public sealed class MetaInputMode : MonoBehaviour, IInputMode
    {
        [SerializeField, Min(0f)] private float _settleSeconds = 0.2f;

        private TrackingDebouncer _controllersLive;

        public InputMode Current { get; private set; } = InputMode.Controllers;

        public event Action<InputMode> Changed;

        private void Awake()
        {
            _controllersLive = new TrackingDebouncer(true, _settleSeconds, _settleSeconds);
        }

        private void Update()
        {
            OVRInput.Controller active = OVRInput.GetActiveController();
            bool handsOnly = (active & OVRInput.Controller.Hands) != 0 && (active & OVRInput.Controller.Touch) == 0;
            bool controllersLive = _controllersLive.Update(Time.unscaledTime, !handsOnly);
            InputMode mode = controllersLive ? InputMode.Controllers : InputMode.Hands;
            if (mode == Current)
            {
                return;
            }

            Current = mode;
            Changed?.Invoke(mode);
        }
    }
}

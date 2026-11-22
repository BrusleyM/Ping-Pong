using System;
using PingPong.Core;
using PingPong.Gameplay;
using UnityEngine;

namespace PingPong.Platform.Meta
{
    public sealed class MetaInputMode : MonoBehaviour, IInputMode
    {
        [SerializeField, Min(0f)] private float _settleSeconds = 0.2f;

        private InputModeTracker _tracker = new InputModeTracker(false, false, 0.2f);

        public InputMode Current => _tracker.ControllersLive ? InputMode.Controllers : InputMode.Hands;

        public event Action<InputMode> Changed;

        private void Awake()
        {
            OVRInput.Controller active = OVRInput.GetActiveController();
            _tracker = new InputModeTracker(AnyHand(active), AnyController(active), _settleSeconds);
        }

        private void Update()
        {
            OVRInput.Controller active = OVRInput.GetActiveController();
            if (_tracker.Update(Time.unscaledTime, AnyHand(active), AnyController(active)))
            {
                Changed?.Invoke(Current);
            }
        }

        private static bool AnyHand(OVRInput.Controller active) => (active & OVRInput.Controller.Hands) != 0;

        private static bool AnyController(OVRInput.Controller active) => (active & OVRInput.Controller.Touch) != 0;
    }
}

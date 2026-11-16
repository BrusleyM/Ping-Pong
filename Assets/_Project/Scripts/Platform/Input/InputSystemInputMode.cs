using System;
using System.Collections;
using PingPong.Gameplay;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;

namespace PingPong.Platform.Input
{
    public sealed class InputSystemInputMode : MonoBehaviour, IInputModeSource
    {
        [SerializeField] private InputAction _leftTracked = Tracked("Left Controller Tracked", "<XRController>{LeftHand}/isTracked");
        [SerializeField] private InputAction _rightTracked = Tracked("Right Controller Tracked", "<XRController>{RightHand}/isTracked");
        [Tooltip("Both controllers must read untracked this long before the mode becomes Hands, seconds.")]
        [SerializeField, Min(0f)] private float _lostAfter = 0.5f;
        [Tooltip("A controller must read tracked this long before the mode returns to Controllers, seconds.")]
        [SerializeField, Min(0f)] private float _regainAfter = 0.25f;

        private InputModeTracker _tracker;
        private Coroutine _settling;

        private InputModeTracker Tracker => _tracker ?? (_tracker = new InputModeTracker(_lostAfter, _regainAfter));

        public InputMode Current => Tracker.Current;

        public event Action<InputMode> Changed
        {
            add => Tracker.Changed += value;
            remove => Tracker.Changed -= value;
        }

        private void OnEnable()
        {
            _leftTracked.performed += OnTrackingChanged;
            _leftTracked.canceled += OnTrackingChanged;
            _rightTracked.performed += OnTrackingChanged;
            _rightTracked.canceled += OnTrackingChanged;
            InputSystem.onDeviceChange += OnDeviceChange;
            _leftTracked.Enable();
            _rightTracked.Enable();
            Refresh();
        }

        private void OnDisable()
        {
            _leftTracked.Disable();
            _rightTracked.Disable();
            _leftTracked.performed -= OnTrackingChanged;
            _leftTracked.canceled -= OnTrackingChanged;
            _rightTracked.performed -= OnTrackingChanged;
            _rightTracked.canceled -= OnTrackingChanged;
            InputSystem.onDeviceChange -= OnDeviceChange;
            _settling = null;
        }

        private void OnTrackingChanged(InputAction.CallbackContext _) => Refresh();

        private void OnDeviceChange(InputDevice device, InputDeviceChange change)
        {
            if (device is XRController)
            {
                Refresh();
            }
        }

        private void Refresh()
        {
            Tracker.Report(Time.unscaledTime, TrackedControllers.IsActive(Hand.Left), TrackedControllers.IsActive(Hand.Right));
            if (Tracker.Settling && _settling == null)
            {
                _settling = StartCoroutine(Settle());
            }
        }

        private IEnumerator Settle()
        {
            while (Tracker.Settling)
            {
                yield return null;
                Refresh();
            }

            _settling = null;
        }

        private static InputAction Tracked(string name, string binding)
        {
            var action = new InputAction(name, InputActionType.Button);
            action.AddBinding(binding);
            return action;
        }
    }
}

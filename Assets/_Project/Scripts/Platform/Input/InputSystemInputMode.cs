using System;
using PingPong.Gameplay;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PingPong.Platform.Input
{
    public sealed class InputSystemInputMode : MonoBehaviour, IInputModeSource
    {
        [SerializeField] private InputAction _leftTracked = Tracked("Left Controller Tracked", "<XRController>{LeftHand}/isTracked");
        [SerializeField] private InputAction _rightTracked = Tracked("Right Controller Tracked", "<XRController>{RightHand}/isTracked");

        private readonly InputModeTracker _tracker = new InputModeTracker();

        public InputMode Current => _tracker.Current;

        public event Action<InputMode> Changed
        {
            add => _tracker.Changed += value;
            remove => _tracker.Changed -= value;
        }

        private void OnEnable()
        {
            _leftTracked.performed += OnTrackingChanged;
            _leftTracked.canceled += OnTrackingChanged;
            _rightTracked.performed += OnTrackingChanged;
            _rightTracked.canceled += OnTrackingChanged;
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
        }

        private void OnTrackingChanged(InputAction.CallbackContext _) => Refresh();

        private void Refresh() => _tracker.Report(TrackedControllers.IsActive(Hand.Left), TrackedControllers.IsActive(Hand.Right));

        private static InputAction Tracked(string name, string binding)
        {
            var action = new InputAction(name, InputActionType.Button);
            action.AddBinding(binding);
            return action;
        }
    }
}

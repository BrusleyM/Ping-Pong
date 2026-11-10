using UnityEngine;
using UnityEngine.InputSystem;

namespace PingPong.Gameplay
{
    /// <summary>
    /// Player commands from Unity Input System actions. Defaults: grip = grab, A = restart, B = passthrough,
    /// X = easier, Y = harder, left menu button = menu on Touch controllers;
    /// G (grab right), R, P, minus, equals and Escape on the keyboard.
    /// Bindings are editable in the inspector.
    /// </summary>
    public sealed class InputSystemPlayerCommands : MonoBehaviour, IPlayerCommands
    {
        [SerializeField] InputAction _grabLeft = Button("Grab Left", "<XRController>{LeftHand}/gripPressed");
        [SerializeField] InputAction _grabRight = Button("Grab Right", "<XRController>{RightHand}/gripPressed", "<Keyboard>/g");
        [SerializeField] InputAction _restart = Button("Restart", "<XRController>{RightHand}/primaryButton", "<Keyboard>/r");
        [SerializeField] InputAction _togglePassthrough = Button("Toggle Passthrough", "<XRController>{RightHand}/secondaryButton", "<Keyboard>/p");
        [SerializeField] InputAction _easier = Button("Easier", "<XRController>{LeftHand}/primaryButton", "<Keyboard>/minus");
        [SerializeField] InputAction _harder = Button("Harder", "<XRController>{LeftHand}/secondaryButton", "<Keyboard>/equals");
        [SerializeField] InputAction _menu = Button("Menu", "<XRController>{LeftHand}/menuButton", "<Keyboard>/escape");

        public bool GrabPressed(Hand hand) => hand switch
        {
            Hand.Left => _grabLeft.WasPressedThisFrame(),
            Hand.Right => _grabRight.WasPressedThisFrame(),
            _ => false,
        };

        public bool RestartPressed => _restart.WasPressedThisFrame();
        public bool TogglePassthroughPressed => _togglePassthrough.WasPressedThisFrame();
        public bool EasierPressed => _easier.WasPressedThisFrame();
        public bool HarderPressed => _harder.WasPressedThisFrame();
        public bool MenuPressed => _menu.WasPressedThisFrame();

        void OnEnable()
        {
            _grabLeft.Enable(); _grabRight.Enable(); _restart.Enable(); _togglePassthrough.Enable(); _easier.Enable(); _harder.Enable(); _menu.Enable();
        }

        void OnDisable()
        {
            _grabLeft.Disable(); _grabRight.Disable(); _restart.Disable(); _togglePassthrough.Disable(); _easier.Disable(); _harder.Disable(); _menu.Disable();
        }

        static InputAction Button(string name, params string[] bindings)
        {
            var action = new InputAction(name, InputActionType.Button);
            foreach (string b in bindings) action.AddBinding(b);
            return action;
        }
    }
}

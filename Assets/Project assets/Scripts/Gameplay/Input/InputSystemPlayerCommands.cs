using UnityEngine;
using UnityEngine.InputSystem;

namespace PingPong.Gameplay
{
    /// <summary>
    /// Player commands from Unity Input System actions. Defaults: A = restart, B = passthrough,
    /// X = easier, Y = harder on Touch controllers; R, P, minus and equals on the keyboard.
    /// Bindings are editable in the inspector.
    /// </summary>
    public sealed class InputSystemPlayerCommands : MonoBehaviour, IPlayerCommands
    {
        [SerializeField] InputAction _restart = Button("Restart", "<XRController>{RightHand}/primaryButton", "<Keyboard>/r");
        [SerializeField] InputAction _togglePassthrough = Button("Toggle Passthrough", "<XRController>{RightHand}/secondaryButton", "<Keyboard>/p");
        [SerializeField] InputAction _easier = Button("Easier", "<XRController>{LeftHand}/primaryButton", "<Keyboard>/minus");
        [SerializeField] InputAction _harder = Button("Harder", "<XRController>{LeftHand}/secondaryButton", "<Keyboard>/equals");

        public bool RestartPressed => _restart.WasPressedThisFrame();
        public bool TogglePassthroughPressed => _togglePassthrough.WasPressedThisFrame();
        public bool EasierPressed => _easier.WasPressedThisFrame();
        public bool HarderPressed => _harder.WasPressedThisFrame();

        void OnEnable()
        {
            _restart.Enable(); _togglePassthrough.Enable(); _easier.Enable(); _harder.Enable();
        }

        void OnDisable()
        {
            _restart.Disable(); _togglePassthrough.Disable(); _easier.Disable(); _harder.Disable();
        }

        static InputAction Button(string name, params string[] bindings)
        {
            var action = new InputAction(name, InputActionType.Button);
            foreach (string b in bindings) action.AddBinding(b);
            return action;
        }
    }
}

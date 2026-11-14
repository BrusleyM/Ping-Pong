using UnityEngine;
using UnityEngine.InputSystem;

namespace PingPong.Gameplay
{
    /// <summary>
    /// Player commands from Unity Input System actions: grip = grab the paddle, left menu button = menu on
    /// Touch controllers; G (grab right) and Escape on the keyboard. Everything else (mode, difficulty,
    /// passthrough, restart) is in the menu. Bindings are editable in the inspector.
    /// </summary>
    public sealed class InputSystemPlayerCommands : MonoBehaviour, IPlayerCommands
    {
        [SerializeField] InputAction _grabLeft = Button("Grab Left", "<XRController>{LeftHand}/gripPressed");
        [SerializeField] InputAction _grabRight = Button("Grab Right", "<XRController>{RightHand}/gripPressed", "<Keyboard>/g");
        [SerializeField] InputAction _menu = Button("Menu", "<XRController>{LeftHand}/menuButton", "<Keyboard>/escape");

        public event System.Action<Hand> GrabPerformed;
        public event System.Action MenuPerformed;

        public bool ControllerActive(Hand hand)
        {
            UnityEngine.InputSystem.XR.XRController c = hand switch
            {
                Hand.Left => UnityEngine.InputSystem.XR.XRController.leftHand,
                Hand.Right => UnityEngine.InputSystem.XR.XRController.rightHand,
                _ => null,
            };
            return c == null || c.isTracked.isPressed;
        }

        void OnEnable()
        {
            _grabLeft.performed += OnGrabLeft;
            _grabRight.performed += OnGrabRight;
            _menu.performed += OnMenu;
            _grabLeft.Enable(); _grabRight.Enable(); _menu.Enable();
        }

        void OnDisable()
        {
            _grabLeft.Disable(); _grabRight.Disable(); _menu.Disable();
            _grabLeft.performed -= OnGrabLeft;
            _grabRight.performed -= OnGrabRight;
            _menu.performed -= OnMenu;
        }

        void OnGrabLeft(InputAction.CallbackContext _) => GrabPerformed?.Invoke(Hand.Left);
        void OnGrabRight(InputAction.CallbackContext _) => GrabPerformed?.Invoke(Hand.Right);
        void OnMenu(InputAction.CallbackContext _) => MenuPerformed?.Invoke();

        static InputAction Button(string name, params string[] bindings)
        {
            var action = new InputAction(name, InputActionType.Button);
            foreach (string b in bindings) action.AddBinding(b);
            return action;
        }
    }
}

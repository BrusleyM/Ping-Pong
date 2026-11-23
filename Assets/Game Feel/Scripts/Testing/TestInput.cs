using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace PingPong.GameFeel.Testing
{
    /// <summary>
    /// Keyboard/mouse helper for the test scene. Works with the new Input System (this project's setting)
    /// and falls back to the old Input Manager if that is what's enabled.
    /// </summary>
    public static class TestInput
    {
        public enum K { D1, D2, D3, D4, D5, D6, D7, D8, D9, Space, W, A, S, D, Q, E, Shift, F1, F2, F3, F4, F5, F6, Left, Right }

#if ENABLE_INPUT_SYSTEM
        static Key Map(K k)
        {
            switch (k)
            {
                case K.D1: return Key.Digit1; case K.D2: return Key.Digit2; case K.D3: return Key.Digit3;
                case K.D4: return Key.Digit4; case K.D5: return Key.Digit5; case K.D6: return Key.Digit6;
                case K.D7: return Key.Digit7; case K.D8: return Key.Digit8; case K.D9: return Key.Digit9;
                case K.Space: return Key.Space; case K.W: return Key.W; case K.A: return Key.A;
                case K.S: return Key.S; case K.D: return Key.D; case K.Q: return Key.Q; case K.E: return Key.E;
                case K.F1: return Key.F1; case K.F2: return Key.F2; case K.F3: return Key.F3;
                case K.F4: return Key.F4; case K.F5: return Key.F5; case K.F6: return Key.F6;
                case K.Left: return Key.LeftArrow; case K.Right: return Key.RightArrow;
                default: return Key.LeftShift;
            }
        }

        public static bool Pressed(K k) { var kb = Keyboard.current; return kb != null && kb[Map(k)].wasPressedThisFrame; }
        public static bool Held(K k) { var kb = Keyboard.current; return kb != null && kb[Map(k)].isPressed; }
        public static bool RightMouse() { var m = Mouse.current; return m != null && m.rightButton.isPressed; }
        public static Vector2 MouseDelta() { var m = Mouse.current; return m != null ? m.delta.ReadValue() * 0.1f : Vector2.zero; }
#else
        static KeyCode Map(K k)
        {
            switch (k)
            {
                case K.D1: return KeyCode.Alpha1; case K.D2: return KeyCode.Alpha2; case K.D3: return KeyCode.Alpha3;
                case K.D4: return KeyCode.Alpha4; case K.D5: return KeyCode.Alpha5; case K.D6: return KeyCode.Alpha6;
                case K.D7: return KeyCode.Alpha7; case K.D8: return KeyCode.Alpha8; case K.D9: return KeyCode.Alpha9;
                case K.Space: return KeyCode.Space; case K.W: return KeyCode.W; case K.A: return KeyCode.A;
                case K.S: return KeyCode.S; case K.D: return KeyCode.D; case K.Q: return KeyCode.Q; case K.E: return KeyCode.E;
                case K.F1: return KeyCode.F1; case K.F2: return KeyCode.F2; case K.F3: return KeyCode.F3;
                case K.F4: return KeyCode.F4; case K.F5: return KeyCode.F5; case K.F6: return KeyCode.F6;
                case K.Left: return KeyCode.LeftArrow; case K.Right: return KeyCode.RightArrow;
                default: return KeyCode.LeftShift;
            }
        }

        public static bool Pressed(K k) => Input.GetKeyDown(Map(k));
        public static bool Held(K k) => Input.GetKey(Map(k));
        public static bool RightMouse() => Input.GetMouseButton(1);
        public static Vector2 MouseDelta() => new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
#endif
    }
}

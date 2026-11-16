using System;

namespace PingPong.Gameplay
{
    public sealed class InputModeTracker : IInputModeSource
    {
        public InputMode Current { get; private set; } = InputMode.Controllers;

        public event Action<InputMode> Changed;

        public void Report(bool leftControllerActive, bool rightControllerActive)
        {
            var mode = leftControllerActive || rightControllerActive ? InputMode.Controllers : InputMode.Hands;
            if (mode == Current)
            {
                return;
            }

            Current = mode;
            Changed?.Invoke(mode);
        }
    }
}

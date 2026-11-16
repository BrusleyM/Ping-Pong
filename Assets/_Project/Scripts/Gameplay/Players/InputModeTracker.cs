using System;
using PingPong.Core;

namespace PingPong.Gameplay
{
    public sealed class InputModeTracker : IInputModeSource
    {
        private TrackingDebouncer _controllersTracked;

        public InputModeTracker(float lostAfterSeconds, float regainAfterSeconds)
        {
            _controllersTracked = new TrackingDebouncer(true, lostAfterSeconds, regainAfterSeconds);
        }

        public InputMode Current { get; private set; } = InputMode.Controllers;

        public bool Settling { get; private set; }

        public event Action<InputMode> Changed;

        public void Report(float now, bool leftControllerActive, bool rightControllerActive)
        {
            var raw = leftControllerActive || rightControllerActive;
            var stable = _controllersTracked.Update(now, raw);
            Settling = stable != raw;

            var mode = stable ? InputMode.Controllers : InputMode.Hands;
            if (mode == Current)
            {
                return;
            }

            Current = mode;
            Changed?.Invoke(mode);
        }
    }
}

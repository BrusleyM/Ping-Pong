using System;
using Framework.Sequencing;

namespace PingPong.Tutorial
{
    public sealed class EventCondition : IStepCondition
    {
        private readonly Func<Action, Action> _listen;
        private readonly Func<bool> _isAlreadySatisfied;
        private Action _stopListening;

        public EventCondition(Func<Action, Action> listen, Func<bool> isAlreadySatisfied = null)
        {
            _listen = listen;
            _isAlreadySatisfied = isAlreadySatisfied;
        }

        public event Action Satisfied;

        public void Arm()
        {
            Disarm();
            _stopListening = _listen(RaiseSatisfied);

            if (_isAlreadySatisfied != null && _isAlreadySatisfied())
            {
                RaiseSatisfied();
            }
        }

        public void Disarm()
        {
            _stopListening?.Invoke();
            _stopListening = null;
        }

        private void RaiseSatisfied()
        {
            Satisfied?.Invoke();
        }
    }
}

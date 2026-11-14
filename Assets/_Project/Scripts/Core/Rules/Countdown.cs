namespace PingPong.Core
{
    /// <summary>A one-shot timer on a clock the caller supplies, so it is testable without Unity time.</summary>
    public struct Countdown
    {
        float _dueAt;
        bool _pending;

        public bool IsPending => _pending;

        public void Start(float now, float seconds)
        {
            _dueAt = now + seconds;
            _pending = true;
        }

        public void Cancel() => _pending = false;

        /// <summary>True once, when the time has come; the countdown is then finished.</summary>
        public bool TryFinish(float now)
        {
            if (!_pending || now < _dueAt) return false;
            _pending = false;
            return true;
        }
    }
}

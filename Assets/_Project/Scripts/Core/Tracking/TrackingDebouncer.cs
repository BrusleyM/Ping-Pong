namespace PingPong.Core
{
    /// <summary>
    /// Smooths a tracking flag that flickers: the stable state flips to false only after the raw value has been false for
    /// <c>lostAfter</c>, and back to true only after it has been true for <c>regainAfter</c>. Pure, on a clock the caller supplies.
    /// </summary>
    public struct TrackingDebouncer
    {
        readonly float _lostAfter, _regainAfter;
        bool _stable;
        float _differingSince;

        public TrackingDebouncer(bool initial, float lostAfter, float regainAfter)
        {
            _stable = initial;
            _lostAfter = lostAfter;
            _regainAfter = regainAfter;
            _differingSince = -1f;
        }

        public bool Stable => _stable;

        /// <summary>Feeds this frame's raw value; returns the stable state.</summary>
        public bool Update(float now, bool raw)
        {
            if (raw == _stable) { _differingSince = -1f; return _stable; }
            if (_differingSince < 0f) _differingSince = now;
            if (now - _differingSince >= (_stable ? _lostAfter : _regainAfter))
            {
                _stable = raw;
                _differingSince = -1f;
            }
            return _stable;
        }
    }
}

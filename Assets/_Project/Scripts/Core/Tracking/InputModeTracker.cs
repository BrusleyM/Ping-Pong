namespace PingPong.Core
{
    public struct InputModeTracker
    {
        TrackingDebouncer _controllersLive;

        public InputModeTracker(bool anyHandActive, bool anyControllerActive, float settleSeconds)
        {
            _controllersLive = new TrackingDebouncer(!HandsOnly(anyHandActive, anyControllerActive), settleSeconds, settleSeconds);
        }

        public bool ControllersLive => _controllersLive.Stable;

        public bool Update(float now, bool anyHandActive, bool anyControllerActive)
        {
            bool before = _controllersLive.Stable;
            _controllersLive.Update(now, !HandsOnly(anyHandActive, anyControllerActive));
            return _controllersLive.Stable != before;
        }

        static bool HandsOnly(bool anyHandActive, bool anyControllerActive) => anyHandActive && !anyControllerActive;
    }
}

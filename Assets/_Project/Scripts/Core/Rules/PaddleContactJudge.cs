namespace PingPong.Core
{
    /// <summary>
    /// Decides which physical paddle contacts count as hits. One swing can touch the ball twice in a few milliseconds (face then
    /// rim, or the paddle still moving into it): only the first contact of a swing counts, so that is not called a double hit.
    /// Pure, on a clock the caller supplies.
    /// </summary>
    public sealed class PaddleContactJudge
    {
        readonly float _grace;
        readonly float[] _lastCounted = { float.NegativeInfinity, float.NegativeInfinity };

        public PaddleContactJudge(float graceSeconds) => _grace = graceSeconds;

        /// <summary>True if this contact counts: the player's last counted contact was at least the grace period ago.</summary>
        public bool ShouldCount(Side player, float now)
        {
            if (!player.IsPlayer()) return false;
            int i = player == Side.Far ? 1 : 0;
            if (now - _lastCounted[i] < _grace) return false;
            _lastCounted[i] = now;
            return true;
        }

        public void Reset() { _lastCounted[0] = _lastCounted[1] = float.NegativeInfinity; }
    }
}

using System.Collections.Generic;

namespace PingPong.Gameplay
{
    public sealed class GrabPoints
    {
        private readonly HashSet<int> _holding = new HashSet<int>();

        public bool Begin(int id)
        {
            var wasFree = _holding.Count == 0;
            _holding.Add(id);
            return wasFree;
        }

        public bool End(int id)
        {
            return _holding.Remove(id) && _holding.Count == 0;
        }
    }
}

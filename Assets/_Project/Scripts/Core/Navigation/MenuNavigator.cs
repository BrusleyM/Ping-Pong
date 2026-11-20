using System;
using System.Collections.Generic;

namespace PingPong.Core
{
    public sealed class MenuNavigator
    {
        private readonly List<MenuScreen> _trail = new List<MenuScreen>();

        public MenuNavigator(MenuScreen start)
        {
            _trail.Add(start);
        }

        public MenuScreen Current => _trail[_trail.Count - 1];

        public bool CanGoBack => _trail.Count > 1;

        public event Action<MenuScreen> Changed;

        public void Go(MenuScreen screen)
        {
            if (screen == Current)
            {
                return;
            }

            _trail.Add(screen);
            Changed?.Invoke(Current);
        }

        public bool Back()
        {
            if (!CanGoBack)
            {
                return false;
            }

            _trail.RemoveAt(_trail.Count - 1);
            Changed?.Invoke(Current);
            return true;
        }

        public void ResetTo(MenuScreen screen)
        {
            _trail.Clear();
            _trail.Add(screen);
            Changed?.Invoke(Current);
        }
    }
}

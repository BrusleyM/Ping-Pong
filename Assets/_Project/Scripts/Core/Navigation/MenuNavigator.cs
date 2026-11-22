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

        public static MenuNavigator Entering(MenuScreen target)
        {
            var navigator = new MenuNavigator(MenuScreen.Main);
            foreach (var screen in TrailTo(target))
            {
                navigator._trail.Add(screen);
            }

            return navigator;
        }

        private static MenuScreen[] TrailTo(MenuScreen target)
        {
            switch (target)
            {
                case MenuScreen.ChooseWhere:
                    return new[] { MenuScreen.ChooseWhere };
                case MenuScreen.ChooseWho:
                    return new[] { MenuScreen.ChooseWhere, MenuScreen.ChooseWho };
                case MenuScreen.ChooseDifficulty:
                    return new[] { MenuScreen.ChooseWhere, MenuScreen.ChooseWho, MenuScreen.ChooseDifficulty };
                case MenuScreen.Settings:
                    return new[] { MenuScreen.Settings };
                default:
                    return new MenuScreen[0];
            }
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

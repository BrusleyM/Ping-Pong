using System;
using PingPong.Core;
using UnityEngine;

namespace PingPong.Presentation
{
    public sealed class MenuScreenSwitcher : MonoBehaviour
    {
        [Serializable]
        public struct ScreenPanel
        {
            public MenuScreen Screen;
            public PanelPop Panel;
        }

        [Serializable]
        public struct NavLink
        {
            public PopButton Button;
            public MenuScreen Target;
            public bool GoesBack;
        }

        [SerializeField] private ScreenPanel[] _panels;
        [SerializeField] private NavLink[] _links;

        private bool _linked;

        public MenuNavigator Navigator { get; private set; }

        public event Action<MenuScreen> Entered;

        public void Begin(MenuNavigator navigator)
        {
            if (Navigator != null)
            {
                Navigator.Changed -= OnChanged;
            }

            Navigator = navigator;
            Navigator.Changed += OnChanged;
            LinkButtons();
            foreach (var entry in _panels)
            {
                if (entry.Screen == navigator.Current)
                {
                    entry.Panel.ShowNow();
                }
                else
                {
                    entry.Panel.HideNow();
                }
            }

            Entered?.Invoke(navigator.Current);
        }

        private void LinkButtons()
        {
            if (_linked)
            {
                return;
            }

            _linked = true;
            foreach (var link in _links)
            {
                var captured = link;
                captured.Button.Activated += () => OnLink(captured);
            }
        }

        private void OnLink(NavLink link)
        {
            if (link.GoesBack)
            {
                Navigator.Back();
                return;
            }

            Navigator.Go(link.Target);
        }

        private void OnChanged(MenuScreen screen)
        {
            foreach (var entry in _panels)
            {
                if (entry.Screen == screen)
                {
                    entry.Panel.Show();
                }
                else
                {
                    entry.Panel.Hide();
                }
            }

            Entered?.Invoke(screen);
        }
    }
}

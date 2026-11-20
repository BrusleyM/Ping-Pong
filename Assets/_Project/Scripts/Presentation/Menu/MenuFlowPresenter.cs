using PingPong.Core;
using PingPong.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PingPong.Presentation
{
    public sealed class MenuFlowPresenter : MonoBehaviour
    {
        [SerializeField] private MenuScreenSwitcher _switcher;
        [SerializeField] private ProfileScreenPresenter _profile;
        [SerializeField] private PopButton _tutorial;
        [SerializeField] private PopButton _online;
        [SerializeField] private PopButton _friend;
        [SerializeField] private PopButton _editProfile;
        [SerializeField] private PopButton _quit;
        [SerializeField] private TMP_Text _chipName;
        [SerializeField] private UiTheme _theme;
        [SerializeField] private Image _chipDot;
        [SerializeField] private ConfirmDialog _confirm;
        [SerializeField] private ToastPresenter _toast;

        private void Awake()
        {
            _tutorial.Locked = true;
            _online.Locked = true;
            _friend.Locked = true;
            _tutorial.Refused += () => _toast.Show("The tutorial is coming soon!");
            _online.Refused += () => _toast.Show("Online play is coming soon!");
            _friend.Refused += () => _toast.Show("Playing with a friend is coming soon!");
            _editProfile.Activated += OpenProfileEditor;
            _quit.Activated += AskToQuit;
            _profile.Saved += OnProfileSaved;
            _switcher.Entered += EnterScreen;
        }

        private void Start()
        {
            _switcher.Begin(StartingNavigator());
            RefreshChip();
        }

        private static MenuNavigator StartingNavigator()
        {
            if (!AppServices.Profile.HasProfile)
            {
                return new MenuNavigator(MenuScreen.Profile);
            }

            var entry = AppServices.MenuEntry;
            AppServices.MenuEntry = null;
            return entry.HasValue ? MenuNavigator.Entering(entry.Value) : new MenuNavigator(MenuScreen.Main);
        }

        private void OpenProfileEditor()
        {
            _switcher.Navigator.Go(MenuScreen.Profile);
        }

        private void OnProfileSaved()
        {
            RefreshChip();
            _toast.Show("Profile saved!");
            if (_switcher.Navigator.CanGoBack)
            {
                _switcher.Navigator.Back();
            }
            else
            {
                _switcher.Navigator.ResetTo(MenuScreen.Main);
            }
        }

        private void AskToQuit()
        {
            _confirm.Open("Leave Ping Pong Mania?", "Your profile and settings are saved on this headset.", "Leave game", Application.Quit);
        }

        private void EnterScreen(MenuScreen screen)
        {
            if (screen == MenuScreen.Profile)
            {
                _profile.Open();
            }
        }

        private void RefreshChip()
        {
            var profile = AppServices.Profile.Current;
            _chipName.text = profile != null ? profile.Nickname : "Loading…";
            if (profile != null)
            {
                _chipDot.color = _theme.ColourOf(profile.Team);
            }
        }
    }
}

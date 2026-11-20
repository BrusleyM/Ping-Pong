using System.Collections;
using Framework;
using PingPong.Core;
using PingPong.Gameplay;
using TMPro;
using UnityEngine;

namespace PingPong.Presentation
{
    public sealed class PauseMenuPresenter : MonoBehaviour
    {
        [SerializeField] private GameModeController _modes;
        [SerializeField] private MenuScreenSwitcher _switcher;
        [SerializeField] private GameObject _root;
        [SerializeField] private EyeHeightPlacer _placer;
        [SerializeField] private PopButton _resume;
        [SerializeField] private PopButton _restart;
        [SerializeField] private PopButton _passthrough;
        [SerializeField] private TMP_Text _passthroughState;
        [SerializeField] private PopButton _quitToMenu;
        [SerializeField] private PopButton _playAgain;
        [SerializeField] private PopButton _changeDifficulty;
        [SerializeField] private PopButton _mainMenu;
        [SerializeField] private TMP_Text _resultTitle;
        [SerializeField] private TMP_Text _resultScore;
        [SerializeField] private ConfirmDialog _confirm;
        [SerializeField] private Seam<ISceneLoader> _loader;
        [SerializeField, Min(0f)] private float _gameOverDelay = 2.5f;

        private Coroutine _gameOverOpen;

        public bool IsOpen => _root.activeSelf;

        private void OnValidate()
        {
            Validation.Seam(_loader, nameof(_loader), this);
        }

        private void Awake()
        {
            _resume.Activated += Close;
            _restart.Activated += RestartMatch;
            _playAgain.Activated += RestartMatch;
            _passthrough.Activated += TogglePassthrough;
            _quitToMenu.Activated += AskToQuit;
            _mainMenu.Activated += () => GoToMenu(null);
            _changeDifficulty.Activated += () => GoToMenu(MenuScreen.ChooseDifficulty);
        }

        private void Start()
        {
            _switcher.Begin(new MenuNavigator(MenuScreen.Pause));
            _root.SetActive(false);
        }

        private void OnEnable()
        {
            _modes.MenuRequested += Toggle;
            _modes.GameEnded += OnGameEnded;
            _modes.MatchStarted += CancelGameOverOpen;
        }

        private void OnDisable()
        {
            _modes.MenuRequested -= Toggle;
            _modes.GameEnded -= OnGameEnded;
            _modes.MatchStarted -= CancelGameOverOpen;
        }

        public void Toggle()
        {
            if (IsOpen)
            {
                Close();
                return;
            }

            Open(MenuScreen.Pause);
        }

        public void Open(MenuScreen screen)
        {
            _placer.TryPlace();
            _root.SetActive(true);
            _switcher.Navigator.ResetTo(screen);
            RefreshPassthrough();
        }

        public void Close()
        {
            _root.SetActive(false);
        }

        private void OnGameEnded()
        {
            CancelGameOverOpen();
            _gameOverOpen = StartCoroutine(OpenAfterGameOver());
        }

        private IEnumerator OpenAfterGameOver()
        {
            yield return new WaitForSecondsRealtime(_gameOverDelay);
            _gameOverOpen = null;
            var won = _modes.LastWinner == _modes.LocalPlayer;
            _resultTitle.text = won ? "You win!" : "You lose";
            _resultScore.text = _modes.ScoreOf(_modes.LocalPlayer) + " : " + _modes.ScoreOf(Opposite(_modes.LocalPlayer));
            Open(MenuScreen.GameOver);
        }

        private static Side Opposite(Side side) => side == Side.Near ? Side.Far : Side.Near;

        private void CancelGameOverOpen()
        {
            if (_gameOverOpen == null)
            {
                return;
            }

            StopCoroutine(_gameOverOpen);
            _gameOverOpen = null;
        }

        private void RestartMatch()
        {
            Close();
            _modes.Restart();
        }

        private void TogglePassthrough()
        {
            _modes.TogglePassthrough();
            RefreshPassthrough();
        }

        private void RefreshPassthrough()
        {
            _passthroughState.text = "Passthrough: " + (_modes.PassthroughOn ? "ON" : "OFF");
        }

        private void AskToQuit()
        {
            _confirm.Open("Quit this match?", "The current score will be lost.", "Quit match", () => GoToMenu(null));
        }

        private void GoToMenu(MenuScreen? entry)
        {
            var loader = _loader.Value;
            if (loader.IsLoading)
            {
                return;
            }

            AppServices.MenuEntry = entry;
            loader.Load(SceneNames.Menu);
        }
    }
}

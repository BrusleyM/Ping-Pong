using Framework.Sequencing;
using PingPong.Gameplay;
using PingPong.Presentation;
using UnityEngine;

namespace PingPong.Tutorial
{
    [RequireComponent(typeof(SequentialTask))]
    public sealed class TutorialDirector : MonoBehaviour
    {
        [SerializeField] private GameModeController _modes;
        [SerializeField] private MatchController _match;
        [SerializeField] private PaddleGrip _paddle;
        [SerializeField] private Seam<IPlayerCommands> _commands;
        [SerializeField] private Seam<IMenuGesture> _menuGesture;
        [SerializeField] private ModeMenuPresenter _menu;
        [SerializeField] private TutorialPromptView _prompt;
        [SerializeField] private TutorialProfile _profile;
        [SerializeField] private bool _playOnStart = true;

        private SequentialTask _task;
        private SequenceRunner _tutorial;

        private void OnValidate()
        {
            Validation.Require(_modes, "the Game Mode Controller", this);
            Validation.Require(_match, "the Match Controller", this);
            Validation.Require(_paddle, "the paddle grip", this);
            Validation.Require(_menu, "the mode menu", this);
            Validation.Require(_prompt, "the prompt view", this);
            Validation.Require(_profile, "the tutorial profile", this);
            Validation.Seam(_commands, nameof(_commands), this);
            Validation.Seam(_menuGesture, nameof(_menuGesture), this);
        }

        private void Awake()
        {
            _task = GetComponent<SequentialTask>();
            var signals = TutorialSignalFactory.Create(_paddle, _commands.Value, _menuGesture.Value, _match, _modes, _modes.LocalPlayer);
            _tutorial = TutorialPlan.Build(signals, _prompt, _profile.Timing);
        }

        private void OnEnable()
        {
            _tutorial.Completed += OpenMenuAfterTutorial;
            _menu.TutorialRequested += Replay;
        }

        private void OnDisable()
        {
            _tutorial.Completed -= OpenMenuAfterTutorial;
            _menu.TutorialRequested -= Replay;
        }

        private void Start()
        {
            if (_playOnStart)
            {
                Play();
            }
        }

        public void Replay()
        {
            _match.StopMatch();
            Play();
        }

        private void Play()
        {
            if (_task.HasRun)
            {
                _task.Restart();
            }
            else
            {
                _task.Play(_tutorial);
            }
        }

        private void OpenMenuAfterTutorial()
        {
            _match.StopMatch();
            _menu.Open();
        }
    }
}

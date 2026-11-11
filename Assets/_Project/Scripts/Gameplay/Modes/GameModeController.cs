using System;
using PingPong.Core;
using UnityEngine;

namespace PingPong.Gameplay
{
    public enum GameMode { Solo, Online }

    /// <summary>
    /// Starts and switches game modes. Nothing starts on its own: the player picks a mode in the menu
    /// (<see cref="StartSolo"/>). The menu button only asks for the menu (<see cref="MenuRequested"/>);
    /// presentation decides how to show it.
    /// </summary>
    public sealed class GameModeController : MonoBehaviour
    {
        [SerializeField] MatchController _match;
        [SerializeField] AIOpponent _ai;
        [SerializeField] Seam<IPlayerCommands> _commands;
        [SerializeField] Seam<IPassthrough> _passthrough;
        [SerializeField] Side _localPlayer = Side.Near;

        public GameMode Mode { get; private set; } = GameMode.Solo;
        public Side LocalPlayer => _localPlayer;
        public AIOpponent AI => _ai;
        public bool PassthroughOn => _passthrough.Value != null && _passthrough.Value.IsOn;
        /// <summary>Online play needs the Fusion network layer, which isn't in the build yet.</summary>
        public bool OnlineAvailable => false;

        /// <summary>Raised when the mode or AI level changes.</summary>
        public event Action Changed;

        /// <summary>Raised when the player presses the menu button.</summary>
        public event Action MenuRequested;

        /// <summary>Raised when a match starts (or restarts), after the match is set up.</summary>
        public event Action MatchStarted;

        bool CanRestart => _match.Phase is MatchPhase.GameOver or MatchPhase.FreePlay;
        /// <summary>No rally in play: waiting for a serve, game over or free play.</summary>
        public bool BetweenPoints => CanRestart || _match.Phase == MatchPhase.WaitingForServe;

        void OnValidate()
        {
            Validation.Seam(_commands, nameof(_commands), this);
            Validation.Seam(_passthrough, nameof(_passthrough), this);
        }

        void OnEnable() { if (_ai != null) _ai.LevelChanged += OnLevelChanged; }
        void OnDisable() { if (_ai != null) _ai.LevelChanged -= OnLevelChanged; }

        public void StartSolo(int level)
        {
            Mode = GameMode.Solo;
            if (_ai != null) { _ai.Level = level; _ai.enabled = true; }
            _match.IsAuthority = true;
            _match.StartMatch(_localPlayer);
            Changed?.Invoke();
            MatchStarted?.Invoke();
        }

        public void Restart()
        {
            if (Mode == GameMode.Solo) StartSolo(_ai != null ? _ai.Level : 0);
        }

        public void TogglePassthrough()
        {
            IPassthrough passthrough = _passthrough.Value;
            if (passthrough == null) return;
            passthrough.Toggle();
            Changed?.Invoke();
        }

        void OnLevelChanged(int level) => Changed?.Invoke();

        void Update()
        {
            IPlayerCommands c = _commands.Value;
            if (c == null) return;
            if (c.MenuPressed) MenuRequested?.Invoke();
        }
    }
}

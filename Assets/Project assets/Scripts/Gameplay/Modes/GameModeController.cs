using System;
using PingPong.Core;
using UnityEngine;

namespace PingPong.Gameplay
{
    public enum GameMode { Solo, Online }

    /// <summary>
    /// Starts and switches game modes, and maps player commands to them. Restart only works once the
    /// game is over; difficulty changes apply between points and keep the score, so a thumb resting on a
    /// button can't wipe a match. Passthrough can be toggled at any time.
    /// </summary>
    public sealed class GameModeController : MonoBehaviour
    {
        [SerializeField] MatchController _match;
        [SerializeField] AIOpponent _ai;
        [SerializeField] Seam<IPlayerCommands> _commands;
        [SerializeField] Seam<IPassthrough> _passthrough;
        [SerializeField] bool _startSoloOnPlay = true;
        [SerializeField] Side _localPlayer = Side.Near;

        public GameMode Mode { get; private set; } = GameMode.Solo;
        public Side LocalPlayer => _localPlayer;
        public AIOpponent AI => _ai;

        /// <summary>Raised when the mode or AI level changes.</summary>
        public event Action Changed;

        bool GameOver => _match.Phase is MatchPhase.GameOver or MatchPhase.FreePlay;
        bool BetweenPoints => GameOver || _match.Phase == MatchPhase.WaitingForServe;

        void OnValidate()
        {
            foreach (string error in new[] { _commands.Validate(nameof(_commands)), _passthrough.Validate(nameof(_passthrough)) })
                if (error != null) Debug.LogError(error, this);
        }

        void OnEnable() { if (_ai != null) _ai.LevelChanged += OnLevelChanged; }
        void OnDisable() { if (_ai != null) _ai.LevelChanged -= OnLevelChanged; }

        void Start()
        {
            if (_startSoloOnPlay) StartSolo(_ai != null ? _ai.Level : 0);
        }

        public void StartSolo(int level)
        {
            Mode = GameMode.Solo;
            if (_ai != null) { _ai.Level = level; _ai.enabled = true; }
            _match.IsAuthority = true;
            _match.StartMatch(_localPlayer);
            Changed?.Invoke();
        }

        public void Restart()
        {
            if (Mode == GameMode.Solo) StartSolo(_ai != null ? _ai.Level : 0);
        }

        /// <summary>Changes the AI level for the next point; the score is kept.</summary>
        public void ChangeDifficulty(int delta)
        {
            if (_ai != null) _ai.Level += delta;
        }

        public void TogglePassthrough() => _passthrough.Value?.Toggle();

        void OnLevelChanged(int level) => Changed?.Invoke();

        void Update()
        {
            IPlayerCommands c = _commands.Value;
            if (c == null) return;
            if (c.TogglePassthroughPressed) TogglePassthrough();
            if (GameOver && c.RestartPressed) Restart();
            if (BetweenPoints && c.EasierPressed) ChangeDifficulty(-1);
            if (BetweenPoints && c.HarderPressed) ChangeDifficulty(1);
        }
    }
}

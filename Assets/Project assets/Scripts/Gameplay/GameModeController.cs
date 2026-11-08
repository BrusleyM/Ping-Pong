using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace PingPong.Gameplay
{
    public enum GameMode { Solo, Online }

    /// <summary>
    /// Starts and switches game modes. Until the menu exists, controller shortcuts:
    /// A = restart, B = passthrough on/off, X/Y = AI easier/harder.
    /// Restart only works once the game is over; difficulty changes apply between points and keep the score,
    /// so a thumb resting on A/X/Y can't wipe a match.
    /// In the editor: R restart, P passthrough, 1/2/3 difficulty.
    /// </summary>
    public class GameModeController : MonoBehaviour
    {
        [SerializeField] MatchController _match;
        [SerializeField] AIOpponent _ai;
        [SerializeField] PassthroughController _passthrough;
        [SerializeField] bool _startSoloOnPlay = true;

        public GameMode Mode { get; private set; } = GameMode.Solo;
        public string OpponentName => Mode == GameMode.Solo && _ai != null ? $"AI ({_ai.LevelName})" : "Opponent";

        void Start()
        {
            if (_startSoloOnPlay) StartSolo(_ai != null ? _ai.Level : 1);
        }

        public void StartSolo(int level)
        {
            Mode = GameMode.Solo;
            if (_ai != null) { _ai.Level = level; _ai.enabled = true; } // the AI shares the Match object: toggle the component, not the GameObject
            _match.IsAuthority = true;
            _match.StartMatch(0);
        }

        public void Restart()
        {
            if (Mode == GameMode.Solo) StartSolo(_ai != null ? _ai.Level : 1);
        }

        /// <summary>Changes the AI level for the next point; the score is kept.</summary>
        public void ChangeDifficulty(int delta)
        {
            if (_ai == null) return;
            _ai.Level = Mathf.Clamp(_ai.Level + delta, 0, _ai.LevelCount - 1);
        }

        public void TogglePassthrough() { if (_passthrough != null) _passthrough.Toggle(); }

        bool GameOver => _match.State is MatchState.GameOver or MatchState.FreePlay;
        bool BetweenPoints => GameOver || _match.State == MatchState.WaitingForServe;

        void Update()
        {
            if (OVRInput.GetDown(OVRInput.Button.Two)) TogglePassthrough();
            if (GameOver && OVRInput.GetDown(OVRInput.Button.One)) Restart();
            if (BetweenPoints && OVRInput.GetDown(OVRInput.Button.Three)) ChangeDifficulty(-1);
            if (BetweenPoints && OVRInput.GetDown(OVRInput.Button.Four)) ChangeDifficulty(1);
            // Editor keys are deliberate, so they aren't gated.
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            if (k == null) return;
            if (k.rKey.wasPressedThisFrame) Restart();
            if (k.pKey.wasPressedThisFrame) TogglePassthrough();
            if (k.digit1Key.wasPressedThisFrame) StartSolo(0);
            if (k.digit2Key.wasPressedThisFrame) StartSolo(1);
            if (k.digit3Key.wasPressedThisFrame) StartSolo(2);
#else
            if (Input.GetKeyDown(KeyCode.R)) Restart();
            if (Input.GetKeyDown(KeyCode.P)) TogglePassthrough();
            if (Input.GetKeyDown(KeyCode.Alpha1)) StartSolo(0);
            if (Input.GetKeyDown(KeyCode.Alpha2)) StartSolo(1);
            if (Input.GetKeyDown(KeyCode.Alpha3)) StartSolo(2);
#endif
        }
    }
}

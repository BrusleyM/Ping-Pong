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
    /// Restart and difficulty only work between points, so a thumb on A/X/Y mid-rally does nothing.
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

        public void ChangeDifficulty(int delta)
        {
            if (_ai == null) return;
            StartSolo(Mathf.Clamp(_ai.Level + delta, 0, _ai.LevelCount - 1));
        }

        public void TogglePassthrough() { if (_passthrough != null) _passthrough.Toggle(); }

        bool BetweenPoints => _match.State is MatchState.GameOver or MatchState.WaitingForServe or MatchState.FreePlay;

        void Update()
        {
            if (OVRInput.GetDown(OVRInput.Button.Two)) TogglePassthrough();
            if (!BetweenPoints) return;
            if (OVRInput.GetDown(OVRInput.Button.One)) Restart();
            if (OVRInput.GetDown(OVRInput.Button.Three)) ChangeDifficulty(-1);
            if (OVRInput.GetDown(OVRInput.Button.Four)) ChangeDifficulty(1);
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

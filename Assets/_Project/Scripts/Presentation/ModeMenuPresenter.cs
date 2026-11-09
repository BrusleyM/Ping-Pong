using System.Collections;
using System.Collections.Generic;
using PingPong.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PingPong.Presentation
{
    /// <summary>
    /// The mode menu: solo at each AI level, online, passthrough and restart. Opens in front of the
    /// player on the menu button (and at launch), closes once a mode is picked. Builds one difficulty
    /// button per AI level from a template, so the menu follows the AI profile. Wording comes from
    /// <see cref="MenuText"/>; how the panel is pointed at (rays, poke) is the platform's business.
    /// </summary>
    public sealed class ModeMenuPresenter : MonoBehaviour
    {
        [SerializeField] GameModeController _modes;
        [Tooltip("The panel shown and hidden (the world-space canvas).")]
        [SerializeField] GameObject _panel;
        [Tooltip("The player's head (centre eye); the menu opens in front of it.")]
        [SerializeField] Transform _head;
        [Tooltip("Inactive template copied once per AI level, under its parent.")]
        [SerializeField] Button _levelTemplate;
        [SerializeField] Button _online;
        [SerializeField] Button _passthrough;
        [SerializeField] Button _restart;
        [SerializeField] Button _close;
        [Tooltip("Distance from the head to the panel, metres.")]
        [SerializeField] float _distance = 0.8f;
        [Tooltip("How far below eye level the panel's centre sits, metres.")]
        [SerializeField] float _belowEyes = 0.15f;
        [SerializeField] bool _openOnStart = true;

        readonly List<Button> _levels = new List<Button>();

        public bool IsOpen => _panel.activeSelf;

        void OnValidate()
        {
            Validation.Require(_modes, "the Game Mode Controller", this);
            Validation.Require(_panel, "the menu panel", this);
            Validation.Require(_levelTemplate, "a difficulty button template", this);
        }

        void Awake()
        {
            _levelTemplate.gameObject.SetActive(false);
            int levelCount = _modes.AI != null ? _modes.AI.LevelCount : 0;
            for (int i = 0; i < levelCount; i++)
            {
                int level = i;
                Button b = Instantiate(_levelTemplate, _levelTemplate.transform.parent);
                b.name = $"Level {_modes.AI.NameOf(level)}";
                b.gameObject.SetActive(true);
                b.onClick.AddListener(() => Pick(() => _modes.StartSolo(level)));
                _levels.Add(b);
            }
            Bind(_online, null); // no online yet: the button stays disabled
            Bind(_passthrough, _modes.TogglePassthrough);
            Bind(_restart, () => Pick(_modes.Restart));
            Bind(_close, Close);
        }

        void OnEnable()
        {
            _modes.Changed += Refresh;
            _modes.MenuRequested += Toggle;
        }

        void OnDisable()
        {
            _modes.Changed -= Refresh;
            _modes.MenuRequested -= Toggle;
        }

        IEnumerator Start()
        {
            Close();
            if (!_openOnStart) yield break;
            yield return null; // let head tracking settle so the panel opens where the player looks
            Open();
        }

        public void Toggle()
        {
            if (IsOpen) Close(); else Open();
        }

        public void Open()
        {
            PlaceInFrontOfHead();
            _panel.SetActive(true);
            Refresh();
        }

        public void Close() => _panel.SetActive(false);

        void Pick(System.Action choice)
        {
            choice();
            Close();
        }

        void PlaceInFrontOfHead()
        {
            if (_head == null) return;
            Vector3 forward = Vector3.ProjectOnPlane(_head.forward, Vector3.up);
            if (forward.sqrMagnitude < 1e-4f) forward = Vector3.ProjectOnPlane(_head.up, Vector3.up); // looking straight down
            forward.Normalize();
            Vector3 position = _head.position + forward * _distance + Vector3.down * _belowEyes;
            _panel.transform.SetPositionAndRotation(position, Quaternion.LookRotation(forward, Vector3.up));
        }

        void Refresh()
        {
            if (!IsOpen) return;
            for (int i = 0; i < _levels.Count; i++) // only built when there is an AI
                Label(_levels[i], MenuText.Level(_modes.AI.NameOf(i), _modes.Mode == GameMode.Solo && _modes.AI.Level == i));
            _online.interactable = _modes.OnlineAvailable;
            Label(_online, MenuText.OnlineButton(_modes.OnlineAvailable));
            Label(_passthrough, MenuText.Passthrough(_modes.PassthroughOn));
            Label(_restart, MenuText.Restart);
            Label(_close, MenuText.Close);
        }

        static void Bind(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null) return;
            button.onClick.RemoveAllListeners();
            if (action != null) button.onClick.AddListener(action);
        }

        static void Label(Button button, string text)
        {
            if (button == null) return;
            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null) label.text = text;
        }
    }
}

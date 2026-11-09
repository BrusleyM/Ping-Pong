using PingPong.Core;
using PingPong.Gameplay;
using TMPro;
using UnityEngine;

namespace PingPong.Presentation
{
    /// <summary>
    /// Shows the match on a floating TextMeshPro board above the net, facing the local player:
    /// the score, then a status or a short-lived point message. Wording comes from <see cref="MatchText"/>.
    /// </summary>
    public sealed class ScoreboardPresenter : MonoBehaviour
    {
        [SerializeField] MatchController _match;
        [SerializeField] GameModeController _modes;
        [Tooltip("World-space text this board writes to.")]
        [SerializeField] TMP_Text _text;
        [Tooltip("Height of the board above the table surface, metres.")]
        [SerializeField] float _height = 0.55f;
        [Tooltip("How long a point message stays before the status returns, seconds.")]
        [SerializeField] float _messageSeconds = 1.5f;
        [Tooltip("Status line size relative to the score.")]
        [SerializeField, Range(0.2f, 1f)] float _statusScale = 0.5f;

        string _message = "";
        float _messageUntil;

        Side Local => _modes != null ? _modes.LocalPlayer : Side.Near;

        string OpponentName => _modes != null
            ? MatchText.OpponentName(_modes.Mode, _modes.AI != null ? _modes.AI.LevelName : null)
            : MatchText.OpponentName(GameMode.Online, null);

        void OnValidate()
        {
            Validation.Require(_match, "the Match Controller", this);
            Validation.Require(_text, "a TextMeshPro text", this);
        }

        void OnEnable()
        {
            _match.Changed += Refresh;
            _match.PointWon += OnPointWon;
            _match.GameWon += OnGameWon;
            if (_modes != null) _modes.Changed += Refresh;
        }

        void OnDisable()
        {
            _match.Changed -= Refresh;
            _match.PointWon -= OnPointWon;
            _match.GameWon -= OnGameWon;
            if (_modes != null) _modes.Changed -= Refresh;
        }

        void Start()
        {
            TableFrame t = _match.Table.Frame;
            Vector3 viewerLooks = t.Forward * -Local.Sign(); // from the local player's end towards the net
            transform.SetPositionAndRotation(t.SurfaceCenter + Vector3.up * _height, Quaternion.LookRotation(viewerLooks, Vector3.up));
            Refresh();
        }

        void Update()
        {
            if (_message.Length > 0 && Time.time > _messageUntil) { _message = ""; Refresh(); }
        }

        void OnPointWon(Side winner, PointReason reason) => Flash(MatchText.Point(winner, reason, Local, OpponentName), _messageSeconds);

        void OnGameWon(Side winner) => Flash(MatchText.Game(winner, Local, OpponentName), float.PositiveInfinity);

        void Flash(string message, float seconds)
        {
            _message = message;
            _messageUntil = Time.time + seconds;
            Refresh();
        }

        void Refresh()
        {
            string status = MatchText.Status(_match.Phase, _match.Server, Local, OpponentName);
            string line2 = MatchText.Detail(_match.Phase, _message, status);
            int percent = Mathf.RoundToInt(_statusScale * 100f);
            _text.text = $"{MatchText.Score(_match.Score(Local), _match.Score(Local.Opponent()))}\n<size={percent}%>{line2}</size>";
        }
    }
}

using System.Collections;
using PingPong.Core;
using PingPong.Gameplay;
using TMPro;
using UnityEngine;

namespace PingPong.Presentation
{
    /// <summary>
    /// The text of a venue-style scoreboard that shows both scores: the local player on the left, the opponent
    /// on the right, each with its name and a SERVE marker while it serves. The board is a static object placed
    /// in the scene by the setup tool (this does no positioning). Wording comes from <see cref="MatchText"/>.
    /// </summary>
    public sealed class MatchScoreboard : MonoBehaviour
    {
        [SerializeField] MatchController _match;
        [SerializeField] GameModeController _modes;
        [SerializeField] TMP_Text _localName;
        [SerializeField] TMP_Text _localScore;
        [SerializeField] TMP_Text _localServe;
        [SerializeField] TMP_Text _opponentName;
        [SerializeField] TMP_Text _opponentScore;
        [SerializeField] TMP_Text _opponentServe;

        [Tooltip("How far a score swells when it goes up (1 = no punch).")]
        [SerializeField, Min(1f)] float _punchScale = 1.45f;
        [SerializeField, Min(0.05f)] float _punchSeconds = 0.35f;

        int _shownLocal = -1, _shownOpponent = -1;
        Coroutine _localPunch, _opponentPunch;

        Side Local => _modes != null ? _modes.LocalPlayer : Side.Near;

        void OnValidate()
        {
            Validation.Require(_match, "the Match Controller", this);
            Validation.Require(_localScore, "the local score text", this);
            Validation.Require(_opponentScore, "the opponent score text", this);
        }

        void OnEnable()
        {
            _match.Changed += Refresh;
            if (_modes != null) _modes.Changed += Refresh;
        }

        void OnDisable()
        {
            _match.Changed -= Refresh;
            if (_modes != null) _modes.Changed -= Refresh;
        }

        void Start() => Refresh();

        void Refresh()
        {
            GameMode mode = _modes != null ? _modes.Mode : GameMode.Online;
            Side local = Local, other = local.Opponent();
            int l = _match.Score(local), o = _match.Score(other);
            if (_localName != null) _localName.text = MatchText.BoardName(local, local, mode);
            if (_opponentName != null) _opponentName.text = MatchText.BoardName(other, local, mode);
            ShowScore(_localScore, l, ref _shownLocal, ref _localPunch);
            ShowScore(_opponentScore, o, ref _shownOpponent, ref _opponentPunch);
            ShowServe(_localServe, local);
            ShowServe(_opponentServe, other);
        }

        // Text is rewritten only when the score changes; a rise punches the number.
        void ShowScore(TMP_Text text, int score, ref int shown, ref Coroutine punch)
        {
            if (score == shown) return;
            bool rose = shown >= 0 && score > shown;
            shown = score;
            text.text = score.ToString();
            if (!rose) { text.transform.localScale = Vector3.one; return; }
            if (punch != null) StopCoroutine(punch);
            punch = StartCoroutine(Punch(text.transform));
        }

        IEnumerator Punch(Transform t)
        {
            yield return UiTween.Run(_punchSeconds, p => t.localScale = Vector3.one * (1f + (_punchScale - 1f) * UiTween.Pulse(p)));
            t.localScale = Vector3.one;
        }

        void ShowServe(TMP_Text serve, Side side)
        {
            if (serve == null) return;
            bool on = _match.Phase != MatchPhase.FreePlay && _match.Phase != MatchPhase.GameOver && _match.Server == side;
            if (serve.gameObject.activeSelf != on) serve.gameObject.SetActive(on);
        }
    }
}

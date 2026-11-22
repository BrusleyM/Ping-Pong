using System.Collections;
using PingPong.Core;
using PingPong.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PingPong.Presentation
{
    public sealed class MatchScoreboard : MonoBehaviour
    {
        [SerializeField] MatchController _match;
        [SerializeField] GameModeController _modes;
        [SerializeField] UiTheme _theme;
        [SerializeField] Image _localFill;
        [SerializeField] Image _localBorder;
        [SerializeField] TMP_Text _localName;
        [SerializeField] TMP_Text _localScore;
        [SerializeField] GameObject _localServe;
        [SerializeField] Image _opponentFill;
        [SerializeField] Image _opponentBorder;
        [SerializeField] TMP_Text _opponentName;
        [SerializeField] TMP_Text _opponentLevel;
        [SerializeField] TMP_Text _opponentScore;
        [SerializeField] GameObject _opponentServe;

        [Tooltip("How far a score swells when it goes up (1 = no punch).")]
        [SerializeField, Min(1f)] float _punchScale = 1.45f;
        [SerializeField, Min(0.05f)] float _punchSeconds = 0.35f;

        int _shownLocal = -1, _shownOpponent = -1;
        string _shownLocalName, _shownOpponentName, _shownOpponentLevel;
        Coroutine _localPunch, _opponentPunch;

        Side Local => _modes != null ? _modes.LocalPlayer : Side.Near;

        void OnValidate()
        {
            Validation.Require(_match, "the Match Controller", this);
            Validation.Require(_theme, "the UI theme", this);
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

        void Start()
        {
            ApplyTeamColours();
            Refresh();
        }

        void ApplyTeamColours()
        {
            if (_theme == null) return;
            Color local = _theme.ColourOf(AppServices.LocalTeam);
            Color opponent = _theme.ColourOf(TeamColours.Opposite(AppServices.LocalTeam));
            Paint(_localFill, _localBorder, local);
            Paint(_opponentFill, _opponentBorder, opponent);
        }

        static void Paint(Image fill, Image border, Color colour)
        {
            if (fill != null) fill.color = colour;
            if (border != null) border.color = PlateLooks.BorderOf(colour);
        }

        void Refresh()
        {
            GameMode mode = _modes != null ? _modes.Mode : GameMode.Online;
            Side local = Local, other = local.Opponent();
            int l = _match.Score(local), o = _match.Score(other);
            SetName(_localName, MatchText.BoardName(local, local, mode), ref _shownLocalName);
            SetName(_opponentName, MatchText.BoardName(other, local, mode), ref _shownOpponentName);
            SetName(_opponentLevel, mode == GameMode.Solo ? HudNames.OpponentLevel(_theme, _modes) : string.Empty, ref _shownOpponentLevel);
            ShowScore(_localScore, l, ref _shownLocal, ref _localPunch);
            ShowScore(_opponentScore, o, ref _shownOpponent, ref _opponentPunch);
            ShowServe(_localServe, local);
            ShowServe(_opponentServe, other);
        }

        // Names are constants that change only with the mode or the local side.
        static void SetName(TMP_Text text, string name, ref string shown)
        {
            if (text == null || ReferenceEquals(name, shown) || name == shown) return;
            shown = name;
            text.text = name;
        }

        // Text is rewritten only when the score changes; a rise punches the number.
        void ShowScore(TMP_Text text, int score, ref int shown, ref Coroutine punch)
        {
            if (score == shown) return;
            bool rose = shown >= 0 && score > shown;
            shown = score;
            text.text = MatchText.Number(score);
            if (!rose) { text.transform.localScale = Vector3.one; return; }
            if (punch != null) StopCoroutine(punch);
            punch = StartCoroutine(Punch(text.transform));
        }

        IEnumerator Punch(Transform t)
        {
            yield return UiTween.Run(_punchSeconds, p => t.localScale = Vector3.one * (1f + (_punchScale - 1f) * UiTween.Pulse(p)));
            t.localScale = Vector3.one;
        }

        void ShowServe(GameObject serve, Side side)
        {
            if (serve == null) return;
            bool on = _match.Phase != MatchPhase.FreePlay && _match.Phase != MatchPhase.GameOver && _match.Server == side;
            if (serve.activeSelf != on) serve.SetActive(on);
        }
    }
}

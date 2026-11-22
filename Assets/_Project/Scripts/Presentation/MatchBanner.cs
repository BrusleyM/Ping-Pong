using System.Collections;
using PingPong.Core;
using PingPong.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PingPong.Presentation
{
    public sealed class MatchBanner : MonoBehaviour
    {
        [SerializeField] MatchController _match;
        [SerializeField] GameModeController _modes;
        [SerializeField] UiTheme _theme;
        [SerializeField] Transform _visual;
        [SerializeField] Image _fill;
        [SerializeField] Image _border;
        [SerializeField] Image _icon;
        [SerializeField] TMP_Text _title;
        [SerializeField] TMP_Text _reason;
        [SerializeField] GameObject _confetti;
        [Tooltip("One icon per banner kind, in BannerKind order: point won, point lost, let, game won, game lost.")]
        [SerializeField] Sprite[] _icons;
        [SerializeField, Min(1f)] float _titleSize = 96f;
        [SerializeField, Min(1f)] float _reasonSize = 48f;
        [SerializeField, Min(100f)] float _maxTextWidth = 1100f;
        [SerializeField, Range(0.1f, 1f)] float _popFrom = 0.6f;
        [SerializeField, Min(0.05f)] float _popIn = 0.28f;
        [SerializeField, Min(0.05f)] float _popOut = 0.2f;
        [SerializeField, Min(0.1f)] float _hold = 1.4f;

        Coroutine _running;

        Side Local => _modes != null ? _modes.LocalPlayer : Side.Near;

        GameMode Mode => _modes != null ? _modes.Mode : GameMode.Online;

        string OpponentName => MatchText.OpponentName(Mode, HudNames.OpponentLevel(_theme, _modes));

        void OnValidate()
        {
            Validation.Require(_match, "the Match Controller", this);
            Validation.Require(_theme, "the UI theme", this);
            Validation.Require(_visual, "the visual root", this);
            Validation.Require(_fill, "the fill", this);
            Validation.Require(_border, "the border", this);
            Validation.Require(_icon, "the icon", this);
            Validation.Require(_title, "the title", this);
            Validation.Require(_reason, "the reason", this);
            Validation.Require(_confetti, "the confetti", this);
        }

        void OnEnable()
        {
            _match.PointWon += OnPointWon;
            _match.GameWon += OnGameWon;
            _match.Let += OnLet;
            if (_modes != null) _modes.MatchStarted += Hide;
            _visual.gameObject.SetActive(false);
        }

        void OnDisable()
        {
            _match.PointWon -= OnPointWon;
            _match.GameWon -= OnGameWon;
            _match.Let -= OnLet;
            if (_modes != null) _modes.MatchStarted -= Hide;
        }

        void OnPointWon(Side winner, PointReason reason)
        {
            if (_match.Phase == MatchPhase.GameOver) return;
            Show(BannerKinds.ForPoint(winner, Local), MatchText.PointTitle(winner, Local, Mode), MatchText.Describe(reason));
        }

        void OnGameWon(Side winner) => Show(BannerKinds.ForGame(winner, Local), MatchText.Game(winner, Local, OpponentName), string.Empty);

        void OnLet() => Show(BannerKind.Let, MatchText.Let, string.Empty);

        void Hide()
        {
            Stop();
            _visual.gameObject.SetActive(false);
        }

        void Show(BannerKind kind, string title, string reason)
        {
            Stop();
            Dress(kind, title, reason);
            _visual.gameObject.SetActive(true);
            _running = StartCoroutine(Animate(BannerKinds.StaysOnScreen(kind)));
        }

        void Dress(BannerKind kind, string title, string reason)
        {
            BannerLook look = BannerLooks.For(_theme, kind);
            _fill.color = look.Fill;
            _border.color = look.Border;
            _title.color = look.Text;
            _reason.color = look.Text;
            Fit(_title, title, _titleSize);
            Fit(_reason, reason, _reasonSize);
            _reason.gameObject.SetActive(reason.Length > 0);
            _icon.sprite = (int)kind < _icons.Length ? _icons[(int)kind] : null;
            _confetti.SetActive(kind == BannerKind.GameWon);
        }

        void Fit(TMP_Text text, string value, float baseSize)
        {
            text.fontSize = baseSize;
            text.text = value;
            text.fontSize = TextFit.Size(baseSize, text.GetPreferredValues(value).x, _maxTextWidth);
        }

        void Stop()
        {
            if (_running == null) return;
            StopCoroutine(_running);
            _running = null;
        }

        IEnumerator Animate(bool stay)
        {
            yield return UiTween.Run(_popIn, t => _visual.localScale = Vector3.one * Mathf.LerpUnclamped(_popFrom, 1f, UiTween.EaseOutBack(t)));
            if (stay) { _running = null; yield break; }
            yield return new WaitForSecondsRealtime(_hold);
            yield return UiTween.Run(_popOut, t => _visual.localScale = Vector3.one * (1f - UiTween.EaseInCubic(t)));
            _visual.gameObject.SetActive(false);
            _running = null;
        }
    }
}

using System.Collections;
using PingPong.Core;
using PingPong.Gameplay;
using TMPro;
using UnityEngine;

namespace PingPong.Presentation
{
    /// <summary>
    /// A banner that pops in for moments that deserve a cue (a point, a let, the game result) and pops out again.
    /// The game result stays until the next match starts. Static object placed by the setup tool; inactive and
    /// free while idle. Wording comes from <see cref="MatchText"/>.
    /// </summary>
    public sealed class MatchBanner : MonoBehaviour
    {
        [SerializeField] MatchController _match;
        [SerializeField] GameModeController _modes;
        [Tooltip("Everything that pops: the banner's panel and text.")]
        [SerializeField] Transform _visual;
        [SerializeField] TMP_Text _text;
        [SerializeField, Min(0.05f)] float _popIn = 0.28f;
        [SerializeField, Min(0.05f)] float _popOut = 0.2f;
        [Tooltip("How long a point message stays, seconds.")]
        [SerializeField, Min(0.1f)] float _hold = 1.4f;

        Coroutine _running;

        Side Local => _modes != null ? _modes.LocalPlayer : Side.Near;

        string OpponentName => _modes != null
            ? MatchText.OpponentName(_modes.Mode, _modes.AI != null ? _modes.AI.LevelName : null)
            : MatchText.OpponentName(GameMode.Online, null);

        void OnValidate()
        {
            Validation.Require(_match, "the Match Controller", this);
            Validation.Require(_visual, "the visual root", this);
            Validation.Require(_text, "the text", this);
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

        // The game's last point also fires PointWon; the game result replaces it, so skip that one.
        void OnPointWon(Side winner, PointReason reason)
        {
            if (_match.Phase == MatchPhase.GameOver) return;
            Show(MatchText.Point(winner, reason, Local, OpponentName), stay: false);
        }

        void OnGameWon(Side winner) => Show(MatchText.Game(winner, Local, OpponentName), stay: true);

        void OnLet() => Show(MatchText.Let, stay: false);

        void Hide()
        {
            Stop();
            _visual.gameObject.SetActive(false);
        }

        void Show(string message, bool stay)
        {
            Stop();
            _text.text = message;
            _visual.gameObject.SetActive(true);
            _running = StartCoroutine(Animate(stay));
        }

        void Stop()
        {
            if (_running == null) return;
            StopCoroutine(_running);
            _running = null;
        }

        IEnumerator Animate(bool stay)
        {
            yield return UiTween.Run(_popIn, t => _visual.localScale = Vector3.one * UiTween.EaseOutBack(t));
            if (stay) { _running = null; yield break; }
            yield return new WaitForSecondsRealtime(_hold);
            yield return UiTween.Run(_popOut, t => _visual.localScale = Vector3.one * (1f - UiTween.EaseInCubic(t)));
            _visual.gameObject.SetActive(false);
            _running = null;
        }
    }
}

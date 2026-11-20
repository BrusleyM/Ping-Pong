using System.Collections;
using UnityEngine;

namespace PingPong.Presentation
{
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class PanelPop : MonoBehaviour
    {
        [SerializeField] private UiTheme _theme;

        private CanvasGroup _group;
        private Coroutine _running;

        private CanvasGroup Group => _group != null ? _group : (_group = GetComponent<CanvasGroup>());

        public bool IsShown { get; private set; }

        public void ShowNow()
        {
            Stop();
            IsShown = true;
            gameObject.SetActive(true);
            Group.alpha = 1f;
            Group.interactable = true;
            Group.blocksRaycasts = true;
            transform.localScale = Vector3.one;
        }

        public void HideNow()
        {
            Stop();
            IsShown = false;
            gameObject.SetActive(false);
        }

        public void Show()
        {
            if (IsShown)
            {
                return;
            }

            IsShown = true;
            gameObject.SetActive(true);
            Group.interactable = true;
            Group.blocksRaycasts = true;
            Begin(In());
        }

        public void Hide()
        {
            if (!IsShown)
            {
                return;
            }

            IsShown = false;
            Group.interactable = false;
            Group.blocksRaycasts = false;
            Begin(Out());
        }

        private void Begin(IEnumerator routine)
        {
            Stop();
            _running = StartCoroutine(routine);
        }

        private void Stop()
        {
            if (_running != null)
            {
                StopCoroutine(_running);
                _running = null;
            }
        }

        private IEnumerator In()
        {
            var from = _theme.PanelInFrom;
            yield return UiTween.Run(_theme.PanelInSeconds, t =>
            {
                transform.localScale = Vector3.one * Mathf.LerpUnclamped(from, 1f, UiTween.EaseOutBack(t));
                Group.alpha = Mathf.Clamp01(t * 3f);
            });
            _running = null;
        }

        private IEnumerator Out()
        {
            var to = _theme.PanelOutTo;
            yield return UiTween.Run(_theme.PanelOutSeconds, t =>
            {
                transform.localScale = Vector3.one * Mathf.Lerp(1f, to, t);
                Group.alpha = 1f - t;
            });
            gameObject.SetActive(false);
            _running = null;
        }
    }
}

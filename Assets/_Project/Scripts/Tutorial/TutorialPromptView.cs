using PingPong.Presentation;
using PingPong.Gameplay;
using TMPro;
using UnityEngine;

namespace PingPong.Tutorial
{
    public sealed class TutorialPromptView : MonoBehaviour, ITutorialCues
    {
        [SerializeField] private Transform _visual;
        [SerializeField] private TMP_Text _text;
        [SerializeField, Min(0.05f)] private float _popInSeconds = 0.25f;

        private Coroutine _popIn;

        private void OnValidate()
        {
            Validation.Require(_visual, "the visual root", this);
            Validation.Require(_text, "the text", this);
        }

        private void OnEnable()
        {
            _visual.gameObject.SetActive(false);
        }

        public void ShowPrompt(string text)
        {
            StopPopIn();
            _text.text = text;
            _visual.gameObject.SetActive(true);
            _popIn = StartCoroutine(UiTween.Run(_popInSeconds, progress => _visual.localScale = Vector3.one * UiTween.EaseOutBack(progress)));
        }

        public void HidePrompt()
        {
            StopPopIn();
            _visual.gameObject.SetActive(false);
        }

        private void StopPopIn()
        {
            if (_popIn == null)
            {
                return;
            }

            StopCoroutine(_popIn);
            _popIn = null;
        }
    }
}

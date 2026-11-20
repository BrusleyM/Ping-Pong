using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PingPong.Presentation
{
    [RequireComponent(typeof(Button))]
    public sealed class PopButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private UiTheme _theme;
        [SerializeField] private RectTransform _visual;
        [SerializeField] private GameObject _lockedTag;

        private Button _button;
        private Coroutine _scaling;
        private Coroutine _shaking;
        private bool _hovered;
        private bool _pressed;
        private bool _locked;
        private Vector2 _restPosition;
        private Image _image;
        private Color _restColour;

        public event Action Activated;

        public event Action Refused;

        public bool Locked
        {
            get => _locked;
            set
            {
                _locked = value;
                CacheImage();
                if (_image != null)
                {
                    _image.color = value ? Color.Lerp(_restColour, new Color(0.45f, 0.42f, 0.55f, 1f), 0.6f) : _restColour;
                }

                if (_lockedTag != null)
                {
                    _lockedTag.SetActive(value);
                }
            }
        }

        private RectTransform Visual => _visual != null ? _visual : (RectTransform)transform;

        private void CacheImage()
        {
            if (_image != null)
            {
                return;
            }

            _image = GetComponent<Image>();
            _restColour = _image != null ? _image.color : Color.white;
        }

        private void Awake()
        {
            _button = GetComponent<Button>();
            CacheImage();
            _restPosition = Visual.anchoredPosition;
            _button.onClick.AddListener(OnClicked);
        }

        private void OnDestroy()
        {
            if (_button != null)
            {
                _button.onClick.RemoveListener(OnClicked);
            }
        }

        private void OnEnable()
        {
            _hovered = false;
            _pressed = false;
            Visual.localScale = Vector3.one;
            Visual.anchoredPosition = _restPosition;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _hovered = true;
            AnimateToRest();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _hovered = false;
            _pressed = false;
            AnimateToRest();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _pressed = true;
            AnimateToRest();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _pressed = false;
            AnimateToRest();
        }

        private void OnClicked()
        {
            if (_locked)
            {
                Shake();
                Refused?.Invoke();
                return;
            }

            Pop();
            Activated?.Invoke();
        }

        private float RestScale => _pressed ? _theme.PressScale : _hovered ? _theme.HoverScale : 1f;

        private void AnimateToRest()
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            Restart(ref _scaling, ScaleTo(RestScale, _theme.HoverSeconds));
        }

        private void Pop()
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            Restart(ref _scaling, PopRoutine());
        }

        private void Shake()
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            Restart(ref _shaking, ShakeRoutine());
        }

        private void Restart(ref Coroutine slot, IEnumerator routine)
        {
            if (slot != null)
            {
                StopCoroutine(slot);
            }

            slot = StartCoroutine(routine);
        }

        private IEnumerator ScaleTo(float target, float seconds)
        {
            var from = Visual.localScale.x;
            yield return UiTween.Run(seconds, t => Visual.localScale = Vector3.one * Mathf.Lerp(from, target, t));
        }

        private IEnumerator PopRoutine()
        {
            var peak = _theme.SelectPeak;
            yield return UiTween.Run(_theme.SelectSeconds, t => Visual.localScale = Vector3.one * Mathf.LerpUnclamped(peak, RestScale, UiTween.EaseOutBack(t)));
        }

        private IEnumerator ShakeRoutine()
        {
            yield return UiTween.Run(_theme.ShakeSeconds, t => Visual.anchoredPosition = _restPosition + new Vector2(UiTween.Shake(t, _theme.ShakeDistance), 0f));
            Visual.anchoredPosition = _restPosition;
        }
    }
}

using System;
using System.Text;
using PingPong.Core;
using TMPro;
using UnityEngine;

namespace PingPong.Presentation
{
    public sealed class NicknameKeyboard : MonoBehaviour
    {
        [SerializeField] private PopButton _letterTemplate;
        [SerializeField] private PopButton _backspace;
        [SerializeField] private PopButton _space;
        [SerializeField] private string _letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        private readonly StringBuilder _text = new StringBuilder();
        private bool _built;

        public string Text => _text.ToString();

        public event Action<string> Changed;

        private void Awake()
        {
            Build();
        }

        public void SetText(string value)
        {
            _text.Clear();
            _text.Append(NicknameRules.Clean(value));
            Changed?.Invoke(Text);
        }

        private void Build()
        {
            if (_built)
            {
                return;
            }

            _built = true;
            _letterTemplate.gameObject.SetActive(false);
            foreach (var letter in _letters)
            {
                var key = Instantiate(_letterTemplate, _letterTemplate.transform.parent);
                key.name = "Key " + letter;
                key.GetComponentInChildren<TMP_Text>().text = letter.ToString();
                key.gameObject.SetActive(true);
                var character = letter;
                key.Activated += () => Append(character);
            }

            _space.transform.SetAsLastSibling();
            _backspace.transform.SetAsLastSibling();
            _space.Activated += () => Append(' ');
            _backspace.Activated += Backspace;
        }

        private void Append(char character)
        {
            if (_text.Length >= NicknameRules.MaxLength)
            {
                return;
            }

            if (character == ' ' && (_text.Length == 0 || _text[_text.Length - 1] == ' '))
            {
                return;
            }

            var next = _text.Length == 0 || _text[_text.Length - 1] == ' ' ? char.ToUpperInvariant(character) : char.ToLowerInvariant(character);
            _text.Append(next);
            Changed?.Invoke(Text);
        }

        private void Backspace()
        {
            if (_text.Length == 0)
            {
                return;
            }

            _text.Length--;
            Changed?.Invoke(Text);
        }
    }
}

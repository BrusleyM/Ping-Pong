using System;
using TMPro;
using UnityEngine;

namespace PingPong.Presentation
{
    public sealed class ConfirmDialog : MonoBehaviour
    {
        [SerializeField] private PanelPop _panel;
        [SerializeField] private TMP_Text _title;
        [SerializeField] private TMP_Text _body;
        [SerializeField] private TMP_Text _confirmLabel;
        [SerializeField] private PopButton _confirm;
        [SerializeField] private PopButton _cancel;

        private Action _onConfirm;

        public bool IsOpen => _panel.IsShown;

        private void Awake()
        {
            _confirm.Activated += OnConfirmed;
            _cancel.Activated += Close;
        }

        public void Open(string title, string body, string confirmLabel, Action onConfirm)
        {
            _title.text = title;
            _body.text = body;
            _confirmLabel.text = confirmLabel;
            _onConfirm = onConfirm;
            _panel.Show();
        }

        public void Close()
        {
            _onConfirm = null;
            _panel.Hide();
        }

        private void OnConfirmed()
        {
            var action = _onConfirm;
            Close();
            action?.Invoke();
        }
    }
}

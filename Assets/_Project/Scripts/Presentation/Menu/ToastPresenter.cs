using System.Collections;
using TMPro;
using UnityEngine;

namespace PingPong.Presentation
{
    public sealed class ToastPresenter : MonoBehaviour
    {
        [SerializeField] private PanelPop _panel;
        [SerializeField] private TMP_Text _label;
        [SerializeField, Min(0.5f)] private float _seconds = 2f;

        private Coroutine _hiding;

        public void Show(string message)
        {
            _label.text = message;
            _panel.Show();
            if (_hiding != null)
            {
                StopCoroutine(_hiding);
            }

            _hiding = StartCoroutine(HideLater());
        }

        private IEnumerator HideLater()
        {
            yield return new WaitForSecondsRealtime(_seconds);
            _panel.Hide();
            _hiding = null;
        }
    }
}

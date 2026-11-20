using PingPong.Gameplay;
using UnityEngine;

namespace PingPong.Presentation
{
    public sealed class CharacterPreview : MonoBehaviour
    {
        [SerializeField] private Renderer[] _renderers;
        [SerializeField] private Material _red;
        [SerializeField] private Material _blue;
        [SerializeField] private Transform _turntable;
        [SerializeField, Min(0f)] private float _degreesPerSecond = 25f;

        public void Show(TeamColour team)
        {
            var material = team == TeamColour.Red ? _red : _blue;
            foreach (var renderer in _renderers)
            {
                renderer.sharedMaterial = material;
            }
        }

        private void Update()
        {
            if (_turntable != null)
            {
                _turntable.Rotate(0f, _degreesPerSecond * Time.unscaledDeltaTime, 0f, Space.World);
            }
        }
    }
}

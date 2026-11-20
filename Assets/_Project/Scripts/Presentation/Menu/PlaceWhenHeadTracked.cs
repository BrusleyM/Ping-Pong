using System.Collections;
using Framework;
using UnityEngine;

namespace PingPong.Presentation
{
    public sealed class PlaceWhenHeadTracked : MonoBehaviour
    {
        [SerializeField] private EyeHeightPlacer _placer;
        [SerializeField] private Transform _eyes;
        [SerializeField, Min(0f)] private float _minEyeHeight = 0.6f;
        [SerializeField, Min(0f)] private float _maxWaitSeconds = 3f;

        private IEnumerator Start()
        {
            for (var waited = 0f; _eyes.position.y < _minEyeHeight && waited < _maxWaitSeconds; waited += Time.unscaledDeltaTime)
            {
                yield return null;
            }

            _placer.TryPlace();
        }
    }
}

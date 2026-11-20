using System.Collections;
using UnityEngine;

namespace PingPong.Presentation
{
    public sealed class MenuPlacer : MonoBehaviour
    {
        [SerializeField] private Transform _head;
        [SerializeField] private Transform _target;
        [SerializeField, Min(0.3f)] private float _distance = 1.2f;
        [SerializeField] private float _belowEyes = 0.05f;
        [SerializeField, Min(0f)] private float _minEyeHeight = 0.6f;
        [SerializeField, Min(0f)] private float _maxHeadWait = 3f;

        private IEnumerator Start()
        {
            yield return HeadPlacement.WaitForTracking(_head, _minEyeHeight, _maxHeadWait);
            if (_head == null)
            {
                yield break;
            }

            var pose = HeadPlacement.InFrontOf(_head, _distance, _belowEyes);
            _target.SetPositionAndRotation(pose.position, pose.rotation);
        }
    }
}

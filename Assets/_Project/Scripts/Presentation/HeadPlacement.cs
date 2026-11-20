using System.Collections;
using UnityEngine;

namespace PingPong.Presentation
{
    public static class HeadPlacement
    {
        public static Pose InFrontOf(Transform head, float distance, float belowEyes)
        {
            var forward = Vector3.ProjectOnPlane(head.forward, Vector3.up);
            if (forward.sqrMagnitude < 1e-4f)
            {
                forward = Vector3.ProjectOnPlane(head.up, Vector3.up);
            }

            forward.Normalize();
            return new Pose(head.position + forward * distance + Vector3.down * belowEyes, Quaternion.LookRotation(forward, Vector3.up));
        }

        public static IEnumerator WaitForTracking(Transform head, float minEyeHeight, float maxWaitSeconds)
        {
            for (var waited = 0f; head != null && head.position.y < minEyeHeight && waited < maxWaitSeconds; waited += Time.unscaledDeltaTime)
            {
                yield return null;
            }
        }
    }
}

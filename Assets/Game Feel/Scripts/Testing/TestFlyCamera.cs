using UnityEngine;

namespace PingPong.GameFeel.Testing
{
    /// <summary>
    /// Simple desktop camera for the test scene (no headset needed).
    /// Hold right mouse to look, WASD to move, Space/Q up/down, Shift to go faster. (E is used for stations.)
    /// F1-F6 jump to preset views (F1 = overview of the line of stations).
    /// </summary>
    public class TestFlyCamera : MonoBehaviour
    {
        public float moveSpeed = 4f;
        public float lookSpeed = 3f;
        public Transform[] viewpoints;

        float _yaw, _pitch;

        void Start()
        {
            var e = transform.eulerAngles;
            _yaw = e.y;
            _pitch = e.x > 180f ? e.x - 360f : e.x;
        }

        void Update()
        {
            if (TestInput.RightMouse())
            {
                Vector2 d = TestInput.MouseDelta();
                _yaw += d.x * lookSpeed;
                _pitch = Mathf.Clamp(_pitch - d.y * lookSpeed, -85f, 85f);
                transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            }

            Vector3 move = Vector3.zero;
            if (TestInput.Held(TestInput.K.W)) move += Vector3.forward;
            if (TestInput.Held(TestInput.K.S)) move += Vector3.back;
            if (TestInput.Held(TestInput.K.A)) move += Vector3.left;
            if (TestInput.Held(TestInput.K.D)) move += Vector3.right;
            if (TestInput.Held(TestInput.K.Space)) move += Vector3.up;
            if (TestInput.Held(TestInput.K.Q)) move += Vector3.down;
            float speed = moveSpeed * (TestInput.Held(TestInput.K.Shift) ? 3f : 1f);
            transform.Translate(move * speed * Time.deltaTime, Space.Self);

            if (viewpoints != null)
            {
                for (int i = 0; i < viewpoints.Length && i < 6; i++)
                {
                    if (viewpoints[i] != null && TestInput.Pressed(TestInput.K.F1 + i)) GoTo(viewpoints[i]);
                }
            }
        }

        public void GoTo(Transform view)
        {
            transform.SetPositionAndRotation(view.position, view.rotation);
            var e = view.eulerAngles;
            _yaw = e.y;
            _pitch = e.x > 180f ? e.x - 360f : e.x;
        }
    }
}

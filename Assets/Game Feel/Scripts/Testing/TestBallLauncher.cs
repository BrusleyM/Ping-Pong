using PingPong.Gameplay;
using UnityEngine;

namespace PingPong.GameFeel.Testing
{
    /// <summary>
    /// Test station helper: fires the station's ball (a real PingPongBall, so it uses the project's flight model and
    /// raises the same Impact events as in the game) from this transform.
    /// Straight mode shoots at the target at the next speed in the cycle; Lob mode lands the ball on the target
    /// using the ball's own launch solver. Used only in the Game Feel test scene.
    /// </summary>
    public class TestBallLauncher : MonoBehaviour
    {
        public enum FireMode { Straight, Lob }

        public PingPongBall ball;
        public FireMode mode = FireMode.Straight;
        public Transform target;
        [Tooltip("Straight: shot speeds (m/s) cycled through in order (soft, medium, smash). Lob: horizontal speeds.")]
        public float[] speeds = { 3f, 6f, 11f };
        [Tooltip("Random spread around the target (metres).")]
        public Vector3 targetJitter = Vector3.zero;
        [Tooltip("The ball goes back to the launcher after this many seconds.")]
        public float ballLifetime = 3.5f;

        int _speedIndex;
        float _firedAt = -100f;
        bool _parked;

        public string LastShot { get; private set; } = "";

        void Start() => Park();

        void Update()
        {
            if (!_parked && Time.time - _firedAt > ballLifetime) Park();
        }

        void Park()
        {
            if (ball == null) return;
            ball.Hold(transform.position);
            _parked = true;
        }

        /// <summary>Fire the next shot in the speed cycle.</summary>
        public void Fire()
        {
            float speed = speeds != null && speeds.Length > 0 ? speeds[_speedIndex % speeds.Length] : 5f;
            _speedIndex++;
            Fire(speed, target != null ? target.position : transform.position + transform.forward * 2f);
        }

        public void Fire(float speed, Vector3 aim)
        {
            if (ball == null) return;
            Vector3 start = transform.position;
            aim += new Vector3(Random.Range(-targetJitter.x, targetJitter.x), Random.Range(-targetJitter.y, targetJitter.y), Random.Range(-targetJitter.z, targetJitter.z));

            Vector3 v;
            if (mode == FireMode.Lob && ball.Table != null)
            {
                ball.Model.SolveLaunch(ball.Table.Frame, start, aim, speed, out v);
                LastShot = "lob";
            }
            else
            {
                // Aim a touch high to make up for gravity on slow shots.
                float t = (aim - start).magnitude / Mathf.Max(0.5f, speed);
                Vector3 dir = (aim + Vector3.up * (0.5f * 9.81f * t * t) - start).normalized;
                v = dir * speed;
                LastShot = speed.ToString("0") + " m/s";
            }

            ball.Hold(start); // clears any hover, then let it fly
            Rigidbody rb = ball.Body;
            rb.isKinematic = false;
            rb.linearVelocity = v;
            rb.angularVelocity = Vector3.zero;
            foreach (var tr in ball.GetComponentsInChildren<TrailRenderer>()) tr.Clear();
            _firedAt = Time.time;
            _parked = false;
        }
    }
}

using System;
using UnityEngine;
using PingPong.Gameplay;

namespace PingPong.GameObjects
{
    public enum BallContact { Net, Floor }

    [RequireComponent(typeof(Rigidbody))]
    public class PingPongBall : MonoBehaviour
    {
        #region AudioData
        [SerializeField]
        AudioSource _source;
        [SerializeField]
        AudioClip _sfx;
        [SerializeField]
        AudioClip _paddleSfx;
        #endregion

        [SerializeField] float _maxSpeed = 25f;

        public Rigidbody Body { get; private set; }
        public PingPongTable Table { get; set; }

        /// <summary>Ball bounced on the table top. Argument: table side (0 or 1).</summary>
        public event Action<int> TableBounce;
        public event Action<BallContact> OtherContact;

        Vector3 _preVelocity;

        void Awake()
        {
            Body = GetComponent<Rigidbody>();
            Body.mass = BallFlight.Mass;
            Body.linearDamping = 0f;
            Body.angularDamping = 0.05f;
            Body.useGravity = true;
            Body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            Body.interpolation = RigidbodyInterpolation.Interpolate;
        }

        void FixedUpdate()
        {
            if (Body.isKinematic) return;
            Vector3 v = Body.linearVelocity;
            Body.AddForce(BallFlight.DragAccel(v), ForceMode.Acceleration);
            if (v.sqrMagnitude > _maxSpeed * _maxSpeed) Body.linearVelocity = v.normalized * _maxSpeed;
            _preVelocity = Body.linearVelocity;
        }

        void OnCollisionEnter(Collision collision)
        {
            Play(_sfx);
            if (Table == null) return;

            if (collision.collider == Table.Top)
            {
                if (collision.GetContact(0).normal.y < 0.5f) return; // edge, not the playing surface
                Body.linearVelocity = BallFlight.BounceOffTable(_preVelocity);
                TableBounce?.Invoke(Table.SideOf(transform.position));
            }
            else if (collision.collider == Table.Net)
            {
                OtherContact?.Invoke(BallContact.Net);
            }
            else
            {
                OtherContact?.Invoke(BallContact.Floor);
            }
        }

        /// <summary>Freeze the ball in place (serve position).</summary>
        public void Hold(Vector3 position)
        {
            if (!Body.isKinematic)
            {
                Body.linearVelocity = Vector3.zero;
                Body.angularVelocity = Vector3.zero;
            }
            Body.isKinematic = true;
            Body.position = position;
            transform.position = position;
        }

        public void Launch(Vector3 position, Vector3 velocity)
        {
            Body.isKinematic = false;
            Body.position = position;
            transform.position = position;
            Body.linearVelocity = velocity;
            Body.angularVelocity = Vector3.zero;
            _preVelocity = velocity;
        }

        public void PlayPaddleHit() => Play(_paddleSfx != null ? _paddleSfx : _sfx);

        void Play(AudioClip clip)
        {
            if (_source == null || clip == null) return;
            _source.pitch = UnityEngine.Random.Range(0.95f, 1.05f);
            _source.PlayOneShot(clip);
        }
    }
}

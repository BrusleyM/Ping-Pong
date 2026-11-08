using System;
using PingPong.Core;
using UnityEngine;

namespace PingPong.Gameplay
{
    public enum BallContact { Net, DeadBall }

    /// <summary>
    /// The live ball. Applies the shared flight model (gravity, drag, table bounce) to a Rigidbody and
    /// reports what it touches; it knows nothing about rules or players.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public sealed class PingPongBall : MonoBehaviour
    {
        [SerializeField] BallPhysicsProfile _physics;
        [SerializeField] Seam<IImpactAudio> _audio;
        [Tooltip("Colliders under these roots never end a rally (the player rig, hands).")]
        [SerializeField] Transform[] _ignoredRoots = Array.Empty<Transform>();

        public Rigidbody Body { get; private set; }
        public PingPongTable Table { get; set; }
        public BallFlightModel Model => _physics.Model;
        /// <summary>Which collisions end a rally. Defaults to static geometry outside the ignored roots.</summary>
        public IBallContactFilter ContactFilter { get; set; }

        /// <summary>The ball bounced on the playing surface.</summary>
        public event Action<Side> TableBounce;
        /// <summary>The ball touched the net, or something that ends the rally.</summary>
        public event Action<BallContact> OtherContact;

        const float MinSurfaceNormalY = 0.5f; // steeper contacts are the table edge, not the surface

        Vector3 _preVelocity;

        void Awake()
        {
            Body = GetComponent<Rigidbody>();
            Body.mass = _physics.Settings.Mass;
            Body.linearDamping = 0f;
            Body.useGravity = false; // gravity comes from the flight model, so predictions match
            Body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            Body.interpolation = RigidbodyInterpolation.Interpolate;
            ContactFilter ??= new StaticGeometryContactFilter(_ignoredRoots);
        }

        void OnValidate()
        {
            Validation.Require(_physics, "a Ball Physics Profile", this);
            Validation.Seam(_audio, nameof(_audio), this);
        }

        void FixedUpdate()
        {
            if (Body.isKinematic) return;
            BallPhysicsSettings s = _physics.Settings;
            Vector3 v = Body.linearVelocity;
            Body.AddForce(s.Gravity + Model.DragAcceleration(v), ForceMode.Acceleration);
            if (v.sqrMagnitude > s.MaxSpeed * s.MaxSpeed) Body.linearVelocity = v.normalized * s.MaxSpeed;
            _preVelocity = Body.linearVelocity;
        }

        void OnCollisionEnter(Collision collision)
        {
            if (Table == null) return;
            Collider other = collision.collider;
            float speed = collision.relativeVelocity.magnitude;

            if (other == Table.Top)
            {
                if (collision.GetContact(0).normal.y < MinSurfaceNormalY) { PlayImpact(ImpactKind.Other, speed); return; }
                Body.linearVelocity = Model.BounceOffTable(_preVelocity);
                PlayImpact(ImpactKind.Table, speed);
                TableBounce?.Invoke(Table.Frame.SideOf(transform.position));
            }
            else if (other == Table.Net)
            {
                PlayImpact(ImpactKind.Net, speed);
                OtherContact?.Invoke(BallContact.Net);
            }
            else
            {
                PlayImpact(ImpactKind.Other, speed);
                if (ContactFilter.IsDeadBallSurface(other)) OtherContact?.Invoke(BallContact.DeadBall);
            }
        }

        /// <summary>Freezes the ball in place (serve position).</summary>
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

        /// <summary>Releases the ball from 'position' with 'velocity'. Players strike through MatchController.Strike.</summary>
        internal void Launch(Vector3 position, Vector3 velocity, bool byPaddle = true)
        {
            Body.isKinematic = false;
            Body.position = position;
            transform.position = position;
            Body.linearVelocity = velocity;
            Body.angularVelocity = Vector3.zero;
            _preVelocity = velocity;
            if (byPaddle) PlayImpact(ImpactKind.Paddle, velocity.magnitude);
        }

        void PlayImpact(ImpactKind kind, float speed) => _audio.Value?.Play(kind, transform.position, speed);
    }

    /// <summary>Static geometry (no rigidbody) outside the ignored roots ends a rally: floor, walls, furniture.</summary>
    public sealed class StaticGeometryContactFilter : IBallContactFilter
    {
        readonly Transform[] _ignoredRoots;

        public StaticGeometryContactFilter(Transform[] ignoredRoots) => _ignoredRoots = ignoredRoots ?? Array.Empty<Transform>();

        public bool IsDeadBallSurface(Collider collider)
        {
            if (collider.attachedRigidbody != null) return false;
            Transform t = collider.transform;
            foreach (Transform root in _ignoredRoots)
                if (root != null && t.IsChildOf(root)) return false;
            return true;
        }
    }
}

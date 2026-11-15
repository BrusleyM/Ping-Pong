using UnityEngine;
using PingPong.Core;

namespace PingPong.Gameplay
{
    /// <summary>
    /// The physics way to hit the ball (an alternative to the scripted <see cref="PaddleHitter"/>). While enabled the paddle's
    /// colliders really touch the ball, the paddle follows the hand through the physics engine so it carries its speed, and the
    /// ball's reaction comes from the solver. Each touch is passed to the rules (<see cref="MatchController.RegisterContact"/>),
    /// which judge volleys and double hits; the first contact of a swing counts. Disabled (the default), the scripted hits run.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public sealed class PhysicsPaddleContacts : MonoBehaviour
    {
        [SerializeField] MatchController _match;
        [SerializeField] PaddleHitter _hitter;
        [SerializeField] PaddleGrip _grip;
        [SerializeField] Side _player = Side.Near;
        [Tooltip("How much of the ball's speed the rubber gives back, 0-1.")]
        [SerializeField, Range(0f, 1f)] float _bounciness = 0.85f;
        [SerializeField, Range(0f, 1f)] float _friction = 0.6f;

        Rigidbody _body;
        PhysicsMaterial _rubber;
        RigidbodyInterpolation _savedInterpolation;
        CollisionDetectionMode _savedDetection;

        void OnValidate()
        {
            Validation.Require(_match, "the Match Controller", this);
            Validation.Require(_hitter, "the Paddle Hitter", this);
            Validation.Require(_grip, "the Paddle Grip", this);
        }

        void OnEnable()
        {
            _body = GetComponent<Rigidbody>();
            _savedInterpolation = _body.interpolation;
            _savedDetection = _body.collisionDetectionMode;
            _body.interpolation = RigidbodyInterpolation.Interpolate;
            _body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            _rubber ??= new PhysicsMaterial("PaddleRubber")
            {
                bounciness = _bounciness,
                dynamicFriction = _friction,
                staticFriction = _friction,
                bounceCombine = PhysicsMaterialCombine.Maximum,
            };
            Collider ball = _match.Ball.GetComponent<Collider>();
            foreach (Collider c in GetComponentsInChildren<Collider>(true))
            {
                c.sharedMaterial = _rubber;
                if (ball != null) Physics.IgnoreCollision(c, ball, false); // the scripted mode ignored them
            }
            _hitter.DetectHits = false;
            _grip.DrivenByPhysics = true;
            _match.PhysicalContacts = true;
        }

        void OnDisable()
        {
            if (_match != null) _match.PhysicalContacts = false;
            if (_grip != null) _grip.DrivenByPhysics = false;
            if (_hitter != null) _hitter.DetectHits = true;
            Collider ball = _match != null ? _match.Ball.GetComponent<Collider>() : null;
            foreach (Collider c in GetComponentsInChildren<Collider>(true))
            {
                c.sharedMaterial = null;
                if (ball != null) Physics.IgnoreCollision(c, ball, true);
            }
            if (_body != null)
            {
                _body.interpolation = _savedInterpolation;
                _body.collisionDetectionMode = _savedDetection;
            }
        }

        // A kinematic body must be moved with MovePosition for contacts to see its speed (a transform change is a teleport).
        void FixedUpdate()
        {
            if (_body.isKinematic && _grip.TryGetGripPose(out Pose pose))
            {
                _body.MovePosition(pose.position);
                _body.MoveRotation(pose.rotation);
            }
        }

        void OnCollisionEnter(Collision collision)
        {
            if (collision.rigidbody != null && collision.rigidbody == _match.Ball.Body)
                _match.RegisterContact(_player, collision.GetContact(0).point);
        }
    }
}

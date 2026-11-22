using PingPong.Core;
using UnityEngine;

namespace PingPong.Gameplay
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class PaddleRecovery : MonoBehaviour
    {
        enum Phase { Idle, Settling, Gliding }

        [SerializeField] PaddleRecoveryProfile _profile;
        [SerializeField] PingPongTable _table;
        [SerializeField] Transform _tableRoot;
        [SerializeField] PaddleModel _model;
        [SerializeField] PaddleGrip _grip;
        [SerializeField] PaddleHitter _hitter;
        [SerializeField] TrailRenderer _trail;
        [SerializeField] Side _player = Side.Near;

        Rigidbody _body;
        StaticGeometryContactFilter _surfaces;
        Phase _phase;
        float _phaseElapsed;
        RecoveryPath _path;

        public bool IsRecovering => _phase != Phase.Idle;

        void OnValidate()
        {
            Validation.Require(_profile, "the Recovery Profile", this);
            Validation.Require(_table, "the Table", this);
            Validation.Require(_tableRoot, "the Table Root", this);
            Validation.Require(_model, "the Paddle Model", this);
            Validation.Require(_grip, "the Paddle Grip", this);
            Validation.Require(_hitter, "the Paddle Hitter", this);
        }

        Rigidbody Body => _body != null ? _body : _body = GetComponent<Rigidbody>();

        void Awake()
        {
            enabled = false;
            _surfaces = new StaticGeometryContactFilter(new[] { _tableRoot });
            if (_trail != null) _trail.emitting = false;
        }

        void OnCollisionEnter(Collision collision) => NoticeContact(collision.collider);

        public void NoticeContact(Collider other)
        {
            if (_phase != Phase.Idle || _grip.IsHeldByAnything || !_surfaces.IsDeadBallSurface(other)) return;
            _phase = Phase.Settling;
            _phaseElapsed = 0f;
            enabled = true;
        }

        void FixedUpdate() => Tick(Time.fixedDeltaTime);

        public void Tick(float deltaTime)
        {
            if (_phase == Phase.Idle) return;
            if (_grip.IsHeldByAnything) { Stop(); return; }
            _phaseElapsed += deltaTime;
            if (_phase == Phase.Settling)
            {
                if (_phaseElapsed >= _profile.Feel.SettleSeconds) BeginGlide();
                return;
            }
            Follow(deltaTime);
        }

        void BeginGlide()
        {
            PaddleRecoveryFeel feel = _profile.Feel;
            TableFrame frame = _table.Frame;
            Vector3 bladeCentre = RecoverySpot.For(frame, _player, _grip.HandOnMatchStart != Hand.Left, feel, _model.BladeHalfThickness);
            Pose resting = _model.RootPoseFor(bladeCentre, Vector3.up, frame.Forward);
            _path = new RecoveryPath(Body.position, Body.rotation, resting.position, resting.rotation, feel.GlideSeconds, feel.ArcHeight);
            _phase = Phase.Gliding;
            _phaseElapsed = 0f;
            Body.useGravity = false;
            Body.detectCollisions = false;
            _hitter.Suspended = true;
            if (_trail != null)
            {
                _trail.Clear();
                _trail.emitting = true;
            }
        }

        void Follow(float deltaTime)
        {
            if (_path.IsFinished(_phaseElapsed))
            {
                Body.position = _path.End;
                Body.rotation = _path.EndRotation;
                Stop();
                return;
            }
            Body.linearVelocity = deltaTime > 0f ? (_path.PositionAt(_phaseElapsed) - Body.position) / deltaTime : Vector3.zero;
            Body.angularVelocity = Vector3.zero;
            Body.MoveRotation(_path.RotationAt(_phaseElapsed));
        }

        void Stop()
        {
            bool wasGliding = _phase == Phase.Gliding;
            _phase = Phase.Idle;
            enabled = false;
            Body.useGravity = true;
            Body.detectCollisions = true;
            _hitter.Suspended = false;
            if (wasGliding && !Body.isKinematic)
            {
                Body.linearVelocity = Vector3.zero;
                Body.angularVelocity = Vector3.zero;
            }
            if (_trail != null) _trail.emitting = false;
        }
    }
}

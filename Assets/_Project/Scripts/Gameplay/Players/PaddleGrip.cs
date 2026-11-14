using System;
using UnityEngine;

namespace PingPong.Gameplay
{
    /// <summary>
    /// Controller grab for the player's paddle (hand tracking keeps Meta's free grab, without a pose):
    /// press grab with a controller near the handle and the paddle snaps into a handshake grip and stays
    /// there without holding the button; press grab on that hand again to let go (it drops with the
    /// hand's motion). The grip is worked out from the PaddleModel, so it
    /// fits any paddle mesh; pitch and blade distance are tunable for feel.
    /// </summary>
    [DefaultExecutionOrder(-100)] // pose the paddle before PaddleHitter samples it in LateUpdate
    [RequireComponent(typeof(Rigidbody))]
    public sealed class PaddleGrip : MonoBehaviour
    {
        [SerializeField] PaddleModel _model;
        [SerializeField] Seam<IPlayerCommands> _commands;
        [Tooltip("The left and right controller poses (tracked anchors).")]
        [SerializeField] Transform _leftHand;
        [SerializeField] Transform _rightHand;
        [Tooltip("How close the hand must be to the handle to grab, metres.")]
        [SerializeField, Min(0.01f)] float _reach = 0.2f;
        [Tooltip("Distance from the hand to the blade centre along the handle, metres.")]
        [SerializeField, Min(0f)] float _bladeDistance = 0.13f;
        [Tooltip("Tilts the blade up (+) or down (-) from the controller's pointing direction, degrees.")]
        [SerializeField, Range(-90f, 90f)] float _pitch = 20f;
        [Tooltip("Optional: when a match starts, the paddle goes into this hand (None to leave it where it is).")]
        [SerializeField] GameModeController _modes;
        [SerializeField] Hand _handOnMatchStart = Hand.Right;

        Rigidbody _body;
        Vector3 _prevHandPos;
        Vector3 _handVelocity;

        public Hand HeldBy { get; private set; } = Hand.None;

        /// <summary>Raised when the paddle is taken or let go (Hand.None).</summary>
        public event Action<Hand> HeldByChanged;

        void OnValidate()
        {
            Validation.Require(_model, "the Paddle Model", this);
            Validation.Seam(_commands, nameof(_commands), this);
        }

        Rigidbody Body => _body != null ? _body : _body = GetComponent<Rigidbody>();

        void OnEnable() { if (_modes != null) _modes.MatchStarted += OnMatchStarted; }
        void OnDisable() { if (_modes != null) _modes.MatchStarted -= OnMatchStarted; }

        // Starting a match puts the paddle in the player's hand, so nobody has to find it first.
        void OnMatchStarted()
        {
            if (HeldBy != Hand.None || HandTransform(_handOnMatchStart) == null || !ControllerActive(_handOnMatchStart)) return;
            Grab(_handOnMatchStart);
        }

        void Update()
        {
            IPlayerCommands c = _commands.Value;
            if (c == null) return;
            // Hand tracking took over: the controller anchor is stale, so let go and leave the paddle to Meta's grab.
            if (HeldBy != Hand.None && !c.ControllerActive(HeldBy)) Release(withHandMotion: false);
            if (c.GrabPressed(Hand.Left)) Toggle(Hand.Left);
            else if (c.GrabPressed(Hand.Right)) Toggle(Hand.Right);
        }

        void LateUpdate()
        {
            Transform hand = HandTransform(HeldBy);
            if (hand == null) return;
            Pose grip = GripPose(hand);
            transform.SetPositionAndRotation(grip.position, grip.rotation);
            if (Time.deltaTime > 0f) _handVelocity = (hand.position - _prevHandPos) / Time.deltaTime;
            _prevHandPos = hand.position;
        }

        bool ControllerActive(Hand hand) { IPlayerCommands c = _commands.Value; return c == null || c.ControllerActive(hand); }

        /// <summary>Grabs with 'hand' if it's free and in reach; lets go if 'hand' is holding it.</summary>
        public void Toggle(Hand hand)
        {
            if (HeldBy == hand) Release();
            else if (HeldBy == Hand.None && InReach(hand)) Grab(hand);
        }

        bool InReach(Hand hand)
        {
            Transform t = HandTransform(hand);
            return t != null && Vector3.Distance(t.position, HandlePoint) <= _reach;
        }

        /// <summary>Where the hand holds the handle: back from the blade centre along the handle.</summary>
        Vector3 HandlePoint => _model.Blade.position - _model.Blade.TransformDirection(_model.HandleToBladeLocal) * _bladeDistance;

        Pose GripPose(Transform hand)
        {
            // Handshake grip: the handle runs along the controller's pointing direction (pitched), the
            // blade face looks out to the side of the hand.
            Vector3 up = hand.rotation * (Quaternion.Euler(-_pitch, 0f, 0f) * Vector3.forward); // +X pitches down
            Vector3 face = hand.right;
            return _model.RootPoseFor(hand.position + up * _bladeDistance, face, up);
        }

        void Grab(Hand hand)
        {
            HeldBy = hand;
            Body.isKinematic = true;
            _prevHandPos = HandTransform(hand).position;
            _handVelocity = Vector3.zero;
            HeldByChanged?.Invoke(hand);
        }

        // 'withHandMotion' false drops it where it is: a stale anchor's last velocity would fling it.
        void Release(bool withHandMotion = true)
        {
            HeldBy = Hand.None;
            Body.isKinematic = false;
            Body.linearVelocity = withHandMotion ? _handVelocity : Vector3.zero;
            HeldByChanged?.Invoke(Hand.None);
        }

        Transform HandTransform(Hand hand) => hand switch
        {
            Hand.Left => _leftHand,
            Hand.Right => _rightHand,
            _ => null,
        };
    }
}

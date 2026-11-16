using System;
using PingPong.Core;
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
        [Tooltip("The controller must read untracked this long before the paddle is let go, seconds.")]
        [SerializeField, Min(0f)] float _lostAfter = 0.2f;
        [Tooltip("After tracking loss, the controller must read tracked this long before the paddle comes back to it, seconds.")]
        [SerializeField, Min(0f)] float _regainAfter = 0.2f;

        Rigidbody _body;
        Vector3 _prevHandPos;
        Vector3 _handVelocity;
        TrackingDebouncer _tracking;
        bool _heldByHandGrab;
        Hand _returnTo = Hand.None; // set after a tracking-loss release: the hand the paddle goes back to
        float _nextReturnCheck;
        const float ReturnCheckInterval = 0.1f;

        public Hand HeldBy { get; private set; } = Hand.None;

        /// <summary>True with physical paddle contacts: the paddle's Rigidbody follows the hand through MovePosition, so the grip does not set the transform.</summary>
        public bool DrivenByPhysics { get; set; }

        /// <summary>Where the held paddle belongs on its hand now; false when it is not held.</summary>
        public bool TryGetGripPose(out Pose pose)
        {
            Transform hand = HandTransform(HeldBy);
            if (_heldByHandGrab || hand == null) { pose = default; return false; }
            pose = GripPose(hand);
            return true;
        }

        /// <summary>Raised when the paddle is taken or let go (Hand.None).</summary>
        public event Action<Hand> HeldByChanged;

        void OnValidate()
        {
            Validation.Require(_model, "the Paddle Model", this);
            Validation.Seam(_commands, nameof(_commands), this);
        }

        Rigidbody Body => _body != null ? _body : _body = GetComponent<Rigidbody>();

        // The component is enabled only while the paddle is held (it follows the hand every frame then), so it
        // listens from Awake to OnDestroy instead of OnEnable/OnDisable, and does no per-frame work otherwise.
        void Awake()
        {
            enabled = false;
            if (_commands.Value != null) _commands.Value.GrabPerformed += Toggle;
            if (_modes != null) _modes.MatchStarted += OnMatchStarted;
        }

        void OnDestroy()
        {
            if (_commands.Value != null) _commands.Value.GrabPerformed -= Toggle;
            if (_modes != null) _modes.MatchStarted -= OnMatchStarted;
        }

        // Starting a match puts the paddle in the player's hand, so nobody has to find it first.
        void OnMatchStarted()
        {
            if (HeldBy != Hand.None || HandTransform(_handOnMatchStart) == null || !ControllerActive(_handOnMatchStart)) return;
            Grab(_handOnMatchStart);
        }

        void LateUpdate() => FollowHand(Time.unscaledTime); // unscaled: a pause must not freeze the debounce

        /// <summary>Poses the held paddle on its hand now (every frame while held), lets go when the controller has lost tracking, and takes the paddle back when it returns.</summary>
        public void FollowHand() => FollowHand(Time.unscaledTime);

        public void FollowHand(float now)
        {
            if (_heldByHandGrab) return;
            if (HeldBy == Hand.None)
            {
                WaitForTheControllerToReturn(now);
                return;
            }
            // Hand tracking took over (the anchor is stale): after the debounce, let go and wait for the controller to come back.
            if (!_tracking.Update(now, ControllerActive(HeldBy)))
            {
                Hand lost = HeldBy;
                Release(withHandMotion: false);
                _returnTo = lost;
                _tracking = new TrackingDebouncer(false, _lostAfter, _regainAfter);
                _nextReturnCheck = now;
                enabled = true; // keep watching for the return
                return;
            }
            Transform hand = HandTransform(HeldBy);
            if (!DrivenByPhysics)
            {
                Pose grip = GripPose(hand);
                transform.SetPositionAndRotation(grip.position, grip.rotation);
            }
            if (Time.deltaTime > 0f) _handVelocity = (hand.position - _prevHandPos) / Time.deltaTime;
            _prevHandPos = hand.position;
        }

        void WaitForTheControllerToReturn(float now)
        {
            if (_returnTo == Hand.None) { enabled = false; return; }
            // Something else picked the paddle up meanwhile (Meta's hand grab holds it kinematic): don't fight it.
            if (Body.isKinematic) { _returnTo = Hand.None; enabled = false; return; }
            if (now < _nextReturnCheck) return; // ten checks a second are plenty
            _nextReturnCheck = now + ReturnCheckInterval;
            if (_tracking.Update(now, ControllerActive(_returnTo))) Grab(_returnTo); // snaps the paddle back into the hand
        }

        bool ControllerActive(Hand hand) { IPlayerCommands c = _commands.Value; return c == null || c.ControllerActive(hand); }

        /// <summary>Grabs with 'hand' if it's free and in reach; lets go if 'hand' is holding it.</summary>
        public void Toggle(Hand hand)
        {
            if (_heldByHandGrab) return;
            if (HeldBy == hand) Release();
            else if (HeldBy == Hand.None && InReach(hand)) Grab(hand);
        }

        public void HandGrabbed(Hand hand)
        {
            if (HeldBy != Hand.None) return;
            _heldByHandGrab = true;
            _returnTo = Hand.None;
            HeldBy = hand;
            enabled = false;
            HeldByChanged?.Invoke(hand);
        }

        public void HandReleased()
        {
            if (!_heldByHandGrab) return;
            _heldByHandGrab = false;
            HeldBy = Hand.None;
            HeldByChanged?.Invoke(Hand.None);
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
            _returnTo = Hand.None;
            _tracking = new TrackingDebouncer(true, _lostAfter, _regainAfter);
            enabled = true; // LateUpdate follows the hand while held
            Body.isKinematic = true;
            _prevHandPos = HandTransform(hand).position;
            if (DrivenByPhysics && TryGetGripPose(out Pose onHand)) // posed on the grab frame, not a physics step later
            {
                transform.SetPositionAndRotation(onHand.position, onHand.rotation);
                Body.position = onHand.position;
                Body.rotation = onHand.rotation;
            }
            _handVelocity = Vector3.zero;
            HeldByChanged?.Invoke(hand);
        }

        // 'withHandMotion' false drops it where it is: a stale anchor's last velocity would fling it.
        void Release(bool withHandMotion = true)
        {
            HeldBy = Hand.None;
            enabled = false;
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

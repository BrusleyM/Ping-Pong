using System;
using PingPong.Core;
using PingPong.Gameplay;
using UnityEngine;

namespace PingPong.Platform.Meta
{
    /// <summary>
    /// The hand-tracking menu gesture: look at your left wrist like a watch. Reads the left hand's wrist and palm
    /// from Meta's skeleton and feeds <see cref="WristGlanceDetector"/>; the glance raises <see cref="Performed"/>.
    /// Checked 30 times a second, and only work when the hand is tracked (controllers in hand: nothing happens).
    /// </summary>
    public sealed class WristGlanceMenu : MonoBehaviour, IMenuGesture
    {
        const float CheckInterval = 1f / 30f;

        [SerializeField] WristGlanceProfile _profile;
        [SerializeField] OVRHand _hand;
        [SerializeField] OVRSkeleton _skeleton;
        [Tooltip("The player's head (centre eye).")]
        [SerializeField] Transform _head;
        [Tooltip("Needs a longer hold while a rally is on.")]
        [SerializeField] MatchController _match;

        public event Action Performed;

        WristGlanceDetector _detector;
        Transform _wrist, _index, _pinky;
        float _nextCheck;

        void OnValidate()
        {
            Validation.Require(_profile, "a Wrist Glance Profile", this);
            Validation.Require(_hand, "the left OVRHand", this);
            Validation.Require(_skeleton, "the left OVRSkeleton", this);
            Validation.Require(_head, "the head (centre eye)", this);
        }

        void Awake() => _detector = new WristGlanceDetector(_profile.Settings);

        void Update()
        {
            if (Time.unscaledTime < _nextCheck) return;
            _nextCheck = Time.unscaledTime + CheckInterval;
            if (!TryReadHand(out WristGlanceInput input))
            {
                input.Tracked = false;
            }
            if (_detector.Update(Time.unscaledTime, input)) Performed?.Invoke();
        }

        bool TryReadHand(out WristGlanceInput input)
        {
            input = default;
            if (_hand == null || !_hand.IsTracked || !_hand.IsDataHighConfidence) return false;
            if (!FindBones()) return false;
            Vector3 wrist = _wrist.position;
            // Left hand: this cross product points out of the palm, the side of the inner wrist.
            Vector3 palm = Vector3.Cross(_index.position - wrist, _pinky.position - wrist).normalized;
            input = new WristGlanceInput
            {
                Tracked = true,
                HeadPosition = _head.position,
                HeadForward = _head.forward,
                WristPosition = wrist,
                PalmNormal = palm,
                InRally = _match != null && _match.Phase == MatchPhase.Rally,
            };
            return true;
        }

        bool FindBones()
        {
            if (_wrist != null) return true;
            if (!_skeleton.IsInitialized || _skeleton.Bones == null) return false;
            foreach (OVRBone bone in _skeleton.Bones)
            {
                switch (bone.Id)
                {
                    // Both skeleton layouts Meta ships: the classic hand and the OpenXR (XR Hands) one.
                    case OVRSkeleton.BoneId.Hand_WristRoot:
                    case OVRSkeleton.BoneId.XRHand_Wrist: _wrist = bone.Transform; break;
                    case OVRSkeleton.BoneId.Hand_Index1:
                    case OVRSkeleton.BoneId.XRHand_IndexProximal: _index = bone.Transform; break;
                    case OVRSkeleton.BoneId.Hand_Pinky1:
                    case OVRSkeleton.BoneId.XRHand_LittleProximal: _pinky = bone.Transform; break;
                }
            }
            if (_wrist != null && _index != null && _pinky != null) return true;
            _wrist = _index = _pinky = null;
            return false;
        }
    }
}

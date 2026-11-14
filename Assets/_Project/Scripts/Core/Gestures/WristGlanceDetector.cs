using System;
using UnityEngine;

namespace PingPong.Core
{
    /// <summary>Tuning for the watch-glance gesture. Angles in degrees, distances in metres, times in seconds.</summary>
    [Serializable]
    public struct WristGlanceSettings
    {
        [Tooltip("How far the back of the wrist may turn away from facing the eyes.")] [Range(5f, 90f)] public float MaxFacingAngle;
        [Tooltip("How far the wrist may be from the line of sight.")] [Range(5f, 90f)] public float MaxGazeAngle;
        [Tooltip("Nearest the wrist may be to the head.")] [Min(0f)] public float MinDistance;
        [Tooltip("Farthest the wrist may be from the head.")] [Min(0.1f)] public float MaxDistance;
        [Tooltip("How long the glance is held before it counts.")] [Min(0.05f)] public float Dwell;
        [Tooltip("The hold needed mid-rally, so a reach for the ball doesn't open the menu.")] [Min(0.05f)] public float RallyDwell;
        [Tooltip("Quiet time after it fires.")] [Min(0f)] public float Cooldown;
        [Tooltip("How long the pose must be broken before it counts as left (rides out a flicker of tracking).")] [Min(0f)] public float ReleaseTime;

        public static WristGlanceSettings Default => new WristGlanceSettings
        {
            MaxFacingAngle = 35f,
            MaxGazeAngle = 40f,
            MinDistance = 0.15f,
            MaxDistance = 0.6f,
            Dwell = 0.5f,
            RallyDwell = 1f,
            Cooldown = 1.5f,
            ReleaseTime = 0.3f,
        };
    }

    /// <summary>What the hand and head are doing this frame.</summary>
    public struct WristGlanceInput
    {
        public bool Tracked;
        public Vector3 HeadPosition;
        public Vector3 HeadForward;
        public Vector3 WristPosition;
        /// <summary>The direction the back of the wrist faces: where a watch face sits.</summary>
        public Vector3 BackOfWristNormal;
        public bool InRally;
    }

    /// <summary>
    /// The "look at your watch" gesture: the back of the wrist (where a watch face sits) faces the eyes, the wrist is within the line of sight at arm's
    /// length, and the pose is held. After it fires the hand must leave the pose before it can fire again, and a cooldown
    /// applies. Pure logic on a caller-supplied clock, so it is tested without a headset.
    /// </summary>
    public sealed class WristGlanceDetector
    {
        readonly WristGlanceSettings _settings;
        float _heldSince = -1f;
        float _brokenSince = -1f;
        float _quietUntil = float.NegativeInfinity;
        bool _armed = true;

        public WristGlanceDetector(WristGlanceSettings settings) => _settings = settings;

        /// <summary>
        /// The back of a LEFT hand's wrist faces this way: opposite to the palm, which is the side
        /// cross(index - wrist, pinky - wrist) points out of (palm down, fingers forward: index on +x, pinky on -x, so
        /// the cross product points down and the back of the hand faces up).
        /// </summary>
        public static Vector3 BackOfLeftWristNormal(Vector3 wrist, Vector3 indexProximal, Vector3 pinkyProximal) =>
            -Vector3.Cross(indexProximal - wrist, pinkyProximal - wrist).normalized;

        /// <summary>True on the one frame the gesture completes.</summary>
        public bool Update(float now, in WristGlanceInput input)
        {
            if (!IsGlancing(input))
            {
                // A short break (one lost frame, an angle on the edge) neither restarts the hold nor re-arms;
                // the hand must stay out of the pose for ReleaseTime.
                if (_brokenSince < 0f) _brokenSince = now;
                if (now - _brokenSince >= _settings.ReleaseTime)
                {
                    _heldSince = -1f;
                    _armed = true;
                }
                return false;
            }
            _brokenSince = -1f;
            if (_heldSince < 0f) _heldSince = now;
            float needed = input.InRally ? _settings.RallyDwell : _settings.Dwell;
            if (!_armed || now < _quietUntil || now - _heldSince < needed) return false;
            _armed = false;
            _quietUntil = now + _settings.Cooldown;
            return true;
        }

        bool IsGlancing(in WristGlanceInput i)
        {
            if (!i.Tracked) return false;
            Vector3 toHead = i.HeadPosition - i.WristPosition;
            float distance = toHead.magnitude;
            if (distance < _settings.MinDistance || distance > _settings.MaxDistance) return false;
            if (Vector3.Angle(i.BackOfWristNormal, toHead) > _settings.MaxFacingAngle) return false;
            return Vector3.Angle(i.HeadForward, -toHead) <= _settings.MaxGazeAngle;
        }
    }
}

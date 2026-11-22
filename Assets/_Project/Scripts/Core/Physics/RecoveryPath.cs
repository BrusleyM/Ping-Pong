using UnityEngine;

namespace PingPong.Core
{
    public readonly struct RecoveryPath
    {
        readonly Vector3 _from;
        readonly Vector3 _to;
        readonly Quaternion _fromRotation;
        readonly Quaternion _toRotation;
        readonly float _duration;
        readonly float _arcHeight;

        public RecoveryPath(Vector3 from, Quaternion fromRotation, Vector3 to, Quaternion toRotation, float duration, float arcHeight)
        {
            _from = from;
            _fromRotation = fromRotation;
            _to = to;
            _toRotation = toRotation;
            _duration = Mathf.Max(0f, duration);
            _arcHeight = arcHeight;
        }

        public Vector3 End => _to;

        public bool IsFinished(float elapsed) => elapsed >= _duration;

        public Vector3 PositionAt(float elapsed)
        {
            float t = Progress(elapsed);
            Vector3 position = Vector3.Lerp(_from, _to, Eased(t));
            position.y += _arcHeight * 4f * t * (1f - t);
            return position;
        }

        public Quaternion RotationAt(float elapsed) => Quaternion.Slerp(_fromRotation, _toRotation, Eased(Progress(elapsed)));

        float Progress(float elapsed) => _duration <= 0f ? 1f : Mathf.Clamp01(elapsed / _duration);

        static float Eased(float t) => t * t * (3f - 2f * t);
    }

    public static class RecoverySpot
    {
        public static Vector3 For(in TableFrame table, Side side, bool rightHanded, in PaddleRecoveryFeel feel, float bladeHalfThickness)
        {
            float across = rightHanded ? feel.AcrossFromCentre : -feel.AcrossFromCentre;
            return table.PointOnSide(side, across, feel.FromNet, bladeHalfThickness + feel.ClearanceAboveTable);
        }
    }
}

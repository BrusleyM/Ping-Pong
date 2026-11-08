using PingPong.Core;
using UnityEngine;

namespace PingPong.Gameplay
{
    /// <summary>
    /// Scene adapter for the table: builds a <see cref="TableFrame"/> from the table-top collider and
    /// exposes the colliders the ball needs to recognise.
    /// </summary>
    public sealed class PingPongTable : MonoBehaviour
    {
        [Tooltip("Collider of the playing surface. A BoxCollider gives the most accurate frame.")]
        [SerializeField] Collider _top;
        [SerializeField] Collider _net;
        [SerializeField] float _netHeight = TableFrame.StandardNetHeight;
        [Tooltip("Swap which end of the table is the Near side (the local player in solo).")]
        [SerializeField] bool _flipSides;

        TableFrame _frame;
        bool _built;

        public Collider Top => _top;
        public Collider Net => _net;

        /// <summary>World-space geometry of the playing surface.</summary>
        public TableFrame Frame
        {
            get
            {
                if (!_built) Rebuild();
                return _frame;
            }
        }

        void Awake() => Rebuild();

        void OnValidate() => _built = false;

        /// <summary>Recomputes the frame (call after moving the table at runtime).</summary>
        public void Rebuild()
        {
            if (_top == null) return;
            Vector3 center, forward;
            float length, width;
            if (_top is BoxCollider box)
            {
                Transform t = box.transform;
                Vector3 s = Vector3.Scale(box.size, Abs(t.lossyScale));
                center = t.TransformPoint(box.center) + Vector3.up * (s.y * 0.5f);
                bool zLong = s.z >= s.x;
                forward = zLong ? t.forward : t.right;
                length = zLong ? s.z : s.x;
                width = zLong ? s.x : s.z;
            }
            else
            {
                Bounds b = _top.bounds;
                center = new Vector3(b.center.x, b.max.y, b.center.z);
                bool zLong = b.size.z >= b.size.x;
                forward = zLong ? Vector3.forward : Vector3.right;
                length = zLong ? b.size.z : b.size.x;
                width = zLong ? b.size.x : b.size.z;
            }
            if (_flipSides) forward = -forward;
            _frame = new TableFrame(center, forward, length, width, _netHeight);
            _built = true;
        }

        static Vector3 Abs(Vector3 v) => new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));

#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            Rebuild();
            if (_top == null) return;
            TableFrame f = _frame;
            Vector3 a = f.ToWorld(new Vector3(-f.HalfWidth, 0, -f.HalfLength)), b = f.ToWorld(new Vector3(f.HalfWidth, 0, -f.HalfLength));
            Vector3 c = f.ToWorld(new Vector3(f.HalfWidth, 0, f.HalfLength)), d = f.ToWorld(new Vector3(-f.HalfWidth, 0, f.HalfLength));
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(a, b); Gizmos.DrawLine(b, c); Gizmos.DrawLine(c, d); Gizmos.DrawLine(d, a);
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(f.ToWorld(new Vector3(-f.HalfWidth, f.NetHeight, 0)), f.ToWorld(new Vector3(f.HalfWidth, f.NetHeight, 0)));
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(f.ToWorld(new Vector3(0, 0, -f.HalfLength)), 0.03f); // Near end
        }
#endif
    }
}

using UnityEngine;

namespace PingPong.Gameplay
{
    /// <summary>
    /// Describes the playing surface. Local frame: x = across the table, y = height above the surface,
    /// z = along the length. Side 0 is at negative z, side 1 at positive z.
    /// </summary>
    public class PingPongTable : MonoBehaviour
    {
        [Tooltip("Collider of the table top. A BoxCollider gives the most accurate frame.")]
        [SerializeField] Collider _top;
        [SerializeField] Collider _net;
        [SerializeField] float _netHeight = 0.1525f;
        [Tooltip("Swap which end of the table is side 0 (the local player in solo mode).")]
        [SerializeField] bool _flipSides;

        public Collider Top => _top;
        public Collider Net => _net;
        public float NetHeight => _netHeight;
        public float HalfLength { get; private set; }
        public float HalfWidth { get; private set; }
        public Vector3 SurfaceCenter { get; private set; }
        public Vector3 Forward { get; private set; }
        public Vector3 Right { get; private set; }

        void Awake() => Recalculate();

        public void Recalculate()
        {
            if (_top == null) return;
            Vector3 size, center, fwd, right;
            if (_top is BoxCollider box)
            {
                Transform t = box.transform;
                Vector3 s = Vector3.Scale(box.size, Abs(t.lossyScale));
                center = t.TransformPoint(box.center) + Vector3.up * (s.y * 0.5f);
                bool zLong = s.z >= s.x;
                fwd = Flatten(zLong ? t.forward : t.right);
                size = new Vector3(zLong ? s.x : s.z, s.y, zLong ? s.z : s.x);
            }
            else
            {
                Bounds b = _top.bounds;
                center = new Vector3(b.center.x, b.max.y, b.center.z);
                bool zLong = b.size.z >= b.size.x;
                fwd = zLong ? Vector3.forward : Vector3.right;
                size = new Vector3(zLong ? b.size.x : b.size.z, b.size.y, zLong ? b.size.z : b.size.x);
            }
            if (_flipSides) fwd = -fwd;
            right = Vector3.Cross(Vector3.up, fwd);
            SurfaceCenter = center;
            Forward = fwd;
            Right = right;
            HalfWidth = size.x * 0.5f;
            HalfLength = size.z * 0.5f;
        }

        public Vector3 ToLocal(Vector3 world)
        {
            Vector3 d = world - SurfaceCenter;
            return new Vector3(Vector3.Dot(d, Right), d.y, Vector3.Dot(d, Forward));
        }

        public Vector3 ToWorld(Vector3 local) =>
            SurfaceCenter + Right * local.x + Vector3.up * local.y + Forward * local.z;

        public Vector3 DirToLocal(Vector3 dir) =>
            new Vector3(Vector3.Dot(dir, Right), dir.y, Vector3.Dot(dir, Forward));

        public Vector3 DirToWorld(Vector3 local) => Right * local.x + Vector3.up * local.y + Forward * local.z;

        public int SideOf(Vector3 world) => ToLocal(world).z < 0f ? 0 : 1;

        public static float SideSign(int side) => side == 0 ? -1f : 1f;

        public bool IsOverTable(Vector3 world, float margin = 0f)
        {
            Vector3 l = ToLocal(world);
            return Mathf.Abs(l.x) <= HalfWidth + margin && Mathf.Abs(l.z) <= HalfLength + margin;
        }

        static Vector3 Abs(Vector3 v) => new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
        static Vector3 Flatten(Vector3 v) { v.y = 0f; return v.normalized; }

#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            Recalculate();
            if (_top == null) return;
            Gizmos.color = Color.cyan;
            Vector3 a = ToWorld(new Vector3(-HalfWidth, 0, -HalfLength)), b = ToWorld(new Vector3(HalfWidth, 0, -HalfLength));
            Vector3 c = ToWorld(new Vector3(HalfWidth, 0, HalfLength)), d = ToWorld(new Vector3(-HalfWidth, 0, HalfLength));
            Gizmos.DrawLine(a, b); Gizmos.DrawLine(b, c); Gizmos.DrawLine(c, d); Gizmos.DrawLine(d, a);
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(ToWorld(new Vector3(-HalfWidth, _netHeight, 0)), ToWorld(new Vector3(HalfWidth, _netHeight, 0)));
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(ToWorld(new Vector3(0, 0, -HalfLength)), 0.03f); // side 0 end
        }
#endif
    }
}

using System;
using UnityEngine;

namespace PingPong.Core
{
    /// <summary>
    /// Geometry of the playing surface in world space. Local frame: x = across the table,
    /// y = height above the surface, z = along the length. Near side is z &lt; 0, Far side z &gt; 0.
    /// </summary>
    [Serializable]
    public struct TableFrame
    {
        /// <summary>ITTF net height above the surface, metres.</summary>
        public const float StandardNetHeight = 0.1525f;

        public Vector3 SurfaceCenter;
        /// <summary>Horizontal unit vector from the Near end towards the Far end.</summary>
        public Vector3 Forward;
        public float HalfLength;
        public float HalfWidth;
        public float NetHeight;

        public TableFrame(Vector3 surfaceCenter, Vector3 forward, float length, float width, float netHeight = StandardNetHeight)
        {
            forward.y = 0f;
            SurfaceCenter = surfaceCenter;
            Forward = forward.normalized;
            HalfLength = length * 0.5f;
            HalfWidth = width * 0.5f;
            NetHeight = netHeight;
        }

        /// <summary>Standard 2.74 m x 1.525 m table with its surface at 'surfaceCenter', length along world z.</summary>
        public static TableFrame Standard(Vector3 surfaceCenter) =>
            new TableFrame(surfaceCenter, Vector3.forward, 2.74f, 1.525f);

        public Vector3 Right => Vector3.Cross(Vector3.up, Forward);

        public Vector3 ToLocal(Vector3 world)
        {
            Vector3 d = world - SurfaceCenter;
            return new Vector3(Vector3.Dot(d, Right), d.y, Vector3.Dot(d, Forward));
        }

        public Vector3 ToWorld(Vector3 local) =>
            SurfaceCenter + Right * local.x + Vector3.up * local.y + Forward * local.z;

        public Vector3 DirToLocal(Vector3 dir) => new Vector3(Vector3.Dot(dir, Right), dir.y, Vector3.Dot(dir, Forward));

        public Vector3 DirToWorld(Vector3 local) => Right * local.x + Vector3.up * local.y + Forward * local.z;

        public Side SideOf(Vector3 world) => ToLocal(world).z < 0f ? Side.Near : Side.Far;

        public bool IsOverTable(Vector3 world, float margin = 0f)
        {
            Vector3 l = ToLocal(world);
            return Mathf.Abs(l.x) <= HalfWidth + margin && Mathf.Abs(l.z) <= HalfLength + margin;
        }

        /// <summary>World point on the surface at a fraction of this side's half (0 = net, 1 = end line).</summary>
        public Vector3 PointOnSide(Side side, float across, float depth01) =>
            ToWorld(new Vector3(across, 0f, side.Sign() * depth01 * HalfLength));
    }
}

using UnityEngine;

namespace PingPong.Gameplay
{
    /// <summary>
    /// Shared ball physics model, used by the live ball and by prediction (AI, aim assist),
    /// so that predictions match what the ball actually does.
    /// </summary>
    public static class BallFlight
    {
        public const float Radius = 0.02f;
        public const float Mass = 0.0027f;
        /// <summary>Quadratic drag coefficient k in a = -k|v|v (0.5 * rho * Cd * A / m).</summary>
        public const float DragK = 0.12f;
        public const float TableRestitution = 0.88f;
        public const float TableFriction = 0.9f;
        public const float SimStep = 1f / 120f;

        public static Vector3 DragAccel(Vector3 v) => -DragK * v.magnitude * v;

        public static void Step(ref Vector3 p, ref Vector3 v, float dt)
        {
            v += (Physics.gravity + DragAccel(v)) * dt;
            p += v * dt;
        }

        public static Vector3 BounceOffTable(Vector3 v)
        {
            if (v.y >= 0f) return v;
            return new Vector3(v.x * TableFriction, -v.y * TableRestitution, v.z * TableFriction);
        }

        public struct Bounce
        {
            public Vector3 Position;
            public float Time;
            public int Side;
        }

        /// <summary>
        /// Simulates the ball forward. Records up to bounces.Length table bounces.
        /// Returns the number of bounces; hitNet is set if the ball meets the net.
        /// </summary>
        public static int Simulate(PingPongTable table, Vector3 pos, Vector3 vel, float maxTime,
            Bounce[] bounces, out bool hitNet)
        {
            hitNet = false;
            int count = 0;
            float t = 0f;
            Vector3 prevLocal = table.ToLocal(pos);
            while (t < maxTime && count < bounces.Length)
            {
                Step(ref pos, ref vel, SimStep);
                t += SimStep;
                Vector3 l = table.ToLocal(pos);
                if (Mathf.Sign(l.z) != Mathf.Sign(prevLocal.z) && Mathf.Abs(l.x) <= table.HalfWidth + 0.15f
                    && l.y < table.NetHeight + Radius && l.y > -Radius)
                {
                    hitNet = true;
                    return count;
                }
                if (l.y <= Radius && vel.y < 0f)
                {
                    if (Mathf.Abs(l.x) > table.HalfWidth || Mathf.Abs(l.z) > table.HalfLength) return count;
                    bounces[count++] = new Bounce { Position = pos, Time = t, Side = l.z < 0f ? 0 : 1 };
                    vel = BounceOffTable(vel);
                    pos = table.ToWorld(new Vector3(l.x, Radius, l.z));
                    l.y = Radius;
                }
                if (l.y < -1.5f) return count;
                prevLocal = l;
            }
            return count;
        }

        /// <summary>
        /// Launch velocity that carries the ball from 'from' to land on 'target' (on the table surface)
        /// with the given horizontal speed, corrected for drag. Returns false if it would hit the net.
        /// </summary>
        public static bool SolveLaunch(PingPongTable table, Vector3 from, Vector3 target, float horizontalSpeed,
            out Vector3 velocity)
        {
            var bounces = new Bounce[1];
            Vector3 aim = target;
            velocity = Vector3.zero;
            bool ok = false;
            for (int i = 0; i < 4; i++)
            {
                velocity = Ballistic(from, aim, horizontalSpeed);
                int n = Simulate(table, from, velocity, 3f, bounces, out bool net);
                ok = !net;
                if (n == 0) { if (net) break; continue; }
                Vector3 err = target - bounces[0].Position;
                err.y = 0f;
                if (err.sqrMagnitude < 0.0004f) break;
                aim += err;
            }
            return ok;
        }

        static Vector3 Ballistic(Vector3 from, Vector3 to, float horizontalSpeed)
        {
            Vector3 d = to - from;
            Vector3 h = new Vector3(d.x, 0f, d.z);
            float dist = h.magnitude;
            float T = Mathf.Max(dist / Mathf.Max(horizontalSpeed, 0.5f), 0.08f);
            float g = -Physics.gravity.y;
            float vy = (d.y + 0.5f * g * T * T) / T;
            return h / T + Vector3.up * vy;
        }
    }
}

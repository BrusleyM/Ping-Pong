using UnityEngine;

namespace PingPong.Core
{
    /// <summary>How a shot that would hit the net is retried with more arc.</summary>
    [System.Serializable]
    public struct NetClearRetry
    {
        [Tooltip("Total tries, including the first.")] public int Attempts;
        [Tooltip("Speed multiplier per retry, 0-1 (lower = more arc).")] public float SpeedFalloff;

        public NetClearRetry(int attempts, float speedFalloff)
        {
            Attempts = attempts;
            SpeedFalloff = speedFalloff;
        }
    }

    /// <summary>A predicted table bounce.</summary>
    public struct BallBounce
    {
        public Vector3 Position;
        /// <summary>Seconds after the start of the prediction.</summary>
        public float Time;
        public Side Side;
        /// <summary>Velocity just after the bounce, with every earlier bounce applied.</summary>
        public Vector3 Velocity;
    }

    /// <summary>
    /// The ball's flight model: gravity, quadratic drag and table bounces. The live ball applies the
    /// same model, so predictions (AI, aim assist) match what the ball really does.
    /// Allocation-free; not thread-safe (it reuses an internal buffer).
    /// </summary>
    public sealed class BallFlightModel
    {
        const int SolverIterations = 4;
        const float SolverTolerance = 0.02f; // metres
        const float NetSideMargin = 0.15f;   // the net overhangs the table edges
        const float LostDepth = -1.5f;       // below the surface: the ball has left play
        const float SolveTime = 3f;          // longest flight the solver considers, seconds

        readonly BallPhysicsSettings _s;
        readonly BallBounce[] _solveBuffer = new BallBounce[1];

        public BallFlightModel(BallPhysicsSettings settings) => _s = settings;

        public BallPhysicsSettings Settings => _s;
        public float Radius => _s.Radius;
        public float SimStep => _s.SimStep;

        public Vector3 DragAcceleration(Vector3 v) => -_s.DragK * v.magnitude * v;

        public void Step(ref Vector3 position, ref Vector3 velocity, float dt)
        {
            velocity += (_s.Gravity + DragAcceleration(velocity)) * dt;
            position += velocity * dt;
        }

        /// <summary>Velocity just after bouncing off the table, given the velocity just before.</summary>
        public Vector3 BounceOffTable(Vector3 v)
        {
            if (v.y >= 0f) return v;
            return new Vector3(v.x * _s.TableFriction, -v.y * _s.TableRestitution, v.z * _s.TableFriction);
        }

        /// <summary>
        /// Simulates the ball forward, recording up to bounces.Length table bounces.
        /// Returns the number of bounces; hitNet is set if the ball meets the net (simulation stops there).
        /// </summary>
        public int Simulate(in TableFrame table, Vector3 pos, Vector3 vel, float maxTime, BallBounce[] bounces, out bool hitNet) =>
            Simulate(table, pos, vel, maxTime, bounces, checkNet: true, out hitNet);

        int Simulate(in TableFrame table, Vector3 pos, Vector3 vel, float maxTime, BallBounce[] bounces, bool checkNet, out bool hitNet)
        {
            hitNet = false;
            int count = 0;
            float t = 0f;
            Vector3 prev = table.ToLocal(pos);
            while (t < maxTime && count < bounces.Length)
            {
                Step(ref pos, ref vel, _s.SimStep);
                t += _s.SimStep;
                Vector3 l = table.ToLocal(pos);
                bool crossedNetPlane = Mathf.Sign(l.z) != Mathf.Sign(prev.z);
                if (checkNet && crossedNetPlane && Mathf.Abs(l.x) <= table.HalfWidth + NetSideMargin
                    && l.y < table.NetHeight + _s.Radius && l.y > -_s.Radius)
                {
                    hitNet = true;
                    return count;
                }
                if (l.y <= _s.Radius && vel.y < 0f)
                {
                    if (Mathf.Abs(l.x) > table.HalfWidth || Mathf.Abs(l.z) > table.HalfLength) return count;
                    vel = BounceOffTable(vel);
                    l.y = _s.Radius;
                    pos = table.ToWorld(l);
                    // Position and Velocity describe the same state, the one the flight continues from.
                    bounces[count++] = new BallBounce { Position = pos, Time = t, Side = l.z < 0f ? Side.Near : Side.Far, Velocity = vel };
                }
                if (l.y < LostDepth) return count;
                prev = l;
            }
            return count;
        }

        /// <summary>
        /// Launch velocity that carries the ball from 'from' to land on 'target' (on the surface) with the
        /// given horizontal speed, corrected for drag. Returns false if that shot would hit the net or would
        /// not land first on the target's side of the table (velocity is then the best attempt).
        /// </summary>
        public bool SolveLaunch(in TableFrame table, Vector3 from, Vector3 target, float horizontalSpeed, out Vector3 velocity)
        {
            // Correct the drag-free guess against the real landing point. The net is ignored while
            // correcting: drag makes the first guess arrive later and lower, so it may clip the net even
            // though the corrected shot clears it.
            Vector3 aim = target;
            velocity = Vector3.zero;
            for (int i = 0; i < SolverIterations; i++)
            {
                velocity = Ballistic(from, aim, horizontalSpeed);
                int n = Simulate(table, from, velocity, SolveTime, _solveBuffer, checkNet: false, out _);
                if (n == 0) break; // never lands on the table: nothing to correct against
                Vector3 err = target - _solveBuffer[0].Position;
                err.y = 0f;
                if (err.sqrMagnitude < SolverTolerance * SolverTolerance) break;
                aim += err;
            }
            int bounces = Simulate(table, from, velocity, SolveTime, _solveBuffer, checkNet: true, out bool hitNet);
            return !hitNet && bounces > 0 && _solveBuffer[0].Side == table.SideOf(target);
        }

        /// <summary>
        /// Like <see cref="SolveLaunch"/>, but if the shot would hit the net, retries with less speed (more arc)
        /// as configured by 'retry'. Returns false if no attempt clears it (velocity is then the last attempt).
        /// </summary>
        public bool SolveLaunchClearingNet(in TableFrame table, Vector3 from, Vector3 target, float horizontalSpeed,
            in NetClearRetry retry, out Vector3 velocity)
        {
            float speed = horizontalSpeed;
            velocity = Vector3.zero;
            int attempts = Mathf.Max(1, retry.Attempts);
            for (int i = 0; i < attempts; i++)
            {
                if (SolveLaunch(table, from, target, speed, out velocity)) return true;
                speed *= retry.SpeedFalloff;
            }
            return false;
        }

        /// <summary>Drag-free launch from 'from' to 'to' at the given horizontal speed.</summary>
        Vector3 Ballistic(Vector3 from, Vector3 to, float horizontalSpeed)
        {
            Vector3 d = to - from;
            Vector3 h = new Vector3(d.x, 0f, d.z);
            float T = Mathf.Max(h.magnitude / Mathf.Max(horizontalSpeed, 0.5f), 0.08f);
            float g = -_s.Gravity.y;
            float vy = (d.y + 0.5f * g * T * T) / T;
            return h / T + Vector3.up * vy;
        }
    }
}

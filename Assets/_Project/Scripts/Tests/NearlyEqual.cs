using UnityEngine;

namespace PingPong.Tests
{
    internal sealed class NearlyEqual : System.Collections.Generic.IEqualityComparer<Vector3>
    {
        public static readonly NearlyEqual Instance = new NearlyEqual();
        public bool Equals(Vector3 a, Vector3 b) => (a - b).magnitude < 1e-3f;
        public int GetHashCode(Vector3 v) => 0;
    }
}

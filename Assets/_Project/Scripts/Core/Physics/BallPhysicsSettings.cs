using System;
using UnityEngine;

namespace PingPong.Core
{
    /// <summary>Physical constants of the ball. Defaults are a regulation 40 mm, 2.7 g ball.</summary>
    [Serializable]
    public struct BallPhysicsSettings
    {
        [Tooltip("Ball radius, metres.")] public float Radius;
        [Tooltip("Ball mass, kg.")] public float Mass;
        [Tooltip("Quadratic drag k in a = -k|v|v (0.5 * air density * Cd * area / mass).")] public float DragK;
        [Tooltip("Vertical speed kept after a table bounce, 0-1.")] public float TableRestitution;
        [Tooltip("Horizontal speed kept after a table bounce, 0-1.")] public float TableFriction;
        [Tooltip("Hard cap on ball speed, m/s.")] public float MaxSpeed;
        [Tooltip("Gravity used by the live ball and by predictions.")] public Vector3 Gravity;
        [Tooltip("Prediction time step, seconds.")] public float SimStep;

        public static BallPhysicsSettings Regulation => new BallPhysicsSettings
        {
            Radius = 0.02f,
            Mass = 0.0027f,
            DragK = 0.12f,
            TableRestitution = 0.88f,
            TableFriction = 0.9f,
            MaxSpeed = 25f,
            Gravity = new Vector3(0f, -9.81f, 0f),
            SimStep = 1f / 120f,
        };
    }
}

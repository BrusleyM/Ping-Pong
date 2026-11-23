using UnityEngine;

namespace PingPong.GameFeel
{
    /// <summary>
    /// Optional per-ball switches, read when GameFeelBootstrap sets the ball up. Without this component every
    /// effect is on. The test scene uses it so a station shows only the feature it is about.
    /// </summary>
    [DisallowMultipleComponent]
    public class GameFeelBallOptions : MonoBehaviour
    {
        [Tooltip("Streak behind the ball.")]
        public bool streak = true;
        [Tooltip("Ring that predicts where the ball will bounce.")]
        public bool landingRing = true;
        [Tooltip("Hits with this ball earn hit points (and show popups).")]
        public bool hitPoints = true;
        [Tooltip("Only paddle hits get effects (no table/net/floor sparks, squash or markers).")]
        public bool onlyPaddleEffects = false;
    }
}

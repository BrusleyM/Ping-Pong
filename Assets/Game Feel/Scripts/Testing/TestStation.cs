using UnityEngine;

namespace PingPong.GameFeel.Testing
{
    /// <summary>
    /// One station in the test scene's line of stations. Stand on its pad and press E to run its demo once.
    /// The TestStationsDirector finds all stations and shows the prompt for the one you stand at.
    /// </summary>
    public class TestStation : MonoBehaviour
    {
        public enum Demo { Hit, Streak, HitPoints, Point, Win, Crowd, Targets, Practice }

        public Demo demo;
        [Tooltip("Position in the line (1 = first). Number keys jump here.")]
        public int number = 1;
        [Tooltip("Short name shown in the prompt.")]
        public string title = "STATION";
        [Tooltip("What the player will see when they press E.")]
        [TextArea] public string description = "";
        [Tooltip("How close (m, on the floor) the camera has to be.")]
        public float radius = 1.8f;
        [Tooltip("Where the camera goes when you jump to this station.")]
        public Transform view;
        [Tooltip("Where this station's effects happen (popups, confetti).")]
        public Transform focus;

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.6f, 0.1f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}

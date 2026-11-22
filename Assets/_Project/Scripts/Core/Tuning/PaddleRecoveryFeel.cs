using System;
using UnityEngine;

namespace PingPong.Core
{
    [Serializable]
    public struct PaddleRecoveryFeel
    {
        [Tooltip("How long the paddle lies where it fell before it is carried back, seconds.")] public float SettleSeconds;
        [Tooltip("How long the trip back to the table takes, seconds.")] public float GlideSeconds;
        [Tooltip("How high the paddle arcs above a straight line on its way, metres.")] public float ArcHeight;
        [Tooltip("Sideways distance from the table's centre line towards the player's paddle hand, metres.")] public float AcrossFromCentre;
        [Tooltip("Distance from the net towards the player's end where the paddle is set down, metres.")] public float FromNet;
        [Tooltip("Gap between the blade and the table when the glide ends, metres.")] public float ClearanceAboveTable;

        public static PaddleRecoveryFeel Default => new PaddleRecoveryFeel
        {
            SettleSeconds = 0.6f,
            GlideSeconds = 0.8f,
            ArcHeight = 0.25f,
            AcrossFromCentre = 0.4f,
            FromNet = 1.0f,
            ClearanceAboveTable = 0.02f,
        };
    }
}

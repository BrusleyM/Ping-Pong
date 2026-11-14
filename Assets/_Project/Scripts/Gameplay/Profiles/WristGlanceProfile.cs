using PingPong.Core;
using UnityEngine;

namespace PingPong.Gameplay
{
    /// <summary>Tuning for the watch-glance menu gesture (angles, distances, hold times). Edit on the headset until it feels right.</summary>
    [CreateAssetMenu(menuName = "Ping Pong/Wrist Glance Profile", fileName = "WristGlance")]
    public sealed class WristGlanceProfile : ScriptableObject
    {
        [SerializeField] WristGlanceSettings _settings = WristGlanceSettings.Default;

        public WristGlanceSettings Settings => _settings;
    }
}

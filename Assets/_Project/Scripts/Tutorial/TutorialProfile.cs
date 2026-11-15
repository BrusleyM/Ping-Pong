using UnityEngine;

namespace PingPong.Tutorial
{
    [CreateAssetMenu(fileName = "TutorialProfile", menuName = "Ping Pong/Tutorial Profile")]
    public sealed class TutorialProfile : ScriptableObject
    {
        [Tooltip("How long the welcome message stays, seconds.")]
        [SerializeField, Min(0f)] float _welcomeSeconds = 2.5f;
        [Tooltip("How long the overview message stays, seconds.")]
        [SerializeField, Min(0f)] float _overviewSeconds = 3f;
        [Tooltip("How long the closing message stays, seconds.")]
        [SerializeField, Min(0f)] float _finishedSeconds = 3f;

        public TutorialTiming Timing => new TutorialTiming(_welcomeSeconds, _overviewSeconds, _finishedSeconds);
    }
}

using UnityEngine;

namespace PingPong.Gameplay
{
    public sealed class GameStarter : MonoBehaviour
    {
        [SerializeField] private GameModeController _modes;

        private void Start()
        {
            var defaultLevel = _modes.AI != null ? _modes.AI.Level : 0;
            var request = MatchRequests.Resolve(AppServices.PendingMatch, defaultLevel, AppServices.Profile.Current);
            AppServices.PendingMatch = request;
            _modes.StartSolo(request.AiLevel);
        }
    }
}

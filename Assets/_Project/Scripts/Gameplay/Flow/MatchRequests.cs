namespace PingPong.Gameplay
{
    public static class MatchRequests
    {
        public static MatchRequest Resolve(MatchRequest? pending, int defaultLevel, PlayerProfile profile)
        {
            if (pending.HasValue)
            {
                return pending.Value;
            }

            return new MatchRequest(defaultLevel, profile != null ? profile.Team : TeamColour.Red);
        }
    }
}

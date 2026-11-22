namespace PingPong.Gameplay
{
    public readonly struct MatchRequest
    {
        public MatchRequest(int aiLevel, TeamColour playerTeam)
        {
            AiLevel = aiLevel;
            PlayerTeam = playerTeam;
        }

        public int AiLevel { get; }

        public TeamColour PlayerTeam { get; }
    }
}

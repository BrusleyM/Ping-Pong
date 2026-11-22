namespace PingPong.Gameplay
{
    public enum TeamColour
    {
        Red,
        Blue
    }

    public static class TeamColours
    {
        public static TeamColour Opposite(TeamColour colour) => colour == TeamColour.Red ? TeamColour.Blue : TeamColour.Red;
    }
}

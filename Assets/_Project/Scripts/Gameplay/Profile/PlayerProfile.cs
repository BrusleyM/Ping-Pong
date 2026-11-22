using System;
using PingPong.Core;

namespace PingPong.Gameplay
{
    [Serializable]
    public sealed class PlayerProfile
    {
        public string Nickname;
        public TeamColour Team;

        public PlayerProfile(string nickname, TeamColour team)
        {
            Nickname = NicknameRules.Clean(nickname);
            Team = team;
        }

        public bool IsComplete => NicknameRules.IsValid(Nickname);
    }
}

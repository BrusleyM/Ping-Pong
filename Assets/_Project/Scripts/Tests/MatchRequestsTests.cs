using NUnit.Framework;
using PingPong.Gameplay;

namespace PingPong.Tests
{
    public class MatchRequestsTests
    {
        [Test]
        public void A_pending_request_from_the_menu_is_used_as_it_is()
        {
            var pending = new MatchRequest(3, TeamColour.Blue);

            var resolved = MatchRequests.Resolve(pending, 1, new PlayerProfile("A", TeamColour.Red));

            Assert.AreEqual(3, resolved.AiLevel);
            Assert.AreEqual(TeamColour.Blue, resolved.PlayerTeam);
        }

        [Test]
        public void Opening_the_game_scene_directly_uses_the_default_level_and_the_profile_team()
        {
            var resolved = MatchRequests.Resolve(null, 2, new PlayerProfile("A", TeamColour.Blue));

            Assert.AreEqual(2, resolved.AiLevel);
            Assert.AreEqual(TeamColour.Blue, resolved.PlayerTeam);
        }

        [Test]
        public void Without_a_profile_the_player_defaults_to_red()
        {
            Assert.AreEqual(TeamColour.Red, MatchRequests.Resolve(null, 0, null).PlayerTeam);
        }
    }
}

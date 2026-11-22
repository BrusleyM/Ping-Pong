using NUnit.Framework;
using PingPong.Gameplay;

namespace PingPong.Tests
{
    public class ProfileServiceTests
    {
        private InMemoryStore<PlayerProfile> _store;

        [SetUp]
        public void SetUp()
        {
            _store = new InMemoryStore<PlayerProfile>();
        }

        [Test]
        public void A_new_install_has_no_profile()
        {
            Assert.IsFalse(new ProfileService(_store).HasProfile);
        }

        [Test]
        public void A_saved_profile_is_there_for_the_next_launch()
        {
            new ProfileService(_store).TrySave("Vishnu", TeamColour.Blue);

            var next = new ProfileService(_store);

            Assert.IsTrue(next.HasProfile);
            Assert.AreEqual("Vishnu", next.Current.Nickname);
            Assert.AreEqual(TeamColour.Blue, next.Current.Team);
        }

        [Test]
        public void A_blank_nickname_is_refused_and_nothing_is_saved()
        {
            var service = new ProfileService(_store);

            Assert.IsFalse(service.TrySave("   ", TeamColour.Red));
            Assert.IsFalse(service.HasProfile);
            Assert.AreEqual(0, _store.Saves);
        }

        [Test]
        public void An_incomplete_stored_profile_is_treated_as_no_profile()
        {
            _store.Save(new PlayerProfile("", TeamColour.Red));

            Assert.IsFalse(new ProfileService(_store).HasProfile);
        }

        [Test]
        public void Saving_announces_the_new_profile()
        {
            PlayerProfile announced = null;
            var service = new ProfileService(_store);
            service.Changed += profile => announced = profile;

            service.TrySave("Vishnu", TeamColour.Red);

            Assert.AreEqual("Vishnu", announced.Nickname);
        }

        [Test]
        public void The_opposite_of_each_team_is_the_other_one()
        {
            Assert.AreEqual(TeamColour.Blue, TeamColours.Opposite(TeamColour.Red));
            Assert.AreEqual(TeamColour.Red, TeamColours.Opposite(TeamColour.Blue));
        }
    }
}

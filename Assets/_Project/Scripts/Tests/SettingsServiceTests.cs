using NUnit.Framework;
using PingPong.Gameplay;

namespace PingPong.Tests
{
    public class SettingsServiceTests
    {
        private InMemoryStore<GameSettings> _store;
        private SettingsService _service;

        [SetUp]
        public void SetUp()
        {
            _store = new InMemoryStore<GameSettings>();
            _service = new SettingsService(_store);
        }

        [Test]
        public void Defaults_are_used_when_nothing_was_saved()
        {
            Assert.AreEqual(Hand.Right, _service.Current.DominantHand);
            Assert.IsFalse(_service.Current.ComfortMode);
        }

        [Test]
        public void A_change_is_saved_and_announced_and_seen_by_the_next_launch()
        {
            GameSettings announced = null;
            _service.Changed += settings => announced = settings;

            _service.Update(settings => settings.DominantHand = Hand.Left);

            Assert.AreEqual(Hand.Left, announced.DominantHand);
            Assert.AreEqual(Hand.Left, new SettingsService(_store).Current.DominantHand);
        }

        [Test]
        public void Volumes_are_kept_between_zero_and_one()
        {
            _service.Update(settings =>
            {
                settings.MusicVolume = 3f;
                settings.EffectsVolume = -1f;
            });

            Assert.AreEqual(1f, _service.Current.MusicVolume);
            Assert.AreEqual(0f, _service.Current.EffectsVolume);
        }

        [Test]
        public void Having_no_dominant_hand_falls_back_to_right()
        {
            _service.Update(settings => settings.DominantHand = Hand.None);

            Assert.AreEqual(Hand.Right, _service.Current.DominantHand);
        }
    }
}

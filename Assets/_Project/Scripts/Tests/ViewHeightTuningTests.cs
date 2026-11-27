using NUnit.Framework;
using PingPong.Core;

namespace PingPong.Tests
{
    public class ViewHeightTuningTests
    {
        private readonly ViewHeightTuning _tuning = ViewHeightTuning.Default;

        [Test]
        public void A_seated_player_is_lifted_to_the_standing_eye_height()
        {
            Assert.IsTrue(_tuning.TryGetSeatedLift(1.15f, out var lift));

            Assert.AreEqual(0.45f, lift, 1e-4f);
        }

        [Test]
        public void A_player_who_is_already_that_tall_is_never_lowered()
        {
            Assert.IsTrue(_tuning.TryGetSeatedLift(1.8f, out var lift));

            Assert.AreEqual(0f, lift);
        }

        [Test]
        public void The_lift_stops_at_the_highest_allowed()
        {
            Assert.IsTrue(_tuning.TryGetSeatedLift(0.75f, out var lift));

            Assert.AreEqual(0.8f, lift, 1e-4f);
        }

        [Test]
        public void A_headset_that_is_lying_around_gives_no_lift()
        {
            Assert.IsFalse(_tuning.TryGetSeatedLift(0.1f, out _));
        }
    }
}

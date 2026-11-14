using NUnit.Framework;
using PingPong.Core;

namespace PingPong.Tests
{
    public sealed class TrackingDebouncerTests
    {
        [Test]
        public void StaysTrue_UntilTheLossLastsLongEnough()
        {
            var d = new TrackingDebouncer(true, lostAfter: 0.2f, regainAfter: 0.2f);
            Assert.That(d.Update(0f, false), Is.True);
            Assert.That(d.Update(0.19f, false), Is.True);
            Assert.That(d.Update(0.21f, false), Is.False);
        }

        [Test]
        public void AFlicker_RestartsTheLossTimer()
        {
            var d = new TrackingDebouncer(true, 0.2f, 0.2f);
            d.Update(0f, false);
            d.Update(0.1f, true);          // back for a frame
            d.Update(0.15f, false);        // lost again: the timer starts over
            Assert.That(d.Update(0.30f, false), Is.True, "only 0.15 s since the new loss");
            Assert.That(d.Update(0.36f, false), Is.False, "0.21 s since the new loss");
        }

        [Test]
        public void StaysFalse_UntilTheRegainLastsLongEnough()
        {
            var d = new TrackingDebouncer(false, 0.2f, 0.3f);
            Assert.That(d.Update(1f, true), Is.False);
            Assert.That(d.Update(1.29f, true), Is.False);
            Assert.That(d.Update(1.31f, true), Is.True);
        }

        [Test]
        public void ZeroTimes_FollowTheRawValueAtOnce()
        {
            var d = new TrackingDebouncer(true, 0f, 0f);
            Assert.That(d.Update(0f, false), Is.False);
            Assert.That(d.Update(0f, true), Is.True);
        }
    }
}

using NUnit.Framework;
using PingPong.Core;

namespace PingPong.Tests
{
    public sealed class InputModeTrackerTests
    {
        [Test]
        public void StartingWithOnlyHandsActive_IsHandsFromTheFirstFrame_WithoutWaitingToSettle()
        {
            var tracker = new InputModeTracker(anyHandActive: true, anyControllerActive: false, settleSeconds: 0.2f);

            Assert.That(tracker.ControllersLive, Is.False);
        }

        [Test]
        public void StartingWithNothingActive_IsControllers()
        {
            var tracker = new InputModeTracker(false, false, 0.2f);

            Assert.That(tracker.ControllersLive, Is.True);
        }

        [Test]
        public void StartingWithControllersActive_IsControllers()
        {
            var tracker = new InputModeTracker(false, true, 0.2f);

            Assert.That(tracker.ControllersLive, Is.True);
        }

        [Test]
        public void HandsOnlyForLongerThanTheSettleTime_FlipsToHandsExactlyOnce()
        {
            var tracker = new InputModeTracker(false, true, 0.2f);

            Assert.That(tracker.Update(0f, true, false), Is.False, "the switch has not settled yet");
            Assert.That(tracker.Update(0.25f, true, false), Is.True);
            Assert.That(tracker.ControllersLive, Is.False);
            Assert.That(tracker.Update(0.5f, true, false), Is.False, "already hands: no second flip");
        }

        [Test]
        public void AFlickerOfHandsDoesNotFlipTheMode()
        {
            var tracker = new InputModeTracker(false, true, 0.2f);

            tracker.Update(0f, true, false);
            tracker.Update(0.1f, false, true);
            tracker.Update(0.15f, true, false);
            bool flipped = tracker.Update(0.30f, true, false);

            Assert.That(flipped, Is.False, "only 0.15 s since the last controller reading");
            Assert.That(tracker.ControllersLive, Is.True);
        }

        [Test]
        public void HandsAndControllersTogether_CountAsControllers()
        {
            var tracker = new InputModeTracker(false, true, 0.2f);

            tracker.Update(0f, true, true);
            tracker.Update(1f, true, true);

            Assert.That(tracker.ControllersLive, Is.True);
        }

        [Test]
        public void ControllersReturningAfterHands_FlipBackOnceSettled()
        {
            var tracker = new InputModeTracker(true, false, 0.2f);

            Assert.That(tracker.Update(0f, false, true), Is.False);
            Assert.That(tracker.Update(0.25f, false, true), Is.True);
            Assert.That(tracker.ControllersLive, Is.True);
        }

        [Test]
        public void NothingActiveAfterHands_FlipsBackToControllers()
        {
            var tracker = new InputModeTracker(true, false, 0.2f);

            tracker.Update(0f, false, false);
            tracker.Update(0.25f, false, false);

            Assert.That(tracker.ControllersLive, Is.True);
        }
    }
}

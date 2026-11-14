using NUnit.Framework;
using PingPong.Core;
using UnityEngine;

namespace PingPong.Tests
{
    public sealed class WristGlanceDetectorTests
    {
        WristGlanceDetector _detector;
        float _now;

        [SetUp]
        public void SetUp()
        {
            _detector = new WristGlanceDetector(WristGlanceSettings.Default);
            _now = 0f;
        }

        // Head at the origin looking along +Z; the wrist half a metre ahead with the palm turned back to the eyes.
        static WristGlanceInput Glancing() => new WristGlanceInput
        {
            Tracked = true,
            HeadPosition = Vector3.zero,
            HeadForward = Vector3.forward,
            WristPosition = new Vector3(0f, 0f, 0.4f),
            PalmNormal = Vector3.back,
        };

        bool Step(WristGlanceInput input, float dt = 0.05f)
        {
            _now += dt;
            return _detector.Update(_now, input);
        }

        bool Hold(WristGlanceInput input, float seconds)
        {
            bool fired = false;
            for (float t = 0f; t < seconds; t += 0.05f) fired |= Step(input);
            return fired;
        }

        [Test]
        public void FiresOnceAfterTheDwell_NotBefore()
        {
            Assert.That(Hold(Glancing(), 0.4f), Is.False, "held too briefly");
            Assert.That(Hold(Glancing(), 0.3f), Is.True, "held for the dwell");
            Assert.That(Hold(Glancing(), 2f), Is.False, "holding on does not fire again");
        }

        [Test]
        public void NeedsTheHandToLeaveThePose_AndTheCooldown_ToFireAgain()
        {
            Hold(Glancing(), 0.7f);
            var away = Glancing(); away.PalmNormal = Vector3.forward; // palm turned away
            Step(away);
            Assert.That(Hold(Glancing(), 0.7f), Is.False, "still inside the 1.5 s cooldown");
            Hold(away, 1.5f);
            Assert.That(Hold(Glancing(), 0.7f), Is.True, "left the pose and the cooldown passed");
        }

        [Test]
        public void IgnoresAHandThatIsNotTracked()
        {
            var lost = Glancing(); lost.Tracked = false;
            Assert.That(Hold(lost, 3f), Is.False);
        }

        [Test]
        public void IgnoresAPalmTurnedAwayFromTheEyes()
        {
            var turned = Glancing(); turned.PalmNormal = new Vector3(1f, 0f, -0.3f).normalized; // far more than 35 degrees off
            Assert.That(Hold(turned, 3f), Is.False);
        }

        [Test]
        public void IgnoresAWristOutsideTheLineOfSight()
        {
            var aside = Glancing(); aside.WristPosition = new Vector3(0.45f, 0f, 0.1f); // beside the head, not ahead
            aside.PalmNormal = (aside.HeadPosition - aside.WristPosition).normalized;
            Assert.That(Hold(aside, 3f), Is.False);
        }

        [Test]
        public void IgnoresAWristTooCloseOrTooFar()
        {
            var near = Glancing(); near.WristPosition = new Vector3(0f, 0f, 0.05f);
            var far = Glancing(); far.WristPosition = new Vector3(0f, 0f, 1.2f);
            Assert.That(Hold(near, 3f), Is.False);
            Assert.That(Hold(far, 3f), Is.False);
        }

        [Test]
        public void NeedsALongerHoldDuringARally()
        {
            var rally = Glancing(); rally.InRally = true;
            Assert.That(Hold(rally, 0.8f), Is.False, "0.5 s is not enough mid-rally");
            Assert.That(Hold(rally, 0.4f), Is.True, "1 s is");
        }

        [Test]
        public void BrokenPoseRestartsTheHold()
        {
            Hold(Glancing(), 0.4f);
            var lost = Glancing(); lost.Tracked = false;
            Step(lost);
            Assert.That(Hold(Glancing(), 0.4f), Is.False, "the hold starts over");
            Assert.That(Hold(Glancing(), 0.3f), Is.True);
        }
    }
}

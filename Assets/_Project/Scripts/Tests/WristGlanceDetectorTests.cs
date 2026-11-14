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

        // Head at the origin looking along +Z; the wrist 0.4 m ahead with the back of the wrist turned back to the eyes.
        static WristGlanceInput Glancing() => new WristGlanceInput
        {
            Tracked = true,
            HeadPosition = Vector3.zero,
            HeadForward = Vector3.forward,
            WristPosition = new Vector3(0f, 0f, 0.4f),
            BackOfWristNormal = Vector3.back,
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
            var away = Glancing(); away.BackOfWristNormal = Vector3.forward; // back of the wrist turned away
            Hold(away, 0.4f);
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
        public void IgnoresABackOfWristTurnedAwayFromTheEyes()
        {
            var turned = Glancing(); turned.BackOfWristNormal = new Vector3(1f, 0f, -0.3f).normalized; // far more than 35 degrees off
            Assert.That(Hold(turned, 3f), Is.False);
        }

        [Test]
        public void IgnoresAWristOutsideTheLineOfSight()
        {
            var aside = Glancing(); aside.WristPosition = new Vector3(0.45f, 0f, 0.1f); // beside the head, not ahead
            aside.BackOfWristNormal = (aside.HeadPosition - aside.WristPosition).normalized;
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
        public void ALongBreakRestartsTheHold()
        {
            Hold(Glancing(), 0.4f);
            var lost = Glancing(); lost.Tracked = false;
            Hold(lost, 0.4f); // longer than the release time
            Assert.That(Hold(Glancing(), 0.4f), Is.False, "the hold starts over");
            Assert.That(Hold(Glancing(), 0.3f), Is.True);
        }

        [Test]
        public void AFlickerOfLostTrackingDoesNotReFireWhileTheHandStaysPut()
        {
            Assert.That(Hold(Glancing(), 0.7f), Is.True);
            var lost = Glancing(); lost.Tracked = false;
            for (int i = 0; i < 6; i++)
            {
                Step(lost);                                  // one lost frame...
                Assert.That(Hold(Glancing(), 1.0f), Is.False, "...must not re-arm: no second fire");
            }
        }

        [Test]
        public void ALeftHandWithItsPalmDown_HasItsBackFacingUp()
        {
            // Palm down, fingers forward (+z): the index finger is on the +x side, the little finger on -x.
            Vector3 wrist = Vector3.zero, index = new Vector3(0.03f, 0f, 0.08f), pinky = new Vector3(-0.03f, 0f, 0.07f);
            Vector3 back = WristGlanceDetector.BackOfLeftWristNormal(wrist, index, pinky);
            Assert.That(Vector3.Angle(back, Vector3.up), Is.LessThan(5f), "back of the hand up");
            // Turned over, palm up: the back faces down.
            Vector3 flipped = WristGlanceDetector.BackOfLeftWristNormal(wrist, new Vector3(-0.03f, 0f, 0.08f), new Vector3(0.03f, 0f, 0.07f));
            Assert.That(Vector3.Angle(flipped, Vector3.down), Is.LessThan(5f), "back of the hand down");
        }
    }
}

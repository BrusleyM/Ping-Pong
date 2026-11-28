using NUnit.Framework;

namespace PingPong.GameFeel.Tests
{
    public sealed class CrowdAmbienceCurveTests
    {
        const float Lively = 0.18f;
        const float Attentive = 0.1f;
        const float Peak = 0.324f;

        [Test]
        public void TheSwellStartsAtTheCurrentVolume() =>
            Assert.That(CrowdAmbienceCurve.Volume(0f, Attentive, Attentive, Lively), Is.EqualTo(Attentive).Within(0.0001f));

        [Test]
        public void TheCrowdRisesAboveItsLivelyVolumeWhileCheering() =>
            Assert.That(CrowdAmbienceCurve.Volume(0.5f, Attentive, Attentive, Lively), Is.GreaterThan(Lively));

        [Test]
        public void ARoarThatInterruptsASwellNeverStacksAboveThePeak()
        {
            float interrupted = CrowdAmbienceCurve.Volume(0.5f, Attentive, Attentive, Lively);
            for (float progress = 0f; progress <= 1f; progress += 0.05f)
                Assert.That(CrowdAmbienceCurve.Volume(progress, interrupted, Attentive, Lively), Is.LessThanOrEqualTo(Peak + 0.0001f));
        }

        [Test]
        public void TheSwellSettlesFromTheStartingVolumeTowardsTheRestingVolume() =>
            Assert.That(CrowdAmbienceCurve.Volume(0.9f, Lively, Attentive, Lively),
                Is.GreaterThan(CrowdAmbienceCurve.Volume(0.9f, Attentive, Attentive, Lively)));

        [Test]
        public void TheCrowdEndsAtTheRestingVolumeOnceSeated() =>
            Assert.That(CrowdAmbienceCurve.Volume(1f, Lively, Attentive, Lively), Is.EqualTo(Attentive).Within(0.0001f));

        [Test]
        public void ProgressPastTheEndStaysAtTheRestingVolume() =>
            Assert.That(CrowdAmbienceCurve.Volume(2f, Lively, Attentive, Lively), Is.EqualTo(Attentive).Within(0.0001f));
    }
}

using NUnit.Framework;
using PingPong.Presentation;

namespace PingPong.Tests
{
    public sealed class UiTweenTests
    {
        [Test]
        public void EaseOutBack_StartsAtZero_EndsAtOne_AndOvershoots()
        {
            Assert.That(UiTween.EaseOutBack(0f), Is.EqualTo(0f).Within(1e-5f));
            Assert.That(UiTween.EaseOutBack(1f), Is.EqualTo(1f).Within(1e-5f));
            Assert.That(UiTween.EaseOutBack(0.7f), Is.GreaterThan(1f), "springs past 1 before settling");
        }

        [Test]
        public void Pulse_IsZeroAtBothEnds_AndPeaksInTheMiddle()
        {
            Assert.That(UiTween.Pulse(0f), Is.EqualTo(0f).Within(1e-5f));
            Assert.That(UiTween.Pulse(1f), Is.EqualTo(0f).Within(1e-5f));
            Assert.That(UiTween.Pulse(0.5f), Is.EqualTo(1f).Within(1e-5f));
        }
    }
}

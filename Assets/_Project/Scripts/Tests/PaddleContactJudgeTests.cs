using NUnit.Framework;
using PingPong.Core;

namespace PingPong.Tests
{
    public sealed class PaddleContactJudgeTests
    {
        [Test]
        public void TheFirstContactCounts_AndOnesInTheSameSwingDoNot()
        {
            var j = new PaddleContactJudge(0.12f);
            Assert.That(j.ShouldCount(Side.Near, 1.00f), Is.True);
            Assert.That(j.ShouldCount(Side.Near, 1.05f), Is.False, "face then rim in the same swing");
            Assert.That(j.ShouldCount(Side.Near, 1.11f), Is.False);
            Assert.That(j.ShouldCount(Side.Near, 1.13f), Is.True, "a separate touch after the grace period");
        }

        [Test]
        public void EachPlayerHasHisOwnClock()
        {
            var j = new PaddleContactJudge(0.12f);
            Assert.That(j.ShouldCount(Side.Near, 1f), Is.True);
            Assert.That(j.ShouldCount(Side.Far, 1.01f), Is.True);
        }

        [Test]
        public void ResetForgetsEarlierContacts_AndNoneSideNeverCounts()
        {
            var j = new PaddleContactJudge(1f);
            j.ShouldCount(Side.Near, 5f);
            j.Reset();
            Assert.That(j.ShouldCount(Side.Near, 5.01f), Is.True);
            Assert.That(j.ShouldCount(Side.None, 9f), Is.False);
        }
    }
}

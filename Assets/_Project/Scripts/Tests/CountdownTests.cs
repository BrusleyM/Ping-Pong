using NUnit.Framework;
using PingPong.Core;

namespace PingPong.Tests
{
    public sealed class CountdownTests
    {
        [Test]
        public void FinishesOnceWhenDue()
        {
            var c = new Countdown();
            c.Start(now: 10f, seconds: 1.5f);
            Assert.That(c.IsPending, Is.True);
            Assert.That(c.TryFinish(11.4f), Is.False, "not due yet");
            Assert.That(c.TryFinish(11.5f), Is.True, "due exactly then");
            Assert.That(c.IsPending, Is.False);
            Assert.That(c.TryFinish(99f), Is.False, "only once");
        }

        [Test]
        public void NeverFinishesWithoutStart_OrAfterCancel()
        {
            var c = new Countdown();
            Assert.That(c.TryFinish(1000f), Is.False);
            c.Start(0f, 1f);
            c.Cancel();
            Assert.That(c.IsPending, Is.False);
            Assert.That(c.TryFinish(5f), Is.False);
        }

        [Test]
        public void RestartingReplacesTheDueTime()
        {
            var c = new Countdown();
            c.Start(0f, 1f);
            c.Start(0.5f, 2f);
            Assert.That(c.TryFinish(1.2f), Is.False);
            Assert.That(c.TryFinish(2.5f), Is.True);
        }
    }
}

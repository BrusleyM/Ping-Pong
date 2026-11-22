using NUnit.Framework;
using PingPong.Presentation;

namespace PingPong.Tests
{
    public sealed class TextFitTests
    {
        [Test]
        public void TextThatFits_KeepsItsFullSize()
        {
            Assert.That(TextFit.Size(96f, 800f, 1100f), Is.EqualTo(96f));
            Assert.That(TextFit.Size(96f, 1100f, 1100f), Is.EqualTo(96f), "exactly the limit still fits");
        }

        [Test]
        public void TextThatIsTooWide_ShrinksInProportion()
        {
            Assert.That(TextFit.Size(96f, 1400f, 1100f), Is.EqualTo(96f * 1100f / 1400f).Within(1e-3f));
        }

        [Test]
        public void ShrunkTextEndsUpExactlyAsWideAsTheLimit()
        {
            float size = TextFit.Size(96f, 1400f, 1100f);

            Assert.That(1400f * size / 96f, Is.EqualTo(1100f).Within(1e-2f));
        }

        [Test]
        public void NoMeasuredWidth_LeavesTheSizeAlone()
        {
            Assert.That(TextFit.Size(48f, 0f, 1100f), Is.EqualTo(48f));
        }
    }
}

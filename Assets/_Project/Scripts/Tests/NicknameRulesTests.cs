using NUnit.Framework;
using PingPong.Core;

namespace PingPong.Tests
{
    public class NicknameRulesTests
    {
        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void A_blank_nickname_is_not_valid(string raw)
        {
            Assert.IsFalse(NicknameRules.IsValid(raw));
            Assert.AreEqual(string.Empty, NicknameRules.Clean(raw));
        }

        [Test]
        public void Surrounding_spaces_are_trimmed()
        {
            Assert.AreEqual("Vishnu", NicknameRules.Clean("  Vishnu  "));
        }

        [Test]
        public void A_long_nickname_is_cut_to_the_maximum_length_without_trailing_space()
        {
            var cleaned = NicknameRules.Clean("Abcdefghijk lmnop");

            Assert.LessOrEqual(cleaned.Length, NicknameRules.MaxLength);
            Assert.AreEqual("Abcdefghijk", cleaned);
        }
    }
}

using NUnit.Framework;
using PingPong.Presentation;

namespace PingPong.Tests
{
    public sealed class MenuTextTests
    {
        [Test]
        public void Level_MarksOnlyTheCurrentLevel()
        {
            Assert.AreEqual("Easy", MenuText.Level("Easy", false));
            StringAssert.Contains("Hard", MenuText.Level("Hard", true));
            Assert.AreNotEqual(MenuText.Level("Hard", false), MenuText.Level("Hard", true));
        }

        [Test]
        public void Passthrough_ShowsTheState()
        {
            Assert.AreEqual("Passthrough: On", MenuText.Passthrough(true));
            Assert.AreEqual("Passthrough: Off", MenuText.Passthrough(false));
        }

        [Test]
        public void OnlineButton_SaysWhenUnavailable()
        {
            Assert.AreEqual(MenuText.Online, MenuText.OnlineButton(true));
            Assert.AreEqual(MenuText.OnlineUnavailable, MenuText.OnlineButton(false));
        }
    }
}

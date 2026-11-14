using NUnit.Framework;
using PingPong.Core;
using PingPong.Gameplay;
using PingPong.Presentation;

namespace PingPong.Tests
{
    public sealed class MatchTextTests
    {
        const string Ai = "AI (Medium)";

        [Test]
        public void BoardNames_LabelPlayerAndAI()
        {
            Assert.That(MatchText.BoardName(Side.Near, Side.Near, GameMode.Solo), Is.EqualTo("PLAYER"));
            Assert.That(MatchText.BoardName(Side.Far, Side.Near, GameMode.Solo), Is.EqualTo("AI"));
            Assert.That(MatchText.BoardName(Side.Far, Side.Near, GameMode.Online), Is.EqualTo("OPPONENT"));
        }

        [Test]
        public void Point_NamesTheWinnerAndTheReason()
        {
            Assert.That(MatchText.Point(Side.Far, PointReason.DoubleBounce, Side.Near, Ai), Is.EqualTo(Ai + " scores - double bounce"));
            Assert.That(MatchText.Point(Side.Near, PointReason.None, Side.Near, Ai), Is.EqualTo("You score"), "no dangling dash without a reason");
        }

        [Test]
        public void EveryPointReason_HasWording()
        {
            foreach (PointReason r in System.Enum.GetValues(typeof(PointReason)))
                if (r != PointReason.None) Assert.That(MatchText.Describe(r), Is.Not.Empty, r.ToString());
        }

        [Test]
        public void OpponentName_IsTheAIWithItsLevelInSoloPlay()
        {
            Assert.That(MatchText.OpponentName(GameMode.Solo, "Hard"), Is.EqualTo("AI (Hard)"));
            Assert.That(MatchText.OpponentName(GameMode.Online, "Hard"), Is.EqualTo("Opponent"));
            Assert.That(MatchText.OpponentName(GameMode.Solo, null), Is.EqualTo("Opponent"), "solo without an AI");
        }

        [Test]
        public void Game_CongratulatesTheLocalWinner()
        {
            Assert.That(MatchText.Game(Side.Near, Side.Near, Ai), Is.EqualTo("You win!"));
            Assert.That(MatchText.Game(Side.Far, Side.Near, Ai), Is.EqualTo(Ai + " wins!"));
        }
    }
}

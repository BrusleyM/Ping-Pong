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
        public void Number_IsTheScoreAsText_AndReusesTheTableStrings()
        {
            Assert.That(MatchText.Number(0), Is.EqualTo("0"));
            Assert.That(MatchText.Number(11), Is.EqualTo("11"));
            Assert.That(MatchText.Number(30), Is.EqualTo("30"));
            Assert.That(MatchText.Number(31), Is.EqualTo("31"), "beyond the table");
            Assert.That(MatchText.Number(-1), Is.EqualTo("-1"));
            Assert.That(ReferenceEquals(MatchText.Number(7), MatchText.Number(7)), Is.True, "no new string per call");
        }

        [Test]
        public void BoardNames_LabelPlayerAndAI()
        {
            Assert.That(MatchText.BoardName(Side.Near, Side.Near, GameMode.Solo), Is.EqualTo("PLAYER"));
            Assert.That(MatchText.BoardName(Side.Far, Side.Near, GameMode.Solo), Is.EqualTo("AI"));
            Assert.That(MatchText.BoardName(Side.Far, Side.Near, GameMode.Online), Is.EqualTo("OPPONENT"));
        }

        [Test]
        public void PointTitle_NamesTheWinner_AndLeavesTheReasonToItsOwnLine()
        {
            Assert.That(MatchText.PointTitle(Side.Near, Side.Near, GameMode.Solo), Is.EqualTo("You score"));
            Assert.That(MatchText.PointTitle(Side.Far, Side.Near, GameMode.Solo), Is.EqualTo("AI scores"));
            Assert.That(MatchText.PointTitle(Side.Far, Side.Near, GameMode.Online), Is.EqualTo("Opponent scores"));
        }

        [Test]
        public void DescribingNoReason_GivesNoReasonLine()
        {
            Assert.That(MatchText.Describe(PointReason.None), Is.Empty);
            Assert.That(MatchText.Describe(PointReason.DoubleBounce), Is.EqualTo("double bounce"));
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

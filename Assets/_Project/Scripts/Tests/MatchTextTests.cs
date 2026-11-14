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
        public void Names_AreFromTheLocalPlayersPointOfView()
        {
            Assert.That(MatchText.Name(Side.Near, Side.Near, Ai), Is.EqualTo("You"));
            Assert.That(MatchText.Name(Side.Far, Side.Near, Ai), Is.EqualTo(Ai));
            Assert.That(MatchText.Name(Side.Near, Side.Far, Ai), Is.EqualTo(Ai), "a Far-side local player sees Near as the opponent");
        }

        [Test]
        public void BoardNames_LabelPlayerAndAI()
        {
            Assert.That(MatchText.BoardName(Side.Near, Side.Near, GameMode.Solo), Is.EqualTo("PLAYER"));
            Assert.That(MatchText.BoardName(Side.Far, Side.Near, GameMode.Solo), Is.EqualTo("AI"));
            Assert.That(MatchText.BoardName(Side.Far, Side.Near, GameMode.Online), Is.EqualTo("OPPONENT"));
        }

        [Test]
        public void Status_DependsOnPhaseAndServer()
        {
            Assert.That(MatchText.Status(MatchPhase.WaitingForServe, Side.Near, Side.Near, Ai), Is.EqualTo("Your serve"));
            Assert.That(MatchText.Status(MatchPhase.WaitingForServe, Side.Far, Side.Near, Ai), Is.EqualTo(Ai + " serves"));
            Assert.That(MatchText.Status(MatchPhase.Rally, Side.Near, Side.Near, Ai), Is.Empty, "nothing distracting during a rally");
            Assert.That(MatchText.Status(MatchPhase.GameOver, Side.Near, Side.Near, Ai), Is.EqualTo(MatchText.PlayAgain));
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
        public void Detail_ShowsMessageOverStatus_AndBothAtGameOver()
        {
            Assert.That(MatchText.Detail(MatchPhase.WaitingForServe, "", "Your serve"), Is.EqualTo("Your serve"));
            Assert.That(MatchText.Detail(MatchPhase.PointOver, "You score - out", "Your serve"), Is.EqualTo("You score - out"));
            Assert.That(MatchText.Detail(MatchPhase.GameOver, "You win!", MatchText.PlayAgain), Is.EqualTo("You win!\n" + MatchText.PlayAgain));
            Assert.That(MatchText.Detail(MatchPhase.GameOver, "", MatchText.PlayAgain), Is.EqualTo(MatchText.PlayAgain), "no blank first line");
        }

        [Test]
        public void Game_CongratulatesTheLocalWinner()
        {
            Assert.That(MatchText.Game(Side.Near, Side.Near, Ai), Is.EqualTo("You win!"));
            Assert.That(MatchText.Game(Side.Far, Side.Near, Ai), Is.EqualTo(Ai + " wins!"));
        }
    }
}

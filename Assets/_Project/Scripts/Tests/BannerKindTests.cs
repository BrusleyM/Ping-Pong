using NUnit.Framework;
using PingPong.Core;

namespace PingPong.Tests
{
    public sealed class BannerKindTests
    {
        [Test]
        public void APointForTheLocalPlayer_IsWon_AndForTheOtherSide_IsLost()
        {
            Assert.That(BannerKinds.ForPoint(Side.Near, Side.Near), Is.EqualTo(BannerKind.PointWon));
            Assert.That(BannerKinds.ForPoint(Side.Far, Side.Near), Is.EqualTo(BannerKind.PointLost));
        }

        [Test]
        public void TheLocalSideDecidesWhatWinningMeans_WhateverSideItPlays()
        {
            Assert.That(BannerKinds.ForPoint(Side.Far, Side.Far), Is.EqualTo(BannerKind.PointWon));
            Assert.That(BannerKinds.ForGame(Side.Far, Side.Far), Is.EqualTo(BannerKind.GameWon));
        }

        [Test]
        public void AGameResult_IsWonOrLostFromTheLocalPlayersPointOfView()
        {
            Assert.That(BannerKinds.ForGame(Side.Near, Side.Near), Is.EqualTo(BannerKind.GameWon));
            Assert.That(BannerKinds.ForGame(Side.Far, Side.Near), Is.EqualTo(BannerKind.GameLost));
        }

        [Test]
        public void OnlyTheGameResultsStayOnScreen()
        {
            Assert.That(BannerKinds.StaysOnScreen(BannerKind.GameWon), Is.True);
            Assert.That(BannerKinds.StaysOnScreen(BannerKind.GameLost), Is.True);
            Assert.That(BannerKinds.StaysOnScreen(BannerKind.PointWon), Is.False);
            Assert.That(BannerKinds.StaysOnScreen(BannerKind.PointLost), Is.False);
            Assert.That(BannerKinds.StaysOnScreen(BannerKind.Let), Is.False);
        }
    }
}

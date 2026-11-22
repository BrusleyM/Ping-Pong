using NUnit.Framework;
using PingPong.Core;
using PingPong.Presentation;
using UnityEngine;

namespace PingPong.Tests
{
    public sealed class BannerLooksTests
    {
        UiTheme _theme;

        [SetUp]
        public void SetUp() => _theme = ScriptableObject.CreateInstance<UiTheme>();

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_theme);

        [Test]
        public void EachKindFillsWithItsThemeColour()
        {
            Assert.That(BannerLooks.For(_theme, BannerKind.PointWon).Fill, Is.EqualTo(_theme.Success));
            Assert.That(BannerLooks.For(_theme, BannerKind.PointLost).Fill, Is.EqualTo(_theme.Danger));
            Assert.That(BannerLooks.For(_theme, BannerKind.Let).Fill, Is.EqualTo(_theme.Secondary));
            Assert.That(BannerLooks.For(_theme, BannerKind.GameWon).Fill, Is.EqualTo(_theme.Accent));
            Assert.That(BannerLooks.For(_theme, BannerKind.GameLost).Fill, Is.EqualTo(_theme.Surface));
        }

        [Test]
        public void ADarkFillGetsWhiteText_AndALightFillGetsInk()
        {
            Assert.That(BannerLooks.For(_theme, BannerKind.PointLost).Text, Is.EqualTo(Color.white));
            Assert.That(BannerLooks.For(_theme, BannerKind.GameLost).Text, Is.EqualTo(Color.white));
            Assert.That(BannerLooks.For(_theme, BannerKind.PointWon).Text, Is.EqualTo(_theme.Ink));
            Assert.That(BannerLooks.For(_theme, BannerKind.GameWon).Text, Is.EqualTo(_theme.Ink));
        }

        [Test]
        public void TheLostGameIsFramedInGold_SoItReadsCalmAndDifferentFromAPointLost()
        {
            Assert.That(BannerLooks.For(_theme, BannerKind.GameLost).Border, Is.EqualTo(_theme.Accent));
        }

        [Test]
        public void EveryKindHasABorderDifferentFromItsFill()
        {
            foreach (BannerKind kind in System.Enum.GetValues(typeof(BannerKind)))
                Assert.That(BannerLooks.For(_theme, kind).Border, Is.Not.EqualTo(BannerLooks.For(_theme, kind).Fill), kind.ToString());
        }

        [Test]
        public void APlateBorderIsADarkerShadeOfItsTeamColour()
        {
            Color border = PlateLooks.BorderOf(_theme.TeamRed);

            Assert.That(border.r, Is.LessThan(_theme.TeamRed.r));
            Assert.That(border.g, Is.LessThan(_theme.TeamRed.g));
            Assert.That(border.b, Is.LessThan(_theme.TeamRed.b));
        }
    }
}

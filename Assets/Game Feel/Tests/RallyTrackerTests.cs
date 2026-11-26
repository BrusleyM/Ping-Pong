using NUnit.Framework;

namespace PingPong.GameFeel.Tests
{
    public class RallyTrackerTests
    {
        const int You = GameFeelTable.South;
        const int Opponent = GameFeelTable.North;

        RallyTracker _rally;

        [SetUp]
        public void CreateTracker() => _rally = new RallyTracker();

        [Test]
        public void TheComboGrowsWhileTheBallAlternatesAndStopsAtTheHighest()
        {
            _rally.TryPaddleHit(You, 0f);
            _rally.TryPaddleHit(Opponent, 1f);
            _rally.TryPaddleHit(You, 2f);

            Assert.That(_rally.Hits, Is.EqualTo(3));
            Assert.That(_rally.Longest, Is.EqualTo(3));
            Assert.That(_rally.Combo(2), Is.EqualTo(2));
        }

        [Test]
        public void ARepeatedContactFromTheSameSwingIsIgnored()
        {
            _rally.TryPaddleHit(You, 0f);

            Assert.That(_rally.TryPaddleHit(You, 0.1f), Is.False);
            Assert.That(_rally.Hits, Is.EqualTo(1));
        }

        [Test]
        public void AShotOntoTheOtherHalfLandsOnce()
        {
            _rally.TryPaddleHit(You, 0f);

            Assert.That(_rally.TableBounce(Opponent), Is.EqualTo(BounceOutcome.Landed));
            Assert.That(_rally.TableBounce(Opponent), Is.EqualTo(BounceOutcome.RallyOver));
            Assert.That(_rally.InPlay, Is.False);
        }

        [Test]
        public void ABounceOutsideARallyDoesNothing()
        {
            Assert.That(_rally.TableBounce(You), Is.EqualTo(BounceOutcome.None));
        }

        [Test]
        public void EndingARallyKeepsTheLongestAndResetClearsIt()
        {
            _rally.TryPaddleHit(You, 0f);
            _rally.TryPaddleHit(Opponent, 1f);
            _rally.End();

            Assert.That(_rally.Hits, Is.Zero);
            Assert.That(_rally.Longest, Is.EqualTo(2));

            _rally.Reset();

            Assert.That(_rally.Longest, Is.Zero);
        }

        [Test]
        public void TheComboIsOneWithoutHitsAndWhenTheHighestIsBelowOne()
        {
            Assert.That(_rally.Combo(8), Is.EqualTo(1));

            _rally.TryPaddleHit(You, 0f);
            _rally.TryPaddleHit(Opponent, 1f);

            Assert.That(_rally.Combo(0), Is.EqualTo(1));
        }

        [Test]
        public void AServeThatBouncesOnItsOwnHalfFirstStillLandsOnTheOtherHalf()
        {
            _rally.TryPaddleHit(You, 0f);

            Assert.That(_rally.TableBounce(You), Is.EqualTo(BounceOutcome.None));
            Assert.That(_rally.TableBounce(Opponent), Is.EqualTo(BounceOutcome.Landed));
        }

        [Test]
        public void APracticeReturnThatDoesNotLandBreaksTheStreak()
        {
            _rally.PracticeMode = true;
            _rally.Feed();
            _rally.TryPaddleHit(You, 0f);
            _rally.TableBounce(Opponent);
            _rally.Feed();
            _rally.TryPaddleHit(You, 1f);
            _rally.End();

            Assert.That(_rally.PracticeStreak, Is.Zero);
        }

        [Test]
        public void AFeedOutsideARallyPutsTheBallInPlayFromTheOtherSide()
        {
            _rally.Feed();

            Assert.That(_rally.InPlay, Is.True);
            Assert.That(_rally.Hitter, Is.EqualTo(Opponent));
        }

        [Test]
        public void APracticeStreakCountsLandedReturnsAndBreaksOnAFeedThatWasNotReturned()
        {
            _rally.PracticeMode = true;
            _rally.Feed();
            _rally.TryPaddleHit(You, 0f);
            _rally.TableBounce(Opponent);

            Assert.That(_rally.PracticeStreak, Is.EqualTo(1));

            _rally.Feed();
            _rally.Feed();

            Assert.That(_rally.PracticeStreak, Is.Zero);
        }
    }
}

using NUnit.Framework;
using PingPong.Core;
using UnityEngine;

namespace PingPong.Tests
{
    public sealed class RecoveryPathTests
    {
        static readonly Vector3 From = new Vector3(1f, 0f, 2f);
        static readonly Vector3 To = new Vector3(0f, 0.8f, -1f);

        static RecoveryPath Path(float duration = 1f, float arc = 0.25f) =>
            new RecoveryPath(From, Quaternion.identity, To, Quaternion.Euler(90f, 0f, 0f), duration, arc);

        [Test]
        public void AtTheStart_ThePaddleIsWhereItFell()
        {
            Assert.That(Path().PositionAt(0f), Is.EqualTo(From).Using(NearlyEqual.Instance));
        }

        [Test]
        public void AtTheEnd_ThePaddleIsOnTheTable_WithNoArcLeft()
        {
            RecoveryPath path = Path();

            Assert.That(path.PositionAt(1f), Is.EqualTo(To).Using(NearlyEqual.Instance));
            Assert.That(path.IsFinished(1f), Is.True);
            Assert.That(path.IsFinished(0.99f), Is.False);
        }

        [Test]
        public void InTheMiddle_ThePaddleIsLiftedAboveTheStraightLine()
        {
            RecoveryPath arced = Path(arc: 0.25f);
            RecoveryPath flat = Path(arc: 0f);

            Assert.That(arced.PositionAt(0.5f).y - flat.PositionAt(0.5f).y, Is.EqualTo(0.25f).Within(1e-4f));
        }

        [Test]
        public void TheTripStartsAndEndsGently_SoItCoversLessThanHalfTheDistanceInTheFirstQuarter()
        {
            RecoveryPath path = Path(arc: 0f);
            float travelled = Vector3.Distance(From, path.PositionAt(0.25f));

            Assert.That(travelled, Is.LessThan(Vector3.Distance(From, To) * 0.25f));
        }

        [Test]
        public void ThePaddleNeverMovesBackwardsAlongTheTrip()
        {
            RecoveryPath path = Path(arc: 0f);
            float previous = float.MaxValue;
            for (float t = 0f; t <= 1f; t += 0.05f)
            {
                float remaining = Vector3.Distance(path.PositionAt(t), To);
                Assert.That(remaining, Is.LessThanOrEqualTo(previous + 1e-4f));
                previous = remaining;
            }
        }

        [Test]
        public void ATimeBeyondTheEnd_StaysOnTheTable()
        {
            Assert.That(Path().PositionAt(5f), Is.EqualTo(To).Using(NearlyEqual.Instance));
        }

        [Test]
        public void ANoDurationTrip_JumpsStraightToTheEnd()
        {
            RecoveryPath path = Path(duration: 0f);

            Assert.That(path.PositionAt(0f), Is.EqualTo(To).Using(NearlyEqual.Instance));
            Assert.That(path.IsFinished(0f), Is.True);
        }

        [Test]
        public void TheBladeTurnsFromHowItFellToLyingFlat()
        {
            RecoveryPath path = Path();

            Assert.That(Quaternion.Angle(path.RotationAt(0f), Quaternion.identity), Is.LessThan(0.01f));
            Assert.That(Quaternion.Angle(path.RotationAt(1f), Quaternion.Euler(90f, 0f, 0f)), Is.LessThan(0.01f));
        }

        [Test]
        public void TheSpotIsOnThePlayersEndOfTheTable_TowardsTheHoldingHand()
        {
            var table = new TableFrame(new Vector3(0f, 0.76f, 0f), Vector3.forward, 2.74f, 1.525f);
            PaddleRecoveryFeel feel = PaddleRecoveryFeel.Default;

            Vector3 right = RecoverySpot.For(table, Side.Near, true, feel, 0.006f);
            Vector3 left = RecoverySpot.For(table, Side.Near, false, feel, 0.006f);

            Assert.That(right.z, Is.LessThan(0f), "the near end is behind the net");
            Assert.That(right.x, Is.EqualTo(feel.AcrossFromCentre).Within(1e-4f));
            Assert.That(left.x, Is.EqualTo(-feel.AcrossFromCentre).Within(1e-4f));
            Assert.That(right.y, Is.EqualTo(0.76f + 0.006f + feel.ClearanceAboveTable).Within(1e-4f));
            Assert.That(table.IsOverTable(right), Is.True, "it lands on the table, not past the end line");
        }
    }
}

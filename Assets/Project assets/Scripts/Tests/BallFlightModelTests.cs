using NUnit.Framework;
using PingPong.Core;
using UnityEngine;

namespace PingPong.Tests
{
    public sealed class BallFlightModelTests
    {
        const float SurfaceHeight = 0.76f;

        BallFlightModel _model;
        TableFrame _table;
        readonly BallBounce[] _bounces = new BallBounce[2];

        [SetUp]
        public void SetUp()
        {
            _model = new BallFlightModel(BallPhysicsSettings.Regulation);
            _table = TableFrame.Standard(new Vector3(0f, SurfaceHeight, 0f));
        }

        Vector3 NearHittingPoint => _table.ToWorld(new Vector3(0f, 0.25f, -_table.HalfLength - 0.2f));

        [TestCase(0.2f, 0.6f, 4f)]
        [TestCase(-0.5f, 1.1f, 6f)]
        [TestCase(0.6f, 0.3f, 3.5f)]
        [TestCase(0f, 1.0f, 9f)]
        public void SolveLaunch_LandsOnTargetAndClearsTheNet(float across, float alongFar, float speed)
        {
            Vector3 target = _table.ToWorld(new Vector3(across, 0f, alongFar));

            bool ok = _model.SolveLaunch(_table, NearHittingPoint, target, speed, out Vector3 v);
            int n = _model.Simulate(_table, NearHittingPoint, v, 3f, _bounces, out bool net);

            Assert.That(ok, Is.True, "solver reports the net is cleared");
            Assert.That(net, Is.False, "simulation agrees the net is cleared");
            Assert.That(n, Is.GreaterThan(0));
            Assert.That(_bounces[0].Side, Is.EqualTo(Side.Far));
            Vector3 miss = _bounces[0].Position - target;
            miss.y = 0f;
            Assert.That(miss.magnitude, Is.LessThan(0.05f), "lands within 5 cm of the target");
        }

        [Test]
        public void SolveLaunch_ImpossiblyFlatShot_ReportsTheNet()
        {
            // From below net height, a fast flat shot to just past the net can't clear it.
            Vector3 from = _table.ToWorld(new Vector3(0f, 0.05f, -0.6f));
            bool ok = _model.SolveLaunch(_table, from, _table.ToWorld(new Vector3(0f, 0f, 0.2f)), 12f, out _);
            Assert.That(ok, Is.False);
        }

        [Test]
        public void Simulate_FlatShotBelowNetHeight_HitsTheNet()
        {
            Vector3 from = _table.ToWorld(new Vector3(0f, _table.NetHeight * 0.5f, -0.5f));
            Vector3 v = _table.DirToWorld(new Vector3(0f, 0f, 8f));
            _model.Simulate(_table, from, v, 1f, _bounces, out bool net);
            Assert.That(net, Is.True);
        }

        [Test]
        public void Simulate_ReportsBouncesOnTheCorrectSides()
        {
            // A short serve from the game's real serve position: own side first, then the receiver's side.
            MatchPacing pacing = MatchPacing.Default;
            Vector3 from = _table.ToWorld(new Vector3(0f, pacing.ServeHeight, -_table.HalfLength - pacing.ServeBehind));
            Vector3 first = _table.PointOnSide(Side.Near, 0f, 0.4f * _table.HalfLength);
            _model.SolveLaunch(_table, from, first, 3.2f, out Vector3 v);
            int n = _model.Simulate(_table, from, v, 3f, _bounces, out bool net);
            Assert.That(net, Is.False);
            Assert.That(n, Is.EqualTo(2));
            Assert.That(_bounces[0].Side, Is.EqualTo(Side.Near));
            Assert.That(_bounces[1].Side, Is.EqualTo(Side.Far));
            Assert.That(_bounces[1].Time, Is.GreaterThan(_bounces[0].Time));
        }

        [Test]
        public void Drag_ShortensTheFlight()
        {
            var noDrag = BallPhysicsSettings.Regulation;
            noDrag.DragK = 0f;
            var vacuum = new BallFlightModel(noDrag);
            Vector3 from = _table.ToWorld(new Vector3(0f, 0.3f, -_table.HalfLength));
            Vector3 v = _table.DirToWorld(new Vector3(0f, 1.5f, 5f));

            _model.Simulate(_table, from, v, 3f, _bounces, out _);
            float withDrag = _table.ToLocal(_bounces[0].Position).z;
            vacuum.Simulate(_table, from, v, 3f, _bounces, out _);
            float withoutDrag = _table.ToLocal(_bounces[0].Position).z;

            Assert.That(withDrag, Is.LessThan(withoutDrag));
        }

        [Test]
        public void BounceOffTable_AppliesRestitutionAndFriction()
        {
            var s = BallPhysicsSettings.Regulation;
            Vector3 after = _model.BounceOffTable(new Vector3(1f, -4f, 3f));
            Assert.That(after.y, Is.EqualTo(4f * s.TableRestitution).Within(1e-5f));
            Assert.That(after.x, Is.EqualTo(1f * s.TableFriction).Within(1e-5f));
            Assert.That(after.z, Is.EqualTo(3f * s.TableFriction).Within(1e-5f));
            Assert.That(_model.BounceOffTable(new Vector3(0f, 2f, 0f)).y, Is.EqualTo(2f), "rising balls are unchanged");
        }

        [Test]
        public void TableFrame_RoundTripsAndKnowsSides()
        {
            var rotated = new TableFrame(new Vector3(1f, 0.76f, -2f), new Vector3(1f, 0f, 1f), 2.74f, 1.525f);
            Vector3 world = new Vector3(0.3f, 1.1f, -1.4f);
            Vector3 back = rotated.ToWorld(rotated.ToLocal(world));
            Assert.That((back - world).magnitude, Is.LessThan(1e-4f));
            Assert.That(rotated.SideOf(rotated.PointOnSide(Side.Near, 0f, 0.5f)), Is.EqualTo(Side.Near));
            Assert.That(rotated.SideOf(rotated.PointOnSide(Side.Far, 0f, 0.5f)), Is.EqualTo(Side.Far));
        }

        [Test]
        public void PointOnSide_MeasuresFromTheNetTowardsThatPlayer()
        {
            Vector3 near = _table.ToLocal(_table.PointOnSide(Side.Near, 0.3f, 1.0f, 0.25f));
            Vector3 far = _table.ToLocal(_table.PointOnSide(Side.Far, -0.2f, 1.6f));
            Assert.That((near - new Vector3(0.3f, 0.25f, -1.0f)).magnitude, Is.LessThan(1e-5f));
            Assert.That((far - new Vector3(-0.2f, 0f, 1.6f)).magnitude, Is.LessThan(1e-5f), "beyond HalfLength is behind the end line");
        }

        [Test]
        public void ClampToSide_KeepsTheSpotInsideTheMargins()
        {
            var margins = new LandingMargins(edge: 0.1f, fromNet: 0.3f, fromEnd: 0.12f);

            Vector3 wild = _table.ToLocal(_table.ClampToSide(Side.Far, 5f, 9f, margins));
            Assert.That(wild.x, Is.EqualTo(_table.HalfWidth - 0.1f).Within(1e-5f));
            Assert.That(wild.z, Is.EqualTo(_table.HalfLength - 0.12f).Within(1e-5f));

            Vector3 shortLeft = _table.ToLocal(_table.ClampToSide(Side.Near, -5f, 0.05f, margins));
            Assert.That(shortLeft.x, Is.EqualTo(-_table.HalfWidth + 0.1f).Within(1e-5f));
            Assert.That(shortLeft.z, Is.EqualTo(-0.3f).Within(1e-5f), "never closer to the net than the margin, on the right side");

            Vector3 inside = _table.ToLocal(_table.ClampToSide(Side.Far, 0.2f, 0.9f, margins));
            Assert.That((inside - new Vector3(0.2f, 0f, 0.9f)).magnitude, Is.LessThan(1e-5f), "legal spots are unchanged");
        }

        [TestCase(0.2f, 10f, 0.8f)]
        [TestCase(0.25f, 8f, 0.4f)]
        public void SolveLaunchClearingNet_AddsArcWhenTheFastShotHitsTheNet(float height, float speed, float fromNet)
        {
            Vector3 from = _table.PointOnSide(Side.Near, 0f, _table.HalfLength + 0.2f, height);
            Vector3 target = _table.PointOnSide(Side.Far, 0f, fromNet);
            var retry = new NetClearRetry(attempts: 5, speedFalloff: 0.85f);

            Assert.That(_model.SolveLaunch(_table, from, target, speed, out _), Is.False, "precondition: the fast shot hits the net");
            Assert.That(_model.SolveLaunchClearingNet(_table, from, target, speed, retry, out Vector3 v), Is.True);
            Assert.That(new Vector2(v.x, v.z).magnitude, Is.LessThan(speed), "the retry slowed the shot down");
            _model.Simulate(_table, from, v, 3f, _bounces, out bool net);
            Assert.That(net, Is.False);
            Assert.That(_bounces[0].Side, Is.EqualTo(Side.Far));
        }

        [Test]
        public void SolveLaunchClearingNet_StopsAfterTheConfiguredAttempts()
        {
            Vector3 from = _table.ToWorld(new Vector3(0f, 0.05f, -0.6f)); // below the net, right next to it
            Vector3 target = _table.PointOnSide(Side.Far, 0f, 0.2f);
            bool ok = _model.SolveLaunchClearingNet(_table, from, target, 12f, new NetClearRetry(attempts: 1, speedFalloff: 0.5f), out _);
            Assert.That(ok, Is.False);
        }
    }
}

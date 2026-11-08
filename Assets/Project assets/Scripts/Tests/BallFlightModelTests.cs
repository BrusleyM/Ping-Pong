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
            Vector3 first = _table.PointOnSide(Side.Near, 0f, 0.4f);
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
    }
}

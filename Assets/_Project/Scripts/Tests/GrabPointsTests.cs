using NUnit.Framework;
using PingPong.Gameplay;

namespace PingPong.Tests
{
    public class GrabPointsTests
    {
        private GrabPoints _points;

        [SetUp]
        public void SetUp()
        {
            _points = new GrabPoints();
        }

        [Test]
        public void The_first_grab_point_makes_the_object_held()
        {
            Assert.IsTrue(_points.Begin(1));
        }

        [Test]
        public void A_second_grab_point_does_not_make_it_newly_held()
        {
            _points.Begin(1);

            Assert.IsFalse(_points.Begin(2));
        }

        [Test]
        public void Ending_one_of_two_grab_points_keeps_it_held()
        {
            _points.Begin(1);
            _points.Begin(2);

            Assert.IsFalse(_points.End(1));
        }

        [Test]
        public void Ending_the_last_grab_point_frees_it()
        {
            _points.Begin(1);
            _points.Begin(2);
            _points.End(1);

            Assert.IsTrue(_points.End(2));
        }

        [Test]
        public void Ending_a_grab_point_that_never_began_frees_nothing()
        {
            Assert.IsFalse(_points.End(7));
        }

        [Test]
        public void Beginning_the_same_grab_point_twice_still_needs_one_end_to_free_it()
        {
            _points.Begin(1);
            _points.Begin(1);

            Assert.IsTrue(_points.End(1));
        }
    }
}

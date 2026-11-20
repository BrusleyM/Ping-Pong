using NUnit.Framework;
using PingPong.Presentation;
using UnityEngine;

namespace PingPong.Tests
{
    public class HeadPlacementTests
    {
        private GameObject _head;

        [SetUp]
        public void SetUp()
        {
            _head = new GameObject("Head");
            _head.transform.position = new Vector3(0f, 1.6f, 0f);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_head);
        }

        [Test]
        public void The_panel_is_placed_the_given_distance_ahead_of_the_head_at_eye_level_less_the_drop()
        {
            var pose = HeadPlacement.InFrontOf(_head.transform, 1.2f, 0.1f);

            Assert.AreEqual(1.2f, pose.position.z, 1e-4f);
            Assert.AreEqual(1.5f, pose.position.y, 1e-4f);
        }

        [Test]
        public void Looking_up_or_down_does_not_tilt_the_panel()
        {
            _head.transform.rotation = Quaternion.Euler(45f, 90f, 0f);

            var pose = HeadPlacement.InFrontOf(_head.transform, 1f, 0f);

            Assert.AreEqual(1.6f, pose.position.y, 1e-4f);
            Assert.AreEqual(1f, pose.position.x, 1e-4f);
            Assert.AreEqual(0f, (pose.rotation * Vector3.forward).y, 1e-4f);
        }

        [Test]
        public void Looking_straight_down_still_gives_a_usable_direction()
        {
            _head.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            var pose = HeadPlacement.InFrontOf(_head.transform, 1f, 0f);

            Assert.AreEqual(0f, (pose.rotation * Vector3.forward).y, 1e-4f);
            Assert.AreEqual(1f, new Vector2(pose.position.x, pose.position.z).magnitude, 1e-4f);
        }
    }
}

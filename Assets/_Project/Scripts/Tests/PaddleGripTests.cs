using NUnit.Framework;
using PingPong.Gameplay;
using UnityEditor;
using UnityEngine;

namespace PingPong.Tests
{
    public sealed class PaddleGripTests
    {
        GameObject _paddle, _left, _right;
        PaddleGrip _grip;

        [SetUp]
        public void SetUp()
        {
            _paddle = new GameObject("Paddle", typeof(Rigidbody));
            var blade = new GameObject("Blade").transform;
            blade.SetParent(_paddle.transform, false);
            blade.localPosition = new Vector3(0f, 0f, 0.15f);
            var model = _paddle.AddComponent<PaddleModel>();
            Set(model, "_blade", blade);
            _left = new GameObject("Left");
            _right = new GameObject("Right");
            _grip = _paddle.AddComponent<PaddleGrip>();
            Set(_grip, "_model", model);
            Set(_grip, "_leftHand", _left.transform);
            Set(_grip, "_rightHand", _right.transform);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_paddle);
            Object.DestroyImmediate(_left);
            Object.DestroyImmediate(_right);
        }

        static void Set(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // The grip point: 13 cm (the default blade distance) down the handle from the blade centre.
        Vector3 Handle => _paddle.transform.TransformPoint(new Vector3(0f, -0.13f, 0.15f));

        [Test]
        public void Grab_NeedsTheHandNearTheHandle()
        {
            _right.transform.position = Handle + Vector3.right * 2f;
            _grip.Toggle(Hand.Right);
            Assert.That(_grip.HeldBy, Is.EqualTo(Hand.None));

            _right.transform.position = Handle;
            _grip.Toggle(Hand.Right);
            Assert.That(_grip.HeldBy, Is.EqualTo(Hand.Right));
            Assert.That(_paddle.GetComponent<Rigidbody>().isKinematic, Is.True);
        }

        [Test]
        public void GrabAgain_WithTheSameHand_LetsGo_AndTheOtherHandCantSteal()
        {
            _left.transform.position = _right.transform.position = Handle;
            _grip.Toggle(Hand.Right);
            _grip.Toggle(Hand.Left);
            Assert.That(_grip.HeldBy, Is.EqualTo(Hand.Right), "the other hand doesn't take it");

            Hand reported = Hand.Right;
            _grip.HeldByChanged += h => reported = h;
            _grip.Toggle(Hand.Right);
            Assert.That(_grip.HeldBy, Is.EqualTo(Hand.None));
            Assert.That(reported, Is.EqualTo(Hand.None));
            Assert.That(_paddle.GetComponent<Rigidbody>().isKinematic, Is.False);
        }

        sealed class FakeCommands : MonoBehaviour, IPlayerCommands
        {
            public bool RightControllerActive = true;
            public bool GrabPressed(Hand hand) => false;
            public bool ControllerActive(Hand hand) => hand != Hand.Right || RightControllerActive;
            public bool MenuPressed => false;
        }

        [Test]
        public void LosingTheControllerReleasesThePaddle_WithoutAFling()
        {
            var host = new GameObject("Commands", typeof(FakeCommands));
            try
            {
                var commands = host.GetComponent<FakeCommands>();
                var so = new SerializedObject(_grip);
                so.FindProperty("_commands._component").objectReferenceValue = commands;
                so.ApplyModifiedPropertiesWithoutUndo();

                _right.transform.position = Handle;
                _grip.Toggle(Hand.Right);
                Assert.That(_grip.HeldBy, Is.EqualTo(Hand.Right));

                commands.RightControllerActive = false; // hand tracking took over
                InvokeUpdate();
                Assert.That(_grip.HeldBy, Is.EqualTo(Hand.None));
                var body = _paddle.GetComponent<Rigidbody>();
                Assert.That(body.isKinematic, Is.False);
                Assert.That(body.linearVelocity, Is.EqualTo(Vector3.zero));
            }
            finally { Object.DestroyImmediate(host); }
        }

        void InvokeUpdate() => typeof(PaddleGrip).GetMethod("Update", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(_grip, null);
    }
}

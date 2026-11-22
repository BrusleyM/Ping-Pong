using NUnit.Framework;
using PingPong.Gameplay;
using UnityEditor;
using UnityEngine;

namespace PingPong.Tests
{
    public sealed class PaddleGripTests
    {
        GameObject _paddle, _left, _right, _commandsHost, _inputModeHost;
        PaddleGrip _grip;
        FakeCommands _commands;
        FakeInputMode _inputMode;

        [SetUp]
        public void SetUp()
        {
            // Wired first, then Awake runs: PaddleGrip subscribes to the commands in Awake.
            _paddle = new GameObject("Paddle", typeof(Rigidbody));
            _paddle.SetActive(false);
            var blade = new GameObject("Blade").transform;
            blade.SetParent(_paddle.transform, false);
            blade.localPosition = new Vector3(0f, 0f, 0.15f);
            var model = _paddle.AddComponent<PaddleModel>();
            Set(model, "_blade", blade);
            _left = new GameObject("Left");
            _right = new GameObject("Right");
            _commandsHost = new GameObject("Commands", typeof(FakeCommands));
            _commands = _commandsHost.GetComponent<FakeCommands>();
            _inputModeHost = new GameObject("InputMode", typeof(FakeInputMode));
            _inputMode = _inputModeHost.GetComponent<FakeInputMode>();
            _grip = _paddle.AddComponent<PaddleGrip>();
            Set(_grip, "_model", model);
            Set(_grip, "_leftHand", _left.transform);
            Set(_grip, "_rightHand", _right.transform);
            Set(_grip, "_commands._component", _commands);
            Set(_grip, "_inputMode._component", _inputMode);
            _paddle.SetActive(true);
            // Edit-mode tests don't run Awake on their own (the component isn't ExecuteAlways): run it as Unity would.
            RunLifecycle("Awake");
        }

        // Edit-mode tests don't get Awake/OnDestroy from Unity (the component isn't ExecuteAlways) and SendMessage asserts there.
        void RunLifecycle(string method) =>
            typeof(PaddleGrip).GetMethod(method, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(_grip, null);

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_paddle);
            Object.DestroyImmediate(_left);
            Object.DestroyImmediate(_right);
            Object.DestroyImmediate(_commandsHost);
            Object.DestroyImmediate(_inputModeHost);
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
            public event System.Action<Hand> GrabPerformed;
            public event System.Action MenuPerformed;
            public void PressGrab(Hand hand) => GrabPerformed?.Invoke(hand);
            public void PressMenu() => MenuPerformed?.Invoke();
            public bool HasGrabListeners => GrabPerformed != null;
        }

        sealed class FakeInputMode : MonoBehaviour, IInputMode
        {
            public InputMode Current { get; private set; } = InputMode.Controllers;
            public event System.Action<InputMode> Changed;
            public bool HasListeners => Changed != null;
            public void Switch(InputMode mode) { Current = mode; Changed?.Invoke(mode); }
        }

        [Test]
        public void SwitchingToHandTracking_ReleasesThePaddleWhereItIs_WithoutAFling()
        {
            _right.transform.position = Handle;
            _grip.Toggle(Hand.Right);
            Assert.That(_grip.HeldBy, Is.EqualTo(Hand.Right));

            _inputMode.Switch(InputMode.Hands);

            Assert.That(_grip.HeldBy, Is.EqualTo(Hand.None));
            var body = _paddle.GetComponent<Rigidbody>();
            Assert.That(body.isKinematic, Is.False);
            Assert.That(body.linearVelocity, Is.EqualTo(Vector3.zero));
            Assert.That(_grip.enabled, Is.False, "idle again: no per-frame work");
        }

        [Test]
        public void SwitchingToHandTracking_WhileNotHolding_ChangesNothing()
        {
            _inputMode.Switch(InputMode.Hands);
            Assert.That(_grip.HeldBy, Is.EqualTo(Hand.None));
            Assert.That(_paddle.GetComponent<Rigidbody>().isKinematic, Is.False);
        }

        [Test]
        public void WhenTheControllersReturn_ThePaddleComesBackToTheHandThatHeldIt()
        {
            _right.transform.position = Handle;
            _grip.Toggle(Hand.Right);
            _inputMode.Switch(InputMode.Hands);

            _inputMode.Switch(InputMode.Controllers);

            Assert.That(_grip.HeldBy, Is.EqualTo(Hand.Right));
            Assert.That(_paddle.GetComponent<Rigidbody>().isKinematic, Is.True);
        }

        [Test]
        public void ThePaddleComesBackOnlyOnce_NotOnEveryLaterSwitch()
        {
            _right.transform.position = Handle;
            _grip.Toggle(Hand.Right);
            _inputMode.Switch(InputMode.Hands);
            _inputMode.Switch(InputMode.Controllers);
            _grip.Toggle(Hand.Right);
            Assert.That(_grip.HeldBy, Is.EqualTo(Hand.None));

            _inputMode.Switch(InputMode.Hands);
            _inputMode.Switch(InputMode.Controllers);

            Assert.That(_grip.HeldBy, Is.EqualTo(Hand.None), "it was let go on purpose");
        }

        [Test]
        public void GrabbingWithTheOtherControllerWhileInHandMode_IsNotStolenBackWhenControllersReturn()
        {
            _right.transform.position = Handle;
            _grip.Toggle(Hand.Right);
            _inputMode.Switch(InputMode.Hands);
            _left.transform.position = Handle;
            _grip.Toggle(Hand.Left);
            Assert.That(_grip.HeldBy, Is.EqualTo(Hand.Left));

            _inputMode.Switch(InputMode.Controllers);

            Assert.That(_grip.HeldBy, Is.EqualTo(Hand.Left));
        }

        [Test]
        public void StartingAMatch_PutsThePaddleInTheHandOnlyWithControllers()
        {
            var onMatchStarted = typeof(PaddleGrip).GetMethod("OnMatchStarted", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            _inputMode.Switch(InputMode.Hands);

            onMatchStarted.Invoke(_grip, null);
            Assert.That(_grip.HeldBy, Is.EqualTo(Hand.None), "hands: it waits on the table");

            _inputMode.Switch(InputMode.Controllers);
            onMatchStarted.Invoke(_grip, null);
            Assert.That(_grip.HeldBy, Is.EqualTo(Hand.Right));
        }

        [Test]
        public void GrabEvent_TakesThenLetsGo()
        {
            _right.transform.position = Handle;
            _commands.PressGrab(Hand.Right);
            Assert.That(_grip.HeldBy, Is.EqualTo(Hand.Right), "a grab event near the handle takes the paddle");
            _commands.PressGrab(Hand.Right);
            Assert.That(_grip.HeldBy, Is.EqualTo(Hand.None), "the same hand again lets go");
        }

        [Test]
        public void TheComponentRunsOnlyWhileHeld()
        {
            Assert.That(_grip.enabled, Is.False, "idle: no per-frame work");
            _right.transform.position = Handle;
            _grip.Toggle(Hand.Right);
            Assert.That(_grip.enabled, Is.True);
            _grip.Toggle(Hand.Right);
            Assert.That(_grip.enabled, Is.False);
        }

        [Test]
        public void DestroyingTheGrip_StopsListeningToTheCommands()
        {
            Assert.That(_commands.HasGrabListeners, Is.True);
            RunLifecycle("OnDestroy"); // edit-mode destruction doesn't call it; run it as Unity would
            Assert.That(_commands.HasGrabListeners, Is.False);
            Assert.That(_inputMode.HasListeners, Is.False);
        }
    }
}

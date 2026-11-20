using Framework;
using NUnit.Framework;
using PingPong.Gameplay;
using PingPong.Presentation;
using UnityEngine;

namespace PingPong.Tests
{
    public class PlaceOnHeadTrackingTests
    {
        private sealed class TestHeadTracking : MonoBehaviour, IHeadTracking
        {
            public bool IsTracked { get; set; }

            public event System.Action Acquired;

            public event System.Action Recentered;

            public void RaiseAcquired() => Acquired?.Invoke();

            public void RaiseRecentered() => Recentered?.Invoke();

            public int AcquiredListeners => Acquired?.GetInvocationList().Length ?? 0;
        }

        private GameObject _eyes;
        private GameObject _canvas;
        private TestHeadTracking _tracking;
        private PlaceOnHeadTracking _trigger;

        [SetUp]
        public void SetUp()
        {
            _eyes = new GameObject("Eyes");
            _eyes.transform.position = new Vector3(0f, 1.7f, 0f);
            _canvas = new GameObject("Canvas");
            _canvas.SetActive(false);
            _tracking = _canvas.AddComponent<TestHeadTracking>();
            var placer = _canvas.AddComponent<EyeHeightPlacer>();
            SetField(placer, "_eyes", _eyes.transform);
            SetField(placer, "_target", _canvas.transform);
            SetField(placer, "_distanceInFront", 1.2f);
            _trigger = _canvas.AddComponent<PlaceOnHeadTracking>();
            SetField(_trigger, "_placer", placer);
            SetField(_trigger, "_tracking", new Seam<IHeadTracking>(_tracking));
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_eyes);
            Object.DestroyImmediate(_canvas);
        }

        private static void SetField(object target, string name, object value)
        {
            target.GetType().GetField(name, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(target, value);
        }

        [Test]
        public void Tracking_that_is_already_on_places_the_canvas_straight_away()
        {
            _tracking.IsTracked = true;

            Enable();

            Assert.AreEqual(1.7f, _canvas.transform.position.y, 1e-4f);
            Assert.AreEqual(1.2f, _canvas.transform.position.z, 1e-4f);
        }

        [Test]
        public void Without_tracking_the_canvas_stays_where_it_was_until_tracking_is_acquired()
        {
            _canvas.transform.position = new Vector3(0f, 1.5f, 1f);
            Enable();

            Assert.AreEqual(1.5f, _canvas.transform.position.y, 1e-4f);

            _tracking.RaiseAcquired();

            Assert.AreEqual(1.7f, _canvas.transform.position.y, 1e-4f);
        }

        [Test]
        public void Recentering_places_the_canvas_again_in_front_of_the_new_view()
        {
            Enable();
            _eyes.transform.rotation = Quaternion.Euler(0f, 90f, 0f);

            _tracking.RaiseRecentered();

            Assert.AreEqual(1.2f, _canvas.transform.position.x, 1e-4f);
        }

        [Test]
        public void Disabling_the_trigger_stops_it_listening_to_tracking()
        {
            Enable();
            Assert.AreEqual(1, _tracking.AcquiredListeners);

            Call("OnDisable");

            Assert.AreEqual(0, _tracking.AcquiredListeners);
        }

        private void Enable()
        {
            Call("OnEnable");
            Call("Start");
        }

        private void Call(string method)
        {
            typeof(PlaceOnHeadTracking).GetMethod(method, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(_trigger, null);
        }
    }
}

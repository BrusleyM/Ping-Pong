using System.Reflection;
using NUnit.Framework;
using PingPong.Gameplay;
using UnityEngine;

namespace PingPong.Tests
{
    public class ViewHeightAdjusterTests
    {
        private sealed class SeatedHead : MonoBehaviour, IHeadTracking
        {
            public bool IsTracked { get; set; } = true;

            public float EyeHeight { get; set; } = 1.15f;

#pragma warning disable CS0067
            public event System.Action Acquired;
#pragma warning restore CS0067

            public event System.Action Recentered;

            public event System.Action PoseUpdated;

            public void RaisePoseUpdated() => PoseUpdated?.Invoke();

            public void RaiseRecentered() => Recentered?.Invoke();
        }

        private static readonly FieldInfo SharedSettings = typeof(AppServices).GetField("_settings", BindingFlags.NonPublic | BindingFlags.Static);

        private object _settingsBefore;
        private SettingsService _settings;
        private GameObject _rig;
        private GameObject _host;
        private SeatedHead _head;
        private ViewHeightAdjuster _adjuster;
        private ViewHeightProfile _profile;

        [SetUp]
        public void SetUp()
        {
            _settingsBefore = SharedSettings.GetValue(null);
            _settings = new SettingsService(new InMemoryStore<GameSettings>());
            SharedSettings.SetValue(null, _settings);
            _rig = new GameObject("Rig");
            _host = new GameObject("ViewHeight");
            _head = _host.AddComponent<SeatedHead>();
            _adjuster = _host.AddComponent<ViewHeightAdjuster>();
            _profile = ScriptableObject.CreateInstance<ViewHeightProfile>();
            SetField("_tracking", new Seam<IHeadTracking>(_head));
            SetField("_rig", _rig.transform);
            SetField("_profile", _profile);
            Call("Awake");
            Call("OnEnable");
        }

        [TearDown]
        public void TearDown()
        {
            Call("OnDisable");
            SharedSettings.SetValue(null, _settingsBefore);
            Object.DestroyImmediate(_rig);
            Object.DestroyImmediate(_host);
            Object.DestroyImmediate(_profile);
        }

        [Test]
        public void Standing_leaves_the_rig_on_the_floor_whatever_the_head_height()
        {
            _head.EyeHeight = 1f;
            _head.RaisePoseUpdated();

            Assert.AreEqual(0f, _rig.transform.position.y);
        }

        [Test]
        public void Choosing_seated_lifts_the_rig_on_the_next_tracked_pose_and_announces_it()
        {
            var announced = 0;
            _adjuster.Adjusted += () => announced++;

            _settings.Update(settings => settings.Posture = PlayPosture.Seated);
            _head.RaisePoseUpdated();
            _head.EyeHeight = 1.3f;
            _head.RaisePoseUpdated();

            Assert.AreEqual(0.45f, _rig.transform.position.y, 1e-4f);
            Assert.AreEqual(1, announced);
        }

        [Test]
        public void An_untracked_headset_is_waited_for()
        {
            _head.IsTracked = false;
            _settings.Update(settings => settings.Posture = PlayPosture.Seated);
            _head.RaisePoseUpdated();
            Assert.AreEqual(0f, _rig.transform.position.y);

            _head.IsTracked = true;
            _head.RaisePoseUpdated();

            Assert.AreEqual(0.45f, _rig.transform.position.y, 1e-4f);
        }

        [Test]
        public void A_headset_reading_that_is_too_low_is_waited_out()
        {
            _head.EyeHeight = 0.2f;
            _settings.Update(settings => settings.Posture = PlayPosture.Seated);
            _head.RaisePoseUpdated();
            Assert.AreEqual(0f, _rig.transform.position.y);

            _head.EyeHeight = 1.15f;
            _head.RaisePoseUpdated();

            Assert.AreEqual(0.45f, _rig.transform.position.y, 1e-4f);
        }

        [Test]
        public void Recentering_measures_the_seated_player_again()
        {
            _settings.Update(settings => settings.Posture = PlayPosture.Seated);
            _head.RaisePoseUpdated();
            _head.EyeHeight = 1.3f;

            _head.RaiseRecentered();
            _head.RaisePoseUpdated();

            Assert.AreEqual(0.3f, _rig.transform.position.y, 1e-4f);
        }

        [Test]
        public void Going_back_to_standing_puts_the_rig_back_on_the_floor()
        {
            _settings.Update(settings => settings.Posture = PlayPosture.Seated);
            _head.RaisePoseUpdated();

            _settings.Update(settings => settings.Posture = PlayPosture.Standing);
            _head.RaisePoseUpdated();

            Assert.AreEqual(0f, _rig.transform.position.y);
        }

        private void SetField(string name, object value)
        {
            typeof(ViewHeightAdjuster).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(_adjuster, value);
        }

        private void Call(string method)
        {
            typeof(ViewHeightAdjuster).GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(_adjuster, null);
        }
    }
}

using System.Collections.Generic;
using NUnit.Framework;
using PingPong.Gameplay;

namespace PingPong.Tests
{
    public class InputModeTrackerTests
    {
        private InputModeTracker _tracker;
        private List<InputMode> _changes;

        [SetUp]
        public void SetUp()
        {
            _tracker = new InputModeTracker();
            _changes = new List<InputMode>();
            _tracker.Changed += _changes.Add;
        }

        [Test]
        public void The_mode_starts_as_controllers()
        {
            Assert.AreEqual(InputMode.Controllers, _tracker.Current);
        }

        [Test]
        public void Neither_controller_being_active_means_hands()
        {
            _tracker.Report(false, false);

            Assert.AreEqual(InputMode.Hands, _tracker.Current);
        }

        [Test]
        public void One_controller_still_active_means_controllers()
        {
            _tracker.Report(false, true);

            Assert.AreEqual(InputMode.Controllers, _tracker.Current);
            CollectionAssert.IsEmpty(_changes);
        }

        [Test]
        public void Changing_mode_is_announced_once_and_repeating_the_same_report_is_silent()
        {
            _tracker.Report(false, false);
            _tracker.Report(false, false);
            _tracker.Report(true, false);

            CollectionAssert.AreEqual(new[] { InputMode.Hands, InputMode.Controllers }, _changes);
        }
    }
}

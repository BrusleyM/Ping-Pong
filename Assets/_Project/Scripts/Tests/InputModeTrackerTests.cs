using System.Collections.Generic;
using NUnit.Framework;
using PingPong.Gameplay;

namespace PingPong.Tests
{
    public class InputModeTrackerTests
    {
        private const float LostAfter = 0.5f;
        private const float RegainAfter = 0.25f;

        private InputModeTracker _tracker;
        private List<InputMode> _changes;

        [SetUp]
        public void SetUp()
        {
            _tracker = new InputModeTracker(LostAfter, RegainAfter);
            _changes = new List<InputMode>();
            _tracker.Changed += _changes.Add;
        }

        [Test]
        public void The_mode_starts_as_controllers()
        {
            Assert.AreEqual(InputMode.Controllers, _tracker.Current);
        }

        [Test]
        public void Neither_controller_being_active_means_hands_once_it_has_lasted_long_enough()
        {
            _tracker.Report(0f, false, false);
            _tracker.Report(LostAfter, false, false);

            Assert.AreEqual(InputMode.Hands, _tracker.Current);
        }

        [Test]
        public void A_controller_flicker_shorter_than_the_lost_delay_does_not_change_the_mode()
        {
            _tracker.Report(0f, false, false);
            _tracker.Report(LostAfter - 0.1f, true, false);

            Assert.AreEqual(InputMode.Controllers, _tracker.Current);
            CollectionAssert.IsEmpty(_changes);
        }

        [Test]
        public void The_tracker_reports_settling_while_the_raw_state_differs_from_the_mode()
        {
            _tracker.Report(0f, false, false);
            Assert.IsTrue(_tracker.Settling);

            _tracker.Report(LostAfter, false, false);
            Assert.IsFalse(_tracker.Settling);
        }

        [Test]
        public void Controllers_return_only_after_the_regain_delay()
        {
            _tracker.Report(0f, false, false);
            _tracker.Report(LostAfter, false, false);

            _tracker.Report(1f, true, false);
            Assert.AreEqual(InputMode.Hands, _tracker.Current);

            _tracker.Report(1f + RegainAfter, true, false);
            Assert.AreEqual(InputMode.Controllers, _tracker.Current);
        }

        [Test]
        public void One_controller_still_active_keeps_the_mode_on_controllers()
        {
            _tracker.Report(0f, false, true);
            _tracker.Report(10f, false, true);

            Assert.AreEqual(InputMode.Controllers, _tracker.Current);
            CollectionAssert.IsEmpty(_changes);
        }

        [Test]
        public void Changing_mode_is_announced_once_and_repeating_the_same_report_is_silent()
        {
            _tracker.Report(0f, false, false);
            _tracker.Report(LostAfter, false, false);
            _tracker.Report(LostAfter + 0.1f, false, false);
            _tracker.Report(2f, true, false);
            _tracker.Report(2f + RegainAfter, true, false);

            CollectionAssert.AreEqual(new[] { InputMode.Hands, InputMode.Controllers }, _changes);
        }
    }
}

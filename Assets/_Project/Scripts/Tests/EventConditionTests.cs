using NUnit.Framework;
using PingPong.Tutorial;

namespace PingPong.Tests
{
    public class EventConditionTests
    {
        [Test]
        public void Raising_the_event_while_armed_satisfies_the_condition()
        {
            var signal = new TestSignal();
            var satisfied = 0;
            signal.Condition.Satisfied += () => satisfied++;
            signal.Condition.Arm();

            signal.Raise();

            Assert.AreEqual(1, satisfied);
        }

        [Test]
        public void Raising_the_event_before_arming_or_after_disarming_does_nothing()
        {
            var signal = new TestSignal();
            var satisfied = 0;
            signal.Condition.Satisfied += () => satisfied++;

            signal.Raise();
            signal.Condition.Arm();
            signal.Condition.Disarm();
            signal.Raise();

            Assert.AreEqual(0, satisfied);
            Assert.AreEqual(0, signal.ListenerCount);
        }

        [Test]
        public void Arming_twice_never_stacks_listeners()
        {
            var signal = new TestSignal();

            signal.Condition.Arm();
            signal.Condition.Arm();

            Assert.AreEqual(1, signal.ListenerCount);
        }

        [Test]
        public void A_condition_that_is_already_true_is_satisfied_while_arming()
        {
            var satisfied = 0;
            var condition = new EventCondition(satisfy => () => { }, () => true);
            condition.Satisfied += () => satisfied++;

            condition.Arm();

            Assert.AreEqual(1, satisfied);
        }
    }
}

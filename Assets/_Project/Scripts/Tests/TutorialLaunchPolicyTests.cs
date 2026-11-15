using NUnit.Framework;
using PingPong.Tutorial;
using UnityEngine;

namespace PingPong.Tests
{
    public class TutorialLaunchPolicyTests
    {
        private InMemoryTutorialCompletionStore _store;
        private TutorialLaunchPolicy _policy;

        [SetUp]
        public void SetUp()
        {
            _store = new InMemoryTutorialCompletionStore();
            _policy = new TutorialLaunchPolicy(_store);
        }

        [Test]
        public void A_first_launch_plays_the_tutorial()
        {
            Assert.IsTrue(_policy.ShouldPlayOnLaunch(true));
        }

        [Test]
        public void A_launch_after_completion_does_not_play_the_tutorial()
        {
            _policy.RecordCompletion();

            Assert.IsFalse(_policy.ShouldPlayOnLaunch(true));
        }

        [Test]
        public void A_launch_with_auto_play_disabled_does_not_play_the_tutorial()
        {
            Assert.IsFalse(_policy.ShouldPlayOnLaunch(false));
        }

        [Test]
        public void Recording_completion_marks_the_store_completed()
        {
            _policy.RecordCompletion();

            Assert.IsTrue(_store.IsCompleted);
        }

        [Test]
        public void A_new_policy_over_the_same_store_remembers_completion()
        {
            _policy.RecordCompletion();

            Assert.IsFalse(new TutorialLaunchPolicy(_store).ShouldPlayOnLaunch(true));
        }
    }

    public class PlayerPrefsTutorialCompletionStoreTests
    {
        [SetUp]
        public void SetUp() => PlayerPrefs.DeleteKey(PlayerPrefsTutorialCompletionStore.Key);

        [TearDown]
        public void TearDown() => PlayerPrefs.DeleteKey(PlayerPrefsTutorialCompletionStore.Key);

        [Test]
        public void A_fresh_store_reports_the_tutorial_as_not_completed()
        {
            Assert.IsFalse(new PlayerPrefsTutorialCompletionStore().IsCompleted);
        }

        [Test]
        public void A_completion_marked_in_one_store_is_visible_to_another_store()
        {
            new PlayerPrefsTutorialCompletionStore().MarkCompleted();

            Assert.IsTrue(new PlayerPrefsTutorialCompletionStore().IsCompleted);
        }
    }
}

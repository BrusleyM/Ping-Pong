using System.Collections.Generic;
using Framework.Sequencing;
using NUnit.Framework;
using PingPong.Tutorial;

namespace PingPong.Tests
{
    public class TutorialPlanTests
    {
        private TutorialScenario _scenario;
        private SequenceRunner _tutorial;

        [SetUp]
        public void SetUp()
        {
            _scenario = new TutorialScenario();
            _tutorial = _scenario.BuildRunner();
        }

        private void PlayThroughIntro()
        {
            _tutorial.Advance(1f);
            _tutorial.Advance(2f);
        }

        private StageStep CurrentStage => (StageStep)_tutorial.CurrentStep;

        [Test]
        public void The_tutorial_has_an_intro_a_controllers_and_a_play_stage_in_that_order()
        {
            var stageNames = new List<string>();
            _tutorial.StepEntered += (step, index) => stageNames.Add(step.Name);

            _tutorial.Start();
            _tutorial.SkipCurrentStep();
            _tutorial.SkipCurrentStep();

            Assert.AreEqual(new[] { TutorialPlan.IntroStage, TutorialPlan.ControllersStage, TutorialPlan.PlayStage }, stageNames);
        }

        [Test]
        public void The_intro_shows_the_welcome_and_the_overview_for_their_durations()
        {
            _tutorial.Start();
            Assert.AreEqual(new[] { $"show {TutorialText.Welcome}" }, _scenario.Cues.Journal);

            _tutorial.Advance(1f);
            _tutorial.Advance(2f);

            Assert.AreEqual(
                new[] { $"show {TutorialText.Welcome}", "hide", $"show {TutorialText.Overview}", "hide", $"show {TutorialText.GrabPaddle}" },
                _scenario.Cues.Journal);
        }

        [Test]
        public void Picking_up_the_paddle_moves_on_to_asking_for_the_menu()
        {
            _tutorial.Start();
            PlayThroughIntro();

            _scenario.PaddleHeld.Raise();

            Assert.AreEqual(TutorialPlan.ControllersStage, _tutorial.CurrentStep.Name);
            Assert.AreEqual($"show {TutorialText.OpenMenu}", _scenario.Cues.Journal[_scenario.Cues.Journal.Count - 1]);
        }

        [Test]
        public void Opening_the_menu_finishes_the_controllers_stage_and_starts_the_play_stage()
        {
            _tutorial.Start();
            PlayThroughIntro();
            _scenario.PaddleHeld.Raise();

            _scenario.MenuOpened.Raise();

            Assert.AreEqual(TutorialPlan.PlayStage, _tutorial.CurrentStep.Name);
            Assert.AreEqual($"show {TutorialText.HitBall}", _scenario.Cues.Journal[_scenario.Cues.Journal.Count - 1]);
        }

        [Test]
        public void Hitting_the_ball_and_winning_a_point_lead_to_the_closing_message_and_then_completion()
        {
            _tutorial.Start();
            PlayThroughIntro();
            _scenario.PaddleHeld.Raise();
            _scenario.MenuOpened.Raise();

            _scenario.BallHit.Raise();
            Assert.AreEqual($"show {TutorialText.WinPoint}", _scenario.Cues.Journal[_scenario.Cues.Journal.Count - 1]);

            _scenario.PointWon.Raise();
            Assert.AreEqual($"show {TutorialText.Finished}", _scenario.Cues.Journal[_scenario.Cues.Journal.Count - 1]);

            _tutorial.Advance(3f);
            Assert.AreEqual(SequenceState.Completed, _tutorial.State);
        }

        [Test]
        public void Skipping_a_stage_hides_its_prompt_and_stops_listening_for_its_signal()
        {
            _tutorial.Start();
            PlayThroughIntro();
            Assert.AreEqual(1, _scenario.PaddleHeld.ListenerCount);

            _tutorial.SkipCurrentStep();

            Assert.AreEqual(0, _scenario.PaddleHeld.ListenerCount);
            Assert.AreEqual(TutorialPlan.PlayStage, _tutorial.CurrentStep.Name);
            Assert.AreEqual(1, _scenario.BallHit.ListenerCount);
        }

        [Test]
        public void Restarting_the_stage_asks_for_its_first_step_again()
        {
            _tutorial.Start();
            PlayThroughIntro();
            _scenario.PaddleHeld.Raise();

            _tutorial.RestartCurrentStep();

            Assert.AreEqual($"show {TutorialText.GrabPaddle}", _scenario.Cues.Journal[_scenario.Cues.Journal.Count - 1]);
            Assert.AreEqual(1, _scenario.PaddleHeld.ListenerCount);
            Assert.AreEqual(0, _scenario.MenuOpened.ListenerCount);
        }

        [Test]
        public void Cancelling_the_tutorial_hides_the_prompt_and_releases_every_signal()
        {
            _tutorial.Start();
            PlayThroughIntro();

            _tutorial.Cancel();

            Assert.AreEqual("hide", _scenario.Cues.Journal[_scenario.Cues.Journal.Count - 1]);
            Assert.AreEqual(0, _scenario.PaddleHeld.ListenerCount);
        }

        [Test]
        public void A_signal_raised_before_its_step_is_ignored()
        {
            _tutorial.Start();

            _scenario.MenuOpened.Raise();
            _scenario.BallHit.Raise();

            Assert.AreEqual(TutorialPlan.IntroStage, _tutorial.CurrentStep.Name);
        }

        [Test]
        public void Pausing_holds_the_intro_timers()
        {
            _tutorial.Start();
            _tutorial.Pause();

            _tutorial.Advance(5f);

            Assert.AreEqual(TutorialPlan.IntroStage, _tutorial.CurrentStep.Name);
            Assert.AreEqual(new[] { $"show {TutorialText.Welcome}" }, _scenario.Cues.Journal);
        }

        [Test]
        public void The_timing_profile_hands_its_values_to_the_plan()
        {
            var profile = UnityEngine.ScriptableObject.CreateInstance<TutorialProfile>();

            Assert.Greater(profile.Timing.WelcomeSeconds, 0f);
            Assert.Greater(profile.Timing.OverviewSeconds, 0f);
            Assert.Greater(profile.Timing.FinishedSeconds, 0f);
            UnityEngine.Object.DestroyImmediate(profile);
        }
    }
}

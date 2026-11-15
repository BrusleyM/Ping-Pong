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
            Assert.AreEqual($"show {TutorialText.StartGame}", _scenario.Cues.Journal[_scenario.Cues.Journal.Count - 1]);
        }

        [Test]
        public void Starting_a_match_asks_the_player_to_hit_the_ball()
        {
            _tutorial.Start();
            PlayThroughIntro();
            _scenario.PaddleHeld.Raise();
            _scenario.MenuOpened.Raise();

            _scenario.MatchRunning.Raise();

            Assert.AreEqual($"show {TutorialText.HitBall}", _scenario.Cues.Journal[_scenario.Cues.Journal.Count - 1]);
        }

        [Test]
        public void A_paddle_already_held_when_the_controllers_stage_starts_skips_straight_to_asking_for_the_menu()
        {
            var scenario = new TutorialScenario();
            scenario.PaddleHeld.AlreadySatisfied = true;
            var tutorial = scenario.BuildRunner();
            tutorial.Start();
            tutorial.Advance(1f);
            tutorial.Advance(2f);

            Assert.AreEqual(TutorialPlan.ControllersStage, tutorial.CurrentStep.Name);
            Assert.AreEqual($"show {TutorialText.OpenMenu}", scenario.Cues.Journal[scenario.Cues.Journal.Count - 1]);
            Assert.AreEqual(0, scenario.PaddleHeld.ListenerCount);
            Assert.AreEqual(1, scenario.MenuOpened.ListenerCount);
        }

        [Test]
        public void A_match_already_running_when_the_play_stage_starts_skips_straight_to_asking_for_a_hit()
        {
            var scenario = new TutorialScenario();
            scenario.MatchRunning.AlreadySatisfied = true;
            var tutorial = scenario.BuildRunner();
            tutorial.Start();
            tutorial.Advance(1f);
            tutorial.Advance(2f);
            scenario.PaddleHeld.Raise();
            scenario.MenuOpened.Raise();

            Assert.AreEqual($"show {TutorialText.HitBall}", scenario.Cues.Journal[scenario.Cues.Journal.Count - 1]);
        }

        [Test]
        public void Hitting_the_ball_and_winning_a_point_lead_to_the_closing_message_and_then_completion()
        {
            _tutorial.Start();
            PlayThroughIntro();
            _scenario.PaddleHeld.Raise();
            _scenario.MenuOpened.Raise();
            _scenario.MatchRunning.Raise();

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
            Assert.AreEqual(1, _scenario.MatchRunning.ListenerCount);
            Assert.AreEqual(new[] { "hide", $"show {TutorialText.StartGame}" }, _scenario.Cues.Journal.GetRange(_scenario.Cues.Journal.Count - 2, 2));
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
        public void The_default_timing_profile_gives_the_welcome_2_5_seconds_and_the_overview_and_closing_3_seconds_each()
        {
            var profile = UnityEngine.ScriptableObject.CreateInstance<TutorialProfile>();

            Assert.AreEqual(2.5f, profile.Timing.WelcomeSeconds);
            Assert.AreEqual(3f, profile.Timing.OverviewSeconds);
            Assert.AreEqual(3f, profile.Timing.FinishedSeconds);
            UnityEngine.Object.DestroyImmediate(profile);
        }
    }
}

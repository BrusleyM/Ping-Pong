using Framework.Sequencing;
using PingPong.Gameplay;

namespace PingPong.Tutorial
{
    public static class TutorialPlan
    {
        public const string Name = "Tutorial";
        public const string IntroStage = "Intro";
        public const string ControllersStage = "Controllers";
        public const string PlayStage = "Play";

        public static SequenceRunner Build(TutorialSignals signals, IInputModeSource modes, ITutorialCues cues, TutorialTiming timing)
        {
            return Sequence.Named(Name)
                .Stage(IntroStage, intro => intro
                    .Then(PromptStep.ForSeconds(PromptText.Same(TutorialText.Welcome), modes, cues, timing.WelcomeSeconds))
                    .Then(PromptStep.ForSeconds(PromptText.Same(TutorialText.Overview), modes, cues, timing.OverviewSeconds)))
                .Stage(ControllersStage, controllers => controllers
                    .Then(PromptStep.UntilSatisfied(PromptText.Differing(TutorialText.GrabPaddle, TutorialText.GrabPaddleByHand), modes, cues, signals.PaddleHeld))
                    .Then(PromptStep.UntilSatisfied(PromptText.Differing(TutorialText.OpenMenu, TutorialText.OpenMenuByHand), modes, cues, signals.MenuOpened)))
                .Stage(PlayStage, play => play
                    .Then(PromptStep.UntilSatisfied(PromptText.Differing(TutorialText.StartGame, TutorialText.StartGameByHand), modes, cues, signals.MatchRunning))
                    .Then(PromptStep.UntilSatisfied(PromptText.Same(TutorialText.HitBall), modes, cues, signals.BallHit))
                    .Then(PromptStep.UntilSatisfied(PromptText.Same(TutorialText.WinPoint), modes, cues, signals.PointWon))
                    .Then(PromptStep.ForSeconds(PromptText.Same(TutorialText.Finished), modes, cues, timing.FinishedSeconds)))
                .Build();
        }
    }
}

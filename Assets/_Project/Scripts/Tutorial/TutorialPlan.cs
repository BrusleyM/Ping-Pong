using Framework.Sequencing;

namespace PingPong.Tutorial
{
    public static class TutorialPlan
    {
        public const string Name = "Tutorial";
        public const string IntroStage = "Intro";
        public const string ControllersStage = "Controllers";
        public const string PlayStage = "Play";

        public static SequenceRunner Build(TutorialSignals signals, ITutorialCues cues, TutorialTiming timing)
        {
            return Sequence.Named(Name)
                .Stage(IntroStage, intro => intro
                    .Then(PromptStep.ForSeconds(TutorialText.Welcome, cues, timing.WelcomeSeconds))
                    .Then(PromptStep.ForSeconds(TutorialText.Overview, cues, timing.OverviewSeconds)))
                .Stage(ControllersStage, controllers => controllers
                    .Then(PromptStep.UntilSatisfied(TutorialText.GrabPaddle, cues, signals.PaddleHeld))
                    .Then(PromptStep.UntilSatisfied(TutorialText.OpenMenu, cues, signals.MenuOpened)))
                .Stage(PlayStage, play => play
                    .Then(PromptStep.UntilSatisfied(TutorialText.StartGame, cues, signals.MatchRunning))
                    .Then(PromptStep.UntilSatisfied(TutorialText.HitBall, cues, signals.BallHit))
                    .Then(PromptStep.UntilSatisfied(TutorialText.WinPoint, cues, signals.PointWon))
                    .Then(PromptStep.ForSeconds(TutorialText.Finished, cues, timing.FinishedSeconds)))
                .Build();
        }
    }
}

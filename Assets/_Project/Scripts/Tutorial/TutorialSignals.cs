using Framework.Sequencing;

namespace PingPong.Tutorial
{
    public sealed class TutorialSignals
    {
        public TutorialSignals(IStepCondition paddleHeld, IStepCondition menuOpened, IStepCondition matchRunning, IStepCondition ballHit, IStepCondition pointWon)
        {
            PaddleHeld = paddleHeld;
            MenuOpened = menuOpened;
            MatchRunning = matchRunning;
            BallHit = ballHit;
            PointWon = pointWon;
        }

        public IStepCondition PaddleHeld { get; }
        public IStepCondition MenuOpened { get; }
        public IStepCondition MatchRunning { get; }
        public IStepCondition BallHit { get; }
        public IStepCondition PointWon { get; }
    }
}

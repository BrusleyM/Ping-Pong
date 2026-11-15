namespace PingPong.Tutorial
{
    public readonly struct TutorialTiming
    {
        public TutorialTiming(float welcomeSeconds, float overviewSeconds, float finishedSeconds)
        {
            WelcomeSeconds = welcomeSeconds;
            OverviewSeconds = overviewSeconds;
            FinishedSeconds = finishedSeconds;
        }

        public float WelcomeSeconds { get; }
        public float OverviewSeconds { get; }
        public float FinishedSeconds { get; }
    }
}

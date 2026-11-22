namespace PingPong.Tutorial
{
    public sealed class TutorialLaunchPolicy
    {
        private readonly ITutorialCompletionStore _store;

        public TutorialLaunchPolicy(ITutorialCompletionStore store)
        {
            _store = store;
        }

        public bool ShouldPlayOnLaunch(bool playOnStart) => playOnStart && !_store.IsCompleted;

        public void RecordCompletion() => _store.MarkCompleted();
    }
}

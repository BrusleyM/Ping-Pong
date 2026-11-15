using PingPong.Tutorial;

namespace PingPong.Tests
{
    internal sealed class InMemoryTutorialCompletionStore : ITutorialCompletionStore
    {
        public bool IsCompleted { get; private set; }

        public void MarkCompleted() => IsCompleted = true;
    }
}

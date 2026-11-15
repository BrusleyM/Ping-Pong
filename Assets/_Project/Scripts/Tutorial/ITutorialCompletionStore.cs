namespace PingPong.Tutorial
{
    public interface ITutorialCompletionStore
    {
        bool IsCompleted { get; }

        void MarkCompleted();
    }
}

using PingPong.Gameplay;

namespace PingPong.Tests
{
    public sealed class InMemoryStore<T> : IKeyedStore<T> where T : class
    {
        private T _value;

        public int Saves { get; private set; }

        public T Load() => _value;

        public void Save(T value)
        {
            _value = value;
            Saves++;
        }

        public void Clear() => _value = null;
    }
}

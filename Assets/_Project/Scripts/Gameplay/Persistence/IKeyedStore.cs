namespace PingPong.Gameplay
{
    public interface IKeyedStore<T> where T : class
    {
        T Load();

        void Save(T value);

        void Clear();
    }
}

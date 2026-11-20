namespace PingPong.Gameplay
{
    public interface ISceneLoader
    {
        bool IsLoading { get; }

        void Load(string sceneName);
    }
}

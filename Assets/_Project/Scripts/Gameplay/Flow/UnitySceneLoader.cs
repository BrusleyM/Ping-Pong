using UnityEngine;
using UnityEngine.SceneManagement;

namespace PingPong.Gameplay
{
    public sealed class UnitySceneLoader : MonoBehaviour, ISceneLoader
    {
        public bool IsLoading { get; private set; }

        public void Load(string sceneName)
        {
            if (IsLoading)
            {
                return;
            }

            IsLoading = true;
            SceneManager.LoadSceneAsync(sceneName);
        }
    }
}

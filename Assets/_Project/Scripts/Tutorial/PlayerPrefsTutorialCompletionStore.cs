using UnityEngine;

namespace PingPong.Tutorial
{
    public sealed class PlayerPrefsTutorialCompletionStore : ITutorialCompletionStore
    {
        public const string Key = "PingPong.Tutorial.Completed";

        public bool IsCompleted => PlayerPrefs.GetInt(Key, 0) == 1;

        public void MarkCompleted()
        {
            PlayerPrefs.SetInt(Key, 1);
            PlayerPrefs.Save();
        }
    }
}

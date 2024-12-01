using UnityEngine;

namespace PingPong.Managers
{
    public class GameManager : MonoBehaviour
    {
        private static GameManager _instance;
        public static GameManager Instance => _instance;
        [SerializeField]
        AudioManager _audioPlayer;
        public AudioManager AudioPlayer => _audioPlayer;
        void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
                Destroy(gameObject);

            if (_audioPlayer == null)
            {
                _audioPlayer = new AudioManager();
            }
        }
    }
}

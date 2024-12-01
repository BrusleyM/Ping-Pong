using UnityEngine;
using PingPong.Managers;
namespace PingPong.GameObjects
{
    public class PingPongBall : MonoBehaviour
    {
        #region AudioData
        [SerializeField]
        AudioSource _source;
        [SerializeField]
        AudioClip _sfx;
        #endregion
        void OnCollisionEnter(Collision collision)
        {
            GameManager.Instance.AudioPlayer.PlaySfx(_source, _sfx);
        }
    }
}
using System;
using UnityEngine;

namespace PingPong.Managers
{
    [Serializable]
    public class AudioManager
    {
        public void PlaySfx(AudioSource source, AudioClip clip)
        {
            if (source.isPlaying)
            {
                source.Stop();
            }
            source.PlayOneShot(clip);
        }
    }
}
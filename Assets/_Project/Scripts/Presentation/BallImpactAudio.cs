using PingPong.Gameplay;
using UnityEngine;

namespace PingPong.Presentation
{
    /// <summary>The ball's impact sounds: per-kind clips, speed-scaled volume and pitch variation.</summary>
    [RequireComponent(typeof(AudioSource))]
    public sealed class BallImpactAudio : MonoBehaviour
    {
        [SerializeField] PingPongBall _ball;
        [SerializeField] AudioClip _table;
        [Tooltip("Falls back to the table clip if empty.")]
        [SerializeField] AudioClip _paddle;
        [Tooltip("Falls back to the table clip if empty.")]
        [SerializeField] AudioClip _net;
        [Tooltip("Floor, walls, table edge. Falls back to the table clip if empty.")]
        [SerializeField] AudioClip _other;
        [Tooltip("Impact speed (m/s) that plays at full volume.")]
        [SerializeField] float _fullVolumeSpeed = 8f;
        [SerializeField, Range(0f, 0.2f)] float _pitchVariation = 0.05f;
        [Tooltip("Impacts slower than this are silent (rolling, resting).")]
        [SerializeField] float _minSpeed = 0.3f;

        AudioSource _source;

        void Awake()
        {
            _source = GetComponent<AudioSource>();
            _source.playOnAwake = false;
        }

        void OnValidate() => Validation.Require(_ball, "the Ball", this);
        void OnEnable() => _ball.Impact += Play;
        void OnDisable() => _ball.Impact -= Play;

        void Play(BallImpact impact)
        {
            if (impact.Speed < _minSpeed) return;
            AudioClip clip = Clip(impact.Kind);
            if (clip == null) clip = _table; // Unity null check: '??' would miss destroyed/missing assets
            if (clip == null) return;
            _source.pitch = 1f + Random.Range(-_pitchVariation, _pitchVariation);
            _source.PlayOneShot(clip, Mathf.Clamp01(impact.Speed / _fullVolumeSpeed));
        }

        AudioClip Clip(ImpactKind kind) => kind switch
        {
            ImpactKind.Table => _table,
            ImpactKind.Paddle => _paddle,
            ImpactKind.Net => _net,
            _ => _other,
        };
    }
}

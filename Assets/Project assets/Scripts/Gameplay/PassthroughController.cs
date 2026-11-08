using UnityEngine;

namespace PingPong.Gameplay
{
    /// <summary>
    /// Switches between the virtual room and Quest passthrough (your real room behind the table).
    /// Needs Passthrough enabled in the OVRManager (Quest Features > Insight Passthrough).
    /// </summary>
    public class PassthroughController : MonoBehaviour
    {
        [SerializeField] OVRPassthroughLayer _layer;
        [Tooltip("Cameras whose background must be transparent in passthrough (the centre eye camera).")]
        [SerializeField] Camera[] _cameras;
        [Tooltip("Virtual environment objects hidden in passthrough (room, walls, decor).")]
        [SerializeField] GameObject[] _hideInPassthrough;
        [SerializeField] bool _startEnabled;

        public bool IsOn { get; private set; }

        CameraClearFlags[] _flags;
        Color[] _colors;
        Material _skybox;

        void Awake()
        {
            _flags = new CameraClearFlags[_cameras.Length];
            _colors = new Color[_cameras.Length];
            for (int i = 0; i < _cameras.Length; i++)
            {
                _flags[i] = _cameras[i].clearFlags;
                _colors[i] = _cameras[i].backgroundColor;
            }
            _skybox = RenderSettings.skybox;
        }

        void Start() => Set(_startEnabled);

        public void Toggle() => Set(!IsOn);

        public void Set(bool on)
        {
            IsOn = on;
            if (OVRManager.instance != null) OVRManager.instance.isInsightPassthroughEnabled = on;
            if (_layer != null) _layer.enabled = on;
            for (int i = 0; i < _cameras.Length; i++)
            {
                _cameras[i].clearFlags = on ? CameraClearFlags.SolidColor : _flags[i];
                _cameras[i].backgroundColor = on ? Color.clear : _colors[i];
            }
            RenderSettings.skybox = on ? null : _skybox;
            foreach (var go in _hideInPassthrough) if (go != null) go.SetActive(!on);
        }
    }
}

using System.Collections.Generic;
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
        // Visuals under the hide roots that were enabled at start; only these are hidden and restored,
        // so objects disabled on purpose in the scene stay off.
        readonly List<Renderer> _renderers = new List<Renderer>();
        readonly List<Light> _lights = new List<Light>();

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
            foreach (var go in _hideInPassthrough)
            {
                if (go == null) continue;
                foreach (var r in go.GetComponentsInChildren<Renderer>(true)) if (r.enabled) _renderers.Add(r);
                foreach (var l in go.GetComponentsInChildren<Light>(true)) if (l.enabled) _lights.Add(l);
            }
        }

        void Start() => Set(_startEnabled);

        public void Toggle() => Set(!IsOn);

        public void Set(bool on)
        {
            IsOn = on;
            // Insight passthrough stays enabled on the OVRManager; the layer is shown or hidden,
            // which switches instantly without restarting the passthrough camera feed.
            if (_layer != null) _layer.hidden = !on;
            for (int i = 0; i < _cameras.Length; i++)
            {
                _cameras[i].clearFlags = on ? CameraClearFlags.SolidColor : _flags[i];
                _cameras[i].backgroundColor = on ? Color.clear : _colors[i];
            }
            RenderSettings.skybox = on ? null : _skybox;
            // Hide only the visuals: floor colliders keep working so dead balls are still detected.
            foreach (var r in _renderers) if (r != null) r.enabled = !on;
            foreach (var l in _lights) if (l != null) l.enabled = !on;
        }
    }
}

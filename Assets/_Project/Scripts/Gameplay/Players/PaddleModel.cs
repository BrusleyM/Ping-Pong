using UnityEngine;

namespace PingPong.Gameplay
{
    /// <summary>
    /// A paddle's geometry: where the blade is and how big it is. Lives on the PaddleModel prefab, which both
    /// the player's paddle (a variant that adds grabbing) and the AI's paddle use.
    /// </summary>
    public sealed class PaddleModel : MonoBehaviour
    {
        [Tooltip("Centre of the blade. Defaults to this transform.")]
        [SerializeField] Transform _blade;
        [Tooltip("Blade face normal in the blade transform's local space.")]
        [SerializeField] Vector3 _faceNormalLocal = Vector3.forward;
        [Tooltip("Blade radius, metres. Measured from the mesh by the scene setup.")]
        [SerializeField] float _bladeRadius = 0.08f;
        [Tooltip("Blade half-thickness, metres.")]
        [SerializeField] float _bladeHalfThickness = 0.006f;

        public Transform Blade => _blade != null ? _blade : transform;
        public Vector3 FaceNormalLocal => _faceNormalLocal.normalized;
        public float BladeRadius => _bladeRadius;
        public float BladeHalfThickness => _bladeHalfThickness;
    }
}

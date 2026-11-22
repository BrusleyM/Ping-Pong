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
        [Tooltip("Direction from the handle to the blade, in the blade transform's local space (in the blade's plane). " +
                 "Measured from the mesh by the scene setup; zero means not measured.")]
        [SerializeField] Vector3 _handleToBladeLocal;
        [Tooltip("Blade radius, metres. Measured from the mesh by the scene setup.")]
        [SerializeField] float _bladeRadius = 0.08f;
        [Tooltip("Blade half-thickness, metres.")]
        [SerializeField] float _bladeHalfThickness = 0.006f;

        public Transform Blade => _blade != null ? _blade : transform;
        public Vector3 FaceNormalLocal => _faceNormalLocal.normalized;
        public Vector3 HandleToBladeLocal => _handleToBladeLocal.sqrMagnitude > 1e-6f ? _handleToBladeLocal.normalized : Vector3.up;
        public float BladeRadius => _bladeRadius;
        public float BladeHalfThickness => _bladeHalfThickness;

        /// <summary>
        /// World pose for this paddle's root that puts the blade centre at 'bladeCentre', facing 'faceNormal',
        /// with the blade above the handle along 'up'.
        /// </summary>
        public Pose RootPoseFor(Vector3 bladeCentre, Vector3 faceNormal, Vector3 up)
        {
            Quaternion rotation = RootRotationFor(faceNormal, up);
            Vector3 bladeOffset = Quaternion.Inverse(transform.rotation) * (Blade.position - transform.position);
            return new Pose(bladeCentre - rotation * bladeOffset, rotation);
        }

        /// <summary>
        /// World rotation for this paddle's root that turns the blade face towards 'faceNormal' with the
        /// blade above the handle along 'up' (projected into the blade's plane). Works for any model axes.
        /// </summary>
        public Quaternion RootRotationFor(Vector3 faceNormal, Vector3 up)
        {
            Quaternion bladeWanted = Quaternion.LookRotation(faceNormal, up)
                                     * Quaternion.Inverse(Quaternion.LookRotation(FaceNormalLocal, HandleToBladeLocal));
            Quaternion bladeInRoot = Quaternion.Inverse(transform.rotation) * Blade.rotation;
            return bladeWanted * Quaternion.Inverse(bladeInRoot);
        }
    }
}

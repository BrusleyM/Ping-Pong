#if UNITY_EDITOR
using UnityEngine;

[RequireComponent(typeof(SphereCollider), typeof(Rigidbody))]
public class AdjustablePingPongBall : MonoBehaviour
{
    [Header("Ball Settings")]
    [Tooltip("The size (diameter) of the ping pong ball in meters.")]
    [Range(0.01f, 1f)]
    [SerializeField]
    private float ballSize = 0.04f;

    [Tooltip("Bounciness of the ball (0 to 1).")]
    [Range(0f, 1f)]
    [SerializeField]
    private float bounciness = 0.8f;

    [Tooltip("Drag in the air.")]
    [Range(0f, 1f)]
    [SerializeField]
    private float drag = 0.1f;

    [Tooltip("Angular drag for rotation.")]
    [Range(0f, 0.5f)]
    [SerializeField]
    private float angularDrag = 0.05f;

    [Tooltip("Mass of the ball in kilograms.")]
    [SerializeField]
    private float massMultiplier = 0.0027f;

    private Rigidbody rb;
    private SphereCollider sphereCollider;

    void OnValidate()
    {
        rb = GetComponent<Rigidbody>();
        sphereCollider = GetComponent<SphereCollider>();
        UpdateBallProperties();
    }

    private void UpdateBallProperties()
    {
        transform.localScale = Vector3.one * ballSize;

        //sphereCollider.radius = ballSize / 2;
        float volume = (4f / 3f) * Mathf.PI * Mathf.Pow(ballSize / 2f, 3f);
        rb.mass = volume * massMultiplier;

        rb.drag = drag;
        rb.angularDrag = angularDrag;

        PhysicMaterial physicMaterial = sphereCollider.sharedMaterial;
        if (physicMaterial == null)
        {
            physicMaterial = new PhysicMaterial();
            sphereCollider.sharedMaterial = physicMaterial;
        }
        physicMaterial.bounciness = bounciness;
        physicMaterial.bounceCombine = PhysicMaterialCombine.Maximum; // Ensure maximum bounce effect
    }
}
#endif
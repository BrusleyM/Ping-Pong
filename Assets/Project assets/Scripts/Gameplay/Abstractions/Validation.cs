using UnityEngine;

namespace PingPong.Gameplay
{
    /// <summary>Shared inspector checks for OnValidate, so every component reports wiring problems the same way.</summary>
    public static class Validation
    {
        /// <summary>Warns if a required reference (profile, component) is not assigned.</summary>
        public static void Require(Object value, string label, Object context)
        {
            if (value == null) Debug.LogWarning($"{context.name}: assign {label}.", context);
        }

        /// <summary>Errors if a seam holds a component that doesn't implement its interface.</summary>
        public static void Seam<T>(in Seam<T> seam, string field, Object context) where T : class
        {
            string error = seam.Validate(field);
            if (error != null) Debug.LogError($"{context.name}: {error}", context);
        }
    }
}

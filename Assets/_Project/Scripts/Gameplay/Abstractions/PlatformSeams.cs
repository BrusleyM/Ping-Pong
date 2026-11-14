using UnityEngine;

namespace PingPong.Gameplay
{
    public enum Hand { None, Left, Right }

    /// <summary>Player commands, polled once per frame. Implementations map devices to these actions.</summary>
    public interface IPlayerCommands
    {
        /// <summary>The grab button on that hand went down this frame.</summary>
        bool GrabPressed(Hand hand);
        /// <summary>That hand's controller is tracked, or there is no controller to judge by (keyboard). False once hand tracking takes over.</summary>
        bool ControllerActive(Hand hand);
        bool MenuPressed { get; }
    }

    /// <summary>Vibration on one controller.</summary>
    public interface IHaptics
    {
        /// <param name="amplitude">0-1.</param>
        /// <param name="seconds">Pulse length.</param>
        void Pulse(float amplitude, float seconds);
    }

    /// <summary>Switches between the virtual room and the headset's camera view of the real room.</summary>
    public interface IPassthrough
    {
        bool IsOn { get; }
        void Set(bool on);
    }

    /// <summary>Decides which collisions end a rally (floor, walls) and which are ignored (hands, paddles).</summary>
    public interface IBallContactFilter
    {
        bool IsDeadBallSurface(Collider collider);
    }

    public static class SeamExtensions
    {
        public static void Toggle(this IPassthrough passthrough) => passthrough.Set(!passthrough.IsOn);
    }

    /// <summary>
    /// A serialized reference to a component that implements interface T. Unity can't serialize
    /// interface fields, so the component is stored and resolved to T on use.
    /// </summary>
    [System.Serializable]
    public struct Seam<T> where T : class
    {
        [SerializeField] Component _component;

        public Seam(Component component) => _component = component;

        /// <summary>The implementation, or null if unassigned, destroyed or of the wrong type.</summary>
        public T Value => _component != null ? _component as T : null;

        public bool IsAssigned => Value != null;

        /// <summary>For OnValidate: a message if a component is assigned that doesn't implement T.</summary>
        public string Validate(string fieldName) =>
            _component != null && !(_component is T) ? $"{fieldName}: {_component.GetType().Name} does not implement {typeof(T).Name}." : null;
    }
}

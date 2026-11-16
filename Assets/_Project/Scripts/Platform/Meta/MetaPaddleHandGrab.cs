using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using PingPong.Gameplay;
using UnityEngine;

namespace PingPong.Platform.Meta
{
    public sealed class MetaPaddleHandGrab : MonoBehaviour
    {
        private const Hand FallbackHand = Hand.Right;

        [SerializeField] private Grabbable _grabbable;
        [SerializeField] private PaddleGrip _grip;

        private readonly GrabPoints _grabPoints = new GrabPoints();

        private void OnValidate()
        {
            Validation.Require(_grabbable, "the paddle's Grabbable", this);
            Validation.Require(_grip, "the Paddle Grip", this);
        }

        private void OnEnable() => _grabbable.WhenPointerEventRaised += OnPointerEvent;

        private void OnDisable() => _grabbable.WhenPointerEventRaised -= OnPointerEvent;

        private void OnPointerEvent(PointerEvent pointerEvent)
        {
            switch (pointerEvent.Type)
            {
                case PointerEventType.Select:
                    if (_grabPoints.Begin(pointerEvent.Identifier))
                    {
                        _grip.HandGrabbed(HandOf(pointerEvent.Data));
                    }

                    break;
                case PointerEventType.Unselect:
                case PointerEventType.Cancel:
                    if (_grabPoints.End(pointerEvent.Identifier))
                    {
                        _grip.HandReleased();
                    }

                    break;
            }
        }

        private static Hand HandOf(object interactor)
        {
            if (interactor is HandGrabInteractor handGrab && handGrab.Hand != null)
            {
                return handGrab.Hand.Handedness == Oculus.Interaction.Input.Handedness.Left ? Hand.Left : Hand.Right;
            }

            return FallbackHand;
        }
    }
}

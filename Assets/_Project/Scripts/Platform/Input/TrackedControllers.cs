using PingPong.Gameplay;
using UnityEngine.InputSystem.XR;

namespace PingPong.Platform.Input
{
    public static class TrackedControllers
    {
        public static bool IsActive(Hand hand)
        {
            XRController controller = hand switch
            {
                Hand.Left => XRController.leftHand,
                Hand.Right => XRController.rightHand,
                _ => null,
            };
            return controller == null || controller.isTracked.isPressed;
        }
    }
}

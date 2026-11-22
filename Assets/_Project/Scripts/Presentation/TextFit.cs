using UnityEngine;

namespace PingPong.Presentation
{
    public static class TextFit
    {
        public static float Size(float baseSize, float preferredWidth, float maxWidth) =>
            preferredWidth > maxWidth && preferredWidth > 0f ? baseSize * maxWidth / preferredWidth : baseSize;
    }
}

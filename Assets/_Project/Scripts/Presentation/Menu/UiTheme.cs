using System;
using UnityEngine;

namespace PingPong.Presentation
{
    [CreateAssetMenu(menuName = "Ping Pong/UI Theme", fileName = "UiTheme")]
    public sealed class UiTheme : ScriptableObject
    {
        [Serializable]
        public struct DifficultyStyle
        {
            public string Name;
            public string Note;
            public Color Colour;
        }

        [Header("Colours")]
        public Color Primary = new Color32(0xFF, 0x3F, 0x88, 0xFF);
        public Color PrimaryDark = new Color32(0xA7, 0x16, 0x57, 0xFF);
        public Color Secondary = new Color32(0x18, 0xBD, 0xEB, 0xFF);
        public Color SecondaryDark = new Color32(0x08, 0x78, 0xAA, 0xFF);
        public Color Accent = new Color32(0xFF, 0xD4, 0x3B, 0xFF);
        public Color AccentDark = new Color32(0xA8, 0x6E, 0x07, 0xFF);
        public Color Success = new Color32(0x49, 0xDD, 0xA7, 0xFF);
        public Color SuccessDark = new Color32(0x16, 0x89, 0x6C, 0xFF);
        public Color Danger = new Color32(0xF2, 0x4F, 0x5E, 0xFF);
        public Color DangerDark = new Color32(0x9D, 0x20, 0x3B, 0xFF);
        public Color Surface = new Color32(0x2D, 0x19, 0x5E, 0xFF);
        public Color SurfaceRaised = new Color32(0x47, 0x26, 0x7C, 0xFF);
        public Color Ink = new Color32(0x23, 0x11, 0x3F, 0xFF);
        public Color TeamRed = new Color32(0xFF, 0x3F, 0x5E, 0xFF);
        public Color TeamBlue = new Color32(0x2E, 0x8B, 0xFF, 0xFF);

        [Header("Button motion")]
        [Min(1f)] public float HoverScale = 1.08f;
        [Range(0.5f, 1f)] public float PressScale = 0.94f;
        [Min(1f)] public float SelectPeak = 1.2f;
        [Min(0.01f)] public float HoverSeconds = 0.12f;
        [Min(0.01f)] public float SelectSeconds = 0.35f;
        [Min(0.01f)] public float ShakeSeconds = 0.35f;
        [Min(0f)] public float ShakeDistance = 14f;

        [Header("Panel motion")]
        [Range(0.3f, 1f)] public float PanelInFrom = 0.8f;
        [Min(0.01f)] public float PanelInSeconds = 0.3f;
        [Range(0.5f, 1f)] public float PanelOutTo = 0.9f;
        [Min(0.01f)] public float PanelOutSeconds = 0.15f;

        [Header("Difficulty")]
        public DifficultyStyle[] Difficulties =
        {
            new DifficultyStyle { Name = "Chill Rally", Note = "Take your time", Colour = new Color32(0x49, 0xDD, 0xA7, 0xFF) },
            new DifficultyStyle { Name = "Party Pace", Note = "A friendly match", Colour = new Color32(0x18, 0xBD, 0xEB, 0xFF) },
            new DifficultyStyle { Name = "Pro Spin", Note = "Quick reactions", Colour = new Color32(0xFF, 0x8A, 0x3D, 0xFF) },
            new DifficultyStyle { Name = "Mania Mode", Note = "No mercy!", Colour = new Color32(0xFF, 0x3F, 0x88, 0xFF) }
        };

        public Color ColourOf(PingPong.Gameplay.TeamColour team) => team == PingPong.Gameplay.TeamColour.Red ? TeamRed : TeamBlue;

        public DifficultyStyle DifficultyAt(int index, string fallbackName)
        {
            if (Difficulties != null && index >= 0 && index < Difficulties.Length)
            {
                return Difficulties[index];
            }

            return new DifficultyStyle { Name = fallbackName, Note = string.Empty, Colour = Primary };
        }
    }
}

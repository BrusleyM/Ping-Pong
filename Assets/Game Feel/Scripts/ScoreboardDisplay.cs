using System.Collections.Generic;
using UnityEngine;

namespace PingPong.GameFeel
{
    /// <summary>
    /// The cube scoreboard: a grid of pixel cubes (built by the stadium generator) that can show any short text.
    /// Text is centred; anything longer than the board is cut off.
    /// </summary>
    public class ScoreboardDisplay : MonoBehaviour
    {
        [Tooltip("How many characters fit on the board.")]
        public int characters = 11;
        [Tooltip("Pixel renderers, ordered by character slot, then row (top to bottom), then column.")]
        public List<Renderer> pixels = new List<Renderer>();
        [Tooltip("Shown when the scene starts.")]
        public string idleText = "PING PONG";
        [Tooltip("The screen behind the pixels (pulsed by MatchPresentation).")]
        public Renderer screen;

        public string CurrentText { get; private set; }

        void Awake()
        {
            SetText(idleText);
        }

        public void SetText(string text)
        {
            text = (text ?? "").ToUpperInvariant();
            if (text.Length > characters) text = text.Substring(0, characters);
            int pad = (characters - text.Length) / 2;
            int perChar = PixelFont.Width * PixelFont.Height;
            if (pixels.Count < characters * perChar) return;

            for (int slot = 0; slot < characters; slot++)
            {
                int ci = slot - pad;
                char ch = ci >= 0 && ci < text.Length ? text[ci] : ' ';
                var glyph = PixelFont.Glyph(ch);
                for (int row = 0; row < PixelFont.Height; row++)
                for (int col = 0; col < PixelFont.Width; col++)
                {
                    var r = pixels[slot * perChar + row * PixelFont.Width + col];
                    if (r != null) r.enabled = glyph[row][col] == '#';
                }
            }
            CurrentText = text;
        }
    }
}

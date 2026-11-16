using PingPong.Gameplay;

namespace PingPong.Tutorial
{
    public readonly struct PromptText
    {
        private readonly string _controllers;
        private readonly string _hands;

        private PromptText(string controllers, string hands)
        {
            _controllers = controllers;
            _hands = hands;
        }

        public static PromptText Same(string text) => new PromptText(text, text);

        public static PromptText Differing(string controllers, string hands) => new PromptText(controllers, hands);

        public string For(InputMode mode) => mode == InputMode.Hands ? _hands : _controllers;
    }
}

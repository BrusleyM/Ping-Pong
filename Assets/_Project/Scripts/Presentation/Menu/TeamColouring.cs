using PingPong.Gameplay;
using UnityEngine;

namespace PingPong.Presentation
{
    public sealed class TeamColouring : MonoBehaviour
    {
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        [SerializeField] private UiTheme _theme;
        [SerializeField] private Renderer _playerHandle;
        [SerializeField] private Renderer _opponentHandle;

        private void Start()
        {
            var team = AppServices.LocalTeam;
            Tint(_playerHandle, _theme.ColourOf(team));
            Tint(_opponentHandle, _theme.ColourOf(TeamColours.Opposite(team)));
        }

        private static void Tint(Renderer renderer, Color colour)
        {
            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            block.SetColor(BaseColor, colour);
            renderer.SetPropertyBlock(block);
        }
    }
}

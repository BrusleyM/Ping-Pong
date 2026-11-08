using PingPong.Core;
using UnityEngine;

namespace PingPong.Gameplay
{
    /// <summary>Floating score above the net, facing the local player. All player-facing text lives here.</summary>
    public sealed class Scoreboard : MonoBehaviour
    {
        [SerializeField] MatchController _match;
        [SerializeField] GameModeController _modes;
        [SerializeField] float _height = 0.55f;
        [SerializeField] float _characterSize = 0.012f;
        [SerializeField] float _messageSeconds = 1.5f;

        const int FontSize = 64;
        const int StatusFontSize = 32;

        TextMesh _text;
        string _message = "";
        float _messageUntil;

        void Start()
        {
            var go = new GameObject("Score Text");
            go.transform.SetParent(transform, false);
            _text = go.AddComponent<TextMesh>();
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _text.font = font;
            go.GetComponent<MeshRenderer>().sharedMaterial = font.material;
            _text.fontSize = FontSize;
            _text.characterSize = _characterSize;
            _text.anchor = TextAnchor.MiddleCenter;
            _text.alignment = TextAlignment.Center;
            _text.color = Color.white;

            TableFrame t = _match.Table.Frame;
            Vector3 viewerLooks = t.Forward * -Local.Sign(); // from the local player's end towards the net
            transform.SetPositionAndRotation(t.SurfaceCenter + Vector3.up * _height, Quaternion.LookRotation(viewerLooks, Vector3.up));

            _match.Changed += Refresh;
            _match.PointWon += OnPointWon;
            _match.GameWon += OnGameWon;
            if (_modes != null) _modes.Changed += Refresh;
            Refresh();
        }

        void OnDestroy()
        {
            if (_match != null) { _match.Changed -= Refresh; _match.PointWon -= OnPointWon; _match.GameWon -= OnGameWon; }
            if (_modes != null) _modes.Changed -= Refresh;
        }

        Side Local => _modes != null ? _modes.LocalPlayer : Side.Near;

        string Name(Side side)
        {
            if (side == Local) return "You";
            return _modes != null && _modes.Mode == GameMode.Solo && _modes.AI != null ? $"AI ({_modes.AI.LevelName})" : "Opponent";
        }

        void OnPointWon(Side winner, PointReason reason) => Flash($"{Name(winner)} scores - {Describe(reason)}", _messageSeconds);
        void OnGameWon(Side winner) => Flash(winner == Local ? "You win!" : $"{Name(winner)} wins!", float.PositiveInfinity);

        void Flash(string message, float seconds)
        {
            _message = message;
            _messageUntil = Time.time + seconds;
            Refresh();
        }

        void Update()
        {
            if (_message.Length > 0 && Time.time > _messageUntil) { _message = ""; Refresh(); }
        }

        void Refresh()
        {
            if (_text == null) return;
            string status = _match.Phase switch
            {
                MatchPhase.WaitingForServe => _match.Server == Local ? "Your serve" : $"{Name(_match.Server)} serves",
                MatchPhase.GameOver => "Press A to play again",
                MatchPhase.FreePlay => "Free play",
                _ => "",
            };
            string line2 = _match.Phase == MatchPhase.GameOver ? $"{_message}\n{status}"
                : _message.Length > 0 ? _message : status;
            _text.text = $"{_match.Score(Local)}  -  {_match.Score(Local.Opponent())}\n<size={StatusFontSize}>{line2}</size>";
        }

        static string Describe(PointReason reason) => reason switch
        {
            PointReason.ServeMissedOwnSide => "serve must bounce on your side first",
            PointReason.ServeBouncedTwiceOnOwnSide => "serve bounced twice on the server's side",
            PointReason.ServeFault => "fault",
            PointReason.BouncedOnOwnSide => "bounced on own side",
            PointReason.DoubleBounce => "double bounce",
            PointReason.DoubleHit => "double hit",
            PointReason.Volley => "volley over the table",
            PointReason.Long => "long",
            PointReason.NotReturned => "not returned",
            PointReason.Out => "out",
            _ => "",
        };
    }
}

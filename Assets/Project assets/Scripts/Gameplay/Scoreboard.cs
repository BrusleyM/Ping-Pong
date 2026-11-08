using UnityEngine;

namespace PingPong.Gameplay
{
    /// <summary>Floating score above the net, facing side 0 (the local player in solo).</summary>
    public class Scoreboard : MonoBehaviour
    {
        [SerializeField] MatchController _match;
        [SerializeField] GameModeController _modes;
        [SerializeField] float _height = 0.55f;
        [SerializeField] float _characterSize = 0.012f;

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
            _text.fontSize = 64;
            _text.characterSize = _characterSize;
            _text.anchor = TextAnchor.MiddleCenter;
            _text.alignment = TextAlignment.Center;
            _text.color = Color.white;

            PingPongTable t = _match.Table;
            transform.SetPositionAndRotation(t.SurfaceCenter + Vector3.up * _height,
                Quaternion.LookRotation(t.Forward, Vector3.up)); // text reads from side 0

            _match.Changed += Refresh;
            _match.PointWon += (w, reason) => Flash($"{Name(w)} scores - {reason}");
            _match.GameWon += w => Flash(w == 0 ? "You win!" : $"{Name(w)} wins!", 1000f);
            Refresh();
        }

        string Name(int side) => side == 0 ? "You" : (_modes != null ? _modes.OpponentName : "Opponent");

        void Flash(string msg, float seconds = 1.5f)
        {
            _message = msg;
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
            string status = _match.State switch
            {
                MatchState.WaitingForServe => _match.Server == 0 ? "Your serve" : $"{Name(1)} serves",
                MatchState.GameOver => "Press A to play again",
                MatchState.FreePlay => "Free play",
                _ => ""
            };
            string line2 = _message.Length > 0 ? _message : status;
            _text.text = $"{_match.Score[0]}  -  {_match.Score[1]}\n<size=32>{line2}</size>";
        }
    }
}

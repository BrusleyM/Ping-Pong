using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PingPong.GameFeel
{
    /// <summary>
    /// Reacts to GameFeelScore events (the project's match points, fed in by MatchLink): live score and
    /// announcements on the stadium screen, floodlights and screen flashing in the winner's colour on each point,
    /// and confetti when the game is won.
    /// Added automatically by GameFeelBootstrap.
    /// </summary>
    public class MatchPresentation : MonoBehaviour
    {
        [Tooltip("Seconds the board's idle text (e.g. PING PONG) stays up when a game starts before the score appears. 0 = show the score at once.")]
        public float introDuration = 0f;
        public float pointFlashDuration = 1.2f;
        public float winFlashDuration = 5f;
        [Tooltip("Brightness of the lights at the peak of a flash.")]
        public float flashBrightness = 4f;
        [Tooltip("Where the confetti goes off (empty = the middle of the match table).")]
        public Transform confettiCenter;

        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

        GameFeelBootstrap _cfg;
        GameFeelScore _score;
        readonly List<ScoreboardDisplay> _boards = new List<ScoreboardDisplay>();
        bool HasBoard => _boards.Count > 0;

        /// <summary>Shows text on every stadium scoreboard (there can be one at each end).</summary>
        void SetBoards(string text)
        {
            foreach (var b in _boards) if (b != null) b.SetText(text);
        }

        Material _lightMat, _screenMat;
        Color _lightBase, _lightEmission, _screenBase, _screenEmission;
        Coroutine _flash;

        static readonly string[] LightNames = { "Floodlight", "Lamp", "Wall Strip" };

        // Subscribe as early as possible: the game scene starts its match in Start, and the board must not miss it.
        void Awake()
        {
            _cfg = GetComponent<GameFeelBootstrap>();
            _score = GetComponent<GameFeelScore>();
            _boards.AddRange(FindObjectsByType<ScoreboardDisplay>(FindObjectsSortMode.None));

            if (_score != null)
            {
                _score.GameStarted += OnGameStarted;
                _score.PointScored += OnPointScored;
                _score.GameWon += OnGameWon;
                _score.ScoreChanged += OnScoreChanged;
                _score.PaddleHit += OnPaddleHit;
                _score.HitScored += OnHitScored;
                _score.NetTouched += OnNetTouched;
                _score.LetCalled += OnLet;
            }
        }

        void Start()
        {
            SetupMaterials();
            // A game that already started before we were ready: show its score straight away.
            if (_score != null && _score.Started && HasBoard && Live) SetBoards(ScoreText);
        }

        string ScoreText => _score != null ? $"{_score.ScoreSouth} - {_score.ScoreNorth}" : "0 - 0";

        void OnDestroy()
        {
            if (_score != null)
            {
                _score.GameStarted -= OnGameStarted;
                _score.PointScored -= OnPointScored;
                _score.GameWon -= OnGameWon;
                _score.ScoreChanged -= OnScoreChanged;
                _score.PaddleHit -= OnPaddleHit;
                _score.HitScored -= OnHitScored;
                _score.NetTouched -= OnNetTouched;
                _score.LetCalled -= OnLet;
            }
            if (_lightMat != null) Destroy(_lightMat);
            if (_screenMat != null) Destroy(_screenMat);
        }

        /// <summary>
        /// Give the floodlights and the screen their own runtime material copies, so flashing them
        /// never changes the material assets on disk.
        /// </summary>
        void SetupMaterials()
        {
            var stadium = FindFirstObjectByType<StadiumBuilder>();
            if (stadium == null) return;

            var lights = new List<Renderer>();
            foreach (var r in stadium.GetComponentsInChildren<MeshRenderer>(true))
            {
                foreach (var n in LightNames)
                {
                    if (r.name.StartsWith(n)) { lights.Add(r); break; }
                }
            }
            if (lights.Count > 0 && lights[0].sharedMaterial != null)
            {
                _lightMat = new Material(lights[0].sharedMaterial) { name = "Floodlights (runtime)" };
                _lightBase = _lightMat.GetColor(BaseColorId);
                _lightEmission = _lightMat.GetColor(EmissionId);
                foreach (var r in lights) r.sharedMaterial = _lightMat;
            }

            // One runtime screen material shared by every board, so they all flash together.
            foreach (var board in _boards)
            {
                if (board == null || board.screen == null || board.screen.sharedMaterial == null) continue;
                if (_screenMat == null)
                {
                    _screenMat = new Material(board.screen.sharedMaterial) { name = "Screen (runtime)" };
                    _screenMat.EnableKeyword("_EMISSION");
                    _screenBase = _screenMat.GetColor(BaseColorId);
                    _screenEmission = _screenMat.GetColor(EmissionId);
                }
                board.screen.sharedMaterial = _screenMat;
            }
        }

        bool Live => _cfg == null || _cfg.liveScoreboard;

        void OnGameStarted()
        {
            _showingWinner = false;
            if (!HasBoard || !Live) return;
            if (introDuration > 0f) StartCoroutine(ShowAfter(_boards[0].idleText, ScoreText, introDuration));
            else SetBoards(ScoreText);
        }

        bool _intro;

        IEnumerator ShowAfter(string first, string then, float delay)
        {
            _intro = true;
            SetBoards(first);
            yield return new WaitForSeconds(delay);
            _intro = false;
            if (_score != null) then = $"{_score.ScoreSouth} - {_score.ScoreNorth}";
            SetBoards(then);
        }

        bool _showingWinner;

        void OnScoreChanged(int south, int north)
        {
            // Keep the "YOU WIN" / "CPU WINS" message (and any announcement) up until it's done.
            if (HasBoard && Live && !_intro && !_showingWinner && Time.time >= _announceUntil)
                SetBoards($"{south} - {north}");
        }

        // ---------------- Shot announcements ----------------

        [Header("Announcements")]
        [Tooltip("Show SMASH / RALLY / NET messages on the scoreboard.")]
        public bool announcements = true;
        public float announceDuration = 1.4f;
        [Tooltip("Announce the rally combo from this multiplier up.")]
        public int announceRallyFrom = 3;

        float _announceUntil;
        int _announceFrame = -1;
        Coroutine _announce;

        /// <summary>Show a short message on the scoreboard, then go back to the score.</summary>
        public void Announce(string text, float duration = -1f, Color? flash = null)
        {
            if (!HasBoard || !Live || !announcements || _showingWinner || _intro) return;
            if (duration <= 0f) duration = announceDuration;
            if (_announce != null) StopCoroutine(_announce);
            _announce = StartCoroutine(AnnounceRoutine(text, duration));
            _announceFrame = Time.frameCount;
            if (flash.HasValue && (_cfg == null || _cfg.pointFlash)) Flash(flash.Value, 0.5f, 1);
        }

        IEnumerator AnnounceRoutine(string text, float duration)
        {
            _announceUntil = Time.time + duration;
            // blink twice, then hold
            for (int i = 0; i < 2; i++)
            {
                SetBoards(text);
                yield return new WaitForSeconds(0.12f);
                SetBoards("");
                yield return new WaitForSeconds(0.06f);
            }
            SetBoards(text);
            yield return new WaitForSeconds(Mathf.Max(0f, duration - 0.36f));
            _announce = null;
            if (!_showingWinner && _score != null) SetBoards($"{_score.ScoreSouth} - {_score.ScoreNorth}");
        }

        void OnPaddleHit(int side, float kmh, float strength)
        {
            if (_cfg == null || strength < _cfg.smashThreshold) return;
            string t = kmh < 100f ? $"SMASH {kmh:0}KMH" : $"SMASH {kmh:0}";
            Announce(t, -1f, TeamColor(side));
        }

        void OnHitScored(int side, int points, int multiplier, bool bonus, Vector3 where)
        {
            if (bonus || multiplier < announceRallyFrom) return;
            if (_announceFrame == Time.frameCount) return; // a smash was just announced on this hit
            string t = _score != null && _score.PracticeMode ? $"STREAK X{multiplier}" : $"RALLY X{multiplier}!";
            Announce(t);
        }

        void OnNetTouched()
        {
            Announce("NET!", 1.1f);
            var audio = GetComponent<CrowdAudio>();
            if (audio != null) audio.Ooh(0.8f);
        }

        void OnLet() => Announce("LET", 1.2f);

        void OnPointScored(int winner, int south, int north)
        {
            // The point beats any announcement still blinking (e.g. NET!), so the new score isn't overwritten.
            if (_announce != null) { StopCoroutine(_announce); _announce = null; }
            _announceUntil = 0f;
            if (HasBoard && Live) SetBoards($"{south} - {north}");
            if (_cfg == null || _cfg.pointFlash) Flash(TeamColor(winner), pointFlashDuration, 2);
        }

        void OnGameWon(int winner)
        {
            if (HasBoard && Live)
            {
                _showingWinner = true;
                SetBoards(winner == GameFeelScore.South ? "YOU WIN" : (_cfg != null ? _cfg.northName : "CPU") + " WINS");
            }
            if (_cfg == null || _cfg.pointFlash) Flash(TeamColor(winner), winFlashDuration, 8);
            if (_cfg != null && _cfg.confetti && _cfg.Effects != null) _cfg.Effects.Confetti(TableCenter(), 1f);
        }

        Color TeamColor(int side)
        {
            return _cfg == null ? Color.white : _cfg.TeamColor(side);
        }

        Vector3 TableCenter()
        {
            if (confettiCenter != null) return confettiCenter.position;
            var table = GameFeelTable.Main;
            return table != null ? GameFeelTable.Center(table) : transform.position;
        }

        void Flash(Color color, float duration, int pulses)
        {
            if (_flash != null) StopCoroutine(_flash);
            _flash = StartCoroutine(FlashRoutine(color, duration, pulses));
        }

        IEnumerator FlashRoutine(Color color, float duration, int pulses)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / duration);
                // pulses that fade out over the duration
                float k = Mathf.Abs(Mathf.Sin(u * Mathf.PI * pulses)) * (1f - u * u);
                Apply(color, k);
                yield return null;
            }
            Apply(color, 0f);
            _flash = null;
        }

        void Apply(Color color, float k)
        {
            if (_lightMat != null)
            {
                _lightMat.SetColor(BaseColorId, Color.Lerp(_lightBase, color, k));
                _lightMat.SetColor(EmissionId, Color.Lerp(_lightEmission, color * flashBrightness, k));
            }
            if (_screenMat != null)
            {
                _screenMat.SetColor(BaseColorId, Color.Lerp(_screenBase, color * 0.6f, k));
                _screenMat.SetColor(EmissionId, Color.Lerp(_screenEmission, color * (flashBrightness * 0.4f), k));
            }
        }
    }
}

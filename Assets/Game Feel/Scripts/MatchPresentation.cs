using System.Collections;
using System.Collections.Generic;
using PingPong.Gameplay;
using UnityEngine;

namespace PingPong.GameFeel
{
    /// <summary>
    /// Reacts to MatchFeed and RallyFeed events: live score and
    /// announcements on the stadium screen, floodlights and screen flashing in the winner's colour on each point,
    /// and confetti when the game is won.
    /// Added automatically by GameFeelBootstrap.
    /// </summary>
    public class MatchPresentation : MonoBehaviour
    {
        [Header("Start sequence")]
        public float getReadyDuration = 1.2f;
        public int countFrom = 3;
        public float countStepDuration = 0.8f;
        public float goDuration = 0.8f;
        [Header("Points")]
        public float pointFlashDuration = 1.2f;
        public float winFlashDuration = 5f;
        [Tooltip("Brightness of the lights at the peak of a flash.")]
        public float flashBrightness = 4f;
        [Tooltip("Where the confetti goes off (empty = the middle of the match table).")]
        public Transform confettiCenter;

        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

        GameFeelBootstrap _cfg;
        MatchFeed _feed;
        RallyFeed _rally;
        ScoreboardDisplay _board;
        MatchController _match;
        Coroutine _startSequence;

        Material _lightMat, _screenMat;
        Color _lightBase, _lightEmission, _screenBase, _screenEmission;
        Coroutine _flash;

        static readonly string[] LightNames = { "Floodlight", "Lamp", "Wall Strip" };

        IEnumerator Start()
        {
            _cfg = GetComponent<GameFeelBootstrap>();
            _feed = GetComponent<MatchFeed>();
            _rally = GetComponent<RallyFeed>();
            _board = FindFirstObjectByType<ScoreboardDisplay>();
            _match = FindFirstObjectByType<MatchController>();
            SetupMaterials();

            if (_rally != null)
            {
                _rally.PaddleHit += OnPaddleHit;
                _rally.ComboHit += OnComboHit;
                _rally.NetTouched += OnNetTouched;
            }
            if (_feed != null)
            {
                _feed.GameStarted += OnGameStarted;
                _feed.PointScored += OnPointScored;
                _feed.GameWon += OnGameWon;
                _feed.ScoreChanged += OnScoreChanged;
                _feed.LetCalled += OnLet;
                if (_feed.GameBegun) OnGameStarted();
            }

            // The score keeper starts the first game shortly after load; show the intro first.
            yield return null;
        }

        void OnDestroy()
        {
            if (_rally != null)
            {
                _rally.PaddleHit -= OnPaddleHit;
                _rally.ComboHit -= OnComboHit;
                _rally.NetTouched -= OnNetTouched;
            }
            if (_feed != null)
            {
                _feed.GameStarted -= OnGameStarted;
                _feed.PointScored -= OnPointScored;
                _feed.GameWon -= OnGameWon;
                _feed.ScoreChanged -= OnScoreChanged;
                _feed.LetCalled -= OnLet;
            }
            ReleaseServe();
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

            if (_board != null && _board.screen != null && _board.screen.sharedMaterial != null)
            {
                _screenMat = new Material(_board.screen.sharedMaterial) { name = "Screen (runtime)" };
                _screenMat.EnableKeyword("_EMISSION");
                _screenBase = _screenMat.GetColor(BaseColorId);
                _screenEmission = _screenMat.GetColor(EmissionId);
                _board.screen.sharedMaterial = _screenMat;
            }
        }

        bool Live => _cfg == null || _cfg.liveScoreboard;

        void OnGameStarted()
        {
            _showingWinner = false;
            if (_board == null || !Live) return;
            if (_startSequence != null) StopCoroutine(_startSequence);
            _startSequence = StartCoroutine(StartSequence());
        }

        void OnDisable() => ReleaseServe();

        void ReleaseServe()
        {
            if (_match != null) _match.ServeHeld = false;
        }

        bool _intro;

        IEnumerator StartSequence()
        {
            _intro = true;
            if (_match != null) _match.ServeHeld = true;
            _board.SetText("GET READY");
            yield return new WaitForSeconds(getReadyDuration);
            for (int n = countFrom; n > 0; n--)
            {
                _board.SetText(n.ToString());
                Flash(TeamColor(GameFeelTable.South), countStepDuration, 1);
                yield return new WaitForSeconds(countStepDuration);
            }
            ReleaseServe();
            _board.SetText("GO!");
            Flash(TeamColor(GameFeelTable.South), goDuration, 2);
            yield return new WaitForSeconds(goDuration);
            _intro = false;
            _startSequence = null;
            _board.SetText($"{_feed.ScoreSouth} - {_feed.ScoreNorth}");
        }

        bool _showingWinner;

        void OnScoreChanged(int south, int north)
        {
            // Keep the "BLUE WINS" message (and any announcement) up until it's done.
            if (_board != null && Live && !_intro && !_showingWinner && Time.time >= _announceUntil)
                _board.SetText($"{south} - {north}");
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
            if (_board == null || !Live || !announcements || _showingWinner || _intro) return;
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
                _board.SetText(text);
                yield return new WaitForSeconds(0.12f);
                _board.SetText("");
                yield return new WaitForSeconds(0.06f);
            }
            _board.SetText(text);
            yield return new WaitForSeconds(Mathf.Max(0f, duration - 0.36f));
            _announce = null;
            if (!_showingWinner && _feed != null) _board.SetText($"{_feed.ScoreSouth} - {_feed.ScoreNorth}");
        }

        void OnPaddleHit(int side, float kmh, float strength)
        {
            if (_cfg == null || strength < _cfg.smashThreshold) return;
            string t = kmh < 100f ? $"SMASH {kmh:0}KMH" : $"SMASH {kmh:0}";
            Announce(t, -1f, TeamColor(side));
        }

        void OnComboHit(int side, int combo, Vector3 where)
        {
            if (combo < announceRallyFrom) return;
            if (_announceFrame == Time.frameCount) return; // a smash was just announced on this hit
            string t = _rally.PracticeMode ? $"STREAK X{combo}" : $"RALLY X{combo}!";
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
            if (_board != null && Live) _board.SetText($"{south} - {north}");
            if (_cfg == null || _cfg.pointFlash) Flash(TeamColor(winner), pointFlashDuration, 2);
        }

        void OnGameWon(int winner)
        {
            if (_board != null && Live)
            {
                _showingWinner = true;
                _board.SetText(winner == GameFeelTable.South ? "YOU WIN" : (_cfg != null ? _cfg.northName : "CPU") + " WINS");
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

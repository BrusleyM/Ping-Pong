using System.Collections;
using System.Linq;
using PingPong.Gameplay;
using UnityEngine;

namespace PingPong.GameFeel.Testing
{
    /// <summary>
    /// Runs the test stations, which stand in a line so each can be watched on its own.
    /// Number keys (1-8) or the Left/Right arrows move the camera to a station; stand on its pad and press E to run
    /// its demo once. Nothing runs by itself.
    ///   1 STREAK     - lob over the table: streak, landing ring, table markers
    ///   2 HIT        - launcher fires at a paddle (soft / medium / smash): just the hit effects
    ///   3 HIT POINTS - scripted rally: +points labels with a growing combo, "RALLY X5!"
    ///   4 CROWD      - crowd cheer
    ///   5 POINT      - a point is won: lights and screen flash, crowd cheers, the score changes
    ///   6 WIN        - the game is won: confetti, "YOU WIN", big cheer, high scores
    ///   7 TARGETS    - targets on, then lobs at the target
    ///   8 PRACTICE   - ball machine on/off
    /// </summary>
    public class TestStationsDirector : MonoBehaviour
    {
        [Tooltip("The camera the player moves.")]
        public TestFlyCamera player;
        public TestBallLauncher hitLauncher;
        public TestBallLauncher streakLauncher;
        public TestBallLauncher targetsLauncher;
        [Tooltip("Table of the TARGETS station.")]
        public PingPongTable targetsTable;
        [Tooltip("Ball and table of the PRACTICE station.")]
        public PingPongBall practiceBall;

        GameFeelScore _score;
        PracticeMode _practice;
        TargetGame _targets;
        MatchPresentation _show;
        TestStation[] _stations;
        TestStation _near;
        int _current = -1;
        bool _rallyRunning;
        int _pointSide = GameFeelTable.South;
        int _winSide = GameFeelTable.South;
        string _last = "";
        float _lastTime = -10f;

        IEnumerator Start()
        {
            yield return null; // let GameFeelBootstrap add its components
            var cfg = GameFeelBootstrap.Instance;
            if (cfg != null)
            {
                _score = cfg.GetComponent<GameFeelScore>();
                _practice = cfg.GetComponent<PracticeMode>();
                _targets = cfg.GetComponent<TargetGame>();
                _show = cfg.GetComponent<MatchPresentation>();
            }
            if (_targets != null) _targets.table = targetsTable;
            if (_practice != null && practiceBall != null) _practice.ball = practiceBall;
            _stations = FindObjectsByType<TestStation>(FindObjectsSortMode.None).OrderBy(s => s.number).ToArray();
            if (player == null) player = FindFirstObjectByType<TestFlyCamera>();
        }

        void Update()
        {
            if (_stations == null) return;
            _near = Nearest();
            if (_near != null && TestInput.Pressed(TestInput.K.E)) Run(_near);

            for (int i = 0; i < 8; i++)
                if (TestInput.Pressed(TestInput.K.D1 + i)) GoTo(i);
            if (TestInput.Pressed(TestInput.K.Right)) GoTo(Index(_near) + 1);
            if (TestInput.Pressed(TestInput.K.Left)) GoTo(Index(_near) - 1);
        }

        int Index(TestStation s)
        {
            if (s == null) return _current;
            return System.Array.IndexOf(_stations, s);
        }

        void GoTo(int index)
        {
            if (_stations.Length == 0) return;
            index = Mathf.Clamp(index, 0, _stations.Length - 1);
            _current = index;
            var s = _stations[index];
            if (player != null && s.view != null) player.GoTo(s.view);
        }

        TestStation Nearest()
        {
            if (player == null) return null;
            TestStation best = null;
            float bestD = float.MaxValue;
            Vector3 pp = player.transform.position;
            Vector2 p = new Vector2(pp.x, pp.z);
            foreach (var s in _stations)
            {
                if (s == null) continue;
                float d = Vector2.Distance(p, new Vector2(s.transform.position.x, s.transform.position.z));
                if (d <= s.radius && d < bestD) { best = s; bestD = d; }
            }
            return best;
        }

        public void Run(TestStation station)
        {
            switch (station.demo)
            {
                case TestStation.Demo.Hit:
                    if (hitLauncher != null) { hitLauncher.Fire(); Say("HIT  " + hitLauncher.LastShot); }
                    break;
                case TestStation.Demo.Streak:
                    if (streakLauncher != null) { streakLauncher.Fire(); Say("LOB"); }
                    break;
                case TestStation.Demo.HitPoints:
                    if (!_rallyRunning && _score != null) StartCoroutine(ScriptedRally(Focus(station)));
                    break;
                case TestStation.Demo.Point:
                    if (_score == null) break;
                    if (_score.GameOver) _score.StartGame();
                    _score.DemoPoint(_pointSide);
                    Say("POINT TO " + Name(_pointSide) + $"  {_score.ScoreSouth} - {_score.ScoreNorth}");
                    _pointSide = -_pointSide;
                    break;
                case TestStation.Demo.Win:
                    if (_score == null) break;
                    if (_show != null) _show.confettiCenter = station.focus;
                    if (_score.GameOver) _score.StartGame();
                    _score.EndGame(_winSide);
                    Say(_winSide == GameFeelTable.South ? "YOU WIN" : Name(_winSide) + " WINS");
                    _winSide = -_winSide;
                    break;
                case TestStation.Demo.Crowd:
                    FanJump.CheerAll(2.8f, 0.9f);
                    Say("CROWD CHEER");
                    break;
                case TestStation.Demo.Targets:
                    if (_targets == null) break;
                    if (!_targets.Active) { _targets.SetTargets(true); Say("TARGETS ON  (press E again to shoot)"); break; }
                    if (targetsLauncher != null)
                    {
                        Vector3 aim = _targets.TryGetTarget(GameFeelTable.North, out var t) ? t : (targetsLauncher.target != null ? targetsLauncher.target.position : targetsLauncher.transform.position);
                        targetsLauncher.Fire(4.5f, aim);
                        Say("SHOT AT THE TARGET");
                    }
                    break;
                case TestStation.Demo.Practice:
                    if (_practice == null) break;
                    _practice.SetPractice(!_practice.Active);
                    Say("BALL MACHINE " + (_practice.Active ? "ON" : "OFF"));
                    break;
            }
        }

        string Name(int side) => GameFeelBootstrap.Instance != null ? GameFeelBootstrap.Instance.TeamName(side) : (side < 0 ? "YOU" : "CPU");

        static Vector3 Focus(TestStation s) => s.focus != null ? s.focus.position : s.transform.position + Vector3.up * 1.3f;

        IEnumerator ScriptedRally(Vector3 p)
        {
            _rallyRunning = true;
            Say("SCRIPTED RALLY");
            if (_score.GameOver) _score.StartGame();
            int side = GameFeelTable.South;
            for (int hit = 1; hit <= 5; hit++)
            {
                _score.DemoHit(side, hit, false, p + Vector3.right * (side == GameFeelTable.South ? -0.25f : 0.25f));
                yield return new WaitForSeconds(0.35f);
                _score.DemoHit(side, hit, true, p + Vector3.right * (side == GameFeelTable.South ? 0.25f : -0.25f));
                yield return new WaitForSeconds(0.35f);
                side = -side;
            }
            FanJump.CheerAll(2f, 0.7f);
            _rallyRunning = false;
        }

        void Say(string s) { _last = s; _lastTime = Time.unscaledTime; }

        GUIStyle _prompt, _desc, _small;

        void OnGUI()
        {
            if (_prompt == null)
            {
                _prompt = new GUIStyle(GUI.skin.label) { fontSize = 24, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, richText = true };
                _desc = new GUIStyle(GUI.skin.label) { fontSize = 16, alignment = TextAnchor.UpperCenter, wordWrap = true };
                _small = new GUIStyle(GUI.skin.label) { fontSize = 14, richText = true };
            }

            // Draw as if the screen were 720 pixels tall, so the text stays readable on high-resolution screens.
            float ui = Mathf.Max(1f, Screen.height / 720f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(ui, ui, 1f));
            float sw = Screen.width / ui, sh = Screen.height / ui;

            GUILayout.BeginArea(new Rect(10, 10, 470, 160), GUI.skin.box);
            GUILayout.Label("<b>GAME FEEL TEST STATIONS</b>", _small);
            GUILayout.Label("<b>1-8</b> or <b>Left/Right</b> go to a station · <b>E</b> runs it", _small);
            GUILayout.Label("RMB look · WASD move · Space/Q up/down · Shift fast · F1 overview", _small);
            if (_score != null)
            {
                GUILayout.Label($"Score  {Name(GameFeelTable.South)} {_score.ScoreSouth}  -  {Name(GameFeelTable.North)} {_score.ScoreNorth}", _small);
                GUILayout.Label($"Hit points  {_score.HitPointsSouth}  -  {_score.HitPointsNorth}", _small);
            }
            GUILayout.EndArea();

            float w = Mathf.Min(820f, sw - 40f);
            var box = new Rect((sw - w) * 0.5f, sh - 150f, w, 140f);
            bool justRan = Time.unscaledTime - _lastTime < 1.5f && !string.IsNullOrEmpty(_last);
            if (_near != null)
            {
                GUI.Box(box, GUIContent.none);
                GUI.Label(new Rect(box.x, box.y + 8, box.width, 38), $"<color=#FFB020>[E]</color>  {_near.title}", _prompt);
                if (justRan)
                    GUI.Label(new Rect(box.x, box.y + 60, box.width, 38), $"<color=#6CFF6C>▶ {_last}</color>", _prompt);
                else
                    GUI.Label(new Rect(box.x + 16, box.y + 50, box.width - 32, 86), _near.description, _desc);
            }
            else if (justRan)
            {
                GUI.Label(new Rect(box.x, box.y + 60, box.width, 38), _last, _prompt);
            }
        }
    }
}

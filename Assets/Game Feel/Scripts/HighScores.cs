using System;
using System.Collections;
using System.Collections.Generic;
using PingPong.Gameplay;
using UnityEngine;

namespace PingPong.GameFeel
{
    /// <summary>
    /// Hit-point high scores, saved as a small JSON file in the game's data folder on the device
    /// (Application.persistentDataPath/gamefeel_highscores.json) through the project's own JsonFileStore,
    /// the same way the profile and settings are saved. Keeps your top N games (your hit points, won or lost)
    /// and the longest rally. Shows them on the stadium screen after a game and announces "NEW BEST!".
    /// Added automatically by GameFeelBootstrap.
    /// </summary>
    public class HighScores : MonoBehaviour
    {
        [Serializable]
        public class Entry
        {
            public string name;
            public int score;
            public int longestRally;
            public string mode;   // "Match won", "Match lost", ...
            public string date;   // ISO date
        }

        [Serializable]
        public class Data
        {
            public List<Entry> entries = new List<Entry>();
            public int longestRally;
        }

        public const string FileName = "gamefeel_highscores.json";

        [Tooltip("How many games to keep.")]
        public int keep = 10;
        [Tooltip("Show BEST / RALLY on the scoreboard between games.")]
        public bool showBetweenGames = true;

        public Data Scores { get; private set; } = new Data();
        IKeyedStore<Data> _store;
        IKeyedStore<Data> Store => _store ??= new JsonFileStore<Data>(Application.persistentDataPath, FileName);
        public int Best => Scores.entries.Count > 0 ? Scores.entries[0].score : 0;

        GameFeelBootstrap _cfg;
        MatchFeed _match;
        RallyFeed _rally;
        HitPoints _points;
        MatchPresentation _show;
        ScoreboardDisplay _board;

        void Start()
        {
            _cfg = GetComponent<GameFeelBootstrap>();
            _match = GetComponent<MatchFeed>();
            _rally = GetComponent<RallyFeed>();
            _points = GetComponent<HitPoints>();
            _show = GetComponent<MatchPresentation>();
            _board = FindFirstObjectByType<ScoreboardDisplay>();
            Load();
            if (_match != null) _match.GameWon += OnGameWon;
        }

        void OnDestroy()
        {
            if (_match != null) _match.GameWon -= OnGameWon;
        }

        void OnGameWon(int winner)
        {
            if (_rally == null) return;
            bool newRally = _rally.LongestRally > Scores.longestRally;
            bool newBest = _points != null && _points.South > Best;
            if (_points != null)
            {
                string name = _cfg != null ? _cfg.southName : "YOU";
                string mode = (_rally.PracticeMode ? "Practice" : "Match") + (winner == GameFeelTable.South ? " won" : " lost");
                Add(name, _points.South, _rally.LongestRally, mode);
            }
            else RecordRally(_rally.LongestRally);
            if (showBetweenGames) StartCoroutine(ShowRecords(newBest, newRally));
        }

        /// <summary>Record a finished game and save.</summary>
        public void Add(string name, int score, int longestRally, string mode)
        {
            Scores.entries.Add(new Entry { name = name, score = score, longestRally = longestRally, mode = mode, date = DateTime.Now.ToString("yyyy-MM-dd") });
            Scores.entries.Sort((a, b) => b.score.CompareTo(a.score));
            if (Scores.entries.Count > keep) Scores.entries.RemoveRange(keep, Scores.entries.Count - keep);
            Scores.longestRally = Mathf.Max(Scores.longestRally, longestRally);
            Save();
        }

        void RecordRally(int longestRally)
        {
            Scores.longestRally = Mathf.Max(Scores.longestRally, longestRally);
            Save();
        }

        // Runs after the "YOU WIN" message on the stadium screen.
        IEnumerator ShowRecords(bool newBest, bool newRally)
        {
            yield return new WaitForSeconds(2.5f);
            if (_board == null) yield break;
            if (newBest) { _board.SetText("NEW BEST!"); yield return new WaitForSeconds(1.5f); }
            if (_points != null)
            {
                _board.SetText($"BEST {Best}");
                yield return new WaitForSeconds(1.5f);
            }
            _board.SetText($"RALLY {Scores.longestRally}" + (newRally ? "!" : ""));
        }

        public void Load()
        {
            Scores = Store.Load() ?? new Data();
            if (Scores.entries == null) Scores.entries = new List<Entry>();
        }

        public void Save()
        {
            try
            {
                Store.Save(Scores);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Game Feel] Could not save high scores: " + e.Message);
            }
        }

        /// <summary>Wipe all saved scores.</summary>
        public void ResetScores()
        {
            Scores = new Data();
            Save();
        }
    }
}

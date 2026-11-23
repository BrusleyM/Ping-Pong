using System;
using System.Collections;
using UnityEngine;

namespace PingPong.GameFeel
{
    /// <summary>
    /// The game-feel side of scoring. It does not referee: the project's MatchController owns the rules and the real
    /// score, and MatchLink passes its points and results in here (<see cref="ApplyPoint"/>, <see cref="EndGame"/>,
    /// <see cref="StartGame"/>). Without a match (the test scene) the demo methods drive it instead.
    ///
    /// On top of that it keeps Hit Points, an extra score for game feel: every paddle hit earns points, hits in a
    /// rally build a combo multiplier (x1, x2, x3 ... up to maxMultiplier) and a shot that lands on the opponent's half
    /// earns a bonus. Hit points never decide the game.
    ///
    /// Sides are ints: South (-1) = Near (you), North (+1) = Far (opponent).
    /// </summary>
    public class GameFeelScore : MonoBehaviour
    {
        public const int South = GameFeelTable.South;
        public const int North = GameFeelTable.North;

        [Header("Hit points")]
        [Tooltip("Points for every hit, before the combo multiplier.")]
        public int pointsPerHit = 10;
        [Tooltip("Extra points when the shot lands on the opponent's half, before the multiplier.")]
        public int landingBonus = 20;
        [Tooltip("Highest combo multiplier a rally can reach.")]
        public int maxMultiplier = 8;

        [Header("Crowd reactions")]
        public float startCheerDelay = 0.75f;
        public float startCheerDuration = 3.5f;
        [Range(0f, 1f)] public float startCheerIntensity = 0.8f;
        public float pointCheerDuration = 2.2f;
        [Range(0f, 1f)] public float pointCheerIntensity = 0.65f;
        public float endCheerDuration = 7f;
        [Range(0f, 1f)] public float endCheerIntensity = 1f;

        [Header("Without a match (test scene)")]
        [Tooltip("Demo games end at this many points.")]
        public int demoPointsToWin = 11;
        [Tooltip("Seconds after a demo game ends before the score resets.")]
        public float demoNewGameDelay = 4f;

        /// <summary>Match score (from the project's rules, or the demo count without a match).</summary>
        public int ScoreSouth { get; private set; }
        public int ScoreNorth { get; private set; }
        /// <summary>Hit points this game.</summary>
        public int HitPointsSouth { get; private set; }
        public int HitPointsNorth { get; private set; }

        /// <summary>True when a MatchLink feeds the real match in.</summary>
        public bool Linked { get; set; }

        /// <summary>(winnerSide, scoreSouth, scoreNorth)</summary>
        public event Action<int, int, int> PointScored;
        /// <summary>(winnerSide)</summary>
        public event Action<int> GameWon;
        public event Action GameStarted;
        /// <summary>(scoreSouth, scoreNorth): the match score changed.</summary>
        public event Action<int, int> ScoreChanged;
        /// <summary>(side, points, multiplier, isLandingBonus, worldPosition): hit points earned.</summary>
        public event Action<int, int, int, bool, Vector3> HitScored;
        /// <summary>(hitPointsSouth, hitPointsNorth)</summary>
        public event Action<int, int> HitPointsChanged;
        /// <summary>(rallyLength): a rally has finished.</summary>
        public event Action<int> RallyEnded;
        /// <summary>(side, speedKmh, strength 0..1): every counted paddle hit.</summary>
        public event Action<int, float, float> PaddleHit;
        /// <summary>The ball touched the net during a rally.</summary>
        public event Action NetTouched;
        /// <summary>A let was called (the serve is replayed).</summary>
        public event Action LetCalled;

        public int RallyHits => _rallyHits;
        public int LongestRally { get; private set; }
        /// <summary>Practice mode: the ball machine is the opponent, combo = consecutive good returns.</summary>
        public bool PracticeMode { get; set; }
        public int PracticeStreak => _practiceStreak;
        public bool GameOver => _gameOver;

        bool _inPlay;
        bool _gameOver;
        int _hitter;
        int _lastBounceSide;
        int _rallyHits;
        int _practiceStreak;
        bool _landedThisShot;
        bool _awaitingLanding;
        float _lastPaddleTime = -10f;
        int _lastPaddleSide;

        void OnEnable() { BallGameFeel.Contact += OnContact; }
        void OnDisable() { BallGameFeel.Contact -= OnContact; }

        IEnumerator Start()
        {
            yield return new WaitForSeconds(startCheerDelay);
            // With a match, MatchLink starts the game when the match starts.
            if (!Linked) StartGame();
        }

        /// <summary>A new game: hit points and rally reset, the crowd cheers.</summary>
        public void StartGame(int scoreSouth = 0, int scoreNorth = 0)
        {
            ScoreSouth = scoreSouth;
            ScoreNorth = scoreNorth;
            HitPointsSouth = HitPointsNorth = 0;
            _gameOver = false;
            _inPlay = false;
            _rallyHits = 0;
            _practiceStreak = 0;
            LongestRally = 0;
            ScoreChanged?.Invoke(ScoreSouth, ScoreNorth);
            HitPointsChanged?.Invoke(0, 0);
            FanJump.CheerAll(startCheerDuration, startCheerIntensity);
            GameStarted?.Invoke();
        }

        // ---------------- Hit points (from ball contacts) ----------------

        void OnContact(BallGameFeel ball, BallGameFeel.ContactKind kind, Vector3 point, Vector3 paddlePos)
        {
            if (_gameOver || ball == null) return;
            if (ball.Options != null && !ball.Options.hitPoints) return;
            Vector3 ballPos = ball.transform.position;
            switch (kind)
            {
                case BallGameFeel.ContactKind.Paddle:
                {
                    int side = ball.SideOf(paddlePos);
                    // One swing can touch the paddle's colliders more than once: count it once.
                    if (side == _lastPaddleSide && Time.time - _lastPaddleTime < 0.25f) return;
                    _lastPaddleTime = Time.time;
                    _lastPaddleSide = side;

                    // The combo grows when the ball goes back and forth between players.
                    // In practice mode the machine is the opponent: the combo is your run of good returns.
                    if (PracticeMode) _rallyHits = _practiceStreak + 1;
                    else if (_inPlay && side != _hitter) _rallyHits++;
                    else _rallyHits = 1;
                    LongestRally = Mathf.Max(LongestRally, _rallyHits);

                    _hitter = side;
                    _inPlay = true;
                    _awaitingLanding = true;
                    _landedThisShot = false;
                    _lastBounceSide = 0;

                    int mult = Mathf.Clamp(_rallyHits, 1, Mathf.Max(1, maxMultiplier));
                    PaddleHit?.Invoke(side, ball.LastImpactSpeed * 3.6f, ball.LastImpactStrength);
                    AddHitPoints(side, pointsPerHit * mult, mult, false, ballPos);
                    break;
                }

                case BallGameFeel.ContactKind.Table:
                {
                    if (!_inPlay) return;
                    int s = ball.SideOf(point);
                    if (_awaitingLanding && s == -_hitter)
                    {
                        _awaitingLanding = false;
                        _landedThisShot = true;
                        if (PracticeMode) _practiceStreak++;
                        int mult = Mathf.Clamp(_rallyHits, 1, Mathf.Max(1, maxMultiplier));
                        if (landingBonus > 0) AddHitPoints(_hitter, landingBonus * mult, mult, true, ballPos);
                    }
                    else if (s == _lastBounceSide)
                    {
                        EndRally(); // two bounces on one half: rally over
                        return;
                    }
                    _lastBounceSide = s;
                    break;
                }

                case BallGameFeel.ContactKind.Net:
                    if (_inPlay) NetTouched?.Invoke();
                    break;

                case BallGameFeel.ContactKind.Other:
                    if (_inPlay) EndRally();
                    break;
            }
        }

        /// <summary>Practice mode: the ball machine fed a ball, i.e. the "opponent" hit it.</summary>
        public void RegisterFeed()
        {
            if (_gameOver) return;
            // A feed that was never returned ends the player's streak.
            if (_inPlay && _hitter == North) { _practiceStreak = 0; EndRally(); }
            _hitter = North;
            _inPlay = true;
            _awaitingLanding = false;
            _lastBounceSide = 0;
        }

        /// <summary>Extra points from outside the normal rules (e.g. hitting a target).</summary>
        public void AddBonus(int side, int points, Vector3 where)
        {
            if (_gameOver) return;
            AddHitPoints(side, points, 1, true, where);
        }

        void AddHitPoints(int side, int points, int mult, bool bonus, Vector3 where)
        {
            if (side == South) HitPointsSouth += points; else HitPointsNorth += points;
            HitScored?.Invoke(side, points, mult, bonus, where);
            HitPointsChanged?.Invoke(HitPointsSouth, HitPointsNorth);
        }

        void EndRally()
        {
            if (!_inPlay) return;
            _inPlay = false;
            _awaitingLanding = false;
            int length = _rallyHits;
            _rallyHits = 0;
            // Practice: missing a feed, or a return that didn't land on the far half, ends the streak.
            if (PracticeMode && (_hitter == North || !_landedThisShot)) _practiceStreak = 0;
            RallyEnded?.Invoke(length);
        }

        // ---------------- The real match (MatchLink calls these) ----------------

        /// <summary>A point was won. Scores are the match score after the point.</summary>
        public void ApplyPoint(int winnerSide, int scoreSouth, int scoreNorth)
        {
            if (_gameOver) return;
            EndRally();
            ScoreSouth = scoreSouth;
            ScoreNorth = scoreNorth;
            PointScored?.Invoke(winnerSide, ScoreSouth, ScoreNorth);
            ScoreChanged?.Invoke(ScoreSouth, ScoreNorth);
            FanJump.CheerAll(pointCheerDuration, pointCheerIntensity);
        }

        /// <summary>A let: no point, the serve is replayed.</summary>
        public void ApplyLet()
        {
            EndRally();
            LetCalled?.Invoke();
        }

        /// <summary>End the game: big cheer and the result.</summary>
        public void EndGame(int winnerSide)
        {
            if (_gameOver) return;
            _gameOver = true;
            EndRally();
            FanJump.CheerAll(endCheerDuration, endCheerIntensity);
            GameWon?.Invoke(winnerSide);
            if (!Linked) StartCoroutine(NewGameAfterDelay());
        }

        IEnumerator NewGameAfterDelay()
        {
            yield return new WaitForSeconds(Mathf.Max(demoNewGameDelay, endCheerDuration));
            StartGame();
        }

        // ---------------- Demo hooks (test scene, no match) ----------------

        /// <summary>Give a demo point to a side; the demo game ends at demoPointsToWin (win by 2).</summary>
        public void DemoPoint(int winnerSide)
        {
            if (_gameOver) return;
            int s = ScoreSouth + (winnerSide == South ? 1 : 0);
            int n = ScoreNorth + (winnerSide == North ? 1 : 0);
            ApplyPoint(winnerSide, s, n);
            if (Mathf.Max(s, n) >= demoPointsToWin && Mathf.Abs(s - n) >= 2) EndGame(s > n ? South : North);
        }

        /// <summary>Award hit points as if a hit (or a landing bonus) happened at a position.</summary>
        public void DemoHit(int side, int multiplier, bool landingBonusHit, Vector3 where)
        {
            if (_gameOver) return;
            int pts = (landingBonusHit ? landingBonus : pointsPerHit) * Mathf.Max(1, multiplier);
            if (!landingBonusHit) PaddleHit?.Invoke(side, 30f, 0.4f);
            AddHitPoints(side, pts, Mathf.Max(1, multiplier), landingBonusHit, where);
        }
    }
}

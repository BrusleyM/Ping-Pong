using System;
using System.Collections;
using PingPong.Gameplay;
using UnityEngine;

namespace PingPong.GameFeel
{
    /// <summary>
    /// Drop-in game feel. Put one in a scene (the Game Feel copy of the game scene, or the test scene).
    /// At runtime it finds every <see cref="PingPongBall"/> and paddle (<see cref="PaddleModel"/>, or a Rigidbody with
    /// "paddle" in its name) and adds the game-feel components to them. Nothing in the project's own prefabs or
    /// scripts is changed: everything listens to the project's events (ball impacts, match points, game won).
    /// </summary>
    [DisallowMultipleComponent]
    public class GameFeelBootstrap : MonoBehaviour
    {
        [Header("Features (toggle on/off)")]
        [Tooltip("Speed-based trail on the ball + sparks at each contact.")]
        public bool ballVisuals = true;
        [Tooltip("Ball squashes on bounce, paddle flashes on hit.")]
        public bool squashAndFlash = true;
        [Tooltip("Expanding ring where the ball lands on the table.")]
        public bool tableMarkers = true;
        [Tooltip("Crowd jumps at the start, on every point and at the end of a game.")]
        public bool crowdReactions = true;
        [Tooltip("Bright flash + shockwave ring on paddle hits, bigger on hard smashes.")]
        public bool bigHitImpact = true;
        [Tooltip("Confetti rain and cannons when a game is won.")]
        public bool confetti = true;
        [Tooltip("Floodlights and stadium screen flash in the winner's colour when a point is scored.")]
        public bool pointFlash = true;
        [Tooltip("Stadium screen shows the live score and announcements.")]
        public bool liveScoreboard = true;
        [Tooltip("Crowd chatter, roars and applause (generated in code). Off for now.")]
        public bool crowdSounds = false;
        [Tooltip("Hit points: every hit earns points, rallies build a combo, landing a shot earns a bonus.")]
        public bool hitPoints = true;
        [Tooltip("Floating '+20 x2' numbers above the ball when hit points are earned.")]
        public bool hitPointPopups = true;
        [Tooltip("Ring on the table showing where the ball is about to bounce.")]
        public bool landingPredictor = true;
        [Tooltip("Ball machine practice (switch on with PracticeMode.SetPractice).")]
        public bool practiceMode = true;
        [Tooltip("Target mini-game (switch on with TargetGame.SetTargets).")]
        public bool targetGame = true;
        [Tooltip("Save the best hit-point scores (JSON file on the device, via the project's JsonFileStore).")]
        public bool highScores = true;

        [Header("Assets")]
        [Tooltip("Additive material for trail, sparks and markers.")]
        public Material effectMaterial;
        [Tooltip("Material for confetti pieces (falls back to Effect Material).")]
        public Material confettiMaterial;
        [Tooltip("Font for the hit-point labels (the game's Fredoka font; TextMeshPro's default if empty).")]
        public TMPro.TMP_FontAsset popupFont;

        [Header("Impact tuning")]
        [Tooltip("Impact speed (m/s) treated as the softest hit.")]
        public float minImpactSpeed = 0.4f;
        [Tooltip("Impact speed (m/s) treated as the hardest hit.")]
        public float maxImpactSpeed = 8f;

        [Header("Ball visuals")]
        public Color sparkColor = new Color(1f, 0.75f, 0.35f, 1f);
        public Color paddleSparkColor = new Color(1f, 0.4f, 0.3f, 1f);

        [Header("Ball streak (movement tracking)")]
        [Tooltip("Bright streak behind the ball so players can track it.")]
        public bool ballStreak = true;
        [Tooltip("Ball speed (m/s) above which the streak shows.")]
        public float streakMinSpeed = 0.3f;
        [Tooltip("Streak length in seconds: x = slow ball, y = fast ball.")]
        public Vector2 streakTime = new Vector2(0.22f, 0.45f);
        [Tooltip("Streak width as a multiple of the ball's diameter.")]
        public float streakWidth = 1.1f;
        [Tooltip("Colour the streak by whoever hit the ball last.")]
        public bool streakTeamColors = true;
        public Color streakColor = new Color(1f, 0.85f, 0.45f, 1f);
        [Tooltip("Thin white core inside the streak for extra visibility.")]
        public bool streakCore = true;

        [Header("Hit impact")]
        [Tooltip("Hit strength (0..1) above which a hit counts as a smash: bigger flash and ring.")]
        [Range(0f, 1f)] public float smashThreshold = 0.75f;
        public Color impactColor = new Color(1f, 0.9f, 0.6f, 1f);

        [Header("Players (South = Near = you, North = Far = opponent)")]
        public string southName = "YOU";
        public Color southColor = new Color(0.15f, 0.45f, 1f, 1f);
        public string northName = "CPU";
        public Color northColor = new Color(1f, 0.18f, 0.2f, 1f);

        [Header("Confetti")]
        public Color[] confettiColors =
        {
            new Color(1f, 0.2f, 0.25f), new Color(0.2f, 0.5f, 1f), new Color(1f, 0.85f, 0.1f),
            new Color(0.2f, 0.9f, 0.45f), new Color(1f, 1f, 1f), new Color(0.95f, 0.35f, 0.9f)
        };

        [Header("Squash & flash")]
        [Range(0f, 0.6f)] public float maxSquash = 0.35f;
        public Color flashColor = Color.white;

        [Header("Table markers")]
        public Color markerColor = new Color(0.45f, 0.9f, 1f, 1f);
        [Tooltip("Rigidbodies with this in their name are treated as paddles (besides PaddleModel objects).")]
        public string paddleNameContains = "paddle";

        public static GameFeelBootstrap Instance { get; private set; }
        public GameFeelEffects Effects { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Instance = null;

        void Awake()
        {
            Instance = this;
            Effects = GetOrAdd<GameFeelEffects>();
            Effects.Init(this);

            var match = FindFirstObjectByType<MatchController>();
            if (match != null && match.Ball != null) GameFeelTable.Main = match.Ball.Table;

            // Score first: the others listen to it.
            GetOrAdd<GameFeelScore>();
            if (match != null) GetOrAdd<MatchLink>();
            if (confetti || pointFlash || liveScoreboard || crowdReactions) GetOrAdd<MatchPresentation>();
            if (crowdSounds) GetOrAdd<CrowdAudio>();
            if (hitPoints && hitPointPopups) GetOrAdd<HitScorePopups>();
            if (practiceMode) GetOrAdd<PracticeMode>();
            if (targetGame) GetOrAdd<TargetGame>();
            if (highScores) GetOrAdd<HighScores>();
        }

        T GetOrAdd<T>() where T : Component
        {
            var c = GetComponent<T>();
            return c != null ? c : gameObject.AddComponent<T>();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        IEnumerator Start()
        {
            // Balls and paddles can be spawned later, so keep scanning (cheap: twice a second).
            var wait = new WaitForSeconds(0.5f);
            while (true)
            {
                Scan();
                yield return wait;
            }
        }

        void Scan()
        {
            foreach (var ball in FindObjectsByType<PingPongBall>(FindObjectsSortMode.None))
            {
                if (ball.GetComponent<BallGameFeel>() != null) continue;
                ball.gameObject.AddComponent<BallGameFeel>().Init(this, ball);
                var options = ball.GetComponent<GameFeelBallOptions>();
                if (landingPredictor && (options == null || options.landingRing)) ball.gameObject.AddComponent<LandingPredictor>().Init(this, ball);
            }

            foreach (var model in FindObjectsByType<PaddleModel>(FindObjectsSortMode.None))
            {
                if (model.GetComponent<PaddleGameFeel>() == null) model.gameObject.AddComponent<PaddleGameFeel>().Init(this);
            }

            foreach (var rb in FindObjectsByType<Rigidbody>(FindObjectsSortMode.None))
            {
                if (!NameContains(rb.name, paddleNameContains)) continue;
                if (rb.GetComponent<PaddleGameFeel>() != null || rb.GetComponent<PingPongBall>() != null) continue;
                if (rb.GetComponentInChildren<PaddleGameFeel>() != null) continue; // has a PaddleModel already
                rb.gameObject.AddComponent<PaddleGameFeel>().Init(this);
            }
        }

        /// <summary>0 for a soft hit, 1 for a hard one.</summary>
        public float Intensity(float impactSpeed)
        {
            return Mathf.Clamp01(Mathf.InverseLerp(minImpactSpeed, maxImpactSpeed, impactSpeed));
        }

        public Color TeamColor(int side) => side == GameFeelTable.South ? southColor : northColor;
        public string TeamName(int side) => side == GameFeelTable.South ? southName : northName;

        public static bool NameContains(string name, string fragment)
        {
            return !string.IsNullOrEmpty(fragment) &&
                   name.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}

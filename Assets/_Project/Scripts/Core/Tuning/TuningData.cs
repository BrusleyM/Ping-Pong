using System;
using UnityEngine;

namespace PingPong.Core
{
    /// <summary>Timing and serve placement around the rules (the rules engine itself has no clock).</summary>
    [Serializable]
    public struct MatchPacing
    {
        [Tooltip("Pause after a point before the next serve, seconds.")] public float PointPause;
        [Tooltip("A rally with no paddle hit for this long ends as a dead ball, seconds.")] public float RallyTimeout;
        [Tooltip("Serve position: height above the table, metres.")] public float ServeHeight;
        [Tooltip("Serve position: distance behind the end line, metres.")] public float ServeBehind;
        [Tooltip("Below this height (relative to the surface) the ball is out of play, metres.")] public float DeadBelow;
        [Tooltip("Farther than this from the table centre the ball is out of play, metres.")] public float DeadBeyond;
        [Tooltip("A tossed serve that falls below this height without being hit is reset, metres.")] public float ServeResetBelow;

        public static MatchPacing Default => new MatchPacing
        {
            PointPause = 1.5f,
            RallyTimeout = 4f,
            ServeHeight = 0.3f,
            ServeBehind = 0.1f,
            DeadBelow = -1.2f,
            DeadBeyond = 8f,
            ServeResetBelow = -0.5f,
        };
    }

    /// <summary>How the player's paddle responds to the ball.</summary>
    [Serializable]
    public struct PaddleFeel
    {
        [Tooltip("Normal speed kept off the rubber, 0-1.")] public float Restitution;
        [Tooltip("Tangential speed kept off the rubber, 0-1.")] public float TangentialRetain;
        [Tooltip("0 = pure physics, 1 = always lands on the opponent's half.")] public float AimAssist;
        [Tooltip("Hard cap on return speed, m/s.")] public float MaxSpeed;
        [Tooltip("Minimum time between two hits, seconds.")] public float Cooldown;
        [Tooltip("Aim assist keeps its landing spot inside these margins.")] public LandingMargins AssistMargins;
        [Tooltip("Horizontal speed range the aim assist may use, m/s.")] public Vector2 AssistSpeed;
        [Tooltip("How the aim assist retries a shot that would hit the net.")] public NetClearRetry AssistNetRetry;

        public static PaddleFeel Default => new PaddleFeel
        {
            Restitution = 0.85f,
            TangentialRetain = 0.6f,
            AimAssist = 0.35f,
            MaxSpeed = 20f,
            Cooldown = 0.12f,
            AssistMargins = new LandingMargins(edge: 0.1f, fromNet: 0.3f, fromEnd: 0.12f),
            AssistSpeed = new Vector2(3f, 12f),
            AssistNetRetry = new NetClearRetry(attempts: 2, speedFalloff: 0.8f),
        };
    }

    /// <summary>One AI difficulty level.</summary>
    [Serializable]
    public struct AIDifficulty
    {
        public string Name;
        [Tooltip("Seconds before the AI reacts to the player's hit.")] public float ReactionTime;
        [Tooltip("Paddle movement speed, m/s.")] public float MoveSpeed;
        [Tooltip("Horizontal speed range of returns, m/s.")] public Vector2 ShotSpeed;
        [Tooltip("Random aim error on the landing point, metres.")] public float AimError;
        [Tooltip("How deep into the opponent's half it aims, 0-1 (1 = end line).")] public Vector2 Depth;
        [Range(0f, 1f)] public float MissChance;

        public static AIDifficulty[] Defaults => new[]
        {
            new AIDifficulty { Name = "Easy", ReactionTime = 0.35f, MoveSpeed = 1.6f, ShotSpeed = new Vector2(3.5f, 4.5f), AimError = 0.25f, Depth = new Vector2(0.35f, 0.7f), MissChance = 0.15f },
            new AIDifficulty { Name = "Normal", ReactionTime = 0.22f, MoveSpeed = 2.6f, ShotSpeed = new Vector2(4.5f, 6.5f), AimError = 0.15f, Depth = new Vector2(0.45f, 0.85f), MissChance = 0.06f },
            new AIDifficulty { Name = "Hard", ReactionTime = 0.12f, MoveSpeed = 4f, ShotSpeed = new Vector2(6f, 9f), AimError = 0.08f, Depth = new Vector2(0.6f, 0.92f), MissChance = 0.02f },
        };
    }

    /// <summary>AI behaviour shared by all difficulty levels.</summary>
    [Serializable]
    public struct AIBehaviour
    {
        [Tooltip("Max distance from the paddle at which the AI can still reach the ball, metres.")] public float Reach;
        [Tooltip("Delay before the AI serves, seconds.")] public float ServeDelay;
        [Tooltip("Serve horizontal speed range, m/s.")] public Vector2 ServeSpeed;
        [Tooltip("Paddle rest position: height above the table and distance behind the end line, metres.")] public Vector2 RestOffset;
        [Tooltip("Returns land inside these margins.")] public LandingMargins TargetMargins;
        [Tooltip("Share of the half-width returns are spread across, 0-1.")] public float AcrossSpread;
        [Tooltip("How a return that would hit the net is retried with more arc.")] public NetClearRetry NetRetry;
        [Tooltip("Serve: first bounce depth range on its own half, 0-1 of the half length.")] public Vector2 ServeFirstBounce;
        [Tooltip("Serve: share of the half-width the first bounce may be placed across, 0-1.")] public float ServeAcrossSpread;
        [Tooltip("Serve: minimum depth of the second bounce on the receiver's half, metres.")] public float ServeMinReceiverDepth;
        [Tooltip("Hits the ball as it drops through this height above the table, metres.")] public float ComfortHeight;
        [Tooltip("Stands this far behind the ball, away from the net, metres.")] public float BehindBall;
        [Tooltip("Swing animation: angle in degrees and recovery speed per second.")] public Vector2 Swing;

        public static AIBehaviour Default => new AIBehaviour
        {
            Reach = 0.3f,
            ServeDelay = 1.2f,
            ServeSpeed = new Vector2(2.8f, 4.2f),
            RestOffset = new Vector2(0.25f, 0.35f),
            TargetMargins = new LandingMargins(edge: 0.05f, fromNet: 0.2f, fromEnd: 0.05f),
            AcrossSpread = 0.6f,
            NetRetry = new NetClearRetry(attempts: 5, speedFalloff: 0.85f),
            ServeFirstBounce = new Vector2(0.4f, 0.7f),
            ServeAcrossSpread = 0.3f,
            ServeMinReceiverDepth = 0.15f,
            ComfortHeight = 0.3f,
            BehindBall = 0.04f,
            Swing = new Vector2(40f, 4f),
        };
    }
}

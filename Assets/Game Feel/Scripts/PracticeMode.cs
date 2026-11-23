using PingPong.Core;
using PingPong.Gameplay;
using UnityEngine;

namespace PingPong.GameFeel
{
    /// <summary>
    /// Practice mode: a ball machine at the far end of the table feeds the real ball to you.
    /// Your combo is your run of good returns (each return that lands on the far half adds one).
    ///
    /// With a match in the scene it stops the match (the rules go to FreePlay, where anyone may hit and nothing is
    /// scored) and switches the AI off; switching practice off restarts the match through GameModeController.
    /// Feeds are aimed with the ball's own flight model (BallFlightModel.SolveLaunchClearingNet).
    /// Not in the project's menu yet: call SetPractice(true) (the test scene's PRACTICE station does).
    /// </summary>
    public class PracticeMode : MonoBehaviour
    {
        [Header("Ball machine")]
        [Range(0f, 1f)] [Tooltip("0 = slow, high lobs. 1 = fast, flat feeds.")]
        public float difficulty = 0.35f;
        [Tooltip("Seconds between feeds at difficulty 0 and 1.")]
        public Vector2 feedInterval = new Vector2(2.8f, 1.7f);
        [Tooltip("Horizontal ball speed (m/s) of a feed at difficulty 0 and 1.")]
        public Vector2 feedSpeed = new Vector2(3.2f, 6.5f);
        [Tooltip("Feeds land this far across the centre line (m), either way.")]
        public float targetHalfWidth = 0.55f;
        [Tooltip("Feeds land this far from the net on your half (m): x = shortest, y = deepest.")]
        public Vector2 targetDepth = new Vector2(0.35f, 1.15f);

        [Header("References (found if empty)")]
        public PingPongBall ball;
        public MatchController match;
        public AIOpponent ai;
        public GameModeController modes;

        public bool Active { get; private set; }

        static readonly NetClearRetry Retry = new NetClearRetry(4, 0.85f);

        GameFeelScore _score;
        Transform _machine, _muzzle;
        float _timer;
        bool _aiWasOn;

        void Start()
        {
            _score = GetComponent<GameFeelScore>();
            if (match == null) match = FindFirstObjectByType<MatchController>();
            if (ball == null) ball = match != null ? match.Ball : FindFirstObjectByType<PingPongBall>();
            if (ai == null) ai = FindFirstObjectByType<AIOpponent>();
            if (modes == null) modes = FindFirstObjectByType<GameModeController>();
        }

        public void SetPractice(bool on)
        {
            if (on == Active) return;
            Active = on;
            if (_score == null) _score = GetComponent<GameFeelScore>();
            if (_score != null)
            {
                _score.PracticeMode = on;
                _score.StartGame();
            }

            if (on)
            {
                if (match != null) match.StopMatch(); // FreePlay: anyone may hit, nothing is scored
                if (ai != null) { _aiWasOn = ai.enabled; ai.enabled = false; }
                EnsureMachine();
            }
            else
            {
                if (ai != null) ai.enabled = _aiWasOn;
                if (modes != null) modes.Restart();
            }
            if (_machine != null) _machine.gameObject.SetActive(on);
            _timer = -1.2f; // short pause before the first feed
        }

        void Update()
        {
            if (!Active) return;
            _timer += Time.deltaTime;
            float interval = Mathf.Lerp(feedInterval.x, feedInterval.y, difficulty);
            if (_timer >= interval) { _timer = 0f; Feed(); }
        }

        PingPongTable Table => ball != null ? ball.Table : GameFeelTable.Main;

        void EnsureMachine()
        {
            if (_machine != null || Table == null) return;
            TableFrame f = Table.Frame;

            _machine = new GameObject("Ball Machine").transform;
            Vector3 basePos = f.PointOnSide(Side.Far, 0f, f.HalfLength + 0.55f);
            basePos.y = f.SurfaceCenter.y - 0.76f; // on the floor
            _machine.SetPositionAndRotation(basePos, GameFeelTable.Rotation(f));

            var shader = Shader.Find("Universal Render Pipeline/Lit");
            var mat = new Material(shader) { color = new Color(0.16f, 0.17f, 0.2f) };
            var accent = new Material(shader) { color = new Color(1f, 0.55f, 0.1f) };
            Part("Stand", new Vector3(0f, 0.45f, 0f), new Vector3(0.12f, 0.9f, 0.12f), mat);
            Part("Body", new Vector3(0f, 1.0f, 0f), new Vector3(0.36f, 0.26f, 0.3f), mat);
            Part("Hopper", new Vector3(0f, 1.2f, 0.04f), new Vector3(0.3f, 0.14f, 0.24f), accent);
            Part("Barrel", new Vector3(0f, 1.0f, -0.22f), new Vector3(0.08f, 0.08f, 0.18f), accent);

            _muzzle = new GameObject("Muzzle").transform;
            _muzzle.SetParent(_machine, false);
            _muzzle.localPosition = new Vector3(0f, 1.0f, -0.36f);
        }

        void Part(string name, Vector3 localPos, Vector3 size, Material m)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            Destroy(go.GetComponent<Collider>()); // the ball flies straight out
            go.transform.SetParent(_machine, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = size;
            go.GetComponent<MeshRenderer>().sharedMaterial = m;
        }

        /// <summary>Feed the ball to a random spot on your half.</summary>
        public void Feed()
        {
            EnsureMachine();
            if (ball == null || _muzzle == null) return;
            TableFrame f = Table.Frame;
            Vector3 start = _muzzle.position;
            Vector3 aim = f.PointOnSide(Side.Near, Random.Range(-targetHalfWidth, targetHalfWidth), Random.Range(targetDepth.x, targetDepth.y));
            float speed = Mathf.Lerp(feedSpeed.x, feedSpeed.y, difficulty);
            ball.Model.SolveLaunchClearingNet(f, start, aim, speed, Retry, out Vector3 v);

            ball.Hold(start); // clears any serve hover, then let it fly
            Rigidbody rb = ball.Body;
            rb.isKinematic = false;
            rb.linearVelocity = v;
            rb.angularVelocity = Vector3.zero;
            foreach (var tr in ball.GetComponentsInChildren<TrailRenderer>()) tr.Clear();
            if (_score != null) _score.RegisterFeed();
        }
    }
}

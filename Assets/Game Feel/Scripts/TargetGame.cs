using PingPong.Gameplay;
using UnityEngine;

namespace PingPong.GameFeel
{
    /// <summary>
    /// Target practice: glowing targets appear on the table. Land a shot inside one for bonus points.
    /// A target only counts for a shot that lands on the far half from the hitter (a legal return).
    /// In practice mode there is one target, on the machine's half. In a match there is one on each half.
    /// Call SetTargets(true) to switch it on (the test scene's TARGETS station does).
    /// Added automatically by GameFeelBootstrap.
    /// </summary>
    public class TargetGame : MonoBehaviour
    {
        public int targetPoints = 100;
        [Tooltip("Target radius in metres.")]
        public float radius = 0.13f;
        [Tooltip("A target moves somewhere else after this many seconds if nobody hits it.")]
        public float moveAfter = 12f;

        [Tooltip("Table for the targets (empty = the match table).")]
        public PingPongTable table;

        public bool Active { get; private set; }

        const int Rings = 3, Segments = 32;

        GameFeelBootstrap _cfg;
        GameFeelScore _score;
        PracticeMode _practice;
        PingPongTable Table => table != null ? table : GameFeelTable.Main;
        readonly Target[] _targets = new Target[2];
        int _lastHitter = GameFeelScore.South;

        class Target
        {
            public Transform root;
            public LineRenderer[] rings;
            public int side;          // which half of the table it sits on
            public Vector3 local;     // table-local centre
            public float age;
            public float hitFlash;
        }

        void Start()
        {
            _cfg = GetComponent<GameFeelBootstrap>();
            _score = GetComponent<GameFeelScore>();
            _practice = GetComponent<PracticeMode>();
            BallGameFeel.Contact += OnContact;
        }

        void OnDestroy() { BallGameFeel.Contact -= OnContact; }

        public void SetTargets(bool on)
        {
            Active = on;
            if (on) Ensure();
            Refresh();
            Debug.Log("[Game Feel] Targets " + (on ? "ON" : "OFF"));
        }

        void Ensure()
        {
            for (int i = 0; i < 2; i++)
            {
                if (_targets[i] != null) continue;
                var tg = new Target { side = i == 0 ? GameFeelScore.North : GameFeelScore.South, rings = new LineRenderer[Rings] };
                tg.root = new GameObject("Target " + (tg.side > 0 ? "North" : "South")).transform;
                tg.root.SetParent(transform, false);
                for (int r = 0; r < Rings; r++)
                {
                    var go = new GameObject("Ring " + r);
                    go.transform.SetParent(tg.root, false);
                    var lr = go.AddComponent<LineRenderer>();
                    lr.sharedMaterial = _cfg != null ? _cfg.effectMaterial : null;
                    lr.useWorldSpace = false;
                    lr.loop = true;
                    lr.positionCount = Segments;
                    float rr = (r + 1f) / Rings;
                    for (int s = 0; s < Segments; s++)
                    {
                        float a = s / (float)Segments * Mathf.PI * 2f;
                        lr.SetPosition(s, new Vector3(Mathf.Cos(a) * rr, 0f, Mathf.Sin(a) * rr));
                    }
                    lr.widthMultiplier = r == Rings - 1 ? 0.01f : 0.006f;
                    lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    lr.receiveShadows = false;
                    tg.rings[r] = lr;
                }
                _targets[i] = tg;
                Move(tg);
            }
        }

        /// <summary>Centre of the target on a side's half, if it is showing.</summary>
        public bool TryGetTarget(int side, out Vector3 position)
        {
            position = Vector3.zero;
            foreach (var t in _targets)
            {
                if (t == null || t.side != side || !Shown(t)) continue;
                position = t.root.position;
                return true;
            }
            return false;
        }

        bool Shown(Target t)
        {
            if (!Active || t == null) return false;
            bool practice = _practice != null && _practice.Active;
            // Practice: only the machine's half (that's where your returns land).
            return !practice || t.side == GameFeelScore.North;
        }

        void Refresh()
        {
            foreach (var t in _targets)
            {
                if (t == null) continue;
                t.root.gameObject.SetActive(Shown(t));
            }
        }

        void Move(Target t)
        {
            t.age = 0f;
            t.local = new Vector3(Random.Range(-0.5f, 0.5f), 0.003f, t.side * Random.Range(0.35f, 1.1f));
            if (Table != null)
            {
                var f = Table.Frame;
                t.root.SetPositionAndRotation(f.ToWorld(t.local), GameFeelTable.Rotation(f));
            }
            t.root.localScale = Vector3.one * radius;
        }

        void Update()
        {
            if (!Active) return;
            Refresh();
            foreach (var t in _targets)
            {
                if (t == null || !Shown(t)) continue;
                t.age += Time.deltaTime;
                if (t.age > moveAfter) Move(t);
                t.hitFlash = Mathf.MoveTowards(t.hitFlash, 0f, Time.deltaTime * 2f);

                // Gentle pulse; the outer ring is in the colour of the player aiming at it.
                Color team = _cfg == null ? Color.white : _cfg.TeamColor(-t.side); // the player aiming at it
                float pulse = 0.6f + 0.4f * Mathf.Sin(Time.time * 4f + t.side);
                for (int r = 0; r < Rings; r++)
                {
                    Color c = r == Rings - 1 ? team : Color.Lerp(team, Color.white, 0.6f);
                    c *= (0.8f + 0.6f * pulse + 2f * t.hitFlash);
                    c.a = 0.85f;
                    t.rings[r].startColor = t.rings[r].endColor = c;
                }
                // pop when hit
                t.root.localScale = Vector3.one * radius * (1f + 0.6f * t.hitFlash);
            }
        }

        void OnContact(BallGameFeel ball, BallGameFeel.ContactKind kind, Vector3 point, Vector3 paddlePos)
        {
            if (ball == null || ball.Ball == null || ball.Ball.Table != Table) return; // another table (test scene)
            if (kind == BallGameFeel.ContactKind.Paddle)
            {
                _lastHitter = ball.SideOf(paddlePos);
                return;
            }
            if (!Active || kind != BallGameFeel.ContactKind.Table || Table == null) return;

            Vector3 lp = Table.Frame.ToLocal(point);
            foreach (var t in _targets)
            {
                if (t == null || !Shown(t)) continue;
                if (t.side != -_lastHitter) continue; // must be a return onto the other half
                var d = new Vector2(lp.x - t.local.x, lp.z - t.local.z);
                if (d.magnitude > radius) continue;
                Score(t, point);
                break;
            }
        }

        void Score(Target t, Vector3 point)
        {
            t.hitFlash = 1f;
            Vector3 lp = Table.Frame.ToLocal(point);
            bool bullseye = new Vector2(lp.x - t.local.x, lp.z - t.local.z).magnitude < radius / Rings;
            int pts = bullseye ? targetPoints * 2 : targetPoints;
            if (_score != null) _score.AddBonus(_lastHitter, pts, point);

            var popups = GetComponent<HitScorePopups>();
            if (popups != null && _cfg != null)
                popups.Show(bullseye ? "BULLSEYE!" : "TARGET!", Color.white, point + Vector3.up * 0.32f, 1.2f);
            var show = GetComponent<MatchPresentation>();
            if (show != null) show.Announce(bullseye ? "BULLSEYE!" : "TARGET!", 1.4f, _cfg != null ? _cfg.TeamColor(_lastHitter) : Color.white);
            if (_cfg != null && _cfg.Effects != null) _cfg.Effects.Impact(point, Vector3.up, 1f, Color.white);
            FanJump.CheerAll(1.6f, bullseye ? 0.9f : 0.6f);
            var audio = GetComponent<CrowdAudio>();
            if (audio != null) audio.Roar(bullseye ? 0.8f : 0.5f);
            Debug.Log($"[Game Feel] {(bullseye ? "Bullseye" : "Target")} +{pts}");

            // move to a new spot after a moment
            t.age = moveAfter - 0.6f;
        }
    }
}

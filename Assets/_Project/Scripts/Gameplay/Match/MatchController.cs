using System;
using System.Collections;
using PingPong.Core;
using UnityEngine;

namespace PingPong.Gameplay
{
    /// <summary>
    /// Scene adapter around <see cref="MatchRulesEngine"/>: feeds it what the ball does, owns the clock
    /// (rally timeout, pause between points) and places the ball for serves. Only the authority (solo, or
    /// the network state authority) runs the rules; other peers apply snapshots.
    /// </summary>
    public sealed class MatchController : MonoBehaviour
    {
        [SerializeField] PingPongBall _ball;
        [SerializeField] MatchRulesProfile _profile;

        MatchRulesEngine _engine;
        Coroutine _pendingServe;

        /// <summary>The table the ball plays on (the ball owns the reference).</summary>
        public PingPongTable Table => _ball.Table;
        public PingPongBall Ball => _ball;
        public MatchSnapshot Snapshot => Engine.State;
        public MatchPhase Phase => Engine.State.Phase;
        public Side Server => Engine.State.Server;
        public Side LastHitter => Engine.State.LastHitter;
        public int Score(Side side) => Engine.State.Score(side);
        /// <summary>Time.time of the last paddle hit (local, not part of the snapshot).</summary>
        public float LastHitTime { get; private set; }
        /// <summary>Increments on every paddle hit, so listeners can tell hits apart.</summary>
        public int HitCount { get; private set; }

        /// <summary>False on network clients: rules run on the state authority only.</summary>
        public bool IsAuthority
        {
            get => _isAuthority;
            set { _isAuthority = value; SyncWatching(); }
        }

        bool _isAuthority = true;

        public event Action Changed;
        public event Action<Side, PointReason> PointWon;
        public event Action<Side> GameWon;
        /// <summary>A serve touched the net and landed legally: replayed, no point.</summary>
        public event Action Let;
        public event Action<Side> PaddleHit;

        MatchRulesEngine Engine => _engine ??= CreateEngine();
        MatchPacing Pacing => _profile.Pacing;

        MatchRulesEngine CreateEngine()
        {
            var engine = new MatchRulesEngine(_profile.Rules);
            engine.Changed += () => { SyncWatching(); Changed?.Invoke(); };
            engine.PointAwarded += OnPointAwarded;
            engine.GameWon += w => GameWon?.Invoke(w);
            engine.Let += OnLet;
            return engine;
        }

        // Update (ball lost / rally timeout / dropped serve) matters only in these phases, so the component is
        // enabled just then. It listens from Awake to OnDestroy for that reason; coroutines keep running while disabled.
        void Awake()
        {
            _ball.TableBounce += OnTableBounce;
            _ball.OtherContact += OnOtherContact;
            SyncWatching();
        }

        void OnDestroy()
        {
            _ball.TableBounce -= OnTableBounce;
            _ball.OtherContact -= OnOtherContact;
            CancelPendingServe();
        }

        void SyncWatching() => enabled = IsAuthority && Phase is MatchPhase.Rally or MatchPhase.WaitingForServe;

        void OnValidate()
        {
            Validation.Require(_profile, "a Match Rules Profile", this);
            Validation.Require(_ball, "the Ball", this);
        }

        public void StartMatch(Side firstServer)
        {
            CancelPendingServe();
            Engine.StartMatch(firstServer);
            PlaceServeBall();
        }

        public void StopMatch()
        {
            CancelPendingServe();
            Engine.Stop();
        }

        public Vector3 ServePosition(Side side)
        {
            TableFrame f = Table.Frame;
            return f.PointOnSide(side, 0f, f.HalfLength + Pacing.ServeBehind, Pacing.ServeHeight);
        }

        public bool CanHit(Side player) => Engine.CanHit(player);

        /// <summary>
        /// The one way a player strikes the ball (local paddle, AI, and later the network): launches the
        /// ball from 'from' with 'velocity', raises <see cref="PaddleHit"/> and lets the rules judge the hit.
        /// </summary>
        public void Strike(Side player, Vector3 from, Vector3 velocity)
        {
            _ball.Launch(from, velocity);
            LastHitTime = Time.time;
            HitCount++;
            PaddleHit?.Invoke(player);
            if (IsAuthority) Engine.RegisterHit(player, Table.Frame.IsOverTable(from));
        }

        /// <summary>Network clients mirror the authority's rules state through this; point and game events fire as on the authority.</summary>
        public void ApplySnapshot(MatchSnapshot snapshot) => Engine.Restore(snapshot);

        void OnTableBounce(Side side)
        {
            if (IsAuthority) Engine.RegisterTableBounce(side);
        }

        void OnOtherContact(BallContact contact)
        {
            if (!IsAuthority) return;
            if (contact == BallContact.Net) { Engine.RegisterNetTouch(); return; }
            if (Phase == MatchPhase.WaitingForServe) PlaceServeBall(); // dropped while serving
            else Engine.RegisterBallDead();
        }

        void Update()
        {
            if (!IsAuthority) return;
            Vector3 local = Table.Frame.ToLocal(_ball.transform.position);
            switch (Phase)
            {
                case MatchPhase.Rally:
                    bool lost = local.y < Pacing.DeadBelow || local.sqrMagnitude > Pacing.DeadBeyond * Pacing.DeadBeyond;
                    if (lost || Time.time - LastHitTime > Pacing.RallyTimeout) Engine.RegisterBallDead();
                    break;
                case MatchPhase.WaitingForServe when !_ball.Body.isKinematic && local.y < Pacing.ServeResetBelow:
                    PlaceServeBall(); // a tossed serve fell without being hit
                    break;
            }
        }

        void OnPointAwarded(Side winner, PointReason reason)
        {
            PointWon?.Invoke(winner, reason);
            ScheduleNextServe();
        }

        void OnLet()
        {
            Let?.Invoke();
            ScheduleNextServe();
        }

        // The engine's state is final here. Only the authority schedules serves; clients follow snapshots.
        void ScheduleNextServe()
        {
            if (IsAuthority && Phase == MatchPhase.PointOver) _pendingServe = StartCoroutine(NextServeAfterPause());
        }

        IEnumerator NextServeAfterPause()
        {
            yield return new WaitForSeconds(Pacing.PointPause);
            _pendingServe = null;
            if (Engine.State.Phase != MatchPhase.PointOver) yield break;
            Engine.BeginServe();
            PlaceServeBall();
        }

        void PlaceServeBall()
        {
            if (Phase != MatchPhase.WaitingForServe) Engine.BeginServe();
            _ball.Hold(ServePosition(Server));
        }

        void CancelPendingServe()
        {
            if (_pendingServe == null) return;
            StopCoroutine(_pendingServe);
            _pendingServe = null;
        }
    }
}

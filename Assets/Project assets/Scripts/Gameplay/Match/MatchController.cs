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
        [SerializeField] PingPongTable _table;
        [SerializeField] PingPongBall _ball;
        [SerializeField] MatchRulesProfile _profile;

        MatchRulesEngine _engine;
        Coroutine _pendingServe;

        public PingPongTable Table => _table;
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
        public bool IsAuthority { get; set; } = true;

        public event Action Changed;
        public event Action<Side, PointReason> PointWon;
        public event Action<Side> GameWon;
        public event Action<Side> PaddleHit;

        MatchRulesEngine Engine => _engine ??= CreateEngine();
        MatchPacing Pacing => _profile.Pacing;

        MatchRulesEngine CreateEngine()
        {
            var engine = new MatchRulesEngine(_profile.Rules);
            engine.Changed += () => Changed?.Invoke();
            engine.PointAwarded += OnPointAwarded;
            engine.GameWon += w => GameWon?.Invoke(w);
            return engine;
        }

        void OnEnable()
        {
            _ball.Table = _table;
            _ball.TableBounce += OnTableBounce;
            _ball.OtherContact += OnOtherContact;
        }

        void OnDisable()
        {
            _ball.TableBounce -= OnTableBounce;
            _ball.OtherContact -= OnOtherContact;
            CancelPendingServe();
        }

        void OnValidate()
        {
            if (_profile == null) Debug.LogWarning($"{name}: assign a Match Rules Profile.", this);
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
            TableFrame f = _table.Frame;
            return f.ToWorld(new Vector3(0f, Pacing.ServeHeight, side.Sign() * (f.HalfLength + Pacing.ServeBehind)));
        }

        public bool CanHit(Side player) => Engine.CanHit(player);

        /// <summary>Called by paddles (and the AI) after they strike the ball.</summary>
        public void ReportPaddleHit(Side player, Vector3 hitPosition)
        {
            LastHitTime = Time.time;
            HitCount++;
            PaddleHit?.Invoke(player);
            if (IsAuthority) Engine.RegisterHit(player, _table.Frame.IsOverTable(hitPosition));
        }

        /// <summary>Network clients mirror the authority's rules state through this; point and game events fire as on the authority.</summary>
        public void ApplySnapshot(MatchSnapshot snapshot) => Engine.Restore(snapshot);

        void OnTableBounce(Side side)
        {
            if (IsAuthority) Engine.RegisterTableBounce(side);
        }

        void OnOtherContact(BallContact contact)
        {
            if (!IsAuthority || contact == BallContact.Net) return;
            if (Phase == MatchPhase.WaitingForServe) PlaceServeBall(); // dropped while serving
            else Engine.RegisterBallDead();
        }

        void Update()
        {
            if (!IsAuthority) return;
            Vector3 local = _table.Frame.ToLocal(_ball.transform.position);
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
            // The engine's state is final here. Only the authority schedules serves; clients follow snapshots.
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

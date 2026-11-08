using System;
using System.Collections;
using UnityEngine;
using PingPong.GameObjects;

namespace PingPong.Gameplay
{
    public enum MatchState { FreePlay, WaitingForServe, Rally, PointOver, GameOver }

    /// <summary>
    /// Table tennis rules: serve, bounce legality, scoring to 11 (win by 2), serve rotation.
    /// Only the authority (solo, or the network state authority) runs the rules.
    /// </summary>
    public class MatchController : MonoBehaviour
    {
        [SerializeField] PingPongTable _table;
        [SerializeField] PingPongBall _ball;
        [SerializeField] int _pointsToWin = 11;
        [SerializeField] float _pointPause = 1.5f;
        [Tooltip("Serve position: height above the table and distance behind the end line.")]
        [SerializeField] float _serveHeight = 0.3f;
        [SerializeField] float _serveBehind = 0.1f;
        [Tooltip("A rally with no paddle hit for this long ends as if the ball hit the floor.")]
        [SerializeField] float _rallyTimeout = 4f;

        public PingPongTable Table => _table;
        public PingPongBall Ball => _ball;
        public MatchState State { get; private set; } = MatchState.FreePlay;
        public int Server { get; private set; }
        public int LastHitter { get; private set; } = -1;
        public int Winner { get; private set; } = -1;
        public bool IsServing { get; private set; }
        public float LastHitTime { get; private set; }
        public readonly int[] Score = new int[2];

        /// <summary>False on network clients: rules run on the state authority only.</summary>
        public bool IsAuthority { get; set; } = true;

        public event Action Changed;
        /// <summary>Point won. Arguments: winner, reason.</summary>
        public event Action<int, string> PointWon;
        public event Action<int> GameWon;
        public event Action<int> PaddleHit;

        readonly int[] _bouncesOnSide = new int[2];
        int _serveBounces;
        int _servesThisTurn;
        Coroutine _pending;

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
        }

        public void StartMatch(int firstServer = 0)
        {
            if (_pending != null) StopCoroutine(_pending);
            Score[0] = Score[1] = 0;
            Winner = -1;
            Server = firstServer;
            _servesThisTurn = 0;
            PrepareServe();
        }

        public void StopMatch()
        {
            if (_pending != null) StopCoroutine(_pending);
            State = MatchState.FreePlay;
            Changed?.Invoke();
        }

        public Vector3 ServePosition(int side) =>
            _table.ToWorld(new Vector3(0f, _serveHeight, PingPongTable.SideSign(side) * (_table.HalfLength + _serveBehind)));

        /// <summary>Whether a paddle of this player may strike the ball right now.</summary>
        public bool CanHit(int player)
        {
            switch (State)
            {
                case MatchState.WaitingForServe: return player == Server;
                case MatchState.Rally: return player != LastHitter;
                case MatchState.FreePlay: return true;
                default: return false;
            }
        }

        /// <summary>Called by paddles (and the AI) after they strike the ball.</summary>
        public void ReportPaddleHit(int player, Vector3 hitPosition)
        {
            PaddleHit?.Invoke(player);
            if (!IsAuthority) return;
            LastHitTime = Time.time;

            if (State == MatchState.WaitingForServe)
            {
                if (player != Server) return;
                State = MatchState.Rally;
                IsServing = true;
                _serveBounces = 0;
                ResetBounces();
                LastHitter = player;
                Changed?.Invoke();
                return;
            }
            if (State != MatchState.Rally) { LastHitter = player; return; }

            if (player == LastHitter) { AwardPoint(1 - player, "Double hit"); return; }
            if (_bouncesOnSide[player] == 0)
            {
                // Hit before the ball bounced on this player's side.
                if (_table.IsOverTable(hitPosition)) AwardPoint(1 - player, "Volley over the table");
                else AwardPoint(player, "Long");
                return;
            }
            LastHitter = player;
            IsServing = false;
            ResetBounces();
            Changed?.Invoke();
        }

        void OnTableBounce(int side)
        {
            if (!IsAuthority || State != MatchState.Rally) return;
            int hitter = LastHitter, other = 1 - hitter;

            if (IsServing)
            {
                _serveBounces++;
                if (_serveBounces == 1 && side != hitter) { AwardPoint(other, "Serve must bounce on your side first"); return; }
                if (_serveBounces == 2)
                {
                    if (side == hitter) { AwardPoint(other, "Serve bounced twice on your side"); return; }
                    IsServing = false;
                    _bouncesOnSide[other] = 1;
                }
                return;
            }

            if (side == hitter) { AwardPoint(other, "Bounced on your own side"); return; }
            if (++_bouncesOnSide[other] >= 2) AwardPoint(hitter, "Double bounce");
        }

        void OnOtherContact(BallContact contact)
        {
            if (!IsAuthority || contact == BallContact.Net) return;
            if (State == MatchState.WaitingForServe) { PrepareServe(); return; } // dropped while serving
            if (State == MatchState.Rally) BallDead();
        }

        void BallDead()
        {
            int hitter = LastHitter, other = 1 - hitter;
            if (IsServing) AwardPoint(other, "Fault");
            else if (_bouncesOnSide[other] >= 1) AwardPoint(hitter, "Not returned");
            else AwardPoint(other, "Out");
        }

        void Update()
        {
            if (!IsAuthority) return;
            if (State == MatchState.Rally)
            {
                Vector3 l = _table.ToLocal(_ball.transform.position);
                if (l.y < -1.2f || l.sqrMagnitude > 64f || Time.time - LastHitTime > _rallyTimeout) BallDead();
            }
            else if (State == MatchState.WaitingForServe && !_ball.Body.isKinematic)
            {
                if (_table.ToLocal(_ball.transform.position).y < -0.5f) PrepareServe();
            }
        }

        void AwardPoint(int winner, string reason)
        {
            Score[winner]++;
            State = MatchState.PointOver;
            IsServing = false;

            int limit = Score[0] >= _pointsToWin - 1 && Score[1] >= _pointsToWin - 1 ? 1 : 2;
            if (++_servesThisTurn >= limit) { Server = 1 - Server; _servesThisTurn = 0; }

            PointWon?.Invoke(winner, reason);
            if (Score[winner] >= _pointsToWin && Score[winner] - Score[1 - winner] >= 2)
            {
                State = MatchState.GameOver;
                Winner = winner;
                GameWon?.Invoke(winner);
                Changed?.Invoke();
                return;
            }
            Changed?.Invoke();
            _pending = StartCoroutine(NextServeAfterPause());
        }

        IEnumerator NextServeAfterPause()
        {
            yield return new WaitForSeconds(_pointPause);
            _pending = null;
            PrepareServe();
        }

        void PrepareServe()
        {
            State = MatchState.WaitingForServe;
            LastHitter = -1;
            IsServing = false;
            ResetBounces();
            _ball.Hold(ServePosition(Server));
            Changed?.Invoke();
        }

        void ResetBounces() => _bouncesOnSide[0] = _bouncesOnSide[1] = 0;

        /// <summary>Network clients mirror the authority's state through this.</summary>
        public void ApplyRemoteState(MatchState state, int server, int score0, int score1, int lastHitter, int winner)
        {
            bool changed = state != State || server != Server || score0 != Score[0] || score1 != Score[1] || winner != Winner;
            State = state; Server = server; Score[0] = score0; Score[1] = score1; LastHitter = lastHitter; Winner = winner;
            if (changed) Changed?.Invoke();
        }
    }
}

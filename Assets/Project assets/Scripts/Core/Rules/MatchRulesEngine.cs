using System;

namespace PingPong.Core
{
    /// <summary>
    /// Table tennis rules as a deterministic state machine: serve legality, bounces, volleys,
    /// scoring and serve rotation. No engine, time or physics dependencies: callers report what the
    /// ball did and the engine decides. All state lives in <see cref="State"/>, so it can be
    /// snapshotted and restored (networking, replays, tests).
    /// </summary>
    public sealed class MatchRulesEngine
    {
        MatchRules _rules;
        MatchSnapshot _s = MatchSnapshot.Idle;

        public MatchRulesEngine(MatchRules rules)
        {
            if (!rules.IsValid) throw new ArgumentException("Invalid match rules.", nameof(rules));
            _rules = rules;
        }

        public MatchRules Rules => _rules;
        public MatchSnapshot State => _s;

        /// <summary>Raised after any state change.</summary>
        public event Action Changed;
        /// <summary>
        /// Raised when a point is awarded, after the state is final (score, next server, game over).
        /// Also raised by <see cref="Restore"/> when the snapshot carries a new point.
        /// </summary>
        public event Action<Side, PointReason> PointAwarded;
        /// <summary>Raised after <see cref="PointAwarded"/> when that point won the game.</summary>
        public event Action<Side> GameWon;

        public void StartMatch(Side firstServer)
        {
            if (!firstServer.IsPlayer()) throw new ArgumentException("A player must serve first.", nameof(firstServer));
            _s = MatchSnapshot.Idle;
            _s.Server = firstServer;
            BeginServe();
        }

        public void Stop()
        {
            _s.Phase = MatchPhase.FreePlay;
            _s.IsServing = false;
            Changed?.Invoke();
        }

        /// <summary>Sets up the next serve (after a point pause, or when a held serve ball is dropped).</summary>
        public void BeginServe()
        {
            if (_s.Phase == MatchPhase.GameOver) return;
            _s.Phase = MatchPhase.WaitingForServe;
            _s.LastHitter = Side.None;
            _s.IsServing = false;
            _s.ServeBounces = 0;
            ResetBounces();
            Changed?.Invoke();
        }

        /// <summary>Whether this player may strike the ball now.</summary>
        public bool CanHit(Side player) => _s.Phase switch
        {
            MatchPhase.WaitingForServe => player == _s.Server,
            MatchPhase.Rally => player.IsPlayer() && player != _s.LastHitter,
            MatchPhase.FreePlay => true,
            _ => false,
        };

        /// <summary>A paddle struck the ball. overTable: the contact was above the playing surface.</summary>
        public void RegisterHit(Side player, bool overTable)
        {
            if (!player.IsPlayer()) return;
            switch (_s.Phase)
            {
                case MatchPhase.WaitingForServe:
                    if (player != _s.Server) return;
                    _s.Phase = MatchPhase.Rally;
                    _s.IsServing = true;
                    _s.ServeBounces = 0;
                    ResetBounces();
                    _s.LastHitter = player;
                    Changed?.Invoke();
                    return;

                case MatchPhase.Rally:
                    if (player == _s.LastHitter) { Award(player.Opponent(), PointReason.DoubleHit); return; }
                    if (_s.Bounces(player) == 0)
                    {
                        // Struck before the ball bounced on this player's side.
                        if (overTable) Award(player.Opponent(), PointReason.Volley);
                        else Award(player, PointReason.Long);
                        return;
                    }
                    _s.LastHitter = player;
                    _s.IsServing = false;
                    ResetBounces();
                    Changed?.Invoke();
                    return;

                case MatchPhase.FreePlay:
                    _s.LastHitter = player;
                    Changed?.Invoke();
                    return;
            }
        }

        /// <summary>The ball bounced on the playing surface on this side.</summary>
        public void RegisterTableBounce(Side side)
        {
            if (_s.Phase != MatchPhase.Rally || !side.IsPlayer()) return;
            Side hitter = _s.LastHitter, other = hitter.Opponent();

            if (_s.IsServing)
            {
                _s.ServeBounces++;
                if (_s.ServeBounces == 1 && side != hitter) { Award(other, PointReason.ServeMissedOwnSide); return; }
                if (_s.ServeBounces == 2)
                {
                    if (side == hitter) { Award(other, PointReason.ServeBouncedTwiceOnOwnSide); return; }
                    _s.IsServing = false;
                    SetBounces(other, 1);
                }
                return;
            }

            if (side == hitter) { Award(other, PointReason.BouncedOnOwnSide); return; }
            SetBounces(other, _s.Bounces(other) + 1);
            if (_s.Bounces(other) >= 2) Award(hitter, PointReason.DoubleBounce);
        }

        /// <summary>The ball is out of play: floor, room, out of bounds or rally timeout.</summary>
        public void RegisterBallDead()
        {
            if (_s.Phase != MatchPhase.Rally) return;
            Side hitter = _s.LastHitter, other = hitter.Opponent();
            if (_s.IsServing) Award(other, PointReason.ServeFault);
            else if (_s.Bounces(other) >= 1) Award(hitter, PointReason.NotReturned);
            else Award(other, PointReason.Out);
        }

        /// <summary>
        /// Replaces the whole state (network clients, replays) and raises the events a local peer would
        /// have seen: <see cref="PointAwarded"/> if the snapshot carries a new point, <see cref="GameWon"/>
        /// if it ends the game, then <see cref="Changed"/>. If several points arrive in one snapshot, only
        /// the latest is reported (the score is still exact).
        /// </summary>
        public void Restore(MatchSnapshot snapshot)
        {
            if (snapshot.Equals(_s)) return;
            MatchSnapshot previous = _s;
            _s = snapshot;
            bool newPoint = snapshot.PointsPlayed > previous.PointsPlayed && snapshot.LastPointWinner.IsPlayer();
            if (newPoint) PointAwarded?.Invoke(snapshot.LastPointWinner, snapshot.LastPointReason);
            if (snapshot.Phase == MatchPhase.GameOver && previous.Phase != MatchPhase.GameOver && snapshot.Winner.IsPlayer())
                GameWon?.Invoke(snapshot.Winner);
            Changed?.Invoke();
        }

        void Award(Side winner, PointReason reason)
        {
            // Finish the whole state first, so every event handler sees the final, consistent state.
            if (winner == Side.Near) _s.ScoreNear++; else _s.ScoreFar++;
            _s.PointsPlayed++;
            _s.LastPointWinner = winner;
            _s.LastPointReason = reason;
            _s.IsServing = false;

            bool deuce = _s.ScoreNear >= _rules.PointsToWin - 1 && _s.ScoreFar >= _rules.PointsToWin - 1;
            int servesPerTurn = deuce ? _rules.DeuceServesPerTurn : _rules.ServesPerTurn;
            if (++_s.ServesThisTurn >= servesPerTurn)
            {
                _s.Server = _s.Server.Opponent();
                _s.ServesThisTurn = 0;
            }

            int lead = _s.Score(winner) - _s.Score(winner.Opponent());
            bool gameWon = _s.Score(winner) >= _rules.PointsToWin && lead >= _rules.WinBy;
            _s.Phase = gameWon ? MatchPhase.GameOver : MatchPhase.PointOver;
            if (gameWon) _s.Winner = winner;

            PointAwarded?.Invoke(winner, reason);
            if (gameWon) GameWon?.Invoke(winner);
            Changed?.Invoke();
        }

        void ResetBounces() => _s.BouncesNear = _s.BouncesFar = 0;

        void SetBounces(Side side, int value)
        {
            if (side == Side.Near) _s.BouncesNear = value; else _s.BouncesFar = value;
        }
    }
}

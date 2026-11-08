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
        /// <summary>Raised when a point is awarded (before <see cref="Changed"/>).</summary>
        public event Action<Side, PointReason> PointAwarded;
        /// <summary>Raised when the game is won (before <see cref="Changed"/>).</summary>
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

        /// <summary>Replaces the whole state (network clients, replays). Raises <see cref="Changed"/> if it differs.</summary>
        public void Restore(MatchSnapshot snapshot)
        {
            if (snapshot.Equals(_s)) return;
            _s = snapshot;
            Changed?.Invoke();
        }

        void Award(Side winner, PointReason reason)
        {
            if (winner == Side.Near) _s.ScoreNear++; else _s.ScoreFar++;
            _s.Phase = MatchPhase.PointOver;
            _s.IsServing = false;

            bool deuce = _s.ScoreNear >= _rules.PointsToWin - 1 && _s.ScoreFar >= _rules.PointsToWin - 1;
            int servesPerTurn = deuce ? _rules.DeuceServesPerTurn : _rules.ServesPerTurn;
            if (++_s.ServesThisTurn >= servesPerTurn)
            {
                _s.Server = _s.Server.Opponent();
                _s.ServesThisTurn = 0;
            }

            PointAwarded?.Invoke(winner, reason);

            int lead = _s.Score(winner) - _s.Score(winner.Opponent());
            if (_s.Score(winner) >= _rules.PointsToWin && lead >= _rules.WinBy)
            {
                _s.Phase = MatchPhase.GameOver;
                _s.Winner = winner;
                GameWon?.Invoke(winner);
            }
            Changed?.Invoke();
        }

        void ResetBounces() => _s.BouncesNear = _s.BouncesFar = 0;

        void SetBounces(Side side, int value)
        {
            if (side == Side.Near) _s.BouncesNear = value; else _s.BouncesFar = value;
        }
    }
}

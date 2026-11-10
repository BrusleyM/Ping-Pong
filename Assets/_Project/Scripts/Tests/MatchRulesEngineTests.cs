using System.Collections.Generic;
using NUnit.Framework;
using PingPong.Core;

namespace PingPong.Tests
{
    public sealed class MatchRulesEngineTests
    {
        MatchRulesEngine _engine;
        readonly List<(Side winner, PointReason reason)> _points = new List<(Side, PointReason)>();
        Side _gameWinner;

        [SetUp]
        public void SetUp()
        {
            _engine = new MatchRulesEngine(MatchRules.Standard);
            _points.Clear();
            _gameWinner = Side.None;
            _engine.PointAwarded += (w, r) => _points.Add((w, r));
            _engine.GameWon += w => _gameWinner = w;
            _engine.StartMatch(Side.Near);
        }

        MatchSnapshot S => _engine.State;
        (Side winner, PointReason reason) LastPoint => _points[_points.Count - 1];

        /// <summary>Near serves legally: own side, then the receiver's side.</summary>
        void LegalServe()
        {
            Side server = S.Server;
            _engine.RegisterHit(server, overTable: false);
            _engine.RegisterTableBounce(server);
            _engine.RegisterTableBounce(server.Opponent());
        }

        /// <summary>Plays one point that 'winner' wins, then sets up the next serve.</summary>
        void WinPoint(Side winner)
        {
            Side server = S.Server;
            _engine.RegisterHit(server, false);
            if (winner == server)
            {
                _engine.RegisterTableBounce(server);
                _engine.RegisterTableBounce(server.Opponent());
                _engine.RegisterBallDead(); // not returned
            }
            else
            {
                _engine.RegisterTableBounce(server.Opponent()); // serve missed its own side
            }
            _engine.BeginServe();
        }

        void PlayTo(int near, int far)
        {
            while (S.ScoreNear < near) WinPoint(Side.Near);
            while (S.ScoreFar < far) WinPoint(Side.Far);
        }

        [Test]
        public void StartMatch_WaitsForTheFirstServer()
        {
            Assert.That(S.Phase, Is.EqualTo(MatchPhase.WaitingForServe));
            Assert.That(S.Server, Is.EqualTo(Side.Near));
            Assert.That(_engine.CanHit(Side.Near), Is.True);
            Assert.That(_engine.CanHit(Side.Far), Is.False);
        }

        [Test]
        public void LegalServe_StartsRallyAndReceiverMayHit()
        {
            LegalServe();
            Assert.That(S.Phase, Is.EqualTo(MatchPhase.Rally));
            Assert.That(S.IsServing, Is.False);
            Assert.That(_engine.CanHit(Side.Far), Is.True);
            Assert.That(_engine.CanHit(Side.Near), Is.False);
            Assert.That(_points, Is.Empty);
        }

        [Test]
        public void Serve_FirstBounceOnReceiverSide_IsReceiversPoint()
        {
            _engine.RegisterHit(Side.Near, false);
            _engine.RegisterTableBounce(Side.Far);
            Assert.That(LastPoint, Is.EqualTo((Side.Far, PointReason.ServeMissedOwnSide)));
        }

        [Test]
        public void Serve_BouncingTwiceOnServerSide_IsReceiversPoint()
        {
            _engine.RegisterHit(Side.Near, false);
            _engine.RegisterTableBounce(Side.Near);
            _engine.RegisterTableBounce(Side.Near);
            Assert.That(LastPoint, Is.EqualTo((Side.Far, PointReason.ServeBouncedTwiceOnOwnSide)));
        }

        [Test]
        public void Serve_DeadBeforeReachingReceiver_IsFault()
        {
            _engine.RegisterHit(Side.Near, false);
            _engine.RegisterTableBounce(Side.Near);
            _engine.RegisterBallDead();
            Assert.That(LastPoint, Is.EqualTo((Side.Far, PointReason.ServeFault)));
        }

        [Test]
        public void Rally_SecondBounceOnReceiverSide_IsDoubleBounce()
        {
            LegalServe();
            _engine.RegisterTableBounce(Side.Far);
            Assert.That(LastPoint, Is.EqualTo((Side.Near, PointReason.DoubleBounce)));
        }

        [Test]
        public void Rally_ReturnBouncingOnOwnSide_LosesThePoint()
        {
            LegalServe();
            _engine.RegisterHit(Side.Far, false);
            _engine.RegisterTableBounce(Side.Far);
            Assert.That(LastPoint, Is.EqualTo((Side.Near, PointReason.BouncedOnOwnSide)));
        }

        [Test]
        public void Rally_UnreturnedBall_IsHittersPoint()
        {
            LegalServe();
            _engine.RegisterBallDead();
            Assert.That(LastPoint, Is.EqualTo((Side.Near, PointReason.NotReturned)));
        }

        [Test]
        public void Rally_ReturnThatMissesTheTable_IsOut()
        {
            LegalServe();
            _engine.RegisterHit(Side.Far, false);
            _engine.RegisterBallDead();
            Assert.That(LastPoint, Is.EqualTo((Side.Near, PointReason.Out)));
        }

        [Test]
        public void Volley_OverTheTable_LosesThePoint()
        {
            _engine.RegisterHit(Side.Near, false);
            _engine.RegisterTableBounce(Side.Near);
            _engine.RegisterTableBounce(Side.Far);
            _engine.RegisterHit(Side.Far, false);
            _engine.RegisterHit(Side.Near, overTable: true); // before it bounced on Near's side
            Assert.That(LastPoint, Is.EqualTo((Side.Far, PointReason.Volley)));
        }

        [Test]
        public void Volley_BehindTheTable_IsLongForTheVolleyer()
        {
            LegalServe();
            _engine.RegisterHit(Side.Far, false);
            _engine.RegisterHit(Side.Near, overTable: false);
            Assert.That(LastPoint, Is.EqualTo((Side.Near, PointReason.Long)));
        }

        [Test]
        public void DoubleHit_LosesThePoint()
        {
            _engine.RegisterHit(Side.Near, false);
            _engine.RegisterHit(Side.Near, false);
            Assert.That(LastPoint, Is.EqualTo((Side.Far, PointReason.DoubleHit)));
        }

        [Test]
        public void Serve_RotatesEveryTwoPoints()
        {
            var servers = new List<Side>();
            for (int i = 0; i < 6; i++) { servers.Add(S.Server); WinPoint(Side.Near); }
            Assert.That(servers, Is.EqualTo(new[] { Side.Near, Side.Near, Side.Far, Side.Far, Side.Near, Side.Near }));
        }

        [Test]
        public void Serve_RotatesEveryPointAtDeuce()
        {
            PlayTo(10, 10);
            Side first = S.Server;
            WinPoint(Side.Near);
            Assert.That(S.Server, Is.EqualTo(first.Opponent()));
            WinPoint(Side.Far);
            Assert.That(S.Server, Is.EqualTo(first));
        }

        [Test]
        public void Game_IsWonAtElevenWithATwoPointLead()
        {
            PlayTo(10, 5);
            WinPoint(Side.Near);
            Assert.That(S.Phase, Is.EqualTo(MatchPhase.GameOver));
            Assert.That(_gameWinner, Is.EqualTo(Side.Near));
            Assert.That(S.Winner, Is.EqualTo(Side.Near));
        }

        [Test]
        public void Game_NeedsTwoPointLeadAfterDeuce()
        {
            PlayTo(10, 10);
            WinPoint(Side.Near);
            Assert.That(S.Phase, Is.Not.EqualTo(MatchPhase.GameOver), "11-10 is not a win");
            WinPoint(Side.Far);
            WinPoint(Side.Far);
            Assert.That(S.Phase, Is.Not.EqualTo(MatchPhase.GameOver), "11-12 is not a win");
            WinPoint(Side.Far);
            Assert.That(S.Phase, Is.EqualTo(MatchPhase.GameOver));
            Assert.That(S.Winner, Is.EqualTo(Side.Far));
            Assert.That((S.ScoreNear, S.ScoreFar), Is.EqualTo((11, 13)));
        }

        [Test]
        public void GameOver_IgnoresFurtherPlay()
        {
            PlayTo(11, 0);
            MatchSnapshot before = S;
            _engine.RegisterHit(Side.Near, false);
            _engine.RegisterBallDead();
            _engine.BeginServe();
            Assert.That(S, Is.EqualTo(before));
        }

        [Test]
        public void Restore_ReproducesTheExactState()
        {
            PlayTo(3, 2);
            LegalServe();
            MatchSnapshot snapshot = S;

            var client = new MatchRulesEngine(MatchRules.Standard);
            client.Restore(snapshot);
            Assert.That(client.State, Is.EqualTo(snapshot));

            // Both continue identically.
            _engine.RegisterBallDead();
            client.RegisterBallDead();
            Assert.That(client.State, Is.EqualTo(_engine.State));
        }

        [Test]
        public void PointEvents_SeeTheFinalState()
        {
            var seen = new List<MatchSnapshot>();
            _engine.PointAwarded += (w, r) => seen.Add(_engine.State);

            WinPoint(Side.Near); // 1-0: rally over, same server
            WinPoint(Side.Near); // 2-0: server rotates
            Assert.That(seen[0].Phase, Is.EqualTo(MatchPhase.PointOver));
            Assert.That(seen[1].Server, Is.EqualTo(Side.Far), "server already rotated when the event fires");
            Assert.That((seen[1].PointsPlayed, seen[1].LastPointWinner), Is.EqualTo((2, Side.Near)));

            PlayTo(10, 0);
            WinPoint(Side.Near); // game point
            MatchSnapshot last = seen[seen.Count - 1];
            Assert.That(last.Phase, Is.EqualTo(MatchPhase.GameOver), "game over is already decided when the point event fires");
            Assert.That(last.Winner, Is.EqualTo(Side.Near));
        }

        [Test]
        public void Restore_RaisesPointAndGameEventsOnAClient()
        {
            var client = new MatchRulesEngine(MatchRules.Standard);
            var clientPoints = new List<(Side, PointReason)>();
            Side clientWinner = Side.None;
            client.PointAwarded += (w, r) => clientPoints.Add((w, r));
            client.GameWon += w => clientWinner = w;

            client.Restore(S); // waiting for serve: no point yet
            Assert.That(clientPoints, Is.Empty);

            _engine.RegisterHit(Side.Near, false);
            _engine.RegisterTableBounce(Side.Far); // serve fault
            client.Restore(S);
            Assert.That(clientPoints, Is.EqualTo(new[] { (Side.Far, PointReason.ServeMissedOwnSide) }));

            client.Restore(S); // same snapshot again: no duplicate event
            Assert.That(clientPoints.Count, Is.EqualTo(1));

            _engine.BeginServe();
            PlayTo(0, 11);
            client.Restore(S);
            Assert.That(clientWinner, Is.EqualTo(Side.Far));
            Assert.That(clientPoints[clientPoints.Count - 1].Item1, Is.EqualTo(Side.Far), "only the latest point is reported");
        }

        [Test]
        public void InvalidRules_AreRejected()
        {
            Assert.Throws<System.ArgumentException>(() => new MatchRulesEngine(new MatchRules()));
        }

        [Test]
        public void ServeTouchingTheNet_ThenLandingLegally_IsALet()
        {
            int lets = 0;
            _engine.Let += () => lets++;
            Side server = S.Server;
            int servesBefore = S.ServesThisTurn;

            _engine.RegisterHit(server, overTable: false);
            _engine.RegisterTableBounce(server);
            _engine.RegisterNetTouch();
            _engine.RegisterTableBounce(server.Opponent());

            Assert.That(lets, Is.EqualTo(1));
            Assert.That(_points, Is.Empty, "no point for a let");
            Assert.That(S.Phase, Is.EqualTo(MatchPhase.PointOver));
            Assert.That(S.LetsPlayed, Is.EqualTo(1));

            _engine.BeginServe();
            Assert.That(S.Server, Is.EqualTo(server), "the same player serves again");
            Assert.That(S.ServesThisTurn, Is.EqualTo(servesBefore), "a let doesn't use up a serve");
            Assert.That(S.ServeTouchedNet, Is.False);
        }

        [Test]
        public void ServeTouchingTheNet_VolleyedByTheReceiverOverTheTable_IsALet()
        {
            int lets = 0;
            _engine.Let += () => lets++;
            Side server = S.Server;
            _engine.RegisterHit(server, false);
            _engine.RegisterTableBounce(server);
            _engine.RegisterNetTouch();
            _engine.RegisterHit(server.Opponent(), overTable: true);

            Assert.That(lets, Is.EqualTo(1));
            Assert.That(_points, Is.Empty);
            Assert.That(S.Phase, Is.EqualTo(MatchPhase.PointOver));
        }

        [Test]
        public void ServeTouchingTheNet_ThatFaults_IsStillAFault()
        {
            Side server = S.Server;
            _engine.RegisterHit(server, false);
            _engine.RegisterTableBounce(server);
            _engine.RegisterNetTouch();
            _engine.RegisterTableBounce(server); // fell back onto the server's side
            Assert.That(LastPoint, Is.EqualTo((server.Opponent(), PointReason.ServeBouncedTwiceOnOwnSide)));
            Assert.That(S.LetsPlayed, Is.Zero);
        }

        [Test]
        public void NetTouch_InARally_StaysInPlay()
        {
            LegalServe();
            Side receiver = S.Server.Opponent();
            _engine.RegisterHit(receiver, false);
            _engine.RegisterNetTouch();
            _engine.RegisterTableBounce(receiver.Opponent());
            Assert.That(S.Phase, Is.EqualTo(MatchPhase.Rally));
            Assert.That(S.LetsPlayed, Is.Zero);
            Assert.That(_points, Is.Empty);
        }

        [Test]
        public void Restore_RaisesLetOnAClient()
        {
            Side server = S.Server;
            _engine.RegisterHit(server, false);
            _engine.RegisterTableBounce(server);
            _engine.RegisterNetTouch();
            _engine.RegisterTableBounce(server.Opponent());

            var client = new MatchRulesEngine(MatchRules.Standard);
            client.StartMatch(Side.Near);
            int lets = 0;
            client.Let += () => lets++;
            client.Restore(S);
            Assert.That(lets, Is.EqualTo(1));
            Assert.That(client.State, Is.EqualTo(S));
        }
    }
}

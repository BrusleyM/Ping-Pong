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
        public void InvalidRules_AreRejected()
        {
            Assert.Throws<System.ArgumentException>(() => new MatchRulesEngine(new MatchRules()));
        }
    }
}

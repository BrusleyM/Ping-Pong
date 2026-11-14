using NUnit.Framework;
using PingPong.Core;

namespace PingPong.Tests
{
    public sealed class MatchPhaseRulesTests
    {
        [TestCase(MatchPhase.FreePlay, true)]
        [TestCase(MatchPhase.WaitingForServe, true)]
        [TestCase(MatchPhase.Rally, true)]
        [TestCase(MatchPhase.PointOver, false)]
        [TestCase(MatchPhase.GameOver, false)]
        public void AllowsHits_PerPhase(MatchPhase phase, bool expected) => Assert.That(phase.AllowsHits(), Is.EqualTo(expected));

        [TestCase(MatchPhase.FreePlay, false)]
        [TestCase(MatchPhase.WaitingForServe, true)]
        [TestCase(MatchPhase.Rally, true)]
        [TestCase(MatchPhase.PointOver, true)]
        [TestCase(MatchPhase.GameOver, false)]
        public void NeedsWatching_PerPhase(MatchPhase phase, bool expected) => Assert.That(phase.NeedsWatching(), Is.EqualTo(expected));

        // The hitter sleeps when AllowsHits is false, so the engine must never allow a hit then.
        [Test]
        public void TheEngineAllowsNoHitInAPhaseThatSleepsTheHitter()
        {
            var engine = new MatchRulesEngine(MatchRules.Standard);
            foreach (MatchPhase phase in System.Enum.GetValues(typeof(MatchPhase)))
            {
                var snapshot = MatchSnapshot.Idle;
                snapshot.Phase = phase;
                snapshot.Server = Side.Near;
                snapshot.LastHitter = Side.Far;
                engine.Restore(snapshot);
                bool anyCanHit = engine.CanHit(Side.Near) || engine.CanHit(Side.Far);
                if (!phase.AllowsHits()) Assert.That(anyCanHit, Is.False, $"{phase}: the hitter sleeps but the engine would allow a hit");
            }
        }
    }
}

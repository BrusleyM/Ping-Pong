using System;
using System.Collections.Generic;
using PingPong.Tutorial;

namespace PingPong.Tests
{
    internal sealed class TestSignal
    {
        private event Action Raised;

        public TestSignal()
        {
            Condition = new EventCondition(satisfy =>
            {
                Raised += satisfy;
                return () => Raised -= satisfy;
            });
        }

        public EventCondition Condition { get; }
        public int ListenerCount => Raised?.GetInvocationList().Length ?? 0;

        public void Raise() => Raised?.Invoke();
    }

    internal sealed class RecordingCues : ITutorialCues
    {
        public List<string> Journal { get; } = new List<string>();

        public void ShowPrompt(string text) => Journal.Add($"show {text}");

        public void HidePrompt() => Journal.Add("hide");
    }

    internal sealed class TutorialScenario
    {
        public TutorialScenario()
        {
            PaddleHeld = new TestSignal();
            MenuOpened = new TestSignal();
            BallHit = new TestSignal();
            PointWon = new TestSignal();
            Cues = new RecordingCues();
            Signals = new TutorialSignals(PaddleHeld.Condition, MenuOpened.Condition, BallHit.Condition, PointWon.Condition);
            Timing = new TutorialTiming(1f, 2f, 3f);
        }

        public TestSignal PaddleHeld { get; }
        public TestSignal MenuOpened { get; }
        public TestSignal BallHit { get; }
        public TestSignal PointWon { get; }
        public RecordingCues Cues { get; }
        public TutorialSignals Signals { get; }
        public TutorialTiming Timing { get; }

        public Framework.Sequencing.SequenceRunner BuildRunner() => TutorialPlan.Build(Signals, Cues, Timing);
    }
}

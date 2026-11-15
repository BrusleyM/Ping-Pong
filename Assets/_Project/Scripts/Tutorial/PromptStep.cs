using System;
using Framework.Sequencing;

namespace PingPong.Tutorial
{
    public sealed class PromptStep : StepBase
    {
        private readonly string _text;
        private readonly ITutorialCues _cues;
        private readonly IStepCondition _condition;
        private readonly float _seconds;
        private float _elapsedSeconds;

        private PromptStep(string name, string text, ITutorialCues cues, IStepCondition condition, float seconds) : base(name)
        {
            _text = text;
            _cues = cues;
            _condition = condition;
            _seconds = seconds;
        }

        public static PromptStep ForSeconds(string text, ITutorialCues cues, float seconds)
        {
            return new PromptStep(NameOf(text), text, cues, null, seconds);
        }

        public static PromptStep UntilSatisfied(string text, ITutorialCues cues, IStepCondition condition)
        {
            return new PromptStep(NameOf(text), text, cues, condition, 0f);
        }

        private static string NameOf(string text)
        {
            return "Prompt: " + text;
        }

        protected override void OnEnter()
        {
            _elapsedSeconds = 0f;
            _cues.ShowPrompt(_text);

            if (_condition != null)
            {
                _condition.Satisfied += Finish;
                _condition.Arm();
            }
            else if (_seconds <= 0f)
            {
                Finish();
            }
        }

        protected override void OnAdvance(float deltaSeconds)
        {
            if (_condition != null)
            {
                return;
            }

            _elapsedSeconds += deltaSeconds;
            if (_elapsedSeconds >= _seconds)
            {
                Finish();
            }
        }

        protected override void OnExit(ExitReason reason)
        {
            if (_condition != null)
            {
                _condition.Satisfied -= Finish;
                _condition.Disarm();
            }

            _cues.HidePrompt();
        }
    }
}

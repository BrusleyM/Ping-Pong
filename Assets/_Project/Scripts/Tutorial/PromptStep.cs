using System;
using Framework.Sequencing;
using PingPong.Gameplay;

namespace PingPong.Tutorial
{
    public sealed class PromptStep : StepBase
    {
        private readonly PromptText _text;
        private readonly IInputModeSource _modes;
        private readonly ITutorialCues _cues;
        private readonly IStepCondition _condition;
        private readonly float _seconds;
        private float _elapsedSeconds;
        private string _shown;

        private PromptStep(string name, PromptText text, IInputModeSource modes, ITutorialCues cues, IStepCondition condition, float seconds) : base(name)
        {
            _text = text;
            _modes = modes;
            _cues = cues;
            _condition = condition;
            _seconds = seconds;
        }

        public static PromptStep ForSeconds(PromptText text, IInputModeSource modes, ITutorialCues cues, float seconds)
        {
            return new PromptStep(NameOf(text), text, modes, cues, null, seconds);
        }

        public static PromptStep UntilSatisfied(PromptText text, IInputModeSource modes, ITutorialCues cues, IStepCondition condition)
        {
            return new PromptStep(NameOf(text), text, modes, cues, condition, 0f);
        }

        private static string NameOf(PromptText text)
        {
            return "Prompt: " + text.For(InputMode.Controllers);
        }

        private void Show(InputMode mode)
        {
            var text = _text.For(mode);
            if (text == _shown)
            {
                return;
            }

            _shown = text;
            _cues.ShowPrompt(text);
        }

        protected override void OnEnter()
        {
            _elapsedSeconds = 0f;
            _shown = null;
            Show(_modes.Current);
            _modes.Changed += Show;

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
            _modes.Changed -= Show;
            if (_condition != null)
            {
                _condition.Satisfied -= Finish;
                _condition.Disarm();
            }

            _cues.HidePrompt();
        }
    }
}

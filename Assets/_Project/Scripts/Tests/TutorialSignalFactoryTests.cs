using System;
using NUnit.Framework;
using PingPong.Core;
using PingPong.Gameplay;
using PingPong.Tutorial;

namespace PingPong.Tests
{
    public class TutorialSignalFactoryTests
    {
        private sealed class FakePlayerCommands : IPlayerCommands
        {
            public event Action<Hand> GrabPerformed;
            public event Action MenuPerformed;

            public int MenuListenerCount => MenuPerformed?.GetInvocationList().Length ?? 0;


            public void PressMenu() => MenuPerformed?.Invoke();
        }

        private sealed class FakeMenuGesture : IMenuGesture
        {
            public event Action Performed;

            public int ListenerCount => Performed?.GetInvocationList().Length ?? 0;

            public void Perform() => Performed?.Invoke();
        }

        private sealed class FakeMatch
        {
            public bool IsRunning;
            public event Action Started;
            public event Action<Side> PaddleHit;
            public event Action<Side, PointReason> PointWon;

            public int StartedListenerCount => Started?.GetInvocationList().Length ?? 0;
            public int PaddleHitListenerCount => PaddleHit?.GetInvocationList().Length ?? 0;
            public int PointWonListenerCount => PointWon?.GetInvocationList().Length ?? 0;

            public void Start() => Started?.Invoke();
            public void Hit(Side side) => PaddleHit?.Invoke(side);
            public void WinPoint(Side side) => PointWon?.Invoke(side, default);

            public MatchFeed Feed => new MatchFeed(
                () => IsRunning,
                handler => Started += handler,
                handler => Started -= handler,
                handler => PaddleHit += handler,
                handler => PaddleHit -= handler,
                handler => PointWon += handler,
                handler => PointWon -= handler);
        }

        private sealed class FakePaddle
        {
            public Hand HeldBy = Hand.None;
            public event Action<Hand> HeldByChanged;

            public int ListenerCount => HeldByChanged?.GetInvocationList().Length ?? 0;

            public void ChangeHeldBy(Hand hand)
            {
                HeldBy = hand;
                HeldByChanged?.Invoke(hand);
            }

            public PaddleHoldFeed Feed => new PaddleHoldFeed(
                () => HeldBy,
                handler => HeldByChanged += handler,
                handler => HeldByChanged -= handler);
        }

        private const Side LocalSide = Side.Near;

        private FakePaddle _paddle;
        private FakePlayerCommands _commands;
        private FakeMenuGesture _gesture;
        private FakeMatch _match;
        private TutorialSignals _signals;

        [SetUp]
        public void SetUp()
        {
            _paddle = new FakePaddle();
            _commands = new FakePlayerCommands();
            _gesture = new FakeMenuGesture();
            _match = new FakeMatch();
            _signals = TutorialSignalFactory.Create(_paddle.Feed, _commands, _gesture, _match.Feed, LocalSide);
        }

        private static int CountSatisfied(Framework.Sequencing.IStepCondition condition, Action raise)
        {
            var count = 0;
            condition.Satisfied += () => count++;
            condition.Arm();
            raise();
            condition.Disarm();
            return count;
        }

        [Test]
        public void Picking_up_the_paddle_with_the_right_hand_satisfies_the_paddle_held_signal()
        {
            Assert.AreEqual(1, CountSatisfied(_signals.PaddleHeld, () => _paddle.ChangeHeldBy(Hand.Right)));
        }

        [Test]
        public void Picking_up_the_paddle_with_the_left_hand_satisfies_the_paddle_held_signal()
        {
            Assert.AreEqual(1, CountSatisfied(_signals.PaddleHeld, () => _paddle.ChangeHeldBy(Hand.Left)));
        }

        [Test]
        public void Letting_go_of_the_paddle_does_not_satisfy_the_paddle_held_signal()
        {
            Assert.AreEqual(0, CountSatisfied(_signals.PaddleHeld, () => _paddle.ChangeHeldBy(Hand.None)));
        }

        [Test]
        public void A_paddle_already_in_a_hand_satisfies_the_paddle_held_signal_on_arming()
        {
            _paddle.HeldBy = Hand.Left;

            Assert.AreEqual(1, CountSatisfied(_signals.PaddleHeld, () => { }));
        }

        [Test]
        public void An_empty_hand_does_not_satisfy_the_paddle_held_signal_on_arming()
        {
            Assert.AreEqual(0, CountSatisfied(_signals.PaddleHeld, () => { }));
        }

        [Test]
        public void Disarming_the_paddle_held_signal_stops_listening_to_the_paddle()
        {
            _signals.PaddleHeld.Arm();
            Assert.AreEqual(1, _paddle.ListenerCount);

            _signals.PaddleHeld.Disarm();

            Assert.AreEqual(0, _paddle.ListenerCount);
        }

        [Test]
        public void Both_the_menu_button_and_the_wrist_gesture_satisfy_the_menu_opened_signal()
        {
            Assert.AreEqual(1, CountSatisfied(_signals.MenuOpened, _commands.PressMenu));
            Assert.AreEqual(1, CountSatisfied(_signals.MenuOpened, _gesture.Perform));
        }

        [Test]
        public void Disarming_the_menu_opened_signal_stops_listening_to_the_button_and_the_gesture()
        {
            _signals.MenuOpened.Arm();
            Assert.AreEqual(1, _commands.MenuListenerCount);
            Assert.AreEqual(1, _gesture.ListenerCount);

            _signals.MenuOpened.Disarm();

            Assert.AreEqual(0, _commands.MenuListenerCount);
            Assert.AreEqual(0, _gesture.ListenerCount);
        }

        [Test]
        public void The_menu_opened_signal_works_without_a_wrist_gesture()
        {
            var signals = TutorialSignalFactory.Create(_paddle.Feed, _commands, null, _match.Feed, LocalSide);

            Assert.AreEqual(1, CountSatisfied(signals.MenuOpened, _commands.PressMenu));
        }

        [Test]
        public void Starting_a_match_satisfies_the_match_running_signal()
        {
            Assert.AreEqual(1, CountSatisfied(_signals.MatchRunning, _match.Start));
        }

        [Test]
        public void A_match_already_running_satisfies_the_match_running_signal_on_arming()
        {
            _match.IsRunning = true;

            Assert.AreEqual(1, CountSatisfied(_signals.MatchRunning, () => { }));
        }

        [Test]
        public void Disarming_the_match_running_signal_stops_listening_for_match_starts()
        {
            _signals.MatchRunning.Arm();
            Assert.AreEqual(1, _match.StartedListenerCount);

            _signals.MatchRunning.Disarm();

            Assert.AreEqual(0, _match.StartedListenerCount);
        }

        [Test]
        public void Only_a_hit_by_the_local_player_satisfies_the_ball_hit_signal()
        {
            Assert.AreEqual(0, CountSatisfied(_signals.BallHit, () => _match.Hit(Side.Far)));
            Assert.AreEqual(1, CountSatisfied(_signals.BallHit, () => _match.Hit(LocalSide)));
        }

        [Test]
        public void Disarming_the_ball_hit_signal_stops_listening_for_hits()
        {
            _signals.BallHit.Arm();
            Assert.AreEqual(1, _match.PaddleHitListenerCount);

            _signals.BallHit.Disarm();

            Assert.AreEqual(0, _match.PaddleHitListenerCount);
        }

        [Test]
        public void Only_a_point_won_by_the_local_player_satisfies_the_point_won_signal()
        {
            Assert.AreEqual(0, CountSatisfied(_signals.PointWon, () => _match.WinPoint(Side.Far)));
            Assert.AreEqual(1, CountSatisfied(_signals.PointWon, () => _match.WinPoint(LocalSide)));
        }

        [Test]
        public void Disarming_the_point_won_signal_stops_listening_for_points()
        {
            _signals.PointWon.Arm();
            Assert.AreEqual(1, _match.PointWonListenerCount);

            _signals.PointWon.Disarm();

            Assert.AreEqual(0, _match.PointWonListenerCount);
        }
    }
}

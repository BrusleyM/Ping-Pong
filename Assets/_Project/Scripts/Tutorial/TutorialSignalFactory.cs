using System;
using PingPong.Core;
using PingPong.Gameplay;

namespace PingPong.Tutorial
{
    public static class TutorialSignalFactory
    {
        public static TutorialSignals Create(PaddleGrip paddle, IPlayerCommands commands, IMenuGesture menuGesture, MatchController match, GameModeController modes, Side localPlayer)
        {
            return Create(PaddleHoldFeed.Of(paddle), commands, menuGesture, MatchFeed.Of(match, modes), localPlayer);
        }

        public static TutorialSignals Create(PaddleHoldFeed paddle, IPlayerCommands commands, IMenuGesture menuGesture, MatchFeed match, Side localPlayer)
        {
            return new TutorialSignals(
                PaddleHeld(paddle),
                MenuOpened(commands, menuGesture),
                MatchRunning(match),
                BallHit(match, localPlayer),
                PointWon(match, localPlayer));
        }

        private static EventCondition PaddleHeld(PaddleHoldFeed paddle)
        {
            return new EventCondition(
                satisfy =>
                {
                    Action<Hand> onHeldByChanged = hand =>
                    {
                        if (hand != Hand.None)
                        {
                            satisfy();
                        }
                    };
                    paddle.Subscribe(onHeldByChanged);
                    return () => paddle.Unsubscribe(onHeldByChanged);
                },
                () => paddle.HeldBy() != Hand.None);
        }

        private static EventCondition MenuOpened(IPlayerCommands commands, IMenuGesture menuGesture)
        {
            return new EventCondition(satisfy =>
            {
                commands.MenuPerformed += satisfy;
                if (menuGesture != null)
                {
                    menuGesture.Performed += satisfy;
                }

                return () =>
                {
                    commands.MenuPerformed -= satisfy;
                    if (menuGesture != null)
                    {
                        menuGesture.Performed -= satisfy;
                    }
                };
            });
        }

        private static EventCondition MatchRunning(MatchFeed match)
        {
            return new EventCondition(
                satisfy =>
                {
                    match.SubscribeToMatchStarted(satisfy);
                    return () => match.UnsubscribeFromMatchStarted(satisfy);
                },
                match.IsMatchRunning);
        }

        private static EventCondition BallHit(MatchFeed match, Side localPlayer)
        {
            return new EventCondition(satisfy =>
            {
                Action<Side> onPaddleHit = side =>
                {
                    if (side == localPlayer)
                    {
                        satisfy();
                    }
                };
                match.SubscribeToPaddleHit(onPaddleHit);
                return () => match.UnsubscribeFromPaddleHit(onPaddleHit);
            });
        }

        private static EventCondition PointWon(MatchFeed match, Side localPlayer)
        {
            return new EventCondition(satisfy =>
            {
                Action<Side, PointReason> onPointWon = (winner, reason) =>
                {
                    if (winner == localPlayer)
                    {
                        satisfy();
                    }
                };
                match.SubscribeToPointWon(onPointWon);
                return () => match.UnsubscribeFromPointWon(onPointWon);
            });
        }
    }
}

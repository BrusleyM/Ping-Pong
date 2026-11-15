using System;
using PingPong.Core;
using PingPong.Gameplay;

namespace PingPong.Tutorial
{
    public static class TutorialSignalFactory
    {
        public static TutorialSignals Create(PaddleGrip paddle, IPlayerCommands commands, IMenuGesture menuGesture, MatchController match, Side localPlayer)
        {
            return new TutorialSignals(
                PaddleHeld(paddle),
                MenuOpened(commands, menuGesture),
                BallHit(match, localPlayer),
                PointWon(match, localPlayer));
        }

        private static EventCondition PaddleHeld(PaddleGrip paddle)
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
                    paddle.HeldByChanged += onHeldByChanged;
                    return () => paddle.HeldByChanged -= onHeldByChanged;
                },
                () => paddle.HeldBy != Hand.None);
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

        private static EventCondition BallHit(MatchController match, Side localPlayer)
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
                match.PaddleHit += onPaddleHit;
                return () => match.PaddleHit -= onPaddleHit;
            });
        }

        private static EventCondition PointWon(MatchController match, Side localPlayer)
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
                match.PointWon += onPointWon;
                return () => match.PointWon -= onPointWon;
            });
        }
    }
}

using System;
using PingPong.Core;
using PingPong.Gameplay;

namespace PingPong.Tutorial
{
    public sealed class PaddleHoldFeed
    {
        public PaddleHoldFeed(Func<Hand> heldBy, Action<Action<Hand>> subscribe, Action<Action<Hand>> unsubscribe)
        {
            HeldBy = heldBy;
            Subscribe = subscribe;
            Unsubscribe = unsubscribe;
        }

        public Func<Hand> HeldBy { get; }
        public Action<Action<Hand>> Subscribe { get; }
        public Action<Action<Hand>> Unsubscribe { get; }

        public static PaddleHoldFeed Of(PaddleGrip paddle)
        {
            return new PaddleHoldFeed(
                () => paddle.HeldBy,
                handler => paddle.HeldByChanged += handler,
                handler => paddle.HeldByChanged -= handler);
        }
    }

    public sealed class MatchFeed
    {
        public MatchFeed(
            Func<bool> isMatchRunning,
            Action<Action> subscribeToMatchStarted,
            Action<Action> unsubscribeFromMatchStarted,
            Action<Action<Side>> subscribeToPaddleHit,
            Action<Action<Side>> unsubscribeFromPaddleHit,
            Action<Action<Side, PointReason>> subscribeToPointWon,
            Action<Action<Side, PointReason>> unsubscribeFromPointWon)
        {
            IsMatchRunning = isMatchRunning;
            SubscribeToMatchStarted = subscribeToMatchStarted;
            UnsubscribeFromMatchStarted = unsubscribeFromMatchStarted;
            SubscribeToPaddleHit = subscribeToPaddleHit;
            UnsubscribeFromPaddleHit = unsubscribeFromPaddleHit;
            SubscribeToPointWon = subscribeToPointWon;
            UnsubscribeFromPointWon = unsubscribeFromPointWon;
        }

        public Func<bool> IsMatchRunning { get; }
        public Action<Action> SubscribeToMatchStarted { get; }
        public Action<Action> UnsubscribeFromMatchStarted { get; }
        public Action<Action<Side>> SubscribeToPaddleHit { get; }
        public Action<Action<Side>> UnsubscribeFromPaddleHit { get; }
        public Action<Action<Side, PointReason>> SubscribeToPointWon { get; }
        public Action<Action<Side, PointReason>> UnsubscribeFromPointWon { get; }

        public static MatchFeed Of(MatchController match, GameModeController modes)
        {
            return new MatchFeed(
                () => match.Phase != MatchPhase.FreePlay,
                handler => modes.MatchStarted += handler,
                handler => modes.MatchStarted -= handler,
                handler => match.PaddleHit += handler,
                handler => match.PaddleHit -= handler,
                handler => match.PointWon += handler,
                handler => match.PointWon -= handler);
        }
    }
}

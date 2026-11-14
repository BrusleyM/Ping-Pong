namespace PingPong.Core
{
    /// <remarks>
    /// Adding a phase: decide it in <see cref="MatchPhaseRules"/> (AllowsHits, NeedsWatching) and handle it in
    /// MatchController.Update if it needs watching; a phase missing from NeedsWatching is never watched.
    /// </remarks>
    public enum MatchPhase
    {
        /// <summary>No match: anyone may hit, nothing is scored.</summary>
        FreePlay,
        WaitingForServe,
        Rally,
        /// <summary>A point was just awarded; the next serve has not been set up yet.</summary>
        PointOver,
        GameOver,
    }

    /// <summary>Why a point was awarded. Presentation turns this into text.</summary>
    public enum PointReason
    {
        None,
        /// <summary>The serve's first bounce was not on the server's side.</summary>
        ServeMissedOwnSide,
        /// <summary>The serve bounced twice on the server's side.</summary>
        ServeBouncedTwiceOnOwnSide,
        /// <summary>The serve went dead before reaching the receiver's side.</summary>
        ServeFault,
        BouncedOnOwnSide,
        DoubleBounce,
        DoubleHit,
        /// <summary>Struck before it bounced, while over the table.</summary>
        Volley,
        /// <summary>Struck before it bounced, behind the table: the shot was long.</summary>
        Long,
        NotReturned,
        Out,
    }
}

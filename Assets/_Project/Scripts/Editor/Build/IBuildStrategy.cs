namespace PingPong.EditorTools.Build
{
    public interface IBuildStrategy
    {
        /// <param name="kind">The release to make (moves the version on), or null for a check build that keeps it.</param>
        void Build(ReleaseKind? kind);
    }
}

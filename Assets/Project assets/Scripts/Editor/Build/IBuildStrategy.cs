using System;

namespace PingPong.EditorTools.Build
{
    public interface IBuildStrategy
    {
        void Build(string versionName, string buildNumber);
    }
}

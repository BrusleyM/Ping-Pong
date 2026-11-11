using UnityEditor;
using UnityEngine;

namespace PingPong.EditorTools.Build
{
    /// <summary>
    /// The version lives in Player Settings (ProjectSettings.asset), for local and CI builds alike: a release
    /// build moves it on (<see cref="ReleaseVersion.Next"/>) and saves it, and CI commits the version back
    /// before uploading. A failed build puts the old version back.
    /// </summary>
    public sealed class BuildVersioning
    {
        readonly int _releasedCode;

        /// <param name="releasedCode">The highest code already released (CI reads it from the release tags; 0 locally).</param>
        public BuildVersioning(int releasedCode = 0) => _releasedCode = releasedCode;

        public static ReleaseVersion Current =>
            ReleaseVersion.Parse(PlayerSettings.bundleVersion, PlayerSettings.Android.bundleVersionCode);

        /// <summary>Moves the version on for this kind of build and returns the previous one.</summary>
        public ReleaseVersion Bump(ReleaseKind kind)
        {
            ReleaseVersion previous = Current;
            Set(previous.Next(kind, _releasedCode));
            return previous;
        }

        public void Set(ReleaseVersion version)
        {
            PlayerSettings.bundleVersion = version.Name;
            PlayerSettings.Android.bundleVersionCode = version.Code;
            AssetDatabase.SaveAssets(); // writes ProjectSettings.asset, so the version is on disk to commit
            Debug.Log($"[Build] Version {version}.");
        }
    }
}

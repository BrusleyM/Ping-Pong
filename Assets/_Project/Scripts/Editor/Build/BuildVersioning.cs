using UnityEditor;
using UnityEngine;

namespace PingPong.EditorTools.Build
{
    /// <summary>
    /// The version lives in Player Settings (ProjectSettings.asset), for local and CI builds alike: a release
    /// build moves it on (<see cref="ReleaseVersion.Next"/>) and saves it, and CI commits the saved file back
    /// after a successful upload. A failed build puts the old version back.
    /// </summary>
    public sealed class BuildVersioning
    {
        public static ReleaseVersion Current =>
            ReleaseVersion.Parse(PlayerSettings.bundleVersion, PlayerSettings.Android.bundleVersionCode);

        /// <summary>Moves the version on for this kind of build and returns the previous one.</summary>
        public ReleaseVersion Bump(ReleaseKind kind)
        {
            ReleaseVersion previous = Current;
            Set(previous.Next(kind));
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

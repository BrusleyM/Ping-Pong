using UnityEditor;
using UnityEngine;

namespace PingPong.EditorTools.Build
{
    /// <summary>
    /// Sets the version for a CI build without committing anything:
    /// version name = RELEASE.MAIN.DEVELOP worked out from the git tags by .github/scripts/version.sh;
    /// version code = the code saved in ProjectSettings + the CI run number, so it always goes up.
    /// Local builds keep whatever Player Settings holds.
    /// </summary>
    public class MyVersionManager
    {
        public void Apply(string versionName, string buildNumber)
        {
            if (!string.IsNullOrEmpty(versionName))
                PlayerSettings.bundleVersion = versionName;

            if (int.TryParse(buildNumber, out int run) && run > 0)
                PlayerSettings.Android.bundleVersionCode += run;
            else
                Debug.Log("[Build] No CI build number; keeping the Player Settings version code.");

            Debug.Log($"[Build] Version {PlayerSettings.bundleVersion} ({PlayerSettings.Android.bundleVersionCode}).");
        }
    }
}

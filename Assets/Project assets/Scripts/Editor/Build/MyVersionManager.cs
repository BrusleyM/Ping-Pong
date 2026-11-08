using UnityEditor;
using UnityEngine;

namespace PingPong.EditorTools.Build
{
    /// <summary>
    /// Sets the Android version code for a CI build without committing anything:
    /// versionCode = the code saved in ProjectSettings + the CI run number, so it always goes up.
    /// The version name (bundleVersion, e.g. 1.1.19) is changed by hand when you want a new release number.
    /// </summary>
    public class MyVersionManager
    {
        public void ApplyBuildNumber(string buildNumber)
        {
            if (!int.TryParse(buildNumber, out int run) || run <= 0)
            {
                Debug.Log($"[Build] No CI build number; keeping versionCode {PlayerSettings.Android.bundleVersionCode}.");
                return;
            }
            int code = PlayerSettings.Android.bundleVersionCode + run;
            PlayerSettings.Android.bundleVersionCode = code;
            Debug.Log($"[Build] Version {PlayerSettings.bundleVersion} ({code}).");
        }
    }
}

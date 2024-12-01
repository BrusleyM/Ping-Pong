#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class AutoVersionIncrement : IPreprocessBuildWithReport
{
    // The callback order determines the sequence of execution in the build pipeline.
    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report)
    {
        if (report.summary.platform == BuildTarget.Android)
        {
            IncrementVersionAndBundleCode();
        }
    }

    private void IncrementVersionAndBundleCode()
    {
        // Ensure version code is incremented by retrieving current settings.
        string currentVersion = PlayerSettings.bundleVersion;
        int currentBundleCode = PlayerSettings.Android.bundleVersionCode;

        // Split the version string into major, minor, and patch parts.
        string[] versionParts = currentVersion.Split('.');
        if (versionParts.Length < 3)
        {
            Debug.LogError("[AutoVersionIncrement] Invalid version format. Please use 'major.minor.patch' format.");
            return;
        }

        int major = int.Parse(versionParts[0]);
        int minor = int.Parse(versionParts[1]);
        int patch = int.Parse(versionParts[2]);

        // Increment the patch version for each build.
        patch++;
        currentBundleCode++;

        // Create the new version string.
        string newVersion = $"{major}.{minor}.{patch}";
        PlayerSettings.bundleVersion = newVersion;
        PlayerSettings.Android.bundleVersionCode = currentBundleCode;

        // Log the changes for review.
        Debug.Log($"[AutoVersionIncrement] Version updated to {newVersion}");
        Debug.Log($"[AutoVersionIncrement] Bundle Code updated to {currentBundleCode}");
    }
}
#endif
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.IO;

public class AndroidBuildStrategy : IBuildStrategy
{
    private readonly string _outputFolder;
    private readonly MyKeystoreManager _keystoreManager;
    private readonly MyVersionManager _myVersionManager;

    public AndroidBuildStrategy(string outputFolder, MyKeystoreManager keystoreManager,MyVersionManager myVersionManager)
    {
        _outputFolder = outputFolder;
        _keystoreManager = keystoreManager;
        _myVersionManager = myVersionManager;
    }

    public void Build()
    {
        // Detect if we are running in CI environment
        bool isCi = IsCiEnvironment();

        // Get enabled scenes
        var scenes = EditorBuildSettings.scenes
                        .Where(s => s.enabled)
                        .Select(s => s.path)
                        .ToArray();

        if (scenes.Length == 0)
        {
            Debug.LogError("No scenes are enabled in Build Settings.");
            return;
        }

        // Ensure output folder exists
        if (!Directory.Exists(_outputFolder))
            Directory.CreateDirectory(_outputFolder);

        // Apply versioning and keystore
        _myVersionManager.PerformVersionBump();
        _keystoreManager.ApplyKeystore();

        // Remove Meta XR Simulator if running in CI
        if (isCi)
        {
            RemoveMetaXrSimulator();
        }

        // Generate APK name
        string apkName = $"{PlayerSettings.productName}_{PlayerSettings.bundleVersion}_{PlayerSettings.Android.bundleVersionCode}.apk";
        string apkPath = Path.Combine(_outputFolder, apkName);

        // Build player
        var buildPlayerOptions = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = apkPath,
            target = BuildTarget.Android,
            options = BuildOptions.None
        };

        var report = BuildPipeline.BuildPlayer(buildPlayerOptions);

        if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
            Debug.Log($"Android build complete: {apkPath}");
        else
            Debug.LogError("Build failed. Check console for details.");
    }

    private bool IsCiEnvironment()
    {
        // Typical CI environment variables
        string[] ciVars = { "CI", "GITHUB_ACTIONS", "GITLAB_CI", "BUILD_NUMBER", "UNITY_CI" };
        return ciVars.Any(v => !string.IsNullOrEmpty(System.Environment.GetEnvironmentVariable(v)));
    }

    private void RemoveMetaXrSimulator()
    {
        string simulatorPath = Path.Combine(Application.dataPath, "../Packages/com.meta.xr.simulator");
        if (Directory.Exists(simulatorPath))
        {
            Directory.Delete(simulatorPath, true);
            File.Delete(simulatorPath + ".meta");
            Debug.Log("Meta XR Simulator removed for CI build.");
        }
    }
}
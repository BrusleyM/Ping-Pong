using UnityEditor;
using UnityEngine;

// High-level entry point (DIP + SRP)
public class BuildScript
{
    [MenuItem("Build/Build Android APK (SOLID)")]
    public static void Execute()
    {
        // Parse custom parameters from CI
        string[] args = System.Environment.GetCommandLineArgs();
        string buildNumber = GetArg(args, "BuildNumber");

        var versionManager = new MyVersionManager();

        var keystoreManager = new MyKeystoreManager(); // CI secrets, or the key set in Player Settings
        var androidBuilder = new AndroidBuildStrategy("Builds/Android", keystoreManager, versionManager);

        // Perform actual build
        androidBuilder.Build(buildNumber);
    }
    private static string GetArg(string[] args, string name)
    {
        foreach (string arg in args)
        {
            if (arg.StartsWith(name + "="))
            {
                return arg.Substring(name.Length + 1);
            }
        }
        return null;
    }
}
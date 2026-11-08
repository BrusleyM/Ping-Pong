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
        string branch = GetArg(args, "BranchName");
        bool isPR = GetArg(args, "IsPR") == "true";

        var versionManager = new MyVersionManager();

        // Keystore passwords come from the environment (GitHub secrets in CI); never hard-code them.
        var keystoreManager = new MyKeystoreManager(
            ".keystore.keystore",
            RequireEnv("ANDROID_KEYSTORE_PASS"),
            "ping pong",
            RequireEnv("ANDROID_KEYALIAS_PASS")
        );
        var androidBuilder = new AndroidBuildStrategy("Builds/Android", keystoreManager, versionManager);

        // Perform actual build
        androidBuilder.Build(branch, isPR);
    }
    private static string RequireEnv(string name)
    {
        string value = System.Environment.GetEnvironmentVariable(name);
        if (string.IsNullOrEmpty(value))
            throw new System.InvalidOperationException($"Environment variable {name} is not set.");
        return value;
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
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

        // Initialize keystore and builder
        var keystoreManager = new MyKeystoreManager(
            ".keystore.keystore",
            "pass1234",
            "ping pong",
            "pass1234"
        );
        var androidBuilder = new AndroidBuildStrategy("Builds/Android", keystoreManager, versionManager);

        // Perform actual build
        androidBuilder.Build(branch, isPR);
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
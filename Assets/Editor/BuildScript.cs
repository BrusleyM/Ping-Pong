using UnityEditor;

// High-level entry point (DIP + SRP)
public class BuildScript
{
    [MenuItem("Build/Build Android APK (SOLID)")]
    public static void Execute()
    {
        var keystoreManager = new MyKeystoreManager(
            ".keystore.keystore",
            "***REMOVED***",
            "ping pong",
            "***REMOVED***"
        );
        var versionManager = new MyVersionManager();
        var androidBuilder = new AndroidBuildStrategy("Builds/Android", keystoreManager, versionManager);
        androidBuilder.Build();
    }
}
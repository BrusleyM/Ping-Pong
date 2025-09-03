using UnityEditor;

// High-level entry point (DIP + SRP)
public class BuildScript
{
    [MenuItem("Build/Build Android APK (SOLID)")]
    public static void Execute()
    {
        var keystoreManager = new MyKeystoreManager(
            ".keystore.keystore",
            "pass1234",
            "ping pong",
            "pass1234"
        );
        var androidBuilder = new AndroidBuildStrategy("Builds/Android", keystoreManager);
        androidBuilder.Build();
    }
}
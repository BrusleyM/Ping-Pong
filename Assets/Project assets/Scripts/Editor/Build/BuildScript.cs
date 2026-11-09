using UnityEditor;
using UnityEngine;

namespace PingPong.EditorTools.Build
{
    /// <summary>Build entry point for the menu and CI (game-ci buildMethod).</summary>
    public static class BuildScript
    {
        [MenuItem("Build/Build Android APK")]
        public static void Execute()
        {
            // Parse custom parameters from CI
            string[] args = System.Environment.GetCommandLineArgs();
            string buildNumber = GetArg(args, "BuildNumber");
            string versionName = GetArg(args, "VersionName");

            var versioning = new BuildVersioning();

            var signing = new AndroidSigning(); // CI secrets, or the key set in Player Settings
            var androidBuilder = new AndroidBuildStrategy("Builds/Android", signing, versioning);

            // In CI (batch mode) any failure must end Unity with a non-zero exit code, so the job fails here
            // instead of later with "no APK found".
            try
            {
                androidBuilder.Build(versionName, buildNumber);
            }
            catch (System.Exception e)
            {
                Debug.LogError("[Build] " + e.Message);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                throw;
            }
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
}

using UnityEditor;
using UnityEngine;

namespace PingPong.EditorTools.Build
{
    /// <summary>
    /// Build entry points, the same for the editor menu and CI (game-ci buildMethod):
    /// Develop bumps z, Staging bumps y (z = 0), Production bumps x (y = z = 0); all bump the bundle code.
    /// CI picks the method from a [Develop], [Staging] or [Production] keyword in the pushed commit message
    /// and commits the bumped ProjectSettings back after a successful upload. PR checks use
    /// <see cref="CheckBuild"/>, which leaves the version alone.
    /// </summary>
    public static class BuildScript
    {
        const string Menu = "Build/";

        [MenuItem(Menu + "Develop", priority = 1)]
        public static void BuildDevelop() => Run(ReleaseKind.Develop);

        [MenuItem(Menu + "Staging", priority = 2)]
        public static void BuildStaging() => Run(ReleaseKind.Staging);

        [MenuItem(Menu + "Production", priority = 3)]
        public static void BuildProduction() => Run(ReleaseKind.Production);

        [MenuItem(Menu + "Check Build (version unchanged)", priority = 20)]
        public static void CheckBuild() => Run(null);

        static void Run(ReleaseKind? kind)
        {
            int.TryParse(Arg("ReleasedCode"), out int releasedCode); // CI only: the highest code already released
            var builder = new AndroidBuildStrategy("Builds/Android", new AndroidSigning(), new BuildVersioning(releasedCode));
            // In CI (batch mode) any failure must end Unity with a non-zero exit code, so the job fails here
            // instead of later with "no APK found".
            try
            {
                builder.Build(kind);
            }
            catch (System.Exception e)
            {
                Debug.LogError("[Build] " + e.Message);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                throw;
            }
        }

        /// <summary>A game-ci custom parameter (Name=value), or null.</summary>
        static string Arg(string name)
        {
            foreach (string arg in System.Environment.GetCommandLineArgs())
                if (arg.StartsWith(name + "=")) return arg.Substring(name.Length + 1);
            return null;
        }
    }
}

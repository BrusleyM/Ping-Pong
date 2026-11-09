using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using System.Linq;
using System.IO;

namespace PingPong.EditorTools.Build
{
    public sealed class AndroidBuildStrategy : IBuildStrategy
    {
        private readonly string _outputFolder;
        private readonly AndroidSigning _signing;
        private readonly BuildVersioning _versioning;

        public AndroidBuildStrategy(string outputFolder, AndroidSigning signing, BuildVersioning versioning)
        {
            _outputFolder = outputFolder;
            _signing = signing;
            _versioning = versioning;
        }

        public void Build(string versionName, string buildNumber)
        {
            // Get enabled scenes
            var scenes = EditorBuildSettings.scenes
                            .Where(s => s.enabled)
                            .Select(s => s.path)
                            .ToArray();

            if (scenes.Length == 0)
                throw new BuildFailedException("No scenes are enabled in Build Settings.");

            // Ensure output folder exists
            if (!Directory.Exists(_outputFolder))
                Directory.CreateDirectory(_outputFolder);

            // Check signing first so a failed build doesn't bump the version.
            if (!_signing.ApplyKeystore())
                throw new BuildFailedException("Android signing key is not set up; see the error above.");
            _versioning.Apply(versionName, buildNumber);

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

            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new BuildFailedException($"Android build {report.summary.result} with {report.summary.totalErrors} error(s); see the log above.");
            Debug.Log($"Android build complete: {apkPath}");
        }
    }
}

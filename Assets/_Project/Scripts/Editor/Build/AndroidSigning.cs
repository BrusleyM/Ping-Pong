using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace PingPong.EditorTools.Build
{
    /// <summary>
    /// Chooses the Android signing key for a build. Passwords are never stored in code.
    /// CI: ANDROID_KEYSTORE_PASS and ANDROID_KEYALIAS_PASS are set (GitHub secrets); the keystore is
    ///     ANDROID_KEYSTORE_PATH, or .keystore.keystore, which game-ci writes from ANDROID_KEYSTORE_BASE64.
    /// Local: the keystore and passwords entered in Player Settings > Publishing Settings are used as-is.
    /// </summary>
    public sealed class AndroidSigning
    {
        const string DefaultKeystore = ".keystore.keystore";
        const string DefaultAlias = "ping pong";

        /// <summary>Applies the signing settings. Returns false, with a clear error, if the build can't be signed.</summary>
        public bool ApplyKeystore()
        {
            string storePass = Env("ANDROID_KEYSTORE_PASS");
            string aliasPass = Env("ANDROID_KEYALIAS_PASS");

            if (storePass != null && aliasPass != null)
            {
                PlayerSettings.Android.useCustomKeystore = true;
                PlayerSettings.Android.keystoreName = Env("ANDROID_KEYSTORE_PATH") ?? DefaultKeystore;
                PlayerSettings.Android.keystorePass = storePass;
                PlayerSettings.Android.keyaliasName = Env("ANDROID_KEYALIAS_NAME") ?? DefaultAlias;
                PlayerSettings.Android.keyaliasPass = aliasPass;
            }
            else if (!PlayerSettings.Android.useCustomKeystore)
            {
                return Fail("No signing key set. Select the keystore in Player Settings > Android > Publishing Settings.");
            }
            else if (string.IsNullOrEmpty(PlayerSettings.Android.keystorePass) || string.IsNullOrEmpty(PlayerSettings.Android.keyaliasPass))
            {
                // Unity forgets the passwords when the editor closes.
                return Fail("Enter the keystore and alias passwords in Player Settings > Android > Publishing Settings, then build again.");
            }

            if (!KeystoreExists(PlayerSettings.Android.keystoreName))
                return Fail($"Keystore not found: {PlayerSettings.Android.keystoreName}");
            return true;
        }

        /// <summary>Unity stores the path absolute, relative to the project, or relative to the
        /// "dedicated" keystore folder (your home folder by default), with or without a {…}: prefix.</summary>
        static bool KeystoreExists(string name)
        {
            int colon = name.StartsWith("{") ? name.IndexOf("}:") : -1;
            string path = colon >= 0 ? name.Substring(colon + 2).Trim() : name;
            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return File.Exists(path) || File.Exists(Path.Combine(home, path));
        }

        static string Env(string name)
        {
            string v = Environment.GetEnvironmentVariable(name);
            return string.IsNullOrEmpty(v) ? null : v;
        }

        static bool Fail(string message)
        {
            Debug.LogError("[Build] " + message);
            return false;
        }
    }
}

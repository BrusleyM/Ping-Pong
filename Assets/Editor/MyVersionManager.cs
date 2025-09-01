using System;
using UnityEditor;
using UnityEngine;
public class MyVersionManager
{
    public void EnsureVersion()
    {
        if (string.IsNullOrEmpty(PlayerSettings.bundleVersion))
            PlayerSettings.bundleVersion = "1.0.0";

        PlayerSettings.Android.bundleVersionCode += 1;
        Debug.Log($"Bundle Version: {PlayerSettings.bundleVersion}, Bundle Code: {PlayerSettings.Android.bundleVersionCode}");
    }
}


using System;
using UnityEditor;
public class MyVersionManager
{
    public void PerformVersionBump(string branch)
    {

        string[] version = PlayerSettings.bundleVersion.Split('.');
        int major = int.Parse(version[0]);
        int minor = int.Parse(version[1]);
        int patch = int.Parse(version[2]);

        if (branch == "develop")
        {
            minor++; patch = 0;
        }
        else if (branch == "main")
        {
            major++; minor = 0; patch = 0;
        }
        else
        {
            patch++;
        }
        PlayerSettings.Android.bundleVersionCode++;
        PlayerSettings.bundleVersion = $"{major}.{minor}.{patch}";
    }
}


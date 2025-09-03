using System;
using UnityEditor;
public class MyVersionManager
{
    public void EnsureVersion()
    {
        PerformVersionBump();
    }
    public static void PerformVersionBump()
    {
        string branch = Environment.GetEnvironmentVariable("BRANCH_NAME");

        string[] version = PlayerSettings.bundleVersion.Split('.');
        int major = int.Parse(version[0]);
        int minor = int.Parse(version[1]);
        int patch = int.Parse(version[2]);

        if (branch == "main")
        {
            major++; minor = 0; patch = 0;
            PlayerSettings.Android.bundleVersionCode++;
        }
        else if (branch=="develop")
        {
            minor++; patch = 0;
            PlayerSettings.Android.bundleVersionCode++;
        }
        else
        { 
            patch++;
        }

        PlayerSettings.bundleVersion = $"{major}.{minor}.{patch}";
    }
}


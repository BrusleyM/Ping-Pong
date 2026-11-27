using Framework;
using PingPong.Gameplay;
using PingPong.Platform.Meta;
using PingPong.Presentation;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using static PingPong.EditorTools.SetupHelpers;

namespace PingPong.EditorTools
{
    internal static class ViewHeightSetup
    {
        private const string ProfilePath = "Assets/_Project/Settings/Gameplay/ViewHeight.asset";

        internal static void Wire(Scene scene, GameModeController modes, GameObject canvas, EyeHeightPlacer placer)
        {
            var eyes = MenuSceneBuilder.FindInScene(scene, "CenterEyeAnchor");
            var tracking = GetOrAdd<MetaHeadTracking>(GetOrCreate("HeadTracking"));
            var adjuster = GetOrAdd<ViewHeightAdjuster>(GetOrCreate("ViewHeight"));
            SetSeam(adjuster, "_tracking", tracking);
            Set(adjuster, "_rig", eyes.GetComponentInParent<OVRCameraRig>(true).transform);
            Set(adjuster, "_profile", Profile());
            Set(adjuster, "_modes", modes);
            Set(adjuster, "_match", modes != null ? modes.GetComponent<MatchController>() : null);

            var follow = GetOrAdd<PlaceOnViewHeight>(canvas);
            Set(follow, "_placer", placer);
            Set(follow, "_viewHeight", adjuster);
        }

        private static ViewHeightProfile Profile()
        {
            var profile = AssetDatabase.LoadAssetAtPath<ViewHeightProfile>(ProfilePath);
            if (profile != null)
            {
                return profile;
            }

            profile = ScriptableObject.CreateInstance<ViewHeightProfile>();
            AssetDatabase.CreateAsset(profile, ProfilePath);
            return profile;
        }
    }
}

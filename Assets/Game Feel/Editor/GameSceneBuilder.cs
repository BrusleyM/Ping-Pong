using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PingPong.GameFeel.EditorTools
{
    /// <summary>
    /// Makes Assets/Game Feel/Scenes/Game_GameFeel.unity: a copy of the project's Game scene with the Game Feel object
    /// and the cube stadium added. The project's own scene and prefabs are not changed.
    /// Menu: Tools > Game Feel > Create Game Feel Game Scene. Running it again starts again from a fresh copy.
    /// </summary>
    public static class GameSceneBuilder
    {
        public const string SourceScenePath = "Assets/_Project/Scenes/Game.unity";
        public const string ScenePath = "Assets/Game Feel/Scenes/Game_GameFeel.unity";
        const string EffectMatPath = "Assets/Game Feel/Materials/GameFeel_Additive.mat";
        /// <summary>The game's own UI font (TextMeshPro asset), used for the hit-point labels.</summary>
        public const string PopupFontPath = "Assets/_Project/Art/Ui/Fredoka-Bold SDF.asset";

        [MenuItem("Tools/Game Feel/Create Game Feel Game Scene")]
        public static void Create()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(SourceScenePath) == null)
            {
                Debug.LogError("[Game Feel] Can't find the game scene at " + SourceScenePath);
                return;
            }
            Selection.activeObject = null;
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null) AssetDatabase.DeleteAsset(ScenePath);
            AssetDatabase.CopyAsset(SourceScenePath, ScenePath);
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            var gf = new GameObject("Game Feel");
            var boot = gf.AddComponent<GameFeelBootstrap>();
            boot.effectMaterial = AssetDatabase.LoadAssetAtPath<Material>(EffectMatPath);
            boot.confettiMaterial = AssetDatabase.LoadAssetAtPath<Material>(StadiumGenerator.ConfettiMaterialPath);
            boot.popupFont = AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>(PopupFontPath);

            var stadium = new GameObject("Stadium").AddComponent<StadiumBuilder>();
            StadiumGenerator.Build(stadium);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Game Feel] Game scene with Game Feel created: " + ScenePath + ". Press Play to start a solo match.");
        }
    }
}

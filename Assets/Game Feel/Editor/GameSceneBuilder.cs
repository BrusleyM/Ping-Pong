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

            ShowTableHideRoom(scene);
            var stadium = new GameObject("Stadium").AddComponent<StadiumBuilder>();
            StadiumGenerator.Build(stadium);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Game Feel] Game scene with Game Feel created: " + ScenePath + ". Press Play to start a solo match.");
        }

        /// <summary>
        /// In the open scene: makes sure the project's Environment (which holds the table and the floor) is on, switches
        /// off only the neon room inside it, and rebuilds the stadium centred on the table. Only this scene changes.
        /// </summary>
        [MenuItem("Tools/Game Feel/Show Table, Hide Room, Rebuild Stadium")]
        public static void FixOpenScene()
        {
            var scene = EditorSceneManager.GetActiveScene();
            ShowTableHideRoom(scene);
            var stadium = Object.FindFirstObjectByType<StadiumBuilder>(FindObjectsInactive.Include);
            if (stadium != null) StadiumGenerator.Build(stadium);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        /// <summary>The table and floor live in World/Environment next to the neon room: keep them, hide the room.</summary>
        static void ShowTableHideRoom(UnityEngine.SceneManagement.Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name == "Environment" && !t.gameObject.activeSelf)
                    {
                        Undo.RecordObject(t.gameObject, "Show Environment");
                        t.gameObject.SetActive(true);
                        PrefabUtility.RecordPrefabInstancePropertyModifications(t.gameObject);
                    }
                    if (t.name == "experimental_neon_room" && t.gameObject.activeSelf)
                    {
                        Undo.RecordObject(t.gameObject, "Hide neon room");
                        t.gameObject.SetActive(false);
                        PrefabUtility.RecordPrefabInstancePropertyModifications(t.gameObject);
                    }
                }
            }
            foreach (var table in Object.FindObjectsByType<PingPong.Gameplay.PingPongTable>(FindObjectsSortMode.None)) table.Rebuild();
        }
    }
}

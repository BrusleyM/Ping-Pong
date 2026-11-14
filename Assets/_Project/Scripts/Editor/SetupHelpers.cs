using System.Linq;
using UnityEditor;
using UnityEngine;

namespace PingPong.EditorTools
{
    /// <summary>Idempotent editing helpers shared by the setup tools: find-or-create, and serialized field writes.</summary>
    internal static class SetupHelpers
    {
        /// <summary>Loads a prefab's contents, lets 'edit' change them, and saves the prefab.</summary>
        internal static void EditPrefab(string path, System.Action<GameObject> edit)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                edit(root);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        internal static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;
            string parent = System.IO.Path.GetDirectoryName(folder).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(folder));
        }

        internal static GameObject GetOrCreate(string name, Transform parent = null)
        {
            Transform found = parent != null ? parent.Find(name) : null;
            if (found == null && parent == null)
                // Anywhere in the scene: objects may sit inside the World prefab's groups.
                found = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .FirstOrDefault(t => t.name == name);
            if (found != null) return found.gameObject;
            var go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            return go;
        }

        internal static T GetOrAdd<T>(GameObject go) where T : Component =>
            go.TryGetComponent(out T c) ? c : go.AddComponent<T>();

        internal static void Set(Object target, string prop, Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(prop).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        internal static void SetBool(Object target, string prop, bool value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(prop).boolValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        internal static void SetEnum(Object target, string prop, int value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(prop).intValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Assigns a Seam&lt;T&gt; field (its serialized component reference).</summary>
        internal static void SetSeam(Object target, string prop, Component implementation) => Set(target, prop + "._component", implementation);

        internal static Object GetRef(Object target, string prop) => new SerializedObject(target).FindProperty(prop).objectReferenceValue;

        internal static void SetF(Object target, string prop, float value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(prop).floatValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        internal static void SetV(Object target, string prop, Vector3 value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(prop).vector3Value = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        internal static void SetArray(Object target, string prop, Object[] values)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(prop);
            p.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}

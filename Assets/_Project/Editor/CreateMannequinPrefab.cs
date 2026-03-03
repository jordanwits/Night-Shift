using UnityEngine;
using UnityEditor;
using NightShift.Systems;

namespace NightShift.Editor
{
    /// <summary>
    /// Creates MannequinStalker prefab. Menu: Night Shift > Create Mannequin Stalker Prefab.
    /// </summary>
    public static class CreateMannequinPrefab
    {
        private const string PrefabPath = "Assets/_Project/Art/Prefabs/Anomalies/MannequinStalker.prefab";
        private const string ResourcesPrefabPath = "Assets/_Project/Resources/Anomalies/MannequinStalker.prefab";

        [MenuItem("Night Shift/Create Mannequin Stalker Prefab")]
        public static void Create()
        {
            EnsureFolderExists("Assets/_Project", "Art");
            EnsureFolderExists("Assets/_Project/Art", "Prefabs");
            EnsureFolderExists("Assets/_Project/Art/Prefabs", "Anomalies");
            EnsureFolderExists("Assets/_Project", "Resources");
            EnsureFolderExists("Assets/_Project/Resources", "Anomalies");

            var go = new GameObject("MannequinStalker");

            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(go.transform);
            body.transform.localPosition = Vector3.up * 1f;
            body.transform.localScale = new Vector3(0.4f, 1f, 0.4f);

            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(go.transform);
            head.transform.localPosition = Vector3.up * 2.2f;
            head.transform.localScale = Vector3.one * 0.5f;

            go.AddComponent<MannequinStalkerAI>();

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, PrefabPath);
            Object.DestroyImmediate(go);

            AssetDatabase.CopyAsset(PrefabPath, ResourcesPrefabPath);

            Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[NightShift] Created MannequinStalker prefab at {PrefabPath} and {ResourcesPrefabPath}");
        }

        private static void EnsureFolderExists(string parentPath, string folderName)
        {
            string fullPath = parentPath + "/" + folderName;
            if (AssetDatabase.IsValidFolder(fullPath))
                return;
            if (!AssetDatabase.IsValidFolder(parentPath))
            {
                var idx = parentPath.LastIndexOf('/');
                if (idx > 0)
                    EnsureFolderExists(parentPath.Substring(0, idx), parentPath.Substring(idx + 1));
            }
            AssetDatabase.CreateFolder(parentPath, folderName);
        }
    }
}

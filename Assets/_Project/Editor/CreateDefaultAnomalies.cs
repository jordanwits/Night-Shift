using UnityEngine;
using UnityEditor;
using NightShift.Core;

namespace NightShift.Editor
{
    /// <summary>
    /// Creates 3 minimal anomaly definitions for testing.
    /// Assets go in Resources/Anomalies for runtime loading.
    /// Menu: Night Shift > Create Default Anomalies.
    /// </summary>
    public static class CreateDefaultAnomalies
    {
        private const string Path = "Assets/_Project/Resources/Anomalies";

        [MenuItem("Night Shift/Create Default Anomalies")]
        public static void Create()
        {
            EnsureFolderExists("Assets/_Project", "Resources");
            EnsureFolderExists("Assets/_Project/Resources", "Anomalies");

            CreateAnomaly("rotated_mannequin", "Rotated Mannequin", 5f, 3f, 1f, false,
                "A mannequin facing the wrong direction.");
            CreateAnomaly("broken_escalator", "Broken Escalator", 8f, 5f, 1.5f, true,
                "An escalator that has stopped unexpectedly.");
            CreateAnomaly("duplicate_store", "Duplicate Store", 6f, 4f, 1f, false,
                "The same store appears twice in different locations.");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[NightShift] Created 3 anomaly definitions in " + Path);
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

        private static void CreateAnomaly(string id, string displayName, float penalty, float reward,
            float spawnWeight, bool isSevere, string description)
        {
            string assetPath = $"{Path}/{id}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<AnomalyDefinition>(assetPath);
            if (existing != null)
            {
                existing.id = id;
                existing.displayName = displayName;
                existing.instabilityPenalty = penalty;
                existing.instabilityReward = reward;
                existing.spawnWeight = spawnWeight;
                existing.isSevere = isSevere;
                existing.description = description;
                EditorUtility.SetDirty(existing);
                return;
            }

            var def = ScriptableObject.CreateInstance<AnomalyDefinition>();
            def.id = id;
            def.displayName = displayName;
            def.instabilityPenalty = penalty;
            def.instabilityReward = reward;
            def.spawnWeight = spawnWeight;
            def.isSevere = isSevere;
            def.description = description;

            AssetDatabase.CreateAsset(def, assetPath);
        }
    }
}

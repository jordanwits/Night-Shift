using UnityEngine;
using UnityEditor;
using NightShift.Core;

namespace NightShift.Editor
{
    /// <summary>
    /// Creates 5 default anomaly definitions. Menu: Night Shift > Create Default Anomalies.
    /// </summary>
    public static class CreateDefaultAnomalies
    {
        private const string Path = "Assets/_Project/Resources/Anomalies";

        [MenuItem("Night Shift/Create Default Anomalies")]
        public static void Create()
        {
            EnsureFolderExists("Assets/_Project", "Resources");
            EnsureFolderExists("Assets/_Project/Resources", "Anomalies");

            CreateAnomaly("anomaly_flickering_light", "Flickering Light", "Lights flicker erratically.", 1, 5f, 3f);
            CreateAnomaly("anomaly_spilled_soda", "Spilled Soda", "Soda machine overflow.", 1, 4f, 2f);
            CreateAnomaly("anomaly_lost_child", "Lost Child Poster", "Missing child poster appears in wrong location.", 2, 8f, 5f);
            CreateAnomaly("anomaly_radio_static", "Radio Static", "Security radio emits static.", 2, 7f, 4f);
            CreateAnomaly("anomaly_shadow_door", "Shadow in Doorway", "Unexplained shadow in corridor.", 3, 12f, 6f);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[NightShift] Created 5 default anomaly definitions in " + Path);
        }

        private static void EnsureFolderExists(string parentPath, string folderName)
        {
            string fullPath = parentPath + "/" + folderName;
            if (!AssetDatabase.IsValidFolder(fullPath))
                AssetDatabase.CreateFolder(parentPath, folderName);
        }

        private static void CreateAnomaly(string id, string name, string desc, int severity, float penalty, float reward)
        {
            string assetPath = $"{Path}/{id}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<AnomalyDefinition>(assetPath);
            if (existing != null)
            {
                Debug.Log($"[NightShift] {id} already exists, skipping.");
                return;
            }

            var def = ScriptableObject.CreateInstance<AnomalyDefinition>();
            def.id = id;
            def.displayName = name;
            def.description = desc;
            def.severity = severity;
            def.baseInstabilityPenalty = penalty;
            def.rewardValue = reward;
            def.spawnRules = new AnomalyDefinition.SpawnRules
            {
                minInstabilityToSpawn = 0,
                maxInstabilityToSpawn = 100,
                maxConcurrent = 2,
                minCooldownSeconds = 15f
            };
            def.fixMethod = new AnomalyDefinition.FixMethod { type = AnomalyDefinition.FixType.Interact };

            AssetDatabase.CreateAsset(def, assetPath);
        }
    }
}

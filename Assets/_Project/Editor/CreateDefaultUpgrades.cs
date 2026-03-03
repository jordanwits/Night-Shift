using UnityEngine;
using UnityEditor;
using NightShift.Core;

namespace NightShift.Editor
{
    public static class CreateDefaultUpgrades
    {
        private const string Path = "Assets/_Project/Resources/Upgrades";

        [MenuItem("Night Shift/Create Default Upgrades")]
        public static void Create()
        {
            EnsureFolderExists("Assets/_Project", "Resources");
            EnsureFolderExists("Assets/_Project/Resources", "Upgrades");

            CreateUpgrade("upgrade_clock", "Overtime Clock", "More real-time to survive the shift.", 50,
                UpgradeEffectType.ClockDurationMultiplier, 1.15f);
            CreateUpgrade("upgrade_decay", "Stabilizing Routine", "Passive reduction in instability per minute.", 70,
                UpgradeEffectType.InstabilityDecayPerMinute, 2f);
            CreateUpgrade("upgrade_report_bonus", "Report Incentive", "Higher credit multiplier for correct reports.", 40,
                UpgradeEffectType.ReportRewardBonus, 1.2f);
            CreateUpgrade("upgrade_damage_reduction", "Security Vest", "Reduces damage taken by 10%.", 60,
                UpgradeEffectType.DamageReductionPercent, 0.10f);
            CreateUpgrade("upgrade_tablet_distortion", "Tablet Filter", "Reduces tablet distortion by 15%.", 45,
                UpgradeEffectType.TabletDistortionReduction, 0.15f);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[NightShift] Created 5 upgrade definitions in " + Path);
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

        private static void CreateUpgrade(string id, string displayName, string description, int cost,
            UpgradeEffectType effectType, float value)
        {
            string assetPath = $"{Path}/{id}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<UpgradeDefinition>(assetPath);
            if (existing != null)
            {
                existing.id = id;
                existing.displayName = displayName;
                existing.description = description;
                existing.cost = cost;
                existing.isRepeatable = false;
                existing.effectType = effectType;
                existing.value = value;
                EditorUtility.SetDirty(existing);
                return;
            }

            var def = ScriptableObject.CreateInstance<UpgradeDefinition>();
            def.id = id;
            def.displayName = displayName;
            def.description = description;
            def.cost = cost;
            def.isRepeatable = false;
            def.effectType = effectType;
            def.value = value;
            AssetDatabase.CreateAsset(def, assetPath);
        }
    }
}

using UnityEngine;

namespace NightShift.Core
{
    public enum UpgradeEffectType
    {
        ClockDurationMultiplier,
        InstabilityDecayPerMinute,
        ReportRewardBonus,
        DamageReductionPercent,
        TabletDistortionReduction
    }

    [CreateAssetMenu(fileName = "NewUpgrade", menuName = "Night Shift/Upgrade Definition")]
    public class UpgradeDefinition : ScriptableObject
    {
        public string id;
        public string displayName;
        [TextArea(2, 4)]
        public string description;
        public int cost;
        public bool isRepeatable;
        public UpgradeEffectType effectType;
        [Tooltip("Multiplier (e.g. 1.15), or value (e.g. 0.10 for 10%) depending on effect type")]
        public float value;
    }
}

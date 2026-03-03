using System.Collections.Generic;
using UnityEngine;
using NightShift.Core;

namespace NightShift.Systems
{
    /// <summary>
    /// Loads purchased upgrades and provides effective values for other systems.
    /// Centralizes all upgrade effect application.
    /// </summary>
    public class UpgradeManager : MonoBehaviour
    {
        public static UpgradeManager Instance { get; private set; }

        private const string UpgradesResourcePath = "Upgrades";

        private ProgressionData _progression;
        private UpgradeDefinition[] _definitions;
        private Dictionary<UpgradeEffectType, float> _effectValues = new Dictionary<UpgradeEffectType, float>();

        public int Credits => _progression?.credits ?? 0;
        public ProgressionData Progression => _progression;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _progression = ProgressionData.Load();
            _definitions = Resources.LoadAll<UpgradeDefinition>(UpgradesResourcePath);
            RecomputeEffects();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void RecomputeEffects()
        {
            _effectValues.Clear();
            _effectValues[UpgradeEffectType.ClockDurationMultiplier] = 1f;
            _effectValues[UpgradeEffectType.InstabilityDecayPerMinute] = 0f;
            _effectValues[UpgradeEffectType.ReportRewardBonus] = 1f;
            _effectValues[UpgradeEffectType.DamageReductionPercent] = 0f;
            _effectValues[UpgradeEffectType.TabletDistortionReduction] = 0f;

            if (_progression == null || _definitions == null) return;

            foreach (var def in _definitions)
            {
                if (def == null || !_progression.HasUpgrade(def.id)) continue;

                switch (def.effectType)
                {
                    case UpgradeEffectType.ClockDurationMultiplier:
                        _effectValues[def.effectType] *= def.value;
                        break;
                    case UpgradeEffectType.InstabilityDecayPerMinute:
                        _effectValues[def.effectType] += def.value;
                        break;
                    case UpgradeEffectType.ReportRewardBonus:
                        _effectValues[def.effectType] *= def.value;
                        break;
                    case UpgradeEffectType.DamageReductionPercent:
                        _effectValues[def.effectType] += def.value;
                        break;
                    case UpgradeEffectType.TabletDistortionReduction:
                        _effectValues[def.effectType] += def.value;
                        break;
                }
            }
        }

        public float GetClockDurationMultiplier() =>
            _effectValues.TryGetValue(UpgradeEffectType.ClockDurationMultiplier, out var v) ? v : 1f;

        public float GetInstabilityDecayPerMinute() =>
            _effectValues.TryGetValue(UpgradeEffectType.InstabilityDecayPerMinute, out var v) ? v : 0f;

        public float GetReportRewardBonusMultiplier() =>
            _effectValues.TryGetValue(UpgradeEffectType.ReportRewardBonus, out var v) ? v : 1f;

        public float GetDamageReductionPercent() =>
            _effectValues.TryGetValue(UpgradeEffectType.DamageReductionPercent, out var v) ? v : 0f;

        public float GetTabletDistortionReduction() =>
            _effectValues.TryGetValue(UpgradeEffectType.TabletDistortionReduction, out var v) ? v : 0f;

        public IReadOnlyList<UpgradeDefinition> GetAllDefinitions() => _definitions ?? System.Array.Empty<UpgradeDefinition>();

        public bool TryPurchase(string upgradeId, int cost)
        {
            if (_progression == null) return false;
            if (!_progression.TryPurchase(upgradeId, cost)) return false;
            _progression.Save();
            RecomputeEffects();
            return true;
        }

        public void AddCredits(int amount)
        {
            if (_progression == null) return;
            _progression.AddCredits(amount);
            _progression.Save();
        }

        public void RefreshFromDisk()
        {
            _progression = ProgressionData.Load();
            RecomputeEffects();
        }
    }
}

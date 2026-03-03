using System;
using System.Collections.Generic;
using UnityEngine;

namespace NightShift.Core
{
    /// <summary>
    /// Persistent progression: credits and purchased upgrades.
    /// Save/load via PlayerPrefs (JSON).
    /// </summary>
    [Serializable]
    public class ProgressionData
    {
        public int credits;
        public string[] purchasedUpgradesArray = Array.Empty<string>();

        private const string PrefsKey = "NightShift_Progression";

        private HashSet<string> _purchasedSet;

        public HashSet<string> PurchasedUpgrades
        {
            get
            {
                if (_purchasedSet == null)
                {
                    _purchasedSet = new HashSet<string>();
                    if (purchasedUpgradesArray != null)
                    {
                        foreach (var id in purchasedUpgradesArray)
                            if (!string.IsNullOrEmpty(id))
                                _purchasedSet.Add(id);
                    }
                }
                return _purchasedSet;
            }
        }

        public static ProgressionData Load()
        {
            string json = PlayerPrefs.GetString(PrefsKey, null);
            if (string.IsNullOrEmpty(json))
                return new ProgressionData();

            try
            {
                var data = JsonUtility.FromJson<ProgressionData>(json);
                return data ?? new ProgressionData();
            }
            catch
            {
                return new ProgressionData();
            }
        }

        public void Save()
        {
            purchasedUpgradesArray = PurchasedUpgrades != null
                ? new List<string>(PurchasedUpgrades).ToArray()
                : Array.Empty<string>();
            string json = JsonUtility.ToJson(this);
            PlayerPrefs.SetString(PrefsKey, json);
            PlayerPrefs.Save();
        }

        public void AddCredits(int amount)
        {
            credits = Mathf.Max(0, credits + amount);
        }

        public bool TryPurchase(string upgradeId, int cost)
        {
            if (credits < cost) return false;
            if (PurchasedUpgrades.Contains(upgradeId)) return false;
            credits -= cost;
            PurchasedUpgrades.Add(upgradeId);
            return true;
        }

        public bool HasUpgrade(string upgradeId)
        {
            return PurchasedUpgrades != null && PurchasedUpgrades.Contains(upgradeId);
        }

        public static void ClearAll()
        {
            PlayerPrefs.DeleteKey(PrefsKey);
            PlayerPrefs.Save();
        }
    }
}

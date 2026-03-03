using UnityEngine;
using UnityEngine.UI;
using NightShift.Core;
using NightShift.Systems;

namespace NightShift.UI
{
    public class UpgradeShopItemView : MonoBehaviour
    {
        private UpgradeDefinition _def;
        private Button _buyButton;
        private Text _costText;

        public void Init(UpgradeDefinition def, Button buyButton, Text costText)
        {
            _def = def;
            _buyButton = buyButton;
            _costText = costText;
        }

        public void Refresh()
        {
            if (_def == null) return;
            bool owned = UpgradeManager.Instance != null && UpgradeManager.Instance.Progression != null &&
                UpgradeManager.Instance.Progression.HasUpgrade(_def.id);
            bool canAfford = UpgradeManager.Instance != null && UpgradeManager.Instance.Credits >= _def.cost;
            if (_buyButton != null)
                _buyButton.interactable = !owned && canAfford;
            if (_costText != null)
                _costText.text = owned ? "OWNED" : $"{_def.cost} credits";
        }
    }
}

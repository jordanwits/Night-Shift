using UnityEngine;
using NightShift.Core;
using NightShift.Systems;
using NightShift.Generation;

namespace NightShift.Debug
{
    /// <summary>
    /// Mandatory debug overlay: time, instability, anomaly count, tier, seed.
    /// </summary>
    public class DebugOverlay : MonoBehaviour
    {
        [Header("Visibility")]
        [SerializeField] private bool _showOverlay = true;
        [SerializeField] private KeyCode _toggleKey = KeyCode.F1;

        private GUIStyle _style;
        private bool _stylesInitialized;

        private void Update()
        {
            if (Input.GetKeyDown(_toggleKey))
                _showOverlay = !_showOverlay;
        }

        private void OnGUI()
        {
            if (!_showOverlay) return;

            InitStyles();
            float y = 10;
            float lineHeight = 22;

            DrawLine(ref y, lineHeight, $"Time: {GetTime()}");
            DrawLine(ref y, lineHeight, $"Instability: {GetInstability():F1}%");
            DrawLine(ref y, lineHeight, $"Active Anomalies: {GetAnomalyCount()}");
            DrawLine(ref y, lineHeight, $"Tier: {GetTierName()}");
            DrawLine(ref y, lineHeight, $"Seed: {GetSeed()}");
        }

        private void InitStyles()
        {
            if (_stylesInitialized) return;
            _style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                normal = { textColor = Color.white },
                padding = new RectOffset(10, 10, 4, 4)
            };
            _stylesInitialized = true;
        }

        private void DrawLine(ref float y, float h, string text)
        {
            GUI.Label(new Rect(10, y, 400, h), text, _style);
            y += h;
        }

        private string GetTime()
        {
            return GameTimeManager.Instance != null
                ? GameTimeManager.Instance.GetFormattedTime()
                : "--:--";
        }

        private float GetInstability()
        {
            return InstabilityManager.Instance != null
                ? InstabilityManager.Instance.Instability
                : 0;
        }

        private int GetAnomalyCount()
        {
            return AnomalyManager.Instance != null
                ? AnomalyManager.Instance.ActiveCount
                : 0;
        }

        private string GetTierName()
        {
            if (InstabilityManager.Instance == null) return "-";
            int tier = InstabilityManager.Instance.CurrentTier;
            return InstabilityManager.Instance.GetTierName(tier);
        }

        private int GetSeed()
        {
            return MallGenerator.Instance != null
                ? MallGenerator.Instance.Seed
                : 0;
        }
    }
}

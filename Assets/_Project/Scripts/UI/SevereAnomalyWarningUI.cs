using UnityEngine;
using UnityEngine.UI;
using NightShift.Systems;

namespace NightShift.UI
{
    /// <summary>
    /// Shows "⚠ SEVERE ANOMALY ACTIVE" when mannequin is spawned and active.
    /// </summary>
    public class SevereAnomalyWarningUI : MonoBehaviour
    {
        [SerializeField] private GameObject _panel;
        [SerializeField] private Text _text;
        [SerializeField] private string _warningText = "⚠ SEVERE ANOMALY ACTIVE";

        private MannequinSpawner _spawner;

        private void Awake()
        {
            _spawner = FindFirstObjectByType<MannequinSpawner>();
            if (_panel != null)
                _panel.SetActive(false);
        }

        private void Update()
        {
            bool active = _spawner != null && _spawner.IsMannequinActive;
            if (_panel != null)
                _panel.SetActive(active);
            if (_text != null && active)
                _text.text = _warningText;
        }

        /// <summary>Create warning UI at runtime if none exists.</summary>
        public static SevereAnomalyWarningUI Ensure()
        {
            var existing = FindFirstObjectByType<SevereAnomalyWarningUI>();
            if (existing != null)
                return existing;

            var canvasGo = new GameObject("SevereAnomalyWarningCanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9997;

            canvasGo.AddComponent<CanvasScaler>();

            var panelGo = new GameObject("WarningPanel");
            panelGo.transform.SetParent(canvasGo.transform, false);
            var panelRect = panelGo.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 1f);
            panelRect.anchorMax = new Vector2(0.5f, 1f);
            panelRect.pivot = new Vector2(0.5f, 1f);
            panelRect.anchoredPosition = new Vector2(0, -20);
            panelRect.sizeDelta = new Vector2(400, 40);

            var img = panelGo.AddComponent<Image>();
            img.color = new Color(0.4f, 0f, 0f, 0.7f);

            var textGo = new GameObject("WarningText");
            textGo.transform.SetParent(panelGo.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var text = textGo.AddComponent<Text>();
            text.text = "⚠ SEVERE ANOMALY ACTIVE";
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 24;
            text.color = Color.yellow;
            text.alignment = TextAnchor.MiddleCenter;

            var warning = canvasGo.AddComponent<SevereAnomalyWarningUI>();
            warning._panel = panelGo;
            warning._text = text;
            panelGo.SetActive(false);

            return warning;
        }
    }
}

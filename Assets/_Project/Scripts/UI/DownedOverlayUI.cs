using UnityEngine;
using UnityEngine.UI;
using NightShift.Core;

namespace NightShift.UI
{
    /// <summary>
    /// Shows "DOWNED" overlay when player is downed.
    /// </summary>
    public class DownedOverlayUI : MonoBehaviour
    {
        [SerializeField] private GameObject _panel;
        [SerializeField] private Text _text;
        [SerializeField] private string _downedText = "DOWNED";

        private void Awake()
        {
            if (_panel != null)
                _panel.SetActive(false);
        }

        private void Update()
        {
            bool downed = PlayerStateProxy.IsDowned;
            if (_panel != null)
                _panel.SetActive(downed);
            if (_text != null && downed)
                _text.text = _downedText;
        }

        /// <summary>Create overlay at runtime if none exists.</summary>
        public static DownedOverlayUI Ensure(Camera cameraOrNull)
        {
            var existing = FindFirstObjectByType<DownedOverlayUI>();
            if (existing != null)
                return existing;

            var canvasGo = new GameObject("DownedOverlayCanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9998;

            canvasGo.AddComponent<CanvasScaler>();
            var rect = canvas.GetComponent<RectTransform>();
            if (rect == null)
                rect = canvasGo.AddComponent<RectTransform>();

            var panelGo = new GameObject("DownedPanel");
            panelGo.transform.SetParent(canvasGo.transform, false);
            var panelRect = panelGo.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            var img = panelGo.AddComponent<Image>();
            img.color = new Color(0.5f, 0f, 0f, 0.4f);

            var textGo = new GameObject("DownedText");
            textGo.transform.SetParent(panelGo.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.5f, 0.5f);
            textRect.anchorMax = new Vector2(0.5f, 0.5f);
            textRect.anchoredPosition = Vector2.zero;
            textRect.sizeDelta = new Vector2(400, 80);

            var text = textGo.AddComponent<Text>();
            text.text = "DOWNED";
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 48;
            text.color = Color.red;
            text.alignment = TextAnchor.MiddleCenter;

            var overlay = canvasGo.AddComponent<DownedOverlayUI>();
            overlay._panel = panelGo;
            overlay._text = text;
            panelGo.SetActive(false);

            return overlay;
        }
    }
}

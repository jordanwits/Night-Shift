using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using NightShift.Core;
using NightShift.Systems;

namespace NightShift.UI
{
    /// <summary>
    /// Top-left panel showing last 6 dispatch alerts. Newest at top. Fade after 25 seconds.
    /// </summary>
    public class AlertFeedUI : MonoBehaviour
    {
        private const int MaxVisibleAlerts = 6;
        private const float AlertLifetimeSeconds = 25f;

        private struct AlertEntry
        {
            public DispatchAlert alert;
            public float spawnTime;
        }

        private Canvas _canvas;
        private readonly List<AlertEntry> _entries = new List<AlertEntry>();
        private readonly List<Text> _textElements = new List<Text>();
        private Font _font;

        private void Awake()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            CreateUI();
        }

        private void OnEnable()
        {
            GameEvents.OnDispatchAlert += OnDispatchAlert;
        }

        private void OnDisable()
        {
            GameEvents.OnDispatchAlert -= OnDispatchAlert;
        }

        private void CreateUI()
        {
            var canvasGo = new GameObject("AlertFeedCanvas");
            _canvas = canvasGo.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 500;

            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();

            var panelGo = new GameObject("AlertFeedPanel");
            panelGo.transform.SetParent(canvasGo.transform, false);

            var panelRect = panelGo.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0, 1);
            panelRect.anchorMax = new Vector2(0, 1);
            panelRect.pivot = new Vector2(0, 1);
            panelRect.anchoredPosition = new Vector2(10, -10);
            panelRect.sizeDelta = new Vector2(320, 180);

            var panelImage = panelGo.AddComponent<Image>();
            panelImage.color = new Color(0.05f, 0.05f, 0.08f, 0.85f);

            var layout = panelGo.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            layout.spacing = 4;
            layout.padding = new RectOffset(8, 8, 8, 8);

            for (int i = 0; i < MaxVisibleAlerts; i++)
            {
                var textGo = new GameObject($"Alert{i}");
                textGo.transform.SetParent(panelGo.transform, false);
                var textRect = textGo.AddComponent<RectTransform>();
                textRect.sizeDelta = new Vector2(290, 24);

                var text = textGo.AddComponent<Text>();
                text.font = _font;
                text.fontSize = 12;
                text.color = Color.white;
                text.text = "";
                text.supportRichText = true;
                _textElements.Add(text);
            }

            canvasGo.transform.SetParent(transform);
        }

        private void OnDispatchAlert(DispatchAlert alert)
        {
            _entries.Insert(0, new AlertEntry { alert = alert, spawnTime = Time.time });
            while (_entries.Count > MaxVisibleAlerts)
                _entries.RemoveAt(_entries.Count - 1);

            RefreshDisplay();
        }

        private void Update()
        {
            bool changed = false;
            for (int i = _entries.Count - 1; i >= 0; i--)
            {
                if (Time.time - _entries[i].spawnTime > AlertLifetimeSeconds)
                {
                    _entries.RemoveAt(i);
                    changed = true;
                }
            }
            if (changed)
                RefreshDisplay();
        }

        private void RefreshDisplay()
        {
            for (int i = 0; i < _textElements.Count; i++)
            {
                if (i < _entries.Count)
                {
                    var entry = _entries[i];
                    float age = Time.time - entry.spawnTime;
                    float alpha = age < AlertLifetimeSeconds - 2f ? 1f : 1f - (age - (AlertLifetimeSeconds - 2f)) / 2f;
                    var c = entry.alert.severity == DispatchSeverity.Severe ? new Color(1f, 0.5f, 0.5f) : Color.white;
                    c.a = alpha;

                    _textElements[i].text = entry.alert.message;
                    _textElements[i].color = c;
                    _textElements[i].gameObject.SetActive(true);
                }
                else
                {
                    _textElements[i].gameObject.SetActive(false);
                }
            }
        }
    }
}

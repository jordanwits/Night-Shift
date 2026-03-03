using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using NightShift.Core;
using NightShift.Systems;

namespace NightShift.UI
{
    /// <summary>
    /// Minimal report UI: opens on R when near anomaly. Lists anomaly types, Submit/Cancel.
    /// Blocks player input while open.
    /// </summary>
    public class ReportUIController : MonoBehaviour
    {
        public static bool IsOpen { get; private set; }

        private Canvas _canvas;
        private GameObject _panel;
        private Text _titleText;
        private RectTransform _typeButtonsRoot;
        private List<Button> _typeButtons = new List<Button>();
        private Button _submitButton;
        private Button _cancelButton;
        private Text _feedbackText;

        private AnomalyInstance _target;
        private List<AnomalyDefinition> _definitions = new List<AnomalyDefinition>();
        private int _selectedIndex;
        private Coroutine _feedbackCoroutine;

        private void Update()
        {
            if (IsOpen && !_feedbackText.gameObject.activeSelf && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                OnCancel();
        }

        private void Awake()
        {
            CreateUI();
            Hide();
        }

        private void CreateUI()
        {
            EnsureEventSystem();

            var canvasGo = new GameObject("ReportUICanvas");
            _canvas = canvasGo.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 1000;

            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();

            // Panel background
            _panel = new GameObject("ReportPanel");
            _panel.transform.SetParent(canvasGo.transform, false);

            var panelRect = _panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(400, 320);
            panelRect.anchoredPosition = Vector2.zero;

            var panelImage = _panel.AddComponent<Image>();
            panelImage.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);

            // Title
            var titleGo = new GameObject("Title");
            titleGo.transform.SetParent(_panel.transform, false);
            var titleRect = titleGo.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 1);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.pivot = new Vector2(0.5f, 1);
            titleRect.anchoredPosition = new Vector2(0, -15);
            titleRect.sizeDelta = new Vector2(-40, 40);

            _titleText = titleGo.AddComponent<Text>();
            _titleText.text = "FILE INCIDENT REPORT";
            _titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _titleText.fontSize = 22;
            _titleText.color = Color.white;
            _titleText.alignment = TextAnchor.MiddleCenter;

            // Type buttons container (populated when shown)
            var rootGo = new GameObject("TypeButtonsRoot");
            rootGo.transform.SetParent(_panel.transform, false);
            _typeButtonsRoot = rootGo.AddComponent<RectTransform>();
            _typeButtonsRoot.anchorMin = new Vector2(0.5f, 0.5f);
            _typeButtonsRoot.anchorMax = new Vector2(0.5f, 0.5f);
            _typeButtonsRoot.sizeDelta = new Vector2(340, 120);
            _typeButtonsRoot.anchoredPosition = new Vector2(0, 20);

            // Buttons
            var submitGo = CreateButton("Submit", new Vector2(-60, -100));
            submitGo.transform.SetParent(_panel.transform, false);
            _submitButton = submitGo.GetComponent<Button>();
            _submitButton.onClick.AddListener(OnSubmit);

            var cancelGo = CreateButton("Cancel", new Vector2(60, -100));
            cancelGo.transform.SetParent(_panel.transform, false);
            _cancelButton = cancelGo.GetComponent<Button>();
            _cancelButton.onClick.AddListener(OnCancel);

            // Feedback text (hidden by default, shown after submit)
            var feedbackGo = new GameObject("FeedbackText");
            feedbackGo.transform.SetParent(_panel.transform, false);
            var feedbackRect = feedbackGo.AddComponent<RectTransform>();
            feedbackRect.anchorMin = new Vector2(0.5f, 0.5f);
            feedbackRect.anchorMax = new Vector2(0.5f, 0.5f);
            feedbackRect.sizeDelta = new Vector2(350, 60);
            feedbackRect.anchoredPosition = new Vector2(0, 0);

            _feedbackText = feedbackGo.AddComponent<Text>();
            _feedbackText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _feedbackText.fontSize = 24;
            _feedbackText.color = Color.white;
            _feedbackText.alignment = TextAnchor.MiddleCenter;
            feedbackGo.SetActive(false);

            canvasGo.transform.SetParent(transform);
        }

        private static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null)
                return;

            var esGo = new GameObject("EventSystem");
            esGo.AddComponent<EventSystem>();
            esGo.AddComponent<InputSystemUIInputModule>();
        }

        private GameObject CreateButton(string label, Vector2 position)
        {
            var go = new GameObject(label + "Button");
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0);
            rect.anchorMax = new Vector2(0.5f, 0);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(120, 35);
            rect.anchoredPosition = position;

            var image = go.AddComponent<Image>();
            image.color = new Color(0.3f, 0.3f, 0.3f);

            var button = go.AddComponent<Button>();
            button.targetGraphic = image;

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            var text = textGo.AddComponent<Text>();
            text.text = label;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 16;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;

            return go;
        }

        public void Show(AnomalyInstance target)
        {
            if (target == null || !target.IsActive || AnomalyManager.Instance == null)
                return;

            _target = target;
            _definitions.Clear();

            foreach (var def in AnomalyManager.Instance.AvailableDefinitions)
            {
                if (def != null)
                    _definitions.Add(def);
            }

            if (_definitions.Count == 0)
            {
                Debug.LogWarning("[ReportUI] No anomaly definitions available.");
                return;
            }

            // Clear old type buttons
            foreach (var b in _typeButtons)
            {
                if (b != null && b.gameObject != null)
                    Destroy(b.gameObject);
            }
            _typeButtons.Clear();

            // Create a button per definition
            float y = 0f;
            float step = 32f;
            for (int i = 0; i < _definitions.Count; i++)
            {
                int index = i;
                var def = _definitions[i];
                string label = string.IsNullOrEmpty(def.displayName) ? def.id : def.displayName;

                var btnGo = CreateButton(label, new Vector2(0, y));
                btnGo.transform.SetParent(_typeButtonsRoot, false);
                var btn = btnGo.GetComponent<Button>();
                btn.onClick.AddListener(() => _selectedIndex = index);
                _typeButtons.Add(btn);
                y -= step;
            }
            _selectedIndex = 0;

            _titleText.gameObject.SetActive(true);
            _typeButtonsRoot.gameObject.SetActive(true);
            _submitButton.gameObject.SetActive(true);
            _cancelButton.gameObject.SetActive(true);
            _feedbackText.gameObject.SetActive(false);

            _canvas.enabled = true;
            _panel.SetActive(true);
            IsOpen = true;
        }

        public void Hide()
        {
            _target = null;
            _canvas.enabled = false;
            _panel?.SetActive(false);
            IsOpen = false;

            if (_feedbackCoroutine != null)
            {
                StopCoroutine(_feedbackCoroutine);
                _feedbackCoroutine = null;
            }
        }

        private void OnSubmit()
        {
            if (_target == null || !_target.IsActive || AnomalyManager.Instance == null)
            {
                Hide();
                return;
            }

            int index = Mathf.Clamp(_selectedIndex, 0, _definitions.Count - 1);
            var selectedDef = _definitions[index];

            bool correct = selectedDef != null && _target.Definition != null &&
                selectedDef.id == _target.Definition.id;

            AnomalyManager.Instance.FileReport(_target, selectedDef);

            _titleText.gameObject.SetActive(false);
            _typeButtonsRoot.gameObject.SetActive(false);
            _submitButton.gameObject.SetActive(false);
            _cancelButton.gameObject.SetActive(false);

            _feedbackText.text = correct ? "REPORT ACCEPTED" : "REPORT REJECTED";
            _feedbackText.color = correct ? new Color(0.4f, 1f, 0.4f) : new Color(1f, 0.4f, 0.4f);
            _feedbackText.gameObject.SetActive(true);

            _target = null;

            if (_feedbackCoroutine != null)
                StopCoroutine(_feedbackCoroutine);
            _feedbackCoroutine = StartCoroutine(FeedbackThenClose());
        }

        private IEnumerator FeedbackThenClose()
        {
            yield return new WaitForSeconds(2f);
            _feedbackCoroutine = null;
            Hide();
        }

        private void OnCancel()
        {
            Hide();
        }
    }
}

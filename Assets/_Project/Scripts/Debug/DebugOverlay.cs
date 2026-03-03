using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using NightShift.Core;
using NightShift.Systems;

namespace NightShift.Debug
{
    /// <summary>
    /// Debug overlay (Unity UI): GameState, Time, Instability.
    /// F1 toggle, F2 +5 instability, F3 -5, F4 restart run.
    /// </summary>
    public class DebugOverlay : MonoBehaviour
    {
        [SerializeField] private bool _showOverlay = true;

        private Canvas _canvas;
        private Text _text;
        private GameStateManager _stateManager;
        private GameClock _clock;
        private InstabilityManager _instability;
        private AnomalyManager _anomalyManager;

        private void Awake()
        {
            CreateCanvas();
            _stateManager = FindFirstObjectByType<GameStateManager>();
            _clock = FindFirstObjectByType<GameClock>();
            _instability = FindFirstObjectByType<InstabilityManager>();
            _anomalyManager = FindFirstObjectByType<AnomalyManager>();
        }

        private void CreateCanvas()
        {
            var canvasGo = new GameObject("DebugOverlayCanvas");
            _canvas = canvasGo.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 9999;

            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();

            var textGo = new GameObject("DebugOverlayText");
            textGo.transform.SetParent(canvasGo.transform, false);

            var rect = textGo.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(10, -10);
            rect.sizeDelta = new Vector2(400, 120);

            _text = textGo.AddComponent<Text>();
            _text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _text.fontSize = 16;
            _text.color = Color.white;
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            // F1: toggle overlay
            if (kb.f1Key.wasPressedThisFrame)
                _showOverlay = !_showOverlay;

            // F2: +5 instability
            if (kb.f2Key.wasPressedThisFrame && _instability != null)
                _instability.Add(5f);

            // F3: -5 instability
            if (kb.f3Key.wasPressedThisFrame && _instability != null)
                _instability.Add(-5f);

            // F4: restart run (reload scene)
            if (kb.f4Key.wasPressedThisFrame)
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);

            // F5: spawn anomaly at (0,0,0)
            if (kb.f5Key.wasPressedThisFrame && _anomalyManager != null)
                _anomalyManager.DebugSpawnAnomaly();

            if (_showOverlay && _text != null)
            {
                _canvas.enabled = true;
                _text.text = BuildOverlayText();
            }
            else if (_canvas != null)
            {
                _canvas.enabled = false;
            }
        }

        private string BuildOverlayText()
        {
            string state = _stateManager != null ? _stateManager.CurrentState.ToString() : "-";
            string time = _clock != null ? _clock.CurrentTimeText : "--:-- AM";
            float instability = _instability != null ? _instability.Instability : 0f;
            int anomalies = _anomalyManager != null ? _anomalyManager.ActiveCount : 0;

            int reports = _anomalyManager != null ? _anomalyManager.TotalReports : 0;
            int correct = _anomalyManager != null ? _anomalyManager.CorrectReports : 0;
            int incorrect = _anomalyManager != null ? _anomalyManager.IncorrectReports : 0;

            return $"GameState: {state}\nTime: {time}\nInstability: {instability:F1}%\nAnomalies: {anomalies}\nReports: {reports} (✓{correct} ✗{incorrect})";
        }
    }
}

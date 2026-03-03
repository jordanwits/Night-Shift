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

        private const float DistortionChance = 0.15f;

        private Canvas _canvas;
        private Text _text;
        private GameStateManager _stateManager;
        private GameClock _clock;
        private InstabilityManager _instability;
        private AnomalyManager _anomalyManager;
        private DispatchManager _dispatchManager;

        private string _lastDistortedAlert;
        private float _distortedAlertTime;

        private void Awake()
        {
            CreateCanvas();
            _stateManager = FindFirstObjectByType<GameStateManager>();
            _clock = FindFirstObjectByType<GameClock>();
            _instability = FindFirstObjectByType<InstabilityManager>();
            _anomalyManager = FindFirstObjectByType<AnomalyManager>();
            _dispatchManager = FindFirstObjectByType<DispatchManager>();
        }

        private void OnEnable()
        {
            GameEvents.OnDispatchAlert += OnDispatchAlert;
        }

        private void OnDisable()
        {
            GameEvents.OnDispatchAlert -= OnDispatchAlert;
        }

        private void OnDispatchAlert(DispatchAlert alert)
        {
            if (_instability == null || _instability.Instability < 60f) return;
            if (Random.value >= DistortionChance) return;

            _lastDistortedAlert = DistortAlertText(alert.message);
            _distortedAlertTime = Time.time;
        }

        private static string DistortAlertText(string text)
        {
            if (string.IsNullOrEmpty(text) || text.Length < 10) return text;

            var words = text.Split(' ');
            if (words.Length >= 2 && Random.value < 0.5f)
            {
                int i = Random.Range(0, words.Length - 1);
                var t = words[i];
                words[i] = words[i + 1];
                words[i + 1] = t;
                return string.Join(" ", words);
            }

            int pos = Random.Range(0, text.Length);
            char c = text[pos];
            char alt = c == 'a' ? 'e' : c == 'e' ? 'a' : c == 'o' ? 'e' : c == 'r' ? 'n' : c;
            if (alt == c) return text;
            return text.Substring(0, pos) + alt + text.Substring(pos + 1);
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
            rect.sizeDelta = new Vector2(420, 220);

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

            // F5: spawn anomaly (legacy)
            if (kb.f5Key.wasPressedThisFrame && _anomalyManager != null)
                _anomalyManager.DebugSpawnAnomaly();

            // F6: force real anomaly spawn
            if (kb.f6Key.wasPressedThisFrame && _anomalyManager != null)
                _anomalyManager.DebugSpawnAnomaly();

            // F7: force false dispatch alert
            if (kb.f7Key.wasPressedThisFrame && _dispatchManager != null)
                _dispatchManager.DebugForceFalseAlert();

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
            int maxAnomalies = _anomalyManager != null ? _anomalyManager.MaxActiveAnomalies : 4;

            int reports = _anomalyManager != null ? _anomalyManager.TotalReports : 0;
            int correct = _anomalyManager != null ? _anomalyManager.CorrectReports : 0;
            int incorrect = _anomalyManager != null ? _anomalyManager.IncorrectReports : 0;

            int totalAlerts = _dispatchManager != null ? _dispatchManager.TotalAlertsSent : 0;
            int falseAlerts = _dispatchManager != null ? _dispatchManager.FalseAlertsSent : 0;
            float falseChance = GetFalseAlertChance(instability);

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"GameState: {state}");
            sb.AppendLine($"Time: {time}");
            sb.AppendLine($"Instability: {instability:F1}%");
            sb.AppendLine($"Anomalies: {anomalies}/{maxAnomalies}");
            sb.AppendLine($"Reports: {reports} (✓{correct} ✗{incorrect})");
            sb.AppendLine($"False alert chance: {falseChance * 100:F0}%");
            sb.AppendLine($"Alerts: {totalAlerts} total, {falseAlerts} false");

            if (!string.IsNullOrEmpty(_lastDistortedAlert) && instability >= 60f && Time.time - _distortedAlertTime < 30f)
                sb.AppendLine($"[Distorted] {_lastDistortedAlert}");

            return sb.ToString();
        }

        private static float GetFalseAlertChance(float instability)
        {
            if (instability < 30f) return 0f;
            if (instability < 60f) return 0.1f;
            if (instability < 80f) return 0.3f;
            return 0.5f;
        }
    }
}

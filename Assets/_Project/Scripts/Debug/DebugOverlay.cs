using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using NightShift.Core;
using NightShift.Generation;
using NightShift.Systems;

namespace NightShift.Debug
{
    /// <summary>
    /// Debug overlay (Unity UI): GameState, Time, Instability.
    /// F1 toggle, F2 +5 instability, F3 -5, F4 restart run.
    /// </summary>
    [DefaultExecutionOrder(-100)]
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
        private MannequinSpawner _mannequinSpawner;

        private string _lastDistortedAlert;
        private float _distortedAlertTime;

        private void Awake()
        {
            CreateCanvas();
            _stateManager = FindFirstObjectByType<GameStateManager>();
            _clock = FindFirstObjectByType<GameClock>();
            _instability = FindFirstObjectByType<InstabilityManager>();
            _anomalyManager = FindFirstObjectByType<AnomalyManager>();
            _mannequinSpawner = FindFirstObjectByType<MannequinSpawner>();
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
            // No GraphicRaycaster - overlay is display-only and was blocking tablet button clicks

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
            _text.raycastTarget = false;
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

            // F4: restart run (reload scene) — only when Shift not held (Shift+F4 = skip to 6AM)
            if (kb.f4Key.wasPressedThisFrame && !kb.shiftKey.isPressed)
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

            // F8: mark random camera suspicious (real)
            if (kb.f8Key.wasPressedThisFrame && CctvManager.Instance != null)
                CctvManager.Instance.DebugMarkSuspiciousReal();

            // F9: mark random camera suspicious (false)
            if (kb.f9Key.wasPressedThisFrame && CctvManager.Instance != null)
                CctvManager.Instance.DebugMarkSuspiciousFalse();

            // F10: damage player 25
            if (kb.f10Key.wasPressedThisFrame && Player.PlayerVitals.Instance != null)
                Player.PlayerVitals.Instance.DebugDamage(25f);

            // F11: revive player
            if (kb.f11Key.wasPressedThisFrame && Player.PlayerVitals.Instance != null)
                Player.PlayerVitals.Instance.DebugRevive();

            // Shift+F4: regenerate mall (same seed)
            if (kb.shiftKey.isPressed && kb.f4Key.wasPressedThisFrame && MallGenerator.Instance != null)
                MallGenerator.Instance.DebugRegenerateSameSeed();

            // Shift+F5: regenerate mall (new random seed)
            if (kb.shiftKey.isPressed && kb.f5Key.wasPressedThisFrame && MallGenerator.Instance != null)
                MallGenerator.Instance.DebugRegenerateNewSeed();

            // Shift+F6: skip to 6AM (end run as Survived)
            if (kb.shiftKey.isPressed && kb.f6Key.wasPressedThisFrame)
                Core.GameEvents.RaiseRunEnded(Core.RunEndReason.Survived);

            // Shift+F2: add 100 credits
            if (kb.shiftKey.isPressed && kb.f2Key.wasPressedThisFrame && Systems.UpgradeManager.Instance != null)
                Systems.UpgradeManager.Instance.AddCredits(100);

            // Shift+F3: toggle UseFixedSeed (mall)
            if (kb.shiftKey.isPressed && kb.f3Key.wasPressedThisFrame && MallGenerator.Instance != null)
                MallGenerator.Instance.DebugToggleUseFixedSeed();

            // Shift+F7: toggle dressing on/off
            if (kb.shiftKey.isPressed && kb.f7Key.wasPressedThisFrame && MallDresser.Instance != null)
                MallDresser.Instance.ToggleDressing();

            // Shift+F8: toggle floor overlap validation (runs after next generation)
            if (kb.shiftKey.isPressed && kb.f8Key.wasPressedThisFrame)
            {
                FloorOverlapValidator.ValidationEnabled = !FloorOverlapValidator.ValidationEnabled;
                UnityEngine.Debug.Log($"[FloorOverlapValidator] Validation {(FloorOverlapValidator.ValidationEnabled ? "ON" : "OFF")}");
            }

            // Shift+F9: reset progression
            if (kb.shiftKey.isPressed && kb.f9Key.wasPressedThisFrame)
            {
                Core.ProgressionData.ClearAll();
                Systems.UpgradeManager.Instance?.RefreshFromDisk();
            }

            // F12: toggle mannequin spawn
            if (kb.f12Key.wasPressedThisFrame)
            {
                var spawner = FindFirstObjectByType<MannequinSpawner>();
                spawner?.DebugToggleMannequin();
            }

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

            float health = Player.PlayerVitals.Instance != null ? Player.PlayerVitals.Instance.Health : -1f;
            bool downed = Player.PlayerVitals.Instance != null && Player.PlayerVitals.Instance.IsDowned;
            bool mannequinActive = _mannequinSpawner != null && _mannequinSpawner.IsMannequinActive;
            sb.AppendLine($"Player Health: {health:F0}");
            sb.AppendLine($"Player Downed: {(downed ? "yes" : "no")}");
            sb.AppendLine($"Mannequin Active: {(mannequinActive ? "yes" : "no")}");
            sb.AppendLine($"Anomalies: {anomalies}/{maxAnomalies}");
            sb.AppendLine($"Reports: {reports} (✓{correct} ✗{incorrect})");
            sb.AppendLine($"False alert chance: {falseChance * 100:F0}%");
            sb.AppendLine($"Alerts: {totalAlerts} total, {falseAlerts} false");

            int credits = Systems.UpgradeManager.Instance != null ? Systems.UpgradeManager.Instance.Credits : 0;
            int mallSeed = MallGenerator.Instance != null ? MallGenerator.Instance.Seed : 0;
            bool useFixedSeed = MallGenerator.Instance != null && MallGenerator.Instance.UseFixedSeed;
            int mallSections = MallGenerator.Instance != null ? MallGenerator.Instance.SpawnedSections.Count : 0;
            int anomalyPts = MallGenerator.Instance != null ? MallGenerator.Instance.AnomalySpawnPoints.Count : 0;
            int cctvPts = MallGenerator.Instance != null ? MallGenerator.Instance.CctvPoints.Count : 0;
            int propsSpawned = MallDresser.Instance != null ? MallDresser.Instance.PropsSpawned : 0;
            int landmarksSpawned = MallDresser.Instance != null ? MallDresser.Instance.LandmarksSpawned : 0;
            int corridors = MallGenerator.Instance != null ? MallGenerator.Instance.CorridorCount : 0;
            int branches = MallGenerator.Instance != null ? MallGenerator.Instance.BranchCount : 0;
            sb.AppendLine($"Mall: seed={mallSeed} fixed={useFixedSeed} sections={mallSections} corridors={corridors} branches={branches}");
            sb.AppendLine($"AnomalyPts={anomalyPts} CctvPts={cctvPts}");
            sb.AppendLine($"Dressing: props={propsSpawned} landmarks={landmarksSpawned} | Shift+F7: toggle | Shift+F8: floor overlap validate");
            sb.AppendLine($"Credits: {credits} | Shift+F2: +100 | Shift+F3: fixedSeed | Shift+F4: regen | Shift+F5: regen new | Shift+F9: reset prog");

            var tablet = FindFirstObjectByType<NightShift.UI.SecurityTabletUI>();
            bool tabletOpen = tablet != null && tablet.IsTabletOpen;
            string camName = CctvManager.Instance?.CurrentCamera?.CameraName ?? "-";
            bool camSuspicious = CctvManager.Instance != null && CctvManager.Instance.IsCurrentCameraSuspicious;
            int camIndex = CctvManager.Instance != null ? CctvManager.Instance.CurrentIndex : -1;
            int camCount = CctvManager.Instance != null ? CctvManager.Instance.Cameras.Count : 0;
            int displayedSuspicious = CctvManager.Instance != null ? CctvManager.Instance.DisplayedSuspiciousIndex : -1;
            int tier = instability >= 80f ? 3 : instability >= 60f ? 2 : instability >= 30f ? 1 : 0;
            sb.AppendLine($"Tablet: {(tabletOpen ? "OPEN" : "closed")} | Cam: {camName} ({camIndex + 1}/{camCount}) | Suspicious: {camSuspicious} (idx {displayedSuspicious + 1}) | Distort: T{tier}");

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

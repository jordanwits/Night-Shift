using System.Collections.Generic;
using UnityEngine;
using NightShift.Core;

namespace NightShift.Systems
{
    /// <summary>
    /// Receives anomaly spawn events, creates dispatch alerts. Handles false alerts.
    /// Broadcasts OnDispatchAlert.
    /// </summary>
    public class DispatchManager : MonoBehaviour, IGameStateListener
    {
        public static DispatchManager Instance { get; private set; }

        private static readonly string[] RealAlertTemplates =
        {
            "Dispatch: Unusual movement detected near STOREfront.",
            "Dispatch: Escalator anomaly suspected.",
            "Dispatch: Camera feed mismatch reported.",
            "Dispatch: Possible anomaly near food court.",
            "Dispatch: Suspicious activity in corridor."
        };

        private static readonly string[] FalseAlertTemplates =
        {
            "Dispatch: Unusual movement detected near STOREfront.",
            "Dispatch: Escalator anomaly suspected.",
            "Dispatch: Camera feed mismatch reported.",
            "Dispatch: Possible anomaly near food court.",
            "Dispatch: Suspicious activity in corridor."
        };

        private int _totalAlertsSent;
        private int _falseAlertsSent;
        private float _runStartTime;

        public int TotalAlertsSent => _totalAlertsSent;
        public int FalseAlertsSent => _falseAlertsSent;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void Start()
        {
            GameEvents.OnAnomalySpawned += OnAnomalySpawned;
            GameEvents.OnFalseDispatchNeeded += OnFalseDispatchNeeded;
        }

        private void OnDisable()
        {
            GameEvents.OnAnomalySpawned -= OnAnomalySpawned;
            GameEvents.OnFalseDispatchNeeded -= OnFalseDispatchNeeded;
        }

        public void OnGameStateEntered(GameState state)
        {
            if (state == GameState.InRun)
            {
                _totalAlertsSent = 0;
                _falseAlertsSent = 0;
                _runStartTime = Time.time;
            }
        }

        public void OnGameStateExited(GameState state) { }

        private void OnAnomalySpawned(string anomalyId)
        {
            var definition = FindDefinition(anomalyId);
            bool severe = definition != null && definition.isSevere;
            string message = PickRealMessage(anomalyId, definition);
            float timestamp = Time.time - _runStartTime;

            var alert = DispatchAlert.Create(message, severe ? DispatchSeverity.Severe : DispatchSeverity.Normal, timestamp, false);
            _totalAlertsSent++;
            GameEvents.RaiseDispatchAlert(alert);
        }

        private void OnFalseDispatchNeeded()
        {
            string message = FalseAlertTemplates[Random.Range(0, FalseAlertTemplates.Length)];
            float timestamp = Time.time - _runStartTime;
            var alert = DispatchAlert.Create(message, DispatchSeverity.Normal, timestamp, true);
            _totalAlertsSent++;
            _falseAlertsSent++;
            GameEvents.RaiseDispatchAlert(alert);

            float inst = InstabilityManager.Instance != null ? InstabilityManager.Instance.Instability : 0f;
            float chance = inst < 30f ? 0f : inst < 60f ? 0.2f : inst < 80f ? 0.4f : 0.6f;
            if (Random.value < chance && CctvManager.Instance != null)
                CctvManager.Instance.MarkRandomSuspicious(false, 20f);
        }

        private AnomalyDefinition FindDefinition(string anomalyId)
        {
            if (AnomalyManager.Instance == null) return null;
            foreach (var d in AnomalyManager.Instance.AvailableDefinitions)
            {
                if (d != null && d.id == anomalyId) return d;
            }
            return null;
        }

        private static string PickRealMessage(string anomalyId, AnomalyDefinition definition)
        {
            if (definition != null && !string.IsNullOrEmpty(definition.displayName))
            {
                if (definition.id.Contains("escalator"))
                    return "Dispatch: Escalator anomaly suspected.";
                if (definition.id.Contains("mannequin") || definition.id.Contains("rotated"))
                    return "Dispatch: Unusual movement detected near STOREfront.";
                if (definition.id.Contains("duplicate"))
                    return "Dispatch: Camera feed mismatch reported.";
            }
            return RealAlertTemplates[Random.Range(0, RealAlertTemplates.Length)];
        }

        /// <summary>Debug: force a false dispatch alert.</summary>
        public void DebugForceFalseAlert()
        {
            OnFalseDispatchNeeded();
        }
    }
}

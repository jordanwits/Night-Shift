using UnityEngine;
using NightShift.Core;

namespace NightShift.Systems
{
    /// <summary>
    /// Tracks RunStats during InRun: anomaly spawn/resolve, reports, instability, player downed.
    /// </summary>
    public class RunStatsTracker : MonoBehaviour, IGameStateListener
    {
        public static RunStats CurrentStats { get; private set; }

        private RunStats _stats;
        private bool _runActive;

        private void OnEnable()
        {
            GameEvents.OnAnomalySpawned += OnAnomalySpawned;
            GameEvents.OnReportFiled += OnReportFiled;
            GameEvents.OnDispatchAlert += OnDispatchAlert;
            GameEvents.OnInstabilityChanged += OnInstabilityChanged;
            GameEvents.OnRunEnded += OnRunEnded;
        }

        private void OnDisable()
        {
            GameEvents.OnAnomalySpawned -= OnAnomalySpawned;
            GameEvents.OnReportFiled -= OnReportFiled;
            GameEvents.OnDispatchAlert -= OnDispatchAlert;
            GameEvents.OnInstabilityChanged -= OnInstabilityChanged;
            GameEvents.OnRunEnded -= OnRunEnded;
        }

        private void OnAnomalySpawned(string _, string __) { if (_runActive) _stats.anomaliesSpawned++; }

        private void OnReportFiled(AnomalyReportData report)
        {
            if (!_runActive) return;
            _stats.reportsFiled++;
            if (report.wasCorrect)
                _stats.anomaliesResolvedCorrect++;
            else
                _stats.anomaliesResolvedIncorrect++;
        }

        private void OnDispatchAlert(DispatchAlert alert)
        {
            if (_runActive && alert.isFalse)
                _stats.falseAlertsReceived++;
        }

        private void OnInstabilityChanged(float value)
        {
            if (_runActive && value > _stats.maxInstabilityReached)
                _stats.maxInstabilityReached = value;
        }

        private void OnRunEnded(RunEndReason reason)
        {
            if (reason == RunEndReason.Survived)
                _stats.survivedTo6AM = true;
        }

        private void Update()
        {
            if (_runActive && PlayerStateProxy.IsDowned)
                _stats.playerDownedAtAnyPoint = true;
        }

        public void OnGameStateEntered(GameState state)
        {
            if (state == GameState.InRun)
            {
                _stats = RunStats.CreateEmpty();
                CurrentStats = _stats;
                _runActive = true;
            }
        }

        public void OnGameStateExited(GameState state)
        {
            if (state == GameState.InRun)
            {
                _runActive = false;
                CurrentStats = _stats;
            }
        }
    }
}

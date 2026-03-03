using System;

namespace NightShift.Core
{
    /// <summary>
    /// Dispatch alert data. isFalse = alert without real anomaly (false positive).
    /// </summary>
    public struct DispatchAlert
    {
        public string message;
        public DispatchSeverity severity;
        public float timestamp;
        public bool isFalse;

        public static DispatchAlert Create(string message, DispatchSeverity severity, float timestamp, bool isFalse)
        {
            return new DispatchAlert { message = message, severity = severity, timestamp = timestamp, isFalse = isFalse };
        }
    }

    public enum DispatchSeverity { Normal, Severe }

    /// <summary>
    /// Central event bus for game-wide communication.
    /// Systems subscribe to events instead of direct references.
    /// </summary>
    public static class GameEvents
    {
        // === Game State ===
        public static event Action<GameState> OnGameStateChanged;
        public static void RaiseGameStateChanged(GameState state) => OnGameStateChanged?.Invoke(state);

        // === Instability ===
        public static event Action<float> OnInstabilityChanged;
        public static void RaiseInstabilityChanged(float value) => OnInstabilityChanged?.Invoke(value);

        public static event Action<int> OnInstabilityThresholdCrossed;
        public static void RaiseInstabilityThresholdCrossed(int tier) => OnInstabilityThresholdCrossed?.Invoke(tier);

        // === Anomaly ===
        public static event Action<string, string> OnAnomalySpawned;
        public static void RaiseAnomalySpawned(string anomalyId, string storeName = null) => OnAnomalySpawned?.Invoke(anomalyId, storeName);

        public static event Action<string> OnAnomalyFixed;
        public static void RaiseAnomalyFixed(string anomalyId) => OnAnomalyFixed?.Invoke(anomalyId);

        public static event Action<string> OnAnomalyFailed;
        public static void RaiseAnomalyFailed(string anomalyId) => OnAnomalyFailed?.Invoke(anomalyId);

        public static event Action<int> OnActiveAnomalyCountChanged;
        public static void RaiseActiveAnomalyCountChanged(int count) => OnActiveAnomalyCountChanged?.Invoke(count);

        public static event Action<AnomalyReportData> OnReportFiled;
        public static void RaiseReportFiled(AnomalyReportData report) => OnReportFiled?.Invoke(report);

        // === Dispatch ===
        public static event Action<DispatchAlert> OnDispatchAlert;
        public static void RaiseDispatchAlert(DispatchAlert alert) => OnDispatchAlert?.Invoke(alert);

        public static event Action OnFalseDispatchNeeded;
        public static void RaiseFalseDispatchNeeded() => OnFalseDispatchNeeded?.Invoke();

        // === Time ===
        public static event Action<float> OnGameTimeChanged;
        public static void RaiseGameTimeChanged(float time) => OnGameTimeChanged?.Invoke(time);

        public static event Action OnSixAMReached;
        public static void RaiseSixAMReached() => OnSixAMReached?.Invoke();

        // === Run End ===
        public static event Action<RunEndReason> OnRunEnded;
        public static void RaiseRunEnded(RunEndReason reason) => OnRunEnded?.Invoke(reason);

        // === Player Damage (avoids Systems->Player reference) ===
        public static event Action<float> OnPlayerDamageRequested;
        public static void RaisePlayerDamageRequested(float amount) => OnPlayerDamageRequested?.Invoke(amount);

        /// <summary>
        /// Clears all subscribers. Call when transitioning to Bootstrap to prevent stale references.
        /// </summary>
        public static void ClearAll()
        {
            OnGameStateChanged = null;
            OnInstabilityChanged = null;
            OnInstabilityThresholdCrossed = null;
            OnAnomalySpawned = null;
            OnAnomalyFixed = null;
            OnAnomalyFailed = null;
            OnActiveAnomalyCountChanged = null;
            OnReportFiled = null;
            OnDispatchAlert = null;
            OnFalseDispatchNeeded = null;
            OnGameTimeChanged = null;
            OnSixAMReached = null;
            OnRunEnded = null;
            OnPlayerDamageRequested = null;
        }
    }

    /// <summary>
    /// High-level game states.
    /// </summary>
    public enum GameState
    {
        Bootstrap,  // Initializing systems
        InRun,      // Active patrol loop
        EndRun      // Game over, show summary
    }

    /// <summary>
    /// Reason the run ended.
    /// </summary>
    public enum RunEndReason
    {
        Survived,       // Reached 6AM
        InstabilityMax, // Hit 100%
        PlayerDied      // Downed too long (singleplayer)
    }
}

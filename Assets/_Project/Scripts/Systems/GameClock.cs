using System;
using UnityEngine;
using NightShift.Core;

namespace NightShift.Systems
{
    /// <summary>
    /// Simulated in-game time from 12:00 AM to 6:00 AM.
    /// Configurable real-time duration; fires event when reaching 6:00 AM.
    /// </summary>
    public class GameClock : MonoBehaviour, IGameStateListener
    {
        private const float StartHour = 0f;   // 12:00 AM
        private const float EndHour = 6f;     // 6:00 AM
        private const float HoursInRun = EndHour - StartHour; // 6 hours

        [Header("Time")]
        [Tooltip("Real seconds for full 12AM→6AM run (e.g. 12 min = 720 sec)")]
        [SerializeField] private float _realTimeDurationSeconds = 720f;

        private float _currentHour;
        private bool _running;
        private float _effectiveDurationSeconds;

        /// <summary>Normalized time 0..1 (0 = 12AM, 1 = 6AM).</summary>
        public float CurrentTimeNormalized => Mathf.Clamp01((_currentHour - StartHour) / HoursInRun);

        /// <summary>Formatted as "12:34 AM".</summary>
        public string CurrentTimeText => FormatTime(_currentHour);

        /// <summary>Fired once when clock reaches 6:00 AM.</summary>
        public event Action OnSixAMReached;

        public void OnGameStateEntered(GameState state)
        {
            if (state == GameState.InRun)
            {
                _currentHour = StartHour;
                float mult = UpgradeManager.Instance != null ? UpgradeManager.Instance.GetClockDurationMultiplier() : 1f;
                _effectiveDurationSeconds = _realTimeDurationSeconds * mult;
                _running = true;
            }
            else
            {
                _running = false;
            }
        }

        public void OnGameStateExited(GameState state) { }

        private void Update()
        {
            if (!_running)
                return;
            float duration = _effectiveDurationSeconds > 0 ? _effectiveDurationSeconds : _realTimeDurationSeconds;
            if (duration <= 0f) return;

            float progressPerSecond = HoursInRun / duration;
            _currentHour += progressPerSecond * Time.deltaTime;

            GameEvents.RaiseGameTimeChanged(_currentHour);

            if (_currentHour >= EndHour)
            {
                _running = false;
                _currentHour = EndHour;
                OnSixAMReached?.Invoke();
                GameEvents.RaiseSixAMReached();
                GameEvents.RaiseRunEnded(RunEndReason.Survived);
            }
        }

        private static string FormatTime(float hour)
        {
            int h = Mathf.FloorToInt(hour) % 12;
            if (h == 0) h = 12;
            float frac = hour - Mathf.Floor(hour);
            int m = Mathf.FloorToInt(frac * 60f);
            string ampm = (Mathf.FloorToInt(hour) % 24) >= 12 ? "PM" : "AM";
            return $"{h}:{m:D2} {ampm}";
        }
    }
}

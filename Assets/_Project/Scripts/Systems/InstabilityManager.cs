using System;
using UnityEngine;
using NightShift.Core;

namespace NightShift.Systems
{
    /// <summary>
    /// Instability meter 0..100. Methods: Add(float), Set(float).
    /// Threshold events at 30 / 60 / 80 fire once each per run.
    /// </summary>
    public class InstabilityManager : MonoBehaviour, IGameStateListener
    {
        public static InstabilityManager Instance { get; private set; }

        private const float Min = 0f;
        private const float Max = 100f;
        private const int Threshold30 = 30;
        private const int Threshold60 = 60;
        private const int Threshold80 = 80;

        [SerializeField, Range(0f, 100f)] private float _instability;

        private bool _runActive;
        private bool _threshold30Fired;
        private bool _threshold60Fired;
        private bool _threshold80Fired;

        public float Instability => _instability;

        /// <summary>Fired when instability changes.</summary>
        public event Action<float> OnInstabilityChanged;

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

        public void OnGameStateEntered(GameState state)
        {
            if (state == GameState.InRun)
            {
                _runActive = true;
                _threshold30Fired = false;
                _threshold60Fired = false;
                _threshold80Fired = false;
            }
            else if (state == GameState.Bootstrap)
            {
                _instability = 0f;
                OnInstabilityChanged?.Invoke(_instability);
                GameEvents.RaiseInstabilityChanged(_instability);
            }
        }

        public void OnGameStateExited(GameState state)
        {
            if (state == GameState.InRun)
                _runActive = false;
        }

        /// <summary>Add or subtract instability. Clamped 0..100.</summary>
        public void Add(float delta)
        {
            float previous = _instability;
            _instability = Mathf.Clamp(_instability + delta, Min, Max);

            if (Mathf.Approximately(previous, _instability))
                return;

            OnInstabilityChanged?.Invoke(_instability);
            GameEvents.RaiseInstabilityChanged(_instability);
            CheckThresholds();
        }

        /// <summary>Set instability directly. Clamped 0..100.</summary>
        public void Set(float value)
        {
            float previous = _instability;
            _instability = Mathf.Clamp(value, Min, Max);

            if (Mathf.Approximately(previous, _instability))
                return;

            OnInstabilityChanged?.Invoke(_instability);
            GameEvents.RaiseInstabilityChanged(_instability);
            CheckThresholds();
        }

        /// <summary>Alias for Add. Kept for compatibility.</summary>
        public void AddInstability(float delta) => Add(delta);

        /// <summary>Alias for Set. Kept for compatibility.</summary>
        public void SetInstability(float value) => Set(value);

        /// <summary>Debug: set instability directly.</summary>
        public void DebugSetInstability(float value) => Set(value);

        private void CheckThresholds()
        {
            if (!_runActive)
                return;

            if (!_threshold30Fired && _instability >= Threshold30)
            {
                _threshold30Fired = true;
                Debug.Log($"[InstabilityManager] Threshold reached: 30%");
            }
            if (!_threshold60Fired && _instability >= Threshold60)
            {
                _threshold60Fired = true;
                Debug.Log($"[InstabilityManager] Threshold reached: 60%");
            }
            if (!_threshold80Fired && _instability >= Threshold80)
            {
                _threshold80Fired = true;
                Debug.Log($"[InstabilityManager] Threshold reached: 80%");
            }
        }
    }
}

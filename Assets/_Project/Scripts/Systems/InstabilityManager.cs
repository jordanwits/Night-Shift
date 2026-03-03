using System.Collections.Generic;
using UnityEngine;
using NightShift.Core;

namespace NightShift.Systems
{
    /// <summary>
    /// Global instability meter 0-100. Threshold events at 30, 60, 80.
    /// Event-driven: other systems subscribe via IInstabilityListener or GameEvents.
    /// </summary>
    public class InstabilityManager : MonoBehaviour, IGameStateListener
    {
        public static InstabilityManager Instance { get; private set; }

        [Header("Instability")]
        [SerializeField, Range(0f, 100f)] private float _instability = 0f;
        [SerializeField] private float _passiveDecayPerSecond = 0.5f; // Slight natural decay
        [SerializeField] private float _maxInstability = 100f;

        [Header("Thresholds")]
        [SerializeField] private int _lightFlickerTier = 30;
        [SerializeField] private int _radioDistortionTier = 60;
        [SerializeField] private int _hallucinationTier = 80;

        private readonly List<IInstabilityListener> _listeners = new List<IInstabilityListener>();
        private int _currentTier;
        private bool _runActive;

        public float Instability => _instability;
        public int CurrentTier => _currentTier;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            foreach (var mb in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            {
                if (mb is IInstabilityListener il)
                    RegisterListener(il);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void Update()
        {
            if (!_runActive || _currentState != GameState.InRun)
                return;

            // Passive decay (optional - keeps instability from permanently sitting at high values)
            if (_instability > 0 && _passiveDecayPerSecond > 0)
            {
                AddInstability(-_passiveDecayPerSecond * Time.deltaTime);
            }
        }

        private GameState _currentState;

        public void OnGameStateEntered(GameState state)
        {
            _currentState = state;
            if (state == GameState.InRun)
                _runActive = true;
            else if (state == GameState.Bootstrap)
                ResetForNewRun();
        }

        public void OnGameStateExited(GameState state)
        {
            if (state == GameState.InRun)
                _runActive = false;
        }

        public void RegisterListener(IInstabilityListener listener)
        {
            if (listener != null && !_listeners.Contains(listener))
                _listeners.Add(listener);
        }

        public void UnregisterListener(IInstabilityListener listener)
        {
            _listeners.Remove(listener);
        }

        /// <summary>
        /// Add or subtract instability. Clamped 0-100.
        /// </summary>
        public void AddInstability(float delta)
        {
            float previous = _instability;
            _instability = Mathf.Clamp(_instability + delta, 0f, _maxInstability);

            if (Mathf.Approximately(previous, _instability))
                return;

            GameEvents.RaiseInstabilityChanged(_instability);
            UpdateTier();

            if (_instability >= _maxInstability)
                GameEvents.RaiseRunEnded(RunEndReason.InstabilityMax);
        }

        /// <summary>
        /// Set instability directly (e.g. for debug slider).
        /// </summary>
        public void SetInstability(float value)
        {
            float delta = value - _instability;
            AddInstability(delta);
        }

        private void UpdateTier()
        {
            int newTier = GetTierForValue(_instability);
            if (newTier == _currentTier)
                return;

            int oldTier = _currentTier;
            _currentTier = newTier;

            GameEvents.RaiseInstabilityThresholdCrossed(_currentTier);

            for (int i = _listeners.Count - 1; i >= 0; i--)
            {
                if (i < _listeners.Count)
                    _listeners[i].OnInstabilityThresholdCrossed(_currentTier);
            }
        }

        /// <summary>
        /// Returns tier: 0 = &lt;30, 1 = 30-59, 2 = 60-79, 3 = 80+.
        /// </summary>
        public int GetTierForValue(float value)
        {
            if (value >= _hallucinationTier) return 3;
            if (value >= _radioDistortionTier) return 2;
            if (value >= _lightFlickerTier) return 1;
            return 0;
        }

        /// <summary>
        /// Human-readable tier name for debug.
        /// </summary>
        public string GetTierName(int tier)
        {
            switch (tier)
            {
                case 1: return "Light Flicker";
                case 2: return "Radio Distortion";
                case 3: return "Hallucination";
                default: return "Stable";
            }
        }

        public void ResetForNewRun()
        {
            _instability = 0f;
            _currentTier = 0;
            GameEvents.RaiseInstabilityChanged(_instability);
        }

        /// <summary>
        /// Debug hook: set instability via slider.
        /// </summary>
        public void DebugSetInstability(float value)
        {
            SetInstability(value);
        }
    }
}

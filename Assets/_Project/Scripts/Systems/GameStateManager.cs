using System;
using System.Collections.Generic;
using UnityEngine;
using NightShift.Core;

namespace NightShift.Systems
{
    /// <summary>
    /// Manages high-level game state: Bootstrap → InRun → EndRun.
    /// On Play, enters InRun automatically.
    /// </summary>
    public class GameStateManager : MonoBehaviour
    {
        public static GameStateManager Instance { get; private set; }

        [SerializeField] private GameState _currentState = GameState.Bootstrap;

        private readonly List<IGameStateListener> _listeners = new List<IGameStateListener>();

        public GameState CurrentState => _currentState;

        /// <summary>Fired when state changes. Args: (newState, previousState).</summary>
        public event Action<GameState, GameState> OnStateChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            foreach (var mb in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            {
                if (mb is IGameStateListener listener)
                    RegisterListener(listener);
            }
        }

        private void Start()
        {
            // On Play: enter InRun automatically
            if (_currentState == GameState.Bootstrap)
                TransitionTo(GameState.InRun);
        }

        public void RegisterListener(IGameStateListener listener)
        {
            if (listener != null && !_listeners.Contains(listener))
                _listeners.Add(listener);
        }

        public void UnregisterListener(IGameStateListener listener)
        {
            _listeners.Remove(listener);
        }

        public void TransitionTo(GameState newState)
        {
            if (_currentState == newState)
                return;

            GameState previous = _currentState;
            _currentState = newState;

            for (int i = _listeners.Count - 1; i >= 0; i--)
            {
                if (i < _listeners.Count)
                    _listeners[i].OnGameStateExited(previous);
            }
            for (int i = 0; i < _listeners.Count; i++)
            {
                _listeners[i].OnGameStateEntered(_currentState);
            }

            OnStateChanged?.Invoke(_currentState, previous);
            GameEvents.RaiseGameStateChanged(_currentState);
        }

        /// <summary>Debug: force transition to a state.</summary>
        public void DebugTransitionTo(GameState state) => TransitionTo(state);

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}

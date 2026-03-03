using System.Collections.Generic;
using UnityEngine;
using NightShift.Core;

namespace NightShift.Systems
{
    /// <summary>
    /// Manages high-level game state flow: Bootstrap → InRun → EndRun.
    /// Notifies listeners and raises events for event-driven systems.
    /// </summary>
    public class GameStateManager : MonoBehaviour
    {
        public static GameStateManager Instance { get; private set; }

        [Header("State")]
        [SerializeField] private GameState _currentState = GameState.Bootstrap;

        private readonly List<IGameStateListener> _listeners = new List<IGameStateListener>();

        public GameState CurrentState => _currentState;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

        }

        private void Start()
        {
            // Auto-register listeners (after all Awakes, so GameBootstrap-created managers are found)
            foreach (var listener in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            {
                if (listener is IGameStateListener gsl)
                    RegisterListener(gsl);
            }
            TransitionTo(GameState.Bootstrap);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
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

        /// <summary>
        /// Transition to a new game state.
        /// </summary>
        public void TransitionTo(GameState newState)
        {
            if (_currentState == newState)
                return;

            GameState previous = _currentState;
            ExitState(previous);
            _currentState = newState;
            EnterState(_currentState);

            GameEvents.RaiseGameStateChanged(_currentState);
        }

        /// <summary>
        /// Debug hook: Force transition (e.g. for testing).
        /// </summary>
        public void DebugTransitionTo(GameState state)
        {
            TransitionTo(state);
        }

        private void ExitState(GameState state)
        {
            for (int i = _listeners.Count - 1; i >= 0; i--)
            {
                if (i < _listeners.Count)
                    _listeners[i].OnGameStateExited(state);
            }
        }

        private void EnterState(GameState state)
        {
            for (int i = 0; i < _listeners.Count; i++)
            {
                _listeners[i].OnGameStateEntered(state);
            }
        }
    }
}

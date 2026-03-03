using UnityEngine;
using NightShift.Core;

namespace NightShift.Systems
{
    /// <summary>
    /// Coordinates Bootstrap phase: waits for systems to initialize, then transitions to InRun.
    /// </summary>
    public class BootstrapCoordinator : MonoBehaviour, IGameStateListener
    {
        [Header("Bootstrap")]
        [SerializeField] private float _bootstrapDelaySeconds = 2f;
        [SerializeField] private bool _skipDelayInEditor;

        private float _timer;
        private bool _inBootstrap;

        public void OnGameStateEntered(GameState state)
        {
            if (state == GameState.Bootstrap)
            {
                _inBootstrap = true;
                _timer = 0f;
            }
        }

        public void OnGameStateExited(GameState state)
        {
            if (state == GameState.Bootstrap)
                _inBootstrap = false;
        }

        private void Update()
        {
            if (!_inBootstrap)
                return;

            float delay = _skipDelayInEditor && Application.isEditor ? 0.5f : _bootstrapDelaySeconds;
            _timer += Time.deltaTime;
            if (_timer >= delay)
            {
                _inBootstrap = false;
                GameStateManager.Instance?.TransitionTo(GameState.InRun);
            }
        }
    }
}

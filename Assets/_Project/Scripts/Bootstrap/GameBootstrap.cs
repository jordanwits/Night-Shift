using UnityEngine;
using NightShift.Systems;
using NightShift.Debug;

namespace NightShift.Core
{
    /// <summary>
    /// Phase 1 bootstrap: creates single "Bootstrap" GameObject with GameStateManager,
    /// GameClock, InstabilityManager, DebugOverlay. Works when pressing Play in Bootstrap.unity.
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureBootstrap()
        {
            if (FindFirstObjectByType<GameBootstrap>() != null)
                return;

            var bootstrap = new GameObject("Bootstrap");
            bootstrap.AddComponent<GameBootstrap>();
        }

        private void Awake()
        {
            EnsureComponent<GameStateManager>();
            EnsureComponent<GameClock>();
            EnsureComponent<InstabilityManager>();
            EnsureComponent<DebugOverlay>();
        }

        private void EnsureComponent<T>() where T : Component
        {
            if (GetComponent<T>() != null)
                return;
            gameObject.AddComponent<T>();
        }
    }
}

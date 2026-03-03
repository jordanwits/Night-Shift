using UnityEngine;
using NightShift.Core;
using NightShift.Systems;

namespace NightShift.Systems
{
    /// <summary>
    /// Listens for run end (6AM or instability max) and transitions to EndRun.
    /// Stores last reason for UI to query.
    /// </summary>
    public class RunEndHandler : MonoBehaviour
    {
        public static RunEndReason LastRunEndReason { get; private set; }

        private void OnEnable()
        {
            GameEvents.OnRunEnded += HandleRunEnded;
        }

        private void OnDisable()
        {
            GameEvents.OnRunEnded -= HandleRunEnded;
        }

        private void HandleRunEnded(RunEndReason reason)
        {
            LastRunEndReason = reason;
            Debug.Log($"[RunEndHandler] Run ended: {reason}");
            GameStateManager.Instance?.TransitionTo(GameState.EndRun);
        }
    }
}

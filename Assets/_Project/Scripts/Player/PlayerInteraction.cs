using UnityEngine;
using UnityEngine.InputSystem;
using NightShift.Core;
using NightShift.Systems;

namespace NightShift.Player
{
    /// <summary>
    /// Detects anomalies in range and resolves on E key.
    /// Temporary: E = correct fix (Resolve(true)).
    /// Uses Input System package.
    /// </summary>
    public class PlayerInteraction : MonoBehaviour
    {
        [Header("Detection")]
        [SerializeField] private float _interactionRange = 5f;

        private void Update()
        {
            if (GameStateManager.Instance?.CurrentState != GameState.InRun)
                return;

            var target = FindNearestAnomaly();
            var kb = Keyboard.current;
            if (target != null && kb != null && kb.eKey.wasPressedThisFrame)
            {
                target.Resolve(true);
            }
        }

        private AnomalyInstance FindNearestAnomaly()
        {
            var instances = FindObjectsByType<AnomalyInstance>(FindObjectsSortMode.None);
            AnomalyInstance nearest = null;
            float nearestDist = float.MaxValue;

            foreach (var instance in instances)
            {
                if (instance == null || !instance.IsActive)
                    continue;

                float d = Vector3.Distance(transform.position, instance.transform.position);
                if (d < _interactionRange && d < nearestDist)
                {
                    nearestDist = d;
                    nearest = instance;
                }
            }
            return nearest;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _interactionRange);
        }
    }
}

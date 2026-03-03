using UnityEngine;
using UnityEngine.InputSystem;
using NightShift.Core;
using NightShift.Systems;
using NightShift.UI;

namespace NightShift.Player
{
    /// <summary>
    /// Detects anomalies in range. R opens report UI when near an active anomaly.
    /// </summary>
    public class PlayerInteraction : MonoBehaviour
    {
        [Header("Detection")]
        [SerializeField] private float _interactionRange = 5f;

        private ReportUIController _reportUI;

        private void Start()
        {
            _reportUI = FindFirstObjectByType<ReportUIController>();
        }

        private void Update()
        {
            if (GameStateManager.Instance?.CurrentState != GameState.InRun)
                return;

            if (ReportUIController.IsOpen)
                return;

            var target = FindNearestAnomaly();
            var kb = Keyboard.current;
            if (target != null && kb != null && kb.rKey.wasPressedThisFrame && _reportUI != null)
            {
                _reportUI.Show(target);
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

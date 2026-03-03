using UnityEngine;
using NightShift.Core;
using NightShift.Systems;

namespace NightShift.Player
{
    /// <summary>
    /// Detects anomalies in range and attempts fix on interaction.
    /// Correct fix reduces instability, incorrect increases it.
    /// </summary>
    public class PlayerInteraction : MonoBehaviour
    {
        [Header("Detection")]
        [SerializeField] private float _interactionRange = 3f;
        [SerializeField] private LayerMask _anomalyLayer = -1;

        [Header("Input")]
        [SerializeField] private KeyCode _interactKey = KeyCode.E;

        private AnomalyInstance _currentTarget;

        private void Update()
        {
            if (GameStateManager.Instance?.CurrentState != GameState.InRun)
                return;

            _currentTarget = FindNearestAnomaly();

            if (_currentTarget != null && Input.GetKeyDown(_interactKey))
            {
                AttemptFix(_currentTarget);
            }
        }

        private AnomalyInstance FindNearestAnomaly()
        {
            var hits = Physics.OverlapSphere(transform.position, _interactionRange, _anomalyLayer);
            float nearestDist = float.MaxValue;
            AnomalyInstance nearest = null;

            foreach (var col in hits)
            {
                var instance = col.GetComponentInParent<AnomalyInstance>();
                if (instance == null || instance.IsFixed) continue;

                float d = Vector3.Distance(transform.position, instance.transform.position);
                if (d < nearestDist)
                {
                    nearestDist = d;
                    nearest = instance;
                }
            }
            return nearest;
        }

        private void AttemptFix(AnomalyInstance instance)
        {
            if (instance == null || instance.IsFixed)
                return;

            string interactionId = instance.Definition?.fixMethod.correctInteractionId ?? "";
            bool correct = instance.AttemptFix(interactionId);

            if (correct)
                instance.MarkFixed();

            AnomalyManager.Instance?.OnAnomalyFixed(instance, correct);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _interactionRange);
        }
    }
}

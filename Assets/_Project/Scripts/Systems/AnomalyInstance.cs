using System;
using UnityEngine;
using NightShift.Core;

namespace NightShift.Systems
{
    /// <summary>
    /// Runtime component attached to spawned anomaly instances.
    /// Tracks definition, active state, and resolution.
    /// </summary>
    public class AnomalyInstance : MonoBehaviour
    {
        public AnomalyDefinition Definition { get; set; }
        public bool IsActive { get; private set; } = true;

        /// <summary>Fired when anomaly is resolved. Args: (instance, correctFix).</summary>
        public event Action<AnomalyInstance, bool> OnResolved;

        /// <summary>
        /// Resolve this anomaly. Correct fix reduces instability; incorrect adds penalty.
        /// </summary>
        public void Resolve(bool correctFix)
        {
            if (!IsActive)
                return;

            IsActive = false;

            if (Definition != null && InstabilityManager.Instance != null)
            {
                if (correctFix)
                    InstabilityManager.Instance.Add(-Definition.instabilityReward);
                else
                    InstabilityManager.Instance.Add(Definition.instabilityPenalty);
            }

            OnResolved?.Invoke(this, correctFix);
            AnomalyManager.Instance?.NotifyInstanceResolved(this);

            Destroy(gameObject);
        }
    }
}

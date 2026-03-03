using UnityEngine;

namespace NightShift.Core
{
    [CreateAssetMenu(fileName = "NewAnomaly", menuName = "Night Shift/Anomaly Definition")]
    /// <summary>
    /// ScriptableObject defining an anomaly type. All anomaly data is ScriptableObject-driven.
    /// Asset instances go in ScriptableObjects/Anomalies/.
    /// </summary>
    public class AnomalyDefinition : ScriptableObject
    {
        public string id;
        public string displayName;
        public float instabilityPenalty;
        public float instabilityReward;
        public float spawnWeight;
        public bool isSevere;
        [Tooltip("Instability threshold to activate (for severe anomalies). Default 70.")]
        public float activateInstabilityThreshold = 70f;
        [TextArea(2, 4)]
        public string description;
    }
}

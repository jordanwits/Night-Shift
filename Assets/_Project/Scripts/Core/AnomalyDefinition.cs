using UnityEngine;

namespace NightShift.Core
{
    [CreateAssetMenu(fileName = "NewAnomaly", menuName = "Night Shift/Anomaly Definition")]
    /// <summary>
    /// ScriptableObject defining an anomaly type. All gameplay data is SO-driven.
    /// Asset instances go in ScriptableObjects/Anomalies/.
    /// </summary>
    public class AnomalyDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string id;
        public string displayName;
        [TextArea(2, 4)]
        public string description;

        [Header("Gameplay")]
        [Range(1, 5)] public int severity = 1;
        public float baseInstabilityPenalty = 5f;   // When left unfixed or failed fix
        public float rewardValue = 3f;              // Instability reduction on correct fix

        [Header("Spawn")]
        public SpawnRules spawnRules;
        public FixMethod fixMethod;

        [Header("Prefab")]
        public GameObject anomalyPrefab;

        [System.Serializable]
        public class SpawnRules
        {
            public float minInstabilityToSpawn = 0f;
            public float maxInstabilityToSpawn = 100f;
            public int maxConcurrent = 2;
            public float minCooldownSeconds = 30f;
        }

        [System.Serializable]
        public class FixMethod
        {
            public FixType type = FixType.Interact;
            public string correctInteractionId;  // For multiple-choice fixes
        }

        public enum FixType
        {
            Interact,       // Single interaction
            MultiChoice     // Player picks correct option
        }
    }
}

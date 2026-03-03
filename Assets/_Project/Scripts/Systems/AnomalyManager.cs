using System.Collections.Generic;
using UnityEngine;
using NightShift.Core;

namespace NightShift.Systems
{
    /// <summary>
    /// Spawns and tracks anomalies. Event-driven; integrates with InstabilityManager.
    /// </summary>
    public class AnomalyManager : MonoBehaviour, IGameStateListener
    {
        public static AnomalyManager Instance { get; private set; }

        [Header("Definitions")]
        [SerializeField] private List<AnomalyDefinition> _availableAnomalies = new List<AnomalyDefinition>();

        private const string AnomaliesResourcePath = "Anomalies";

        [Header("Spawn")]
        [SerializeField] private Transform _spawnRoot;
        [SerializeField] private float _firstSpawnDelay = 10f;

        private readonly List<AnomalyInstance> _activeAnomalies = new List<AnomalyInstance>();
        private bool _running;

        public IReadOnlyList<AnomalyInstance> ActiveAnomalies => _activeAnomalies;
        public int ActiveCount => _activeAnomalies.Count;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (_spawnRoot == null)
                _spawnRoot = transform;

            if (_availableAnomalies == null || _availableAnomalies.Count == 0)
            {
                var loaded = Resources.LoadAll<AnomalyDefinition>(AnomaliesResourcePath);
                if (loaded != null && loaded.Length > 0)
                {
                    _availableAnomalies = new List<AnomalyDefinition>(loaded);
                    Debug.Log($"[AnomalyManager] Loaded {loaded.Length} definitions from Resources/{AnomaliesResourcePath}");
                }
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void Start()
        {
            GameEvents.OnRunEnded += OnRunEnded;
        }

        private void OnRunEnded(RunEndReason reason)
        {
            _running = false;
        }

        public void OnGameStateEntered(GameState state)
        {
            if (state == GameState.InRun)
            {
                _running = true;
                Invoke(nameof(SpawnFirstTestAnomaly), _firstSpawnDelay);
                _activeAnomalies.Clear();
            }
        }

        public void OnGameStateExited(GameState state)
        {
            if (state == GameState.InRun)
                _running = false;
        }

        private void SpawnFirstTestAnomaly()
        {
            if (_running && _availableAnomalies != null && _availableAnomalies.Count > 0)
            {
                var pos = GetGroundSpawnPosition();
                SpawnAnomaly(pos);
            }
        }

        /// <summary>
        /// Select anomaly via weighted random (spawnWeight), spawn at position.
        /// </summary>
        public AnomalyInstance SpawnAnomaly(Vector3 position)
        {
            var definition = ChooseWeightedDefinition();
            if (definition == null)
                return null;

            var go = AnomalyPlaceholder.Create(definition);
            go.transform.position = position;
            if (_spawnRoot != null)
                go.transform.SetParent(_spawnRoot);

            var instance = go.GetComponent<AnomalyInstance>();
            if (instance == null)
                instance = go.AddComponent<AnomalyInstance>();

            instance.Definition = definition;
            _activeAnomalies.Add(instance);

            GameEvents.RaiseAnomalySpawned(definition.id);
            GameEvents.RaiseActiveAnomalyCountChanged(_activeAnomalies.Count);

            Debug.Log($"[AnomalyManager] Spawned {definition.id} at {position}");
            return instance;
        }

        private AnomalyDefinition ChooseWeightedDefinition()
        {
            if (_availableAnomalies == null || _availableAnomalies.Count == 0)
                return null;

            float totalWeight = 0f;
            foreach (var d in _availableAnomalies)
            {
                if (d != null && d.spawnWeight > 0f)
                    totalWeight += d.spawnWeight;
            }

            if (totalWeight <= 0f)
                return _availableAnomalies[0];

            float r = Random.Range(0f, totalWeight);
            foreach (var d in _availableAnomalies)
            {
                if (d == null || d.spawnWeight <= 0f)
                    continue;
                r -= d.spawnWeight;
                if (r <= 0f)
                    return d;
            }

            return _availableAnomalies[0];
        }

        /// <summary>
        /// Called by AnomalyInstance when resolved. Removes from active list.
        /// </summary>
        public void NotifyInstanceResolved(AnomalyInstance instance)
        {
            if (instance != null && _activeAnomalies.Remove(instance))
                GameEvents.RaiseActiveAnomalyCountChanged(_activeAnomalies.Count);
        }

        /// <summary>
        /// Debug: spawn anomaly in front of camera at ground level.
        /// </summary>
        public void DebugSpawnAnomaly()
        {
            var pos = GetGroundSpawnPosition();
            SpawnAnomaly(pos);
        }

        private Vector3 GetGroundSpawnPosition()
        {
            var cam = Camera.main;
            if (cam == null)
                return new Vector3(0f, 1f, 5f);

            var forward = cam.transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.01f)
                forward = Vector3.forward;
            forward.Normalize();

            var camPos = cam.transform.position;
            return new Vector3(camPos.x, 1f, camPos.z) + forward * 6f;
        }
    }
}

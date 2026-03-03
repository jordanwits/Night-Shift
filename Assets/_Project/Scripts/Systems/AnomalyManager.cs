using System.Collections.Generic;
using UnityEngine;
using NightShift.Core;
using NightShift.Systems;

namespace NightShift.Systems
{
    /// <summary>
    /// Spawns and tracks anomalies. Event-driven; integrates with InstabilityManager.
    /// </summary>
    public class AnomalyManager : MonoBehaviour, IGameStateListener
    {
        public static AnomalyManager Instance { get; private set; }

        [Header("Definitions")]
        [SerializeField] private AnomalyDefinition[] _definitions;

        private const string AnomaliesResourcePath = "Anomalies";

        [Header("Spawn")]
        [SerializeField] private Transform _spawnRoot;
        [SerializeField] private float _spawnRadius = 50f;
        [SerializeField] private float _firstSpawnDelay = 30f;
        [SerializeField] private float _spawnIntervalMin = 20f;
        [SerializeField] private float _spawnIntervalMax = 45f;

        private readonly List<AnomalyInstance> _activeAnomalies = new List<AnomalyInstance>();
        private readonly Dictionary<string, float> _lastSpawnTime = new Dictionary<string, float>();
        private float _nextSpawnTime;
        private bool _running;
        private int _spawnCounter;

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
            DontDestroyOnLoad(gameObject);
            if (_spawnRoot == null)
                _spawnRoot = transform;

            if (_definitions == null || _definitions.Length == 0)
            {
                var loaded = Resources.LoadAll<AnomalyDefinition>(AnomaliesResourcePath);
                if (loaded != null && loaded.Length > 0)
                {
                    _definitions = loaded;
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
            GameEvents.OnInstabilityChanged += OnInstabilityChanged;
        }

        private void OnRunEnded(RunEndReason reason)
        {
            _running = false;
        }

        private void OnInstabilityChanged(float value)
        {
            // Could use for spawn rate scaling
        }

        public void OnGameStateEntered(GameState state)
        {
            if (state == GameState.InRun)
            {
                _running = true;
                _nextSpawnTime = Time.time + _firstSpawnDelay;
                _activeAnomalies.Clear();
            }
        }

        public void OnGameStateExited(GameState state)
        {
            if (state == GameState.InRun)
                _running = false;
        }

        private void Update()
        {
            if (!_running || _definitions == null || _definitions.Length == 0)
                return;

            if (Time.time >= _nextSpawnTime)
            {
                TrySpawnAnomaly();
                _nextSpawnTime = Time.time + Random.Range(_spawnIntervalMin, _spawnIntervalMax);
            }
        }

        private void TrySpawnAnomaly()
        {
            float instability = InstabilityManager.Instance != null ? InstabilityManager.Instance.Instability : 0f;

            var candidates = new List<AnomalyDefinition>();
            foreach (var def in _definitions)
            {
                if (def == null) continue;
                if (instability < def.spawnRules.minInstabilityToSpawn) continue;
                if (instability > def.spawnRules.maxInstabilityToSpawn) continue;

                int count = CountActive(def.id);
                if (count >= def.spawnRules.maxConcurrent) continue;

                if (_lastSpawnTime.TryGetValue(def.id, out float last) && Time.time - last < def.spawnRules.minCooldownSeconds)
                    continue;

                candidates.Add(def);
            }

            if (candidates.Count == 0) return;

            AnomalyDefinition chosen = candidates[Random.Range(0, candidates.Count)];
            SpawnAnomaly(chosen);
        }

        private int CountActive(string anomalyId)
        {
            int c = 0;
            foreach (var a in _activeAnomalies)
            {
                if (a != null && a.Definition != null && a.Definition.id == anomalyId && !a.IsFixed)
                    c++;
            }
            return c;
        }

        /// <summary>
        /// Spawn a specific anomaly (used by manual debug spawn).
        /// </summary>
        public AnomalyInstance SpawnAnomaly(AnomalyDefinition definition)
        {
            if (definition == null)
                return null;

            Vector3 pos = _spawnRoot.position + Random.insideUnitSphere * _spawnRadius;
            pos.y = _spawnRoot.position.y; // Keep on ground plane

            GameObject go = definition.anomalyPrefab != null
                ? Instantiate(definition.anomalyPrefab, pos, Quaternion.identity, _spawnRoot)
                : AnomalyPlaceholder.Create(definition);
            if (go.transform.parent != _spawnRoot)
                go.transform.SetParent(_spawnRoot);
            go.transform.position = pos;

            var instance = go.GetComponent<AnomalyInstance>();
            if (instance == null)
                instance = go.AddComponent<AnomalyInstance>();

            instance.Definition = definition;
            instance.InstanceId = $"{definition.id}_{++_spawnCounter}";

            _activeAnomalies.Add(instance);
            _lastSpawnTime[definition.id] = Time.time;

            GameEvents.RaiseAnomalySpawned(definition.id);
            GameEvents.RaiseActiveAnomalyCountChanged(_activeAnomalies.Count);

            Debug.Log($"[AnomalyManager] Spawned {definition.id} at {pos}");
            return instance;
        }

        public void OnAnomalyFixed(AnomalyInstance instance, bool correct)
        {
            if (instance == null || !_activeAnomalies.Contains(instance))
                return;

            var def = instance.Definition;
            if (correct)
            {
                instance.MarkFixed();
                InstabilityManager.Instance?.AddInstability(-def.rewardValue);
                GameEvents.RaiseAnomalyFixed(def.id);
                Debug.Log($"[AnomalyManager] Fixed {def.id} correctly. -{def.rewardValue} instability.");
            }
            else
            {
                InstabilityManager.Instance?.AddInstability(def.baseInstabilityPenalty);
                GameEvents.RaiseAnomalyFailed(def.id);
                Debug.Log($"[AnomalyManager] Failed fix on {def.id}. +{def.baseInstabilityPenalty} instability.");
            }

            _activeAnomalies.Remove(instance);
            Destroy(instance.gameObject);
            GameEvents.RaiseActiveAnomalyCountChanged(_activeAnomalies.Count);
        }

        private void OnDrawGizmosSelected()
        {
            Transform root = _spawnRoot != null ? _spawnRoot : transform;
            Gizmos.color = new Color(1, 0, 0, 0.2f);
            Gizmos.DrawSphere(root.position, _spawnRadius);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(root.position, _spawnRadius);
        }

        /// <summary>
        /// Debug: Manually spawn first available anomaly.
        /// </summary>
        public void DebugSpawnAnomaly()
        {
            if (_definitions == null || _definitions.Length == 0)
            {
                Debug.LogWarning("[AnomalyManager] No definitions assigned.");
                return;
            }
            foreach (var d in _definitions)
            {
                if (d != null)
                {
                    SpawnAnomaly(d);
                    return;
                }
            }
        }
    }
}

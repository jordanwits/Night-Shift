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
        [SerializeField] private List<Transform> _spawnPoints = new List<Transform>();
        [SerializeField] private float _minSpawnIntervalSeconds = 30f;
        [SerializeField] private float _maxSpawnIntervalSeconds = 75f;
        [SerializeField] private int _maxActiveAnomalies = 4;
        [SerializeField] private int _spawnSeed = -1;

        private readonly List<AnomalyInstance> _activeAnomalies = new List<AnomalyInstance>();
        private readonly List<AnomalyReportData> _reports = new List<AnomalyReportData>();
        private float _runStartTime;
        private bool _running;
        private float _nextSpawnTime;
        private System.Random _spawnRandom;

        public IReadOnlyList<AnomalyInstance> ActiveAnomalies => _activeAnomalies;
        public int MaxActiveAnomalies => _maxActiveAnomalies;
        public IReadOnlyList<AnomalyDefinition> AvailableDefinitions => _availableAnomalies;
        public IReadOnlyList<AnomalyReportData> Reports => _reports;
        public int TotalReports => _reports.Count;
        public int CorrectReports => _reports.FindAll(r => r.wasCorrect).Count;
        public int IncorrectReports => _reports.FindAll(r => !r.wasCorrect).Count;
        public float RunElapsedSeconds => _running ? Time.time - _runStartTime : 0f;
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
                foreach (var instance in _activeAnomalies)
                {
                    if (instance != null && instance.gameObject != null)
                        Destroy(instance.gameObject);
                }
                _running = true;
                _runStartTime = Time.time;
                _reports.Clear();
                _activeAnomalies.Clear();
                _spawnRandom = _spawnSeed >= 0 ? new System.Random(_spawnSeed) : new System.Random();
                ScheduleNextSpawn();
            }
        }

        public void OnGameStateExited(GameState state)
        {
            if (state == GameState.InRun)
                _running = false;
        }

        private void Update()
        {
            if (!_running || _availableAnomalies == null || _availableAnomalies.Count == 0)
                return;

            if (Time.time < _nextSpawnTime)
                return;

            if (_activeAnomalies.Count >= _maxActiveAnomalies)
            {
                ScheduleNextSpawn();
                return;
            }

            float instability = InstabilityManager.Instance != null ? InstabilityManager.Instance.Instability : 0f;
            float falseChance = GetFalseAlertChance(instability);
            float roll = _spawnRandom != null ? (float)_spawnRandom.NextDouble() : Random.value;

            if (roll < falseChance)
            {
                GameEvents.RaiseFalseDispatchNeeded();
            }
            else
            {
                Transform spawnPt = null;
                var pos = GetGroundSpawnPosition();
                if (_spawnPoints != null && _spawnPoints.Count > 0)
                {
                    spawnPt = _spawnPoints[_spawnRandom.Next(_spawnPoints.Count)];
                    if (spawnPt != null)
                    {
                        pos = spawnPt.position;
                        pos.y = Mathf.Max(pos.y, 0.5f);
                    }
                }
                SpawnAnomaly(pos, spawnPt);
            }

            ScheduleNextSpawn();
        }

        private static float GetFalseAlertChance(float instability)
        {
            if (instability < 30f) return 0f;
            if (instability < 60f) return 0.1f;
            if (instability < 80f) return 0.3f;
            return 0.5f;
        }

        private void ScheduleNextSpawn()
        {
            float min = Mathf.Max(1f, _minSpawnIntervalSeconds);
            float max = Mathf.Max(min, _maxSpawnIntervalSeconds);
            float interval = _spawnRandom != null
                ? (float)(min + _spawnRandom.NextDouble() * (max - min))
                : Random.Range(min, max);
            _nextSpawnTime = Time.time + interval;
        }

        /// <summary>
        /// Select anomaly via weighted random (spawnWeight), spawn at position.
        /// </summary>
        public AnomalyInstance SpawnAnomaly(Vector3 position, Transform spawnPoint = null)
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

            string storeName = GetStoreNameFromSpawnPoint(spawnPoint);
            GameEvents.RaiseAnomalySpawned(definition.id, storeName);
            GameEvents.RaiseActiveAnomalyCountChanged(_activeAnomalies.Count);

            if (CctvManager.Instance != null)
                CctvManager.Instance.MarkRandomSuspicious(true, 20f);

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

            float r = _spawnRandom != null ? (float)(_spawnRandom.NextDouble() * totalWeight) : Random.Range(0f, totalWeight);
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
        /// File a report for an anomaly. Compares reported type to actual, applies reward/penalty, resolves anomaly.
        /// </summary>
        public void FileReport(AnomalyInstance instance, AnomalyDefinition reportedDefinition)
        {
            if (instance == null || !instance.IsActive)
                return;

            bool correct = reportedDefinition != null && instance.Definition != null &&
                reportedDefinition.id == instance.Definition.id;

            float timestamp = RunElapsedSeconds;
            var report = AnomalyReportData.Create(
                reportedDefinition?.id ?? string.Empty,
                instance.Definition?.id ?? string.Empty,
                correct,
                timestamp);

            _reports.Add(report);
            GameEvents.RaiseReportFiled(report);

            instance.Resolve(correct);
        }

        /// <summary>
        /// Called by AnomalyInstance when resolved. Removes from active list.
        /// </summary>
        public void NotifyInstanceResolved(AnomalyInstance instance)
        {
            if (instance != null && _activeAnomalies.Remove(instance))
                GameEvents.RaiseActiveAnomalyCountChanged(_activeAnomalies.Count);
        }

        /// <summary>Set spawn points from mall generator. Overrides default camera-relative spawn.</summary>
        public void SetSpawnPoints(IEnumerable<Transform> points)
        {
            _spawnPoints.Clear();
            if (points != null)
            {
                foreach (var t in points)
                {
                    if (t != null)
                        _spawnPoints.Add(t);
                }
            }
            Debug.Log($"[AnomalyManager] Set {_spawnPoints.Count} spawn points from mall.");
        }

        /// <summary>Debug: spawn anomaly in front of camera at ground level.</summary>
        public void DebugSpawnAnomaly()
        {
            var pos = GetGroundSpawnPosition();
            SpawnAnomaly(pos, null);
        }

        private static string GetStoreNameFromSpawnPoint(Transform spawnPoint)
        {
            if (spawnPoint == null) return null;
            var t = spawnPoint;
            while (t != null)
            {
                var label = t.GetComponent<SectionLabel>();
                if (label != null && !string.IsNullOrEmpty(label.storeName))
                    return label.storeName;
                t = t.parent;
            }
            return null;
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

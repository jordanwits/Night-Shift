using System.Collections.Generic;
using UnityEngine;
using NightShift.Core;
using NightShift.Systems;

namespace NightShift.Generation
{
    /// <summary>
    /// Mall Generation v1: modular layout generator using section prefabs with connector points.
    /// Collects spawn points for player, anomalies, and CCTV.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class MallGenerator : MonoBehaviour, IGameStateListener
    {
        public static MallGenerator Instance { get; private set; }

        [Header("Generation")]
        [SerializeField] private GameObject[] _sectionPrefabs;
        [SerializeField] private GameObject _startHubPrefab;
        [SerializeField] private int _sectionCount = 10;
        [SerializeField] private int _seed = 12345;
        [SerializeField] private bool _useFixedSeed = false;
        [SerializeField] private float _connectorSnapDistance = 1.5f;
        [SerializeField] private Transform _generationRoot;
        [SerializeField] private int _maxPlacementTries = 5;
        [SerializeField] private float _boundsInflation = 0.05f;

        private System.Random _rng;

        public bool UseFixedSeed => _useFixedSeed;
        private readonly List<MallSection> _spawnedSections = new List<MallSection>();
        private Transform _playerSpawn;
        private readonly List<Transform> _anomalySpawnPoints = new List<Transform>();
        private readonly List<CctvCameraPoint> _cctvPoints = new List<CctvCameraPoint>();

        public int Seed => _seed;
        public IReadOnlyList<MallSection> SpawnedSections => _spawnedSections;
        public Transform PlayerSpawn => _playerSpawn;
        public IReadOnlyList<Transform> AnomalySpawnPoints => _anomalySpawnPoints;
        public IReadOnlyList<CctvCameraPoint> CctvPoints => _cctvPoints;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            if (_generationRoot == null)
                _generationRoot = transform;

            if (_startHubPrefab == null || _sectionPrefabs == null || _sectionPrefabs.Length == 0)
            {
                var config = Resources.Load<MallGeneratorConfig>("MallSections/MallGeneratorConfig");
                if (config != null)
                {
                    if (_startHubPrefab == null) _startHubPrefab = config.startHubPrefab;
                    if (_sectionPrefabs == null || _sectionPrefabs.Length == 0) _sectionPrefabs = config.sectionPrefabs;
                }
            }

            Generate();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void OnGameStateEntered(GameState state)
        {
            if (state == GameState.InRun)
                ApplySpawnPoints();
        }

        public void OnGameStateExited(GameState state) { }

        /// <summary>Generate mall with current or given seed.</summary>
        public void Generate(int? seed = null)
        {
            ClearExisting();

            if (seed.HasValue)
                _seed = seed.Value;
            else if (!_useFixedSeed)
                _seed = GetNewRandomSeed();

            _rng = new System.Random(_seed);

            _playerSpawn = null;
            _anomalySpawnPoints.Clear();
            _cctvPoints.Clear();

            GameObject startPrefab = _startHubPrefab != null ? _startHubPrefab : GetFirstStartPrefab();
            if (startPrefab == null)
            {
                Debug.LogError("[MallGenerator] No StartHub prefab assigned. Cannot generate mall.");
                return;
            }

            var startPos = NormalizePlacementPosition(_generationRoot.position);
            var start = Instantiate(startPrefab, startPos, Quaternion.identity, _generationRoot);
            start.name = "StartHub_0";
            var startSection = start.GetComponent<MallSection>();
            if (startSection == null)
                startSection = start.AddComponent<MallSection>();

            ApplyFloorZOffset(startSection, 0);
            CollectSpawnPoints(startSection);
            _spawnedSections.Add(startSection);

            var openConnectors = new List<(MallSection section, Transform connector)>();
            foreach (var cp in startSection.ConnectorPoints)
                openConnectors.Add((startSection, cp));

            int placed = 1;
            int maxSections = Mathf.Clamp(_sectionCount, 2, 50);

            while (placed < maxSections && openConnectors.Count > 0)
            {
                int connIdx = _rng.Next(openConnectors.Count);
                var (parentSection, parentConn) = openConnectors[connIdx];
                openConnectors.RemoveAt(connIdx);

                var nextPrefab = PickRandomSectionPrefab();
                if (nextPrefab == null) continue;

                bool placedSection = false;
                for (int t = 0; t < _maxPlacementTries && !placedSection; t++)
                {
                    if (TryPlaceSection(nextPrefab, parentSection.transform, parentConn, out var newSection, out var newOpenConnectors))
                    {
                        CollectSpawnPoints(newSection);
                        _spawnedSections.Add(newSection);
                        newSection.name = $"Section_{placed}";
                        placed++;
                        foreach (var c in newOpenConnectors)
                            openConnectors.Add(c);
                        placedSection = true;
                    }
                }
            }

            EnsureCctvComponents();
            LogGenerationSummary();

            var dresser = FindFirstObjectByType<MallDresser>();
            if (dresser != null)
                dresser.Dress(_spawnedSections, _generationRoot, _seed);

            if (GameStateManager.Instance != null && GameStateManager.Instance.CurrentState == GameState.InRun)
                ApplySpawnPoints();
        }

        private GameObject GetFirstStartPrefab()
        {
            if (_startHubPrefab != null) return _startHubPrefab;
            if (_sectionPrefabs != null)
            {
                foreach (var p in _sectionPrefabs)
                {
                    if (p == null) continue;
                    var ms = p.GetComponent<MallSection>();
                    if (ms != null && ms.IsStart) return p;
                }
            }
            return null;
        }

        private GameObject PickRandomSectionPrefab()
        {
            var pool = new List<GameObject>();
            if (_sectionPrefabs != null)
            {
                foreach (var p in _sectionPrefabs)
                {
                    if (p != null)
                    {
                        var ms = p.GetComponent<MallSection>();
                        if (ms == null || !ms.IsStart)
                            pool.Add(p);
                    }
                }
            }
            if (pool.Count == 0) return null;
            return pool[_rng.Next(pool.Count)];
        }

        private bool TryPlaceSection(GameObject prefab, Transform parentRoot, Transform parentConn,
            out MallSection newSection, out List<(MallSection, Transform)> newOpenConnectors)
        {
            newSection = null;
            newOpenConnectors = new List<(MallSection, Transform)>();

            var ms = prefab.GetComponent<MallSection>();
            if (ms == null)
            {
                var pos = NormalizePlacementPosition(parentConn.position);
                var inst = Instantiate(prefab, pos, Quaternion.identity, _generationRoot);
                newSection = inst.GetComponent<MallSection>() ?? inst.AddComponent<MallSection>();
                foreach (var cp in newSection.ConnectorPoints)
                    newOpenConnectors.Add((newSection, cp));
                if (WouldOverlap(newSection)) { Destroy(inst); newSection = null; return false; }
                ApplyFloorZOffset(newSection, _spawnedSections.Count);
                return true;
            }

            var childConns = ms.ConnectorPoints;
            if (childConns.Count == 0)
            {
                var pos = NormalizePlacementPosition(parentConn.position);
                var inst = Instantiate(prefab, pos, Quaternion.identity, _generationRoot);
                newSection = inst.GetComponent<MallSection>() ?? inst.AddComponent<MallSection>();
                if (WouldOverlap(newSection)) { Destroy(inst); newSection = null; return false; }
                ApplyFloorZOffset(newSection, _spawnedSections.Count);
                return true;
            }

            int usedConnIdx = _rng.Next(childConns.Count);
            Transform childConn = childConns[usedConnIdx];
            Vector3 parentWorldPos = parentConn.position;
            Vector3 parentForward = parentConn.forward;
            parentForward.y = 0f;
            if (parentForward.sqrMagnitude < 0.01f)
                parentForward = Vector3.forward;

            Quaternion rot = Quaternion.FromToRotation(childConn.forward, -parentForward);
            Vector3 placePos = NormalizePlacementPosition(parentWorldPos - rot * childConn.localPosition);

            var instance = Instantiate(prefab, placePos, rot, _generationRoot);
            newSection = instance.GetComponent<MallSection>() ?? instance.AddComponent<MallSection>();
            newSection.CollectMarkersIfNeeded();

            if (WouldOverlap(newSection))
            {
                Destroy(instance);
                newSection = null;
                return false;
            }

            ApplyFloorZOffset(newSection, _spawnedSections.Count);
            var newConns = newSection.ConnectorPoints;
            for (int i = 0; i < newConns.Count; i++)
            {
                if (i == usedConnIdx) continue;
                newOpenConnectors.Add((newSection, newConns[i]));
            }

            return true;
        }

        private const float FloorZOffsetAmount = 0.002f;

        private static void ApplyFloorZOffset(MallSection section, int sectionIndex)
        {
            if (section == null) return;
            float off = (sectionIndex % 2 == 0) ? 0f : FloorZOffsetAmount;
            var floor = section.transform.Find("Floor");
            if (floor != null)
            {
                var fp = floor.localPosition;
                fp.y += off;
                floor.localPosition = fp;
            }
            var ceiling = section.transform.Find("Ceiling");
            if (ceiling != null)
            {
                var cp = ceiling.localPosition;
                cp.y -= off;
                ceiling.localPosition = cp;
            }
        }

        /// <summary>Snap Y to base height. No floating offsets to ensure floor consistency.</summary>
        private Vector3 NormalizePlacementPosition(Vector3 pos)
        {
            float baseY = _generationRoot != null ? _generationRoot.position.y : 0f;
            pos.y = baseY;
            return pos;
        }

        private bool WouldOverlap(MallSection section)
        {
            Bounds newBounds = GetSectionBounds(section);
            if (newBounds.size.sqrMagnitude < 0.0001f) return false;

            float margin = Mathf.Clamp(_boundsInflation, 0.02f, 0.2f);
            Bounds shrinkNew = ShrinkBounds(newBounds, margin);

            foreach (var existing in _spawnedSections)
            {
                if (existing == null || existing == section) continue;
                if (existing.transform == section.transform) continue;

                Bounds existingBounds = GetSectionBounds(existing);
                if (existingBounds.size.sqrMagnitude < 0.0001f) continue;

                Bounds shrinkExisting = ShrinkBounds(existingBounds, margin);
                if (shrinkNew.Intersects(shrinkExisting))
                    return true;
            }
            return false;
        }

        private static Bounds ShrinkBounds(Bounds b, float margin)
        {
            var size = b.size - Vector3.one * (2f * margin);
            if (size.x < 0.01f) size.x = 0.01f;
            if (size.y < 0.01f) size.y = 0.01f;
            if (size.z < 0.01f) size.z = 0.01f;
            return new Bounds(b.center, size);
        }

        private static Bounds GetSectionBounds(MallSection section)
        {
            if (section == null) return new Bounds();
            var renderers = section.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                var colliders = section.GetComponentsInChildren<Collider>();
                if (colliders.Length == 0) return new Bounds(section.transform.position, Vector3.zero);
                var b = colliders[0].bounds;
                for (int i = 1; i < colliders.Length; i++)
                    b.Encapsulate(colliders[i].bounds);
                return b;
            }
            var bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        private void CollectSpawnPoints(MallSection section)
        {
            if (section.PlayerSpawn != null && _playerSpawn == null)
                _playerSpawn = section.PlayerSpawn;

            foreach (var t in section.AnomalySpawnPoints)
            {
                if (t != null)
                    _anomalySpawnPoints.Add(t);
            }

        }

        private void EnsureCctvComponents()
        {
            _cctvPoints.Clear();
            foreach (var s in _spawnedSections)
            {
                foreach (var t in s.CctvPoints)
                {
                    if (t == null) continue;
                    var cctv = t.GetComponent<CctvCameraPoint>();
                    if (cctv == null)
                        cctv = t.gameObject.AddComponent<CctvCameraPoint>();
                    _cctvPoints.Add(cctv);
                }
            }
        }

        private void ClearExisting()
        {
            var dresser = FindFirstObjectByType<MallDresser>();
            dresser?.ClearDressing();

            foreach (var s in _spawnedSections)
            {
                if (s != null && s.gameObject != null)
                    Destroy(s.gameObject);
            }
            _spawnedSections.Clear();
        }

        private void LogGenerationSummary()
        {
            Debug.Log($"[MallGenerator] Generated {_spawnedSections.Count} sections. Seed={_seed}. " +
                $"PlayerSpawn={(_playerSpawn != null ? "yes" : "no")} AnomalyPoints={_anomalySpawnPoints.Count} CctvPoints={_cctvPoints.Count}");
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0f, 1f, 0.5f, 0.6f);
            foreach (var s in _spawnedSections)
            {
                if (s == null) continue;
                Bounds b = GetSectionBounds(s);
                if (b.size.sqrMagnitude > 0.0001f)
                    Gizmos.DrawWireCube(b.center, b.size);
            }
            Gizmos.color = Color.yellow;
            foreach (var s in _spawnedSections)
            {
                if (s == null) continue;
                foreach (var t in s.ConnectorPoints)
                {
                    if (t != null)
                        Gizmos.DrawWireSphere(t.position, 0.3f);
                }
            }
            Gizmos.color = Color.red;
            foreach (var t in _anomalySpawnPoints)
            {
                if (t != null)
                    Gizmos.DrawWireSphere(t.position, 0.25f);
            }
            Gizmos.color = Color.cyan;
            foreach (var c in _cctvPoints)
            {
                if (c != null && c.transform != null)
                    Gizmos.DrawWireSphere(c.transform.position, 0.25f);
            }
            if (_playerSpawn != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(_playerSpawn.position, 0.4f);
            }
        }
#endif

        /// <summary>Debug: regenerate with same seed.</summary>
        public void DebugRegenerateSameSeed() => Generate(_seed);

        /// <summary>Debug: regenerate with new random seed (forces new seed regardless of UseFixedSeed).</summary>
        public void DebugRegenerateNewSeed() => Generate(GetNewRandomSeed());

        /// <summary>Debug: toggle UseFixedSeed at runtime.</summary>
        public void DebugToggleUseFixedSeed()
        {
            _useFixedSeed = !_useFixedSeed;
            Debug.Log($"[MallGenerator] UseFixedSeed={_useFixedSeed}, current seed={_seed}");
        }

        private static int GetNewRandomSeed()
        {
            return unchecked((int)System.DateTime.UtcNow.Ticks) + System.Environment.TickCount;
        }

        private void ApplySpawnPoints()
        {
            if (_playerSpawn != null)
            {
                var player = FindFirstObjectByType<Player.PlayerInteraction>();
                if (player != null)
                {
                    var pos = _playerSpawn.position;
                    pos.y = Mathf.Max(pos.y, 0f);
                    player.transform.position = pos;
                    var cc = player.GetComponent<CharacterController>();
                    if (cc != null) cc.enabled = false;
                    player.transform.position = pos;
                    if (cc != null) cc.enabled = true;
                    Debug.Log($"[MallGenerator] Moved player to spawn at {pos}");
                }
            }
            else
            {
                Debug.LogWarning("[MallGenerator] No PlayerSpawn found. Player stays at default position.");
            }

            if (AnomalyManager.Instance != null)
            {
                if (_anomalySpawnPoints.Count > 0)
                {
                    AnomalyManager.Instance.SetSpawnPoints(_anomalySpawnPoints);
                }
                else
                {
                    Debug.LogWarning("[MallGenerator] No anomaly spawn points. Anomalies will use fallback spawn.");
                }
            }

            if (CctvManager.Instance != null)
            {
                if (_cctvPoints.Count > 0)
                {
                    CctvManager.Instance.RegisterCameras(_cctvPoints);
                }
                else
                {
                    Debug.LogWarning("[MallGenerator] No CCTV points. Using existing cameras or none.");
                }
            }
        }
    }
}

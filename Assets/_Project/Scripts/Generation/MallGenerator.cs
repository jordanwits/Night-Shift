using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
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
            RemoveOverlappingFloors();

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

            ForceSectionRootY(startSection);
            CollectSpawnPoints(startSection);
            _spawnedSections.Add(startSection);

            var openConnectors = new List<(MallSection section, Transform connector)>();
            foreach (var cp in startSection.ConnectorPoints)
                openConnectors.Add((startSection, cp));

            int placed = 1;
            int maxSections = Mathf.Clamp(_sectionCount, 2, 50);
            var prefabPool = GetNonStartPrefabPool();
            if (prefabPool.Count == 0)
                Debug.LogWarning("[MallGenerator] Prefab pool is empty (no non-Start section prefabs). Check MallGeneratorConfig or _sectionPrefabs.");
            Debug.Log($"[MallGenerator] Start: openConnectors={openConnectors.Count} prefabPool={prefabPool.Count} maxSections={maxSections}");

            while (placed < maxSections && openConnectors.Count > 0)
            {
                int connIdx = _rng.Next(openConnectors.Count);
                var (parentSection, parentConn) = openConnectors[connIdx];
                openConnectors.RemoveAt(connIdx);

                bool placedSection = false;
                string rejectReason = "no prefabs";
                foreach (var nextPrefab in Shuffle(prefabPool))
                {
                    if (nextPrefab == null) continue;
                    if (TryPlaceSectionExhaustive(nextPrefab, parentSection.transform, parentConn,
                            out var newSection, out var newOpenConnectors, out rejectReason))
                    {
                        CollectSpawnPoints(newSection);
                        _spawnedSections.Add(newSection);
                        newSection.name = $"Section_{placed}";
                        placed++;
                        foreach (var c in newOpenConnectors)
                            openConnectors.Add(c);
                        placedSection = true;
                        Debug.Log($"[MallGenerator] Placed section {placed}/{maxSections} at connector (prefab={nextPrefab.name})");
                        break;
                    }
                }

                if (!placedSection)
                    Debug.Log($"[MallGenerator] Exhausted connector, could not place (reason: {rejectReason}). Open connectors left: {openConnectors.Count}");
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

        private List<GameObject> GetNonStartPrefabPool()
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
            return pool;
        }

        private GameObject PickRandomSectionPrefab()
        {
            var pool = GetNonStartPrefabPool();
            if (pool.Count == 0) return null;
            return pool[_rng.Next(pool.Count)];
        }

        private List<T> Shuffle<T>(List<T> list)
        {
            var result = new List<T>(list);
            for (int i = result.Count - 1; i > 0; i--)
            {
                int j = _rng.Next(i + 1);
                (result[i], result[j]) = (result[j], result[i]);
            }
            return result;
        }

        /// <summary>Tries all child connectors for the prefab; returns true if any placement succeeds.</summary>
        private bool TryPlaceSectionExhaustive(GameObject prefab, Transform parentRoot, Transform parentConn,
            out MallSection newSection, out List<(MallSection, Transform)> newOpenConnectors, out string rejectReason)
        {
            newSection = null;
            newOpenConnectors = new List<(MallSection, Transform)>();
            rejectReason = "unknown";

            var ms = prefab.GetComponent<MallSection>();
            var childConns = ms != null ? ms.ConnectorPoints : new List<Transform>();
            List<int> indices;
            if (childConns.Count == 0)
            {
                indices = new List<int> { -1 };
            }
            else
            {
                indices = Shuffle(Enumerable.Range(0, childConns.Count).ToList());
            }

            foreach (int childConnIdx in indices)
            {
                Debug.Log($"[MallGenerator] Attempt: prefab={prefab.name} connIdx={childConnIdx}");
                if (TryPlaceSection(prefab, parentRoot, parentConn, childConnIdx,
                        out newSection, out newOpenConnectors, out rejectReason))
                {
                    return true;
                }
                Debug.Log($"[MallGenerator] Rejected: prefab={prefab.name} connIdx={childConnIdx} reason={rejectReason}");
            }
            return false;
        }

        private bool TryPlaceSection(GameObject prefab, Transform parentRoot, Transform parentConn, int childConnIdx,
            out MallSection newSection, out List<(MallSection, Transform)> newOpenConnectors, out string rejectReason)
        {
            newSection = null;
            newOpenConnectors = new List<(MallSection, Transform)>();
            rejectReason = "unknown";

            var ms = prefab.GetComponent<MallSection>();
            if (ms == null)
            {
                var pos = NormalizePlacementPosition(parentConn.position);
                var inst = Instantiate(prefab, pos, Quaternion.identity, _generationRoot);
                newSection = inst.GetComponent<MallSection>() ?? inst.AddComponent<MallSection>();
                foreach (var cp in newSection.ConnectorPoints)
                    newOpenConnectors.Add((newSection, cp));
                if (WouldOverlap(newSection, out rejectReason)) { Destroy(inst); newSection = null; rejectReason = "overlap"; return false; }
                ForceSectionRootY(newSection);
                rejectReason = null;
                return true;
            }

            var childConns = ms.ConnectorPoints;
            if (childConns.Count == 0)
            {
                var pos = NormalizePlacementPosition(parentConn.position);
                var inst = Instantiate(prefab, pos, Quaternion.identity, _generationRoot);
                newSection = inst.GetComponent<MallSection>() ?? inst.AddComponent<MallSection>();
                if (WouldOverlap(newSection, out rejectReason)) { Destroy(inst); newSection = null; rejectReason = "overlap"; return false; }
                ForceSectionRootY(newSection);
                rejectReason = null;
                return true;
            }

            if (childConnIdx < 0 || childConnIdx >= childConns.Count)
            {
                rejectReason = "invalid connector index";
                return false;
            }

            int usedConnIdx = childConnIdx;
            Transform childConn = childConns[usedConnIdx];
            Vector3 parentWorldPos = parentConn.position;
            Vector3 parentForward = parentConn.forward;
            parentForward.y = 0f;
            if (parentForward.sqrMagnitude < 0.01f)
                parentForward = Vector3.forward;

            // Snap: align childConn.forward with -parentForward (facing each other)
            Quaternion rot = Quaternion.FromToRotation(childConn.forward, -parentForward);
            // Position: child connector world pos must equal parent connector world pos exactly
            Vector3 childConnOffsetInWorld = rot * childConn.localPosition;
            Vector3 placePos = NormalizePlacementPosition(parentWorldPos - childConnOffsetInWorld);

            var instance = Instantiate(prefab, placePos, rot, _generationRoot);
            newSection = instance.GetComponent<MallSection>() ?? instance.AddComponent<MallSection>();
            newSection.CollectMarkersIfNeeded();

            // Exact position snap: correct any accumulated offset so connectors align
            Transform instanceChildConn = newSection.ConnectorPoints[usedConnIdx];
            if (instanceChildConn != null)
            {
                Vector3 actualConnPos = instanceChildConn.position;
                Vector3 drift = parentWorldPos - actualConnPos;
                drift.y = 0f;
                if (drift.sqrMagnitude > 0.0001f)
                {
                    instance.transform.position += drift;
                }
            }

            if (WouldOverlap(newSection, out rejectReason))
            {
                Destroy(instance);
                newSection = null;
                rejectReason = "overlap";
                return false;
            }

            ForceSectionRootY(newSection);
            DisableFloorLipAtConnector(newSection, usedConnIdx);
            var newConns = newSection.ConnectorPoints;
            for (int i = 0; i < newConns.Count; i++)
            {
                if (i == usedConnIdx) continue;
                newOpenConnectors.Add((newSection, newConns[i]));
            }

            rejectReason = null;
            return true;
        }

        /// <summary>Force section root Y to base height to avoid vertical offsets and floor z-fighting.</summary>
        private void ForceSectionRootY(MallSection section)
        {
            if (section == null) return;
            float baseY = _generationRoot != null ? _generationRoot.position.y : 0f;
            var p = section.transform.position;
            if (Mathf.Abs(p.y - baseY) > 0.0001f)
            {
                p.y = baseY;
                section.transform.position = p;
            }
        }

        /// <summary>Disable the new section's FloorLip at the connector used for attachment to prevent z-fighting. Keep the existing section's lip enabled.</summary>
        private void DisableFloorLipAtConnector(MallSection section, int connectorIndex)
        {
            if (section == null) return;
            section.CollectMarkersIfNeeded();
            var lips = section.FloorLips;
            if (connectorIndex >= 0 && connectorIndex < lips.Count && lips[connectorIndex] != null)
                lips[connectorIndex].gameObject.SetActive(false);
        }

        /// <summary>Snap Y to base height. No floating offsets to ensure floor consistency.</summary>
        private Vector3 NormalizePlacementPosition(Vector3 pos)
        {
            float baseY = _generationRoot != null ? _generationRoot.position.y : 0f;
            pos.y = baseY;
            return pos;
        }

        private bool WouldOverlap(MallSection section, out string reason)
        {
            reason = null;
            Bounds newBounds = GetSectionBoundsFromBoxColliders(section);
            if (newBounds.size.sqrMagnitude < 0.0001f) return false;

            // Connectors sit ~0.5u inside section edges; use 0.75 shrink to allow intentional junction overlap
            Bounds shrinkNew = ShrinkBoundsByFactor(newBounds, 0.75f);

            foreach (var existing in _spawnedSections)
            {
                if (existing == null || existing == section) continue;
                if (existing.transform == section.transform) continue;

                Bounds existingBounds = GetSectionBoundsFromBoxColliders(existing);
                if (existingBounds.size.sqrMagnitude < 0.0001f) continue;

                Bounds shrinkExisting = ShrinkBoundsByFactor(existingBounds, 0.75f);
                if (shrinkNew.Intersects(shrinkExisting))
                {
                    reason = "overlap";
                    return true;
                }
            }
            reason = null;
            return false;
        }

        /// <summary>Shrink bounds by scaling size (e.g. 0.95f = 5% margin per axis).</summary>
        private static Bounds ShrinkBoundsByFactor(Bounds b, float factor)
        {
            var size = b.size * factor;
            if (size.x < 0.01f) size.x = 0.01f;
            if (size.y < 0.01f) size.y = 0.01f;
            if (size.z < 0.01f) size.z = 0.01f;
            return new Bounds(b.center, size);
        }

        /// <summary>Bounds from BoxColliders only, ignoring triggers. Use after positioning. Fallback to Renderer if no non-trigger BoxColliders.</summary>
        private static Bounds GetSectionBoundsFromBoxColliders(MallSection section)
        {
            if (section == null) return new Bounds();
            var boxColliders = section.GetComponentsInChildren<BoxCollider>();
            bool first = true;
            Bounds b = default;
            foreach (var bc in boxColliders)
            {
                if (bc == null || bc.isTrigger) continue;
                if (first) { b = bc.bounds; first = false; }
                else b.Encapsulate(bc.bounds);
            }
            if (!first) return b;
            var renderers = section.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return new Bounds(section.transform.position, Vector3.zero);
            b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                b.Encapsulate(renderers[i].bounds);
            return b;
        }

        private static Bounds GetSectionBounds(MallSection section)
        {
            if (section == null) return new Bounds();
            var bounds = GetSectionBoundsFromBoxColliders(section);
            if (bounds.size.sqrMagnitude > 0.0001f) return bounds;
            var renderers = section.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return bounds;
            bounds = renderers[0].bounds;
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

        /// <summary>Destroy any floor not under our generation root (bootstrap Plane, scene floor) to prevent overlapping layers.</summary>
        private void RemoveOverlappingFloors()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid()) return;

            var toDestroy = new HashSet<GameObject>();
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name == "Floor" && (_generationRoot == null || !root.transform.IsChildOf(_generationRoot)))
                    toDestroy.Add(root);
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                {
                    if (t != root && t.name == "Floor" && (_generationRoot == null || !t.IsChildOf(_generationRoot)))
                        toDestroy.Add(t.gameObject);
                }
            }
            foreach (var go in toDestroy)
            {
                if (go != null)
                    Destroy(go);
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

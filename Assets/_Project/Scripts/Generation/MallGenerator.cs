using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using NightShift.Core;
using NightShift.Systems;

namespace NightShift.Generation
{
    /// <summary>
    /// Mall Generation v2: hub-and-spoke layout on 90-degree grid.
    /// StartHub in center, 4 main corridors (N/E/S/W), 2-4 sections each, optional StoreRooms.
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
        [SerializeField] private int _maxCorridorFailures = 3;
        [SerializeField] private float _boundsInflation = 0.05f;
        [SerializeField] private Material _floorMaterial;

        private const float FloorThickness = 0.2f;

        /// <summary>Layout stats for debug overlay.</summary>
        public int CorridorCount { get; private set; }
        public int BranchCount { get; private set; }

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

            CorridorCount = 0;
            BranchCount = 0;

            GenerateHubAndSpokeLayout(startSection);

            EnsureCctvComponents();
            LogGenerationSummary();

            var dresser = FindFirstObjectByType<MallDresser>();
            if (dresser != null)
                dresser.Dress(_spawnedSections, _generationRoot, _seed);

            FloorOverlapValidator.ValidateIfEnabled(_spawnedSections);

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

        private GameObject GetPrefabByName(string name)
        {
            if (_sectionPrefabs == null) return null;
            foreach (var p in _sectionPrefabs)
            {
                if (p != null && p.name == name) return p;
            }
            return null;
        }

        /// <summary>Hub-and-spoke v2: 4 corridors N/E/S/W, 2-4 sections each, optional StoreRooms.</summary>
        private void GenerateHubAndSpokeLayout(MallSection startSection)
        {
            var hallStraight = GetPrefabByName("HallStraight");
            var hallCorner = GetPrefabByName("HallCorner");
            var storeRoom = GetPrefabByName("StoreRoom");

            if (hallStraight == null || hallCorner == null)
            {
                Debug.LogWarning("[MallGenerator] HallStraight or HallCorner missing. Falling back to random placement.");
                GenerateFallbackRandomLayout(startSection);
                return;
            }

            int sectionIdx = 1;
            int maxCorridorFailures = Mathf.Max(1, _maxCorridorFailures);

            for (int dir = 0; dir < 4; dir++)
            {
                int connIdx = dir;
                var conn = startSection.ConnectorPoints;
                if (connIdx >= conn.Count) continue;

                Transform hubConn = conn[connIdx];
                Vector3 dirVec = hubConn.forward;
                dirVec.y = 0f;
                if (dirVec.sqrMagnitude < 0.01f) dirVec = Vector3.forward;
                else dirVec.Normalize();

                int corridorLen = _rng.Next(2, 5);
                int storeCount = _rng.Next(0, 2);
                int failures = 0;
                MallSection head = startSection;
                Transform headConn = hubConn;
                Vector3 headDir = dirVec;
                int corridorSections = 0;

                for (int i = 0; i < corridorLen && failures < maxCorridorFailures; i++)
                {
                    bool useCorner = (i > 0 && i < corridorLen - 1 && _rng.NextDouble() < 0.2);
                    var prefab = useCorner ? hallCorner : hallStraight;

                    if (TryPlaceSectionGridAligned(prefab, head, headConn, out var newSection, out var outConn, out var outDir))
                    {
                        CollectSpawnPoints(newSection);
                        _spawnedSections.Add(newSection);
                        newSection.name = $"Section_{sectionIdx}";
                        sectionIdx++;
                        corridorSections++;
                        if (i == 0) CorridorCount++;

                        if (useCorner)
                        {
                            BranchCount++;
                            head = newSection;
                            headConn = outConn;
                            headDir = outDir;
                        }
                        else
                        {
                            head = newSection;
                            headConn = outConn;
                            headDir = outDir;
                        }
                        failures = 0;
                    }
                    else
                    {
                        failures++;
                        if (failures >= maxCorridorFailures)
                        {
                            Debug.Log($"[MallGenerator] Corridor {dir} stopped early after {corridorSections} sections (placement failed)");
                            break;
                        }
                    }
                }

                for (int s = 0; s < storeCount && head != null; s++)
                {
                    if (storeRoom == null) break;
                    var openConns = GetOpenConnectors(head);
                    if (openConns.Count == 0) break;
                    Transform storeConn = openConns[_rng.Next(openConns.Count)];
                    if (TryPlaceSectionGridAligned(storeRoom, head, storeConn, out var storeSection, out _, out _))
                    {
                        CollectSpawnPoints(storeSection);
                        _spawnedSections.Add(storeSection);
                        storeSection.name = $"Section_{sectionIdx}";
                        sectionIdx++;
                        BranchCount++;
                    }
                }
            }

            Debug.Log($"[MallGenerator] Hub-and-spoke: {CorridorCount} corridors, {BranchCount} branches, {_spawnedSections.Count} sections");
        }

        /// <summary>Fallback if prefabs missing: simple random placement.</summary>
        private void GenerateFallbackRandomLayout(MallSection startSection)
        {
            var pool = GetNonStartPrefabPool();
            if (pool.Count == 0) return;

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

                var prefab = pool[_rng.Next(pool.Count)];
                if (TryPlaceSectionExhaustive(prefab, parentSection.transform, parentConn,
                        out var newSection, out var newOpenConnectors, out _))
                {
                    CollectSpawnPoints(newSection);
                    _spawnedSections.Add(newSection);
                    newSection.name = $"Section_{placed}";
                    placed++;
                    foreach (var c in newOpenConnectors)
                        openConnectors.Add(c);
                }
            }
        }

        /// <summary>Place section with strict 90° alignment. Returns the connector and direction to extend.</summary>
        private bool TryPlaceSectionGridAligned(GameObject prefab, MallSection parentSection, Transform parentConn,
            out MallSection newSection, out Transform outConnector, out Vector3 outDirection)
        {
            newSection = null;
            outConnector = null;
            outDirection = Vector3.forward;

            var ms = prefab.GetComponent<MallSection>();
            var childConns = ms != null ? ms.ConnectorPoints : new List<Transform>();
            if (childConns.Count == 0) return false;

            Vector3 parentWorldPos = parentConn.position;
            Vector3 parentForward = parentConn.forward;
            parentForward.y = 0f;
            if (parentForward.sqrMagnitude < 0.01f) parentForward = Vector3.forward;
            parentForward.Normalize();

            foreach (int childConnIdx in Enumerable.Range(0, childConns.Count))
            {
                Transform childConn = childConns[childConnIdx];
                Vector3 childFwd = childConn.forward;
                childFwd.y = 0f;
                if (childFwd.sqrMagnitude < 0.01f) childFwd = Vector3.forward;
                childFwd.Normalize();

                Quaternion rot = Quaternion.FromToRotation(childFwd, -parentForward);
                Vector3 euler = rot.eulerAngles;
                float yaw = Mathf.Round(euler.y / 90f) * 90f;
                rot = Quaternion.Euler(0f, yaw, 0f);

                Vector3 childConnOffsetInWorld = rot * childConn.localPosition;
                Vector3 placePos = NormalizePlacementPosition(parentWorldPos - childConnOffsetInWorld);

                var instance = Instantiate(prefab, placePos, rot, _generationRoot);
                newSection = instance.GetComponent<MallSection>() ?? instance.AddComponent<MallSection>();
                newSection.CollectMarkersIfNeeded();

                Transform instanceChildConn = newSection.ConnectorPoints[childConnIdx];
                if (instanceChildConn != null)
                {
                    Vector3 drift = parentWorldPos - instanceChildConn.position;
                    drift.y = 0f;
                    if (drift.sqrMagnitude > 0.0001f)
                        instance.transform.position += drift;
                }

                if (WouldOverlapStrict(newSection))
                {
                    Destroy(instance);
                    newSection = null;
                    continue;
                }

                ForceSectionRootY(newSection);

                var newConns = newSection.ConnectorPoints;
                int otherIdx = childConns.Count == 1 ? -1 : (childConnIdx == 0 ? 1 : 0);
                if (otherIdx >= 0 && otherIdx < newConns.Count)
                {
                    outConnector = newConns[otherIdx];
                    Vector3 fwd = outConnector.forward;
                    fwd.y = 0f;
                    outDirection = fwd.sqrMagnitude > 0.01f ? fwd.normalized : -parentForward;
                }

                return true;
            }

            return false;
        }

        /// <summary>Connectors of section that don't yet have a neighboring section snapped to them.</summary>
        private List<Transform> GetOpenConnectors(MallSection section)
        {
            var open = new List<Transform>();
            const float snapTolerance = 0.5f;
            foreach (var conn in section.ConnectorPoints)
            {
                if (conn == null) continue;
                Vector3 pos = conn.position;
                bool used = false;
                foreach (var s in _spawnedSections)
                {
                    if (s == section) continue;
                    foreach (var oc in s.ConnectorPoints)
                    {
                        if (oc == null) continue;
                        if ((oc.position - pos).sqrMagnitude < snapTolerance * snapTolerance)
                        {
                            used = true;
                            break;
                        }
                    }
                    if (used) break;
                }
                if (!used) open.Add(conn);
            }
            return open;
        }

        /// <summary>Strict overlap: reject if bounds intersect (no shrink).</summary>
        private bool WouldOverlapStrict(MallSection section)
        {
            Bounds newBounds = GetSectionBoundsFromBoxColliders(section);
            if (newBounds.size.sqrMagnitude < 0.0001f) return false;

            foreach (var existing in _spawnedSections)
            {
                if (existing == null || existing == section) continue;
                Bounds existingBounds = GetSectionBoundsFromBoxColliders(existing);
                if (existingBounds.size.sqrMagnitude < 0.0001f) continue;
                if (newBounds.Intersects(existingBounds)) return true;
            }
            return false;
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

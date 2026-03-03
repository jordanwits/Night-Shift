using System.Collections.Generic;
using UnityEngine;
using NightShift.Systems;

namespace NightShift.Generation
{
    /// <summary>
    /// Mall section prefab component. Holds connector points and spawn markers.
    /// Auto-collects child markers on Awake/OnValidate.
    /// </summary>
    public class MallSection : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField] private bool _isStart;

        [Header("Floor (auto-collected if empty)")]
        [SerializeField] private Transform _floorMain;
        [SerializeField] private List<Transform> _floorLips = new List<Transform>();

        [Header("Markers (auto-collected if empty)")]
        [SerializeField] private Transform[] _connectorPoints;
        [SerializeField] private Transform[] _anomalySpawnPoints;
        [SerializeField] private Transform[] _cctvPoints;
        [SerializeField] private Transform[] _propPoints;
        [SerializeField] private Transform[] _landmarkPoints;
        [SerializeField] private Transform[] _signPoints;
        [SerializeField] private Transform[] _arrowSignPoints;
        [SerializeField] private Transform _playerSpawn;

        public bool IsStart => _isStart;
        public Transform FloorMain => _floorMain;
        public IReadOnlyList<Transform> FloorLips => _floorLips ?? new List<Transform>();
        public IReadOnlyList<Transform> ConnectorPoints => _connectorPoints ?? System.Array.Empty<Transform>();
        public IReadOnlyList<Transform> AnomalySpawnPoints => _anomalySpawnPoints ?? System.Array.Empty<Transform>();
        public IReadOnlyList<Transform> CctvPoints => _cctvPoints ?? System.Array.Empty<Transform>();
        public IReadOnlyList<Transform> PropPoints => _propPoints ?? System.Array.Empty<Transform>();
        public IReadOnlyList<Transform> LandmarkPoints => _landmarkPoints ?? System.Array.Empty<Transform>();
        public IReadOnlyList<Transform> SignPoints => _signPoints ?? System.Array.Empty<Transform>();
        public IReadOnlyList<Transform> ArrowSignPoints => _arrowSignPoints ?? System.Array.Empty<Transform>();
        public Transform PlayerSpawn => _playerSpawn;

        /// <summary>Store name for dispatch messaging. Set by MallDresser for StoreRoom sections.</summary>
        public string StoreName { get; set; }

        private void Awake()
        {
            CollectMarkersIfNeeded();
            EnsureWallColliders();
        }

        private void OnValidate()
        {
            CollectMarkersIfNeeded();
            EnsureWallColliders();
        }

        /// <summary>Ensures all child objects named *Wall* have a BoxCollider matching their mesh bounds.</summary>
        private void EnsureWallColliders()
        {
            foreach (Transform child in GetComponentsInChildren<Transform>(true))
            {
                if (child == transform) continue;
                if (!child.name.Contains("Wall")) continue;

                GameObject go = child.gameObject;
                if (go.GetComponent<Collider>() != null) continue;

                var meshFilter = go.GetComponent<MeshFilter>();
                if (meshFilter == null || meshFilter.sharedMesh == null) continue;

                var box = go.AddComponent<BoxCollider>();
                box.isTrigger = false;
                var b = meshFilter.sharedMesh.bounds;
                box.center = b.center;
                box.size = b.size;
            }
        }

        /// <summary>Auto-collect FloorMain (or first FloorCore) and FloorLip_* by name. FloorLips kept for backward compat but unused with FloorCore.</summary>
        private void CollectFloorIfNeeded()
        {
            if (_floorMain == null)
            {
                var floor = transform.Find("Floor");
                if (floor != null)
                {
                    foreach (Transform child in floor)
                    {
                        if (child != null && child.name.StartsWith("FloorCore"))
                        {
                            _floorMain = child;
                            break;
                        }
                    }
                }
                if (_floorMain == null)
                {
                    foreach (Transform child in GetComponentsInChildren<Transform>(true))
                    {
                        if (child != transform && child.name == "FloorMain")
                        {
                            _floorMain = child;
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>Auto-collect markers from children by component type.</summary>
        public void CollectMarkersIfNeeded()
        {
            CollectFloorIfNeeded();
            bool needsConnectors = _connectorPoints == null || _connectorPoints.Length == 0;
            bool needsAnomalies = _anomalySpawnPoints == null || _anomalySpawnPoints.Length == 0;
            bool needsCctv = _cctvPoints == null || _cctvPoints.Length == 0;
            bool needsPlayer = _playerSpawn == null;
            bool needsMarkers = needsConnectors || needsAnomalies || needsCctv || needsPlayer;

            if (needsMarkers)
            {
                var connectors = new List<Transform>();
                var anomalies = new List<Transform>();
                var cctvs = new List<Transform>();
                Transform player = null;

                foreach (Transform child in GetComponentsInChildren<Transform>(true))
                {
                    if (child == transform) continue;
                    if (child.name == "ConnectorPoint")
                        connectors.Add(child);
                    else if (child.name == "AnomalySpawnPoint")
                        anomalies.Add(child);
                    else if (child.name == "CctvPoint" || child.GetComponent<CctvCameraPoint>() != null)
                        cctvs.Add(child);
                    else if (child.name == "PlayerSpawn")
                        player = child;
                }

                if (connectors.Count > 0) _connectorPoints = connectors.ToArray();
                if (anomalies.Count > 0) _anomalySpawnPoints = anomalies.ToArray();
                if (cctvs.Count > 0) _cctvPoints = cctvs.ToArray();
                if (player != null) _playerSpawn = player;
            }

            var propComps = GetComponentsInChildren<PropPoint>(true);
            var landmarkComps = GetComponentsInChildren<LandmarkPoint>(true);
            var signComps = GetComponentsInChildren<SignPoint>(true);
            var arrowComps = GetComponentsInChildren<ArrowSignPoint>(true);

            var props = new Transform[propComps.Length];
            for (int i = 0; i < propComps.Length; i++)
                props[i] = propComps[i].transform;
            _propPoints = props;

            var landmarks = new Transform[landmarkComps.Length];
            for (int i = 0; i < landmarkComps.Length; i++)
                landmarks[i] = landmarkComps[i].transform;
            _landmarkPoints = landmarks;

            var signs = new Transform[signComps.Length];
            for (int i = 0; i < signComps.Length; i++)
                signs[i] = signComps[i].transform;
            _signPoints = signs;

            var arrows = new Transform[arrowComps.Length];
            for (int i = 0; i < arrowComps.Length; i++)
                arrows[i] = arrowComps[i].transform;
            _arrowSignPoints = arrows;

            Debug.Log($"[MallSection] {gameObject.name}: propPts={_propPoints.Length} signPts={_signPoints.Length} arrowPts={_arrowSignPoints.Length} landmarkPts={_landmarkPoints.Length}");
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            foreach (var t in ConnectorPoints)
            {
                if (t != null)
                    Gizmos.DrawWireSphere(t.position, 0.3f);
            }
            Gizmos.color = Color.red;
            foreach (var t in AnomalySpawnPoints)
            {
                if (t != null)
                    Gizmos.DrawWireSphere(t.position, 0.25f);
            }
            Gizmos.color = Color.cyan;
            foreach (var t in CctvPoints)
            {
                if (t != null)
                    Gizmos.DrawWireSphere(t.position, 0.25f);
            }
            if (_playerSpawn != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(_playerSpawn.position, 0.4f);
            }
            Gizmos.color = Color.magenta;
            foreach (var t in PropPoints)
            {
                if (t != null)
                    Gizmos.DrawWireSphere(t.position, 0.2f);
            }
            Gizmos.color = new Color(1f, 0.5f, 0f);
            foreach (var t in LandmarkPoints)
            {
                if (t != null)
                    Gizmos.DrawWireSphere(t.position, 0.25f);
            }
            Gizmos.color = Color.blue;
            foreach (var t in SignPoints)
            {
                if (t != null)
                    Gizmos.DrawWireSphere(t.position, 0.22f);
            }
            Gizmos.color = Color.cyan;
            foreach (var t in ArrowSignPoints)
            {
                if (t != null)
                    Gizmos.DrawWireSphere(t.position, 0.22f);
            }
        }
#endif
    }
}

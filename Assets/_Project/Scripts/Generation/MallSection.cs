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
        }

        private void OnValidate()
        {
            CollectMarkersIfNeeded();
        }

        /// <summary>Auto-collect markers from children by name/component.</summary>
        public void CollectMarkersIfNeeded()
        {
            bool needsCollect = (_connectorPoints == null || _connectorPoints.Length == 0) ||
                (_anomalySpawnPoints == null || _anomalySpawnPoints.Length == 0 && HasChildNamed("AnomalySpawnPoint")) ||
                (_cctvPoints == null || _cctvPoints.Length == 0 && HasChildWithCctvPoint()) ||
                (_playerSpawn == null && HasChildNamed("PlayerSpawn")) ||
                ((_propPoints == null || _propPoints.Length == 0) && HasChildNamed("PropPoint")) ||
                ((_landmarkPoints == null || _landmarkPoints.Length == 0) && HasChildNamed("LandmarkPoint")) ||
                ((_signPoints == null || _signPoints.Length == 0) && HasChildNamed("SignPoint")) ||
                ((_arrowSignPoints == null || _arrowSignPoints.Length == 0) && HasChildNamed("ArrowSignPoint"));

            if (!needsCollect)
                return;

            var connectors = new List<Transform>();
            var anomalies = new List<Transform>();
            var cctvs = new List<Transform>();
            var props = new List<Transform>();
            var landmarks = new List<Transform>();
            var signs = new List<Transform>();
            var arrows = new List<Transform>();
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
                else if (child.name == "PropPoint")
                    props.Add(child);
                else if (child.name == "LandmarkPoint")
                    landmarks.Add(child);
                else if (child.name == "SignPoint")
                    signs.Add(child);
                else if (child.name == "ArrowSignPoint")
                    arrows.Add(child);
            }

            if (connectors.Count > 0) _connectorPoints = connectors.ToArray();
            if (anomalies.Count > 0) _anomalySpawnPoints = anomalies.ToArray();
            if (cctvs.Count > 0) _cctvPoints = cctvs.ToArray();
            if (player != null) _playerSpawn = player;
            if (props.Count > 0) _propPoints = props.ToArray();
            if (landmarks.Count > 0) _landmarkPoints = landmarks.ToArray();
            if (signs.Count > 0) _signPoints = signs.ToArray();
            if (arrows.Count > 0) _arrowSignPoints = arrows.ToArray();
        }

        private bool HasChildNamed(string name)
        {
            foreach (Transform t in GetComponentsInChildren<Transform>(true))
            {
                if (t != transform && t.name == name) return true;
            }
            return false;
        }

        private bool HasChildWithCctvPoint()
        {
            return GetComponentInChildren<CctvCameraPoint>(true) != null;
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

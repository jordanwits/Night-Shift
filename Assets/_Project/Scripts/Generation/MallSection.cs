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
        [SerializeField] private Transform _playerSpawn;

        public bool IsStart => _isStart;
        public IReadOnlyList<Transform> ConnectorPoints => _connectorPoints ?? System.Array.Empty<Transform>();
        public IReadOnlyList<Transform> AnomalySpawnPoints => _anomalySpawnPoints ?? System.Array.Empty<Transform>();
        public IReadOnlyList<Transform> CctvPoints => _cctvPoints ?? System.Array.Empty<Transform>();
        public Transform PlayerSpawn => _playerSpawn;

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
                (_playerSpawn == null && HasChildNamed("PlayerSpawn"));

            if (!needsCollect)
                return;

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
        }
#endif
    }
}

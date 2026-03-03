using UnityEngine;
using NightShift.Core;

namespace NightShift.Systems
{
    /// <summary>
    /// Spawns the Mannequin Stalker severe anomaly once per run when instability
    /// first crosses threshold. Despawns on EndRun.
    /// </summary>
    public class MannequinSpawner : MonoBehaviour, IGameStateListener
    {
        private const string PrefabPath = "Anomalies/MannequinStalker";
        private const float SpawnThreshold = 70f;

        private GameObject _mannequinInstance;
        private bool _hasSpawnedThisRun;
        private bool _running;
        private bool _debugForceSpawned;

        public bool IsMannequinActive => _mannequinInstance != null && _mannequinInstance.activeInHierarchy;

        private void Start()
        {
            GameEvents.OnRunEnded += OnRunEnded;
        }

        private void OnDestroy()
        {
            GameEvents.OnRunEnded -= OnRunEnded;
        }

        private void OnRunEnded(RunEndReason reason)
        {
            DespawnMannequin();
        }

        public void OnGameStateEntered(GameState state)
        {
            if (state == GameState.InRun)
            {
                _running = true;
                _hasSpawnedThisRun = false;
                _debugForceSpawned = false;
            }
        }

        public void OnGameStateExited(GameState state)
        {
            if (state == GameState.InRun)
                _running = false;
        }

        private void Update()
        {
            if (!_running)
                return;

            float instability = InstabilityManager.Instance != null ? InstabilityManager.Instance.Instability : 0f;

            if (!_hasSpawnedThisRun && instability >= SpawnThreshold)
            {
                SpawnMannequin();
                _hasSpawnedThisRun = true;
            }
        }

        private void SpawnMannequin()
        {
            if (_mannequinInstance != null)
                return;

            var prefab = Resources.Load<GameObject>(PrefabPath);
            if (prefab == null)
            {
                Debug.LogWarning("[MannequinSpawner] Prefab not found at Resources/" + PrefabPath + ". Run Night Shift > Create Mannequin Stalker Prefab.");
                var go = CreateFallbackMannequin();
                if (go != null)
                {
                    _mannequinInstance = go;
                    PlaceMannequin();
                }
                return;
            }

            _mannequinInstance = Instantiate(prefab);
            _mannequinInstance.name = "MannequinStalker";
            PlaceMannequin();

            var ai = _mannequinInstance.GetComponent<MannequinStalkerAI>();
            if (ai != null)
                ai.SetThresholds(SpawnThreshold);

            Debug.Log("[MannequinSpawner] Spawned Mannequin Stalker");
        }

        private GameObject CreateFallbackMannequin()
        {
            var go = new GameObject("MannequinStalker");
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.transform.SetParent(go.transform);
            body.transform.localPosition = Vector3.up;
            go.AddComponent<MannequinStalkerAI>();
            return go;
        }

        private void PlaceMannequin()
        {
            var player = GameObject.Find("Player");
            if (player != null)
            {
                var forward = player.transform.forward;
                forward.y = 0f;
                if (forward.sqrMagnitude < 0.01f) forward = Vector3.forward;
                forward.Normalize();
                var pos = player.transform.position - forward * 8f;
                pos.y = 1f;
                _mannequinInstance.transform.position = pos;
            }
            else
            {
                _mannequinInstance.transform.position = new Vector3(5f, 1f, 5f);
            }
        }

        private void DespawnMannequin()
        {
            if (_mannequinInstance != null)
            {
                Destroy(_mannequinInstance);
                _mannequinInstance = null;
            }
            _hasSpawnedThisRun = false;
            _debugForceSpawned = false;
        }

        /// <summary>Debug: toggle mannequin spawn (F12).</summary>
        public void DebugToggleMannequin()
        {
            if (IsMannequinActive)
            {
                DespawnMannequin();
                Debug.Log("[MannequinSpawner] Debug: despawned mannequin");
            }
            else
            {
                _hasSpawnedThisRun = true;
                _debugForceSpawned = true;
                SpawnMannequin();
                Debug.Log("[MannequinSpawner] Debug: spawned mannequin");
            }
        }
    }
}

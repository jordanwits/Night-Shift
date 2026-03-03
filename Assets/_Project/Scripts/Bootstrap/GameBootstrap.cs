using UnityEngine;
using NightShift.Systems;
using NightShift.Debug;
using NightShift.Player;
using NightShift.UI;

namespace NightShift.Core
{
    /// <summary>
    /// Phase 1 bootstrap: creates single "Bootstrap" GameObject with GameStateManager,
    /// GameClock, InstabilityManager, AnomalyManager, DebugOverlay. Ensures a Player exists for interaction.
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureBootstrap()
        {
            if (FindFirstObjectByType<GameBootstrap>() != null)
                return;

            var bootstrap = new GameObject("Bootstrap");
            bootstrap.AddComponent<GameBootstrap>();
        }

        private void Awake()
        {
            EnsureFloor();
            EnsureComponent<GameClock>();
            EnsureComponent<InstabilityManager>();
            EnsureComponent<AnomalyManager>();
            EnsureComponent<MannequinSpawner>();
            EnsureComponent<DispatchManager>();
            EnsureComponent<AlertFeedUI>();
            SevereAnomalyWarningUI.Ensure();
            EnsureCctvSystem();
            EnsureComponent<SecurityTabletUI>();
            EnsureComponent<DebugOverlay>();
            EnsureComponent<ReportUIController>();
            EnsurePlayer();
            EnsureComponent<GameStateManager>();
        }

        private void EnsureFloor()
        {
            if (GameObject.Find("Floor") != null)
                return;

            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor";
            floor.transform.position = Vector3.zero;
            floor.transform.localScale = new Vector3(5f, 1f, 5f);
        }

        private void EnsureCctvSystem()
        {
            if (FindFirstObjectByType<CctvManager>() != null)
                return;

            var cctvRoot = GameObject.Find("CCTV");
            if (cctvRoot == null)
            {
                cctvRoot = new GameObject("CCTV");
            }

            for (int i = 1; i <= 3; i++)
            {
                var childName = $"CAM-0{i}";
                if (cctvRoot.transform.Find(childName) != null)
                    continue;

                var point = new GameObject(childName);
                point.transform.SetParent(cctvRoot.transform);
                point.transform.localPosition = new Vector3((i - 2) * 8f, 4f, 5f);
                var lookAt = new Vector3(0f, 1.5f, 0f) - point.transform.position;
                point.transform.rotation = Quaternion.LookRotation(lookAt.normalized, Vector3.up);

                point.AddComponent<CctvCameraPoint>();
            }

            EnsureComponent<CctvManager>();
        }

        private void EnsureComponent<T>() where T : Component
        {
            if (GetComponent<T>() != null)
                return;
            gameObject.AddComponent<T>();
        }

        private void EnsurePlayer()
        {
            if (FindFirstObjectByType<PlayerInteraction>() != null)
                return;

            var player = new GameObject("Player");
            player.transform.position = new Vector3(0f, 1f, 0f);
            player.AddComponent<CharacterController>();

            var cam = Camera.main;
            if (cam != null)
            {
                cam.transform.SetParent(player.transform);
                cam.transform.localPosition = new Vector3(0, 1.6f, -2f);
                cam.transform.localRotation = Quaternion.identity;
            }

            player.AddComponent<PlayerVitals>();
            player.AddComponent<PlayerController>();
            player.AddComponent<PlayerInteraction>();

            DownedOverlayUI.Ensure(null);
        }
    }
}

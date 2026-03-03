using UnityEngine;
using NightShift.Systems;
using NightShift.Debug;
using NightShift.Player;

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
            EnsureComponent<DebugOverlay>();
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

            player.AddComponent<PlayerController>();
            player.AddComponent<PlayerInteraction>();
        }
    }
}

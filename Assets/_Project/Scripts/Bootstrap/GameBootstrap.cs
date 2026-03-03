using UnityEngine;
using NightShift.Systems;
using NightShift.Generation;
using NightShift.Player;
using NightShift.UI;
using NightShift.Debug;

namespace NightShift.Core
{
    /// <summary>
    /// Ensures all core managers and systems exist. Add to a GameObject in the bootstrap scene.
    /// Lives in Bootstrap folder (no asmdef) so it can reference all assemblies.
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        [Header("Optional: Assign if you have prefabs")]
        [SerializeField] private GameObject _playerPrefab;
        [SerializeField] private Transform _playerSpawnPoint;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureBootstrap()
        {
            if (FindFirstObjectByType<GameBootstrap>() == null)
            {
                var go = new GameObject("GameBootstrap");
                go.AddComponent<GameBootstrap>();
            }
        }

        private void Awake()
        {
            EnsureManager<GameStateManager>("GameStateManager");
            EnsureManager<InstabilityManager>("InstabilityManager");
            EnsureManager<GameTimeManager>("GameTimeManager");
            EnsureManager<BootstrapCoordinator>("BootstrapCoordinator");
            EnsureManager<RunEndHandler>("RunEndHandler");
            EnsureManager<MallGenerator>("MallGenerator");
            EnsureManager<AnomalyManager>("AnomalyManager");
            EnsureManager<DebugOverlay>("DebugOverlay");
            EnsureManager<DebugTools>("DebugTools");

            EnsureEndScreen();
            EnsurePlayer();
        }

        private void EnsureManager<T>(string name) where T : Component
        {
            if (FindFirstObjectByType<T>() == null)
            {
                var go = new GameObject(name);
                go.AddComponent<T>();
                Debug.Log($"[GameBootstrap] Created {name}");
            }
        }

        private void EnsureEndScreen()
        {
            if (FindFirstObjectByType<EndScreenController>() != null) return;

            var canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                var canvasGo = new GameObject("EndScreenCanvas");
                canvas = canvasGo.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasGo.AddComponent<UnityEngine.UI.CanvasScaler>();
                canvasGo.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            }

            var panel = new GameObject("EndScreenPanel");
            panel.transform.SetParent(canvas.transform, false);
            var rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;

            var image = panel.AddComponent<UnityEngine.UI.Image>();
            image.color = new Color(0, 0, 0, 0.85f);

            var title = new GameObject("Title");
            title.transform.SetParent(panel.transform, false);
            var titleText = title.AddComponent<UnityEngine.UI.Text>();
            titleText.text = "GAME OVER";
            titleText.fontSize = 48;
            titleText.alignment = TextAnchor.MiddleCenter;
            var titleRect = title.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 0.7f);
            titleRect.anchorMax = new Vector2(0.5f, 0.7f);
            titleRect.sizeDelta = new Vector2(400, 60);
            titleRect.anchoredPosition = Vector2.zero;

            var summary = new GameObject("Summary");
            summary.transform.SetParent(panel.transform, false);
            var summaryText = summary.AddComponent<UnityEngine.UI.Text>();
            summaryText.text = "";
            summaryText.fontSize = 24;
            summaryText.alignment = TextAnchor.MiddleCenter;
            var summaryRect = summary.GetComponent<RectTransform>();
            summaryRect.anchorMin = new Vector2(0.5f, 0.5f);
            summaryRect.anchorMax = new Vector2(0.5f, 0.5f);
            summaryRect.sizeDelta = new Vector2(400, 200);
            summaryRect.anchoredPosition = Vector2.zero;

            var controller = panel.AddComponent<EndScreenController>();
            controller.SetReferences(panel, titleText, summaryText);
            panel.SetActive(false);
        }

        private void EnsurePlayer()
        {
            if (FindFirstObjectByType<PlayerController>() != null) return;

            GameObject player;
            if (_playerPrefab != null)
            {
                var spawnPos = _playerSpawnPoint != null ? _playerSpawnPoint.position : Vector3.zero;
                player = Instantiate(_playerPrefab, spawnPos, Quaternion.identity);
            }
            else
            {
                player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                player.name = "Player";
                player.transform.position = _playerSpawnPoint != null ? _playerSpawnPoint.position : Vector3.zero;
                player.AddComponent<CharacterController>();
                player.AddComponent<PlayerController>();
                player.AddComponent<PlayerInteraction>();
            }

            var cam = Camera.main;
            if (cam != null)
            {
                cam.transform.SetParent(player.transform);
                cam.transform.localPosition = new Vector3(0, 1.6f, 0);
                cam.transform.localRotation = Quaternion.identity;
            }
        }
    }
}

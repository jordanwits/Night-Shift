using UnityEngine;
using UnityEngine.SceneManagement;
using NightShift.Systems;
using NightShift.Generation;

namespace NightShift.Debug
{
    /// <summary>
    /// Developer tooling: manual spawn, instability slider, quick restart.
    /// </summary>
    public class DebugTools : MonoBehaviour
    {
        [Header("Overlay")]
        [SerializeField] private bool _showTools = true;
        [SerializeField] private KeyCode _toggleKey = KeyCode.F2;

        private bool _showPanel;
        private float _instabilitySliderValue;
        private GUIStyle _boxStyle;

        private void Start()
        {
            if (InstabilityManager.Instance != null)
                _instabilitySliderValue = InstabilityManager.Instance.Instability;
        }

        private void Update()
        {
            if (Input.GetKeyDown(_toggleKey))
                _showTools = !_showTools;

            if (Input.GetKeyDown(KeyCode.F5))
                QuickRestart();
        }

        private void OnGUI()
        {
            if (!_showTools) return;

            float panelWidth = 220;
            float panelHeight = 180;
            float x = Screen.width - panelWidth - 10;
            float y = 10;

            GUI.Box(new Rect(x, y, panelWidth, panelHeight), "Debug Tools");

            y += 30;

            if (GUI.Button(new Rect(x + 10, y, panelWidth - 20, 25), "Spawn Anomaly"))
            {
                AnomalyManager.Instance?.DebugSpawnAnomaly();
            }
            y += 35;

            GUI.Label(new Rect(x + 10, y, 100, 20), "Instability:");
            if (InstabilityManager.Instance != null)
            {
                _instabilitySliderValue = GUI.HorizontalSlider(new Rect(x + 110, y + 2, panelWidth - 130, 20),
                    _instabilitySliderValue, 0, 100);
                InstabilityManager.Instance.DebugSetInstability(_instabilitySliderValue);
            }
            y += 30;

            if (GUI.Button(new Rect(x + 10, y, panelWidth - 20, 25), "Quick Restart (F5)"))
            {
                QuickRestart();
            }
        }

        private void QuickRestart()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}

using UnityEngine;
using UnityEngine.UI;
using NightShift.Core;
using NightShift.Systems;

namespace NightShift.UI
{
    /// <summary>
    /// Shows performance summary when run ends. Listens for EndRun state.
    /// </summary>
    public class EndScreenController : MonoBehaviour, IGameStateListener
    {
        [Header("UI")]
        [SerializeField] private GameObject _panel;
        [SerializeField] private UnityEngine.UI.Text _titleText;
        [SerializeField] private UnityEngine.UI.Text _summaryText;

        public void SetReferences(GameObject panel, UnityEngine.UI.Text titleText, UnityEngine.UI.Text summaryText)
        {
            _panel = panel;
            _titleText = titleText;
            _summaryText = summaryText;
        }

        private void Awake()
        {
            if (_panel != null)
                _panel.SetActive(false);
        }

        public void OnGameStateEntered(GameState state)
        {
            if (state == GameState.EndRun)
                ShowEndScreen();
            else if (_panel != null)
                _panel.SetActive(false);
        }

        public void OnGameStateExited(GameState state) { }

        private void ShowEndScreen()
        {
            var reason = RunEndHandler.LastRunEndReason;
            bool survived = reason == RunEndReason.Survived;
            if (_titleText != null)
                _titleText.text = survived ? "SURVIVED" : "GAME OVER";

            string summary = BuildSummary(survived, reason);
            if (_summaryText != null)
                _summaryText.text = summary;

            if (_panel != null)
                _panel.SetActive(true);
        }

        private string BuildSummary(bool survived, RunEndReason reason)
        {
            float instability = InstabilityManager.Instance != null ? InstabilityManager.Instance.Instability : 0;
            string time = GameTimeManager.Instance != null ? GameTimeManager.Instance.GetFormattedTime() : "6:00";
            string outcome = reason switch
            {
                RunEndReason.Survived => "Reached 6AM",
                RunEndReason.InstabilityMax => "Instability maxed",
                RunEndReason.PlayerDied => "Downed too long",
                _ => "Unknown"
            };
            return $"Time: {time}\nInstability: {instability:F0}%\nOutcome: {outcome}";
        }
    }
}


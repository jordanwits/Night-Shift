using UnityEngine;
using NightShift.Core;

namespace NightShift.Systems
{
    /// <summary>
    /// Manages in-game time. Run runs from start (e.g. midnight) until 6AM.
    /// Raises OnSixAMReached when win condition is met.
    /// </summary>
    public class GameTimeManager : MonoBehaviour, IGameStateListener
    {
        public static GameTimeManager Instance { get; private set; }

        [Header("Time")]
        [SerializeField] private float _startHour = 0f;   // Midnight
        [SerializeField] private float _endHour = 6f;    // 6AM
        [SerializeField] private float _secondsPerGameMinute = 1f; // 1 real second = 1 game minute

        private float _currentHour;
        private bool _running;

        public float CurrentHour => _currentHour;
        public float CurrentMinute => (_currentHour - Mathf.Floor(_currentHour)) * 60f;
        public bool HasReachedSixAM => _currentHour >= _endHour;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void OnGameStateEntered(GameState state)
        {
            if (state == GameState.InRun)
            {
                _currentHour = _startHour;
                _running = true;
            }
            else
            {
                _running = false;
            }
        }

        public void OnGameStateExited(GameState state) { }

        private void Update()
        {
            if (!_running)
                return;

            // Advance time: 1 game minute per _secondsPerGameMinute real seconds
            float gameMinutesPerRealSecond = 1f / Mathf.Max(0.01f, _secondsPerGameMinute);
            float gameHoursPerRealSecond = gameMinutesPerRealSecond / 60f;
            _currentHour += gameHoursPerRealSecond * Time.deltaTime;

            GameEvents.RaiseGameTimeChanged(_currentHour);

            if (_currentHour >= _endHour)
            {
                _running = false;
                GameEvents.RaiseSixAMReached();
                GameEvents.RaiseRunEnded(RunEndReason.Survived);
            }
        }

        /// <summary>
        /// Format as "HH:MM" for display.
        /// </summary>
        public string GetFormattedTime()
        {
            int h = Mathf.FloorToInt(_currentHour) % 24;
            int m = Mathf.FloorToInt(CurrentMinute);
            return $"{h:D2}:{m:D2}";
        }

        public void ResetForNewRun()
        {
            _currentHour = _startHour;
            _running = false;
        }
    }
}

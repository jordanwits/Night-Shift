using UnityEngine;
using NightShift.Core;

namespace NightShift.Player
{
    /// <summary>
    /// Player health and downed state. Supports TakeDamage, Down, Revive.
    /// Downed too long = EndRun (singleplayer).
    /// </summary>
    public class PlayerVitals : MonoBehaviour
    {
        public static PlayerVitals Instance { get; private set; }

        [Header("Health")]
        [SerializeField] private float _health = 100f;
        [SerializeField] private float _maxHealth = 100f;

        [Header("Downed State")]
        [SerializeField] private float _downedTimeToDieSeconds = 30f;

        private bool _isDowned;
        private float _downedElapsed;

        public float Health => _health;
        public float MaxHealth => _maxHealth;
        public bool IsDowned => _isDowned;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
            PlayerStateProxy.IsDowned = _isDowned;
        }

        private void OnEnable()
        {
            GameEvents.OnPlayerDamageRequested += OnDamageRequested;
        }

        private void OnDisable()
        {
            GameEvents.OnPlayerDamageRequested -= OnDamageRequested;
        }

        private void OnDamageRequested(float amount)
        {
            TakeDamage(amount);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
                PlayerStateProxy.IsDowned = false;
            }
        }

        private void Update()
        {
            if (_isDowned)
            {
                _downedElapsed += Time.deltaTime;
                if (_downedElapsed >= _downedTimeToDieSeconds)
                {
                    Die();
                }
            }
        }

        /// <summary>Apply damage. At 0 health, player Downs.</summary>
        public void TakeDamage(float amount)
        {
            if (_isDowned)
                return;

            _health = Mathf.Max(0f, _health - amount);
            if (_health <= 0f)
                Down();
        }

        /// <summary>Put player in downed state (movement disabled/slowed, overlay shown).</summary>
        public void Down()
        {
            if (_isDowned)
                return;

            _isDowned = true;
            _downedElapsed = 0f;
            _health = 0f;
            PlayerStateProxy.IsDowned = true;
        }

        /// <summary>Revive player from downed state.</summary>
        public void Revive()
        {
            if (!_isDowned)
                return;

            _isDowned = false;
            PlayerStateProxy.IsDowned = false;
            _downedElapsed = 0f;
            _health = Mathf.Min(50f, _maxHealth * 0.5f); // Partial health on revive
        }

        /// <summary>Call when downed too long; ends run for singleplayer.</summary>
        private void Die()
        {
            GameEvents.RaiseRunEnded(RunEndReason.PlayerDied);
        }

        /// <summary>Debug: apply damage.</summary>
        public void DebugDamage(float amount) => TakeDamage(amount);

        /// <summary>Debug: revive.</summary>
        public void DebugRevive() => Revive();
    }
}

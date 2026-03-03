using UnityEngine;
using NightShift.Core;

namespace NightShift.Player
{
    /// <summary>
    /// Player health and downed state. Supports TakeDamage, Down, Revive.
    /// Downed too long = EndRun (singleplayer).
    /// </summary>
    public class PlayerVitals : MonoBehaviour, IGameStateListener
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
            Systems.GameStateManager.Instance?.RegisterListener(this);
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
            float reduction = Systems.UpgradeManager.Instance != null
                ? Systems.UpgradeManager.Instance.GetDamageReductionPercent()
                : 0f;
            float mitigated = amount * Mathf.Max(0f, 1f - reduction);
            TakeDamage(mitigated);
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

        public void OnGameStateEntered(GameState state)
        {
            if (state == GameState.InRun)
                ResetForNewRun();
        }

        public void OnGameStateExited(GameState state) { }

        private void ResetForNewRun()
        {
            _health = _maxHealth;
            _isDowned = false;
            _downedElapsed = 0f;
            PlayerStateProxy.IsDowned = false;

            var cc = GetComponent<CharacterController>();
            if (cc != null)
            {
                cc.enabled = false;
                transform.position = new Vector3(0f, 1f, 0f);
                transform.rotation = Quaternion.identity;
                cc.enabled = true;
            }
            else
            {
                transform.position = new Vector3(0f, 1f, 0f);
                transform.rotation = Quaternion.identity;
            }
        }
    }
}

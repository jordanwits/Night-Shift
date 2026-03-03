using UnityEngine;
using NightShift.Core;

namespace NightShift.Systems
{
    /// <summary>
    /// Simple AI for The Mannequin: Dormant -> Stalk -> Chase -> Attack.
    /// Activates when instability >= threshold; deactivates when below threshold - 10.
    /// Uses MoveTowards (no NavMesh).
    /// </summary>
    public class MannequinStalkerAI : MonoBehaviour
    {
        public static MannequinStalkerAI Instance { get; private set; }

        [Header("Thresholds")]
        [SerializeField] private float _activateThreshold = 70f;
        [SerializeField] private float _deactivateHysteresis = 10f;

        [Header("Movement")]
        [SerializeField] private float _stalkSpeed = 3f;
        [SerializeField] private float _chaseSpeed = 4f;
        [SerializeField] private float _attackRange = 2f;
        [SerializeField] private float _chaseRange = 8f;
        [SerializeField] private float _seenFreezeDuration = 0.5f;
        [SerializeField] private float _repositionInterval = 3f;
        [SerializeField] private float _repositionDistance = 4f;

        [Header("Attack")]
        [SerializeField] private float _attackDamage = 15f;
        [SerializeField] private float _attackCooldown = 2f;
        [SerializeField] private float _instabilitySpikeOnHit = 2f;
        [SerializeField] private float _backOffDuration = 1f;
        [SerializeField] private float _backOffDistance = 3f;

        private enum State { Dormant, Stalk, Chase, Attack, BackOff }
        private State _state = State.Dormant;
        private Transform _player;
        private Transform _cameraTransform;
        private float _lastAttackTime = -999f;
        private float _backOffEndTime;
        private float _lastSeenCheck;
        private float _lastRepositionTime = -999f;
        private bool _wasSeenLastFrame;
        private float _freezeEndTime;

        private float DeactivateThreshold => _activateThreshold - _deactivateHysteresis;
        private bool IsActive => _state != State.Dormant;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void Start()
        {
            CachePlayerReference();
        }

        private void CachePlayerReference()
        {
            var playerGo = GameObject.Find("Player");
            if (playerGo != null)
            {
                _player = playerGo.transform;
                var cam = playerGo.GetComponentInChildren<Camera>();
                _cameraTransform = cam != null ? cam.transform : null;
            }
        }

        public void SetThresholds(float activate, float deactivateHysteresis = 10f)
        {
            _activateThreshold = activate;
            _deactivateHysteresis = deactivateHysteresis;
        }

        private void Update()
        {
            float instability = InstabilityManager.Instance != null ? InstabilityManager.Instance.Instability : 0f;

            if (instability < DeactivateThreshold)
            {
                if (IsActive)
                {
                    _state = State.Dormant;
                    Debug.Log("[MannequinStalker] Deactivated: instability dropped below threshold");
                }
                return;
            }

            if (!IsActive && instability >= _activateThreshold)
            {
                _state = State.Stalk;
                Debug.Log("[MannequinStalker] Activated: instability crossed threshold");
            }

            if (_player == null)
            {
                CachePlayerReference();
                if (_player == null)
                    return;
            }

            if (PlayerStateProxy.IsDowned)
            {
                _state = State.Dormant;
                return;
            }

            switch (_state)
            {
                case State.Stalk:
                    UpdateStalk();
                    break;
                case State.Chase:
                    UpdateChase();
                    break;
                case State.Attack:
                    UpdateAttack();
                    break;
                case State.BackOff:
                    UpdateBackOff();
                    break;
            }
        }

        private void UpdateStalk()
        {
            float dist = Vector3.Distance(transform.position, _player.position);
            if (dist <= _attackRange)
            {
                TryAttack();
                return;
            }

            bool seen = dist <= _chaseRange && IsPlayerLookingAtMe();
            if (seen)
            {
                _state = State.Chase;
                return;
            }

            if (Time.time - _lastRepositionTime >= _repositionInterval)
            {
                RepositionBehindPlayer();
                _lastRepositionTime = Time.time;
            }
            else
            {
                MoveToward(_player.position, _stalkSpeed);
            }
        }

        private void UpdateChase()
        {
            float dist = Vector3.Distance(transform.position, _player.position);
            if (dist <= _attackRange)
            {
                TryAttack();
                return;
            }

            bool seen = IsPlayerLookingAtMe();
            if (seen)
            {
                _freezeEndTime = Time.time + _seenFreezeDuration;
                _wasSeenLastFrame = true;
                return;
            }
            if (Time.time < _freezeEndTime)
                return;

            MoveToward(_player.position, _chaseSpeed);
            _wasSeenLastFrame = false;
        }

        private void UpdateAttack()
        {
            if (Time.time - _lastAttackTime >= _attackCooldown)
            {
                _state = State.Stalk;
            }
        }

        private void UpdateBackOff()
        {
            if (Time.time >= _backOffEndTime)
            {
                _state = State.Stalk;
                return;
            }
            Vector3 away = (transform.position - _player.position).normalized;
            if (away.sqrMagnitude < 0.01f)
                away = -transform.forward;
            MoveToward(transform.position + away * _backOffDistance, _stalkSpeed);
        }

        private void TryAttack()
        {
            if (Time.time - _lastAttackTime < _attackCooldown)
                return;

            _lastAttackTime = Time.time;
            GameEvents.RaisePlayerDamageRequested(_attackDamage);
            if (InstabilityManager.Instance != null)
                InstabilityManager.Instance.Add(_instabilitySpikeOnHit);
            _state = State.BackOff;
            _backOffEndTime = Time.time + _backOffDuration;
        }

        private bool IsPlayerLookingAtMe()
        {
            if (_cameraTransform == null)
                return false;

            Vector3 toMe = (transform.position + Vector3.up * 1f) - _cameraTransform.position;
            float angle = Vector3.Angle(_cameraTransform.forward, toMe.normalized);
            if (angle > 45f)
                return false;

            float dist = toMe.magnitude;
            if (dist < 0.5f) return true;
            if (Physics.Raycast(_cameraTransform.position, toMe.normalized, out var hit, dist))
            {
                if (hit.collider != null && (hit.collider.gameObject == gameObject || hit.collider.transform.IsChildOf(transform)))
                    return true;
                return false;
            }
            return true;
        }

        private void RepositionBehindPlayer()
        {
            Vector3 forward = _player.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.01f)
                forward = Vector3.forward;
            forward.Normalize();
            Vector3 behind = _player.position - forward * _repositionDistance;
            behind.y = transform.position.y;
            transform.position = behind;
            transform.LookAt(_player.position + Vector3.up * 1f);
        }

        private void MoveToward(Vector3 target, float speed)
        {
            target.y = transform.position.y;
            Vector3 diff = target - transform.position;
            float sqrDist = diff.sqrMagnitude;
            if (sqrDist < 0.0001f)
                return;
            Vector3 dir = diff.normalized;
            float step = speed * Time.deltaTime;
            transform.position += dir * Mathf.Min(step, Mathf.Sqrt(sqrDist));
            transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
        }
    }
}

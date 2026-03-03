using UnityEngine;
using UnityEngine.InputSystem;
using NightShift.Core;
using NightShift.UI;

namespace NightShift.Player
{
    /// <summary>
    /// Simple first-person movement. Yaw on body, pitch on camera. Uses Input System package.
    /// Blocks look/move when report UI is open. Movement disabled/slowed when downed.
    /// </summary>
    public class PlayerController : MonoBehaviour, IGameStateListener
    {
        [Header("Movement")]
        [SerializeField] private float _moveSpeed = 5f;
        [SerializeField] private float _lookSpeed = 180f;
        [SerializeField] private float _downedMoveMultiplier = 0.05f;

        private CharacterController _controller;
        private Transform _cameraTransform;
        private float _rotationY;
        private float _rotationX;
        private PlayerVitals _vitals;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            if (_controller == null)
                _controller = gameObject.AddComponent<CharacterController>();

            var cam = GetComponentInChildren<Camera>();
            _cameraTransform = cam != null ? cam.transform : null;
            _vitals = GetComponent<PlayerVitals>();
            Systems.GameStateManager.Instance?.RegisterListener(this);
        }

        private void Update()
        {
            if (ReportUIController.IsOpen)
                return;
            if (SecurityTabletUI.Instance != null && SecurityTabletUI.Instance.IsTabletOpen)
                return;
            if (EndScreenController.IsEndScreenVisible)
                return;

            bool downed = _vitals != null && _vitals.IsDowned;
            float moveMult = downed ? _downedMoveMultiplier : 1f;

            float h = 0f;
            float v = 0f;

            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.aKey.isPressed) h -= 1f;
                if (kb.dKey.isPressed) h += 1f;
                if (kb.sKey.isPressed) v -= 1f;
                if (kb.wKey.isPressed) v += 1f;
            }

            var mouse = Mouse.current;
            if (mouse != null && !downed)
            {
                var delta = mouse.delta.ReadValue();
                _rotationY += delta.x * 0.2f;
                _rotationX -= delta.y * 0.2f;
                _rotationX = Mathf.Clamp(_rotationX, -89f, 89f);
            }

            transform.rotation = Quaternion.Euler(0, _rotationY, 0);
            if (_cameraTransform != null)
                _cameraTransform.localRotation = Quaternion.Euler(_rotationX, 0, 0);

            Vector3 move = (transform.forward * v + transform.right * h).normalized * _moveSpeed * moveMult * Time.deltaTime;
            move.y = -9.81f * Time.deltaTime;
            _controller.Move(move);
        }

        public void OnGameStateEntered(GameState state)
        {
            if (state == GameState.InRun)
            {
                _rotationY = 0f;
                _rotationX = 0f;
            }
        }

        public void OnGameStateExited(GameState state) { }
    }
}

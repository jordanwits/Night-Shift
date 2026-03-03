using UnityEngine;

namespace NightShift.Player
{
    /// <summary>
    /// Simple first-person or third-person movement. Transform-based, no complex animation.
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float _moveSpeed = 5f;
        [SerializeField] private float _turnSpeed = 180f;

        private CharacterController _controller;
        private float _rotationY;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            if (_controller == null)
                _controller = gameObject.AddComponent<CharacterController>();
        }

        private void Update()
        {
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            float mouseX = Input.GetAxis("Mouse X");

            _rotationY += mouseX * _turnSpeed * Time.deltaTime;
            transform.rotation = Quaternion.Euler(0, _rotationY, 0);

            Vector3 move = (transform.forward * v + transform.right * h).normalized * _moveSpeed * Time.deltaTime;
            move.y = -9.81f * Time.deltaTime; // Simple gravity
            _controller.Move(move);
        }
    }
}

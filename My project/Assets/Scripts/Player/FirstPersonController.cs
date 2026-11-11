using UnityEngine;

namespace ProjectRac.Player
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerInputHandler))]
    public class FirstPersonController : MonoBehaviour
    {
        [Header("Movement Speeds")]
        [SerializeField] private float walkSpeed = 5.0f;
        [SerializeField] private float sprintSpeed = 9.0f;
        [SerializeField] private float acceleration = 12.0f;

        [Header("Jumping & Gravity")]
        [SerializeField] private float jumpHeight = 1.4f;
        [SerializeField] private float gravity = -19.62f;

        [Header("Ground Check")]
        [SerializeField] private float groundedOffset = -0.1f;
        [SerializeField] private float groundedRadius = 0.28f;
        [SerializeField] private LayerMask groundLayers = ~0;

        private CharacterController _characterController;
        private PlayerInputHandler _inputHandler;
        private Vector3 _velocity;
        private float _currentSpeed;
        private bool _isGrounded;

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            _inputHandler = GetComponent<PlayerInputHandler>();
        }

        private void Update()
        {
            CheckGrounded();
            HandleMovement();
            HandleJumpAndGravity();
        }

        private void CheckGrounded()
        {
            Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y + groundedOffset, transform.position.z);
            _isGrounded = _characterController.isGrounded || Physics.CheckSphere(spherePosition, groundedRadius, groundLayers, QueryTriggerInteraction.Ignore);

            if (_isGrounded && _velocity.y < 0f)
            {
                // Small downward force to stay glued to slopes and ground
                _velocity.y = -2f;
            }
        }

        private void HandleMovement()
        {
            Vector2 input = _inputHandler.MoveInput;
            float targetSpeed = _inputHandler.IsSprinting ? sprintSpeed : walkSpeed;

            if (input == Vector2.zero)
            {
                targetSpeed = 0f;
            }

            _currentSpeed = Mathf.Lerp(_currentSpeed, targetSpeed, Time.deltaTime * acceleration);

            // Move relative to player facing direction
            Vector3 moveDirection = transform.right * input.x + transform.forward * input.y;
            _characterController.Move(moveDirection * (_currentSpeed * Time.deltaTime));
        }

        private void HandleJumpAndGravity()
        {
            if (_isGrounded && _inputHandler.JumpTriggered)
            {
                _velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }

            // Apply gravity over time
            _velocity.y += gravity * Time.deltaTime;
            _characterController.Move(_velocity * Time.deltaTime);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = _isGrounded ? Color.green : Color.red;
            Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y + groundedOffset, transform.position.z);
            Gizmos.DrawWireSphere(spherePosition, groundedRadius);
        }
    }
}

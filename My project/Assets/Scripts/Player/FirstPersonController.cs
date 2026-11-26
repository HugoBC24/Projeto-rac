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
        [SerializeField] private float jumpHeight = 0.85f;
        [SerializeField] private float gravity = -19.62f;
        [SerializeField] private float jumpLockoutDuration = 0.15f;

        [Header("Ground Check")]
        [SerializeField] private float groundedOffset = -0.05f;
        [SerializeField] private float groundedRadius = 0.24f;
        [SerializeField] private LayerMask groundLayers = ~0;

        public bool IsGrounded => _isGrounded;
        public float JumpHeight => jumpHeight;

        private CharacterController _characterController;
        private PlayerInputHandler _inputHandler;
        private PlayerStamina _stamina;
        private readonly Collider[] _groundHits = new Collider[8];
        private Vector3 _velocity;
        private float _currentSpeed;
        private float _lastJumpTime = -999f;
        private bool _isGrounded;

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            _inputHandler = GetComponent<PlayerInputHandler>();
            _stamina = GetComponent<PlayerStamina>();
        }

        private void Update()
        {
            CheckGrounded();
            HandleMovement();
            HandleJumpAndGravity();
        }

        public void SetJumpHeight(float newJumpHeight)
        {
            jumpHeight = Mathf.Max(0.2f, newJumpHeight);
        }

        private void CheckGrounded()
        {
            // Never report grounded immediately after takeoff or while moving upward.
            if (Time.time < _lastJumpTime + jumpLockoutDuration || _velocity.y > 0.1f)
            {
                _isGrounded = false;
                return;
            }

            if (_characterController.isGrounded)
            {
                _isGrounded = true;
            }
            else
            {
                // Use OverlapSphereNonAlloc ignoring the player's own CharacterController collider so
                // ground check never self-collides and allows infinite mid-air jumps.
                Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y + groundedOffset, transform.position.z);
                int hitCount = Physics.OverlapSphereNonAlloc(spherePosition, groundedRadius, _groundHits, groundLayers, QueryTriggerInteraction.Ignore);
                bool hitExternalGround = false;
                for (int i = 0; i < hitCount; i++)
                {
                    Collider col = _groundHits[i];
                    if (col == null || col == _characterController) continue;
                    if (col.transform == transform || col.transform.IsChildOf(transform)) continue;
                    hitExternalGround = true;
                    break;
                }
                _isGrounded = hitExternalGround;
            }

            if (_isGrounded && _velocity.y < 0f)
            {
                // Small downward force to stay glued to slopes and stairs.
                _velocity.y = -2f;
            }
        }

        private void HandleMovement()
        {
            Vector2 input = _inputHandler.MoveInput;
            bool wantsToSprint = _inputHandler.IsSprinting && input != Vector2.zero;
            bool isActuallySprinting = false;

            if (wantsToSprint)
            {
                isActuallySprinting = _stamina != null ? _stamina.TryConsumeSprint(Time.deltaTime) : true;
            }

            float targetSpeed = input == Vector2.zero ? 0f : (isActuallySprinting ? sprintSpeed : walkSpeed);
            _currentSpeed = Mathf.Lerp(_currentSpeed, targetSpeed, Time.deltaTime * acceleration);

            Vector3 moveDirection = transform.right * input.x + transform.forward * input.y;
            _characterController.Move(moveDirection * (_currentSpeed * Time.deltaTime));
        }

        private void HandleJumpAndGravity()
        {
            if (_isGrounded && _inputHandler.JumpTriggered)
            {
                bool canJump = _stamina == null || _stamina.TryConsumeJump();
                if (canJump)
                {
                    _velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                    _lastJumpTime = Time.time;
                    _isGrounded = false;
                }
            }

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

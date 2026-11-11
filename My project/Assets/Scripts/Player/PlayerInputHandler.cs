using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectRac.Player
{
    public class PlayerInputHandler : MonoBehaviour
    {
        [Header("New Input System Asset")]
        [SerializeField] private InputActionAsset inputActionAsset;

        public Vector2 MoveInput { get; private set; }
        public Vector2 LookInput { get; private set; }
        public bool JumpTriggered { get; private set; }
        public bool IsSprinting { get; private set; }
        public bool InteractTriggered { get; private set; }
        public bool PrimaryActionTriggered { get; private set; }

        private InputAction _moveAction;
        private InputAction _lookAction;
        private InputAction _jumpAction;
        private InputAction _sprintAction;
        private InputAction _interactAction;
        private InputAction _attackAction;

        private InputActionMap _runtimeMap;

        private void Awake()
        {
            SetupInputActions();
        }

        private void OnEnable()
        {
            EnableActions();
        }

        private void OnDisable()
        {
            DisableActions();
        }

        private void LateUpdate()
        {
            // Reset one-frame triggers
            JumpTriggered = false;
            InteractTriggered = false;
            PrimaryActionTriggered = false;
        }

        public void SetActionAsset(InputActionAsset asset)
        {
            DisableActions();
            inputActionAsset = asset;
            SetupInputActions();
            EnableActions();
        }

        private void SetupInputActions()
        {
            if (inputActionAsset != null)
            {
                var playerMap = inputActionAsset.FindActionMap("Player");
                if (playerMap != null)
                {
                    _moveAction = playerMap.FindAction("Move");
                    _lookAction = playerMap.FindAction("Look");
                    _jumpAction = playerMap.FindAction("Jump");
                    _sprintAction = playerMap.FindAction("Sprint");
                    _interactAction = playerMap.FindAction("Interact");
                    _attackAction = playerMap.FindAction("Attack");
                    RegisterCallbacks();
                    return;
                }
            }

            // Fallback: Create dedicated programmatic InputActionMap using New Input System
            _runtimeMap = new InputActionMap("RuntimePlayer");

            _moveAction = _runtimeMap.AddAction("Move", binding: "<Gamepad>/leftStick");
            _moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");

            _lookAction = _runtimeMap.AddAction("Look", binding: "<Mouse>/delta");
            _lookAction.AddBinding("<Gamepad>/rightStick");

            _jumpAction = _runtimeMap.AddAction("Jump", binding: "<Keyboard>/space");
            _jumpAction.AddBinding("<Gamepad>/buttonSouth");

            _sprintAction = _runtimeMap.AddAction("Sprint", binding: "<Keyboard>/leftShift");
            _sprintAction.AddBinding("<Gamepad>/leftStickPress");

            _interactAction = _runtimeMap.AddAction("Interact", binding: "<Keyboard>/e");
            _interactAction.AddBinding("<Gamepad>/buttonWest");

            _attackAction = _runtimeMap.AddAction("Attack", binding: "<Mouse>/leftButton");
            _attackAction.AddBinding("<Gamepad>/rightTrigger");

            RegisterCallbacks();
        }

        private void RegisterCallbacks()
        {
            if (_moveAction != null)
            {
                _moveAction.performed += ctx => MoveInput = ctx.ReadValue<Vector2>();
                _moveAction.canceled += ctx => MoveInput = Vector2.zero;
            }

            if (_lookAction != null)
            {
                _lookAction.performed += ctx => LookInput = ctx.ReadValue<Vector2>();
                _lookAction.canceled += ctx => LookInput = Vector2.zero;
            }

            if (_jumpAction != null)
            {
                _jumpAction.performed += ctx => JumpTriggered = true;
            }

            if (_sprintAction != null)
            {
                _sprintAction.performed += ctx => IsSprinting = true;
                _sprintAction.canceled += ctx => IsSprinting = false;
            }

            if (_interactAction != null)
            {
                _interactAction.performed += ctx => InteractTriggered = true;
            }

            if (_attackAction != null)
            {
                _attackAction.performed += ctx => PrimaryActionTriggered = true;
            }
        }

        private void EnableActions()
        {
            if (inputActionAsset != null)
            {
                var playerMap = inputActionAsset.FindActionMap("Player");
                playerMap?.Enable();
            }
            else
            {
                _runtimeMap?.Enable();
            }
        }

        private void DisableActions()
        {
            if (inputActionAsset != null)
            {
                var playerMap = inputActionAsset.FindActionMap("Player");
                playerMap?.Disable();
            }
            else
            {
                _runtimeMap?.Disable();
            }
        }
    }
}

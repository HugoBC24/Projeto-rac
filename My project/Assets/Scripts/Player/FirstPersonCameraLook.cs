using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectRac.Player
{
    public class FirstPersonCameraLook : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform playerBody;
        [SerializeField] private PlayerInputHandler inputHandler;

        [Header("Sensitivity Settings")]
        [SerializeField] private float mouseSensitivity = 0.15f;
        [SerializeField] private float minPitch = -85f;
        [SerializeField] private float maxPitch = 85f;
        [SerializeField] private bool invertY = false;

        private float _xRotation = 0f;

        private void Start()
        {
            if (playerBody == null && transform.parent != null)
            {
                playerBody = transform.parent;
            }

            if (inputHandler == null)
            {
                inputHandler = GetComponentInParent<PlayerInputHandler>();
            }

            LockCursor();
        }

        private void Update()
        {
            HandleCursorToggle();
            HandleLook();
        }

        private void HandleLook()
        {
            if (Cursor.lockState != CursorLockMode.Locked || inputHandler == null)
            {
                return;
            }

            Vector2 lookInput = inputHandler.LookInput;
            float mouseX = lookInput.x * mouseSensitivity;
            float mouseY = lookInput.y * mouseSensitivity * (invertY ? 1f : -1f);

            _xRotation += mouseY;
            _xRotation = Mathf.Clamp(_xRotation, minPitch, maxPitch);

            // Rotate camera vertically (Pitch)
            transform.localRotation = Quaternion.Euler(_xRotation, 0f, 0f);

            // Rotate player body horizontally (Yaw)
            if (playerBody != null)
            {
                playerBody.Rotate(Vector3.up * mouseX);
            }
        }

        private void HandleCursorToggle()
        {
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;

            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                UnlockCursor();
            }
            else if (mouse != null && mouse.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked)
            {
                LockCursor();
            }
        }

        public void LockCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        public void UnlockCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}

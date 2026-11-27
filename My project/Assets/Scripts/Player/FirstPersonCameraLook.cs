using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectRac.Player
{
    public class FirstPersonCameraLook : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform playerBody;
        [SerializeField] private PlayerInputHandler inputHandler;

        [Header("Sensitivity & PS2 Weight")]
        [SerializeField] private float mouseSensitivity = 0.15f;
        [Tooltip("Higher = snappier, 0 = raw instantaneous mouse, ~16 = weighty PS2 analog feel.")]
        [SerializeField] private float lookSmoothing = 16f;
        [SerializeField] private float minPitch = -85f;
        [SerializeField] private float maxPitch = 85f;
        [SerializeField] private bool invertY = false;

        private PS2CameraFeel _cameraFeel;
        private float _xRotation = 0f;
        private Vector2 _smoothedLook;

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

            _cameraFeel = GetComponent<PS2CameraFeel>();
            LockCursor();
        }

        private void Update()
        {
            HandleCursorToggle();
            HandleLook();
            ApplyCameraRotation();
        }

        private void HandleLook()
        {
            if (Cursor.lockState != CursorLockMode.Locked || inputHandler == null)
            {
                _smoothedLook = Vector2.zero;
                return;
            }

            Vector2 rawInput = inputHandler.LookInput;
            if (lookSmoothing > 0.01f)
            {
                float t = 1f - Mathf.Exp(-lookSmoothing * Time.deltaTime);
                _smoothedLook = Vector2.Lerp(_smoothedLook, rawInput, t);
            }
            else
            {
                _smoothedLook = rawInput;
            }

            float mouseX = _smoothedLook.x * mouseSensitivity;
            float mouseY = _smoothedLook.y * mouseSensitivity * (invertY ? 1f : -1f);

            _xRotation = Mathf.Clamp(_xRotation + mouseY, minPitch, maxPitch);

            if (playerBody != null)
            {
                playerBody.Rotate(Vector3.up * mouseX);
            }
        }

        private void ApplyCameraRotation()
        {
            float roll = _cameraFeel != null ? _cameraFeel.CurrentRollAngle : 0f;
            transform.localRotation = Quaternion.Euler(_xRotation, 0f, roll);
        }

        public void AddRecoil(float pitchOffset)
        {
            _xRotation = Mathf.Clamp(_xRotation - pitchOffset, minPitch, maxPitch);
            ApplyCameraRotation();
        }

        public void SetPitch(float pitchDegrees)
        {
            _xRotation = Mathf.Clamp(pitchDegrees, minPitch, maxPitch);
            ApplyCameraRotation();
        }

        public void SetLookSmoothing(float smoothing)
        {
            lookSmoothing = Mathf.Max(0f, smoothing);
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

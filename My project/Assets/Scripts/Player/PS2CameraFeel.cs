using UnityEngine;
using ProjectRac.UI;

namespace ProjectRac.Player
{
    [RequireComponent(typeof(Camera))]
    public class PS2CameraFeel : MonoBehaviour
    {
        [Header("PS2 Retro Framing")]
        [SerializeField] private float retroFov = 55f;
        [SerializeField] private bool applyRetroFovOnAwake = true;

        [Header("Grounded Head-Bob")]
        [SerializeField] private float walkBobFrequency = 7.8f;
        [SerializeField] private float sprintBobFrequency = 11.2f;
        [SerializeField] private float walkBobAmplitudeY = 0.048f;
        [SerializeField] private float sprintBobAmplitudeY = 0.088f;
        [SerializeField] private float bobAmplitudeX = 0.024f;
        [SerializeField] private float bobBlendSpeed = 10f;

        [Header("Idle Breathing Sway")]
        [SerializeField] private float idleSwayFrequency = 1.4f;
        [SerializeField] private float idleSwayAmplitude = 0.008f;

        [Header("Strafe Tilt (Roll)")]
        [SerializeField] private float maxStrafeRoll = 1.5f;
        [SerializeField] private float rollSmoothSpeed = 9f;

        public float CurrentRollAngle { get; private set; }

        private Camera _camera;
        private CharacterController _characterController;
        private PlayerInputHandler _inputHandler;
        private Vector3 _baseLocalPosition;
        private float _bobPhase;
        private Vector3 _currentOffset;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            _characterController = GetComponentInParent<CharacterController>();
            _inputHandler = GetComponentInParent<PlayerInputHandler>();
            _baseLocalPosition = transform.localPosition;

            if (applyRetroFovOnAwake && _camera != null)
            {
                _camera.fieldOfView = retroFov;
            }
        }

        private void Update()
        {
            if (NoteInspectionUI.IsInspecting)
            {
                SmoothToNeutral();
                return;
            }

            UpdateHeadBob();
            UpdateStrafeRoll();
        }

        public void SetRetroFov(float fov)
        {
            retroFov = Mathf.Clamp(fov, 35f, 90f);
            if (_camera == null) _camera = GetComponent<Camera>();
            if (_camera != null) _camera.fieldOfView = retroFov;
        }

        private void UpdateHeadBob()
        {
            Vector3 horizontalVel = Vector3.zero;
            bool isGrounded = true;

            if (_characterController != null)
            {
                horizontalVel = new Vector3(_characterController.velocity.x, 0f, _characterController.velocity.z);
                isGrounded = _characterController.isGrounded;
            }

            float speed = horizontalVel.magnitude;
            bool isMoving = isGrounded && speed > 0.25f;
            Vector3 targetOffset;

            if (isMoving)
            {
                bool sprinting = _inputHandler != null && _inputHandler.IsSprinting && speed > 5.5f;
                float freq = sprinting ? sprintBobFrequency : walkBobFrequency;
                float ampY = sprinting ? sprintBobAmplitudeY : walkBobAmplitudeY;
                float ampX = sprinting ? bobAmplitudeX * 1.45f : bobAmplitudeX;

                _bobPhase += Time.deltaTime * freq;
                float offsetY = Mathf.Sin(_bobPhase * 2f) * ampY;
                float offsetX = Mathf.Cos(_bobPhase) * ampX;
                targetOffset = new Vector3(offsetX, offsetY, 0f);
            }
            else
            {
                float breath = Mathf.Sin(Time.time * idleSwayFrequency) * idleSwayAmplitude;
                targetOffset = new Vector3(0f, breath, 0f);
            }

            float blend = 1f - Mathf.Exp(-bobBlendSpeed * Time.deltaTime);
            _currentOffset = Vector3.Lerp(_currentOffset, targetOffset, blend);
            transform.localPosition = _baseLocalPosition + _currentOffset;
        }

        private void UpdateStrafeRoll()
        {
            float strafeInput = _inputHandler != null ? _inputHandler.MoveInput.x : 0f;
            float targetRoll = -strafeInput * maxStrafeRoll;
            float blend = 1f - Mathf.Exp(-rollSmoothSpeed * Time.deltaTime);
            CurrentRollAngle = Mathf.Lerp(CurrentRollAngle, targetRoll, blend);
        }

        private void SmoothToNeutral()
        {
            float blend = 1f - Mathf.Exp(-bobBlendSpeed * Time.deltaTime);
            _currentOffset = Vector3.Lerp(_currentOffset, Vector3.zero, blend);
            transform.localPosition = _baseLocalPosition + _currentOffset;
            CurrentRollAngle = Mathf.Lerp(CurrentRollAngle, 0f, blend);
        }
    }
}

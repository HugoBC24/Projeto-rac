using UnityEngine;

namespace ProjectRac.Interaction
{
    public class CabinetController : MonoBehaviour, IInteractable
    {
        [Header("Door Pivots (Single or Double)")]
        [SerializeField] private Transform leftDoorPivot;
        [SerializeField] private Transform rightDoorPivot;
        [SerializeField] private float leftOpenAngle = -105f;
        [SerializeField] private float rightOpenAngle = 105f;
        [SerializeField] private float swingSpeed = 6.5f;
        [SerializeField] private bool startOpen = false;

        [Header("Interaction Prompts")]
        [SerializeField] private string openPrompt = "[E] Abrir Armário";
        [SerializeField] private string closePrompt = "[E] Fechar Armário";

        [Header("Audio Feedback")]
        [SerializeField] private AudioClip openSound;
        [SerializeField] private AudioClip closeSound;

        public bool IsOpen { get; private set; }

        private Quaternion _leftClosedRot;
        private Quaternion _rightClosedRot;
        private Quaternion _leftTargetRot;
        private Quaternion _rightTargetRot;
        private bool _isMoving;
        private AudioSource _audioSource;

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
                _audioSource.spatialBlend = 1f;
                _audioSource.playOnAwake = false;
            }

            if (leftDoorPivot != null) _leftClosedRot = leftDoorPivot.localRotation;
            if (rightDoorPivot != null) _rightClosedRot = rightDoorPivot.localRotation;

            IsOpen = startOpen;
            ApplyTargetRotations(immediate: true);
        }

        private void Update()
        {
            if (!_isMoving) return;

            float t = 1f - Mathf.Exp(-swingSpeed * Time.deltaTime);
            bool leftDone = UpdatePivot(leftDoorPivot, _leftTargetRot, t);
            bool rightDone = UpdatePivot(rightDoorPivot, _rightTargetRot, t);

            if (leftDone && rightDone)
            {
                _isMoving = false;
            }
        }

        public void Configure(Transform leftPivot, Transform rightPivot = null, float leftAngle = -105f, float rightAngle = 105f)
        {
            leftDoorPivot = leftPivot;
            rightDoorPivot = rightPivot;
            leftOpenAngle = leftAngle;
            rightOpenAngle = rightAngle;
        }

        public string GetInteractionPrompt() => IsOpen ? closePrompt : openPrompt;

        public void Interact() => Toggle();

        public void Toggle()
        {
            if (IsOpen) Close();
            else Open();
        }

        public void Open()
        {
            if (IsOpen) return;
            IsOpen = true;
            ApplyTargetRotations(immediate: false);
            PlayClip(openSound);
        }

        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            ApplyTargetRotations(immediate: false);
            PlayClip(closeSound);
        }

        private void ApplyTargetRotations(bool immediate)
        {
            _leftTargetRot = IsOpen ? _leftClosedRot * Quaternion.Euler(0f, leftOpenAngle, 0f) : _leftClosedRot;
            _rightTargetRot = IsOpen ? _rightClosedRot * Quaternion.Euler(0f, rightOpenAngle, 0f) : _rightClosedRot;

            if (immediate)
            {
                if (leftDoorPivot != null) leftDoorPivot.localRotation = _leftTargetRot;
                if (rightDoorPivot != null) rightDoorPivot.localRotation = _rightTargetRot;
                _isMoving = false;
            }
            else
            {
                _isMoving = true;
            }
        }

        private static bool UpdatePivot(Transform pivot, Quaternion target, float t)
        {
            if (pivot == null) return true;
            pivot.localRotation = Quaternion.Slerp(pivot.localRotation, target, t);
            if (Quaternion.Angle(pivot.localRotation, target) <= 0.1f)
            {
                pivot.localRotation = target;
                return true;
            }
            return false;
        }

        private void PlayClip(AudioClip clip)
        {
            if (clip != null && _audioSource != null) _audioSource.PlayOneShot(clip, 0.8f);
        }
    }
}

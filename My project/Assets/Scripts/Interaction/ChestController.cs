using UnityEngine;

namespace ProjectRac.Interaction
{
    [RequireComponent(typeof(Collider))]
    public class ChestController : MonoBehaviour, IInteractable
    {
        [Header("Lid Hinge Settings")]
        [SerializeField] private Transform lidPivot;
        [SerializeField] private float openAngleX = -105f;
        [SerializeField] private float openSpeed = 7f;
        [SerializeField] private Collider containedPickupCollider;

        [Header("Prompts & Audio")]
        [SerializeField] private string openPrompt = "[E] Abrir Bau";
        [SerializeField] private string closePrompt = "[E] Fechar Bau";
        [SerializeField] private AudioClip openSound;
        [SerializeField] private AudioClip closeSound;

        public bool IsOpen { get; private set; }

        private Quaternion _closedRot;
        private Quaternion _openRot;
        private Quaternion _targetRot;
        private bool _isAnimating;
        private AudioSource _audioSource;

        private void Awake()
        {
            if (lidPivot == null) lidPivot = transform;

            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
                _audioSource.spatialBlend = 1f;
                _audioSource.playOnAwake = false;
            }

            _closedRot = lidPivot.localRotation;
            _openRot = _closedRot * Quaternion.Euler(openAngleX, 0f, 0f);
            _targetRot = _closedRot;
            IsOpen = false;
            SetContainedPickupInteractable(false);
        }

        private void Update()
        {
            if (!_isAnimating || lidPivot == null) return;

            lidPivot.localRotation = Quaternion.Slerp(lidPivot.localRotation, _targetRot, 1f - Mathf.Exp(-openSpeed * Time.deltaTime));
            if (Quaternion.Angle(lidPivot.localRotation, _targetRot) <= 0.35f)
            {
                lidPivot.localRotation = _targetRot;
                _isAnimating = false;
            }
        }

        public void Configure(Transform pivot, Collider innerPickupCollider)
        {
            lidPivot = pivot;
            containedPickupCollider = innerPickupCollider;
        }

        public string GetInteractionPrompt() => IsOpen ? closePrompt : openPrompt;

        public void Interact()
        {
            if (IsOpen) Close();
            else Open();
        }

        public void Open()
        {
            if (IsOpen) return;
            IsOpen = true;
            _targetRot = _openRot;
            _isAnimating = true;
            SetContainedPickupInteractable(true);
            if (openSound != null && _audioSource != null) _audioSource.PlayOneShot(openSound, 0.85f);
        }

        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            _targetRot = _closedRot;
            _isAnimating = true;
            SetContainedPickupInteractable(false);
            if (closeSound != null && _audioSource != null) _audioSource.PlayOneShot(closeSound, 0.85f);
        }

        private void SetContainedPickupInteractable(bool enabledState)
        {
            if (containedPickupCollider != null) containedPickupCollider.enabled = enabledState;
        }
    }
}

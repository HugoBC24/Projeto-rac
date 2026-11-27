using UnityEngine;

namespace ProjectRac.Interaction
{
    [RequireComponent(typeof(Collider))]
    public class DrawerController : MonoBehaviour, IInteractable
    {
        [Header("Slide Settings")]
        [SerializeField] private Transform drawerTransform;
        [SerializeField] private Vector3 localSlideAxis = Vector3.forward;
        [SerializeField] private float slideDistance = 0.45f;
        [SerializeField] private float slideSpeed = 8f;
        [SerializeField] private bool startOpen = false;

        [Header("Interaction Prompts")]
        [SerializeField] private string openPrompt = "[E] Abrir Gaveta";
        [SerializeField] private string closePrompt = "[E] Fechar Gaveta";

        [Header("Audio Feedback")]
        [SerializeField] private AudioClip openSound;
        [SerializeField] private AudioClip closeSound;

        [Header("Contained Item (Optional)")]
        [SerializeField] private MonoBehaviour containedInteractable;

        public bool IsOpen { get; private set; }

        private Vector3 _closedLocalPos;
        private Vector3 _targetLocalPos;
        private bool _isMoving;
        private AudioSource _audioSource;

        private void Awake()
        {
            if (drawerTransform == null) drawerTransform = transform;

            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
                _audioSource.spatialBlend = 1f;
                _audioSource.playOnAwake = false;
            }

            _closedLocalPos = drawerTransform.localPosition;
            IsOpen = startOpen;
            _targetLocalPos = IsOpen ? ComputeOpenLocalPosition() : _closedLocalPos;
            drawerTransform.localPosition = _targetLocalPos;
        }

        private void Update()
        {
            if (!_isMoving || drawerTransform == null) return;

            drawerTransform.localPosition = Vector3.Lerp(
                drawerTransform.localPosition,
                _targetLocalPos,
                1f - Mathf.Exp(-slideSpeed * Time.deltaTime));

            if (Vector3.Distance(drawerTransform.localPosition, _targetLocalPos) <= 0.002f)
            {
                drawerTransform.localPosition = _targetLocalPos;
                _isMoving = false;
            }
        }

        public void Configure(Transform target, Vector3 slideAxis, float distance)
        {
            drawerTransform = target;
            localSlideAxis = slideAxis.sqrMagnitude > 0.0001f ? slideAxis.normalized : Vector3.forward;
            slideDistance = distance;
        }

        public void SetContainedInteractable(MonoBehaviour item)
        {
            containedInteractable = item;
        }

        public string GetInteractionPrompt()
        {
            if (IsOpen && containedInteractable != null && containedInteractable is IInteractable item)
            {
                return item.GetInteractionPrompt();
            }

            return IsOpen ? closePrompt : openPrompt;
        }

        public void Interact()
        {
            if (IsOpen && containedInteractable != null && containedInteractable is IInteractable item)
            {
                item.Interact();
                return;
            }

            Toggle();
        }

        public void Toggle()
        {
            if (IsOpen) Close();
            else Open();
        }

        public void Open()
        {
            if (IsOpen || drawerTransform == null) return;
            IsOpen = true;
            _targetLocalPos = ComputeOpenLocalPosition();
            _isMoving = true;
            PlayClip(openSound);
        }

        public void Close()
        {
            if (!IsOpen || drawerTransform == null) return;
            IsOpen = false;
            _targetLocalPos = _closedLocalPos;
            _isMoving = true;
            PlayClip(closeSound);
        }

        private Vector3 ComputeOpenLocalPosition()
        {
            Vector3 axis = localSlideAxis.sqrMagnitude > 0.0001f ? localSlideAxis.normalized : Vector3.forward;
            return _closedLocalPos + axis * slideDistance;
        }

        private void PlayClip(AudioClip clip)
        {
            if (clip != null && _audioSource != null) _audioSource.PlayOneShot(clip, 0.8f);
        }
    }
}

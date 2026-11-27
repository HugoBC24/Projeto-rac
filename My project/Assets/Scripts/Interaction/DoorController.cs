using UnityEngine;
using UnityEngine.AI;

namespace ProjectRac.Interaction
{
    [RequireComponent(typeof(Collider))]
    public class DoorController : MonoBehaviour, IInteractable
    {
        [Header("Door Pivot & Motion")]
        [SerializeField] private Transform doorPivot;
        [SerializeField] private float openAngle = 90f;
        [SerializeField] private float swingSpeed = 6f;
        [SerializeField] private bool startOpen = false;
        [SerializeField] private bool openAwayFromPlayer = true;

        [Header("Interaction Prompts")]
        [SerializeField] private string openPrompt = "[E] Abrir Porta";
        [SerializeField] private string closePrompt = "[E] Fechar Porta";

        [Header("Audio Feedback")]
        [SerializeField] private AudioClip openSound;
        [SerializeField] private AudioClip closeSound;

        [Header("Navigation")]
        [SerializeField] private NavMeshObstacle navMeshObstacle;
        [SerializeField] private bool allowEnemyOpen = true;

        public bool IsOpen { get; private set; }
        public bool AllowEnemyOpen => allowEnemyOpen;

        private Quaternion _closedRotation;
        private Quaternion _targetRotation;
        private bool _isMoving;
        private AudioSource _audioSource;

        private void Awake()
        {
            if (doorPivot == null)
            {
                doorPivot = transform.parent != null ? transform.parent : transform;
            }

            if (navMeshObstacle == null)
            {
                navMeshObstacle = GetComponent<NavMeshObstacle>();
            }

            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
                _audioSource.spatialBlend = 1f;
                _audioSource.playOnAwake = false;
            }

            _closedRotation = doorPivot.localRotation;
            IsOpen = startOpen;
            _targetRotation = IsOpen
                ? _closedRotation * Quaternion.Euler(0f, openAngle, 0f)
                : _closedRotation;

            doorPivot.localRotation = _targetRotation;
            SyncNavMeshObstacle();
        }

        private void Update()
        {
            if (!_isMoving || doorPivot == null) return;

            doorPivot.localRotation = Quaternion.Slerp(
                doorPivot.localRotation,
                _targetRotation,
                1f - Mathf.Exp(-swingSpeed * Time.deltaTime));

            if (Quaternion.Angle(doorPivot.localRotation, _targetRotation) <= 0.1f)
            {
                doorPivot.localRotation = _targetRotation;
                _isMoving = false;
                SyncNavMeshObstacle();
            }
        }

        public void Configure(Transform pivot, NavMeshObstacle obstacle = null)
        {
            doorPivot = pivot;
            navMeshObstacle = obstacle;
        }

        public void SetAllowEnemyOpen(bool allow)
        {
            allowEnemyOpen = allow;
            SyncNavMeshObstacle();
        }

        public string GetInteractionPrompt()
        {
            return IsOpen ? closePrompt : openPrompt;
        }

        public void Interact()
        {
            Toggle(ResolveInteractorTransform());
        }

        public void Toggle(Transform interactor = null)
        {
            if (IsOpen)
            {
                Close();
            }
            else
            {
                Open(interactor);
            }
        }

        public void Open(Transform interactor = null)
        {
            if (IsOpen || doorPivot == null) return;

            float directionSign = ResolveSwingDirection(interactor);
            IsOpen = true;
            _targetRotation = _closedRotation * Quaternion.Euler(0f, openAngle * directionSign, 0f);
            _isMoving = true;

            SyncNavMeshObstacle();
            PlayClip(openSound);
        }

        public void Close()
        {
            if (!IsOpen || doorPivot == null) return;

            IsOpen = false;
            _targetRotation = _closedRotation;
            _isMoving = true;

            SyncNavMeshObstacle();
            PlayClip(closeSound);
        }

        private float ResolveSwingDirection(Transform interactor)
        {
            if (!openAwayFromPlayer || interactor == null || doorPivot == null)
            {
                return 1f;
            }

            // Use the doorway reference frame (pivot parent when present) so the sign stays
            // consistent regardless of the door leaf's current swing angle.
            Transform frame = doorPivot.parent != null ? doorPivot.parent : doorPivot;
            Vector3 toInteractor = interactor.position - doorPivot.position;
            float side = Vector3.Dot(frame.forward, toInteractor);

            // With the pivot on the left jamb (-X) and leaf extending +X, positive Y rotation
            // swings the leaf toward -Z, and negative Y rotation swings it toward +Z.
            return side >= 0f ? 1f : -1f;
        }

        private Transform ResolveInteractorTransform()
        {
            Camera mainCam = Camera.main;
            return mainCam != null ? mainCam.transform : null;
        }

        private void SyncNavMeshObstacle()
        {
            if (navMeshObstacle == null) return;

            // Keep NavMesh continuous through interior doorways that enemies are allowed to open.
            bool shouldBlock = !IsOpen && !allowEnemyOpen;
            navMeshObstacle.carving = shouldBlock;
            navMeshObstacle.enabled = shouldBlock;
        }

        private void PlayClip(AudioClip clip)
        {
            if (clip != null && _audioSource != null)
            {
                _audioSource.PlayOneShot(clip, 0.85f);
            }
        }
    }
}

using UnityEngine;

namespace ProjectRac.Interaction
{
    public class CabinetController : MonoBehaviour, IInteractable
    {
        public enum CabinetSide { Left, Right }

        [Header("Door Pivots (Single or Double)")]
        [SerializeField] private Transform leftDoorPivot;
        [SerializeField] private Transform rightDoorPivot;
        [SerializeField] private float leftOpenAngle = -105f;
        [SerializeField] private float rightOpenAngle = 105f;
        [SerializeField] private float swingSpeed = 6.5f;
        [SerializeField] private bool startOpen = false;

        [Header("Interaction Prompts")]
        [SerializeField] private string openRightPrompt = "[E] Abrir Porta Direita";
        [SerializeField] private string closeRightPrompt = "[E] Fechar Porta Direita";
        [SerializeField] private string openLeftPrompt = "[E] Abrir Porta Esquerda";
        [SerializeField] private string closeLeftPrompt = "[E] Fechar Porta Esquerda";
        [SerializeField] private string singleOpenPrompt = "[E] Abrir Armário";
        [SerializeField] private string singleClosePrompt = "[E] Fechar Armário";

        [Header("Audio Feedback")]
        [SerializeField] private AudioClip openSound;
        [SerializeField] private AudioClip closeSound;

        public bool IsOpen => IsViewerLeftOpen || IsViewerRightOpen;
        public bool IsViewerLeftOpen { get; private set; }
        public bool IsViewerRightOpen { get; private set; }

        // Mapped by viewer perspective (facing cabinet front +Z): localX < 0 is viewer's Right, localX > 0 is viewer's Left.
        private Transform _viewerRightPivot;
        private Transform _viewerLeftPivot;
        private float _viewerRightOpenAngle;
        private float _viewerLeftOpenAngle;
        private Quaternion _viewerRightClosedRot;
        private Quaternion _viewerLeftClosedRot;
        private Quaternion _viewerRightTargetRot;
        private Quaternion _viewerLeftTargetRot;
        private bool _isMoving;
        private AudioSource _audioSource;

        private bool HasDoubleDoors => _viewerLeftPivot != null && _viewerRightPivot != null;

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
                _audioSource.spatialBlend = 1f;
                _audioSource.playOnAwake = false;
            }

            MapPivotsToViewerSides();

            IsViewerLeftOpen = startOpen && _viewerLeftPivot != null;
            IsViewerRightOpen = startOpen && _viewerRightPivot != null;
            ApplyTargetRotations(immediate: true);
        }

        private void Update()
        {
            if (!_isMoving) return;

            float t = 1f - Mathf.Exp(-swingSpeed * Time.deltaTime);
            bool rightDone = UpdatePivot(_viewerRightPivot, _viewerRightTargetRot, t);
            bool leftDone = UpdatePivot(_viewerLeftPivot, _viewerLeftTargetRot, t);

            if (rightDone && leftDone) _isMoving = false;
        }

        public void Configure(Transform leftPivot, Transform rightPivot = null, float leftAngle = -105f, float rightAngle = 105f)
        {
            leftDoorPivot = leftPivot;
            rightDoorPivot = rightPivot;
            leftOpenAngle = leftAngle;
            rightOpenAngle = rightAngle;
            MapPivotsToViewerSides();
        }

        public string GetInteractionPrompt()
        {
            if (!HasDoubleDoors)
            {
                return IsOpen ? singleClosePrompt : singleOpenPrompt;
            }

            CabinetSide side = ResolveCameraSide(Camera.main);
            if (side == CabinetSide.Right)
            {
                return IsViewerRightOpen ? closeRightPrompt : openRightPrompt;
            }

            return IsViewerLeftOpen ? closeLeftPrompt : openLeftPrompt;
        }

        public void Interact()
        {
            if (!HasDoubleDoors)
            {
                if (IsOpen) CloseAll();
                else OpenAll();
                return;
            }

            ToggleSide(ResolveCameraSide(Camera.main));
        }

        public CabinetSide ResolveCameraSide(Camera cam)
        {
            if (cam == null) return CabinetSide.Right;

            // Decide strictly by where the center of the player's camera (crosshair ray) is aiming.
            Ray aimRay = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            if (Physics.Raycast(aimRay, out RaycastHit hit, 4.5f, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.transform == transform || hit.transform.IsChildOf(transform))
                {
                    if (_viewerLeftPivot != null && (hit.transform == _viewerLeftPivot || hit.transform.IsChildOf(_viewerLeftPivot)))
                    {
                        return CabinetSide.Left;
                    }

                    if (_viewerRightPivot != null && (hit.transform == _viewerRightPivot || hit.transform.IsChildOf(_viewerRightPivot)))
                    {
                        return CabinetSide.Right;
                    }

                    // Aiming at cabinet interior/shelves/frame: localX > 0 is viewer's Left, localX <= 0 is viewer's Right.
                    Vector3 localHit = transform.InverseTransformPoint(hit.point);
                    return localHit.x > 0f ? CabinetSide.Left : CabinetSide.Right;
                }
            }

            // Fallback: intersect center aim ray with cabinet front door plane (localZ = 0.29).
            Vector3 originLocal = transform.InverseTransformPoint(aimRay.origin);
            Vector3 dirLocal = transform.InverseTransformDirection(aimRay.direction);
            if (Mathf.Abs(dirLocal.z) > 0.001f)
            {
                float tPlane = (0.29f - originLocal.z) / dirLocal.z;
                if (tPlane > 0f)
                {
                    float hitX = originLocal.x + dirLocal.x * tPlane;
                    return hitX > 0f ? CabinetSide.Left : CabinetSide.Right;
                }
            }

            return CabinetSide.Right;
        }

        public void ToggleSide(CabinetSide side)
        {
            if (side == CabinetSide.Right)
            {
                if (IsViewerRightOpen) CloseSide(CabinetSide.Right);
                else OpenSide(CabinetSide.Right);
            }
            else
            {
                if (IsViewerLeftOpen) CloseSide(CabinetSide.Left);
                else OpenSide(CabinetSide.Left);
            }
        }

        public void OpenSide(CabinetSide side)
        {
            if (side == CabinetSide.Right && _viewerRightPivot != null && !IsViewerRightOpen)
            {
                IsViewerRightOpen = true;
                ApplyTargetRotations(immediate: false);
                PlayClip(openSound);
            }
            else if (side == CabinetSide.Left && _viewerLeftPivot != null && !IsViewerLeftOpen)
            {
                IsViewerLeftOpen = true;
                ApplyTargetRotations(immediate: false);
                PlayClip(openSound);
            }
        }

        public void CloseSide(CabinetSide side)
        {
            if (side == CabinetSide.Right && _viewerRightPivot != null && IsViewerRightOpen)
            {
                IsViewerRightOpen = false;
                ApplyTargetRotations(immediate: false);
                PlayClip(closeSound);
            }
            else if (side == CabinetSide.Left && _viewerLeftPivot != null && IsViewerLeftOpen)
            {
                IsViewerLeftOpen = false;
                ApplyTargetRotations(immediate: false);
                PlayClip(closeSound);
            }
        }

        public void OpenAll()
        {
            if (_viewerRightPivot != null) IsViewerRightOpen = true;
            if (_viewerLeftPivot != null) IsViewerLeftOpen = true;
            ApplyTargetRotations(immediate: false);
            PlayClip(openSound);
        }

        public void CloseAll()
        {
            IsViewerRightOpen = false;
            IsViewerLeftOpen = false;
            ApplyTargetRotations(immediate: false);
            PlayClip(closeSound);
        }

        private void MapPivotsToViewerSides()
        {
            if (leftDoorPivot != null && rightDoorPivot != null)
            {
                // Automatically pair pivots by localPosition.x so viewer-right (localX < 0) and viewer-left (localX > 0) never swap.
                if (leftDoorPivot.localPosition.x <= rightDoorPivot.localPosition.x)
                {
                    _viewerRightPivot = leftDoorPivot;
                    _viewerRightOpenAngle = leftOpenAngle;
                    _viewerLeftPivot = rightDoorPivot;
                    _viewerLeftOpenAngle = rightOpenAngle;
                }
                else
                {
                    _viewerRightPivot = rightDoorPivot;
                    _viewerRightOpenAngle = rightOpenAngle;
                    _viewerLeftPivot = leftDoorPivot;
                    _viewerLeftOpenAngle = leftOpenAngle;
                }
            }
            else
            {
                _viewerRightPivot = leftDoorPivot ?? rightDoorPivot;
                _viewerRightOpenAngle = leftDoorPivot != null ? leftOpenAngle : rightOpenAngle;
                _viewerLeftPivot = null;
            }

            if (_viewerRightPivot != null) _viewerRightClosedRot = _viewerRightPivot.localRotation;
            if (_viewerLeftPivot != null) _viewerLeftClosedRot = _viewerLeftPivot.localRotation;
        }

        private void ApplyTargetRotations(bool immediate)
        {
            _viewerRightTargetRot = IsViewerRightOpen
                ? _viewerRightClosedRot * Quaternion.Euler(0f, _viewerRightOpenAngle, 0f)
                : _viewerRightClosedRot;
            _viewerLeftTargetRot = IsViewerLeftOpen
                ? _viewerLeftClosedRot * Quaternion.Euler(0f, _viewerLeftOpenAngle, 0f)
                : _viewerLeftClosedRot;

            if (immediate)
            {
                if (_viewerRightPivot != null) _viewerRightPivot.localRotation = _viewerRightTargetRot;
                if (_viewerLeftPivot != null) _viewerLeftPivot.localRotation = _viewerLeftTargetRot;
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

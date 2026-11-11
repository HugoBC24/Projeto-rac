using UnityEngine;
using ProjectRac.Interaction;

namespace ProjectRac.Player
{
    public class PlayerInteraction : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float interactionDistance = 3.0f;
        [SerializeField] private LayerMask interactableLayers = ~0;
        [SerializeField] private Camera playerCamera;
        [SerializeField] private PlayerInputHandler inputHandler;

        [Header("UI Feedback")]
        [SerializeField] private bool showCrosshairAndPrompt = true;

        private IInteractable _currentInteractable;
        private string _currentPrompt = "";

        private void Start()
        {
            if (playerCamera == null)
            {
                playerCamera = GetComponentInChildren<Camera>();
            }

            if (inputHandler == null)
            {
                inputHandler = GetComponent<PlayerInputHandler>();
            }
        }

        private void Update()
        {
            CheckForInteractable();
            HandleInteraction();
        }

        private void CheckForInteractable()
        {
            if (playerCamera == null) return;

            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance, interactableLayers, QueryTriggerInteraction.Ignore))
            {
                var interactable = hit.collider.GetComponentInParent<IInteractable>();
                if (interactable != null)
                {
                    _currentInteractable = interactable;
                    _currentPrompt = interactable.GetInteractionPrompt();
                    return;
                }
            }

            _currentInteractable = null;
            _currentPrompt = "";
        }

        private void HandleInteraction()
        {
            if (_currentInteractable != null && inputHandler != null && inputHandler.InteractTriggered)
            {
                _currentInteractable.Interact();
            }
        }

        private void OnGUI()
        {
            if (!showCrosshairAndPrompt || Cursor.lockState != CursorLockMode.Locked) return;

            // Simple clean crosshair at center
            float centerX = Screen.width / 2f;
            float centerY = Screen.height / 2f;
            float size = 4f;

            GUI.color = _currentInteractable != null ? Color.green : Color.white;
            GUI.Box(new Rect(centerX - size / 2f, centerY - size / 2f, size, size), GUIContent.none);

            // Interaction prompt text
            if (!string.IsNullOrEmpty(_currentPrompt))
            {
                GUIStyle style = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 16,
                    fontStyle = FontStyle.Bold
                };
                style.normal.textColor = Color.white;

                GUI.Label(new Rect(centerX - 150f, centerY + 30f, 300f, 30f), _currentPrompt, style);
            }
        }
    }
}

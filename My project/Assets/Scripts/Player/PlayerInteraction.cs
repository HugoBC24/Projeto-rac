using UnityEngine;
using UnityEngine.InputSystem;
using ProjectRac.Interaction;
using ProjectRac.UI;

namespace ProjectRac.Player
{
    public class PlayerInteraction : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float interactionDistance = 3.5f;
        [SerializeField] private LayerMask interactableLayers = ~0;
        [SerializeField] private Camera playerCamera;
        [SerializeField] private PlayerInputHandler inputHandler;

        [Header("UI Feedback")]
        [SerializeField] private CrosshairHUD crosshairHUD;
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

            if (crosshairHUD == null)
            {
                crosshairHUD = FindAnyObjectByType<CrosshairHUD>();
            }
        }

        public void BindCrosshairHUD(CrosshairHUD hud)
        {
            crosshairHUD = hud;
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
            if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance, interactableLayers, QueryTriggerInteraction.Collide))
            {
                var interactable = hit.collider.GetComponentInParent<IInteractable>();
                if (interactable != null)
                {
                    _currentInteractable = interactable;
                    _currentPrompt = interactable.GetInteractionPrompt();
                    crosshairHUD?.SetTargetingInteractable(true, _currentPrompt);
                    return;
                }
            }

            if (_currentInteractable != null)
            {
                _currentInteractable = null;
                _currentPrompt = "";
                crosshairHUD?.SetTargetingInteractable(false, "");
            }
        }

        private void HandleInteraction()
        {
            // Prevent double-triggering on toggleables: InputSystem_Actions maps Interact with
            // a "Hold" interaction, so holding E would otherwise fire once on press and again on hold.
            var kb = Keyboard.current;
            bool ePressed = (kb != null && kb[Key.E].wasPressedThisFrame)
                         || ((kb == null || !kb[Key.E].isPressed) && inputHandler != null && inputHandler.InteractTriggered);

            if (_currentInteractable != null && ePressed)
            {
                _currentInteractable.Interact();
            }
        }

        private void OnGUI()
        {
            if (crosshairHUD != null || !showCrosshairAndPrompt || Cursor.lockState != CursorLockMode.Locked) return;

            float centerX = Screen.width / 2f;
            float centerY = Screen.height / 2f;
            float size = 4f;

            GUI.color = _currentInteractable != null ? Color.green : Color.white;
            GUI.Box(new Rect(centerX - size / 2f, centerY - size / 2f, size, size), GUIContent.none);

            if (!string.IsNullOrEmpty(_currentPrompt))
            {
                GUIStyle style = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 14,
                    fontStyle = FontStyle.Bold
                };
                style.normal.textColor = Color.white;
                GUI.Label(new Rect(centerX - 100f, centerY + 18f, 200f, 25f), _currentPrompt, style);
            }
        }
    }
}

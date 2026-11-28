using UnityEngine;
using UnityEngine.UI;
using ProjectRac.Puzzle;

namespace ProjectRac.UI
{
    public class CrosshairHUD : MonoBehaviour
    {
        [Header("UI Component References")]
        [SerializeField] private Image crosshairImage;
        [SerializeField] private Text promptText;

        [Header("Customizable Crosshair Image (Inspector)")]
        [Tooltip("Assign any custom Sprite here to change the player crosshair.")]
        [SerializeField] private Sprite crosshairSprite;
        [Tooltip("Dimensions (width x height) of the crosshair on screen.")]
        [SerializeField] private Vector2 crosshairSize = new Vector2(24f, 24f);

        [Header("Crosshair Colors")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color interactableColor = new Color(0.25f, 1f, 0.45f, 1f);

        public Sprite CrosshairSprite => crosshairSprite;
        public Vector2 CrosshairSize => crosshairSize;

        private bool _isTargetingInteractable;
        private string _currentPrompt = "";
        private string _tempMessage = "";
        private float _tempMessageUntil;
        private PlayerPuzzleComponentHolder _puzzleHolder;

        private void Awake()
        {
            ResolveReferences();
            ApplyVisualSettings();
        }

        private void Update()
        {
            if (!string.IsNullOrEmpty(_tempMessage) && Time.time >= _tempMessageUntil)
            {
                _tempMessage = "";
            }

            RefreshPromptDisplay();
        }

        public void ShowTemporaryMessage(string message, float duration = 3.5f)
        {
            _tempMessage = message ?? "";
            _tempMessageUntil = Time.time + Mathf.Max(0.5f, duration);
            RefreshPromptDisplay();
        }

        private void OnValidate()
        {
            ResolveReferences();
            ApplyVisualSettings();
        }

        public void BindComponents(Image image, Text prompt)
        {
            crosshairImage = image;
            promptText = prompt;
            ApplyVisualSettings();
        }

        public void SetCrosshairSprite(Sprite sprite)
        {
            crosshairSprite = sprite;
            ApplyVisualSettings();
        }

        public void SetCrosshairSize(Vector2 size)
        {
            crosshairSize = size;
            ApplyVisualSettings();
        }

        public void SetTargetingInteractable(bool isInteractable, string prompt = "")
        {
            _isTargetingInteractable = isInteractable;
            _currentPrompt = prompt ?? "";

            if (crosshairImage != null)
            {
                crosshairImage.color = _isTargetingInteractable ? interactableColor : normalColor;
            }

            RefreshPromptDisplay();
        }

        private void RefreshPromptDisplay()
        {
            if (promptText == null) return;

            if (_puzzleHolder == null)
            {
                _puzzleHolder = FindAnyObjectByType<PlayerPuzzleComponentHolder>();
            }

            string display = _currentPrompt;

            if (_puzzleHolder != null && _puzzleHolder.HasComponent)
            {
                string compName = ShapeRules.GetComponentName(_puzzleHolder.HeldComponent).ToUpper();
                string heldLine = $"[ EM MAOS: {compName} ]";
                display = string.IsNullOrEmpty(display) ? heldLine : $"{display}\n{heldLine}";
            }

            if (!string.IsNullOrEmpty(_tempMessage))
            {
                display = string.IsNullOrEmpty(display) ? _tempMessage : $"{display}\n{_tempMessage}";
            }

            promptText.text = display;
            promptText.gameObject.SetActive(!string.IsNullOrEmpty(display));
        }

        private void ResolveReferences()
        {
            if (crosshairImage == null)
            {
                crosshairImage = GetComponentInChildren<Image>();
            }

            if (promptText == null)
            {
                promptText = GetComponentInChildren<Text>();
            }
        }

        public void ApplyVisualSettings()
        {
            if (crosshairImage != null)
            {
                crosshairImage.sprite = crosshairSprite;
                crosshairImage.rectTransform.sizeDelta = crosshairSize;
                crosshairImage.color = _isTargetingInteractable ? interactableColor : normalColor;
                crosshairImage.enabled = crosshairSprite != null || crosshairImage.color.a > 0f;
            }

            if (promptText != null && string.IsNullOrEmpty(promptText.text))
            {
                promptText.gameObject.SetActive(false);
            }
        }
    }
}

using UnityEngine;
using UnityEngine.UI;

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

        private void Awake()
        {
            ResolveReferences();
            ApplyVisualSettings();
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

            if (crosshairImage != null)
            {
                crosshairImage.color = _isTargetingInteractable ? interactableColor : normalColor;
            }

            if (promptText != null)
            {
                promptText.text = prompt ?? "";
                promptText.gameObject.SetActive(!string.IsNullOrEmpty(prompt));
            }
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

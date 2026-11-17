using UnityEngine;
using UnityEngine.UI;

namespace ProjectRac.UI
{
    public class SlingshotChargeHUD : MonoBehaviour
    {
        [Header("UI Component References")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image fillImage;

        [Header("Customizable Charge Images (Inspector)")]
        [Tooltip("Background frame sprite for the slingshot tension indicator.")]
        [SerializeField] private Sprite backgroundSprite;
        [Tooltip("Fill sprite representing the elastic tension as the slingshot is pulled.")]
        [SerializeField] private Sprite fillSprite;

        [Header("Indicator Layout & Size")]
        [Tooltip("Size (width x height) in pixels of the tension bar.")]
        [SerializeField] private Vector2 barSize = new Vector2(70f, 10f);
        [Tooltip("Offset relative to the screen center/crosshair.")]
        [SerializeField] private Vector2 positionOffset = new Vector2(0f, -28f);

        [Header("Tension Color Gradient")]
        [SerializeField] private Color minChargeColor = new Color(1f, 0.9f, 0.2f, 1f);
        [SerializeField] private Color maxChargeColor = new Color(1f, 0.35f, 0.1f, 1f);

        public Sprite BackgroundSprite => backgroundSprite;
        public Sprite FillSprite => fillSprite;
        public Vector2 BarSize => barSize;
        public Vector2 PositionOffset => positionOffset;

        private RectTransform _rectTransform;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            ResolveReferences();
            ApplyVisualSettings();
            SetVisible(false);
        }

        private void OnValidate()
        {
            if (_rectTransform == null) _rectTransform = GetComponent<RectTransform>();
            ResolveReferences();
            ApplyVisualSettings();
        }

        public void BindComponents(CanvasGroup group, Image bg, Image fill)
        {
            canvasGroup = group;
            backgroundImage = bg;
            fillImage = fill;
            ApplyVisualSettings();
        }

        public void SetSprites(Sprite bg, Sprite fill)
        {
            backgroundSprite = bg;
            fillSprite = fill;
            ApplyVisualSettings();
        }

        public void SetLayout(Vector2 size, Vector2 offset)
        {
            barSize = size;
            positionOffset = offset;
            ApplyVisualSettings();
        }

        public void UpdateCharge(bool isCharging, float chargeNormalized)
        {
            if (!isCharging)
            {
                SetVisible(false);
                return;
            }

            SetVisible(true);

            if (fillImage != null)
            {
                fillImage.fillAmount = Mathf.Clamp01(chargeNormalized);
                fillImage.color = Color.Lerp(minChargeColor, maxChargeColor, chargeNormalized);
            }
        }

        private void SetVisible(bool visible)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = visible ? 1f : 0f;
            }
            else
            {
                gameObject.SetActive(visible);
            }
        }

        private void ResolveReferences()
        {
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

            if (backgroundImage == null)
            {
                var bgTransform = transform.Find("Background");
                if (bgTransform != null) backgroundImage = bgTransform.GetComponent<Image>();
            }

            if (fillImage == null)
            {
                var fillTransform = transform.Find("Fill");
                if (fillTransform != null) fillImage = fillTransform.GetComponent<Image>();
            }
        }

        public void ApplyVisualSettings()
        {
            if (_rectTransform != null)
            {
                _rectTransform.sizeDelta = barSize;
                _rectTransform.anchoredPosition = positionOffset;
            }

            if (backgroundImage != null)
            {
                backgroundImage.sprite = backgroundSprite;
                backgroundImage.type = backgroundSprite != null ? Image.Type.Simple : Image.Type.Sliced;
            }

            if (fillImage != null)
            {
                fillImage.sprite = fillSprite;
                fillImage.type = Image.Type.Filled;
                fillImage.fillMethod = Image.FillMethod.Horizontal;
                fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            }
        }
    }
}

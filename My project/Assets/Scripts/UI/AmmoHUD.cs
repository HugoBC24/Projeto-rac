using UnityEngine;
using UnityEngine.UI;

namespace ProjectRac.UI
{
    public class AmmoHUD : MonoBehaviour
    {
        [Header("UI Component References")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Text weaponNameText;
        [SerializeField] private Text ammoCountText;
        [SerializeField] private Text reloadStatusText;

        [Header("Color Palette (Inspector)")]
        [SerializeField] private Color normalColor = new Color(1f, 0.95f, 0.8f, 1f);
        [SerializeField] private Color lowAmmoColor = new Color(1f, 0.65f, 0.2f, 1f);
        [SerializeField] private Color emptyColor = new Color(1f, 0.25f, 0.2f, 1f);
        [SerializeField] private Color reloadingColor = new Color(1f, 0.85f, 0.25f, 1f);

        [Header("Infinite Ammo Symbol")]
        [SerializeField] private string infiniteSymbol = "∞";

        private RectTransform _rectTransform;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            ResolveReferences();
        }

        public void BindComponents(CanvasGroup group, Image bg, Text nameText, Text countText, Text statusText)
        {
            canvasGroup = group;
            backgroundImage = bg;
            weaponNameText = nameText;
            ammoCountText = countText;
            reloadStatusText = statusText;
        }

        private void ResolveReferences()
        {
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

            if (weaponNameText == null)
            {
                var t = transform.Find("WeaponName");
                if (t != null) weaponNameText = t.GetComponent<Text>();
            }

            if (ammoCountText == null)
            {
                var t = transform.Find("AmmoCount");
                if (t != null) ammoCountText = t.GetComponent<Text>();
            }

            if (reloadStatusText == null)
            {
                var t = transform.Find("ReloadStatus");
                if (t != null) reloadStatusText = t.GetComponent<Text>();
            }
        }

        public void UpdateAmmo(string weaponName, int currentAmmo, int maxAmmo, bool isReloading)
        {
            ResolveReferences();

            if (weaponNameText != null)
            {
                weaponNameText.text = weaponName.ToUpper();
            }

            if (isReloading)
            {
                if (ammoCountText != null) ammoCountText.gameObject.SetActive(false);
                if (reloadStatusText != null)
                {
                    reloadStatusText.gameObject.SetActive(true);
                    reloadStatusText.text = "RECARREGANDO...";
                    reloadStatusText.color = reloadingColor;
                }
                return;
            }

            if (reloadStatusText != null) reloadStatusText.gameObject.SetActive(false);
            if (ammoCountText != null)
            {
                ammoCountText.gameObject.SetActive(true);

                if (maxAmmo < 0)
                {
                    ammoCountText.text = infiniteSymbol;
                    ammoCountText.color = normalColor;
                }
                else
                {
                    ammoCountText.text = $"{currentAmmo} / {maxAmmo}";

                    if (currentAmmo == 0)
                    {
                        ammoCountText.color = emptyColor;
                    }
                    else if (currentAmmo <= maxAmmo * 0.35f)
                    {
                        ammoCountText.color = lowAmmoColor;
                    }
                    else
                    {
                        ammoCountText.color = normalColor;
                    }
                }
            }
        }

        public void SetVisible(bool visible)
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
    }
}

using UnityEngine;
using UnityEngine.UI;

namespace ProjectRac.UI
{
    public class WeaponHUD : MonoBehaviour
    {
        [Header("Slot References")]
        [SerializeField] private Image slot1Background;
        [SerializeField] private Image slot1Icon;
        [SerializeField] private Outline slot1Outline;

        [SerializeField] private Image slot2Background;
        [SerializeField] private Image slot2Icon;
        [SerializeField] private Outline slot2Outline;

        [Header("Customizable Weapon Icons (Inspector)")]
        [Tooltip("Custom icon for Slot 1 (Slingshot).")]
        [SerializeField] private Sprite slingshotIcon;
        [Tooltip("Custom icon for Slot 2 (Shotgun).")]
        [SerializeField] private Sprite shotgunIcon;

        [Header("Color Palette")]
        [SerializeField] private Color activeColor = Color.white;
        [SerializeField] private Color inactiveColor = new Color(0.6f, 0.6f, 0.6f, 0.7f);
        [SerializeField] private Color lockedColor = new Color(0.18f, 0.18f, 0.18f, 0.4f);
        [SerializeField] private Color activeOutlineColor = new Color(1f, 0.85f, 0.3f, 0.95f);
        [SerializeField] private Color inactiveOutlineColor = new Color(0.4f, 0.4f, 0.4f, 0.5f);

        private int _currentSlot = 1;
        private bool _hasShotgun = false;

        private void Awake()
        {
            ApplyVisualSettings();
        }

        private void OnValidate()
        {
            ApplyVisualSettings();
        }

        public void BindComponents(Image bg1, Image icon1, Outline out1, Image bg2, Image icon2, Outline out2)
        {
            slot1Background = bg1;
            slot1Icon = icon1;
            slot1Outline = out1;
            slot2Background = bg2;
            slot2Icon = icon2;
            slot2Outline = out2;
            ApplyVisualSettings();
        }

        public void SetIcons(Sprite sling, Sprite shotgun)
        {
            slingshotIcon = sling;
            shotgunIcon = shotgun;
            ApplyVisualSettings();
        }

        public void UpdateActiveSlot(int activeSlot, bool hasShotgun)
        {
            _currentSlot = activeSlot;
            _hasShotgun = hasShotgun;
            ApplyVisualSettings();
        }

        public void FlashLockedSlot(int slot)
        {
            if (slot == 2 && slot2Background != null)
            {
                slot2Background.color = new Color(0.8f, 0.2f, 0.2f, 0.7f);
                Invoke(nameof(ApplyVisualSettings), 0.2f);
            }
        }

        public void ApplyVisualSettings()
        {
            // Slot 1 (Slingshot - always owned)
            if (slot1Icon != null)
            {
                if (slingshotIcon != null) slot1Icon.sprite = slingshotIcon;
                slot1Icon.color = _currentSlot == 1 ? activeColor : inactiveColor;
            }

            if (slot1Outline != null)
            {
                slot1Outline.effectColor = _currentSlot == 1 ? activeOutlineColor : inactiveOutlineColor;
                slot1Outline.enabled = true;
            }

            if (slot1Background != null)
            {
                slot1Background.color = _currentSlot == 1
                    ? new Color(0.2f, 0.2f, 0.25f, 0.85f)
                    : new Color(0.1f, 0.1f, 0.12f, 0.6f);
            }

            // Slot 2 (Shotgun - locked until collected)
            if (slot2Icon != null)
            {
                if (shotgunIcon != null) slot2Icon.sprite = shotgunIcon;

                if (!_hasShotgun)
                {
                    slot2Icon.color = lockedColor;
                }
                else
                {
                    slot2Icon.color = _currentSlot == 2 ? activeColor : inactiveColor;
                }
            }

            if (slot2Outline != null)
            {
                slot2Outline.effectColor = (_currentSlot == 2 && _hasShotgun)
                    ? activeOutlineColor
                    : inactiveOutlineColor;
            }

            if (slot2Background != null)
            {
                if (!_hasShotgun)
                {
                    slot2Background.color = new Color(0.08f, 0.08f, 0.1f, 0.45f);
                }
                else
                {
                    slot2Background.color = _currentSlot == 2
                        ? new Color(0.2f, 0.2f, 0.25f, 0.85f)
                        : new Color(0.1f, 0.1f, 0.12f, 0.6f);
                }
            }
        }
    }
}

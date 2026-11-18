using UnityEngine;
using UnityEngine.UI;
using ProjectRac.Player;

namespace ProjectRac.UI
{
    public class PlayerHealthBarUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerHealth playerHealth;
        [SerializeField] private Slider healthSlider;
        [SerializeField] private Image fillImage;
        [SerializeField] private Text valueText;

        [Header("Visual Feedback")]
        [SerializeField] private Color normalColor = new Color(0.85f, 0.18f, 0.18f, 0.95f);
        [SerializeField] private Color criticalColor = new Color(0.98f, 0.08f, 0.08f, 1f);
        [SerializeField] private float criticalThreshold = 0.3f;

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            if (playerHealth != null)
            {
                playerHealth.OnHealthChanged += HandleHealthChanged;
                HandleHealthChanged(playerHealth.CurrentHealth, playerHealth.MaxHealth);
            }
        }

        private void OnDisable()
        {
            if (playerHealth != null)
            {
                playerHealth.OnHealthChanged -= HandleHealthChanged;
            }
        }

        public void BindPlayerHealth(PlayerHealth health)
        {
            if (playerHealth != null)
            {
                playerHealth.OnHealthChanged -= HandleHealthChanged;
            }

            playerHealth = health;
            if (playerHealth != null && enabled)
            {
                playerHealth.OnHealthChanged += HandleHealthChanged;
                HandleHealthChanged(playerHealth.CurrentHealth, playerHealth.MaxHealth);
            }
        }

        public void SetUGUIReferences(Slider slider, Image fill, Text text)
        {
            healthSlider = slider;
            fillImage = fill;
            valueText = text;

            if (playerHealth != null)
            {
                HandleHealthChanged(playerHealth.CurrentHealth, playerHealth.MaxHealth);
            }
        }

        private void ResolveReferences()
        {
            if (playerHealth == null)
            {
                playerHealth = GetComponentInParent<PlayerHealth>();
                if (playerHealth == null)
                {
                    playerHealth = FindAnyObjectByType<PlayerHealth>();
                }
            }
        }

        private void HandleHealthChanged(float current, float max)
        {
            float ratio = max > 0f ? Mathf.Clamp01(current / max) : 0f;

            if (healthSlider != null)
            {
                healthSlider.value = ratio;
            }

            if (fillImage != null)
            {
                fillImage.color = ratio <= criticalThreshold ? criticalColor : normalColor;
            }

            if (valueText != null)
            {
                valueText.text = $"VIDA: {Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
            }
        }
    }
}

using UnityEngine;
using UnityEngine.UI;
using ProjectRac.Player;

namespace ProjectRac.UI
{
    public class StaminaBarUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerStamina playerStamina;
        [SerializeField] private Slider staminaSlider;
        [SerializeField] private Image fillImage;
        [SerializeField] private Text valueText;

        [Header("Color Palette")]
        [SerializeField] private Color normalColor = new Color(0.2f, 0.85f, 0.4f, 0.95f);
        [SerializeField] private Color warningColor = new Color(0.95f, 0.75f, 0.15f, 0.95f);
        [SerializeField] private Color exhaustedColor = new Color(0.95f, 0.25f, 0.2f, 0.95f);
        [SerializeField] private Color backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.75f);
        [SerializeField] private Color borderColor = new Color(0.8f, 0.8f, 0.8f, 0.8f);

        [Header("OnGUI Fallback (Active only if uGUI is not assigned)")]
        [SerializeField] private float barX = 30f;
        [SerializeField] private float barYOffsetFromBottom = 60f;
        [SerializeField] private float barWidth = 220f;
        [SerializeField] private float barHeight = 18f;

        private Texture2D _whiteTexture;
        private GUIStyle _labelStyle;
        private bool _isExhausted;

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            if (playerStamina != null)
            {
                playerStamina.OnStaminaChanged += HandleStaminaChanged;
                playerStamina.OnExhaustionChanged += HandleExhaustionChanged;
                HandleStaminaChanged(playerStamina.CurrentStamina, playerStamina.MaxStamina);
            }
        }

        private void OnDisable()
        {
            if (playerStamina != null)
            {
                playerStamina.OnStaminaChanged -= HandleStaminaChanged;
                playerStamina.OnExhaustionChanged -= HandleExhaustionChanged;
            }
        }

        private void OnDestroy()
        {
            if (_whiteTexture != null)
            {
                Destroy(_whiteTexture);
            }
        }

        public void BindPlayerStamina(PlayerStamina stamina)
        {
            if (playerStamina != null)
            {
                playerStamina.OnStaminaChanged -= HandleStaminaChanged;
                playerStamina.OnExhaustionChanged -= HandleExhaustionChanged;
            }

            playerStamina = stamina;
            if (playerStamina != null && enabled)
            {
                playerStamina.OnStaminaChanged += HandleStaminaChanged;
                playerStamina.OnExhaustionChanged += HandleExhaustionChanged;
                HandleStaminaChanged(playerStamina.CurrentStamina, playerStamina.MaxStamina);
            }
        }

        public void SetUGUIReferences(Slider slider, Image fill, Text text)
        {
            staminaSlider = slider;
            fillImage = fill;
            valueText = text;

            if (playerStamina != null)
            {
                HandleStaminaChanged(playerStamina.CurrentStamina, playerStamina.MaxStamina);
            }
        }

        private void ResolveReferences()
        {
            if (playerStamina == null)
            {
                playerStamina = GetComponentInParent<PlayerStamina>();
                if (playerStamina == null)
                {
                    playerStamina = FindAnyObjectByType<PlayerStamina>();
                }
            }

            if (staminaSlider == null)
            {
                staminaSlider = GetComponentInChildren<Slider>();
            }
        }

        private void Update()
        {
            if (_isExhausted)
            {
                // Pulsing red effect when exhausted
                float pulse = 0.7f + 0.3f * Mathf.Sin(Time.time * 8f);
                Color activeColor = new Color(exhaustedColor.r, exhaustedColor.g, exhaustedColor.b, pulse);

                if (fillImage != null) fillImage.color = activeColor;
                if (valueText != null) valueText.color = activeColor;
            }
        }

        private void HandleStaminaChanged(float current, float max)
        {
            float ratio = max > 0f ? Mathf.Clamp01(current / max) : 0f;

            if (staminaSlider != null)
            {
                staminaSlider.value = ratio;
            }

            if (!_isExhausted)
            {
                Color targetColor = ratio < 0.2f ? exhaustedColor : (ratio < 0.4f ? warningColor : normalColor);
                if (fillImage != null) fillImage.color = targetColor;
                if (valueText != null) valueText.color = Color.white;
            }

            if (valueText != null)
            {
                valueText.text = _isExhausted
                    ? "STAMINA: EXAUSTO!"
                    : $"STAMINA: {Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
            }
        }

        private void HandleExhaustionChanged(bool isExhausted)
        {
            _isExhausted = isExhausted;
            if (playerStamina != null)
            {
                HandleStaminaChanged(playerStamina.CurrentStamina, playerStamina.MaxStamina);
            }
        }

        private void OnGUI()
        {
            // If uGUI is assigned and active, suppress OnGUI to avoid duplicate rendering
            if (staminaSlider != null || fillImage != null) return;
            if (playerStamina == null) return;

            if (_whiteTexture == null)
            {
                _whiteTexture = new Texture2D(1, 1);
                _whiteTexture.SetPixel(0, 0, Color.white);
                _whiteTexture.Apply();
            }

            float yPos = Screen.height - barYOffsetFromBottom;
            float currentRatio = playerStamina.StaminaNormalized;

            DrawColoredRect(new Rect(barX - 1f, yPos - 1f, barWidth + 2f, barHeight + 2f), borderColor);
            DrawColoredRect(new Rect(barX, yPos, barWidth, barHeight), backgroundColor);

            Color barColor = normalColor;
            if (_isExhausted || currentRatio < 0.2f)
            {
                float pulse = 0.7f + 0.3f * Mathf.Sin(Time.time * 8f);
                barColor = new Color(exhaustedColor.r, exhaustedColor.g, exhaustedColor.b, pulse);
            }
            else if (currentRatio < 0.4f)
            {
                barColor = warningColor;
            }

            float fillWidth = barWidth * currentRatio;
            if (fillWidth > 0f)
            {
                DrawColoredRect(new Rect(barX, yPos, fillWidth, barHeight), barColor);
            }

            if (_labelStyle == null)
            {
                _labelStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleLeft,
                    fontSize = 12,
                    fontStyle = FontStyle.Bold
                };
            }

            string labelText = _isExhausted
                ? "STAMINA: EXAUSTO!"
                : $"STAMINA: {Mathf.CeilToInt(playerStamina.CurrentStamina)} / {Mathf.CeilToInt(playerStamina.MaxStamina)}";

            _labelStyle.normal.textColor = Color.black;
            GUI.Label(new Rect(barX + 6f, yPos - 19f, barWidth, 20f), labelText, _labelStyle);

            _labelStyle.normal.textColor = _isExhausted ? Color.red : Color.white;
            GUI.Label(new Rect(barX + 5f, yPos - 20f, barWidth, 20f), labelText, _labelStyle);
        }

        private void DrawColoredRect(Rect rect, Color color)
        {
            Color prevColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, _whiteTexture);
            GUI.color = prevColor;
        }
    }
}

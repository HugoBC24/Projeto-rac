using UnityEngine;
using UnityEngine.UI;
using ProjectRac.Player;

namespace ProjectRac.UI
{
    public class StaminaBarUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerStamina playerStamina;
        [SerializeField] private Slider optionalSlider;

        [Header("Position & Dimensions (OnGUI)")]
        [SerializeField] private float barX = 30f;
        [SerializeField] private float barYOffsetFromBottom = 60f;
        [SerializeField] private float barWidth = 220f;
        [SerializeField] private float barHeight = 18f;

        [Header("Colors")]
        [SerializeField] private Color normalColor = new Color(0.2f, 0.85f, 0.4f, 0.95f);
        [SerializeField] private Color warningColor = new Color(0.95f, 0.75f, 0.15f, 0.95f);
        [SerializeField] private Color exhaustedColor = new Color(0.95f, 0.25f, 0.2f, 0.95f);
        [SerializeField] private Color backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.75f);
        [SerializeField] private Color borderColor = new Color(0.8f, 0.8f, 0.8f, 0.8f);

        private Texture2D _whiteTexture;
        private GUIStyle _labelStyle;

        private void Awake()
        {
            if (playerStamina == null)
            {
                playerStamina = GetComponentInParent<PlayerStamina>();
                if (playerStamina == null)
                {
                    playerStamina = FindAnyObjectByType<PlayerStamina>();
                }
            }

            CreateTexture();
        }

        private void OnDestroy()
        {
            if (_whiteTexture != null)
            {
                Destroy(_whiteTexture);
            }
        }

        private void CreateTexture()
        {
            _whiteTexture = new Texture2D(1, 1);
            _whiteTexture.SetPixel(0, 0, Color.white);
            _whiteTexture.Apply();
        }

        private void Update()
        {
            if (optionalSlider != null && playerStamina != null)
            {
                optionalSlider.value = playerStamina.StaminaNormalized;
            }
        }

        private void OnGUI()
        {
            if (playerStamina == null || _whiteTexture == null) return;

            float yPos = Screen.height - barYOffsetFromBottom;
            float currentRatio = playerStamina.StaminaNormalized;

            // 1. Draw Outer Border (1px)
            Rect borderRect = new Rect(barX - 1f, yPos - 1f, barWidth + 2f, barHeight + 2f);
            DrawColoredRect(borderRect, borderColor);

            // 2. Draw Background
            Rect bgRect = new Rect(barX, yPos, barWidth, barHeight);
            DrawColoredRect(bgRect, backgroundColor);

            // 3. Select Color
            Color barColor = normalColor;
            if (playerStamina.IsExhausted || currentRatio < 0.2f)
            {
                // Pulsing red effect when exhausted
                float pulse = 0.7f + 0.3f * Mathf.Sin(Time.time * 8f);
                barColor = new Color(exhaustedColor.r, exhaustedColor.g, exhaustedColor.b, pulse);
            }
            else if (currentRatio < 0.4f)
            {
                barColor = warningColor;
            }

            // 4. Draw Fill Bar
            float fillWidth = barWidth * currentRatio;
            if (fillWidth > 0f)
            {
                Rect fillRect = new Rect(barX, yPos, fillWidth, barHeight);
                DrawColoredRect(fillRect, barColor);
            }

            // 5. Draw Text Label (e.g. STAMINA: 85 / 100)
            if (_labelStyle == null)
            {
                _labelStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleLeft,
                    fontSize = 12,
                    fontStyle = FontStyle.Bold
                };
                _labelStyle.normal.textColor = Color.white;
            }

            string labelText = playerStamina.IsExhausted
                ? "STAMINA: EXAUSTO!"
                : $"STAMINA: {Mathf.CeilToInt(playerStamina.CurrentStamina)} / {Mathf.CeilToInt(playerStamina.MaxStamina)}";

            // Drop shadow for text readability
            Rect shadowRect = new Rect(barX + 6f, yPos - 19f, barWidth, 20f);
            _labelStyle.normal.textColor = Color.black;
            GUI.Label(shadowRect, labelText, _labelStyle);

            Rect textRect = new Rect(barX + 5f, yPos - 20f, barWidth, 20f);
            _labelStyle.normal.textColor = playerStamina.IsExhausted ? Color.red : Color.white;
            GUI.Label(textRect, labelText, _labelStyle);
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

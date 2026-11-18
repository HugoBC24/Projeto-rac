using UnityEngine;
using UnityEngine.UI;
using ProjectRac.Player;
using ProjectRac.UI;

namespace ProjectRac.Editor
{
    public static class HealthHUDBuilder
    {
        public static void BuildHealthHUD(Transform parent, GameObject player)
        {
            GameObject healthRoot = new GameObject("HealthHUD");
            healthRoot.transform.SetParent(parent, false);

            RectTransform rootRt = healthRoot.AddComponent<RectTransform>();
            rootRt.anchorMin = new Vector2(0f, 0f);
            rootRt.anchorMax = new Vector2(0f, 0f);
            rootRt.pivot = new Vector2(0f, 0f);
            rootRt.anchoredPosition = new Vector2(40f, 96f);
            rootRt.sizeDelta = new Vector2(260f, 24f);

            // Background
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(healthRoot.transform, false);
            RectTransform bgRt = bgObj.AddComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.sizeDelta = Vector2.zero;

            Image bgImg = bgObj.AddComponent<Image>();
            bgImg.color = new Color(0.1f, 0.1f, 0.1f, 0.85f);

            Outline bgOutline = bgObj.AddComponent<Outline>();
            bgOutline.effectColor = new Color(0.7f, 0.7f, 0.7f, 0.75f);
            bgOutline.effectDistance = new Vector2(1.5f, -1.5f);

            // Slider
            Slider slider = healthRoot.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;
            slider.interactable = false;
            slider.transition = Selectable.Transition.None;

            GameObject fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(healthRoot.transform, false);
            RectTransform fillAreaRt = fillArea.AddComponent<RectTransform>();
            fillAreaRt.anchorMin = Vector2.zero;
            fillAreaRt.anchorMax = Vector2.one;
            fillAreaRt.sizeDelta = new Vector2(-4f, -4f);

            GameObject fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(fillArea.transform, false);
            RectTransform fillRt = fillObj.AddComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.sizeDelta = Vector2.zero;

            Image fillImg = fillObj.AddComponent<Image>();
            fillImg.color = new Color(0.85f, 0.18f, 0.18f, 0.95f);

            slider.fillRect = fillRt;
            slider.targetGraphic = fillImg;

            // Label
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(healthRoot.transform, false);
            RectTransform labelRt = labelObj.AddComponent<RectTransform>();
            labelRt.anchorMin = new Vector2(0f, 1f);
            labelRt.anchorMax = new Vector2(1f, 1f);
            labelRt.pivot = new Vector2(0f, 0f);
            labelRt.anchoredPosition = new Vector2(2f, 6f);
            labelRt.sizeDelta = new Vector2(0f, 22f);

            Text labelText = labelObj.AddComponent<Text>();
            labelText.text = "VIDA: 100 / 100";
            labelText.fontSize = 13;
            labelText.fontStyle = FontStyle.Bold;
            labelText.alignment = TextAnchor.MiddleLeft;
            labelText.color = Color.white;
            labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");

            Shadow textShadow = labelObj.AddComponent<Shadow>();
            textShadow.effectColor = new Color(0f, 0f, 0f, 0.9f);
            textShadow.effectDistance = new Vector2(1f, -1f);

            PlayerHealthBarUI healthUI = healthRoot.AddComponent<PlayerHealthBarUI>();
            var playerHealth = player.GetComponent<PlayerHealth>();
            healthUI.BindPlayerHealth(playerHealth);
            healthUI.SetUGUIReferences(slider, fillImg, labelText);
        }
    }
}

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEditor;
using ProjectRac.Player;
using ProjectRac.UI;

namespace ProjectRac.Editor
{
    public static class HUDCanvasBuilder
    {
        public static void SetupHUDCanvas(GameObject player)
        {
            GameObject canvasObj = GameObject.Find("HUD_Canvas");
            if (canvasObj != null)
            {
                Undo.DestroyObjectImmediate(canvasObj);
            }

            canvasObj = new GameObject("HUD_Canvas");
            Undo.RegisterCreatedObjectUndo(canvasObj, "Create HUD Canvas");

            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            // EventSystem
            if (Object.FindAnyObjectByType<EventSystem>() == null)
            {
                GameObject eventSys = new GameObject("EventSystem");
                Undo.RegisterCreatedObjectUndo(eventSys, "Create EventSystem");
                eventSys.AddComponent<EventSystem>();
                eventSys.AddComponent<InputSystemUIInputModule>();
            }

            // Stamina HUD Root Container
            GameObject staminaRoot = new GameObject("StaminaHUD");
            staminaRoot.transform.SetParent(canvasObj.transform, false);

            RectTransform rootRt = staminaRoot.AddComponent<RectTransform>();
            rootRt.anchorMin = new Vector2(0f, 0f);
            rootRt.anchorMax = new Vector2(0f, 0f);
            rootRt.pivot = new Vector2(0f, 0f);
            rootRt.anchoredPosition = new Vector2(40f, 40f);
            rootRt.sizeDelta = new Vector2(260f, 24f);

            // Background Panel
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(staminaRoot.transform, false);
            RectTransform bgRt = bgObj.AddComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.sizeDelta = Vector2.zero;

            Image bgImg = bgObj.AddComponent<Image>();
            bgImg.color = new Color(0.1f, 0.1f, 0.1f, 0.85f);

            Outline bgOutline = bgObj.AddComponent<Outline>();
            bgOutline.effectColor = new Color(0.7f, 0.7f, 0.7f, 0.75f);
            bgOutline.effectDistance = new Vector2(1.5f, -1.5f);

            // Slider & Fill Area
            Slider slider = staminaRoot.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;
            slider.interactable = false;
            slider.transition = Selectable.Transition.None;

            GameObject fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(staminaRoot.transform, false);
            RectTransform fillAreaRt = fillArea.AddComponent<RectTransform>();
            fillAreaRt.anchorMin = Vector2.zero;
            fillAreaRt.anchorMax = Vector2.one;
            fillAreaRt.sizeDelta = new Vector2(-4f, -4f);
            fillAreaRt.anchoredPosition = Vector2.zero;

            GameObject fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(fillArea.transform, false);
            RectTransform fillRt = fillObj.AddComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.sizeDelta = Vector2.zero;

            Image fillImg = fillObj.AddComponent<Image>();
            fillImg.color = new Color(0.2f, 0.85f, 0.4f, 0.95f);

            slider.fillRect = fillRt;
            slider.targetGraphic = fillImg;

            // Label Text
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(staminaRoot.transform, false);
            RectTransform labelRt = labelObj.AddComponent<RectTransform>();
            labelRt.anchorMin = new Vector2(0f, 1f);
            labelRt.anchorMax = new Vector2(1f, 1f);
            labelRt.pivot = new Vector2(0f, 0f);
            labelRt.anchoredPosition = new Vector2(2f, 6f);
            labelRt.sizeDelta = new Vector2(0f, 22f);

            Text labelText = labelObj.AddComponent<Text>();
            labelText.text = "STAMINA: 100 / 100";
            labelText.fontSize = 13;
            labelText.fontStyle = FontStyle.Bold;
            labelText.alignment = TextAnchor.MiddleLeft;
            labelText.color = Color.white;
            labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");

            Shadow textShadow = labelObj.AddComponent<Shadow>();
            textShadow.effectColor = new Color(0f, 0f, 0f, 0.9f);
            textShadow.effectDistance = new Vector2(1f, -1f);

            // Bind UI Controller
            StaminaBarUI staminaUI = staminaRoot.AddComponent<StaminaBarUI>();
            var playerStamina = player.GetComponent<PlayerStamina>();
            staminaUI.BindPlayerStamina(playerStamina);
            staminaUI.SetUGUIReferences(slider, fillImg, labelText);
        }
    }
}

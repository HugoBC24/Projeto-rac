using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEditor;
using ProjectRac.Player;
using ProjectRac.UI;
using ProjectRac.Weapons;

namespace ProjectRac.Editor
{
    public static class HUDCanvasBuilder
    {
        private const string CrosshairAssetPath = "Assets/Art/UI/default_crosshair.png";
        private const string ChargeBgAssetPath = "Assets/Art/UI/slingshot_charge_bg.png";
        private const string ChargeFillAssetPath = "Assets/Art/UI/slingshot_charge_fill.png";

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

            // 0. Health HUD (stacked above Stamina)
            HealthHUDBuilder.BuildHealthHUD(canvasObj.transform, player);

            // 1. Stamina HUD
            BuildStaminaHUD(canvasObj.transform, player);

            // 2. Crosshair HUD
            var crosshairHUD = BuildCrosshairHUD(canvasObj.transform);
            var playerInteraction = player.GetComponent<PlayerInteraction>();
            if (playerInteraction != null && crosshairHUD != null)
            {
                playerInteraction.BindCrosshairHUD(crosshairHUD);
            }

            // 3. Slingshot Tension Charge HUD
            var chargeHUD = BuildSlingshotChargeHUD(canvasObj.transform);
            var slingshot = player.GetComponentInChildren<SlingshotWeapon>(true);
            if (slingshot != null && chargeHUD != null)
            {
                slingshot.BindChargeHUD(chargeHUD);
            }

            // 4. Current Ammo HUD
            var ammoHUD = AmmoHUDBuilder.BuildAmmoHUD(canvasObj.transform);
            var inventory = player.GetComponent<PlayerWeaponInventory>();
            if (inventory != null && ammoHUD != null)
            {
                inventory.BindAmmoHUD(ammoHUD);
            }
        }

        private static void BuildStaminaHUD(Transform parent, GameObject player)
        {
            GameObject staminaRoot = new GameObject("StaminaHUD");
            staminaRoot.transform.SetParent(parent, false);

            RectTransform rootRt = staminaRoot.AddComponent<RectTransform>();
            rootRt.anchorMin = new Vector2(0f, 0f);
            rootRt.anchorMax = new Vector2(0f, 0f);
            rootRt.pivot = new Vector2(0f, 0f);
            rootRt.anchoredPosition = new Vector2(40f, 40f);
            rootRt.sizeDelta = new Vector2(260f, 24f);

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

            StaminaBarUI staminaUI = staminaRoot.AddComponent<StaminaBarUI>();
            var playerStamina = player.GetComponent<PlayerStamina>();
            staminaUI.BindPlayerStamina(playerStamina);
            staminaUI.SetUGUIReferences(slider, fillImg, labelText);
        }

        private static CrosshairHUD BuildCrosshairHUD(Transform parent)
        {
            Sprite crosshairSprite = EnsureAndLoadSprite(CrosshairAssetPath);

            GameObject crosshairObj = new GameObject("Crosshair");
            crosshairObj.transform.SetParent(parent, false);

            RectTransform crosshairRt = crosshairObj.AddComponent<RectTransform>();
            crosshairRt.anchorMin = new Vector2(0.5f, 0.5f);
            crosshairRt.anchorMax = new Vector2(0.5f, 0.5f);
            crosshairRt.pivot = new Vector2(0.5f, 0.5f);
            crosshairRt.anchoredPosition = Vector2.zero;
            crosshairRt.sizeDelta = new Vector2(24f, 24f);

            Image crosshairImg = crosshairObj.AddComponent<Image>();
            crosshairImg.sprite = crosshairSprite;
            crosshairImg.color = Color.white;
            crosshairImg.raycastTarget = false;

            GameObject promptObj = new GameObject("PromptText");
            promptObj.transform.SetParent(crosshairObj.transform, false);

            RectTransform promptRt = promptObj.AddComponent<RectTransform>();
            promptRt.anchorMin = new Vector2(0.5f, 0.5f);
            promptRt.anchorMax = new Vector2(0.5f, 0.5f);
            promptRt.pivot = new Vector2(0.5f, 1f);
            promptRt.anchoredPosition = new Vector2(0f, -20f);
            promptRt.sizeDelta = new Vector2(300f, 30f);

            Text promptText = promptObj.AddComponent<Text>();
            promptText.text = "";
            promptText.fontSize = 14;
            promptText.fontStyle = FontStyle.Bold;
            promptText.alignment = TextAnchor.MiddleCenter;
            promptText.color = Color.white;
            promptText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            promptText.raycastTarget = false;

            Shadow promptShadow = promptObj.AddComponent<Shadow>();
            promptShadow.effectColor = new Color(0f, 0f, 0f, 0.9f);
            promptShadow.effectDistance = new Vector2(1f, -1f);
            promptObj.SetActive(false);

            CrosshairHUD hud = crosshairObj.AddComponent<CrosshairHUD>();
            hud.BindComponents(crosshairImg, promptText);
            if (crosshairSprite != null) hud.SetCrosshairSprite(crosshairSprite);

            return hud;
        }

        private static SlingshotChargeHUD BuildSlingshotChargeHUD(Transform parent)
        {
            Sprite bgSprite = EnsureAndLoadSprite(ChargeBgAssetPath);
            Sprite fillSprite = EnsureAndLoadSprite(ChargeFillAssetPath);

            GameObject chargeObj = new GameObject("SlingshotCharge");
            chargeObj.transform.SetParent(parent, false);

            RectTransform chargeRt = chargeObj.AddComponent<RectTransform>();
            chargeRt.anchorMin = new Vector2(0.5f, 0.5f);
            chargeRt.anchorMax = new Vector2(0.5f, 0.5f);
            chargeRt.pivot = new Vector2(0.5f, 0.5f);
            chargeRt.anchoredPosition = new Vector2(0f, -28f);
            chargeRt.sizeDelta = new Vector2(70f, 10f);

            CanvasGroup group = chargeObj.AddComponent<CanvasGroup>();
            group.alpha = 0f;

            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(chargeObj.transform, false);
            RectTransform bgRt = bgObj.AddComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.sizeDelta = Vector2.zero;

            Image bgImg = bgObj.AddComponent<Image>();
            bgImg.sprite = bgSprite;
            bgImg.color = Color.white;
            bgImg.raycastTarget = false;

            GameObject fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(chargeObj.transform, false);
            RectTransform fillRt = fillObj.AddComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.sizeDelta = Vector2.zero;

            Image fillImg = fillObj.AddComponent<Image>();
            fillImg.sprite = fillSprite;
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
            fillImg.fillAmount = 0f;
            fillImg.color = new Color(1f, 0.9f, 0.2f, 1f);
            fillImg.raycastTarget = false;

            SlingshotChargeHUD hud = chargeObj.AddComponent<SlingshotChargeHUD>();
            hud.BindComponents(group, bgImg, fillImg);
            hud.SetSprites(bgSprite, fillSprite);

            return hud;
        }

        private static Sprite EnsureAndLoadSprite(string assetPath)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null && (importer.textureType != TextureImporterType.Sprite || importer.mipmapEnabled))
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        }
    }
}

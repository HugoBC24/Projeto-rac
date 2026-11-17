using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using ProjectRac.UI;
using ProjectRac.Weapons;

namespace ProjectRac.Editor
{
    public static class WeaponHUDBuilder
    {
        private const string SlingshotIconPath = "Assets/Art/UI/slingshot_icon.png";
        private const string ShotgunIconPath = "Assets/Art/UI/shotgun_icon.png";

        public static WeaponHUD BuildWeaponHUD(Transform canvasTransform, GameObject player)
        {
            Sprite slingSprite = EnsureAndLoadSprite(SlingshotIconPath);
            Sprite shotgunSprite = EnsureAndLoadSprite(ShotgunIconPath);

            GameObject rootObj = new GameObject("WeaponHUD");
            rootObj.transform.SetParent(canvasTransform, false);

            RectTransform rootRt = rootObj.AddComponent<RectTransform>();
            rootRt.anchorMin = new Vector2(1f, 0f);
            rootRt.anchorMax = new Vector2(1f, 0f);
            rootRt.pivot = new Vector2(1f, 0f);
            rootRt.anchoredPosition = new Vector2(-40f, 40f);
            rootRt.sizeDelta = new Vector2(130f, 56f);

            // Slot 1 (Slingshot)
            var slot1 = CreateSlot(rootObj.transform, "Slot1_Slingshot", new Vector2(-68f, 0f), "1", slingSprite);

            // Slot 2 (Shotgun)
            var slot2 = CreateSlot(rootObj.transform, "Slot2_Shotgun", new Vector2(0f, 0f), "2", shotgunSprite);

            WeaponHUD hud = rootObj.AddComponent<WeaponHUD>();
            hud.BindComponents(slot1.bg, slot1.icon, slot1.outline, slot2.bg, slot2.icon, slot2.outline);
            hud.SetIcons(slingSprite, shotgunSprite);

            var inventory = player.GetComponent<PlayerWeaponInventory>();
            if (inventory != null)
            {
                inventory.BindWeaponHUD(hud);
            }

            return hud;
        }

        private static (Image bg, Image icon, Outline outline) CreateSlot(Transform parent, string name, Vector2 pos, string keyLabel, Sprite iconSprite)
        {
            GameObject slotObj = new GameObject(name);
            slotObj.transform.SetParent(parent, false);

            RectTransform slotRt = slotObj.AddComponent<RectTransform>();
            slotRt.anchorMin = new Vector2(1f, 0f);
            slotRt.anchorMax = new Vector2(1f, 0f);
            slotRt.pivot = new Vector2(1f, 0f);
            slotRt.anchoredPosition = pos;
            slotRt.sizeDelta = new Vector2(56f, 56f);

            Image bg = slotObj.AddComponent<Image>();
            bg.color = new Color(0.12f, 0.12f, 0.15f, 0.75f);
            bg.raycastTarget = false;

            Outline outline = slotObj.AddComponent<Outline>();
            outline.effectColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            // Icon
            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(slotObj.transform, false);
            RectTransform iconRt = iconObj.AddComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0.5f, 0.5f);
            iconRt.anchorMax = new Vector2(0.5f, 0.5f);
            iconRt.pivot = new Vector2(0.5f, 0.5f);
            iconRt.anchoredPosition = Vector2.zero;
            iconRt.sizeDelta = new Vector2(40f, 40f);

            Image icon = iconObj.AddComponent<Image>();
            icon.sprite = iconSprite;
            icon.color = Color.white;
            icon.raycastTarget = false;

            // Key badge label
            GameObject keyObj = new GameObject("KeyBadge");
            keyObj.transform.SetParent(slotObj.transform, false);
            RectTransform keyRt = keyObj.AddComponent<RectTransform>();
            keyRt.anchorMin = new Vector2(0f, 1f);
            keyRt.anchorMax = new Vector2(0f, 1f);
            keyRt.pivot = new Vector2(0f, 1f);
            keyRt.anchoredPosition = new Vector2(4f, -3f);
            keyRt.sizeDelta = new Vector2(18f, 16f);

            Text keyText = keyObj.AddComponent<Text>();
            keyText.text = keyLabel;
            keyText.fontSize = 11;
            keyText.fontStyle = FontStyle.Bold;
            keyText.alignment = TextAnchor.MiddleCenter;
            keyText.color = new Color(1f, 0.9f, 0.4f, 1f);
            keyText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            keyText.raycastTarget = false;

            return (bg, icon, outline);
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

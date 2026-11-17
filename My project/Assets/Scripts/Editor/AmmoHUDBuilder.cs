using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using ProjectRac.UI;

namespace ProjectRac.Editor
{
    public static class AmmoHUDBuilder
    {
        public static AmmoHUD BuildAmmoHUD(Transform canvasTransform)
        {
            var oldHUD = canvasTransform.Find("AmmoHUD");
            if (oldHUD != null)
            {
                Undo.DestroyObjectImmediate(oldHUD.gameObject);
            }

            // Remove old WeaponHUD if it exists
            var oldWeaponHUD = canvasTransform.Find("WeaponHUD");
            if (oldWeaponHUD != null)
            {
                Undo.DestroyObjectImmediate(oldWeaponHUD.gameObject);
            }

            Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                            ?? Resources.GetBuiltinResource<Font>("Arial.ttf");

            // Root Container
            GameObject ammoRoot = new GameObject("AmmoHUD");
            ammoRoot.transform.SetParent(canvasTransform, false);
            Undo.RegisterCreatedObjectUndo(ammoRoot, "Create AmmoHUD");

            RectTransform rootRt = ammoRoot.AddComponent<RectTransform>();
            rootRt.anchorMin = new Vector2(1f, 0f);
            rootRt.anchorMax = new Vector2(1f, 0f);
            rootRt.pivot = new Vector2(1f, 0f);
            rootRt.anchoredPosition = new Vector2(-40f, 40f);
            rootRt.sizeDelta = new Vector2(200f, 75f);

            CanvasGroup group = ammoRoot.AddComponent<CanvasGroup>();

            // Background Panel
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(ammoRoot.transform, false);
            RectTransform bgRt = bgObj.AddComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.sizeDelta = Vector2.zero;

            Image bgImg = bgObj.AddComponent<Image>();
            bgImg.color = new Color(0.08f, 0.08f, 0.1f, 0.85f);
            bgImg.raycastTarget = false;

            Outline bgOutline = bgObj.AddComponent<Outline>();
            bgOutline.effectColor = new Color(0.45f, 0.45f, 0.5f, 0.6f);
            bgOutline.effectDistance = new Vector2(1.5f, -1.5f);

            // Weapon Name Text (Top)
            GameObject nameObj = new GameObject("WeaponName");
            nameObj.transform.SetParent(ammoRoot.transform, false);
            RectTransform nameRt = nameObj.AddComponent<RectTransform>();
            nameRt.anchorMin = new Vector2(0f, 1f);
            nameRt.anchorMax = new Vector2(1f, 1f);
            nameRt.pivot = new Vector2(0.5f, 1f);
            nameRt.anchoredPosition = new Vector2(0f, -6f);
            nameRt.sizeDelta = new Vector2(180f, 22f);

            Text nameText = nameObj.AddComponent<Text>();
            nameText.text = "ESTILINGUE";
            nameText.font = defaultFont;
            nameText.fontSize = 13;
            nameText.fontStyle = FontStyle.Bold;
            nameText.alignment = TextAnchor.MiddleCenter;
            nameText.color = new Color(0.85f, 0.85f, 0.9f, 0.9f);
            nameText.raycastTarget = false;

            // Ammo Count Text (Bottom Big)
            GameObject countObj = new GameObject("AmmoCount");
            countObj.transform.SetParent(ammoRoot.transform, false);
            RectTransform countRt = countObj.AddComponent<RectTransform>();
            countRt.anchorMin = new Vector2(0f, 0f);
            countRt.anchorMax = new Vector2(1f, 1f);
            countRt.pivot = new Vector2(0.5f, 0.5f);
            countRt.anchoredPosition = new Vector2(0f, -10f);
            countRt.sizeDelta = new Vector2(180f, 38f);

            Text countText = countObj.AddComponent<Text>();
            countText.text = "∞";
            countText.font = defaultFont;
            countText.fontSize = 28;
            countText.fontStyle = FontStyle.Bold;
            countText.alignment = TextAnchor.MiddleCenter;
            countText.color = Color.white;
            countText.raycastTarget = false;

            Shadow countShadow = countObj.AddComponent<Shadow>();
            countShadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
            countShadow.effectDistance = new Vector2(1.5f, -1.5f);

            // Reload Status Text (Replaces AmmoCount when reloading)
            GameObject reloadObj = new GameObject("ReloadStatus");
            reloadObj.transform.SetParent(ammoRoot.transform, false);
            RectTransform reloadRt = reloadObj.AddComponent<RectTransform>();
            reloadRt.anchorMin = new Vector2(0f, 0f);
            reloadRt.anchorMax = new Vector2(1f, 1f);
            reloadRt.pivot = new Vector2(0.5f, 0.5f);
            reloadRt.anchoredPosition = new Vector2(0f, -10f);
            reloadRt.sizeDelta = new Vector2(180f, 34f);

            Text reloadText = reloadObj.AddComponent<Text>();
            reloadText.text = "RECARREGANDO...";
            reloadText.font = defaultFont;
            reloadText.fontSize = 15;
            reloadText.fontStyle = FontStyle.Bold;
            reloadText.alignment = TextAnchor.MiddleCenter;
            reloadText.color = new Color(1f, 0.85f, 0.25f, 1f);
            reloadText.raycastTarget = false;
            reloadObj.SetActive(false);

            AmmoHUD hud = ammoRoot.AddComponent<AmmoHUD>();
            hud.BindComponents(group, bgImg, nameText, countText, reloadText);

            return hud;
        }
    }
}

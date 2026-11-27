using UnityEngine;

namespace ProjectRac.UI
{
    // Controls the Left/Center/Right character portrait slots and computes safe text margins.
    internal static class VisualNovelPortraitStage
    {
        private const float BaseHorizontalPadding = 44f;
        private const float PortraitClearancePadding = 500f;
        private const float DualPortraitPadding = 475f;

        public static void ClearAll(UnityEngine.UI.Image left, UnityEngine.UI.Image center, UnityEngine.UI.Image right)
        {
            Hide(left);
            Hide(center);
            Hide(right);
        }

        public static void ApplyLine(
            VisualNovelDialogueLine line,
            UnityEngine.UI.Image left,
            UnityEngine.UI.Image center,
            UnityEngine.UI.Image right,
            bool dimInactive,
            Color inactiveColor)
        {
            if (!line.keepOtherCharacters)
            {
                ClearAll(left, center, right);
            }
            else if (dimInactive)
            {
                Dim(left, inactiveColor);
                Dim(center, inactiveColor);
                Dim(right, inactiveColor);
            }

            UnityEngine.UI.Image target = line.slot == CharacterSlot.Left ? left
                : line.slot == CharacterSlot.Center ? center
                : line.slot == CharacterSlot.Right ? right : null;
            if (target == null) return;

            if (line.characterSprite == null)
            {
                Hide(target);
                return;
            }

            target.sprite = line.characterSprite;
            target.color = Color.white;
            target.preserveAspect = true;

            Vector3 scale = target.rectTransform.localScale;
            scale.x = line.flipHorizontal ? -Mathf.Abs(scale.x) : Mathf.Abs(scale.x);
            target.rectTransform.localScale = scale;
            target.gameObject.SetActive(true);
        }

        public static void ApplySafeTextMargins(
            RectTransform textRect,
            UnityEngine.UI.Image left,
            UnityEngine.UI.Image right)
        {
            if (textRect == null) return;

            bool leftOn = left != null && left.gameObject.activeSelf;
            bool rightOn = right != null && right.gameObject.activeSelf;

            float leftPad = leftOn && rightOn ? DualPortraitPadding : (leftOn ? PortraitClearancePadding : BaseHorizontalPadding);
            float rightPad = leftOn && rightOn ? DualPortraitPadding : (rightOn ? PortraitClearancePadding : BaseHorizontalPadding);

            textRect.offsetMin = new Vector2(leftPad, 28f);
            textRect.offsetMax = new Vector2(-rightPad, -26f);
        }

        private static void Hide(UnityEngine.UI.Image img)
        {
            if (img == null) return;
            img.sprite = null;
            img.gameObject.SetActive(false);
        }

        private static void Dim(UnityEngine.UI.Image img, Color color)
        {
            if (img != null && img.gameObject.activeSelf) img.color = color;
        }
    }
}

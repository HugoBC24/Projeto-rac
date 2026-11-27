using UnityEngine;
using ProjectRac.Editor.UI;

public static class SetupVisualNovelDialogueInScene
{
    public static object Apply()
    {
        var ui = VisualNovelDialogueBuilder.EnsureVisualNovelDialogueUI();
        if (ui == null)
        {
            return new { ok = false, error = "HUD_Canvas not found or builder returned null." };
        }

        Transform canvas = ui.transform.parent;
        return new
        {
            ok = true,
            panelName = ui.gameObject.name,
            parentCanvas = canvas != null ? canvas.name : "null",
            siblingIndex = ui.transform.GetSiblingIndex(),
            siblingCount = canvas != null ? canvas.childCount : 0,
            dialogueCount = ui.LineCount
        };
    }
}

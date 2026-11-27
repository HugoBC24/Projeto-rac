using UnityEditor;
using UnityEngine;
using ProjectRac.Editor.Enemy;
using ProjectRac.Editor.Map;
using ProjectRac.Enemy;
using ProjectRac.Interaction;

public static class SetupBonecaAndLivingRoomEvent
{
    public static object Apply()
    {
        GameObject prefab = BonecaEnemyAssetSetup.BuildAll();
        GameObject fireplaceRoot = MansionLivingRoomBuilder.BuildLivingRoomFireplaceAndAmbush();

        var noteObj = fireplaceRoot != null ? fireplaceRoot.transform.Find("Nota_Lareira") : null;
        var ambush = noteObj != null ? noteObj.GetComponent<FireplaceNoteAmbushTrigger>() : null;
        var animator = prefab != null ? prefab.GetComponentInChildren<Animator>(true) : null;
        var clips = animator != null && animator.runtimeAnimatorController != null
            ? animator.runtimeAnimatorController.animationClips
            : new AnimationClip[0];

        var clipInfo = new System.Collections.Generic.List<string>();
        foreach (var c in clips)
        {
            clipInfo.Add($"{c.name} ({c.length:F2}s, loop={c.isLooping})");
        }

        return new
        {
            ok = prefab != null && fireplaceRoot != null && ambush != null && clips.Length == 3,
            prefabReady = prefab != null,
            clipCount = clips.Length,
            clips = clipInfo,
            fireplacePos = fireplaceRoot != null ? fireplaceRoot.transform.position.ToString("F2") : "null",
            ambushDialogueCount = ambush != null ? ambush.DialogueCount : 0,
            arenaMeleeUpgraded = GameObject.Find("MeleeEnemy")?.GetComponentInChildren<Animator>(true) != null
        };
    }
}

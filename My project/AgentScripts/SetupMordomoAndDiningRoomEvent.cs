using UnityEngine;
using ProjectRac.Editor.Enemy;
using ProjectRac.Editor.Map;
using ProjectRac.Interaction;

public static class SetupMordomoAndDiningRoomEvent
{
    public static object Apply()
    {
        GameObject prefab = MordomoEnemyAssetSetup.BuildAll();
        GameObject diningRoot = MansionDiningRoomBuilder.BuildDiningRoomTableAndMordomo();

        var trigObj = diningRoot != null ? diningRoot.transform.Find("DiningRoom_EntryTrigger") : null;
        var trigger = trigObj != null ? trigObj.GetComponent<DiningRoomEncounterTrigger>() : null;
        var animator = prefab != null ? prefab.GetComponentInChildren<Animator>(true) : null;
        var clips = animator != null && animator.runtimeAnimatorController != null
            ? animator.runtimeAnimatorController.animationClips
            : new AnimationClip[0];

        var clipInfo = new System.Collections.Generic.List<string>();
        foreach (var c in clips) clipInfo.Add($"{c.name} ({c.length:F2}s, loop={c.isLooping})");

        return new
        {
            ok = prefab != null && diningRoot != null && trigger != null && clips.Length == 3,
            prefabReady = prefab != null,
            clipCount = clips.Length,
            clips = clipInfo,
            encounterDialogueCount = trigger != null ? trigger.DialogueCount : 0,
            arenaRangedUpgraded = GameObject.Find("RangedEnemy")?.GetComponentInChildren<Animator>(true) != null
        };
    }
}

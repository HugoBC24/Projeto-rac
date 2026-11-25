using System.Linq;
using UnityEngine;
using ProjectRac.Editor.Map;
using ProjectRac.Interaction;

public static class BuildMansionInScene
{
    public static object Build()
    {
        var mansion = MansionMapBuilder.BuildAndRelocateTestArena();
        var arena = GameObject.Find("TestArena");
        var player = GameObject.Find("Player");
        var mansionDoors = mansion.GetComponentsInChildren<DoorController>(true);

        return new
        {
            ok = true,
            mansionPos = mansion.transform.position.ToString("F2"),
            testArenaPos = arena != null ? arena.transform.position.ToString("F2") : "NOT_FOUND",
            playerPos = player != null ? player.transform.position.ToString("F2") : "NOT_FOUND",
            doorCount = mansionDoors.Length,
            doorNames = mansionDoors.Select(d => d.transform.parent.parent.name).ToArray()
        };
    }
}

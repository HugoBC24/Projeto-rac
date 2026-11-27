using UnityEngine;
using ProjectRac.Editor.Map;
using ProjectRac.Weapons;

public static class SetupMansionWeaponsAndLoot
{
    public static object Apply()
    {
        GameObject lootRoot = MansionLootBuilder.BuildAll();
        var inv = Object.FindAnyObjectByType<PlayerWeaponInventory>();
        var slingshotPickup = Object.FindAnyObjectByType<SlingshotPickup>(FindObjectsInactive.Include);
        var ammoPickups = Object.FindObjectsByType<SlingshotAmmoPickup>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var chest = GameObject.Find("Bau_QuartoPais");

        return new
        {
            ok = lootRoot != null && slingshotPickup != null && ammoPickups.Length >= 5 && chest != null,
            playerStartsUnarmed = inv != null && !inv.HasSlingshot && !inv.HasShotgun && inv.ActiveSlot == 0,
            hasHallDrawerSlingshot = slingshotPickup != null,
            ammoPickupCount = ammoPickups.Length,
            hasParentsChest = chest != null
        };
    }
}

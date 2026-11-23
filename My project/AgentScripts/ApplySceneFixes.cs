using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using ProjectRac.Editor;
using ProjectRac.Weapons;

public static class ApplySceneFixes
{
    public static object Apply()
    {
        var activeScene = EditorSceneManager.GetActiveScene();
        var arena = GameObject.Find("TestArena");
        var player = GameObject.Find("Player");

        if (arena == null || player == null)
        {
            return new { ok = false, reason = "TestArena or Player not found in active scene." };
        }

        // 1. Align Slingshot and Shotgun weapon roots to camera origin so ShootPoint sits at muzzle height.
        var slingshot = player.GetComponentInChildren<SlingshotWeapon>(true);
        if (slingshot != null)
        {
            slingshot.transform.localPosition = Vector3.zero;
            slingshot.transform.localRotation = Quaternion.identity;
            slingshot.transform.localScale = Vector3.one;
        }

        var shotgun = player.GetComponentInChildren<ShotgunWeapon>(true);
        Vector3 shootPointWorld = Vector3.zero;
        if (shotgun != null)
        {
            shotgun.transform.localPosition = Vector3.zero;
            shotgun.transform.localRotation = Quaternion.identity;
            shotgun.transform.localScale = Vector3.one;

            var sp = shotgun.transform.Find("ShootPoint");
            if (sp != null)
            {
                sp.localPosition = new Vector3(0.24f, -0.22f, 1.05f);
                shotgun.SetShootPoint(sp);
                shootPointWorld = sp.position;
            }
        }

        // 2. Replace inflated CapsuleCollider on Shotgun_Pedestal with flush BoxCollider.
        var pedestal = arena.transform.Find("Shotgun_Pedestal");
        string pedestalBounds = "none";
        if (pedestal != null)
        {
            var oldCol = pedestal.GetComponent<Collider>();
            if (oldCol != null && !(oldCol is BoxCollider))
            {
                Object.DestroyImmediate(oldCol);
            }

            var pedBox = pedestal.GetComponent<BoxCollider>();
            if (pedBox == null) pedBox = pedestal.gameObject.AddComponent<BoxCollider>();
            pedBox.center = Vector3.zero;
            pedBox.size = new Vector3(1f, 2f, 1f);
            Physics.SyncTransforms();
            pedestalBounds = pedBox.bounds.ToString();
        }

        var pickup = arena.transform.Find("ShotgunPickup");
        string pickupBounds = "none";
        if (pickup != null)
        {
            var box = pickup.GetComponent<BoxCollider>();
            if (box != null)
            {
                box.size = new Vector3(0.8f, 0.5f, 1.5f);
                box.center = new Vector3(0f, 0.25f, 0f);
                Physics.SyncTransforms();
                pickupBounds = box.bounds.ToString();
            }
        }

        // 3. Build WallWithDoor in the middle of the arena and rebake NavMesh.
        var wallWithDoor = DoorArenaBuilder.BuildWallWithDoor(arena.transform);
        AssetDatabase.SaveAssets();
        EnemyBuilder.SetupNavMesh(arena.transform);

        EditorSceneManager.MarkSceneDirty(activeScene);
        EditorSceneManager.SaveScene(activeScene);

        return new
        {
            ok = true,
            scene = activeScene.path,
            shootPointWorld = shootPointWorld.ToString("F2"),
            pedestalBounds,
            pickupBounds,
            wallWithDoorPos = wallWithDoor.transform.position.ToString("F2")
        };
    }
}
